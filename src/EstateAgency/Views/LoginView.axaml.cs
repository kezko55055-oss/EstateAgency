using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Services;

namespace EstateAgency.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    // Кнопка «Войти».
    private async void LoginClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            await Session.LoginAsync(LoginBox.Text ?? "", PasswordBox.Text ?? "");
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
        }
    }

    // Кнопка «Войти как гость».
    private void GuestClick(object? sender, RoutedEventArgs e)
    {
        Session.Logout();
        MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
    }

    // Кнопка «Регистрация».
    private void RegisterClick(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.OpenPage(new RegistrationView());
    }
}
