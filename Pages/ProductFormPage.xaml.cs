using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using TRPO.ElectronicsStore.Service;
using TRPO.ElectronicsStore.UI;

namespace TRPO.ElectronicsStore.Pages;

public partial class ProductFormPage : Page
{
    private readonly MainWindow mainWindow;
    private readonly CatalogPage catalogPage;
    private readonly ProductInput formData;
    private List<TagItem> tagItems = new();
    private bool isBusy, isLoaded;

    // Проверяет права и заполняет форму отдельной копией данных нового или выбранного товара.
    public ProductFormPage(MainWindow mainWindow, CatalogPage catalogPage, ProductInput formData)
    {
        mainWindow.Session.CheckManagerAccess();
        this.mainWindow = mainWindow;
        this.catalogPage = catalogPage;
        this.formData = formData;
        InitializeComponent();
        DataContext = formData;
        Title = formData.Id.HasValue ? "Изменение товара" : "Добавление товара";
        HeadingText.Text = Title;
        DateInput.Text = formData.CreatedAt?.ToString("dd.MM.yyyy") ?? "";
    }

    // При первом открытии формы загружает категории, бренды и теги.
    private async void Page_Loaded(object sender, RoutedEventArgs eventArgs)
    {
        if (!isLoaded)
            await LoadListsAsync();
    }

    // Загружает справочники формы, восстанавливает выбор и разрешает сохранение после успешной загрузки.
    private async Task LoadListsAsync()
    {
        if (isBusy)
            return;
        isBusy = true;
        ContentRoot.IsEnabled = false;
        try
        {
            var categories = await mainWindow.Shop.GetDirectoryAsync(DirectoryType.Category);
            var brands = await mainWindow.Shop.GetDirectoryAsync(DirectoryType.Brand);
            var allTags = await mainWindow.Shop.GetDirectoryAsync(DirectoryType.Tag);
            var categoryId = formData.CategoryId;
            var brandId = formData.BrandId;
            CategoryBox.ItemsSource = categories;
            BrandBox.ItemsSource = brands;
            CategoryBox.SelectedValue = categoryId;
            BrandBox.SelectedValue = brandId;
            tagItems = allTags.Select(tag => new TagItem { Id = tag.Id, Name = tag.Name, IsSelected = formData.TagIds.Contains(tag.Id) }).ToList();
            TagsList.ItemsSource = tagItems;
            isLoaded = true;
            SaveButton.IsEnabled = true;
            LoadStatus.Text = "";
            RetryButton.Visibility = Visibility.Collapsed;
        }
        catch (Exception error)
        {
            LoadStatus.Text = "Справочники не загружены. Повторите загрузку.";
            RetryButton.Visibility = Visibility.Visible;
            UiMessages.ShowError(error);
        }
        finally
        {
            isBusy = false;
            ContentRoot.IsEnabled = true;
        }
    }

    // Собирает данные формы, проверяет их и сохраняет товар после необходимого подтверждения.
    private async void SaveProduct_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (isBusy || !isLoaded)
            return;
        formData.CreatedAt = DateTime.TryParseExact(DateInput.Text.Trim(), "dd.MM.yyyy",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var createdDate) ? createdDate : null;
        formData.CategoryId = CategoryBox.SelectedValue as int?;
        formData.BrandId = BrandBox.SelectedValue as int?;
        formData.TagIds = tagItems.Where(tag => tag.IsSelected).Select(tag => tag.Id).ToList();
        var errors = InputValidation.CheckProduct(formData);
        if (errors.Count != 0)
        {
            MessageBox.Show(string.Join("\n", errors.Values), "Проверьте поля", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (formData.Id.HasValue && !UiMessages.ConfirmAction($"Сохранить изменения товара «{formData.Name}»?"))
            return;
        isBusy = true;
        ContentRoot.IsEnabled = false;
        try
        {
            await mainWindow.Shop.SaveProductAsync(formData);
            mainWindow.ShowPage(catalogPage);
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
    }

    // Возвращает в каталог без сохранения изменений формы.
    private void CancelEdit_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (!isBusy)
            mainWindow.ShowPage(catalogPage);
    }

    // Повторяет загрузку справочников после ошибки подключения.
    private async void ReloadLists_Click(object sender, RoutedEventArgs eventArgs) => await LoadListsAsync();
}

public sealed class TagItem
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public bool IsSelected { get; set; }
}
