using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TRPO.ElectronicsStore.UI;

public sealed class StockBorderConverter : IValueConverter
{

    // Выбирает жёлтую рамку при остатке меньше 10, в остальных случаях возвращает чёрную.
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int stock && stock < 10
            ? new SolidColorBrush(Color.FromRgb(255, 209, 41))
            : Brushes.Black;

    // Не переводит цвет рамки обратно в остаток, потому что здесь нужна только односторонняя привязка.
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
