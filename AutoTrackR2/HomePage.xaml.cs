using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Documents;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using AutoTrackR2.LogEventHandlers;

namespace AutoTrackR2;

public partial class HomePage : UserControl
{
    private Process runningProcess; // Field to store the running process
    private LogHandler? _logHandler;
    private KillHistoryManager _killHistoryManager;
    private bool _UIEventsRegistered = false;
    
    public HomePage()
    {
        InitializeComponent();
        
        _killHistoryManager = new KillHistoryManager(ConfigManager.KillHistoryFile);

        // Set the TextBlock text
        KillTallyTitle.Text = $"Kill Tally - {_killHistoryManager.GetKillsInCurrentMonth().Count}";
        AddKillHistoryKillsToUI();
    }
    
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
        
        RegisterUIEventHandlers();
    }

    public void StartButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateButtonState(true);
        //string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KillTrackR_MainScript.ps1");
        // TailFileAsync(scriptPath);
        
        // _logHandler = new LogHandler(@"U:\\StarCitizen\\StarCitizen\\LIVE\\Game.log");
        _logHandler = new LogHandler(ConfigManager.LogFile);
        _logHandler.Initialize();
        
    }
    
    private void AddKillHistoryKillsToUI()
    {
        var kills = _killHistoryManager.GetKills();
        foreach (var kill in kills)
        {
            Dispatcher.Invoke(() => { AddKillToScreen(kill); });
        }
    }

    private void RegisterUIEventHandlers()
    {
        if (_UIEventsRegistered)
            return;
        
        // Username
        TrackREventDispatcher.PlayerLoginEvent += (username) => {
            Dispatcher.Invoke(() =>
            {
                PilotNameTextBox.Text = username;
                AdjustFontSize(PilotNameTextBox);
                LocalPlayerData.Username = username;
            });
        };
        
        // Ship
        TrackREventDispatcher.JumpDriveStateChangedEvent += (shipName) => {
                Dispatcher.Invoke(() =>
                {
                    PlayerShipTextBox.Text = shipName;
                    AdjustFontSize(PlayerShipTextBox);
                    LocalPlayerData.PlayerShip = shipName;
                });
        };
        
        // Game Mode
        TrackREventDispatcher.PlayerChangedGameModeEvent += (mode) => {
            Dispatcher.Invoke(() =>
            {
                GameModeTextBox.Text = mode.ToString();
                AdjustFontSize(GameModeTextBox);
                LocalPlayerData.CurrentGameMode = mode;
            });
        };
        
        // Game Version
        TrackREventDispatcher.GameVersionEvent += (version) => {
                LocalPlayerData.GameVersion = version;
        };
        
        // Actor Death
        TrackREventDispatcher.ActorDeathEvent += async (actorDeathData) => {
            if (actorDeathData.VictimPilot != LocalPlayerData.Username)
            {
                var playerData = await WebHandler.GetPlayerData(actorDeathData.VictimPilot);

                if (playerData != null)
                {
                    var killData = new KillData
                    {
                        EnemyPilot = actorDeathData.VictimPilot,
                        EnemyShip = actorDeathData.VictimShip,
                        OrgAffiliation = playerData?.OrgName,
                        Weapon = actorDeathData.Weapon,
                        Ship = LocalPlayerData.PlayerShip ?? "Unknown",
                        Method = actorDeathData.DamageType,
                        RecordNumber = playerData?.UEERecord,
                        GameVersion = LocalPlayerData.GameVersion ?? "Unknown",
                        TrackRver = UpdatePage.currentVersion.Replace("v", "") ?? "Unknown",
                        Enlisted = playerData?.JoinDate,
                        KillTime = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm"),
                        PFP = playerData?.PFPURL ?? "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg"
                    };
                    
                    switch (LocalPlayerData.CurrentGameMode)
                    {
                        case GameMode.PersistentUniverse:
                            killData.Mode = "pu";
                            break;
                        case GameMode.ArenaCommander:
                            killData.Mode = "ac";
                            break;
                    }
                    
                    // Add kill to UI
                    Dispatcher.Invoke(() =>
                    {
                        AddKillToScreen(killData);
                    });
    
                    // Only submit kill data if not in offline mode
                    if (ConfigManager.OfflineMode == 0)
                    {
                        await WebHandler.SubmitKill(killData);
                    }

                    _killHistoryManager.AddKill(killData);
                    VisorWipe();
                    VideoRecord();
                }
            }
        };
        
        // Vehicle Destruction
        TrackREventDispatcher.VehicleDestructionEvent += (data) => {
            LocalPlayerData.LastSeenVehicleLocation = data.VehicleZone;
        };
        
        _UIEventsRegistered = true;
    }

    private void AddKillToScreen(KillData killData)
    { 
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
        killTextBlock.Inlines.Add(new Run("Victim Name: ")
        {
            Foreground = altTextColorBrush,
            FontFamily = orbitronFontFamily,
        });
        killTextBlock.Inlines.Add(new Run($"{killData.EnemyPilot}\n"));

        // Repeat for other lines
        killTextBlock.Inlines.Add(new Run("Victim Ship: ")
        {
            Foreground = altTextColorBrush,
            FontFamily = orbitronFontFamily,
        });
        killTextBlock.Inlines.Add(new Run($"{killData.EnemyShip}\n"));

        killTextBlock.Inlines.Add(new Run("Victim Org: ")
        {
            Foreground = altTextColorBrush,
            FontFamily = orbitronFontFamily,
        });
        killTextBlock.Inlines.Add(new Run($"{killData.OrgAffiliation}\n"));
        
        killTextBlock.Inlines.Add(new Run("Join Date: ")
        {
            Foreground = altTextColorBrush,
            FontFamily = orbitronFontFamily,
        });
        killTextBlock.Inlines.Add(new Run($"{killData.Enlisted}\n"));
        
        killTextBlock.Inlines.Add(new Run("UEE Record: ")
        {
            Foreground = altTextColorBrush,
            FontFamily = orbitronFontFamily,
        });
        
        killTextBlock.Inlines.Add(new Run($"{killData.RecordNumber}\n"));
        
        killTextBlock.Inlines.Add(new Run("Kill Time: ")
        {
            Foreground = altTextColorBrush,
            FontFamily = orbitronFontFamily,
        });
        killTextBlock.Inlines.Add(new Run($"{killData.KillTime}"));

        // Create a Border and apply the RoundedTextBlockWithBorder style
        var killBorder = new Border
        {
            Style = (Style)Application.Current.Resources["RoundedTextBlockWithBorder"], // Apply border style
        };

        // Create a Grid to hold the TextBlock and the Image
        var killGrid = new Grid
        {
            Width = 400, // Adjust the width of the Grid
            Height = 130, // Adjust the height as needed
        };

        // Define two columns in the Grid: one for the text and one for the image
        killGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }); // Text column
        killGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Image column

        // Add the TextBlock to the first column of the Grid
        Grid.SetColumn(killTextBlock, 0);
        killGrid.Children.Add(killTextBlock);

        if (killData.PFP == "")
        {
            killData.PFP = "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg";
        }
        
        // Create the Image for the profile
        var profileImage = new Image
        {
            Source = new BitmapImage(new Uri(killData.PFP)), // Assuming the 8th part contains the profile image URL
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
        Dispatcher.Invoke(() =>
        {
            KillFeedStackPanel.Children.Insert(0, killBorder);
        });
    }

    public void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _logHandler?.Stop();

        // Clear the text boxes
        // System.Threading.Thread.Sleep(200);
        // PilotNameTextBox.Text = string.Empty;
        // PlayerShipTextBox.Text = string.Empty;
        // GameModeTextBox.Text = string.Empty;
        // KillTallyTextBox.Text = string.Empty;
        // KillFeedStackPanel.Children.Clear();
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
    
    public static void RunAHKScript(string path)
    {
        string scriptPath = Path.Combine(ConfigManager.AHKScriptFolder, path);
            
        if (!File.Exists(scriptPath))
        {
            return;
        }
            
        // Run the script using powershell
        using var ahkProcess = new Process();
            
        // Runs the script via Explorer, ensuring it uses whatever the
        // default binary for AHK is. Skips having to find a specific path to AHK
        ahkProcess.StartInfo.FileName = "explorer";
        ahkProcess.StartInfo.Arguments = "\"" + scriptPath + "\"";
        ahkProcess.Start();
    }

    private void VisorWipe()
    {
        if (ConfigManager.VisorWipe == 1)
        {
            RunAHKScript(ConfigManager.VisorWipeScript);
        }
    }
    
    private void VideoRecord()
    {
        if (ConfigManager.VideoRecord == 1)
        {
            RunAHKScript(ConfigManager.VideoRecordScript);
        }
    }
}
