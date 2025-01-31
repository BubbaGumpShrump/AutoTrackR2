using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.Win32;

namespace AutoTrackR2
{
    public partial class ConfigPage : UserControl
    {
        // Store the current slider value
        private double savedSliderValue = 0;

        private MainWindow mainWindow;

        public ConfigPage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
         
            LogFilePath.Text = ConfigManager.LogFile;
            ApiUrl.Text = ConfigManager.ApiUrl;
            ApiKey.Text = ConfigManager.ApiKey;
            VideoPath.Text = ConfigManager.VideoPath;
            VisorWipeSlider.Value = ConfigManager.VisorWipe;
            VideoRecordSlider.Value = ConfigManager.VideoRecord;
            OfflineModeSlider.Value = ConfigManager.OfflineMode;
            KillLogSlider.Value = ConfigManager.KillLog;
            DeathLogSlider.Value = ConfigManager.DeathLog;
            OtherLogSlider.Value = ConfigManager.OtherLog;
            ThemeSlider.Value = ConfigManager.Theme;

            ApplyToggleModeStyle(OfflineModeSlider.Value, VisorWipeSlider.Value, VideoRecordSlider.Value, KillLogSlider.Value, DeathLogSlider.Value, OtherLogSlider.Value);
        }

        // Method to change the logo image in MainWindow
        public void ChangeLogo(string imagePath, Color? glowColor = null)
        {
            // Update the logo from ConfigPage
            mainWindow.ChangeLogoImage(imagePath);

            if (glowColor.HasValue)
            {
                // Add the glow effect to the logo in MainWindow
                DropShadowEffect glowEffect = new DropShadowEffect
                {
                    Color = glowColor.Value, // Glow color
                    ShadowDepth = 0,   // Centered glow
                    BlurRadius = 20,   // Glow spread
                    Opacity = 0.8      // Intensity
                };

                // Apply the effect to the logo
                mainWindow.Logo.Effect = glowEffect;
            }
            else
            {
                mainWindow.Logo.Effect = null;
            }
        }

        // This method will set the loaded config values to the UI controls
        public void SetConfigValues(string logFile, string apiUrl, string apiKey, string videoPath,
                                     int visorWipe, int videoRecord, int offlineMode, int killLog, int deathLog, int otherLog, int theme)
        {
            // Set the textboxes with the loaded values
            LogFilePath.Text = logFile;
            ApiUrl.Text = apiUrl;
            ApiKey.Text = apiKey;
            VideoPath.Text = videoPath;

            // Set the sliders with the loaded values
            VideoRecordSlider.Value = videoRecord;
            VisorWipeSlider.Value = visorWipe;
            OfflineModeSlider.Value = offlineMode;
            KillLogSlider.Value = killLog;
            DeathLogSlider.Value = deathLog;
            OtherLogSlider.Value = otherLog;

            // Handle themes
            if (theme >= 0 && theme <= 3)
            {
                ThemeSlider.Value = theme; // Set slider only for visible themes
            }
            else
            {
                ApplyTheme(theme); // Apply hidden themes directly
            }
        }

        private void ApplyToggleModeStyle(double offlineModeValue, double visorWipeValue, double videoRecordValue, double killLogValue, double deathLogvalue, double otherLogValue)
        {
            // Get the slider
            Slider offlineModeSlider = OfflineModeSlider;
            Slider visorWipeSlider = VisorWipeSlider;
            Slider videoRecordSlider = VideoRecordSlider;
            Slider killLogSlider = KillLogSlider;
            Slider deathLogSlider = DeathLogSlider;
            Slider otherLogSlider = OtherLogSlider;

            // Set the appropriate style based on value (0 or 1)
            if (offlineModeValue == 0)
            {
                offlineModeSlider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
            }

            if (visorWipeValue == 0)
            {
                visorWipeSlider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
            }

            if (videoRecordValue == 0)
            {
                videoRecordSlider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
            }

            if (killLogValue == 0)
            {
                killLogSlider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
            }

            if (deathLogvalue == 0)
            {
                deathLogSlider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
            }

            if (otherLogValue == 0)
            {
                otherLogSlider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
            }
        }

        private void ThemeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Save the current slider value when it changes
            savedSliderValue = e.NewValue;

            // Get the slider value (0, 1, or 2)
            int themeIndex = (int)savedSliderValue;

            // Apply the selected theme
            ApplyTheme(themeIndex);
            
