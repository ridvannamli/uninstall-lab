using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UninstallLab.Core;
using UninstallLab.Platform.Windows;

namespace UninstallLab.App;

public partial class MainWindow : Window
{
    private readonly UninstallService _uninstaller = new(new ProcessRunner());
    private readonly BatchUninstaller _batch;
    private readonly PowerShellStoreAppSource _storeSource = new();
    private readonly StoreAppService _storeService = new(new PowerShellStoreAppRemover());
    private IReadOnlyList<StoreApp> _storeApps = Array.Empty<StoreApp>();
    private bool _storeLoaded;
    private readonly StartupManager _startup = new(new RegistryStartupStore());
    private IReadOnlyList<StartupEntry> _startupAll = Array.Empty<StartupEntry>();
    private bool _startupLoaded;
    private readonly BrowserExtensionSource _extensionSource = new();
    private IReadOnlyList<BrowserExtension> _extensionsAll = Array.Empty<BrowserExtension>();
    private bool _extensionsLoaded;

    private readonly LeftoverScanner _scanner =
        new(new FileSystemProbe(), new RegistryProbe(), WindowsScanLocations.Default());

    private static readonly string BackupRoot = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UninstallLab", "Backups");

    private readonly CleanupExecutor _cleaner = new(
        new CleanupSystem(),
        BackupRoot,
        restorePoint: new RestorePointCreator());

    private IReadOnlyList<InstalledProgram> _all = Array.Empty<InstalledProgram>();

    public MainWindow()
    {
        _batch = new BatchUninstaller(_uninstaller);
        InitializeComponent();

        var settings = SettingsFile.Load();
        QuietBox.IsChecked = settings.QuietByDefault;
        ShowSystemBox.IsChecked = settings.ShowSystemStoreApps;
        Closing += (_, _) => SettingsFile.Save(new AppSettings(
            QuietByDefault: QuietBox.IsChecked == true,
            ShowSystemStoreApps: ShowSystemBox.IsChecked == true));
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _all = await Task.Run(() => new RegistryProgramSource().GetPrograms());
        Refresh();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();

    private void Refresh() =>
        ProgramList.ItemsSource = ProgramFilter.Apply(_all, SearchBox.Text);

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        if (ProgramList.SelectedItem is not InstalledProgram program)
        {
            MessageBox.Show("Önce listeden bir program seçin.");
            return;
        }

        var dryRun = DryRunBox.IsChecked == true;
        if (!dryRun)
        {
            var answer = MessageBox.Show($"'{program.Name}' kaldırılsın mı?", "Onay",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }

        var result = await _uninstaller.UninstallAsync(program, quiet: false, dryRun: dryRun);
        MessageBox.Show(Describe(result));

        if (!dryRun && result.Success)
        {
            await LoadAsync();
            // Kaldırma sonrası otomatik kalıntı taraması: yollar listelenir, seçilenler canlı günlükle temizlenir.
            await ShowLeftoversAsync(new[] { program }, program.Name);
        }
    }

    private async void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // ListView seçimleri de buraya kabarcıklanır; yalnızca sekme değişimine tepki ver.
        if (!ReferenceEquals(e.OriginalSource, Tabs)) return;

        if (ReferenceEquals(Tabs.SelectedItem, StoreTab) && !_storeLoaded)
        {
            _storeLoaded = true;
            await LoadStoreAsync();
        }
        else if (ReferenceEquals(Tabs.SelectedItem, StartupTab) && !_startupLoaded)
        {
            _startupLoaded = true;
            await LoadStartupAsync();
        }
        else if (ReferenceEquals(Tabs.SelectedItem, ExtensionsTab) && !_extensionsLoaded)
        {
            _extensionsLoaded = true;
            await LoadExtensionsAsync();
        }
    }

    private async Task LoadExtensionsAsync()
    {
        try
        {
            _extensionsAll = await Task.Run(() => _extensionSource.GetExtensions());
        }
        catch (Exception ex)
        {
            _extensionsLoaded = false;
            MessageBox.Show($"Eklentiler okunamadı: {ex.Message}");
            return;
        }

        RefreshExtensions();
    }

    private void ExtensionSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshExtensions();

    private void RefreshExtensions() =>
        ExtensionList.ItemsSource = ExtensionFilter.Apply(_extensionsAll, ExtensionSearchBox.Text)
            .Select(x => new ExtensionRow(x))
            .ToList();

