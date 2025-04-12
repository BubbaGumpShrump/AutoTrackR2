using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace AutoTrackR2;

public partial class ConfigPage : UserControl
{
    // Store the current slider value
    private double savedSliderValue = 0;

    private MainWindow mainWindow;

    Dictionary<string, Theme>? _themes = null;

    private KillStreakManager? _testKillStreakManager;
    private bool _isTestRunning = false;
    private int _currentTestStep = 0;

    public ConfigPage(MainWindow mainWindow)
    {
        InitializeComponent();
        this.mainWindow = mainWindow;

        LogFilePath.Text = ConfigManager.LogFile;
        ApiUrl.Text = ConfigManager.ApiUrl;
        ApiKey.Password = ConfigManager.ApiKey;
        VideoPath.Text = ConfigManager.VideoPath;
        VisorWipeSlider.Value = ConfigManager.VisorWipe;
        VideoRecordSlider.Value = ConfigManager.VideoRecord;
        OfflineModeSlider.Value = ConfigManager.OfflineMode;
        ThemeSlider.Value = ConfigManager.Theme;
        StreamlinkSlider.Value = ConfigManager.StreamlinkEnabled;
        StreamlinkDurationSlider.Value = ConfigManager.StreamlinkDuration;

        // Initialize Streamlink slider style
        if (StreamlinkSlider.Value == 0)
        {
            StreamlinkSlider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
        }
        else
        {
            StreamlinkSlider.Style = (Style)Application.Current.FindResource("ToggleSliderStyle");
        }

        ApplyToggleModeStyle(OfflineModeSlider.Value, VisorWipeSlider.Value, VideoRecordSlider.Value);

        const string themeJsonPath = "themes.json";
        var themeJson = File.ReadAllText(themeJsonPath);
        _themes = JsonSerializer.Deserialize<Dictionary<string, Theme>>(themeJson);

        // Set the theme slider's maximum value based on the number of themes
        if (_themes != null)
        {
            ThemeSlider.Maximum = _themes.Count - 1;
        }
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
    public void SetConfigValues(string logFile, string apiUrl, string apiKey, string videoPath, int visorWipe, int videoRecord, int offlineMode, int theme)
    {
        LogFilePath.Text = logFile;
        ApiUrl.Text = apiUrl;
        ApiKey.Password = apiKey;
        VideoPath.Text = videoPath;
        VisorWipeSlider.Value = visorWipe;
        VideoRecordSlider.Value = videoRecord;
        OfflineModeSlider.Value = offlineMode;
        ThemeSlider.Value = theme;

        // Handle themes
        ApplyTheme(theme);
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
        var theme = _themes?.Values.ElementAtOrDefault(themeIndex);
        if (theme == null) return;

        // Update the logo
        if (theme.Logo != null && theme.Logo.Path != null)
        {
            ChangeLogo(theme.Logo.Path,
                theme.Logo.Primary != null
                    ? (Color)ColorConverter.ConvertFromString(theme.Logo.Primary)
                    : Colors.Transparent);
        }

        // Update the colors
        if (theme.Colors != null)
        {
            var accent = (Color)ColorConverter.ConvertFromString(theme.Colors.Accent);
            var button = (Color)ColorConverter.ConvertFromString(theme.Colors.Button);
            var backgroundDark = (Color)ColorConverter.ConvertFromString(theme.Colors.Background);
            var text = (Color)ColorConverter.ConvertFromString(theme.Colors.Text);
            var altText = (Color)ColorConverter.ConvertFromString(theme.Colors.AltText);

            UpdateThemeColors(accent, button, backgroundDark, text, altText);
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
        var dialog = new Microsoft.Win32.OpenFileDialog();
        dialog.ValidateNames = false;
        dialog.CheckFileExists = false;
        dialog.CheckPathExists = true;
        dialog.FileName = "Folder Selection";

        if (dialog.ShowDialog() == true)
        {
            string? selectedFolder = Path.GetDirectoryName(dialog.FileName);
            if (selectedFolder != null)
            {
                VideoPath.Text = selectedFolder;
            }
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

    private void VisorWipeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Slider slider = (Slider)sender;

        // Build the dynamic file path for the current user
        if (string.IsNullOrEmpty(ConfigManager.AHKScriptFolder))
        {
            MessageBox.Show("AHK script folder path is not configured.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        string filePath = Path.Combine(
            ConfigManager.AHKScriptFolder,
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ConfigManager.ApiKey = ApiKey.Password;
        ConfigManager.ApiUrl = ApiUrl.Text;
        ConfigManager.LogFile = LogFilePath.Text;
        ConfigManager.VideoPath = VideoPath.Text;
        ConfigManager.VisorWipe = (int)VisorWipeSlider.Value;
        ConfigManager.VideoRecord = (int)VideoRecordSlider.Value;
        ConfigManager.OfflineMode = (int)OfflineModeSlider.Value;
        ConfigManager.Theme = (int)ThemeSlider.Value;

        // Save the current config values
        ConfigManager.SaveConfig();
        // Start the flashing effect
        FlashSaveButton();
    }

    private void FlashSaveButton()
    {
        string? originalText = SaveButton.Content?.ToString() ?? string.Empty;
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
        string apiKey = ApiKey.Password;
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
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoTrackR2");

                // Create JSON body with version
                var jsonBody = new { version = "2.10" };
                var content = new StringContent(JsonSerializer.Serialize(jsonBody), Encoding.UTF8, "application/json");

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

    private void StreamlinkSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Slider slider = (Slider)sender;

        // Only allow enabling if streamlink is installed
        if (slider.Value == 1)
        {
            if (!StreamlinkHandler.IsStreamlinkInstalled())
            {
                MessageBox.Show("Streamlink is not installed. Please install Streamlink to use this feature.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                slider.Value = 0;
                return;
            }
        }

        ConfigManager.StreamlinkEnabled = (int)slider.Value;

        // Apply the appropriate style based on the value
        if (slider.Value == 0)
        {
            slider.Style = (Style)Application.Current.FindResource("FalseToggleStyle");
        }
        else
        {
            slider.Style = (Style)Application.Current.FindResource("ToggleSliderStyle");
        }
    }

    private void StreamlinkDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (StreamlinkDurationText != null)
        {
            int duration = (int)e.NewValue;
            StreamlinkDurationText.Text = $"{duration}s";
            ConfigManager.StreamlinkDuration = duration;
        }
    }

    private async void TestStreamlinkButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("TestStreamlinkButton_Click: Starting streamlink test");

        if (ConfigManager.StreamlinkEnabled != 1)
        {
            Debug.WriteLine("TestStreamlinkButton_Click: Streamlink is not enabled");
            MessageBox.Show("Streamlink is not enabled. Please enable Streamlink to test.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Debug.WriteLine("TestStreamlinkButton_Click: Configuring HTTP client");
            // Configure HttpClient with TLS 1.2
            var handler = new HttpClientHandler
            {
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };

            using (var client = new HttpClient(handler))
            {
                // Set headers
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ConfigManager.ApiKey);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AutoTrackR2");

                // Create JSON body with version
                var jsonBody = new { version = "2.10" };
                var content = new StringContent(JsonSerializer.Serialize(jsonBody), Encoding.UTF8, "application/json");

                // Send POST to test endpoint
                string baseUrl = Regex.Replace(ConfigManager.ApiUrl ?? "", @"(https?://[^/]+)/?.*", "$1");
                string endpoint = "test";
                string fullUrl = $"{baseUrl}/{endpoint}";
                Debug.WriteLine($"TestStreamlinkButton_Click: Sending request to {fullUrl}");

                var response = await client.PostAsync(fullUrl, content);
                Debug.WriteLine($"TestStreamlinkButton_Click: Response status code: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"TestStreamlinkButton_Click: Response content: {responseContent}");
                    WebHandler.ProcessStreamerResponse(responseContent);
                    MessageBox.Show("Streamlink Test Success.");
                }
                else
                {
                    Debug.WriteLine($"TestStreamlinkButton_Click: Error response: {response.StatusCode} - {response.ReasonPhrase}");
                    MessageBox.Show($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TestStreamlinkButton_Click: Exception: {ex.Message}");
            MessageBox.Show($"Streamlink Test Failure. {ex.Message}");
        }
    }

    private void LogFileOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(LogFilePath.Text))
        {
            string directory = Path.GetDirectoryName(LogFilePath.Text) ?? string.Empty;
            if (Directory.Exists(directory))
            {
                Process.Start("explorer.exe", directory);
            }
            else
            {
                MessageBox.Show("Directory does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void VideoPathOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(VideoPath.Text) && Directory.Exists(VideoPath.Text))
        {
            Process.Start("explorer.exe", VideoPath.Text);
        }
        else
        {
            MessageBox.Show("Directory does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TestKillStreakButton_Click(object sender, RoutedEventArgs e)
    {
        // Create a single KillStreakManager instance if it doesn't exist
        if (_testKillStreakManager == null)
        {
            var soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds");
            _testKillStreakManager = new KillStreakManager(soundsPath);
        }

        // Simulate 5 quick kills
        for (int i = 0; i < 5; i++)
        {
            _testKillStreakManager.OnKill();
        }

        // Reset the streak after all sounds have played
        await Task.Delay(1000);
        _testKillStreakManager.OnDeath();
    }
}

public class Theme
{
    public ThemeColors? Colors { get; set; }
    public ThemeLogo? Logo { get; set; }
}

public class ThemeColors
{
    public string? Accent { get; set; }
    public string? Button { get; set; }
    public string? Background { get; set; }
    public string? Text { get; set; }
    public string? AltText { get; set; }
}

public class ThemeLogo
{
    public string? Path { get; set; }
    public string? Primary { get; set; } // Optional: null if not used
}
