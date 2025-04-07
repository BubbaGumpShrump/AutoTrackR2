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
using System.Timers;
using System.Linq;

namespace AutoTrackR2;

public partial class HomePage : UserControl
{
    private LogHandler? _logHandler;
    private KillHistoryManager _killHistoryManager;
    private bool _UIEventsRegistered = false;
    private System.Timers.Timer _statusCheckTimer;
    private bool _isLogHandlerRunning = false;

    public HomePage()
    {
        InitializeComponent();

        _killHistoryManager = new KillHistoryManager(ConfigManager.KillHistoryFile);

        // Set the TextBlock text
        KillTallyTitle.Text = $"Kill Tally - {DateTime.Now.ToString("MMMM")}";
        KillTallyTextBox.Text = _killHistoryManager.GetKillsInCurrentMonth().Count.ToString();
        AdjustFontSize(KillTallyTextBox);
        AddKillHistoryKillsToUI();

        // Initialize and start the status check timer
        _statusCheckTimer = new System.Timers.Timer(1000); // Check every second
        _statusCheckTimer.Elapsed += CheckStarCitizenStatus;
        _statusCheckTimer.Start();

        // Check if Star Citizen is already running and initialize accordingly
        if (IsStarCitizenRunning())
        {
            Dispatcher.Invoke(() =>
            {
                UpdateStatusIndicator(true);
                ReadInitialStates(); // Read states first
                InitializeLogHandler(); // Then initialize the log handler
            });
        }
    }

    private void CheckStarCitizenStatus(object sender, ElapsedEventArgs e)
    {
        bool isRunning = IsStarCitizenRunning();
        Dispatcher.Invoke(() =>
        {
            UpdateStatusIndicator(isRunning);

            if (isRunning)
            {
                if (!_isLogHandlerRunning)
                {
                    // Game is running, start log monitoring and read initial states
                    InitializeLogHandler();
                    ReadInitialStates();
                }
            }
            else
            {
                // Game is not running, set everything to Unknown
                GameModeTextBox.Text = "Unknown";
                PlayerShipTextBox.Text = "Unknown";
                PilotNameTextBox.Text = "Unknown";
                LocalPlayerData.CurrentGameMode = GameMode.Unknown;
                LocalPlayerData.PlayerShip = string.Empty;
                LocalPlayerData.Username = string.Empty;

                // Stop log monitoring if it's running
                if (_isLogHandlerRunning)
                {
                    _logHandler?.StopMonitoring();
                    _isLogHandlerRunning = false;
                }
            }
        });
    }

