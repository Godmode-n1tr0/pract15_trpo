using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TRPO.ElectronicsStore.Models;
using TRPO.ElectronicsStore.Service;
using TRPO.ElectronicsStore.UI;

namespace TRPO.ElectronicsStore.Pages;

public partial class CatalogPage : Page
{
    private readonly MainWindow mainWindow;
    private readonly ObservableCollection<Product> products = new();
    private ICollectionView productView = null!;
    private CatalogFilter filter = new();
    private bool isReady, isBusy;

    // Создаёт каталог, подключает список товаров и показывает доступные пользователю действия.
    public CatalogPage(MainWindow mainWindow)
    {
        this.mainWindow = mainWindow;
        InitializeComponent();
        productView = CollectionViewSource.GetDefaultView(products);
        productView.Filter = item => item is Product product && filter.MatchesProduct(product);
        ProductList.ItemsSource = productView;
        RoleText.Text = mainWindow.Session.IsManager ? "Менеджер" : "Посетитель";
        ManagerActions.Visibility = mainWindow.Session.IsManager ? Visibility.Visible : Visibility.Collapsed;
        isReady = true;
        ApplySort();
    }

    // При открытии страницы запускает загрузку товаров и справочников.
    private async void Page_Loaded(object sender, RoutedEventArgs eventArgs) => await LoadProductsAsync();

    // Заново загружает товары и справочники, сохраняя выбранные фильтры и товар.
    private async Task LoadProductsAsync()
    {
        if (isBusy)
            return;
        isBusy = true;
        ContentRoot.IsEnabled = false;
        StatusText.Text = "Загрузка…";
        int? selectedCategoryId = CategoryBox.SelectedValue as int?, selectedBrandId = BrandBox.SelectedValue as int?;
        int? selectedProductId = (ProductList.SelectedItem as Product)?.Id;
        try
        {
            var loadedProducts = await mainWindow.Shop.GetProductsAsync();
            var categories = await mainWindow.Shop.GetDirectoryAsync(DirectoryType.Category);
            var brands = await mainWindow.Shop.GetDirectoryAsync(DirectoryType.Brand);
            isReady = false;
            categories.Insert(0, new DirectoryItem { Id = 0, Name = "Все категории" });
            brands.Insert(0, new DirectoryItem { Id = 0, Name = "Все бренды" });
            CategoryBox.ItemsSource = categories;
            BrandBox.ItemsSource = brands;
            CategoryBox.SelectedValue = categories.Any(item => item.Id == selectedCategoryId) ? selectedCategoryId : 0;
            BrandBox.SelectedValue = brands.Any(item => item.Id == selectedBrandId) ? selectedBrandId : 0;
            // При изменении списка WPF сразу читает его, поэтому обновление не откладываем.
            products.Clear();
            foreach (var product in loadedProducts)
                products.Add(product);

            isReady = true;
            ApplyFilters();
            ProductList.SelectedItem = products.FirstOrDefault(item => item.Id == selectedProductId);
            StatusText.Text = "";
        }
        catch (Exception error)
        {
            StatusText.Text = "Не удалось обновить каталог. Устраните причину ошибки и нажмите «Обновить».";
            UiMessages.ShowError(error);
        }
        finally
        {
            isReady = true;
            isBusy = false;
            ContentRoot.IsEnabled = true;
            UpdateProductCount();
        }
    }

    // Обновляет фильтры после изменения поиска, категории, бренда или цены, если страница готова.
    private void Filters_Changed(object sender, RoutedEventArgs eventArgs)
    {
        if (isReady)
            ApplyFilters();
    }

    // Проверяет диапазон цены и применяет поиск и фильтры; при ошибке оставляет прежний фильтр.
    private void ApplyFilters()
    {
        decimal? minPrice = null, maxPrice = null;
        if (!TryReadPrice(PriceFromBox.Text, out minPrice) || !TryReadPrice(PriceToBox.Text, out maxPrice))
        {
            FilterError.Text = "Цена должна быть неотрицательным числом. Пока действует предыдущий фильтр.";
            return;
        }

        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
        {
            FilterError.Text = "Цена «от» не может превышать цену «до». Пока действует предыдущий фильтр.";
            return;
        }

        FilterError.Text = "";
        filter = new CatalogFilter
        {
            SearchText = SearchBox.Text,
            CategoryId = CategoryBox.SelectedValue is int categoryId && categoryId > 0 ? categoryId : null,
            BrandId = BrandBox.SelectedValue is int brandId && brandId > 0 ? brandId : null,
            PriceFrom = minPrice,
            PriceTo = maxPrice
        };
        productView.Refresh();
        UpdateProductCount();
    }

