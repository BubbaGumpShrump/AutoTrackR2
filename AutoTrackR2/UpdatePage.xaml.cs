using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AutoTrackR2
{
    public partial class UpdatePage : UserControl
    {
        private string currentVersion = "v2.06-release";
        private string latestVersion;

        public UpdatePage()
        {
            InitializeComponent();
            CurrentVersionText.Text = currentVersion;
            CheckForUpdates();
        }

        private async void CheckForUpdates()
        {
            try
            {
                // Fetch the latest release info from GitHub
                latestVersion = await GetLatestVersionFromGitHub();

                // Update the Available Version field
                AvailableVersionText.Text = latestVersion;

                // Enable the Install button if a new version is available
                if (IsNewVersionAvailable(currentVersion, latestVersion))
                {
                    InstallButton.IsEnabled = true;
                    InstallButton.Style = (Style)FindResource("ButtonStyle");
                }
            }
            catch (Exception ex)
            {
                AvailableVersionText.Text = "Error checking updates.";
                MessageBox.Show($"Failed to check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<string> GetLatestVersionFromGitHub()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "AutoTrackR2");

            string repoOwner = "BubbaGumpShrump";
            string repoName = "AutoTrackR2";

            try
            {
                // Attempt to fetch the latest release
                var url = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases/latest";
                var response = await client.GetStringAsync(url);

                // Parse the JSON using System.Text.Json
                using var document = System.Text.Json.JsonDocument.Parse(response);
                var root = document.RootElement;
                var tagName = root.GetProperty("tag_name").GetString();

                return tagName;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Fallback to releases list if 'latest' not found
                var url = $"https://api.github.com/repos/{repoOwner}/{repoName}/releases";
                var response = await client.GetStringAsync(url);

                using var document = System.Text.Json.JsonDocument.Parse(response);
                var root = document.RootElement;

                // Get the tag name of the first release
                if (root.GetArrayLength() > 0)
                {
                    var firstRelease = root[0];
                    return firstRelease.GetProperty("tag_name").GetString();
                }

                throw new Exception("No releases found.");
            }
        }

        private bool IsNewVersionAvailable(string currentVersion, string latestVersion)
        {
            // Compare version strings (you can implement more complex version parsing logic if needed)
            return string.Compare(currentVersion, latestVersion, StringComparison.Ordinal) < 0;
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InstallButton.IsEnabled = false;
                InstallButton.Content = "Preparing to Update...";

                // Get the path to the update.ps1 script
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.ps1");

                // Run the PowerShell script
                RunPowerShellScript(scriptPath);

                // Gracefully close the app after running the script
                Application.Current.Shutdown();

                MessageBox.Show("Update process has started. Please follow the instructions in the PowerShell script.", "Update Started", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to run the update script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                InstallButton.IsEnabled = true;
                InstallButton.Content = "Install Update";
            }
        }

        private void RunPowerShellScript(string scriptPath)
        {
            try
            {
                // Prepare the command to run the PowerShell script with elevation (admin rights)
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"", // Allow script to run
                    Verb = "runas", // Request elevation (admin rights)
                    UseShellExecute = true, // Use the shell to execute the process
                    CreateNoWindow = false    // Show the PowerShell window
                };

                // Start the PowerShell process to run the script with admin rights
                System.Diagnostics.Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to run the PowerShell script with admin rights: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
