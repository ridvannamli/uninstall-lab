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

    private static string Describe(UninstallResult r)
    {
        if (!r.Success) return $"Başarısız: {r.Error}";
        if (r.DryRun) return $"Çalıştırılacak komut:\n{r.Command!.FileName} {r.Command.Arguments}";
        return "Kaldırma tamamlandı.";
    }
}