    private void UpdateStatusIndicator(bool isRunning)
    {
        if (isRunning)
        {
            StatusLight.Fill = new SolidColorBrush(Colors.Green);
            StatusText.Text = "TrackR\nActive";
        }
        else
        {
            StatusLight.Fill = new SolidColorBrush(Colors.Red);
            StatusText.Text = "TrackR\nStandby";
        }
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
        TrackREventDispatcher.PlayerLoginEvent += (username) =>
        {
            Dispatcher.Invoke(() =>
            {
                PilotNameTextBox.Text = username;
                AdjustFontSize(PilotNameTextBox);
                LocalPlayerData.Username = username;
            });
        };

        // Ship
        TrackREventDispatcher.JumpDriveStateChangedEvent += (shipName) =>
        {
            Dispatcher.Invoke(() =>
            {
                PlayerShipTextBox.Text = LocalPlayerData.CurrentGameMode == GameMode.PersistentUniverse ? "Player" : shipName;
                AdjustFontSize(PlayerShipTextBox);
                LocalPlayerData.PlayerShip = shipName;
            });
        };

        // Game Mode
        TrackREventDispatcher.PlayerChangedGameModeEvent += (mode) =>
        {
            Dispatcher.Invoke(() =>
            {
                GameModeTextBox.Text = mode == GameMode.PersistentUniverse ? "Player" : mode.ToString();
                AdjustFontSize(GameModeTextBox);
                LocalPlayerData.CurrentGameMode = mode;
            });
        };

        // Game Version
        TrackREventDispatcher.GameVersionEvent += (version) =>
        {
            LocalPlayerData.GameVersion = version;
        };

        // Actor Death
        TrackREventDispatcher.ActorDeathEvent += async (actorDeathData) =>
        {
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
        TrackREventDispatcher.VehicleDestructionEvent += (data) =>
        {
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
        var imageBorder = new Border();
        imageBorder.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
        imageBorder.BorderThickness = new Thickness(2);
        imageBorder.Padding = new Thickness(0);
        imageBorder.CornerRadius = new CornerRadius(5);
        imageBorder.Margin = new Thickness(10, 18, 15, 18);
        imageBorder.Child = profileImage;

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
        _logHandler?.StopMonitoring();

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

    public void InitializeLogHandler()
    {
        if (_logHandler == null)
        {
            _logHandler = new LogHandler(ConfigManager.LogFile);
            _logHandler.Initialize();
            RegisterUIEventHandlers();
            _isLogHandlerRunning = true;

            // Read initial states after initializing log handler
            ReadInitialStates();
        }
        else if (!_isLogHandlerRunning)
        {
            _logHandler.Initialize();
            _isLogHandlerRunning = true;
            ReadInitialStates();
        }
    }

    private void ReadInitialStates()
    {
        if (string.IsNullOrEmpty(ConfigManager.LogFile) || !File.Exists(ConfigManager.LogFile))
        {
            Debug.WriteLine("Log file not found or path is empty");
            return;
        }

        try
        {
            Debug.WriteLine("Reading initial states from log file...");
            // Read the entire log file
            var lines = File.ReadAllLines(ConfigManager.LogFile);
            string username = "";
            string shipName = "";
            GameMode gameMode = GameMode.Unknown;

            // Read from the end of the file to get the most recent states
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i];

                // Check for username (login)
                if (line.Contains("'s Character"))
                {
                    int startIndex = line.IndexOf("'s Character");
                    if (startIndex > 0)
                    {
                        username = line.Substring(0, startIndex).Trim();
                        Debug.WriteLine($"Found username: {username}");
                    }
                }
                // Check for ship name
                else if (line.Contains("Entering quantum travel from"))
                {
                    int startIndex = line.IndexOf("in ship") + 8;
                    int endIndex = line.IndexOf(" to ", startIndex);
                    if (startIndex > 8 && endIndex > startIndex)
                    {
                        shipName = line.Substring(startIndex, endIndex - startIndex).Trim();
                        Debug.WriteLine($"Found ship: {shipName}");
                    }
                }
                // Check for game mode
                else if (line.Contains("Loading level"))
                {
                    if (line.Contains("Persistent_Universe"))
                    {
                        gameMode = GameMode.PersistentUniverse;
                        Debug.WriteLine("Found game mode: PU");
                    }
                    else if (line.Contains("Arena_Commander"))
                    {
                        gameMode = GameMode.ArenaCommander;
                        Debug.WriteLine("Found game mode: AC");
                    }
                }

                // If we've found all the information we need, we can stop reading
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(shipName) && gameMode != GameMode.Unknown)
                {
                    break;
                }
            }

            // Update UI with found states
            Dispatcher.Invoke(() =>
            {
                if (!string.IsNullOrEmpty(username))
                {
                    PilotNameTextBox.Text = username;
                    LocalPlayerData.Username = username;
                    AdjustFontSize(PilotNameTextBox);
                    Debug.WriteLine($"Set username in UI: {username}");
                }
                else
                {
                    PilotNameTextBox.Text = "Unknown";
                    LocalPlayerData.Username = string.Empty;
                    AdjustFontSize(PilotNameTextBox);
                    Debug.WriteLine("Username not found, set to Unknown");
                }

                if (!string.IsNullOrEmpty(shipName))
                {
                    PlayerShipTextBox.Text = gameMode == GameMode.PersistentUniverse ? "Player" : shipName;
                    LocalPlayerData.PlayerShip = shipName;
                    AdjustFontSize(PlayerShipTextBox);
                    Debug.WriteLine($"Set ship in UI: {PlayerShipTextBox.Text}");
                }
                else
                {
                    PlayerShipTextBox.Text = "Unknown";
                    LocalPlayerData.PlayerShip = string.Empty;
                    AdjustFontSize(PlayerShipTextBox);
                    Debug.WriteLine("Ship not found, set to Unknown");
                }

                if (gameMode != GameMode.Unknown)
                {
                    GameModeTextBox.Text = gameMode == GameMode.PersistentUniverse ? "Player" : gameMode.ToString();
                    LocalPlayerData.CurrentGameMode = gameMode;
                    AdjustFontSize(GameModeTextBox);
                    Debug.WriteLine($"Set game mode in UI: {GameModeTextBox.Text}");
                }
                else
                {
                    GameModeTextBox.Text = "Unknown";
                    LocalPlayerData.CurrentGameMode = GameMode.Unknown;
                    AdjustFontSize(GameModeTextBox);
                    Debug.WriteLine("Game mode not found, set to Unknown");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading initial states: {ex.Message}");
        }
    }

    public void Cleanup()
    {
        // Stop and dispose the status check timer
        _statusCheckTimer?.Stop();
        _statusCheckTimer?.Dispose();

        // Stop the log handler if it's running
        _logHandler?.StopMonitoring();
    }

    private bool IsStarCitizenRunning()
    {
        return Process.GetProcessesByName("StarCitizen").Length > 0;
    }
}
