using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Models;
using EstateAgency.Services;

namespace EstateAgency.Views;

public partial class PropertiesManagementView : UserControl
{
    private int propertyToDelete;

    public PropertiesManagementView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            ConfirmPanel.IsVisible = false;
            DeleteButton.IsVisible = Session.IsAdmin; // удаление — только администратор
            PropertiesList.ItemsSource = await PropertyService.GetPropertiesAsync();
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

    private void AddClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.OpenPage(new PropertyEditView());
    }

    private void EditClick(object? sender, RoutedEventArgs e)
    {
        if (PropertiesList.SelectedItem is not Property property)
        {
            MainWindow.Instance.ShowMessage("Сначала выберите объект в списке.");
            return;
        }
        MainWindow.Instance.OpenPage(new PropertyEditView { PropertyId = property.PropertyId });
    }

    private void DeleteClick(object? sender, RoutedEventArgs e)
    {
        if (PropertiesList.SelectedItem is not Property property)
        {
            MainWindow.Instance.ShowMessage("Сначала выберите объект в списке.");
            return;
        }
        propertyToDelete = property.PropertyId;
        ConfirmText.Text = $"Удалить объект {property.PropertyCode}? Объект с заявками удалить нельзя.";
        ConfirmPanel.IsVisible = true;
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        ConfirmPanel.IsVisible = false;
    }

    private async void ConfirmClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await PropertyService.DeleteAsync(propertyToDelete);
            await LoadAsync();
            MainWindow.Instance.ShowMessage("Объект удалён.");
        }
        catch (Exception ex)
        {
            ConfirmPanel.IsVisible = false;
            MainWindow.Instance.ShowError(ex);
        }
    }
}
