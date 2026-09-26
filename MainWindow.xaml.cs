using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using TRPO.ElectronicsStore.Pages;
using TRPO.ElectronicsStore.UI;
using TRPO.ElectronicsStore.Service;

namespace TRPO.ElectronicsStore;

public partial class MainWindow : Window
{
    private bool isResizing;
    public Session Session { get; } = new();
    public ShopService Shop { get; }

    // Создаёт главное окно, подключает изменение его размеров и открывает страницу входа.
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => FitToScreen();
        LocationChanged += (_, _) => FitToScreen();
        StateChanged += (_, _) => FitToScreen();
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(new Action(FitToScreen));
        Shop = new ShopService(Session);
        ShowPage(new LoginPage(this));
    }

    // Подгоняет окно под экран и не допускает повторного изменения размеров во время этой операции.
    private void FitToScreen()
    {
        if (!IsLoaded || isResizing)
            return;
        isResizing = true;
        try
        {
            MonitorWorkArea.FitToScreen(this);
        }
        finally
        {
            isResizing = false;
        }
    }

    // Пересчитывает размер области страницы, чтобы её элементы помещались в окне.
    private void PageHost_SizeChanged(object sender, SizeChangedEventArgs eventArgs)
    {
        if (PageFrame is null || eventArgs.NewSize.Width <= 0 || eventArgs.NewSize.Height <= 0)
            return;
        var contentSize = WindowLayout.GetContentSize(eventArgs.NewSize.Width, eventArgs.NewSize.Height);
        PageFrame.Width = contentSize.Width;
        PageFrame.Height = contentSize.Height;
    }

    // Открывает переданную страницу и добавляет её название в заголовок окна.
    public void ShowPage(Page page)
    {
        Title = "Магазин электроники — " + page.Title;
        PageFrame.Navigate(page);
    }

    // Сбрасывает права менеджера и возвращает пользователя на страницу входа.
    public void Logout()
    {
        Session.EnterVisitor();
        ShowPage(new LoginPage(this));
    }

    // Запрещает переходы назад и вперёд через журнал страниц.
    private void PageFrame_Navigating(object sender, NavigatingCancelEventArgs eventArgs)
    {
        if (eventArgs.NavigationMode is NavigationMode.Back or NavigationMode.Forward)
            eventArgs.Cancel = true;
    }

    // Удаляет предыдущие страницы из журнала после перехода.
    private void PageFrame_Navigated(object sender, NavigationEventArgs eventArgs)
    {
        while (PageFrame.CanGoBack)
            PageFrame.RemoveBackEntry();
    }
}
