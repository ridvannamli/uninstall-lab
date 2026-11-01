using System.Windows;
using System.Windows.Controls;
using UninstallLab.Core;
using UninstallLab.Platform.Windows;

namespace UninstallLab.App;

public partial class MainWindow : Window
{
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
}
