namespace TRPO.ElectronicsStore.Service;

public readonly record struct LayoutSize(double Width, double Height);
// Размеры заданы в единицах WPF, которые учитывают масштаб экрана.
public static class WindowLayout
{

    // Ограничивает ширину и высоту окна доступной рабочей областью экрана.
    public static LayoutSize GetWindowSize(double width, double height, double workWidth, double workHeight) => new(Math.Min(width, workWidth), Math.Min(height, workHeight));

    // Рассчитывает размер содержимого, чтобы Viewbox мог уменьшить целую форму без обрезания кнопок.
    public static LayoutSize GetContentSize(double width, double height)
    {
        // Сохраняем для формы область не меньше 760 на 560 единиц WPF.
        // При нехватке места Viewbox уменьшает форму целиком, чтобы кнопки оставались видны.
        double scaleFactor = Math.Min(1, Math.Min(width / 760, height / 560));
        return new LayoutSize(width / scaleFactor, height / scaleFactor);
    }
}
