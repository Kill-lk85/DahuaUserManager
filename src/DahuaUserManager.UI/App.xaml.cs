using System.Windows;
using DahuaUserManager.UI.Database;
using DahuaUserManager.UI.Windows;

namespace DahuaUserManager.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(
            StartupEventArgs e)
        {
            base.OnStartup(e);

            // Пока выбираем базу, не даём приложению
            // закрыться после закрытия стартового окна.
            ShutdownMode =
                ShutdownMode.OnExplicitShutdown;

            DatabaseManager.Instance.Load();

            var databaseWindow =
                new DatabaseSelectionWindow();

            bool? result =
                databaseWindow.ShowDialog();

            if (result != true)
            {
                Shutdown();
                return;
            }

            // После выбора базы запускаем главное окно.
            var mainWindow =
                new MainWindow();

            MainWindow =
                mainWindow;

            // Теперь приложение закрывается
            // вместе с главным окном.
            ShutdownMode =
                ShutdownMode.OnMainWindowClose;

            mainWindow.Show();
        }
    }
}