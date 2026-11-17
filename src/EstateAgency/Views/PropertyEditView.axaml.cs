using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Data;
using EstateAgency.Models;
using EstateAgency.Services;
using Microsoft.EntityFrameworkCore;

namespace EstateAgency.Views;

public partial class PropertyEditView : UserControl
{
    // 0 — новый объект, иначе номер изменяемого.
    public int PropertyId { get; set; }

    public PropertyEditView()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            HeaderText.Text = PropertyId == 0 ? "Новый объект" : "Изменение объекта";
            List<PropertyType> types = await PropertyService.GetTypesAsync();
            List<PropertyStatus> statuses = await PropertyService.GetStatusesAsync();
            List<User> managers = await PropertyService.GetManagersAsync();
            TypeBox.ItemsSource = types;
            StatusBox.ItemsSource = statuses;
            ManagerBox.ItemsSource = managers;
            ManagerPanel.IsVisible = Session.IsAdmin; // ответственного назначает администратор
            TypeBox.SelectedIndex = 0;
            StatusBox.SelectedIndex = 0;
            ManagerBox.SelectedIndex = 0;

            if (PropertyId != 0)
            {
                using var db = Db.Create();
                Property? p = await db.Properties.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.PropertyId == PropertyId);
                if (p == null)
                    throw new AppException("Объект уже удалён. Вернитесь к списку.");
                CodeBox.Text = p.PropertyCode;
                TitleBox.Text = p.Title;
                AddressBox.Text = p.Address;
                TypeBox.SelectedItem = types.FirstOrDefault(t => t.TypeId == p.TypeId);
                StatusBox.SelectedItem = statuses.FirstOrDefault(s => s.StatusId == p.StatusId);
                ManagerBox.SelectedItem = managers.FirstOrDefault(m => m.UserId == p.ManagerId);
                PriceBox.Text = p.Price.ToString("0.##", CultureInfo.CurrentCulture);
                AreaBox.Text = p.Area.ToString("0.##", CultureInfo.CurrentCulture);
                RoomsBox.Text = p.Rooms.ToString();
                ViewingsBox.Text = p.FreeViewings.ToString();
                ImageBox.Text = p.ImagePath;
                DescriptionBox.Text = p.Description;
            }
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Число из поля ввода: и «1,5», и «1.5» считаются одним значением.
    private static decimal ReadDecimal(TextBox box, string name)
    {
        string text = (box.Text ?? "").Trim().Replace(" ", "").Replace(',', '.');
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
            throw new AppException($"Поле «{name}»: введите число.");
        return value;
    }

    private static int ReadInt(TextBox box, string name)
    {
        if (!int.TryParse((box.Text ?? "").Trim(), out int value))
            throw new AppException($"Поле «{name}»: введите целое число.");
        return value;
    }

    private async void SaveClick(object? sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false; // защита от двойного щелчка
        try
        {
            if (TypeBox.SelectedItem is not PropertyType type || StatusBox.SelectedItem is not PropertyStatus status)
                throw new AppException("Выберите тип и статус объекта.");
            var input = new Property
            {
                PropertyId = PropertyId,
                PropertyCode = CodeBox.Text ?? "",
                Title = TitleBox.Text ?? "",
                Address = AddressBox.Text ?? "",
                TypeId = type.TypeId,
                StatusId = status.StatusId,
                ManagerId = (ManagerBox.SelectedItem as User)?.UserId ?? Session.CurrentUser!.UserId,
                Price = ReadDecimal(PriceBox, "Цена"),
                Area = ReadDecimal(AreaBox, "Площадь"),
                Rooms = ReadInt(RoomsBox, "Комнат"),
                FreeViewings = ReadInt(ViewingsBox, "Показов"),
                ImagePath = ImageBox.Text,
                Description = DescriptionBox.Text
            };
            await PropertyService.SaveAsync(input);
            MainWindow.Instance.OpenPage(new PropertiesManagementView(), clearHistory: true);
            MainWindow.Instance.ShowMessage("Карточка объекта сохранена.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
            SaveButton.IsEnabled = true;
        }
    }
}
