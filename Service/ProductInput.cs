using TRPO.ElectronicsStore.Models;

namespace TRPO.ElectronicsStore.Service;

public sealed class ProductInput
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string PriceText { get; set; } = "";
    public string StockText { get; set; } = "";
    public string RatingText { get; set; } = "";
    public DateTime? CreatedAt { get; set; } = DateTime.Today;
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public List<int> TagIds { get; set; } = new();

    // Создаёт отдельную копию данных товара для формы, чтобы отмена правок не меняла исходный товар.
    public static ProductInput FromProduct(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        PriceText = product.Price.ToString(System.Globalization.CultureInfo.CurrentCulture),
        StockText = product.Stock.ToString(),
        RatingText = product.Rating.ToString(System.Globalization.CultureInfo.CurrentCulture),
        CreatedAt = product.CreatedAt.ToDateTime(TimeOnly.MinValue),
        CategoryId = product.CategoryId,
        BrandId = product.BrandId,
        TagIds = product.Tags.Select(tag => tag.Id).ToList()
    };
}