    private void ExtensionPage_Click(object sender, RoutedEventArgs e)
    {
        if (ExtensionList.SelectedItem is not ExtensionRow row)
        {
            MessageBox.Show("Önce listeden bir eklenti seçin.");
            return;
        }

        try
        {
            BrowserLauncher.OpenExtensionsPage(row.Extension.Browser);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Tarayıcı açılamadı: {ex.Message}");
        }
    }

    private void ExtensionFolder_Click(object sender, RoutedEventArgs e)
    {
        if (ExtensionList.SelectedItem is not ExtensionRow row)
        {
            MessageBox.Show("Önce listeden bir eklenti seçin.");
            return;
        }

        try
        {
            BrowserLauncher.OpenFolder(row.Extension.Path);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Klasör açılamadı: {ex.Message}");
        }
    }

    private async Task LoadStartupAsync()
    {
        try
        {
            _startupAll = await Task.Run(() => _startup.List());
        }
        catch (Exception ex)
        {
            _startupLoaded = false;
            MessageBox.Show($"Başlangıç girdileri okunamadı: {ex.Message}");
            return;
        }

        RefreshStartup();
    }

    private void StartupSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshStartup();

    private void RefreshStartup() =>
        StartupList.ItemsSource = StartupFilter.Apply(_startupAll, StartupSearchBox.Text)
            .Select(entry => new StartupRow(entry))
            .ToList();

    private async void StartupDisable_Click(object sender, RoutedEventArgs e) => await ToggleStartupAsync(false);

    private async void StartupEnable_Click(object sender, RoutedEventArgs e) => await ToggleStartupAsync(true);

    private async Task ToggleStartupAsync(bool enable)
    {
        if (StartupList.SelectedItem is not StartupRow row)
        {
            MessageBox.Show("Önce listeden bir girdi seçin.");
            return;
        }

        var result = await Task.Run(() => _startup.SetEnabled(row.Entry, enable));
        if (!result.Success)
        {
            MessageBox.Show($"Başarısız: {result.Error}");
            return;
        }

        _startupAll = await Task.Run(() => _startup.List());
        RefreshStartup();
    }

    private async Task LoadStoreAsync()
    {
        try
        {
            _storeApps = await _storeSource.GetAppsAsync();
        }
        catch (Exception ex)
        {
            _storeLoaded = false;
            MessageBox.Show($"Mağaza uygulamaları okunamadı: {ex.Message}");
            return;
        }

        RefreshStore();
    }

    private void StoreSearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshStore();

    private void RefreshStore() =>
        StoreList.ItemsSource = StoreAppFilter.Apply(_storeApps, StoreSearchBox.Text, ShowSystemBox.IsChecked == true);

