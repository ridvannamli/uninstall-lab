using System.Windows;
using System.Windows.Controls;
using UninstallLab.Core;
using UninstallLab.Platform.Windows;

namespace UninstallLab.App;

public partial class MainWindow : Window
{
    private readonly UninstallService _uninstaller = new(new ProcessRunner());
    private readonly LeftoverScanner _scanner =
        new(new FileSystemProbe(), new RegistryProbe(), WindowsScanLocations.Default());

    private readonly CleanupExecutor _cleaner = new(
        new CleanupSystem(),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UninstallLab", "Backups"),
        restorePoint: new RestorePointCreator());

    private IReadOnlyList<InstalledProgram> _all = Array.Empty<InstalledProgram>();

    public MainWindow()
    {
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
