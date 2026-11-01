using System.Windows;
using System.Windows.Controls;
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

    private readonly LeftoverScanner _scanner =
        new(new FileSystemProbe(), new RegistryProbe(), WindowsScanLocations.Default());

    private readonly CleanupExecutor _cleaner = new(
        new CleanupSystem(),
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UninstallLab", "Backups"),
        restorePoint: new RestorePointCreator());

    private IReadOnlyList<InstalledProgram> _all = Array.Empty<InstalledProgram>();

    public MainWindow()
    {
        _batch = new BatchUninstaller(_uninstaller);
        InitializeComponent();
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

        if (!dryRun && result.Success) await LoadAsync();
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
        StoreList.ItemsSource = StoreAppFilter.Apply(_storeApps, StoreSearchBox.Text);

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

        if (!dryRun) await LoadAsync();
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        if (ProgramList.SelectedItem is not InstalledProgram program)
        {
            MessageBox.Show("Önce listeden bir program seçin.");
            return;
        }

        var items = await Task.Run(() => _scanner.Scan(program));
        var text = items.Count == 0
            ? "Kalıntı bulunamadı."
            : string.Join("\n", items.Take(40).Select(i => $"[risk {i.Risk}] {i.Path}"));
        MessageBox.Show(text + "\n\n(Yalnızca tarama: hiçbir şey silinmedi.)", "Kalıntı raporu");
    }

    private async void Clean_Click(object sender, RoutedEventArgs e)
    {
        if (ProgramList.SelectedItem is not InstalledProgram program)
        {
            MessageBox.Show("Önce listeden bir program seçin.");
            return;
        }

        var items = await Task.Run(() => _scanner.Scan(program));
        var safeCount = items.Count(i => i.SafeByDefault);
        if (safeCount == 0)
        {
            MessageBox.Show("Temizlenecek güvenli kalıntı bulunamadı.");
            return;
        }

        var dryRun = DryRunBox.IsChecked == true;
        if (!dryRun)
        {
            var answer = MessageBox.Show(
                $"{safeCount} güvenli kalıntı yedeklenip kaldırılacak (geri alınabilir). Devam edilsin mi?",
                "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }

        var report = await Task.Run(() => _cleaner.Execute(items, dryRun));
        MessageBox.Show(DescribeCleanup(report), "Temizlik raporu");
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

    private static string DescribeCleanup(CleanupReport r)
    {
        var lines = new List<string>();
        lines.Add(r.DryRun ? "Önizleme (hiçbir şey değiştirilmedi):" : $"Oturum: {r.SessionId}");
        lines.AddRange(r.Done);
        if (r.Skipped.Count > 0) lines.Add("\nAtlananlar:");
        lines.AddRange(r.Skipped);
        if (r.Failed.Count > 0) lines.Add("\nHatalar:");
        lines.AddRange(r.Failed);
        if (!r.DryRun) lines.Add(r.RestorePointCreated ? "\nGeri yükleme noktası oluşturuldu." : "\nGeri yükleme noktası oluşturulamadı (yedekler yine alındı).");
        return string.Join("\n", lines);
    }

    private static string Describe(UninstallResult r)
    {
        if (!r.Success) return $"Başarısız: {r.Error}";
        if (r.DryRun) return $"Çalıştırılacak komut:\n{r.Command!.FileName} {r.Command.Arguments}";
        return "Kaldırma tamamlandı.";
    }
}
