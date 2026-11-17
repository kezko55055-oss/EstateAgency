using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Models;
using EstateAgency.Services;

namespace EstateAgency.Views;

public partial class RequestDetailsView : UserControl
{
    // Номер открытой заявки. Задаётся при создании страницы:
    //     new RequestDetailsView { RequestId = 7 }
    public int RequestId { get; set; }

    private int managerId;              // менеджер объекта: для списка свободных часов
    private Func<Task>? pendingAction;  // что выполнить после подтверждения

    public RequestDetailsView()
    {
        InitializeComponent();
        NewDateBox.DisplayDateStart = DateTime.Today;
        MoveDateBox.DisplayDateStart = DateTime.Today;
        Loaded += async (s, e) => await LoadRequestAsync();
    }

    // Загрузить заявку и показать её состав.
    private async Task LoadRequestAsync()
    {
        try
        {
            ConfirmPanel.IsVisible = false;
            Request? request = await RequestService.GetRequestAsync(RequestId);
            if (request == null)
            {
                // Заявки больше нет (удалён её последний показ).
                MainWindow.Instance.OpenPage(new RequestsView(), clearHistory: true);
                MainWindow.Instance.ShowMessage("Заявка удалена, показы возвращены.");
                return;
            }
            string status = request.Status.StatusName;
            bool active = status == "Новая" || status == "В работе";
            managerId = request.Property.ManagerId;

            HeaderText.Text = $"Заявка № {request.RequestId} · {status}";
            // Контакты клиента видит только сотрудник.
            ClientText.Text = Session.IsStaff
                ? $"Клиент: {request.Client.FullName}, {request.Client.Phone}, {request.Client.Email}"
                : $"Клиент: {request.Client.FullName}";
            PropertyText.Text = $"Объект: {request.Property.PropertyCode} · {request.Property.Title} · {request.Property.Address}";
            InfoText.Text = $"Тип: {request.Type.TypeName} · создана {request.CreatedAt:dd.MM.yyyy} · менеджер: {request.Property.Manager.FullName}" +
                (string.IsNullOrWhiteSpace(request.Notes) ? "" : $" · примечание: {request.Notes}");
            CommissionText.Text = $"Комиссия на момент заявки: {request.CommissionAmount:N2} руб.";
            ViewingsList.ItemsSource = request.Viewings.OrderBy(v => v.ScheduledAt).ToList();

            // Настройка интерфейса под роль. Сами права ещё раз проверяет RequestService.
            StaffPanel.IsVisible = Session.IsStaff && active;
            AdminPanel.IsVisible = Session.IsAdmin && active;
            CompleteButton.IsVisible = Session.IsStaff && active;
            CancelRequestButton.IsVisible = active && (Session.IsStaff || status == "Новая");

            NewDateBox.SelectedDate ??= DateTime.Today.AddDays(1);
            MoveDateBox.SelectedDate ??= DateTime.Today.AddDays(1);
            await LoadHoursAsync(NewDateBox, NewHourBox);
            await LoadHoursAsync(MoveDateBox, MoveHourBox);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Заполнить список часов свободным временем менеджера.
    private async Task LoadHoursAsync(CalendarDatePicker date, ComboBox hours)
    {
        if (date.SelectedDate == null || managerId == 0 || !Session.IsStaff)
            return;
        List<string> free = await RequestService.GetFreeHoursAsync(
            managerId, DateOnly.FromDateTime(date.SelectedDate.Value));
        hours.ItemsSource = free;
        hours.SelectedIndex = free.Count > 0 ? 0 : -1;
    }

    // Дата и час из пары элементов формы.
    private static DateTime ReadTime(CalendarDatePicker date, ComboBox hours)
    {
        if (date.SelectedDate == null)
            throw new AppException("Выберите дату.");
        if (hours.SelectedItem is not string hour)
            throw new AppException("На эту дату нет свободного времени. Выберите другую дату.");
        return date.SelectedDate.Value.Date.AddHours(int.Parse(hour[..2]));
    }

    private Viewing SelectedViewing()
    {
        if (ViewingsList.SelectedItem is not Viewing viewing)
            throw new AppException("Сначала выберите показ в списке.");
        return viewing;
    }

    // Выполнить действие и обновить страницу; ошибка — в строку сообщений.
    private async Task RunAsync(Func<Task> action, string doneMessage)
    {
        try
        {
            await action();
            await LoadRequestAsync();
            MainWindow.Instance.ShowMessage(doneMessage);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Показать вопрос «Вы уверены?»: действие выполнится только после подтверждения.
    private void Ask(string question, Func<Task> action)
    {
        pendingAction = action;
        ConfirmText.Text = question;
        ConfirmPanel.IsVisible = true;
    }

    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        await LoadRequestAsync();
    }

    private async void NewDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        try { await LoadHoursAsync(NewDateBox, NewHourBox); }
        catch (Exception ex) { MainWindow.Instance.ShowError(ex); }
    }

    private async void MoveDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        try { await LoadHoursAsync(MoveDateBox, MoveHourBox); }
        catch (Exception ex) { MainWindow.Instance.ShowError(ex); }
    }

    // Кнопка «Назначить показ» (сотрудник).
    private async void AddViewingClick(object? sender, RoutedEventArgs e)
    {
        await RunAsync(() => RequestService.AddViewingAsync(RequestId, ReadTime(NewDateBox, NewHourBox)),
            "Показ назначен, остаток показов объекта уменьшен.");
    }

    // Кнопка «Отметить выбранный показ проведённым» (сотрудник).
    private async void RecordClick(object? sender, RoutedEventArgs e)
    {
        await RunAsync(async () =>
        {
            Viewing viewing = SelectedViewing();
            if (ResultBox.SelectedItem is not ComboBoxItem result)
                throw new AppException("Укажите результат показа.");
            // Показ считается начавшимся в назначенное время и длившимся один час.
            DateTime start = viewing.ScheduledAt;
            await RequestService.RecordResultAsync(viewing.ViewingId, start, start.AddHours(1),
                (string)result.Content!, FeedbackBox.Text);
        }, "Результат показа записан.");
    }

    // Кнопка «Сохранить дату» (администратор).
    private async void SaveDateClick(object? sender, RoutedEventArgs e)
    {
        await RunAsync(() => RequestService.ChangeViewingDateAsync(
                SelectedViewing().ViewingId, ReadTime(MoveDateBox, MoveHourBox)),
            "Показ перенесён. Комиссия заявки не изменилась.");
    }

    // Кнопка «Удалить выбранный показ» (администратор): только показываем вопрос.
    private void DeleteViewingClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            int id = SelectedViewing().ViewingId;
            Ask("Удалить показ? Он вернётся в остаток объекта. Если показ последний, удалится и заявка.",
                () => RequestService.DeleteViewingAsync(id));
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопка «Отменить заявку»: только показываем вопрос.
    private void CancelRequestClick(object? sender, RoutedEventArgs e)
    {
        Ask("Отменить заявку? Запланированные показы вернутся в остаток объекта.",
            () => RequestService.CancelRequestAsync(RequestId));
    }

    // Кнопка «Завершить сделку»: только показываем вопрос.
    private void CompleteClick(object? sender, RoutedEventArgs e)
    {
        Ask("Завершить сделку? Объект будет закрыт, остальные заявки по нему отменятся.",
            () => RequestService.CompleteAsync(RequestId));
    }

    // Кнопка «Отмена» в панели подтверждения: ничего не меняем.
    private void ConfirmCancelClick(object? sender, RoutedEventArgs e)
    {
        pendingAction = null;
        ConfirmPanel.IsVisible = false;
    }

    // Кнопка «Да, выполнить».
    private async void ConfirmClick(object? sender, RoutedEventArgs e)
    {
        Func<Task>? action = pendingAction;
        pendingAction = null;
        ConfirmPanel.IsVisible = false;
        if (action != null)
            await RunAsync(action, "Операция выполнена.");
    }
}