    // Читает неотрицательную границу цены; пустое поле означает отсутствие ограничения.
    private static bool TryReadPrice(string text, out decimal? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        if (!InputValidation.TryReadDecimal(text, out var priceValue) || priceValue < 0)
            return false;
        result = priceValue;
        return true;
    }

    // Меняет порядок товаров после выбора другого варианта сортировки.
    private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (isReady)
            ApplySort();
    }

    // Применяет выбранную сортировку; при одинаковых значениях дополнительно сравнивает ID товаров.
    private void ApplySort()
    {
        var (sortProperty, sortDirection) = SortBox.SelectedIndex switch
        {
            1 => (nameof(Product.Price), ListSortDirection.Ascending),
            2 => (nameof(Product.Price), ListSortDirection.Descending),
            3 => (nameof(Product.Stock), ListSortDirection.Ascending),
            4 => (nameof(Product.Stock), ListSortDirection.Descending),
            _ => (nameof(Product.Name), ListSortDirection.Ascending)
        };
        using (productView.DeferRefresh())
        {
            productView.SortDescriptions.Clear();
            productView.SortDescriptions.Add(new SortDescription(sortProperty, sortDirection));
            productView.SortDescriptions.Add(new SortDescription(nameof(Product.Id), ListSortDirection.Ascending));
        }
    }

    // Обновляет количество всех и видимых товаров и сообщение о пустом списке.
    private void UpdateProductCount()
    {
        int visibleCount = productView.Cast<Product>().Count();
        CountText.Text = $"Всего товаров: {products.Count}    Отображено: {visibleCount}";
        EmptyText.Visibility = visibleCount == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // Очищает поиск и фильтры и возвращает сортировку по названию.
    private void ResetFilters_Click(object sender, RoutedEventArgs eventArgs)
    {
        isReady = false;
        SearchBox.Clear();
        PriceFromBox.Clear();
        PriceToBox.Clear();
        CategoryBox.SelectedValue = 0;
        BrandBox.SelectedValue = 0;
        SortBox.SelectedIndex = 0;
        isReady = true;
        ApplyFilters();
        ApplySort();
    }

    // Разрешает изменение и удаление только менеджеру, который выделил товар.
    private void ProductList_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
    {
        if (!isReady)
            return;
        EditButton.IsEnabled = DeleteButton.IsEnabled = mainWindow.Session.IsManager && ProductList.SelectedItem is Product;
    }

    // Открывает менеджеру пустую форму для добавления товара.
    private void AddProduct_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (!isBusy && mainWindow.Session.IsManager)
            mainWindow.ShowPage(new ProductFormPage(mainWindow, this, new ProductInput()));
    }

    // Открывает менеджеру форму с копией данных выбранного товара.
    private void EditProduct_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (!isBusy && mainWindow.Session.IsManager && ProductList.SelectedItem is Product product)
            mainWindow.ShowPage(new ProductFormPage(mainWindow, this, ProductInput.FromProduct(product)));
    }

    // При двойном щелчке именно по строке товара запускает его редактирование.
    private void ProductList_MouseDoubleClick(object sender, MouseButtonEventArgs eventArgs)
    {
        if (ItemsControl.ContainerFromElement(ProductList, eventArgs.OriginalSource as DependencyObject) is ListBoxItem)
            EditProduct_Click(sender, eventArgs);
    }

    // Запрашивает подтверждение, удаляет выбранный товар и обновляет каталог после успеха.
    private async void DeleteProduct_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (isBusy || !mainWindow.Session.IsManager || ProductList.SelectedItem is not Product product)
            return;
        if (!UiMessages.ConfirmAction($"Удалить товар «{product.Name}»? Действие нельзя отменить."))
            return;
        isBusy = true;
        ContentRoot.IsEnabled = false;
        bool isDeleted = false;
        try
        {
            await mainWindow.Shop.DeleteProductAsync(product.Id);
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
            await LoadProductsAsync();
    }

    // Открывает менеджеру страницу категорий, брендов и тегов.
    private void Directories_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (!isBusy && mainWindow.Session.IsManager)
            mainWindow.ShowPage(new DirectoriesPage(mainWindow, this));
    }

    // Повторно загружает каталог по кнопке Обновить.
    private async void ReloadProducts_Click(object sender, RoutedEventArgs eventArgs) => await LoadProductsAsync();

    // Выходит из текущего режима, если операция с базой сейчас не выполняется.
    private void Logout_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (!isBusy)
            mainWindow.Logout();
    }
}
