using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using ControlzEx.Theming;

namespace BurageSnap;

/// <summary>
/// App.xaml の相互作用ロジック
/// </summary>
public partial class App
{
    public App()
    {
#if DEBUG
        const string lang = "en";
        Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
        Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
            XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
#endif
        if (PreLaunch.ProcessAlreadyExists())
            Shutdown();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply MahApps.Metro theme (Light base + Steel accent)
        ThemeManager.Current.ChangeTheme(this, "Light.Steel");
    }

}