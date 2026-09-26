using System.Globalization;

namespace TRPO.ElectronicsStore.Service;

public static class InputValidation
{

    // Пытается прочитать дробное число с точкой или запятой; возвращает false при неверном вводе.
    public static bool TryReadDecimal(string text, out decimal value) =>
        decimal.TryParse(text.Trim().Replace(',', '.'),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);

    // Проверяет поля формы товара и возвращает ошибки по именам полей. Если ошибок нет, данные верны.
    public static IReadOnlyDictionary<string, string> CheckProduct(ProductInput formData)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(formData.Name) || formData.Name.Trim().Length > 200)
            errors[nameof(formData.Name)] = "Введите название: от 1 до 200 символов.";
        if (string.IsNullOrWhiteSpace(formData.Description) || formData.Description.Trim().Length > 2000)
            errors[nameof(formData.Description)] = "Введите описание: от 1 до 2000 символов.";
        if (!TryReadDecimal(formData.PriceText, out var price) || price < 0 || price >= 10000000000000000m || decimal.Round(price, 2) != price)
            errors[nameof(formData.PriceText)] = "Введите неотрицательную цену, до 2 знаков после запятой.";
        if (!int.TryParse(formData.StockText, out var stock) || stock < 0)
            errors[nameof(formData.StockText)] = "Введите целый неотрицательный остаток (до 2147483647).";
        if (!TryReadDecimal(formData.RatingText, out var rating) || rating < 0 || rating > 5 || decimal.Round(rating, 1) != rating)
            errors[nameof(formData.RatingText)] = "Введите рейтинг от 0 до 5, до 1 знака после запятой.";
        if (formData.CreatedAt is null)
            errors[nameof(formData.CreatedAt)] = "Введите корректную дату создания (дд.мм.гггг).";
        if (formData.CategoryId is null or <= 0)
            errors[nameof(formData.CategoryId)] = "Выберите категорию.";
        if (formData.BrandId is null or <= 0)
            errors[nameof(formData.BrandId)] = "Выберите бренд.";
        return errors;
    }
}
