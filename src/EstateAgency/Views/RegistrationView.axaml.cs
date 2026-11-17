using Avalonia.Controls;
using Avalonia.Interactivity;
using EstateAgency.Services;

namespace EstateAgency.Views;

public partial class RegistrationView : UserControl
{
    public RegistrationView()
    {
        InitializeComponent();
    }

    // Кнопка «Зарегистрироваться»: все проверки — внутри Session.RegisterAsync.
    private async void RegisterClick(object? sender, RoutedEventArgs e)
    {
        RegisterButton.IsEnabled = false; // защита от двойного щелчка
        try
        {
            await Session.RegisterAsync(LastNameBox.Text ?? "", FirstNameBox.Text ?? "", MiddleNameBox.Text,
                PhoneBox.Text ?? "", EmailBox.Text ?? "", LoginBox.Text ?? "", PasswordBox.Text ?? "");
            MainWindow.Instance.OpenPage(new CatalogView(), clearHistory: true);
            MainWindow.Instance.ShowMessage("Регистрация выполнена. Вы вошли в систему.");
        }
        catch (Exception ex)
        {
            MainWindow.Instance.ShowError(ex);
            RegisterButton.IsEnabled = true;
        }
    }
}
