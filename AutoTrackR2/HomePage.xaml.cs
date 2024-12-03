using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.IO;

namespace AutoTrackR2
{
    public partial class HomePage : UserControl
    {
        public HomePage()
        {
            InitializeComponent();
        }

        // Update Start/Stop button states based on the isRunning flag
        public void UpdateButtonState(bool isRunning)
        {
            var accentColor = (Color)Application.Current.Resources["AccentColor"];

            if (isRunning)
            {
                // Set Start button to "Running..." and apply glow effect
                StartButton.Content = "Running...";
                StartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00A9E0"));
                StartButton.IsEnabled = false; // Disable Start button
                StartButton.Style = (Style)FindResource("DisabledButtonStyle");

                // Add glow effect to the Start button
                StartButton.Effect = new DropShadowEffect
                {
                    Color = accentColor,
                    BlurRadius = 20,       // Adjust blur radius for desired glow intensity
                    ShadowDepth = 0,       // Set shadow depth to 0 for a pure glow effect
                    Opacity = 1,           // Set opacity for glow visibility
                    Direction = 0          // Direction doesn't matter for glow
                };

                StopButton.Style = (Style)FindResource("ButtonStyle");
                StopButton.IsEnabled = true;  // Enable Stop button
            }
            else
            {
                // Reset Start button back to its original state
                StartButton.Content = "Start";
                StartButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F1A2B"));
                StartButton.IsEnabled = true;  // Enable Start button

                // Remove the glow effect from Start button
                StartButton.Effect = null;

                StopButton.Style = (Style)FindResource("DisabledButtonStyle");
                StartButton.Style = (Style)FindResource("ButtonStyle");
                StopButton.IsEnabled = false; // Disable Stop button
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KillTrackR_MainScript.ps1");
            TailFileAsync(scriptPath);
        }

        private async void TailFileAsync(string scriptPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = new Process { StartInfo = psi })
                    {
                        process.OutputDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    // Parse and display key-value pairs in the OutputTextBox
                                    if (e.Data.Contains("PlayerName="))
                                    {
                                        string pilotName = e.Data.Split('=')[1].Trim();
                                        PilotNameTextBox.Text = pilotName; // Update the Button's Content
                                    }
                                    else if (e.Data.Contains("PlayerShip="))
                                    {
                                        string playerShip = e.Data.Split('=')[1].Trim();
                                        PlayerShipTextBox.Text = playerShip;
                                    }
                                    else if (e.Data.Contains("GameMode="))
                                    {
                                        string gameMode = e.Data.Split('=')[1].Trim();
                                        GameModeTextBox.Text = gameMode;
                                    }
                                    else
                                    {
                                        OutputTextBox.AppendText(e.Data + Environment.NewLine);
                                    }
                                });
                            }
                        };

                        process.ErrorDataReceived += (s, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    OutputTextBox.AppendText("Error: " + e.Data + Environment.NewLine);
                                });
                            }
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Error running script: {ex.Message}");
                    });
                }
            });
        }

    }
}