    private void ShowSystem_Changed(object sender, RoutedEventArgs e) => RefreshStore();

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var version = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?";
        MessageBox.Show(
            $"Uninstall Lab {version}\nMIT Lisansı - Rıdvan Namlı\n\n" +
            $"Yedekler: {BackupRoot}\nAyarlar: {SettingsFile.FilePath}\n\n" +
            "Bu yazılım \"olduğu gibi\" sunulur. Silme ve kaldırma işlemlerini önce sanal makinede veya " +
            "Windows Sandbox'ta deneyin. \"Sadece göster\" kutusu her açılışta işaretli başlar.",
            "Hakkında");
    }

    private async void StoreRemove_Click(object sender, RoutedEventArgs e)
    {
        if (StoreList.SelectedItem is not StoreApp app)
        {
            MessageBox.Show("Önce listeden bir uygulama seçin.");
            return;
        }

        var dryRun = DryRunBox.IsChecked == true;
        if (!dryRun)
        {
            var answer = MessageBox.Show($"'{app.Name}' kaldırılsın mı?", "Onay",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }

        var result = await _storeService.RemoveAsync(app, dryRun);
        MessageBox.Show(!result.Success
            ? $"Başarısız: {result.Error}"
            : result.DryRun ? $"Çalıştırılacak komut:\n{result.Command}" : "Kaldırıldı.");

        if (!dryRun && result.Success) await LoadStoreAsync();
    }

    private async void Batch_Click(object sender, RoutedEventArgs e)
    {
        var selected = ProgramList.SelectedItems.OfType<InstalledProgram>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Önce listeden bir veya daha fazla program seçin (Ctrl/Shift ile çoklu seçim).");
            return;
        }

        var dryRun = DryRunBox.IsChecked == true;
        if (!dryRun)
        {
            var answer = MessageBox.Show($"{selected.Count} program sırayla kaldırılacak. Devam edilsin mi?",
                "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }

        var progress = new Progress<BatchProgress>(p => Title = $"Uninstall Lab — {p.Index}/{p.Total}: {p.Name}");
        var results = await _batch.RunAsync(selected, QuietBox.IsChecked == true, dryRun, progress);
        Title = "Uninstall Lab";

        var ok = results.Count(r => r.Result.Success);
        var lines = results.Select(r =>
            (r.Result.Success ? "[OK] " : "[HATA] ") + r.Program.Name +
            (r.Result.Error is null ? string.Empty : $": {r.Result.Error}"));
        MessageBox.Show($"{ok}/{results.Count} başarılı{(dryRun ? " (önizleme)" : string.Empty)}\n\n" + string.Join("\n", lines),
            "Toplu kaldırma");

        if (dryRun) return;

        var removed = results.Where(r => r.Result.Success).Select(r => r.Program).ToList();
        await LoadAsync();
        if (removed.Count > 0)
            await ShowLeftoversAsync(removed, removed.Count == 1 ? removed[0].Name : $"{removed.Count} program");
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        var selected = ProgramList.SelectedItems.OfType<InstalledProgram>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Önce listeden bir veya daha fazla program seçin.");
            return;
        }

        await ShowLeftoversAsync(selected, selected.Count == 1 ? selected[0].Name : $"{selected.Count} program");
    }

    /// <summary>Programların kalıntılarını tarar ve yolları listeleyen pencereyi açar.</summary>
    private async Task ShowLeftoversAsync(IReadOnlyList<InstalledProgram> programs, string title)
    {
        var items = await Task.Run(() => programs
            .SelectMany(p => _scanner.Scan(p))
            .GroupBy(i => (i.Kind, Path: i.Path.ToLowerInvariant()))
            .Select(g => g.OrderBy(i => i.Risk).First())
            .OrderBy(i => i.Risk)
            .ThenBy(i => i.Path, StringComparer.OrdinalIgnoreCase)
            .ToList());

        // Liste yenilendikten sonra hâlâ görünen program, kaldırılmamış demektir; kullanıcı uyarılır.
        var stillInstalled = programs
            .Where(p => _all.Any(a => a.Name == p.Name && a.Version == p.Version))
            .Select(p => p.Name)
            .ToList();

        var window = new LeftoverWindow(title, items, _cleaner, DryRunBox.IsChecked == true, stillInstalled)
        {
            Owner = this,
        };
        window.ShowDialog();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshCurrentTabAsync();

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F5) return;
        e.Handled = true;
        await RefreshCurrentTabAsync();
    }

    /// <summary>Etkin sekmenin listesini yeniden okur (yeni kurulan programlar görünsün diye).</summary>
    private async Task RefreshCurrentTabAsync()
    {
        if (ReferenceEquals(Tabs.SelectedItem, StoreTab))
        {
            _storeLoaded = true;
            await LoadStoreAsync();
        }
        else if (ReferenceEquals(Tabs.SelectedItem, StartupTab))
        {
            _startupLoaded = true;
            await LoadStartupAsync();
        }
        else if (ReferenceEquals(Tabs.SelectedItem, ExtensionsTab))
        {
            _extensionsLoaded = true;
            await LoadExtensionsAsync();
        }
        else
        {
            await LoadAsync();
        }
    }

    private async void Undo_Click(object sender, RoutedEventArgs e)
    {
        var id = _cleaner.LatestSessionId();
        if (id is null)
        {
            MessageBox.Show("Geri alınacak bir temizlik yok.");
            return;
        }

        var answer = MessageBox.Show($"'{id}' temizliği geri alınsın mı?", "Onay",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        var result = await Task.Run(() => _cleaner.Undo(id));
        var text = $"Geri yüklenen öğe: {result.Restored}";
        if (result.Failed.Count > 0) text += "\n\nSorunlar:\n" + string.Join("\n", result.Failed);
        MessageBox.Show(text, "Geri alma");
    }

    private static string Describe(UninstallResult r)
    {
        if (!r.Success) return $"Başarısız: {r.Error}";
        if (r.DryRun) return $"Çalıştırılacak komut:\n{r.Command!.FileName} {r.Command.Arguments}";
        return "Kaldırma tamamlandı.";
    }
}
