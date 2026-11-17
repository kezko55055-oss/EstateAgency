using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Data;
using EstateAgency.Models;
using EstateAgency.Services;
using Microsoft.EntityFrameworkCore;

namespace EstateAgency.Views;

public partial class PropertyView : UserControl
{
    // Номер открытого объекта. Задаётся при создании страницы:
    //     new PropertyView { PropertyId = 5 }
    public int PropertyId { get; set; }

    private Property? property;
    private bool hadRequests;       // были ли заявки в прошлом месяце
    private decimal shownCommission; // комиссия, которую увидел пользователь

    public PropertyView()
    {
        InitializeComponent();
        DateBox.DisplayDateStart = DateTime.Today;
        Loaded += async (s, e) => await LoadPropertyAsync();
    }

    // Загрузить объект, типы заявок и (для сотрудника) список клиентов.
    private async Task LoadPropertyAsync()
    {
        try
        {
            SaveButton.IsEnabled = false; // пока данные не загружены — не оформляем
            using var db = Db.Create();
            property = await db.Properties
                .Include(p => p.Type)
                .Include(p => p.Status)
                .Include(p => p.Manager)
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.PropertyId == PropertyId);
            if (property == null)
                throw new AppException("Объект не найден. Вернитесь в каталог.");

            string status = property.Status.StatusName;
            bool open = PriceRules.IsOpen(status);
            NameText.Text = property.Title;
            PropertyImage.Source = UiSupport.Picture(property.ImagePath);
            AddressText.Text = "Адрес: " + property.Address;
            ParamsText.Text = $"{property.Type.TypeName} · {property.Area:0.##} м² · комнат: {property.Rooms} · код {property.PropertyCode}";
            DescriptionText.Text = property.Description ?? "Описание не указано.";
            ManagerText.Text = "Менеджер: " + property.Manager.FullName;
            PriceText.Text = PriceRules.DealType(status) == PriceRules.Rent
                ? $"{status}: {property.Price:N2} руб./мес." : $"{status}: {property.Price:N2} руб.";
            ViewingsText.Text = $"Осталось показов: {property.FreeViewings} ({PriceRules.Availability(property.FreeViewings)})";

            hadRequests = await CatalogService.HadRequestsAsync(db, property.PropertyId);

            // Только типы заявок, допустимые для статуса этого объекта.
            List<RequestType> types = (await db.RequestTypes.AsNoTracking().ToListAsync())
                .Where(t => PriceRules.TypeAllowed(status, t.TypeName))
                .OrderByDescending(t => t.CommissionRate)
                .ToList();
            TypeBox.ItemsSource = types;
            TypeBox.SelectedIndex = types.Count > 0 ? 0 : -1;

            // Выбор клиента — только для менеджера и администратора.
            ClientPanel.IsVisible = Session.IsStaff;
            if (Session.IsStaff)
            {
                List<User> clients = await Session.GetClientsAsync();
                ClientBox.ItemsSource = clients;
                ClientBox.SelectedIndex = clients.Count > 0 ? 0 : -1;
            }

            DateBox.SelectedDate ??= DateTime.Today.AddDays(1);
            await LoadHoursAsync();

            bool canApply = open && property.FreeViewings > 0;
            SaveButton.IsEnabled = canApply;
            if (!open)
                MainWindow.Instance.ShowMessage("Сделка по объекту закрыта, заявку подать нельзя.");
            else if (property.FreeViewings == 0)
                MainWindow.Instance.ShowMessage("Показы по объекту закончились, заявку подать нельзя.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Свободные часы менеджера на выбранную дату.
    private async Task LoadHoursAsync()
    {
        if (property == null || DateBox.SelectedDate == null)
            return;
        List<string> hours = await RequestService.GetFreeHoursAsync(
            property.ManagerId, DateOnly.FromDateTime(DateBox.SelectedDate.Value));
        HourBox.ItemsSource = hours;
        HourBox.SelectedIndex = hours.Count > 0 ? 0 : -1;
    }

    // Сменился тип заявки — пересчитываем комиссию на экране.
    private void TypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (property == null || TypeBox.SelectedItem is not RequestType type)
        {
            CommissionText.Text = "";
            return;
        }
        shownCommission = PriceRules.Commission(property.Price, type.CommissionRate, hadRequests);
        string discount = hadRequests || type.CommissionRate == 0 ? "" : " (скидка 25 %)";
        CommissionText.Text = $"Комиссия агентства: {shownCommission:N2} руб.{discount}";
    }

    // Сменилась дата — показываем часы этой даты.
    private async void DateChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            await LoadHoursAsync();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопка «Обновить карточку».
    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        await LoadPropertyAsync();
    }

    // Кнопка «Подать заявку».
    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false; // защита от двойного щелчка
        try
        {
            // 1) Тип заявки.
            if (TypeBox.SelectedItem is not RequestType type)
                throw new AppException("Выберите тип заявки.");
            // 2) Дата и время показа.
            if (DateBox.SelectedDate == null)
                throw new AppException("Выберите дату показа.");
            if (HourBox.SelectedItem is not string hour)
                throw new AppException("На эту дату нет свободного времени. Выберите другую дату.");
            DateTime scheduledAt = DateBox.SelectedDate.Value.Date.AddHours(int.Parse(hour[..2]));
            // 3) Клиент: обычный пользователь — сам, сотрудник — выбранный в списке.
            int clientId = Session.CurrentUser!.UserId;
            if (Session.IsStaff)
            {
                if (ClientBox.SelectedItem is not User client)
                    throw new AppException("Выберите клиента.");
                clientId = client.UserId;
            }
            // 4) Сохранение. Все проверки остатка, времени и комиссии — внутри RequestService.
            int requestId = await RequestService.CreateRequestAsync(
                PropertyId, type.TypeId, clientId, scheduledAt, shownCommission, NotesBox.Text);
            // 5) Успех: открываем свежий каталог и показываем номер заявки.
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
            MainWindow.Instance.ShowMessage(
                $"Заявка № {requestId} оформлена, показ назначен на {scheduledAt:dd.MM.yyyy HH:mm}. Каталог обновлён.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
            SaveButton.IsEnabled = true; // ошибка — можно исправить и нажать снова
        }
    }
}