            mainWindow.UpdateTabVisuals();
        }

        private void ApplyTheme(int themeIndex)
        {
            switch (themeIndex)
            {
                case 0: // Default Blue Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#00A9E0"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#0F1A2B"), // Button
                        (Color)ColorConverter.ConvertFromString("#1D2D44"), // Background
                        (Color)ColorConverter.ConvertFromString("#FFFFFF"), // Text
                        (Color)ColorConverter.ConvertFromString("#A88F2C")  // AltText
                    );
                    ChangeLogo("/Assets/AutoTrackR.png");
                    break;
                case 1: // Green Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#1D9F00"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#262424"), // Button
                        (Color)ColorConverter.ConvertFromString("#072501"), // Background
                        (Color)ColorConverter.ConvertFromString("#D7AF3C"), // Text
                        (Color)ColorConverter.ConvertFromString("#DCD6C4")  // AltText
                    );
                    ChangeLogo("/Assets/AutoTrackR.png");
                    break;
                case 2: // Red Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#D32F2F"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#424242"), // Button
                        (Color)ColorConverter.ConvertFromString("#212121"), // Light Background
                        (Color)ColorConverter.ConvertFromString("#E0E0E0"), // Text
                        (Color)ColorConverter.ConvertFromString("#A88F2C")  // AltText
                    );
                    ChangeLogo("/Assets/AutoTrackR.png");
                    break;
                case 3: // Purple Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#32CD32"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#33065F"), // Button
                        (Color)ColorConverter.ConvertFromString("#43065F"), // Background
                        (Color)ColorConverter.ConvertFromString("#00FF00"), // Text
                        (Color)ColorConverter.ConvertFromString("#B3976E")  // AltText
                    );
                    ChangeLogo("/Assets/AutoTrackR.png");
                    break;
                case 4: // GN Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#FF0000"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#1C1C1C"), // Button
                        (Color)ColorConverter.ConvertFromString("#000000"), // Background
                        (Color)ColorConverter.ConvertFromString("#FBC603"), // Text
                        (Color)ColorConverter.ConvertFromString("#BFA8A6")  // AltText
                    );
                    ChangeLogo("/Assets/GN.png", (Color)ColorConverter.ConvertFromString("#FF0000"));
                    break;
                case 5: // NW Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#B92D2D"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#1C1C1C"), // Button
                        (Color)ColorConverter.ConvertFromString("#262424"), // Background
                        (Color)ColorConverter.ConvertFromString("#01DDDA"), // Text
                        (Color)ColorConverter.ConvertFromString("#A88F2C")  // AltText
                    );
                    ChangeLogo("/Assets/NW.png", (Color)ColorConverter.ConvertFromString("#01DDDA"));
                    break;
                case 6: // D3VL Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#AA0000"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#333333"), // Button
                        (Color)ColorConverter.ConvertFromString("#220000"), // Background
                        (Color)ColorConverter.ConvertFromString("#FF0000"), // Text
                        (Color)ColorConverter.ConvertFromString("#A88F2C")  // AltText
                    );
                    ChangeLogo("/Assets/D3VL.png", (Color)ColorConverter.ConvertFromString("#CC0000"));
                    break;
                case 7: // HIT Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#B92D2D"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#1C1C1C"), // Button
                        (Color)ColorConverter.ConvertFromString("#262424"), // Background
                        (Color)ColorConverter.ConvertFromString("#7d7d7d"), // Text
                        (Color)ColorConverter.ConvertFromString("#A88F2C")  // AltText
                    );
                    ChangeLogo("/Assets/HIT.png");
                    break;
                case 8: // WRAITH Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#ff0000"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#2a2a2a"), // Button
                        (Color)ColorConverter.ConvertFromString("#0a0a0a"), // Background
                        (Color)ColorConverter.ConvertFromString("#DFDFDF"), // Text
                        (Color)ColorConverter.ConvertFromString("#8B0000")  // AltText
                    );
                    ChangeLogo("/Assets/WRITH.png", (Color)ColorConverter.ConvertFromString("#ff0000"));
                    break;
                case 9: // VOX Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#C0C0C0"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#1C1C1C"), // Button
                        (Color)ColorConverter.ConvertFromString("#424242"), // Background
                        (Color)ColorConverter.ConvertFromString("#FFD700"), // Text
                        (Color)ColorConverter.ConvertFromString("#817E79")  // AltText
                    );
                    ChangeLogo("/Assets/VOX.png", (Color)ColorConverter.ConvertFromString("#FFD700"));
                    break;
                case 10: // EMP Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#F5721C"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#535353"), // Button
                        (Color)ColorConverter.ConvertFromString("#080000"), // Background
                        (Color)ColorConverter.ConvertFromString("#FFFFFF"), // Text
                        (Color)ColorConverter.ConvertFromString("#CEA75B")  // AltText
                    );
                    ChangeLogo("/Assets/EMP.png", (Color)ColorConverter.ConvertFromString("#F3BD9B"));
                    break;
                case 11: // AVS Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#3fbcff"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#060606"), // Button
                        (Color)ColorConverter.ConvertFromString("#333333"), // Background
                        (Color)ColorConverter.ConvertFromString("#e8e8e8"), // Text
                        (Color)ColorConverter.ConvertFromString("#A88F2C")  // AltText
                    );
                    ChangeLogo("/Assets/AVSQN.png", (Color)ColorConverter.ConvertFromString("#3fbcff"));
                    break;
                case 12: // HEX Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#39FF14"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#535353"), // Button
                        (Color)ColorConverter.ConvertFromString("#000800"), // Background
                        (Color)ColorConverter.ConvertFromString("#FFFFFF"), // Text
                        (Color)ColorConverter.ConvertFromString("#CFFF04")  // AltText
                    );
                    ChangeLogo("/Assets/HEX.png", (Color)ColorConverter.ConvertFromString("#39FF14"));
                    break;
                case 13: // Mammon Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#FFD700"), // Accent/Border - Royal Gold
                        (Color)ColorConverter.ConvertFromString("#2C2C2C"), // Button - Dark Gray
                        (Color)ColorConverter.ConvertFromString("#1A1A1A"), // Background - Rich Black
                        (Color)ColorConverter.ConvertFromString("#FFFFFF"), // Text - White
                        (Color)ColorConverter.ConvertFromString("#DAA520")  // AltText - Golden Rod
                    );
                    ChangeLogo("/Assets/MAMMON.png", (Color)ColorConverter.ConvertFromString("#FFD700"));
                    break;
                case 14: // Shadow Moses Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#FF69B4"), // Accent/Border - Hot Pink
                        (Color)ColorConverter.ConvertFromString("#2C2C2C"), // Button - Dark Gray
                        (Color)ColorConverter.ConvertFromString("#2C1F28"), // Background - Dark Pink-Gray
                        (Color)ColorConverter.ConvertFromString("#E6E6E6"), // Text - Light Gray
                        (Color)ColorConverter.ConvertFromString("#FF1493")  // AltText - Deep Pink
                    );
                    ChangeLogo("/Assets/ShadowMoses.png", (Color)ColorConverter.ConvertFromString("#FF69B4"));
                    break;
                case 15: // Mongrel Squad
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#00416A"), // Accent/Border - NyQuil Dark Blue
                        (Color)ColorConverter.ConvertFromString("#1B3F5C"), // Button - Midnight Blue
                        (Color)ColorConverter.ConvertFromString("#002E4D"), // Background - Deep NyQuil Blue
                        (Color)ColorConverter.ConvertFromString("#B0C4DE"), // Text - Light Steel Blue
                        (Color)ColorConverter.ConvertFromString("#4F94CD")  // AltText - Steel Blue
                    );
                    ChangeLogo("/Assets/Bobgrel.png", (Color)ColorConverter.ConvertFromString("#00BFFF"));
                    break;
                case 16: // Feezy
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#FFA500"), // Accent/Border - Orange
                        (Color)ColorConverter.ConvertFromString("#FFE4B5"), // Button - Moccasin
                        (Color)ColorConverter.ConvertFromString("#FFF8DC"), // Background - Cornsilk
                        (Color)ColorConverter.ConvertFromString("#8B4513"), // Text - Saddle Brown
                        (Color)ColorConverter.ConvertFromString("#FF7F50")  // AltText - Coral
                    );
                    ChangeLogo("/Assets/chibifox.png", (Color)ColorConverter.ConvertFromString("#FFA500"));
                    break;
                case 17: // NMOS
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#EAB787"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#601C1B"), // Button
                        (Color)ColorConverter.ConvertFromString("#170402"), // Background
                        (Color)ColorConverter.ConvertFromString("#F6DBAD"), // Text
                        (Color)ColorConverter.ConvertFromString("#EBCAA0")  // AltText
                    );
                    ChangeLogo("/Assets/NMOS.png", (Color)ColorConverter.ConvertFromString("#EAB787"));
                    break;
                case 18: // Rakk Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#FF00FF"), // Accent/Border - Magenta
                        (Color)ColorConverter.ConvertFromString("#1C1C1C"), // Button - Dark Gray
                        (Color)ColorConverter.ConvertFromString("#0A0A0A"), // Background - Nearly Black
                        (Color)ColorConverter.ConvertFromString("#E6E6E6"), // Text - Light Gray
                        (Color)ColorConverter.ConvertFromString("#00BFFF")  // AltText - Deep Sky Blue
                    );
                    ChangeLogo("/Assets/RACKETEERS.png", (Color)ColorConverter.ConvertFromString("#FF00FF"));
                    break;
                case 19: // Blightveil Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#8B4AC6"), // Accent/Border - Purple from the logo border
                        (Color)ColorConverter.ConvertFromString("#2A2035"), // Button - Darker purple for buttons
                        (Color)ColorConverter.ConvertFromString("#1A1621"), // Background - Very dark purple/black
                        (Color)ColorConverter.ConvertFromString("#FFFFFF"), // Text - White like the logo text
                        (Color)ColorConverter.ConvertFromString("#FF3333")  // AltText - Red like the eyes
                    );
                    ChangeLogo("/Assets/Blightveil.png", (Color)ColorConverter.ConvertFromString("#8B4AC6"));
                    break;
                case 20: // Gankhub Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#ffa500"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#2C2C2C"), // Button
                        (Color)ColorConverter.ConvertFromString("#1b1b1b"), // Background
                        (Color)ColorConverter.ConvertFromString("#FFFFFF"), // Text
                        (Color)ColorConverter.ConvertFromString("#ffa500")  // AltText
                    );
                    ChangeLogo("/Assets/Gankhub.png");
                    break;
            }
        }

        // Helper method to update both Color and Brush resources
        private void UpdateThemeColors(Color accent, Color backgroundDark, Color backgroundLight, Color text, Color altText)
        {
            // Update color resources
            Application.Current.Resources["AccentColor"] = accent;
            Application.Current.Resources["BackgroundDarkColor"] = backgroundDark;
            Application.Current.Resources["BackgroundLightColor"] = backgroundLight;
            Application.Current.Resources["TextColor"] = text;
            Application.Current.Resources["AltTextColor"] = altText;

            // Update SolidColorBrush resources
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush(accent);
            Application.Current.Resources["BackgroundDarkBrush"] = new SolidColorBrush(backgroundDark);
            Application.Current.Resources["BackgroundLightBrush"] = new SolidColorBrush(backgroundLight);
            Application.Current.Resources["TextBrush"] = new SolidColorBrush(text);
            Application.Current.Resources["AltTextBrush"] = new SolidColorBrush(altText);
        }

        // This method will be called when switching tabs to restore the saved slider position.
        public void RestoreSliderValue()
        {
            // Set the slider back to the previously saved value
            ThemeSlider.Value = savedSliderValue;
        }

        // Log File Browse Button Handler
        private void LogFileBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Log files (*.log)|*.log|All files (*.*)|*.*"; // Adjust as needed

            if (dialog.ShowDialog() == true)
            {
                LogFilePath.Text = dialog.FileName; // Set the selected file path to the TextBox
            }
        }

        // Video Path Browse Button Handler
        private void VideoPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Prepare a dummy string, thos would appear in the dialog
            string dummyFileName = "Path where the videos should be";

            SaveFileDialog dialog = new SaveFileDialog();
            // Feed the dummy name to the save dialog
            dialog.FileName = dummyFileName;

            if(dialog.ShowDialog() == true)
            {
                // Extract only the directory path from the file
                string selectedFolder = Path.GetDirectoryName(dialog.FileName);
                VideoPath.Text = selectedFolder; // Set the folder path
            }
        }

        private void VisorWipeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;

            // Build the dynamic file path for the current user
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTrackR2",
                "visorwipe.ahk"
            );

            // Get the current value of the slider (0 or 1)
            ConfigManager.VisorWipe = (int)slider.Value;

            if (ConfigManager.VisorWipe == 1)
            {
                // Check if the file exists
                if (File.Exists(filePath))
                {
                    // Apply the enabled style if the file exists
                    slider.Style = (Style)Application.Current.FindResource("ToggleSliderStyle");
                }
                else
                {
                    // File does not exist; revert the toggle to 0
                    ConfigManager.VisorWipe = 0;
                    slider.Value = 0; // Revert the slider value
                    slider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");

                    // Optionally, display a message to the user
                    MessageBox.Show($"Visor wipe script not found. Please ensure the file exists at:\n{filePath}",
                                    "File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                // Apply the disabled style
                slider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
            }
        }

        private void VideoRecordSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;

            // Build the dynamic file path for the current user
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTrackR2",
                "videorecord.ahk"
            );

            // Get the current value of the slider (0 or 1)
            ConfigManager.VideoRecord = (int)slider.Value;

            if (ConfigManager.VideoRecord == 1)
            {
                // Check if the file exists
                if (File.Exists(filePath))
                {
                    // Apply the enabled style if the file exists
                    slider.Style = (Style)Application.Current.FindResource("ToggleSliderStyle");
                }
                else
                {
                    // File does not exist; revert the toggle to 0
                    ConfigManager.VideoRecord = 0;
                    slider.Value = 0; // Revert the slider value
                    slider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");

                    // Optionally, display a message to the user
                    MessageBox.Show($"Video record script not found. Please ensure the file exists at:\n{filePath}",
                                    "File Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                // Apply the disabled style
                slider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
            }
        }

        private void OfflineModeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            ConfigManager.OfflineMode = (int)slider.Value; // 0 or 1

            // Check if the value is 0 or 1 and apply the corresponding style
            if (ConfigManager.OfflineMode == 0)
            {
                slider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");  // Apply FalseToggleStyle
            }
            else
            {
                slider.Style = (Style)Application.Current.FindResource("ToggleSliderStyle"); // Apply ToggleSliderStyle
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the directory for the user's local application data
            string appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoTrackR2"
            );

            // Ensure the directory exists
            if (!Directory.Exists(appDataDirectory))
            {
                Directory.CreateDirectory(appDataDirectory);
            }

            // Combine the app data directory with the config file name
            string configFilePath = Path.Combine(appDataDirectory, "config.ini");

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                writer.WriteLine($"LogFile={LogFilePath.Text}");
                writer.WriteLine($"ApiUrl={ApiUrl.Text}");
                writer.WriteLine($"ApiKey={ApiKey.Text}");
                writer.WriteLine($"VideoPath={VideoPath.Text}");
                writer.WriteLine($"VisorWipe={(int)VisorWipeSlider.Value}");
                writer.WriteLine($"VideoRecord={(int)VideoRecordSlider.Value}");
                writer.WriteLine($"OfflineMode={(int)OfflineModeSlider.Value}");
                writer.WriteLine($"KillLog={(int)KillLogSlider.Value}");
                writer.WriteLine($"DeathLog={(int)DeathLogSlider.Value}");
                writer.WriteLine($"OtherLog={(int)OtherLogSlider.Value}");
                writer.WriteLine($"Theme={(int)ThemeSlider.Value}"); // Assumes you are saving the theme slider value (0, 1, or 2)
            }

            // Start the flashing effect
            FlashSaveButton();
            ConfigManager.LoadConfig();
        }

        private void FlashSaveButton()
        {
            string originalText = SaveButton.Content.ToString();
            SaveButton.Content = "Saved";

            // Save button color change effect
            var originalColor = SaveButton.Background;
            var accentColor = (Color)Application.Current.Resources["AccentColor"];
            SaveButton.Background = new SolidColorBrush(accentColor);  // Change color to accent color

            // Apply glow effect
            SaveButton.Effect = new DropShadowEffect
            {
                Color = accentColor,
                BlurRadius = 15,      // Add subtle blur
                ShadowDepth = 0,      // Set shadow depth to 0 for a pure glow effect
                Opacity = 0.8,        // Set opacity for glow visibility
                Direction = 0         // Direction doesn't matter for glow
            };

            // Create a DispatcherTimer to reset everything after the effect
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(600) // Interval for flash effect
            };

            int flashCount = 0;
            timer.Tick += (sender, e) =>
            {
                if (flashCount < 2) // Flash effect (flash 2 times)
                {
                    flashCount++;
                }
                else
                {
                    // Stop the timer and restore the original button state
                    timer.Stop();
                    SaveButton.Content = originalText;
                    SaveButton.Background = originalColor; // Restore the original button color
                    SaveButton.Effect = null; // Remove the glow effect
                }
            };

            // Start the timer
            timer.Start();
        }

        private async void TestApiButton_Click(object sender, RoutedEventArgs e)
        {
            string apiUrl = ApiUrl.Text;
            string modifiedUrl = Regex.Replace(apiUrl, @"(https?://[^/]+)/?.*", "$1/test");
            string apiKey = ApiKey.Text;
            Debug.WriteLine($"Sending to {modifiedUrl}");

            try
            {
                // Configure HttpClient with TLS 1.2
                var handler = new HttpClientHandler
                {
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                };

                using (var client = new HttpClient(handler))
                {
                    // Set headers
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoTrackR");

                    // Empty JSON body
                    var content = new StringContent("{}", Encoding.UTF8, "application/json");

                    // Send POST
                    var response = await client.PostAsync(modifiedUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("API Test Success.");
                    }
                    else
                    {
                        MessageBox.Show($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"API Test Failure. {ex.Message}");
            }
        }
    }
}
