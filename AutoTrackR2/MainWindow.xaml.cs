//using System.Collections.Generic;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace AutoTrackR2
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, bool> tabStates = new Dictionary<string, bool>
        {
            { "HomeTab", true }, // HomeTab is selected by default
            { "StatsTab", false },
            { "UpdateTab", false },
            { "ConfigTab", false }
        };

        private HomePage homePage; // Persistent HomePage instance

        public void ChangeLogoImage(string imagePath)
        {
            try
            {
                // Ensure the path starts with a forward slash for WPF resource paths
                if (!imagePath.StartsWith("/"))
                {
                    imagePath = "/" + imagePath;
                }

                // Create a pack URI for the resource
                Uri uri = new Uri($"pack://application:,,,/AutoTrackR2;component{imagePath}", UriKind.Absolute);
                Logo.Source = new BitmapImage(uri);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading logo image: {ex.Message}");
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            homePage = new HomePage(); // Create a single instance of HomePage
            ContentControl.Content = homePage; // Default to HomePage

            // Create ConfigPage and pass the MainWindow reference to it
            var configPage = new ConfigPage(this);

            // Set config values after loading them
            InitializeConfigPage();

            UpdateTabVisuals();

            Loaded += MainWindow_Loaded; // Handle Loaded event
            Closing += MainWindow_Closing; // Handle Closing event
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Check command-line arguments after the window is loaded
            var args = Environment.GetCommandLineArgs();
            if (args.Contains("-start", StringComparer.OrdinalIgnoreCase))
            {
                // Initialize log handler if needed
                homePage.InitializeLogHandler();
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Clean up resources
            homePage?.Cleanup();

            // Make sure the application exits completely
            Application.Current.Shutdown();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            // This will trigger the Closing event
            this.Close();
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            string clickedTabName = clickedButton.Name;

            if (clickedTabName == "HomeTab")
            {
                // Reuse the existing HomePage instance
                ContentControl.Content = homePage;
            }
            else if (clickedTabName == "ConfigTab")
            {
                ContentControl.Content = new ConfigPage(this);
            }

            // Update tab selection states
            UpdateTabStates(clickedTabName);

            // Update the visual appearance of all tabs
            UpdateTabVisuals();
        }

        private void ProcessLogBackups_Click(object sender, RoutedEventArgs e)
        {
            homePage.ProcessLogBackups_Click(sender, e);
        }

        private void UpdateTabStates(string activeTab)
        {
            foreach (var key in tabStates.Keys)
            {
                tabStates[key] = key == activeTab;
            }
        }

        public void UpdateTabVisuals()
        {
            var accentColor = (Color)Application.Current.Resources["AccentColor"];
            var backgroundDarkColor = (Color)Application.Current.Resources["BackgroundDarkColor"];
            var textColor = (Color)Application.Current.Resources["TextColor"];

            foreach (var tabState in tabStates)
            {
                Button tabButton = (Button)this.FindName(tabState.Key);
                if (tabButton != null)
                {
                    tabButton.Effect = null;

                    if (tabState.Value) // Active tab
                    {
                        tabButton.Background = new SolidColorBrush(accentColor); // Highlight color from theme

                        // Add glow effect
                        tabButton.Effect = new DropShadowEffect
                        {
                            Color = accentColor,
                            BlurRadius = 30,       // Adjust blur radius for desired glow intensity
                            ShadowDepth = 0,      // Set shadow depth to 0 for a pure glow effect
                            Opacity = 1,        // Set opacity for glow visibility
                            Direction = 0         // Direction doesn't matter for glow
                        };
                    }
                    else // Inactive tab
                    {
                        tabButton.Background = new SolidColorBrush(backgroundDarkColor); // Default background from theme
                    }
                }
            }
        }

        private void InitializeConfigPage()
        {
            // Set the values from the loaded config
            ConfigPage configPage = new ConfigPage(this);

            // Set the fields in ConfigPage.xaml.cs based on the loaded config
            configPage.SetConfigValues(
                ConfigManager.LogFile ?? string.Empty,
                ConfigManager.ApiUrl ?? string.Empty,
                ConfigManager.ApiKey ?? string.Empty,
                ConfigManager.VideoPath ?? string.Empty,
                ConfigManager.VisorWipe,
                ConfigManager.VideoRecord,
                ConfigManager.OfflineMode,
                ConfigManager.Theme
            );
        }
    }

    public static class ConfigManager
    {
        public static string? LogFile { get; set; } = string.Empty;
        public static string? KillHistoryFile { get; set; } = string.Empty;
        public static string? AHKScriptFolder { get; set; } = string.Empty;
        public static string? VisorWipeScript { get; set; } = string.Empty;
        public static string? VideoRecordScript { get; set; } = string.Empty;
        public static string? ApiUrl { get; set; } = string.Empty;
        public static string? ApiKey { get; set; } = string.Empty;
        public static string? VideoPath { get; set; } = string.Empty;
        public static int VisorWipe { get; set; }
        public static int VideoRecord { get; set; }
        public static int OfflineMode { get; set; }
        public static int Theme { get; set; }
        public static int StreamlinkEnabled { get; set; }
        public static int StreamlinkDuration { get; set; } = 30;
        public static int KillStreakEnabled { get; set; } = 1; // Default to enabled
        public static int? KillFeedLimit { get; set; } = null; // Default to null (load all)

        static ConfigManager()
        {
            LoadConfig();

            // Set default values
            // AppData\Local\AutoTrackR2\Kill-log.csv
            KillHistoryFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTrackR2",
                "Kill-log.csv"
            );

            AHKScriptFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTrackR2"
            );

            VisorWipeScript = "visorwipe.ahk";
            VideoRecordScript = "videorecord.ahk";
        }

        public static void LoadConfig()
        {
            // Define the config file path in a writable location
            string configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTrackR2"
            );
            string configFilePath = Path.Combine(configDirectory, "config.ini");

            if (File.Exists(configFilePath))
            {
                foreach (var line in File.ReadLines(configFilePath))
                {
                    if (line.StartsWith("LogFile="))
                        LogFile = line.Substring("LogFile=".Length).Trim();
                    else if (line.StartsWith("ApiUrl="))
                        ApiUrl = line.Substring("ApiUrl=".Length).Trim();
                    else if (line.StartsWith("ApiKey="))
                        ApiKey = line.Substring("ApiKey=".Length).Trim();
                    else if (line.StartsWith("VideoPath="))
                        VideoPath = line.Substring("VideoPath=".Length).Trim();
                    else if (line.StartsWith("VisorWipe="))
                        VisorWipe = int.Parse(line.Substring("VisorWipe=".Length).Trim());
                    else if (line.StartsWith("VideoRecord="))
                        VideoRecord = int.Parse(line.Substring("VideoRecord=".Length).Trim());
                    else if (line.StartsWith("OfflineMode="))
                        OfflineMode = int.Parse(line.Substring("OfflineMode=".Length).Trim());
                    else if (line.StartsWith("Theme="))
                        Theme = int.Parse(line.Substring("Theme=".Length).Trim());
                    else if (line.StartsWith("StreamlinkEnabled="))
                        StreamlinkEnabled = int.Parse(line.Substring("StreamlinkEnabled=".Length).Trim());
                    else if (line.StartsWith("StreamlinkDuration="))
                        StreamlinkDuration = int.Parse(line.Substring("StreamlinkDuration=".Length).Trim());
                    else if (line.StartsWith("KillStreakEnabled="))
                        KillStreakEnabled = int.Parse(line.Substring("KillStreakEnabled=".Length).Trim());
                    else if (line.StartsWith("KillFeedLimit="))
                    {
                        var value = line.Substring("KillFeedLimit=".Length).Trim();
                        KillFeedLimit = string.IsNullOrEmpty(value) ? null : int.Parse(value);
                    }
                }
            }
        }

        public static void SaveConfig()
        {
            // Define the config file path in a writable location
            string configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTrackR2"
            );

            // Ensure the directory exists
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            string configFilePath = Path.Combine(configDirectory, "config.ini");

            // Write the configuration to the file
            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                writer.WriteLine($"LogFile={LogFile}");
                writer.WriteLine($"ApiUrl={ApiUrl}");
                writer.WriteLine($"ApiKey={ApiKey}");
                writer.WriteLine($"VideoPath={VideoPath}");
                writer.WriteLine($"VisorWipe={VisorWipe}");
                writer.WriteLine($"VideoRecord={VideoRecord}");
                writer.WriteLine($"OfflineMode={OfflineMode}");
                writer.WriteLine($"Theme={Theme}");
                writer.WriteLine($"StreamlinkEnabled={StreamlinkEnabled}");
                writer.WriteLine($"StreamlinkDuration={StreamlinkDuration}");
                writer.WriteLine($"KillStreakEnabled={KillStreakEnabled}");
                writer.WriteLine($"KillFeedLimit={KillFeedLimit}");
            }
        }
    }
}
