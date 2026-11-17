using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Models;
using EstateAgency.Services;

namespace EstateAgency.Views;

public partial class RequestsView : UserControl
{
    public RequestsView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadRequestsAsync();
    }

    // Загрузить заявки: сервис сам отбирает те, что разрешены текущей роли.
    private async Task LoadRequestsAsync()
    {
        try
        {
            HeaderText.Text = Session.IsAdmin ? "Все заявки"
                : Session.IsStaff ? "Заявки по моим объектам" : "Мои заявки";
            List<Request> requests = await RequestService.GetRequestsAsync();
            RequestsList.ItemsSource = requests;
            CountText.Text = $"Заявок: {requests.Count}";
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопка «Обновить».
    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        await LoadRequestsAsync();
    }

    // Кнопка «Новая заявка»: переходим в каталог, там выбираем объект.
    private void NewClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.OpenPage(new CatalogView());
    }

    // Кнопка «Открыть заявку».
    private void OpenClick(object? sender, RoutedEventArgs e)
    {
        if (RequestsList.SelectedItem is not Request request)
        {
            MainWindow.Instance.ShowMessage("Сначала выберите заявку в списке.");
            return;
        }
        MainWindow.Instance.OpenPage(new RequestDetailsView { RequestId = request.RequestId });
    }
}
