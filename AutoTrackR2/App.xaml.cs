using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading;
using System.IO;
using System;

namespace AutoTrackR2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static Mutex _mutex = null;
        private static bool _mutexOwned = false;
        private static readonly string CrashLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoTrackR2",
            "crash.log"
        );

        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure crash log directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath));

            // Set up unhandled exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            const string appName = "AutoTrackR2";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);
            _mutexOwned = createdNew;

            if (!createdNew)
            {
                // App is already running, silently exit
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogCrash(e.ExceptionObject as Exception);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception);
            e.Handled = true; // Prevent the application from crashing
        }

        private void LogCrash(Exception? ex)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"[{timestamp}] CRASH: {ex?.Message}\n" +
                               $"Stack Trace:\n{ex?.StackTrace}\n" +
                               $"Source: {ex?.Source}\n" +
                               $"Target Site: {ex?.TargetSite}\n" +
                               "----------------------------------------\n";

                File.AppendAllText(CrashLogPath, logMessage);

                // Show error message to user
                MessageBox.Show(
                    "AutoTrackR2 has encountered an error. A crash log has been created.\n" +
                    $"Location: {CrashLogPath}",
                    "AutoTrackR2 Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception logEx)
            {
                // If logging fails, at least show a basic error message
                MessageBox.Show(
                    "AutoTrackR2 has encountered an error and failed to create a crash log.\n" +
                    $"Error: {ex?.Message}",
                    "AutoTrackR2 Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mutex != null && _mutexOwned)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    // Log the mutex release error but don't throw
                    LogCrash(ex);
                }
                _mutex.Dispose();
            }
            base.OnExit(e);
        }
    }
}
