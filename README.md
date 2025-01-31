AutoTrackR2 - Star Citizen Kill-Tracking Tool
AutoTrackR2 is a powerful and customizable kill-tracking tool for Star Citizen. Designed with gankers and combat enthusiasts in mind, it integrates seamlessly with the game to log, display, and manage your kills, providing detailed information and optional API integration for advanced tracking.

🚀 Features
Log File Integration: Point to Star Citizen's live game.log to track kills, death and other way's to die, in real-time.

API Integration (Optional):
Configure a desired API to send kill data for external tracking or display.
Secure your data with an optional API key.

Video Clipping (Optional):
Set a path to your clipping software to save log events except suicides automatically.

Visor Wipe Integration:
Automates visor wiping using an AutoHotkey script (visorwipe.ahk).
Requires AutoHotkey v2.
Script must be placed in C:\Users\<Username>\AppData\Local\AutoTrackR2\.

Video Record Integration:
Customize the videorecord.ahk script for your specific video recording keybinds.
Requires AutoHotkey v2.
Script must be placed in C:\Users\<Username>\AppData\Local\AutoTrackR2\

Offline Mode:
Disables API submissions. The tool will still scrape and display information from the RobertsSpaceIndustries website for the profile of whomever you have killed.

Custom Themes:
Easily add or modify themes by adjusting the ThemeSlider_ValueChanged function in ConfigPage.xaml.cs.
Update the ThemeSlider maximum value in ConfigPage.xaml to reflect the number of themes added.

📁 Configuration
Log File:
Specify the path to Star Citizen's game.log.

API Settings (Optional):

API URL: Provide the endpoint for posting kill data.

API Key: Secure access to the API with your unique key.

Video Clipping Path (Optional):
Set the directory where your clipping software saves kills.

Visor Wipe Setup:
Place visorwipe.ahk in C:\Users\<Username>\AppData\Local\AutoTrackR2\.
AutoHotkey v2 is required.

Video Recording Setup:
Modify videorecord.ahk to use the keybinds of your video recording software.
Place videorecord.ahk in C:\Users\<Username>\AppData\Local\AutoTrackR2\.
AutoHotkey v2 is required.

Offline Mode:
Enable to disable API submission. Restart the tracker to apply changes.

KillLog:
Enable to disable the log of a Kill event.
There will also no wipe or Video Recording if disabled.

DeathLog:
Enable to disable the log of a Death event.
There will also no wipe or Video Recording if disabled.


OtherLog:
Enable to disable the log of a Oter way to die event.
There will also no wipe or Video Recording if disabled.
Suicide event will never be Recorded.


🛡️ Privacy & Data Usage
No Personal Data Collection:
AutoTrackR2 does not collect or store personal or system information, other than common file paths to manage necessary files.

Access and Permissions:
The tool reads its own config.ini and the game.log from Star Citizen. It will also create a CSV file of all your logged kills, stored locally on your machine in the AppData folder.

Optional Data Submission:
Data is only sent to an API if explicitly configured by the user. Offline Mode disables all outgoing submissions.

Killfeed Scraping:
The program scrapes the profile page of killed players to display their information locally. This feature remains active even in Offline Mode.

⚙️ Installation
Download the latest release from the releases page.
Follow the setup instructions included in the installer.
Configure the tool using the settings outlined above.

❗Update
Log-Files 2.06-stable and older will be automatically backup into _backup_Kill-log.csv and converted.

💡 Customization
To customize themes or behaviors:

Add Themes:
Update the ThemeSlider_ValueChanged function in ConfigPage.xaml.cs with your desired colors and logos.
Adjust the ThemeSlider maximum value in ConfigPage.xaml to match the number of themes.

Modify AHK Scripts:
Edit visorwipe.ahk and videorecord.ahk to fit your specific keybinds and preferences.

📞 Support
For questions, issues, or feature requests, please visit discord.gg/griefernet.

🔒 License
AutoTrackR2 is released under the GNU v3 License.

GRIEFERNET VICTORY!
