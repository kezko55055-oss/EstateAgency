using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Services;

namespace EstateAgency.Views;

public partial class CatalogView : UserControl
{
    // ВСЕ карточки, загруженные из БД. Фильтры каждый раз начинают с полного списка,
    // поэтому очистка поиска возвращает скрытые карточки.
    private List<PropertyCard> allCards = new();

    public CatalogView()
    {
        InitializeComponent();
        SortBox.SelectedIndex = 0; // по умолчанию — «Цена по возрастанию»
        Loaded += async (s, e) => await LoadCardsAsync();
    }

    // Загрузить карточки из БД и заполнить список типов.
    private async Task LoadCardsAsync()
    {
        try
        {
            // Запоминаем выбранный тип, чтобы после обновления он остался.
            string? oldType = TypeBox.SelectedItem as string;

            allCards = await CatalogService.LoadCardsAsync();

            // Типы: первая строка «Все типы», дальше — без повторов, по алфавиту.
            var types = new List<string> { "Все типы" };
            types.AddRange(allCards.Select(c => c.Type).Distinct().OrderBy(c => c));
            TypeBox.ItemsSource = types;

            if (oldType != null && types.Contains(oldType))
                TypeBox.SelectedItem = oldType;
            else
                TypeBox.SelectedIndex = 0;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Поиск + тип + доступность + сортировка. Вызывается при любом изменении фильтров.
    private void ApplyFilters()
    {
        string text = (SearchBox.Text ?? "").Trim();

        // 1) Поиск: название, код или адрес содержит текст (регистр букв не важен).
        IEnumerable<PropertyCard> rows = allCards.Where(c =>
            c.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            c.Code.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            c.Address.Contains(text, StringComparison.OrdinalIgnoreCase));

        // 2) Тип объекта: индекс 0 — «Все типы», тогда не фильтруем.
        if (TypeBox.SelectedIndex > 0)
        {
            string type = (string)TypeBox.SelectedItem!;
            rows = rows.Where(c => c.Type == type);
        }

        // 3) Только объекты, по которым ещё можно подать заявку.
        if (OpenOnlyBox.IsChecked == true)
            rows = rows.Where(c => c.IsOpen);

        // 4) Сортировка по цене: индекс 1 — по убыванию, иначе по возрастанию.
        if (SortBox.SelectedIndex == 1)
            rows = rows.OrderByDescending(c => c.Price);
        else
            rows = rows.OrderBy(c => c.Price);

        // 5) Показать результат.
        List<PropertyCard> result = rows.ToList();
        CardsList.ItemsSource = result;
        CountText.Text = $"Найдено: {result.Count} из {allCards.Count}";
    }

    // Изменился текст поиска.
    private void SearchChanged(object? sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    // Изменился тип или сортировка.
    private void FilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    // Изменился флажок «Только доступные».
    private void OpenOnlyClick(object? sender, RoutedEventArgs e)
    {
        ApplyFilters();
    }

    // Кнопка «Обновить».
    private async void ReloadClick(object? sender, RoutedEventArgs e)
    {
        await LoadCardsAsync();
    }

    // Пользователь выбрал карточку — открываем страницу объекта.
    private void CardSelected(object? sender, SelectionChangedEventArgs e)
    {
        PropertyCard? card = CardsList.SelectedItem as PropertyCard;
        if (card == null)
            return; // выбор сброшен — ничего не делаем
        if (Session.IsGuest)
        {
            CardsList.SelectedItem = null;
            MainWindow.Instance.ShowMessage("Чтобы подать заявку, войдите в систему.");
            return;
        }
        MainWindow.Instance.OpenPage(new PropertyView { PropertyId = card.Id });
    }
}
