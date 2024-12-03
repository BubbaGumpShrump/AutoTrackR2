using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System;

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
        private bool isRunning = false; // Single source of truth for the running state

        // Ensure this method is not static
        public void ChangeLogoImage(string imagePath)
        {
            Logo.Source = new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
        }

        public MainWindow()
        {
            InitializeComponent();

            // Load configuration settings before setting them in any page
            ConfigManager.LoadConfig();

            homePage = new HomePage(); // Create a single instance of HomePage
            ContentControl.Content = homePage; // Default to HomePage

            // Attach event handlers for the HomePage buttons
            homePage.StartButton.Click += StartButton_Click;
            homePage.StopButton.Click += StopButton_Click;

            // Create ConfigPage and pass the MainWindow reference to it
            var configPage = new ConfigPage(this);

            // Set config values after loading them
            InitializeConfigPage();

            UpdateTabVisuals();
        }

        private void CloseWindow(object sender, RoutedEventArgs e) => this.Close();

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

                // Update the button state on the HomePage
                homePage.UpdateButtonState(isRunning);
            }
            else if (clickedTabName == "StatsTab")
            {
                ContentControl.Content = new StatsPage();
            }
            else if (clickedTabName == "UpdateTab")
            {
                ContentControl.Content = new UpdatePage();
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
                            BlurRadius = 20,       // Adjust blur radius for desired glow intensity
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

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            isRunning = true; // Update the running state
            homePage.UpdateButtonState(isRunning); // Update HomePage button visuals
            // Start your logic here
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            isRunning = false; // Update the running state
            homePage.UpdateButtonState(isRunning); // Update HomePage button visuals
            // Stop your logic here
        }

        private void InitializeConfigPage()
        {
            // Set the values from the loaded config
            ConfigPage configPage = new ConfigPage(this);

            // Set the fields in ConfigPage.xaml.cs based on the loaded config
            configPage.SetConfigValues(
                ConfigManager.LogFile,
                ConfigManager.ApiUrl,
                ConfigManager.ApiKey,
                ConfigManager.VideoPath,
                ConfigManager.VisorWipe,
                ConfigManager.VideoRecord,
                ConfigManager.OfflineMode,
                ConfigManager.Theme
            );
        }
    }

    public static class ConfigManager
    {
        public static string LogFile { get; set; }
        public static string ApiUrl { get; set; }
        public static string ApiKey { get; set; }
        public static string VideoPath { get; set; }
        public static int VisorWipe { get; set; }
        public static int VideoRecord { get; set; }
        public static int OfflineMode { get; set; }
        public static int Theme { get; set; }

        public static void LoadConfig()
        {
            string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string configFilePath = Path.Combine(exeDirectory, "config.ini");

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
                }
            }
        }

        public static void SaveConfig()
        {
            string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string configFilePath = Path.Combine(exeDirectory, "config.ini");

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                writer.WriteLine($"LogFile=\"{LogFile}\"");
                writer.WriteLine($"ApiUrl=\"{ApiUrl}\"");
                writer.WriteLine($"ApiKey=\"{ApiKey}\"");
                writer.WriteLine($"VideoPath=\"{VideoPath}\"");
                writer.WriteLine($"VisorWipe=\"{VisorWipe}\"");
                writer.WriteLine($"VideoRecord=\"{VideoRecord}\"");
                writer.WriteLine($"OfflineMode=\"{OfflineMode}\"");
                writer.WriteLine($"Theme=\"{Theme}\"");
            }
        }
    }
}
