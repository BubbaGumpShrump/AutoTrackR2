# AutoTrackR2 - Star Citizen Kill-Tracking Tool

AutoTrackR2 is a powerful and customizable kill-tracking tool for Star Citizen. Designed with gankers and combat enthusiasts in mind, it integrates seamlessly with the game to log, display, and manage your kills, providing detailed information and optional API integration for advanced tracking.

## Testimonials

https://www.youtube.com/watch?v=bE6_MYY7ARU

## Features

- **Log File Integration**: Point to Star Citizen's live game.log to track kills in real-time.
- **API Integration (Optional)**:
  - Configure a desired API to send kill data for external tracking or display.
  - Secure your data with an optional API key.
- **Video Clipping (Optional)**:
  - Set a path to your clipping software to save kills automatically.
- **Visor Wipe Integration (Optional)**:
  - Automates visor wiping using an AutoHotkey script (`visorwipe.ahk`).
  - Requires AutoHotkey v2.
  - Script must be placed in `%LOCALAPPDATA%\AutoTrackR2\`.
- **Video Record Integration (Optional)**:
  - Customize the `videorecord.ahk` script for your specific video recording keybinds.
  - Requires AutoHotkey v2.
  - Script must be placed in `%LOCALAPPDATA%\AutoTrackR2\`
- **Offline Mode**:
  - Disables API submissions. The tool will still scrape and display information from the RobertsSpaceIndustries website for the profile of whomever you have killed.
- **Custom Themes**:
  - Easily add or modify themes by adjusting the `ThemeSlider_ValueChanged` function in `ConfigPage.xaml.cs`.
  - Update the ThemeSlider maximum value in `ConfigPage.xaml` to reflect the number of themes added.

## Setup

1. **Prerequisites**:

   - Windows 10 or later
   - .NET Framework 4.7.2 or higher
   - Star Citizen installed
   - AutoHotkey v2 (optional - only needed for visor wipe and video recording features):
     - Download from the official website at https://www.autohotkey.com/
     - Install AutoHotkey v2 (not v1.x as they are not compatible)
     - Verify installation by right-clicking on your desktop and confirming "New > AutoHotkey v2 Script" appears in the context menu

2. **Installation Steps**:

   - Download the latest release package from the releases page
   - Run the installer and follow the on-screen instructions
   - Launch AutoTrackR2 after installation completes

3. **Initial Configuration**:
   - Open the Configuration panel within the application
   - Set the path to your Star Citizen game.log file (typically found in `[Star Citizen Install Path]\LIVE\game.log`)
   - Configure API settings if desired (leave blank for offline mode)
   - Set up video clipping paths if using this feature
   - If using AutoHotkey features, verify the scripts are properly placed in the `%LOCALAPPDATA%\AutoTrackR2\` location

## Configuration

- **Log File**:
  - Specify the path to Star Citizen's `game.log`.
- **API Settings (Optional)**:
  - API URL: Provide the endpoint for posting kill data.
  - API Key: Secure access to the API with your unique key.
- **Video Clipping Path (Optional)**:
  - Set the directory where your clipping software saves kills.
- **Visor Wipe Setup (Optional)**:
  - Place `visorwipe.ahk` in `%LOCALAPPDATA%\AutoTrackR2\`.
  - AutoHotkey v2 is required.
- **Video Recording Setup (Optional)**:
  - Modify `videorecord.ahk` to use the keybinds of your video recording software.
  - Place `videorecord.ahk` in `%LOCALAPPDATA%\AutoTrackR2\`.
  - AutoHotkey v2 is required.
- **Offline Mode**:
  - Enable to disable API submission. Restart the tracker to apply changes.

## Privacy & Data Usage

- **No Personal Data Collection**:
  - AutoTrackR2 does not collect or store personal or system information, other than common file paths to manage necessary files.
- **Access and Permissions**:
  - The tool reads its own `config.ini` and the `game.log` from Star Citizen. It will also create a CSV file of all your logged kills, stored locally on your machine in the `%LOCALAPPDATA%\AutoTrackR2\` folder.
- **Optional Data Submission**:
  - Data is only sent to an API if explicitly configured by the user. Offline Mode disables all outgoing submissions.
- **Killfeed Scraping**:
  - The program scrapes the profile page of killed players to display their information locally. This feature remains active even in Offline Mode.

## Customization

To customize themes or behaviors:

- **Add Themes**:
  - Update the `ThemeSlider_ValueChanged` function in `ConfigPage.xaml.cs` with your desired colors and logos.
  - Adjust the ThemeSlider maximum value in `ConfigPage.xaml` to match the number of themes.
- **Modify AHK Scripts (Optional)**:
  - Edit `visorwipe.ahk` and `videorecord.ahk` to fit your specific keybinds and preferences.

## Support

For questions, issues, or feature requests:

- Join our Discord server: [discord.gg/griefernet](https://discord.gg/griefernet)
- Report bugs through the Discord's #autotrackr-bugs channel
- For urgent issues, you can escalate in Discord by tagging moderators

## License

AutoTrackR2 is released under the GNU v3 License.

GRIEFERNET VICTORY!
