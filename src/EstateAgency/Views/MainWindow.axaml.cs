using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EstateAgency.Services;

namespace EstateAgency.Views;

public partial class MainWindow : Window
{
    // Ссылка на единственное главное окно. Страницы пишут:
    //     MainWindow.Instance.OpenPage(new CatalogView());
    public static MainWindow Instance { get; private set; } = null!;

    // История открытых страниц для кнопки «Назад» (последняя — сверху).
    private readonly Stack<UserControl> history = new();

    public MainWindow()
    {
        InitializeComponent();                         // создаёт элементы из .axaml
        Instance = this;                               // запоминаем окно
        OpenPage(new LoginView(), clearHistory: true); // первая страница — вход
    }

    // Открыть страницу. clearHistory: true — начать историю заново (вход, выход).
    public void OpenPage(UserControl page, bool clearHistory = false)
    {
        if (clearHistory)
        {
            history.Clear();
        }
        else
        {
            // Текущую страницу кладём в историю, чтобы к ней можно было вернуться.
            if (PageHost.Content is UserControl current)
                history.Push(current);
        }
        ShowPage(page);
    }

    // Показать страницу и обновить шапку и кнопки.
    private void ShowPage(UserControl page)
    {
        PageHost.Content = page;                         // подставить страницу в окно
        Title = "Агентство «Ключ» — " + page.Tag;        // Tag страницы = её заголовок
        MessageText.Text = "";                           // убрать старое сообщение
        UserText.Text = Session.Caption;                 // ФИО и роль (или «Гость»)
        RequestsButton.IsVisible = !Session.IsGuest;     // «Заявки» — после входа
        RequestsButton.Content = Session.IsStaff ? "Заявки" : "Мои заявки";
        PropertiesButton.IsVisible = Session.IsStaff;    // «Объекты» — только сотрудникам
        ReportsButton.IsVisible = Session.IsStaff;       // «Отчёт» — только сотрудникам
        AccountButton.Content = Session.IsGuest ? "Войти" : "Сменить пользователя";
        BackButton.IsEnabled = history.Count > 0;        // «Назад», если есть куда
    }

    // Обычное сообщение.
    public void ShowMessage(string text)
    {
        MessageText.Foreground = Brushes.Black;
        MessageText.Text = text;
    }

    // Сообщение об ошибке (тёмно-красным цветом).
    public void ShowError(Exception error)
    {
        System.Diagnostics.Trace.WriteLine(error); // подробности — в окно Output
        MessageText.Foreground = Brushes.DarkRed;
        MessageText.Text = UiSupport.ErrorText(error);
    }

    // ---------- Обработчики кнопок навигации ----------
    private void BackClick(object? sender, RoutedEventArgs e)
    {
        if (history.Count > 0)
            ShowPage(history.Pop()); // Pop — достать последнюю страницу из истории
    }

    private void CatalogClick(object? sender, RoutedEventArgs e)
    {
        OpenPage(new CatalogView(), clearHistory: true);
    }

    private void RequestsClick(object? sender, RoutedEventArgs e)
    {
        OpenPage(new RequestsView());
    }

    private void PropertiesClick(object? sender, RoutedEventArgs e)
    {
        OpenPage(new PropertiesManagementView());
    }

    private void ReportsClick(object? sender, RoutedEventArgs e)
    {
        OpenPage(new ReportsView());
    }

    private void LogoutClick(object? sender, RoutedEventArgs e)
    {
        Session.Logout();                              // снова гость
        OpenPage(new LoginView(), clearHistory: true); // история очищается
    }
}
