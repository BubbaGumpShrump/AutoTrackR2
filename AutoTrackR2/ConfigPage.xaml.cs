using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
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
            ThemeSlider.Value = ConfigManager.Theme;

            ApplyToggleModeStyle(OfflineModeSlider.Value, VisorWipeSlider.Value, VideoRecordSlider.Value);
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
                                     int visorWipe, int videoRecord, int offlineMode, int theme)
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

        private void ApplyToggleModeStyle(double offlineModeValue, double visorWipeValue, double videoRecordValue)
        {
            // Get the slider
            Slider offlineModeSlider = OfflineModeSlider;
            Slider visorWipeSlider = VisorWipeSlider;
            Slider videoRecordSlider = VideoRecordSlider;

            // Set the appropriate style based on the offlineMode value (0 or 1)
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
                        (Color)ColorConverter.ConvertFromString("#FFFFFF")  // Text
                    );
                    ChangeLogo("/Assets/AutoTrackR.png");
                    break;
                case 1: // Green Theme
                    UpdateThemeColors(
                         (Color)ColorConverter.ConvertFromString("#1D9F00"), // Accent/Border
                         (Color)ColorConverter.ConvertFromString("#262424"), // Button
                         (Color)ColorConverter.ConvertFromString("#072501"), // Background
                         (Color)ColorConverter.ConvertFromString("#D7AF3C")  // Text
                    );
                    ChangeLogo("/Assets/AutoTrackR.png");
                    break;
                case 2: // Red Theme
                    UpdateThemeColors(
                         (Color)ColorConverter.ConvertFromString("#D32F2F"), // Accent/Border
                         (Color)ColorConverter.ConvertFromString("#424242"), // Button
                         (Color)ColorConverter.ConvertFromString("#212121"), // Light Background
                         (Color)ColorConverter.ConvertFromString("#E0E0E0")  // Text
                    );
                    ChangeLogo("/Assets/AutoTrackR.png");
                    break;
                case 3: // Purple Theme
                    UpdateThemeColors(
                         (Color)ColorConverter.ConvertFromString("#32CD32"), // Accent/Border
                         (Color)ColorConverter.ConvertFromString("#33065F"), // Button
                         (Color)ColorConverter.ConvertFromString("#43065F"), // Background
                         (Color)ColorConverter.ConvertFromString("#00FF00")  // Text
                    );
                    ChangeLogo("/Assets/AutoTrackR.png");
                    break;
                case 4: // GN Theme
                    UpdateThemeColors(
                         (Color)ColorConverter.ConvertFromString("#FF0000"), // Accent/Border
                         (Color)ColorConverter.ConvertFromString("#1C1C1C"), // Button
                         (Color)ColorConverter.ConvertFromString("#000000"), // Background
                         (Color)ColorConverter.ConvertFromString("#FBC603")  // Text
                    );
                    ChangeLogo("/Assets/GN.png", (Color)ColorConverter.ConvertFromString("#FF0000"));
                    break;
                case 5: // NW Theme
                    UpdateThemeColors(
                         (Color)ColorConverter.ConvertFromString("#B92D2D"), // Accent/Border
                         (Color)ColorConverter.ConvertFromString("#1C1C1C"), // Button
                         (Color)ColorConverter.ConvertFromString("#262424"), // Background
                         (Color)ColorConverter.ConvertFromString("#01DDDA")  // Text
                    );
                    ChangeLogo("/Assets/NW.png", (Color)ColorConverter.ConvertFromString("#01DDDA"));
                    break;
                case 6: // D3VL Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#000000"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#3E3E3E"), // Button
                        (Color)ColorConverter.ConvertFromString("#4C1C1C"), // Background
                        (Color)ColorConverter.ConvertFromString("#FF0000")  // Text
                    );
                    ChangeLogo("/Assets/D3VL.png", (Color)ColorConverter.ConvertFromString("#000000"));
                    break;
                case 7: // HIT Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#000000"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#3E3E3E"), // Button
                        (Color)ColorConverter.ConvertFromString("#4C1C1C"), // Background
                        (Color)ColorConverter.ConvertFromString("#FF0000")  // Text
                    );
                    ChangeLogo("/Assets/HIT.png", (Color)ColorConverter.ConvertFromString("#fd2929"));
                    break;
                case 8: // VOX Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#C0C0C0"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#1C1C1C"), // Button
                        (Color)ColorConverter.ConvertFromString("#424242"), // Background
                        (Color)ColorConverter.ConvertFromString("#FFD700")  // Text
                    );
                    ChangeLogo("/Assets/VOX.png", (Color)ColorConverter.ConvertFromString("#FFD700"));
                    break;
                case 9: // EMP Theme
                    UpdateThemeColors(
                        (Color)ColorConverter.ConvertFromString("#F5721C"), // Accent/Border
                        (Color)ColorConverter.ConvertFromString("#535353"), // Button
                        (Color)ColorConverter.ConvertFromString("#080000"), // Background
                        (Color)ColorConverter.ConvertFromString("#FFFFFF")  // Text
                    );
                    ChangeLogo("/Assets/EMP.png", (Color)ColorConverter.ConvertFromString("#F3BD9B"));
                    break;
            }
        }

        // Helper method to update both Color and Brush resources
        private void UpdateThemeColors(Color accent, Color backgroundDark, Color backgroundLight, Color text)
        {
            // Update color resources
            Application.Current.Resources["AccentColor"] = accent;
            Application.Current.Resources["BackgroundDarkColor"] = backgroundDark;
            Application.Current.Resources["BackgroundLightColor"] = backgroundLight;
            Application.Current.Resources["TextColor"] = text;

            // Update SolidColorBrush resources
            Application.Current.Resources["AccentBrush"] = new SolidColorBrush(accent);
            Application.Current.Resources["BackgroundDarkBrush"] = new SolidColorBrush(backgroundDark);
            Application.Current.Resources["BackgroundLightBrush"] = new SolidColorBrush(backgroundLight);
            Application.Current.Resources["TextBrush"] = new SolidColorBrush(text);
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
            var dialog = new OpenFileDialog();
            dialog.CheckFileExists = false;
            dialog.ValidateNames = false;
            dialog.Filter = "All files|*.*";

            if (dialog.ShowDialog() == true)
            {
                // Extract only the directory path from the file
                string selectedFolder = Path.GetDirectoryName(dialog.FileName);
                VideoPath.Text = selectedFolder; // Set the folder path
            }
        }

        private void VisorWipeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            ConfigManager.VisorWipe = (int)slider.Value; // 0 or 1

            // Check if the value is 0 or 1 and apply the corresponding style
            if (ConfigManager.VisorWipe == 0)
            {
                slider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");  // Apply FalseToggleStyle
            }
            else
            {
                slider.Style = (Style)Application.Current.FindResource("ToggleSliderStyle"); // Apply ToggleSliderStyle
            }
        }

        private void VideoRecordSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            ConfigManager.VideoRecord = (int)slider.Value; // 0 or 1

            // Check if the value is 0 or 1 and apply the corresponding style
            if (ConfigManager.VideoRecord == 0)
            {
                slider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");  // Apply FalseToggleStyle
            }
            else
            {
                slider.Style = (Style)Application.Current.FindResource("ToggleSliderStyle"); // Apply ToggleSliderStyle
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
            // Get the directory of the running executable
            string exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            // Combine the executable directory with the config file name
            string configFilePath = Path.Combine(exeDirectory, "config.ini");

            using (StreamWriter writer = new StreamWriter(configFilePath))
            {
                writer.WriteLine($"LogFile={LogFilePath.Text}");
                writer.WriteLine($"ApiUrl={ApiUrl.Text}");
                writer.WriteLine($"ApiKey={ApiKey.Text}");
                writer.WriteLine($"VideoPath={VideoPath.Text}");
                writer.WriteLine($"VisorWipe={(int)VisorWipeSlider.Value}");
                writer.WriteLine($"VideoRecord={(int)VideoRecordSlider.Value}");
                writer.WriteLine($"OfflineMode={(int)OfflineModeSlider.Value}");
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
    }
}
