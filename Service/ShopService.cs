using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TRPO.ElectronicsStore.Data;
using TRPO.ElectronicsStore.Models;

namespace TRPO.ElectronicsStore.Service;

public enum DirectoryType
{
    Category,
    Brand,
    Tag
}

public sealed record DirectoryItem
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

// Каждая операция использует свой контекст. Несохранённые правки формы не отслеживаются EF.
public sealed class ShopService
{
    private readonly Session session;
    private readonly Func<ShopContext> createContext;

    // Запоминает текущего пользователя и способ создания контекста базы для каждой операции.
    public ShopService(Session session, Func<ShopContext>? createContext = null)
    {
        this.session = session;
        this.createContext = createContext ?? DbFactory.CreateContext;
    }

    // Загружает товары вместе с категориями, брендами и тегами без отслеживания изменений.
    public async Task<List<Product>> GetProductsAsync()
    {
        await using var context = createContext();
        return await context.Products.AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Brand)
            .Include(product => product.Tags)
            .OrderBy(product => product.Id)
            .ToListAsync();
    }

    // Проверяет права и данные, затем добавляет или изменяет товар вместе с его тегами.
    public async Task SaveProductAsync(ProductInput formData)
    {
        session.CheckManagerAccess();
        var errors = InputValidation.CheckProduct(formData);
        if (errors.Count != 0)
            throw new InvalidOperationException(string.Join("\n", errors.Values));
        await using var context = createContext();
        if (!await context.Categories.AnyAsync(item => item.Id == formData.CategoryId) || !await context.Brands.AnyAsync(item => item.Id == formData.BrandId))
            throw new InvalidOperationException("Выбранная категория или бренд удалены. Откройте форму заново.");
        var tagIds = formData.TagIds.Distinct().ToArray();
        var tags = await context.Tags.Where(item => tagIds.Contains(item.Id)).ToListAsync();
        if (tags.Count != tagIds.Length)
            throw new InvalidOperationException("Один из выбранных тегов удалён. Откройте форму заново.");
        Product product;
        if (formData.Id is int id)
            product = await context.Products.Include(item => item.Tags)
                .SingleOrDefaultAsync(item => item.Id == id)
                ?? throw new InvalidOperationException("Товар уже удалён. Обновите каталог.");
        else
        {
            product = new Product();
            context.Products.Add(product);
        }

        product.Name = formData.Name.Trim();
        product.Description = formData.Description.Trim();
        InputValidation.TryReadDecimal(formData.PriceText, out var price);
        InputValidation.TryReadDecimal(formData.RatingText, out var rating);
        product.Price = price;
        product.Stock = int.Parse(formData.StockText.Trim());
        product.Rating = rating;
        product.CreatedAt = DateOnly.FromDateTime(formData.CreatedAt!.Value);
        product.CategoryId = formData.CategoryId!.Value;
        product.BrandId = formData.BrandId!.Value;
        product.Tags.Clear();
        foreach (var tag in tags)
            product.Tags.Add(tag);
        session.CheckManagerAccess();
        await SaveChangesAsync(context);
        // Запоминаем ID после сохранения, чтобы повторное нажатие не создало копию товара.
        formData.Id = product.Id;
    }

    // Проверяет права менеджера и удаляет товар по его ID.
    public async Task DeleteProductAsync(int id)
    {
        session.CheckManagerAccess();
        await using var context = createContext();
        var product = await context.Products.FindAsync(id) ?? throw new InvalidOperationException("Товар уже удалён. Обновите каталог.");
        context.Products.Remove(product);
        session.CheckManagerAccess();
        await SaveChangesAsync(context);
    }

    // Загружает выбранный справочник категорий, брендов или тегов и сортирует записи по названию.
    public async Task<List<DirectoryItem>> GetDirectoryAsync(DirectoryType directoryType)
    {
        await using var context = createContext();
        return await GetDirectoryQuery(context, directoryType).OrderBy(item => item.Name).ThenBy(item => item.Id).ToListAsync();
    }

    // Создаёт запрос к нужному справочнику и выбирает ID и название каждой записи.
    private static IQueryable<DirectoryItem> GetDirectoryQuery(ShopContext context, DirectoryType directoryType) => directoryType switch
    {
        DirectoryType.Category => context.Categories.AsNoTracking().Select(item => new DirectoryItem { Id = item.Id, Name = item.Name }),
        DirectoryType.Brand => context.Brands.AsNoTracking().Select(item => new DirectoryItem { Id = item.Id, Name = item.Name }),
        DirectoryType.Tag => context.Tags.AsNoTracking().Select(item => new DirectoryItem { Id = item.Id, Name = item.Name }),
        _ => throw new ArgumentOutOfRangeException(nameof(directoryType))
    };

    // Добавляет или изменяет запись справочника после проверки названия и его уникальности.
    public async Task SaveDirectoryAsync(DirectoryType directoryType, int? id, string name)
    {
        session.CheckManagerAccess();
        name = name.Trim();
        if (name.Length is < 1 or > 100)
            throw new InvalidOperationException("Введите название длиной от 1 до 100 символов.");
        await using var context = createContext();
        // Настройка сравнения в базе проверяет уникальность названия без учёта регистра.
        if (await GetDirectoryQuery(context, directoryType).AnyAsync(item => item.Name == name && (!id.HasValue || item.Id != id.Value)))
            throw new InvalidOperationException("Запись с таким названием уже существует.");
        switch (directoryType)
        {
            case DirectoryType.Category:
                var category = id.HasValue ? await context.Categories.FindAsync(id.Value) : new Category();
                if (category is null)
                    throw CreateMissingError();
                category.Name = name;
                if (!id.HasValue)
                    context.Categories.Add(category);
                break;
            case DirectoryType.Brand:
                var brand = id.HasValue ? await context.Brands.FindAsync(id.Value) : new Brand();
                if (brand is null)
                    throw CreateMissingError();
                brand.Name = name;
                if (!id.HasValue)
                    context.Brands.Add(brand);
                break;
            case DirectoryType.Tag:
                var tag = id.HasValue ? await context.Tags.FindAsync(id.Value) : new Tag();
                if (tag is null)
                    throw CreateMissingError();
                tag.Name = name;
                if (!id.HasValue)
                    context.Tags.Add(tag);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(directoryType));
        }

        session.CheckManagerAccess();
        await SaveChangesAsync(context);
    }

    // Удаляет запись справочника; используемые товарами категории и бренды удалить не даёт.
    public async Task DeleteDirectoryAsync(DirectoryType directoryType, int id)
    {
        session.CheckManagerAccess();
        await using var context = createContext();
        switch (directoryType)
        {
            case DirectoryType.Category:
                if (await context.Products.AnyAsync(item => item.CategoryId == id))
                    throw new InvalidOperationException("Категория используется товарами. Сначала измените категорию этих товаров.");
                context.Categories.Remove(await context.Categories.FindAsync(id) ?? throw CreateMissingError());
                break;
            case DirectoryType.Brand:
                if (await context.Products.AnyAsync(item => item.BrandId == id))
                    throw new InvalidOperationException("Бренд используется товарами. Сначала измените бренд этих товаров.");
                context.Brands.Remove(await context.Brands.FindAsync(id) ?? throw CreateMissingError());
                break;
            case DirectoryType.Tag:
                context.Tags.Remove(await context.Tags.FindAsync(id) ?? throw CreateMissingError());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(directoryType));
        }

        session.CheckManagerAccess();
        await SaveChangesAsync(context);
    }

    // Создаёт понятную ошибку для записи, которая уже удалена из базы.
    private static InvalidOperationException CreateMissingError() => new("Запись уже удалена. Обновите список.");

    // Записывает изменения в базу и заменяет типичные ошибки SQL понятными сообщениями.
    private static async Task SaveChangesAsync(ShopContext context)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException sqlError && sqlError.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Запись с таким названием уже существует.");
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException sqlError && sqlError.Number == 547)
        {
            throw new InvalidOperationException("Связанные данные изменились или используются товарами. Обновите список и повторите действие.");
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Запись уже удалена или изменена. Обновите список.");
        }
    }
}
