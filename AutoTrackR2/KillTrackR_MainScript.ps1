# Path to the config file
$scriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Path
$configFile = Join-Path -Path $scriptFolder -ChildPath "config.ini"

# Read the config file into a hashtable
if (Test-Path $configFile) {
    Write-Output "Config.ini found."
    $configContent = Get-Content $configFile | Where-Object { $_ -notmatch '^#|^\s*$' }

    # Escape backslashes by doubling them
    $configContent = $configContent -replace '\\', '\\\\'

    # Convert to key-value pairs
    $config = $configContent -replace '^([^=]+)=(.+)$', '$1=$2' | ConvertFrom-StringData
} else {
    Write-Output "Config.ini not found."
    exit
}

$PlayerName = "Immersion_Breaker"
$PlayerShip = "ANVL_F7A_Mk2"
$GameMode = "PU"

Write-Output "PlayerName=$PlayerName"
Write-Output "PlayerShip=$PlayerShip"
Write-Output "GameMode=$GameMode"

# Access config values
$logFile = $config.Logfile
$apiUrl = $config.ApiUrl
$apiKey = $config.ApiKey
$videoPath = $config.VideoPath
$visorWipe = $config.VisorWipe
$videoRecord = $config.VideoRecord
$offlineMode = $config.OfflineMode

if ($offlineMode -eq 1){
	$offlineMode = $true
} else {
	$offlineMode = $false
}

if ($videoRecord -eq 1){
	$videoRecord = $true
} else {
	$videoRecord = $false
}

$logfileContent = Get-Content $logFile

If (Test-Path $logFile) {
	Write-Output "Logfile found."
} else {
	Write-Output "Logfile not found."
}

Write-Output $logfileContent
<# Define the regex pattern to extract information
$killPattern = "<Actor Death> CActor::Kill: '(?<EnemyPilot>[^']+)' \[\d+\] in zone '(?<EnemyShip>[^']+)' killed by '(?<Player>[^']+)' \[[^']+\] using '(?<Weapon>[^']+)' \[Class (?<Class>[^\]]+)\] with damage type '(?<DamageType>[^']+)'"
$puPattern = '<\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z> \[Notice\] <ContextEstablisherTaskFinished> establisher="CReplicationModel" message="CET completed" taskname="StopLoadingScreen" state=[^ ]+ status="Finished" runningTime=\d+\.\d+ numRuns=\d+ map="megamap" gamerules="SC_Default" sessionId="[a-f0-9\-]+" \[Team_Network\]\[Network\]\[Replication\]\[Loading\]\[Persistence\]'
$acPattern = "ArenaCommanderFeature"
$loadoutPattern = '\[InstancedInterior\] OnEntityLeaveZone - InstancedInterior \[(?<InstancedInterior>[^\]]+)\] \[\d+\] -> Entity \[(?<Entity>[^\]]+)\] \[\d+\] -- m_openDoors\[\d+\], m_managerGEID\[(?<ManagerGEID>\d+)\], m_ownerGEID\[(?<OwnerGEID>[^\[]+)\]'
# $loginPattern = "\[Notice\] <AccountLoginCharacterStatus_Character> Character: createdAt [A-Za-z0-9]+ - updatedAt [A-Za-z0-9]+ - geid [A-Za-z0-9]+ - accountId [A-Za-z0-9]+ - name (?<Player>[A-Za-z0-9_-]+) - state STATE_CURRENT" # KEEP THIS INCASE LEGACY LOGIN IS REMOVED 
$loginPattern = "\[Notice\] <Legacy login response> \[CIG-net\] User Login Success - Handle\[(?<Player>[A-Za-z0-9_-]+)\]"
$cleanupPattern = '^(.+?)_\d+$'

