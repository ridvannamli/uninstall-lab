using System.Windows;
using UninstallLab.Core;

namespace UninstallLab.App;

/// <summary>
/// Kalıntı yollarını listeler. Kaldırma sonrası düşük riskli kalıntılar otomatik (yedekli) temizlenir;
/// kalanlar elle seçilip temizlenebilir. Silme adımları canlı günlükte görünür.
/// </summary>
public partial class LeftoverWindow : Window
{
    private readonly CleanupExecutor _cleaner;
    private readonly List<LeftoverRow> _rows;
    private readonly IReadOnlyList<LeftoverItem> _autoClean;
    private readonly string _subject;
    private readonly bool _dryRun;
    private readonly bool _stillInstalled;
    private bool _busy;

    /// <param name="autoClean">Pencere açılır açılmaz otomatik temizlenecek (güvenli) öğeler; boş olabilir.</param>
    public LeftoverWindow(
        string subject,
        IReadOnlyList<LeftoverItem> items,
        IReadOnlyList<LeftoverItem> autoClean,
        CleanupExecutor cleaner,
        bool dryRun,
        IReadOnlyList<string> stillInstalled)
    {
        InitializeComponent();

        _cleaner = cleaner;
        _autoClean = autoClean;
        _subject = subject;
        _dryRun = dryRun;
        _stillInstalled = stillInstalled.Count > 0;
        Title = $"Kalıntılar - {subject}";

        // Hâlâ yüklü görünen program varsa hiçbir şey önceden seçilmez.
        _rows = items.Select(i => new LeftoverRow(i) { Selected = i.SafeByDefault && !_stillInstalled }).ToList();
        ItemList.ItemsSource = _rows;

        if (_stillInstalled)
        {
            WarningText.Text = "Dikkat: şu program(lar) hâlâ yüklü görünüyor: " + string.Join(", ", stillInstalled) +
                               ". Bulunan öğeler kalıntı değil, programın kendisi olabilir; temizlemeden önce yolları dikkatle kontrol edin.";
            WarningText.Visibility = Visibility.Visible;
        }

        CleanButton.Content = dryRun ? "Önizle (silmez)" : "Seçilenleri temizle";
        UpdateHeader();
        CleanButton.IsEnabled = _rows.Count > 0;

        Log(dryRun
            ? "Önizleme modu: hiçbir şey silinmeyecek."
            : "Silme modu: klasörler yedeğe taşınır, registry anahtarları .reg olarak yedeklenip silinir. Hepsi geri alınabilir.");

        if (_autoClean.Count > 0)
        {
            HeaderText.Text = $"{subject}: {_autoClean.Count} güvenli kalıntı otomatik temizleniyor...";
            Loaded += async (_, _) => await RunCleanupAsync(_autoClean.ToList(), auto: true);
        }
    }

    private void UpdateHeader()
    {
        if (_rows.Count == 0)
        {
            HeaderText.Text = $"{_subject}: temizlenecek kalıntı kalmadı.";
            return;
        }

        var folders = _rows.Count(r => r.Item.Kind == LeftoverKind.Folder);
        HeaderText.Text = $"{_subject}: {_rows.Count} olası kalıntı ({folders} klasör, {_rows.Count - folders} registry anahtarı). " +
                          "Tam yollar aşağıda; düşük riskli olanlar önceden seçili.";
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

        if (!_dryRun)
        {
            var risky = chosen.Any(i => !i.SafeByDefault);
            var warning = risky || _stillInstalled
                ? "Yüksek riskli veya hâlâ yüklü görünen programa ait öğeler seçili olabilir. Yine de devam edilsin mi? (Geri alınabilir.)"
                : $"{chosen.Count} öğe yedeklenip kaldırılacak (geri alınabilir). Devam edilsin mi?";
            var answer = MessageBox.Show(this, warning, "Onay", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }

        await RunCleanupAsync(chosen, auto: false);
    }

    private async Task RunCleanupAsync(List<LeftoverItem> chosen, bool auto)
    {
        if (_busy) return;
        _busy = true;
        CleanButton.IsEnabled = false;

        Log(string.Empty);
        Log(_dryRun
            ? "--- Önizleme başladı ---"
            : auto ? "--- Otomatik temizlik başladı (yalnızca düşük riskli öğeler) ---" : "--- Temizlik başladı ---");

        var includeRisky = chosen.Any(i => !i.SafeByDefault);
        var progress = new Progress<string>(Log);
        var report = await Task.Run(() => _cleaner.Execute(chosen, _dryRun, includeRisky, progress));

        Log($"--- Bitti: {report.Done.Count} işlendi, {report.Skipped.Count} atlandı, {report.Failed.Count} hata ---");

        if (!_dryRun)
        {
            // Başarıyla işlenen satırları listeden çıkar; kalanlar (riskli/hatalı) elle incelenebilir.
            var processed = chosen
                .Where(item => report.Done.Any(d => d.EndsWith(": " + item.Path, StringComparison.OrdinalIgnoreCase)))
                .Select(item => item.Path)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            _rows.RemoveAll(r => processed.Contains(r.Path));
            ItemList.ItemsSource = _rows.ToList();
            UpdateHeader();

            if (_rows.Count == 0)
                HeaderText.Text = $"{_subject}: tüm kalıntılar temizlendi (yedekli, geri alınabilir).";
            else if (auto)
                HeaderText.Text += " Otomatik temizlik bitti; aşağıdakiler elle inceleme gerektirir.";

            Log("Geri almak için ana penceredeki 'Son temizliği geri al' düğmesini kullanın.");
        }

        _busy = false;
        CleanButton.IsEnabled = _rows.Count > 0;
    }
}
