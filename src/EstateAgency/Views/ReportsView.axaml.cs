using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Services;

namespace EstateAgency.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            RowsList.ItemsSource = await ReportService.GetReportAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }
}
