using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.IO;
using System.Windows.Documents;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace AutoTrackR2
{
    public partial class HomePage : UserControl
    {
        public HomePage()
        {
            InitializeComponent();

            // Get the current month
            string currentMonth = DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture);

            // Set the TextBlock text
            KillTallyTitle.Text = $"Tally - {currentMonth}";
        }

        private Process runningProcess; // Field to store the running process

        // Update Start/Stop button states based on the isRunning flag
        public void UpdateButtonState(bool isRunning)
        {
            var accentColor = (Color)Application.Current.Resources["AccentColor"];

            if (isRunning)
            {
                // Set Start button to "Running..." and apply glow effect
                StartButton.Content = "Running...";
                StartButton.IsEnabled = false; // Disable Start button
                StartButton.Style = (Style)FindResource("DisabledButtonStyle");

                // Add glow effect to the Start button
                StartButton.Effect = new DropShadowEffect
                {
                    Color = accentColor,
                    BlurRadius = 30,       // Adjust blur radius for desired glow intensity
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
                StartButton.IsEnabled = true;  // Enable Start button

                // Remove the glow effect from Start button
                StartButton.Effect = null;

                StopButton.Style = (Style)FindResource("DisabledButtonStyle");
                StartButton.Style = (Style)FindResource("ButtonStyle");
                StopButton.IsEnabled = false; // Disable Stop button
            }
        }

        public void StartButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButtonState(true);
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
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    runningProcess = new Process { StartInfo = psi }; // Store the process in the field

                    runningProcess.OutputDataReceived += (s, e) =>
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
                                    AdjustFontSize(PilotNameTextBox);
                                }
                                else if (e.Data.Contains("PlayerShip="))
                                {
                                    string playerShip = e.Data.Split('=')[1].Trim();
                                    PlayerShipTextBox.Text = playerShip;
                                    AdjustFontSize(PlayerShipTextBox);
                                }
                                else if (e.Data.Contains("GameMode="))
                                {
                                    string gameMode = e.Data.Split('=')[1].Trim();
                                    GameModeTextBox.Text = gameMode;
                                    AdjustFontSize(GameModeTextBox);
                                }
                                else if (e.Data.Contains("KillTally="))
                                {
                                    string killTally = e.Data.Split('=')[1].Trim();
                                    KillTallyTextBox.Text = killTally;
                                    AdjustFontSize(KillTallyTextBox);
                                }
                                else if (e.Data.Contains("DeathTally="))
                                {
                                    string deathTally = e.Data.Split('=')[1].Trim();
                                    DeathTallyTextBox.Text = deathTally;
                                    AdjustFontSize(DeathTallyTextBox);
                                }
                                else if (e.Data.Contains("OtherTally="))
                                {
                                    string otherTally = e.Data.Split('=')[1].Trim();
                                    OtherTallyTextBox.Text = otherTally;
                                    AdjustFontSize(OtherTallyTextBox);
                                }
                                else if (e.Data.Contains("NewKill="))
                                {
                                    // Parse the kill data
                                    var killData = e.Data.Split('=')[1].Trim(); // Assume the kill data follows after "NewKill="
                                    var killParts = killData.Split(',');

                                    // Fetch the dynamic resource for AltTextColor
                                    var altTextColorBrush = new SolidColorBrush((Color)Application.Current.Resources["AltTextColor"]);
                                    var accentColorBrush = new SolidColorBrush((Color)Application.Current.Resources["AccentColor"]);

                                    // Fetch the Orbitron FontFamily from resources
                                    var orbitronFontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
                                    var gemunuFontFamily = (FontFamily)Application.Current.Resources["Gemunu"];

                                    // Create a new TextBlock for each kill
                                    var killTextBlock = new TextBlock
                                    {
                                        Margin = new Thickness(0, 10, 0, 10),
                                        Style = (Style)Application.Current.Resources["RoundedTextBlock"], // Apply style for text
                                        FontSize = 14,
                                        FontWeight = FontWeights.Bold,
                                        FontFamily = gemunuFontFamily,
                                    };

                                    // Add styled content using Run elements
                                    killTextBlock.Inlines.Add(new Run("Event Type: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run("Kill\n"));

                                    // Repeat for other lines
                                    killTextBlock.Inlines.Add(new Run("Victim Name: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[1]}\n"));

                                    killTextBlock.Inlines.Add(new Run("Victim Ship: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[2]}\n"));

                                    killTextBlock.Inlines.Add(new Run("Victim Org: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[3]}\n"));

                                    killTextBlock.Inlines.Add(new Run("Join Date: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[4]}\n"));

                                    killTextBlock.Inlines.Add(new Run("UEE Record: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[5]}\n"));

                                    killTextBlock.Inlines.Add(new Run("Kill Time: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[6]}"));

                                    // Create a Border and apply the RoundedTextBlockWithBorder style
                                    var killBorder = new Border
                                    {
                                        Style = (Style)Application.Current.Resources["RoundedTextBlockWithBorder"], // Apply border style
                                    };

                                    // Create a Grid to hold the TextBlock and the Image
                                    var killGrid = new Grid
                                    {
                                        Width = 400, // Adjust the width of the Grid
                                        Height = 150, // Adjust the height as needed
                                    };

                                    // Define two columns in the Grid: one for the text and one for the image
                                    killGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); // Text column
                                    killGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Image column

                                    // Add the TextBlock to the first column of the Grid
                                    Grid.SetColumn(killTextBlock, 0);
                                    killGrid.Children.Add(killTextBlock);

                                    // Create the Image for the profile
                                    string urlToUse = string.IsNullOrEmpty(killParts[7]) ? "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg" : killParts[7];
                                    var profileImage = new Image
                                    {
                                        Source = new BitmapImage(new Uri(urlToUse)), // Assuming the 8th part contains the profile image URL
                                        Width = 90,
                                        Height = 90,
                                        Stretch = Stretch.Fill, // Adjust how the image fits
                                    };

                                    // Create a Border around the Image
                                    var imageBorder = new Border
                                    {
                                        BorderBrush = accentColorBrush, // Set the border color
                                        BorderThickness = new Thickness(2), // Set the border thickness
                                        Padding = new Thickness(0), // Optional padding inside the border
                                        CornerRadius = new CornerRadius(5),
                                        Margin = new Thickness(10,18,15,18),
                                        Child = profileImage // Set the Image as the content of the Border
                                    };

                                    // Add the Border (with the image inside) to the Grid
                                    Grid.SetColumn(imageBorder, 1);
                                    killGrid.Children.Add(imageBorder);

                                    // Set the Grid as the child of the Border
                                    killBorder.Child = killGrid;

                                    // Add the new Border to the StackPanel inside the Border
                                    KillFeedStackPanel.Children.Insert(0, killBorder);
                                }
                                else if (e.Data.Contains("NewDeath="))
                                {
                                    // Parse the death data
                                    var killData = e.Data.Split('=')[1].Trim(); // Assume the death data follows after "NewKill="
                                    var killParts = killData.Split(',');

                                    // Fetch the dynamic resource for AltTextColor
                                    var altTextColorBrush = new SolidColorBrush((Color)Application.Current.Resources["AltTextColor"]);
                                    var accentColorBrush = new SolidColorBrush((Color)Application.Current.Resources["AccentColor"]);

                                    // Fetch the Orbitron FontFamily from resources
                                    var orbitronFontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
                                    var gemunuFontFamily = (FontFamily)Application.Current.Resources["Gemunu"];

                                    // Create a new TextBlock for each Death
                                    var killTextBlock = new TextBlock
                                    {
                                        Margin = new Thickness(0, 10, 0, 10),
                                        Style = (Style)Application.Current.Resources["RoundedTextBlock"], // Apply style for text
                                        FontSize = 14,
                                        FontWeight = FontWeights.Bold,
                                        FontFamily = gemunuFontFamily,
                                    };

                                    // Add styled content using Run elements
                                    killTextBlock.Inlines.Add(new Run("Event Type: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run("Death\n"));

                                    // Repeat for other lines
                                    killTextBlock.Inlines.Add(new Run("Agressor Name: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[1]}\n"));

                                    killTextBlock.Inlines.Add(new Run("Agressor Ship: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[2]}\n"));

                                    killTextBlock.Inlines.Add(new Run("Agressor Org: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[3]}\n"));

                                    killTextBlock.Inlines.Add(new Run("Join Date: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[4]}\n"));

                                    killTextBlock.Inlines.Add(new Run("UEE Record: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[5]}\n"));

                                    killTextBlock.Inlines.Add(new Run("Death Time: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[6]}"));

                                    // Create a Border and apply the RoundedTextBlockWithBorder style
                                    var killBorder = new Border
                                    {
                                        Style = (Style)Application.Current.Resources["RoundedTextBlockWithBorder"], // Apply border style
                                    };

                                    // Create a Grid to hold the TextBlock and the Image
                                    var killGrid = new Grid
                                    {
                                        Width = 400, // Adjust the width of the Grid
                                        Height = 150, // Adjust the height as needed
                                    };

                                    // Define two columns in the Grid: one for the text and one for the image
                                    killGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); // Text column
                                    killGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Image column

                                    // Add the TextBlock to the first column of the Grid
                                    Grid.SetColumn(killTextBlock, 0);
                                    killGrid.Children.Add(killTextBlock);

                                    // Create the Image for the profile
                                    string urlToUse = string.IsNullOrEmpty(killParts[7]) ? "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg" : killParts[7];
                                    var profileImage = new Image
                                    {
                                        Source = new BitmapImage(new Uri(urlToUse)), // Assuming the 8th part contains the profile image URL
                                        Width = 90,
                                        Height = 90,
                                        Stretch = Stretch.Fill, // Adjust how the image fits
                                    };

                                    // Create a Border around the Image
                                    var imageBorder = new Border
                                    {
                                        BorderBrush = accentColorBrush, // Set the border color
                                        BorderThickness = new Thickness(2), // Set the border thickness
                                        Padding = new Thickness(0), // Optional padding inside the border
                                        CornerRadius = new CornerRadius(5),
                                        Margin = new Thickness(10,18,15,18),
                                        Child = profileImage // Set the Image as the content of the Border
                                    };

                                    // Add the Border (with the image inside) to the Grid
                                    Grid.SetColumn(imageBorder, 1);
                                    killGrid.Children.Add(imageBorder);

                                    // Set the Grid as the child of the Border
                                    killBorder.Child = killGrid;

                                    // Add the new Border to the StackPanel inside the Border
                                    KillFeedStackPanel.Children.Insert(0, killBorder);
                                }
                                else if (e.Data.Contains("NewOther="))
                                {
                                    // Parse the death data
                                    var killData = e.Data.Split('=')[1].Trim(); // Assume the death data follows after "NewKill="
                                    var killParts = killData.Split(',');

                                    // Fetch the dynamic resource for AltTextColor
                                    var altTextColorBrush = new SolidColorBrush((Color)Application.Current.Resources["AltTextColor"]);
                                    var accentColorBrush = new SolidColorBrush((Color)Application.Current.Resources["AccentColor"]);

                                    // Fetch the Orbitron FontFamily from resources
                                    var orbitronFontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
                                    var gemunuFontFamily = (FontFamily)Application.Current.Resources["Gemunu"];

                                    // Create a new TextBlock for each Death
                                    var killTextBlock = new TextBlock
                                    {
                                        Margin = new Thickness(0, 10, 0, 10),
                                        Style = (Style)Application.Current.Resources["RoundedTextBlock"], // Apply style for text
                                        FontSize = 14,
                                        FontWeight = FontWeights.Bold,
                                        FontFamily = gemunuFontFamily,
                                    };

                                    // Add styled content using Run elements
                                    killTextBlock.Inlines.Add(new Run("Event Type: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run("Other\n"));

                                    killTextBlock.Inlines.Add(new Run("Death by: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[8]}\n"));

                                    // Repeat for other lines
                                    killTextBlock.Inlines.Add(new Run("Sueside Time: ")
                                    {
                                        Foreground = altTextColorBrush,
                                        FontFamily = orbitronFontFamily,
                                    });
                                    killTextBlock.Inlines.Add(new Run($"{killParts[6]}"));

                                    // Create a Border and apply the RoundedTextBlockWithBorder style
                                    var killBorder = new Border
                                    {
                                        Style = (Style)Application.Current.Resources["RoundedTextBlockWithBorder"], // Apply border style
                                    };

                                    // Create a Grid to hold the TextBlock and the Image
                                    var killGrid = new Grid
                                    {
                                        Width = 400, // Adjust the width of the Grid
                                        Height = 70, // Adjust the height as needed
                                    };

                                    // Define two columns in the Grid: one for the text and one for the image
                                    killGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); // Text column

                                    // Add the TextBlock to the first column of the Grid
                                    Grid.SetColumn(killTextBlock, 0);
                                    killGrid.Children.Add(killTextBlock);

                                    // Set the Grid as the child of the Border
                                    killBorder.Child = killGrid;

                                    // Add the new Border to the StackPanel inside the Border
                                    KillFeedStackPanel.Children.Insert(0, killBorder);
                                }

                                else
                                {
                                    string currentText = DebugPanel.Text;
                                    DebugPanel.Text = e.Data + Environment.NewLine + currentText;
                                }
                            });
                        }
                    };

                    runningProcess.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                string currentText = DebugPanel.Text;
                                DebugPanel.Text = e.Data + Environment.NewLine + currentText;
                            });
                        }
                    };

                    runningProcess.Start();
                    runningProcess.BeginOutputReadLine();
                    runningProcess.BeginErrorReadLine();

                    runningProcess.WaitForExit();
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

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (runningProcess != null && !runningProcess.HasExited)
            {
                // Kill the running process
                runningProcess.Kill();
                runningProcess = null; // Clear the reference to the process
            }

            // Clear the text boxes
            System.Threading.Thread.Sleep(200);
            PilotNameTextBox.Text = string.Empty;
            PlayerShipTextBox.Text = string.Empty;
            GameModeTextBox.Text = string.Empty;
            KillTallyTextBox.Text = "K";
            DeathTallyTextBox.Text = "D";
            OtherTallyTextBox.Text = "O";
            KillFeedStackPanel.Children.Clear();
        }

        private void AdjustFontSize(TextBlock textBlock)
        {
            // Set a starting font size
            double fontSize = 14;
            double maxWidth = textBlock.Width;

            if (string.IsNullOrEmpty(textBlock.Text) || double.IsNaN(maxWidth))
                return;

            // Measure the rendered width of the text
            FormattedText formattedText = new FormattedText(
                textBlock.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                fontSize,
                textBlock.Foreground,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            // Reduce font size until text fits within the width
            while (formattedText.Width > maxWidth && fontSize > 6)
            {
                fontSize -= 0.5;
                formattedText = new FormattedText(
                    textBlock.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                    fontSize,
                    textBlock.Foreground,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip
                );
            }

            // Apply the adjusted font size
            textBlock.FontSize = fontSize;
        }
    }
}