using System.Windows;
using System.Windows.Controls;
using TRPO.ElectronicsStore.Service;
using TRPO.ElectronicsStore.UI;

namespace TRPO.ElectronicsStore.Pages;

public partial class DirectoriesPage : Page
{
    private readonly MainWindow mainWindow;
    private readonly CatalogPage catalogPage;
    private bool isReady, isBusy;
    private int? selectedItemId;
    private DirectoryType SelectedType => (DirectoryType)Math.Max(0, DirectoryTabs.SelectedIndex);

    // Проверяет права менеджера и создаёт страницу редактирования справочников.
    public DirectoriesPage(MainWindow mainWindow, CatalogPage catalogPage)
    {
        mainWindow.Session.CheckManagerAccess();
        this.mainWindow = mainWindow;
        this.catalogPage = catalogPage;
        InitializeComponent();
        isReady = true;
    }

    // При открытии страницы загружает записи выбранного справочника.
    private async void Page_Loaded(object sender, RoutedEventArgs eventArgs) => await LoadItemsAsync();

    // Очищает старый список и загружает записи текущей вкладки, блокируя действия на время загрузки.
    private async Task LoadItemsAsync()
    {
        if (isBusy)
            return;
        isBusy = true;
        ContentRoot.IsEnabled = false;
        SaveButton.IsEnabled = false;
        // Убираем записи прежней вкладки, чтобы их нельзя было изменить как записи другого справочника.
        DirectoryList.ItemsSource = null;
        ClearEditor();
        StatusText.Text = "Загрузка…";
        try
        {
            DirectoryList.ItemsSource = await mainWindow.Shop.GetDirectoryAsync(SelectedType);
            SaveButton.IsEnabled = true;
            StatusText.Text = "";
        }
        catch (Exception error)
        {
            StatusText.Text = "Список не загружен. Нажмите «Обновить список».";
            UiMessages.ShowError(error);
        }
        finally
        {
            isBusy = false;
            ContentRoot.IsEnabled = true;
        }
    }

    // Загружает другой справочник при переключении вкладки.
    private async void DirectoryTabs_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (isReady && ReferenceEquals(eventArgs.Source, DirectoryTabs))
            await LoadItemsAsync();
    }

    // Переносит выбранную запись в поля редактирования и включает кнопку удаления.
    private void DirectoryList_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (!isReady)
            return;
        if (DirectoryList.SelectedItem is DirectoryItem item)
        {
            selectedItemId = item.Id;
            NameInput.Text = item.Name;
            EditorTitle.Text = "Изменение записи";
            SaveButton.Content = "Сохранить";
            DeleteButton.IsEnabled = true;
        }
        else
            ClearEditor();
    }

    // Очищает поля и переводит редактор в режим добавления новой записи.
    private void ClearEditor()
    {
        selectedItemId = null;
        NameInput.Clear();
        EditorTitle.Text = "Добавление записи";
        SaveButton.Content = "Добавить";
        DeleteButton.IsEnabled = false;
    }

    // Снимает выделение записи, очищает редактор и ставит курсор в поле названия.
    private void NewItem_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (!isBusy)
        {
            DirectoryList.SelectedItem = null;
            ClearEditor();
            NameInput.Focus();
        }
    }

    // Проверяет название, подтверждает правку существующей записи и сохраняет её в справочник.
    private async void SaveItem_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (isBusy)
            return;
        var itemName = NameInput.Text.Trim();
        if (itemName.Length is < 1 or > 100)
        {
            MessageBox.Show("Введите название длиной от 1 до 100 символов.", "Проверьте название", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (selectedItemId.HasValue && !UiMessages.ConfirmAction($"Сохранить изменения записи «{itemName}»?"))
            return;
        isBusy = true;
        ContentRoot.IsEnabled = false;
        bool isSaved = false;
        try
        {
            await mainWindow.Shop.SaveDirectoryAsync(SelectedType, selectedItemId, itemName);
            isSaved = true;
            ClearEditor();
        }
        catch (Exception error)
        {
            UiMessages.ShowError(error);
        }
        finally
        {
            isBusy = false;
            ContentRoot.IsEnabled = true;
        }

        if (isSaved)
            await LoadItemsAsync();
    }

    // Подтверждает удаление записи и обновляет список после успешного удаления.
    private async void DeleteItem_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (isBusy || DirectoryList.SelectedItem is not DirectoryItem item)
            return;
        var deleteNote = SelectedType == DirectoryType.Tag ? " Тег будет удалён из всех товаров; сами товары останутся." : "";
        if (!UiMessages.ConfirmAction($"Удалить запись «{item.Name}»? Действие нельзя отменить.{deleteNote}"))
            return;
        isBusy = true;
        ContentRoot.IsEnabled = false;
        bool isDeleted = false;
        try
        {
            await mainWindow.Shop.DeleteDirectoryAsync(SelectedType, item.Id);
            isDeleted = true;
        }
        catch (Exception error)
        {
            UiMessages.ShowError(error);
        }
        finally
        {
            isBusy = false;
            ContentRoot.IsEnabled = true;
        }

        if (isDeleted)
            await LoadItemsAsync();
    }

    // Повторно загружает записи текущей вкладки по кнопке обновления.
    private async void ReloadItems_Click(object sender, RoutedEventArgs eventArgs) => await LoadItemsAsync();

    // Возвращает пользователя в каталог, если операция с базой не выполняется.
    private void Back_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (!isBusy)
            mainWindow.ShowPage(catalogPage);
    }
}
