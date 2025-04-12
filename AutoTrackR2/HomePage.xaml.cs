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
    private int _counter = 1;
    private System.Timers.Timer _counterTimer;
    private bool _isInitializing = false;
    private System.Timers.Timer? _initializationTimer;
    private bool _wasStarCitizenRunningOnStart = false;

    public HomePage()
    {
        InitializeComponent();

        if (string.IsNullOrEmpty(ConfigManager.KillHistoryFile))
        {
            throw new InvalidOperationException("KillHistoryFile path is not configured.");
        }
        _killHistoryManager = new KillHistoryManager(ConfigManager.KillHistoryFile, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds"));

        // Set the TextBlock text
        KillTallyTitle.Text = $"Kill Tally - {DateTime.Now.ToString("MMMM")}";
        KillTallyTextBox.Text = _killHistoryManager.GetKillsInCurrentMonth().Count.ToString();
        AdjustFontSize(KillTallyTextBox);
        AddKillHistoryKillsToUI();

        // Check if Star Citizen is already running
        _wasStarCitizenRunningOnStart = IsStarCitizenRunning();
        if (_wasStarCitizenRunningOnStart)
        {
            UpdateStatusIndicator(true);
            InitializeLogHandler();
        }
        else
        {
            UpdateStatusIndicator(false);
        }

        // Initialize and start the status check timer
        _statusCheckTimer = new System.Timers.Timer(1000); // Check every second
        _statusCheckTimer.Elapsed += CheckStarCitizenStatus;
        _statusCheckTimer.Start();

        // Initialize and start the counter timer
        _counterTimer = new System.Timers.Timer(1000); // Update every second
        _counterTimer.Elapsed += UpdateCounter;
        _counterTimer.Start();
    }

    private void CheckStarCitizenStatus(object? sender, ElapsedEventArgs e)
    {
        bool isRunning = IsStarCitizenRunning();
        Dispatcher.Invoke(() =>
        {
            if (_isInitializing)
            {
                return; // Don't update status while initializing
            }

            if (isRunning)
            {
                if (!_isLogHandlerRunning)
                {
                    if (_wasStarCitizenRunningOnStart)
                    {
                        // Game was already running on start, initialize immediately
                        UpdateStatusIndicator(true);
                        InitializeLogHandler();
                    }
                    else
                    {
                        // Game started after app launch, use initialization delay
                        _isInitializing = true;
                        UpdateStatusIndicator(false, true); // Set to yellow for initialization

                        _initializationTimer = new System.Timers.Timer(20000); // 20 seconds
                        _initializationTimer.Elapsed += (sender, e) =>
                        {
                            _isInitializing = false;
                            _initializationTimer?.Stop();
                            _initializationTimer?.Dispose();
                            _initializationTimer = null;

                            Dispatcher.Invoke(() =>
                            {
                                // Game is running, start log monitoring and read initial states
                                UpdateStatusIndicator(true);
                                InitializeLogHandler();
                            });
                        };
                        _initializationTimer.Start();
                    }
                }
            }
            else
            {
                // Game is not running, set everything to Unknown
                GameModeTextBox.Text = "Unknown";
                PlayerShipTextBox.Text = "Unknown";
                PilotNameTextBox.Text = "Unknown";
                LocationTextBox.Text = "Unknown";
                LocalPlayerData.CurrentGameMode = GameMode.Unknown;
                LocalPlayerData.PlayerShip = string.Empty;
                LocalPlayerData.Username = string.Empty;
                LocalPlayerData.LastSeenVehicleLocation = "Unknown";

                // Stop log monitoring if it's running
                if (_isLogHandlerRunning)
                {
                    _logHandler?.StopMonitoring();
                    _isLogHandlerRunning = false;
                }

                UpdateStatusIndicator(false);
            }
        });
    }

    private void UpdateStatusIndicator(bool isRunning, bool isInitializing = false)
    {
        if (isInitializing)
        {
            StatusLight.Fill = new SolidColorBrush(Colors.Yellow);
            StatusText.Text = "TrackR\nInitializing";
        }
        else if (isRunning)
        {
            StatusLight.Fill = new SolidColorBrush(Colors.Green);
            StatusText.Text = "TrackR\nRunning";
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
        TrackREventDispatcher.JumpDriveStateChangedEvent += (data) =>
        {
            Dispatcher.Invoke(() =>
            {
                PlayerShipTextBox.Text = data.ShipName;
                AdjustFontSize(PlayerShipTextBox);
                LocalPlayerData.PlayerShip = data.ShipName;
                LocalPlayerData.LastSeenVehicleLocation = data.Location;
                LocationTextBox.Text = data.Location;
                AdjustFontSize(LocationTextBox);
            });
        };

        // Game Mode
        TrackREventDispatcher.PlayerChangedGameModeEvent += (mode) =>
        {
            Dispatcher.Invoke(() =>
            {
                GameModeTextBox.Text = mode == GameMode.PersistentUniverse ? mode.ToString() : GameMode.Unknown.ToString();
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
                        Location = LocalPlayerData.LastSeenVehicleLocation,
                        OrgAffiliation = playerData?.OrgName,
                        Weapon = actorDeathData.Weapon,
                        Ship = LocalPlayerData.PlayerShip ?? "Unknown",
                        Method = actorDeathData.DamageType,
                        RecordNumber = playerData?.UEERecord,
                        GameVersion = LocalPlayerData.GameVersion ?? "Unknown",
                        TrackRver = "2.10",
                        Enlisted = playerData?.JoinDate,
                        KillTime = ((DateTimeOffset)DateTime.ParseExact(actorDeathData.Timestamp, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)).ToUnixTimeSeconds().ToString(),
                        PFP = playerData?.PFPURL ?? "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg",
                        Hash = WebHandler.GenerateKillHash(actorDeathData.VictimPilot, ((DateTimeOffset)DateTime.ParseExact(actorDeathData.Timestamp, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)).ToUnixTimeSeconds())
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

                    // Check if this is a duplicate kill
                    if (WebHandler.IsDuplicateKill(killData.Hash))
                    {
                        Console.WriteLine("Duplicate kill detected, skipping...");
                        return;
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

                    // Update kill tally
                    Dispatcher.Invoke(() =>
                    {
                        KillTallyTextBox.Text = _killHistoryManager.GetKillsInCurrentMonth().Count.ToString();
                        AdjustFontSize(KillTallyTextBox);
                    });
                }
            }
            else
            {
                // Player died, reset kill streak
                _killHistoryManager.ResetKillStreak();
            }
        };

        // Vehicle Destruction
        TrackREventDispatcher.VehicleDestructionEvent += (data) =>
        {
            Dispatcher.Invoke(() =>
            {
                LocalPlayerData.LastSeenVehicleLocation = data.VehicleZone;
                LocationTextBox.Text = data.VehicleZone;
                AdjustFontSize(LocationTextBox);
            });
        };

        _UIEventsRegistered = true;
    }

    private void AddKillToScreen(KillData killData)
    {
        // Use resource references instead of creating new brushes
        var killTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 10, 0, 10),
            Style = (Style)Application.Current.Resources["RoundedTextBlock"],
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            FontFamily = (FontFamily)Application.Current.Resources["Gemunu"],
        };

        // Add styled content using Run elements with resource references
        var titleRun = new Run("Victim Name: ");
        titleRun.SetResourceReference(TextElement.ForegroundProperty, "AltTextBrush");
        titleRun.FontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
        killTextBlock.Inlines.Add(titleRun);
        killTextBlock.Inlines.Add(new Run($"{killData.EnemyPilot}\n"));

        titleRun = new Run("Victim Ship: ");
        titleRun.SetResourceReference(TextElement.ForegroundProperty, "AltTextBrush");
        titleRun.FontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
        killTextBlock.Inlines.Add(titleRun);
        killTextBlock.Inlines.Add(new Run($"{killData.EnemyShip}\n"));

        titleRun = new Run("Victim Org: ");
        titleRun.SetResourceReference(TextElement.ForegroundProperty, "AltTextBrush");
        titleRun.FontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
        killTextBlock.Inlines.Add(titleRun);
        killTextBlock.Inlines.Add(new Run($"{killData.OrgAffiliation}\n"));

        titleRun = new Run("Join Date: ");
        titleRun.SetResourceReference(TextElement.ForegroundProperty, "AltTextBrush");
        titleRun.FontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
        killTextBlock.Inlines.Add(titleRun);
        killTextBlock.Inlines.Add(new Run($"{killData.Enlisted}\n"));

        titleRun = new Run("UEE Record: ");
        titleRun.SetResourceReference(TextElement.ForegroundProperty, "AltTextBrush");
        titleRun.FontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
        killTextBlock.Inlines.Add(titleRun);
        killTextBlock.Inlines.Add(new Run($"{killData.RecordNumber}\n"));

        titleRun = new Run("Kill Time: ");
        titleRun.SetResourceReference(TextElement.ForegroundProperty, "AltTextBrush");
        titleRun.FontFamily = (FontFamily)Application.Current.Resources["Orbitron"];
        killTextBlock.Inlines.Add(titleRun);

        string displayTime;
        if (long.TryParse(killData.KillTime, out long unixTime))
        {
            displayTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToString("dd MMM yyyy HH:mm");
        }
        else
        {
            displayTime = killData.KillTime ?? "Unknown";
        }
        killTextBlock.Inlines.Add(new Run(displayTime));

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
            Source = new BitmapImage(new Uri(killData.PFP ?? "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg")),
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

    public static void RunAHKScript(string? path)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(ConfigManager.AHKScriptFolder))
        {
            return;
        }

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
            RegisterUIEventHandlers();
            _logHandler = new LogHandler(ConfigManager.LogFile);
            _logHandler.Initialize();
            _isLogHandlerRunning = true;
        }
        else if (!_isLogHandlerRunning)
        {
            _logHandler.Initialize();
            _isLogHandlerRunning = true;
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

    private void UpdateCounter(object? sender, ElapsedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            DebugPanel.Text = _counter.ToString();
            _counter = (_counter % 10) + 1; // Count from 1 to 10 and loop
        });
    }
}