# Lookup Patterns
$joinDatePattern = '<span class="label">Enlisted</span>\s*<strong class="value">([^<]+)</strong>'
$orgPattern = '<IMG[^>]*>\s*([^<]+)'
$ueePattern = '<p class="entry citizen-record">\s*<span class="label">UEE Citizen Record<\/span>\s*<strong class="value">#?(n\/a|\d+)<\/strong>\s*<\/p>'

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Function to process new log entries and write to the host
function Read-LogEntry {
    param (
        [string]$line
    )
    
    # Apply the regex pattern to the line
    if ($line -match $killPattern -and $global:logStart -eq $TRUE) {
        # Access the named capture groups from the regex match
        $enemyPilot = $matches['EnemyPilot']
        $enemyShip = $matches['EnemyShip']
        $player = $matches['Player']
	$weapon = $matches['Weapon']
	$damageType = $matches['DamageType']
	$ship = $global:loadOut
	
	If ($enemyShip -eq $global:lastKill){
		$enemyShip = "Passenger"
	} Else {
		$global:lastKill = $enemyShip
	}

	If (($player -eq $global:userName -and $enemyPilot -ne $global:userName) -and ($enemyPilot -notlike "PU_*" -and $enemyPilot -notlike "NPC_*")){
		If ($enemyShip -match $cleanupPattern){
			$enemyShip = $matches[1]
		}
		If ($weapon -match $cleanupPattern){
			$weapon = $matches[1]
		}
		If ($weapon -eq "KLWE_MassDriver_S10"){
			$global:loadOut = "AEGS_Idris"
			$ship = "AEGS_Idris"
		}
		if ($damageType -like "*bullet*") {
			$ship = "Player"
		}
		If ($ship -match $cleanupPattern){
			$ship = $matches[1]
		}
		if ($ship -like "OOC_*"){
			$ship = "Player"
		}
		If ($enemyShip -like "OOC_*" -or $enemyShip -like "hangar*") {
			$enemyShip = "Player"
		}
			
			# Repeatedly remove all suffixes
			while ($enemyShip -match '_(PU|AI|CIV|MIL|PIR)$') {
				$enemyShip = $enemyShip -replace '_(PU|AI|CIV|MIL|PIR)$', ''
			}
			# Repeatedly remove all suffixes
			while ($ship -match '_(PU|AI|CIV|MIL|PIR)$') {
				$ship = $ship -replace '_(PU|AI|CIV|MIL|PIR)$', ''
			}

			$KillTime = Get-Date([DateTime]::UtcNow) -UFormat "%d%b%Y %r"
			$page1 = Invoke-WebRequest -uri "https://robertsspaceindustries.com/citizens/$enemyPilot"
			$page2 = Invoke-WebRequest -uri "https://robertsspaceindustries.com/citizens/$enemyPilot/organizations"
			
			# Get Enlisted Date
			if ($($page1.content) -match $joinDatePattern) {
				$joinDate = $matches[1]
			} else {
				$joinDate = "UNKNOWN"
			}

			# Find Org matches using the regex pattern
			$orgMatches = [regex]::matches($($page2.links.innerhtml), $orgPattern)
			# Check if there are any matches
			$enemyOrgs = @()
			if ($orgMatches.Count -eq 0) {
				$enemyOrgs = "N/A"
			} else {
				# Loop through each match and display the organization name
				foreach ($match in $orgMatches) {
					$organizationName = $match.Groups[1].Value.Trim()
					$enemyOrgs = $enemyOrgs + $organizationName
				}
			}

			# Get UEE Number
			if ($($page1.content) -match $ueePattern) {
				# The matched UEE Citizen Record number is in $matches[1]
				$citizenRecord = $matches[1]
			} else {
				$citizenRecord = "-1"
			}
			If ($citizenRecord -eq "N/A") {
				$citizenRecordAPI = "-1"
			} Else {
				$citizenRecordAPI = $citizenRecord
			}

			# Cleanup Output
			$textColor = "Green"
			$killText = "Congratulations!"

			# Send to API
			# Define the data to send
			If ($apiDetected -eq $true -and $urlDetected -eq $true){
				$data = @{
					victim_ship = $enemyShip
					victim = $enemyPilot
					enlisted = $joinDate
					rsi = $citizenRecordAPI
					weapon = $weapon
					method = $damageType
					loadout_ship = $ship
					#version = $version
				}

				# Headers which may or may not be necessary
				$headers = @{
					"Authorization" = "Bearer $apiToken"
					"Content-Type" = "application/json"
					"User-Agent" = "TEST-PS"
				}

				try {
					# Send the POST request with JSON data
					Invoke-RestMethod -Uri $apiURL -Method Post -Body ($data | ConvertTo-Json -Depth 5) -Headers $headers
				} catch {
					# Catch and display errors
					Write-Output "Error: $($_)" -ForegroundColor red
					Write-output "Send error to devs"
					Write-Output "Kill saved in $outputPath"
					$apiError = $_
					# Add to output file
					Add-Content -Path $outputPath -value $($_)
				}
			}

			# Write-Output to console
			write-host "=== $killText" -ForegroundColor $textColor
			Write-Host "$killTime"
			Write-Host "Enemy Pilot: " -NoNewLine -ForegroundColor $textColor
			Write-Host "$enemyPilot"
			Write-Host "Enemy Ship: " -NoNewLine -ForegroundColor $textColor
			Write-Host "$enemyShip"
			Write-Host "Enlisted: " -NoNewLine -ForegroundColor $textColor
			Write-Host "$joinDate"
			Write-Host "Record #: " -NoNewLine -ForegroundColor $textColor
			Write-Host "$citizenRecord"
			Write-Host "Org Affiliation: " -NoNewLine -ForegroundColor $textColor
			ForEach ($org in $enemyOrgs){
				Write-Host $org
			}
			Write-Host "Player: " -NoNewLine -ForegroundColor $textColor
			Write-Host "$player"
			Write-Host "Weapon: " -NoNewLine -ForegroundColor $textColor
			Write-Host "$weapon"
			Write-Host "Ship: " -NoNewLine -ForegroundColor $textColor
			Write-Host "$ship"s
			Write-Host "Method: " -NoNewLine -ForegroundColor $textColor
			Write-Host "$damageType"
			Write-Host "-------------------------"	
			
			# Write output to local log
			If ($apiDetected -eq $false -or $urlDetected -eq $false -or $null -ne $apiError){
				Add-Content -Path $outputPath -Value "=== $killText"
				Add-Content -Path $outputPath -Value "$killTime"
				Add-Content -Path $outputPath -Value "Enemy Pilot: $enemyPilot"
				Add-Content -Path $outputPath -Value "Enemy Ship: $enemyShip"
				Add-Content -Path $outputPath -Value "Enlisted: $joinDate"
				Add-Content -Path $outputPath -Value "Record #: $citizenRecord"
				Add-Content -Path $outputPath -Value "Org Affiliation: " -NoNewLine
				ForEach ($org in $enemyOrgs){
					Add-Content -Path $outputPath -Value $org
				}
				Add-Content -Path $outputPath -Value "Player: $player"
				Add-Content -Path $outputPath -Value "Weapon: $weapon"
				Add-Content -Path $outputPath -Value "Ship: $ship"
				Add-Content -Path $outputPath -Value "Method: $damageType"
				Add-Content -Path $outputPath -Value "-------------------------"
			}

			$sleeptimer = 10

			# VisorWipe
			If ($visorwipe -eq $true -and $enemyShip -ne "Passenger" -and $damageType -notlike "*Bullet*"){
				# send keybind for visorwipe
				start-sleep 1
				$sleeptimer = $sleeptimer -1
				&$visorPath
			}
			
			# Record video
			if ($recording -eq $true -and $enemyShip -ne "Passenger"){
				# send keybind for windows game bar recording
				Start-Sleep 2
				$sleeptimer = $sleeptimer -9
				&$recordPath
				Write-Host "=== Kill Clipped!" -ForegroundColor Green
				Write-Host "-------------------------"
				Start-Sleep 7

				# 
				$latestFile = Get-ChildItem -Path $videoPath | Where-Object { -not $_.PSIsContainer } | Sort-Object CreationTime -Descending | Select-Object -First 1
				# Generate a timestamp in ddMMMyyyy-HH:mm format
				$timestamp = (Get-Date).ToString("ddMMMyyyy-HHmm")
				# Rename the file if it exists
				if ($latestFile) {
					Rename-Item -Path $latestFile.FullName -NewName "$enemyPilot.$enemyShip.$timestamp.mp4"
				}
			}
			Start-Sleep $sleeptimer
		}
	}

	# Get Logged-in User
	If ($line -match $loginPattern) {
		# Load gamelog into memory
		$authLog = Get-Content -Path $logFilePath
		$authLog = $authlog -match $loginPattern
		$authLog = $authLog | Out-String

		# Extract User Name
		$nameExtract = "name\s+(?<PlayerName>[^\s-]+)"

		If ($authLog -match $nameExtract -and $global:userName -ne $nameExtract){
			$global:userName = $matches['PlayerName']
			Write-Host "Logged in as $global:userName" -ForegroundColor Green
			Write-Host "-------------------------"
		}
	}

	# Detect PU or AC
	if ($line -match $puPattern -and $global:logStart -eq $FALSE) {
		$global:logStart = $TRUE
		Write-Host "=== Logging: $global:logStart" -ForegroundColor Green
		Write-Host "-------------------------"
	}
	if ($line -match $acPattern -and $global:logStart -eq $TRUE) {
		$global:logStart = $FALSE
		Write-Host "=== Logging: $global:logStart" -ForegroundColor Red
		Write-Host "-------------------------"
	}

	#Set loadout 
	if ($line -match $loadoutPattern) {
		$entity = $matches['Entity']
		$ownerGEID = $matches['OwnerGEID']

        If ($ownerGEID -eq $global:userName -and $entity -notlike "*SoundListener*" -and $entity -notlike "*StreamingSOC*" -and $entity -ne $global:userName) {
			$global:loadOut = $entity
			If ($global:loadOut -match $cleanupPattern){
				$global:loadOut = $matches[1]
			}
			Write-Host "=== Loadout: $global:loadOut" -ForegroundColor Yellow
			Write-Host "-------------------------"
			
		}
	}
}

# Monitor the log file and process new lines as they are added
Get-Content -Path $logFile -Wait -Tail 0 | ForEach-Object {
    Read-LogEntry $_
}
#>