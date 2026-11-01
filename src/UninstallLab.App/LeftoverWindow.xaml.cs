using System.Windows;
using UninstallLab.Core;

namespace UninstallLab.App;

/// <summary>Kalıntı yollarını listeler; seçilenleri temizler ve silme adımlarını canlı günlükte gösterir.</summary>
public partial class LeftoverWindow : Window
{
    private readonly CleanupExecutor _cleaner;
    private readonly IReadOnlyList<LeftoverRow> _rows;
    private readonly bool _dryRun;
    private readonly bool _stillInstalled;

    public LeftoverWindow(
        string title,
        IReadOnlyList<LeftoverItem> items,
        CleanupExecutor cleaner,
        bool dryRun,
        IReadOnlyList<string> stillInstalled)
    {
        InitializeComponent();

        _cleaner = cleaner;
        _dryRun = dryRun;
        _stillInstalled = stillInstalled.Count > 0;
        Title = $"Kalıntılar - {title}";

        _rows = items.Select(i => new LeftoverRow(i)).ToList();
        ItemList.ItemsSource = _rows;

        var folders = items.Count(i => i.Kind == LeftoverKind.Folder);
        var keys = items.Count - folders;
        HeaderText.Text = items.Count == 0
            ? $"{title}: kalıntı bulunamadı."
            : $"{title}: {items.Count} olası kalıntı bulundu ({folders} klasör, {keys} registry anahtarı). " +
              "Tam yollar aşağıda; düşük riskli olanlar önceden seçili.";

        if (_stillInstalled)
        {
            WarningText.Text = "Dikkat: şu program(lar) hâlâ yüklü görünüyor: " + string.Join(", ", stillInstalled) +
                               ". Bulunan öğeler kalıntı değil, programın kendisi olabilir; temizlemeden önce yolları dikkatle kontrol edin.";
            WarningText.Visibility = Visibility.Visible;
        }

        CleanButton.Content = dryRun ? "Önizle (silmez)" : "Seçilenleri temizle";
        CleanButton.IsEnabled = items.Count > 0;

        Log(dryRun
            ? "Önizleme modu: hiçbir şey silinmeyecek."
            : "Silme modu: klasörler yedeğe taşınır, registry anahtarları .reg olarak yedeklenip silinir. Hepsi geri alınabilir.");
    }

    private void Log(string line)
    {
        LogBox.AppendText(line + Environment.NewLine);
        LogBox.ScrollToEnd();
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e) => SetAll(true);

    private void SelectNone_Click(object sender, RoutedEventArgs e) => SetAll(false);

    private void SetAll(bool value)
    {
        foreach (var row in _rows) row.Selected = value;
        ItemList.Items.Refresh();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void Clean_Click(object sender, RoutedEventArgs e)
    {
        var chosen = _rows.Where(r => r.Selected).Select(r => r.Item).ToList();
        if (chosen.Count == 0)
        {
            Log("Seçili öğe yok.");
            return;
        }

        var includeRisky = chosen.Any(i => !i.SafeByDefault);

        if (!_dryRun)
        {
            var warning = includeRisky || _stillInstalled
                ? "Yüksek riskli veya hâlâ yüklü görünen programa ait öğeler seçili olabilir. Yine de devam edilsin mi? (Geri alınabilir.)"
                : $"{chosen.Count} öğe yedeklenip kaldırılacak (geri alınabilir). Devam edilsin mi?";
            var answer = MessageBox.Show(this, warning, "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }

        CleanButton.IsEnabled = false;
        Log(string.Empty);
        Log(_dryRun ? "--- Önizleme başladı ---" : "--- Temizlik başladı ---");

        var progress = new Progress<string>(Log);
        var report = await Task.Run(() => _cleaner.Execute(chosen, _dryRun, includeRisky, progress));

        Log($"--- Bitti: {report.Done.Count} işlendi, {report.Skipped.Count} atlandı, {report.Failed.Count} hata ---");
        if (!_dryRun)
            Log("Geri almak için ana penceredeki 'Son temizliği geri al' düğmesini kullanın.");

        // Gerçek temizlikten sonra aynı öğeler tekrar işlenmesin; önizleme tekrar çalıştırılabilir.
        CleanButton.IsEnabled = _dryRun;
    }
}
