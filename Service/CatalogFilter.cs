using TRPO.ElectronicsStore.Models;

namespace TRPO.ElectronicsStore.Service;

public sealed class CatalogFilter
{
    public string SearchText { get; set; } = "";
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public decimal? PriceFrom { get; set; }
    public decimal? PriceTo { get; set; }

    // Проверяет, подходит ли товар одновременно под поиск, категорию, бренд и диапазон цены.
    public bool MatchesProduct(Product product) =>
        product.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)
        && (!CategoryId.HasValue || product.CategoryId == CategoryId)
        && (!BrandId.HasValue || product.BrandId == BrandId)
        && (!PriceFrom.HasValue || product.Price >= PriceFrom)
        && (!PriceTo.HasValue || product.Price <= PriceTo);
}
