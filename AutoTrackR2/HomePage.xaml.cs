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
using AutoTrackR2.Constants;
using System.Threading;

namespace AutoTrackR2;

public partial class HomePage : UserControl
{

    private LogHandler? _logHandler;
    private KillHistoryManager _killHistoryManager;
    private LogBackupProcessor? _logBackupProcessor;
    private bool _UIEventsRegistered = false;
    private System.Timers.Timer _statusCheckTimer;
    private bool _isLogHandlerRunning = false;
    private bool _isInitializing = false;
    private System.Timers.Timer? _initializationTimer;
    private bool _wasStarCitizenRunningOnStart = false;
    private bool _isProcessingLogBackups = false;

    public HomePage()
    {
        InitializeComponent();

        // Initialize default values
        LocalPlayerData.PlayerShip = "Player";

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
    }

    private bool IsLogHandlerProperlyConfigured()
    {
        return !string.IsNullOrEmpty(ConfigManager.LogFile) &&
               _logHandler != null &&
               !_logHandler.IsUsingPlaceholderFile;
    }

    private void CheckStarCitizenStatus(object? sender, ElapsedEventArgs e)
    {
        if (_isProcessingLogBackups)
        {
            // Simulate TrackR as running during log backup processing
            Dispatcher.Invoke(() => UpdateStatusIndicator(true));
            return;
        }
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
                else
                {
                    // Log handler is running, update status based on configuration
                    UpdateStatusIndicator(true);
                }
            }
            else
            {
                // Game is not running, set everything to Unknown
                GameModeTextBox.Text = "Unknown";
                PlayerShipTextBox.Text = "Player";
                PilotNameTextBox.Text = "Unknown";
                LocationTextBox.Text = "Unknown";
                LocalPlayerData.CurrentGameMode = GameMode.Unknown;
                LocalPlayerData.PlayerShip = "Player";
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
        // Check if log handler is using a placeholder file
        bool isUsingPlaceholder = _logHandler?.IsUsingPlaceholderFile == true;

        if (isInitializing)
        {
            StatusLight.Fill = new SolidColorBrush(Colors.Yellow);
            StatusText.Text = "TrackR\nInitializing";
        }
        else if (isRunning && !isUsingPlaceholder)
        {
            StatusLight.Fill = new SolidColorBrush(Colors.Green);
            StatusText.Text = "TrackR\nRunning";
        }
        else if (isRunning && isUsingPlaceholder)
        {
            StatusLight.Fill = new SolidColorBrush(Colors.Orange);
            StatusText.Text = "TrackR\nConfig\nRequired";
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

        // Vehicle Control
        TrackREventDispatcher.VehicleControlEvent += (data) =>
        {
            Dispatcher.Invoke(() =>
            {
                PlayerShipTextBox.Text = data.Ship ?? "Player";
                AdjustFontSize(PlayerShipTextBox);
                LocalPlayerData.PlayerShip = data.Ship ?? "Player";
            });
        };

        // Jump Drive State Changed (Location Only)
        TrackREventDispatcher.JumpDriveStateChangedEvent += (data) =>
        {
            Dispatcher.Invoke(() =>
            {
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
                        TrackRver = AppConstants.Version,
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

                    // Only enable real-time features if TrackR is fully initialized
                    if (RealTimeFeatureManager.ShouldEnableVisorWipe())
                    {
                        VisorWipe();
                    }

                    if (RealTimeFeatureManager.ShouldEnableVideoRecord())
                    {
                        VideoRecord(actorDeathData.VictimPilot, actorDeathData.VictimShip);
                    }

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

    public static void RunAHKScript(string? path, string? victimName = null, string? shipName = null)
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

        // Only do file handling for video recording
        if (victimName != null && shipName != null)
        {
            // Start the recording
            using var ahkProcess = new Process();
            ahkProcess.StartInfo.FileName = "explorer";
            ahkProcess.StartInfo.Arguments = "\"" + scriptPath + "\"";
            ahkProcess.Start();

            // Wait 7 seconds for the recording to complete
            System.Threading.Thread.Sleep(7000);

            // Rename the most recent file
            if (!string.IsNullOrEmpty(ConfigManager.VideoPath))
            {
                var videoDir = new DirectoryInfo(ConfigManager.VideoPath);
                var newestFile = videoDir.GetFiles()
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (newestFile != null)
                {
                    string newFileName = $"{victimName}.{shipName}.{DateTime.Now:yyyyMMdd_HHmmss}{newestFile.Extension}";
                    string newPath = Path.Combine(videoDir.FullName, newFileName);

                    try
                    {
                        File.Move(newestFile.FullName, newPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("File rename failed: " + ex.Message);
                    }
                }
            }
        }
        else
        {
            // For visor wipe, just run the script
            using var ahkProcess = new Process();
            ahkProcess.StartInfo.FileName = "explorer";
            ahkProcess.StartInfo.Arguments = "\"" + scriptPath + "\"";
            ahkProcess.Start();
        }
    }

    private void VisorWipe()
    {
        // Check if TrackR is ready before enabling visor wipe
        if (!RealTimeFeatureManager.ShouldEnableVisorWipe())
        {
            return;
        }

        if (ConfigManager.VisorWipe == 1)
        {
            RunAHKScript(ConfigManager.VisorWipeScript);
        }
    }

    private void VideoRecord(string victimName, string shipName)
    {
        // Check if TrackR is ready before enabling video recording
        if (!RealTimeFeatureManager.ShouldEnableVideoRecord())
        {
            return;
        }

        if (ConfigManager.VideoRecord == 1 && !string.IsNullOrEmpty(victimName) && !string.IsNullOrEmpty(shipName))
        {
            RunAHKScript(ConfigManager.VideoRecordScript, victimName, shipName);
        }
    }

    private void ShowLogFileNotConfiguredNotification()
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                "Log file path is not configured. Please go to the Config tab and set the correct Star Citizen log file path to enable tracking.",
                "Configuration Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        });
    }

    public void InitializeLogHandler()
    {
        // Check if LogFile is configured before proceeding
        if (string.IsNullOrEmpty(ConfigManager.LogFile))
        {
            // LogFile is not configured, show notification
            ShowLogFileNotConfiguredNotification();
            return;
        }

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

    private void PauseLogHandlerMonitoring()
    {
        if (_logHandler != null && _isLogHandlerRunning)
        {
            Console.WriteLine("Pausing LogHandler monitoring for log backup import process");
            _logHandler.StopMonitoring();
            _isLogHandlerRunning = false;
        }
    }

    private void ResumeLogHandlerMonitoring()
    {
        if (_logHandler != null && !_isLogHandlerRunning)
        {
            Console.WriteLine("Resuming LogHandler monitoring after log backup import completion");

            // Use InitializeForImport if Star Citizen isn't running, otherwise use normal Initialize
            if (IsStarCitizenRunning())
            {
                _logHandler.Initialize();
            }
            else
            {
                _logHandler.InitializeForImport();
            }

            _isLogHandlerRunning = true;
        }
    }

    public async void ProcessLogBackups_Click(object sender, RoutedEventArgs e)
    {
        if (_isProcessingLogBackups)
        {
            MessageBox.Show("Already processing log backups. Please wait.", "Processing", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Check if LogFile is configured before proceeding
        if (string.IsNullOrEmpty(ConfigManager.LogFile))
        {
            MessageBox.Show("Log file path is not configured. Please configure the log file path in settings first.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Store the current log handler state
        bool wasLogHandlerRunning = _isLogHandlerRunning;

        try
        {
            _isProcessingLogBackups = true;
            UpdateStatusIndicator(true, true); // Set to yellow for processing

            // Stop the log handler monitoring to prevent interference with import process
            PauseLogHandlerMonitoring();

            if (_logBackupProcessor == null)
            {
                var logBackupsPath = Path.Combine(Path.GetDirectoryName(ConfigManager.LogFile)!, "logbackups");
                var eventHandlers = _logHandler?.GetEventHandlers() ?? new List<ILogEventHandler>();
                Console.WriteLine($"Creating LogBackupProcessor with {eventHandlers.Count} event handlers");
                foreach (var handler in eventHandlers)
                {
                    Console.WriteLine($"  Handler: {handler.GetType().Name}");
                }
                _logBackupProcessor = new LogBackupProcessor(logBackupsPath, _killHistoryManager, eventHandlers);
            }

            var importStats = await _logBackupProcessor.ProcessLogBackupsAsync((logFile) =>
            {
                DebugPanel.Text = $"Processing: {Path.GetFileName(logFile)}";
            });

            var message = $"Log backups processed successfully!\n\n" +
                         $"=== IMPORT SUMMARY ===\n" +
                         $"Total kills found: {importStats.TotalKillsFound}\n" +
                         $"Total kills imported: {importStats.TotalKillsImported}\n" +
                         $"Kills not imported: {importStats.TotalKillsNotImported}\n" +
                         $"Import success rate: {importStats.ImportSuccessRate:F1}%";

            MessageBox.Show(message, "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            DebugPanel.Text = ""; // Clear the debug panel after successful processing
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing log backups: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isProcessingLogBackups = false;

            // Resume log handler monitoring if it was running before
            if (wasLogHandlerRunning)
            {
                ResumeLogHandlerMonitoring();
            }

            // Update status based on current Star Citizen state
            UpdateStatusIndicator(IsStarCitizenRunning());
        }
    }
}
