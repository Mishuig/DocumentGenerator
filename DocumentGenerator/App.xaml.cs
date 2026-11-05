using System.Windows;
using DocumentGenerator.Infrastructure;

namespace DocumentGenerator;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            AppPaths.Initialize();
            base.OnStartup(e);
            using var db = new AppDbContext();
            db.Database.EnsureCreated();
            DispatcherUnhandledException += (_, args) =>
            {
                try { File.AppendAllText(Path.Combine(AppPaths.Logs, "application.log"), $"{DateTime.UtcNow:O} | Unhandled UI exception\n{args.Exception}\n\n"); } catch { }
                MessageBox.Show($"Сталася неочікувана помилка. Деталі: logs/application.log\n{args.Exception.Message}", "Document Generator", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                try { File.AppendAllText(Path.Combine(AppPaths.Logs, "application.log"), $"{DateTime.UtcNow:O} | Fatal exception\n{args.ExceptionObject}\n\n"); } catch { }
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не вдалося запустити Document Generator.\n{ex.Message}\n\nПереконайтеся, що папка програми доступна для запису.", "Помилка запуску", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
