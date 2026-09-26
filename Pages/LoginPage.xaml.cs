using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TRPO.ElectronicsStore.Pages;

public partial class LoginPage : Page
{
    private readonly MainWindow mainWindow;

    // Создаёт страницу входа и запоминает главное окно для переходов между страницами.
    public LoginPage(MainWindow mainWindow)
    {
        this.mainWindow = mainWindow;
        InitializeComponent();
    }

    // Включает режим посетителя и открывает каталог.
    private void VisitorLogin_Click(object sender, RoutedEventArgs eventArgs)
    {
        mainWindow.Session.EnterVisitor();
        mainWindow.ShowPage(new CatalogPage(mainWindow));
    }

    // Проверяет введённый PIN и открывает каталог менеджеру либо показывает ошибку входа.
    private void ManagerLogin_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (mainWindow.Session.TryLogin(PinBox.Password))
            mainWindow.ShowPage(new CatalogPage(mainWindow));
        else
        {
            MessageBox.Show("Неверный PIN. Попробуйте ещё раз.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
            PinBox.Clear();
            PinBox.Focus();
        }
    }

    // При нажатии Enter в поле PIN запускает вход менеджера.
    private void PinBox_KeyDown(object sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key == Key.Enter)
        {
            eventArgs.Handled = true;
            ManagerLogin_Click(sender, eventArgs);
        }
    }
}
