using System.Windows;
using System.Windows.Threading;

namespace CrosshairY;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Unhandled exception:\n\n{args.Exception}",
                "CrosshairY – Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Fatal exception:\n\n{args.ExceptionObject}",
                "CrosshairY – Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        base.OnStartup(e);
    }
}
