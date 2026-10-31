using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DwnoEvoHelper;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var args = e.Args;
        int langAt = Array.IndexOf(args, "--lang");
        if (langAt >= 0 && langAt + 1 < args.Length) Loc.Set(args[langAt + 1]);

        // Mit Admin-Rechten neu gestartet: nur den einen Schritt ausführen
        if (args.Length >= 2 && (args[0] == "--install" || args[0] == "--remove"))
        {
            int code = 0;
            try
            {
                if (args[0] == "--install") Setup.Install(args[1], _ => { }).GetAwaiter().GetResult();
                else Setup.RemovePlugin(args[1]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Loc.F("setup.failed", ex.Message), Loc.T("title"));
                code = 1;
            }
            Shutdown(code);
            return;
        }

        new MainWindow().Show();
    }
}

/// Breite des Fortschrittsbalkens = Anteil × verfügbare Breite.
public sealed class ProgressWidth : IMultiValueConverter
{
    public static readonly ProgressWidth Instance = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 2 && values[0] is double p && values[1] is double w ? Math.Max(0, p * w) : 0d;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
