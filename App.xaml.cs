using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace TRPO.ElectronicsStore;

public partial class App : Application
{

    // При запуске задаёт русский язык и формат чисел и дат для всего приложения.
    protected override void OnStartup(StartupEventArgs eventArgs)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("ru-RU")));
        base.OnStartup(eventArgs);
    }
}
