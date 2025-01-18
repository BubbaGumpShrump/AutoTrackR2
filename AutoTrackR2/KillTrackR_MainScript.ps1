$TrackRver = "2.06-koda-mod"

# Path to the config file
$appName = "AutoTrackR2"
$scriptFolder = Join-Path -Path $env:LOCALAPPDATA -ChildPath $appName
$configFile = Join-Path -Path $scriptFolder -ChildPath "config.ini"

# Read the config file into a hashtable
if (Test-Path $configFile) {
    Write-Output "PlayerName=Config.ini found."
    $configContent = Get-Content $configFile | Where-Object { $_ -notmatch '^#|^\s*$' }

    # Escape backslashes by doubling them
    $configContent = $configContent -replace '\\', '\\\\'

    # Convert to key-value pairs
    $config = $configContent -replace '^([^=]+)=(.+)$', '$1=$2' | ConvertFrom-StringData
} else {
    Write-Output "Config.ini not found."
    exit
}

$parentApp = (Get-Process -Name AutoTrackR2).ID

# Access config values
$logFilePath = $config.Logfile
$apiUrl = $config.ApiUrl
$apiKey = $config.ApiKey
$videoPath = $config.VideoPath
$visorWipe = $config.VisorWipe
$videoRecord = $config.VideoRecord
$offlineMode = $config.OfflineMode
$killLog = $config.KillLog
$deathLog = $config.DeathLog
$otherLog = $config.OtherLog

if ($offlineMode -eq 1){
	$offlineMode = $true
} else {
	$offlineMode = $false
}
Write-Output "PlayerName=OfflineMode: $offlineMode"

if ($videoRecord -eq 1){
	$videoRecord = $true
} else {
	$videoRecord = $false
}
Write-Output "PlayerName=VideoRecord: $videoRecord"

if ($visorWipe -eq 1){
	$visorWipe = $true
} else {
	$visorWipe = $false
}
Write-Output "PlayerName=VisorWipe: $visorWipe"

If (Test-Path $logFilePath) {
	Write-Output "PlayerName=Logfile found"
} else {
	Write-Output "Logfile not found."
}
If ($null -ne $apiUrl){
	if ($apiUrl -notlike "*/register-kill") {
		if ($apiUrl -like "*/"){
			$apiUrl = $apiUrl + "register-kill"
		}
		if ($apiUrl -notlike "*/"){
			$apiUrl = $apiUrl + "/register-kill"
		}
	}
Write-output "PlayerName=$apiURL"
}

if ($killLog -eq 1){
    $killLog = $true
} else {
    $killLog = $false
}
Write-Output "PlayerName=KillLog: $killLog"

if ($deathLog -eq 1){
    $deathLog = $true
} else {
    $deathLog = $false
}
Write-Output "PlayerName=DeathLog: $deathLog"

if ($otherLog -eq 1){
    $otherLog = $true
} else {
    $otherLog = $false
}
Write-Output "PlayerName=OtherLog: $otherLog"

# Ship Manufacturers
$prefixes = @(
    "ORIG",
    "CRUS",
    "RSI",
    "AEGS",
    "VNCL",
    "DRAK",
    "ANVL",
    "BANU",
    "MISC",
    "CNOU",
    "XIAN",
    "GAMA",
    "TMBL",
    "ESPR",
    "KRIG",
    "GRIN",
    "XNAA",
	"MRAI"
)

# Define the regex pattern to extract information
$killPattern = "<Actor Death> CActor::Kill: '(?<VictimPilot>[^']+)' \[\d+\] in zone '(?<VictimShip>[^']+)' killed by '(?<AgressorPilot>[^']+)' \[[^']+\] using '(?<Weapon>[^']+)' \[Class (?<Class>[^\]]+)\] with damage type '(?<DamageType>[^']+)'"
$puPattern = '<\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z> \[Notice\] <ContextEstablisherTaskFinished> establisher="CReplicationModel" message="CET completed" taskname="StopLoadingScreen" state=[^ ]+ status="Finished" runningTime=\d+\.\d+ numRuns=\d+ map="megamap" gamerules="SC_Default" sessionId="[a-f0-9\-]+" \[Team_Network\]\[Network\]\[Replication\]\[Loading\]\[Persistence\]'
$acPattern = "ArenaCommanderFeature"
$loadoutPattern = '\[InstancedInterior\] OnEntityLeaveZone - InstancedInterior \[(?<InstancedInterior>[^\]]+)\] \[\d+\] -> Entity \[(?<Entity>[^\]]+)\] \[\d+\] -- m_openDoors\[\d+\], m_managerGEID\[(?<ManagerGEID>\d+)\], m_ownerGEID\[(?<OwnerGEID>[^\[]+)\]'
$shipManPattern = "^(" + ($prefixes -join "|") + ")"
# $loginPattern = "\[Notice\] <AccountLoginCharacterStatus_Character> Character: createdAt [A-Za-z0-9]+ - updatedAt [A-Za-z0-9]+ - geid [A-Za-z0-9]+ - accountId [A-Za-z0-9]+ - name (?<Player>[A-Za-z0-9_-]+) - state STATE_CURRENT" # KEEP THIS INCASE LEGACY LOGIN IS REMOVED 
$loginPattern = "\[Notice\] <Legacy login response> \[CIG-net\] User Login Success - Handle\[(?<Player>[A-Za-z0-9_-]+)\]"
$cleanupPattern = '^(.+?)_\d+$'
$versionPattern = "--system-trace-env-id='pub-sc-alpha-(?<gameversion>\d{3,4}-\d{7})'"
$vehiclePattern = "<(?<timestamp>[^>]+)> \[Notice\] <Vehicle Destruction> CVehicle::OnAdvanceDestroyLevel: " +
    "Vehicle '(?<vehicle>[^']+)' \[\d+\] in zone '(?<vehicle_zone>[^']+)' " +
    "\[pos x: (?<pos_x>[-\d\.]+), y: (?<pos_y>[-\d\.]+), z: (?<pos_z>[-\d\.]+) " +
    "vel x: [^,]+, y: [^,]+, z: [^\]]+\] driven by '(?<driver>[^']+)' \[\d+\] " +
    "advanced from destroy level (?<destroy_level_from>\d+) to (?<destroy_level_to>\d+) " +
    "caused by '(?<caused_by>[^']+)' \[\d+\] with '(?<damage_type>[^']+)'"

# Lookup Patterns
$joinDatePattern = '<span class="label">Enlisted</span>\s*<strong class="value">([^<]+)</strong>'
$ueePattern = '<p class="entry citizen-record">\s*<span class="label">UEE Citizen Record<\/span>\s*<strong class="value">#?(n\/a|\d+)<\/strong>\s*<\/p>'

#csv variables
$CSVPath = "$scriptFolder\Kill-log.csv"
$CSVBackupPath = "$scriptFolder\_backup_Kill-log.csv"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$process = Get-Process | Where-Object {$_.Name -like "AutoTrackR2"}
$global:killTally = 0
$global:deathTally = 0
$global:otherTally = 0

# Load historic data from csv
if (Test-Path $CSVPath) {
	$historicKills = Import-CSV $CSVPath
	$currentDate = Get-Date
	$dateFormat = "dd MMM yyyy HH:mm UTC"

	#Convert old csv to new
	if($null -eq $($historicKills.Type)){
		#Move old file to a Backup
		Move-Item $csvPath $csvBackupPath

		#now convert Backup to new File
		foreach ($row in $historicKills) {
			$csvData = [PSCustomObject]@{
				Type             = "Kill"
				KillTime         = $($row.KillTime)
				EnemyPilot       = $($row.EnemyPilot)
				EnemyShip        = $($row.EnemyShip)
				Enlisted         = $($row.Enlisted)
				RecordNumber     = $($row.RecordNumber)
				OrgAffiliation   = $($row.OrgAffiliation)
				Player           = $($row.Player)
				Weapon           = $($row.Weapon)
				Ship             = $($row.Ship)
				Method           = $($row.Method)
				Mode             = $($row.Mode)
				GameVersion      = $($row.GameVersion)
				TrackRver		 = $($row.TrackRver)
				Logged			 = $($row.Logged)
				PFP				 = $($row.PFP)
			}
			if ($killLog -eq $true -or $deathLog -eq $true -or $otherLog -eq $true) {
				# Export to CSV
				if (-Not (Test-Path $CSVPath)) {
					# If file doesn't exist, create it with headers
					$csvData | Export-Csv -Path $CSVPath -NoTypeInformation
				} else {
					# Append data to the existing file without adding headers
					$csvData | ConvertTo-Csv -NoTypeInformation | Select-Object -Skip 1 | Out-File -Append -Encoding utf8 -FilePath $CSVPath
				}
			}			
		}
		#load new File
		$historicKills = Import-CSV $CSVPath
	}


	foreach ($kill in $historicKills) {
		$killDate = [datetime]::ParseExact($kill.KillTime.Trim(), $dateFormat, [System.Globalization.CultureInfo]::InvariantCulture)
		If ($killdate.year -eq $currentDate.Year -and $killdate.month -eq $currentDate.Month) {
			IF ($kill.type -eq "Kill" -and $killLog -eq $true){
				$global:killTally++
				Try {
					Write-Output "NewKill=throwaway,$($kill.EnemyPilot),$($kill.EnemyShip),$($kill.OrgAffiliation),$($kill.Enlisted),$($kill.RecordNumber),$($kill.KillTime), $($kill.PFP)"
				} Catch {
					Write-Output "Error Loading Kill: $($kill.EnemyPilot)"
				}
			}elseif($kill.type -eq "Death" -and $deathLog -eq $true){
				$global:deathTally++
				Try {
					Write-Output "NewDeath=throwaway,$($kill.EnemyPilot),$($kill.EnemyShip),$($kill.OrgAffiliation),$($kill.Enlisted),$($kill.RecordNumber),$($kill.KillTime), $($kill.PFP)"
				} Catch {
					Write-Output "Error Loading Death: $($kill.EnemyPilot)"
				}
			}elseif($otherLog -eq $true){
				$global:otherTally++
				Try {
					Write-Output "NewOther=throwaway,$($kill.Method),$($kill.KillTime)"
				} Catch {
					Write-Output "Error Loading Other: $($kill.Method)"
				}
			}
		}
	}
}
Write-Output "KillTally=$global:killTally"
Write-Output "DeathTally=$global:deathTally"
Write-Output "OtherTally=$global:otherTally"

# Match and extract username from gamelog
Do {
    # Load gamelog into memory
    $authLog = Get-Content -Path $logFilePath

    # Initialize variable to store username
    $global:userName = $null
	$global:loadout = "Person"

    # Loop through each line in the log to find the matching line
    foreach ($line in $authLog) {
        if ($line -match $loginPattern) {
            $global:userName = $matches['Player']
            Write-Output "PlayerName=$global:userName"
        }
		# Get Loadout
		if ($line -match $loadoutPattern) {
			$entity = $matches['Entity']
			$ownerGEID = $matches['OwnerGEID']

			If ($ownerGEID -eq $global:userName -and $entity -match $shipManPattern) {
				$tryloadOut = $entity
				If ($tryloadOut -match $cleanupPattern){
					if ($null -ne $matches[1]){
						$global:loadOut = $matches[1]
					}
				}
			}
		}
		Write-Output "PlayerShip=$global:loadOut"
		#Get SC Version
		If ($line -match $versionPattern){
			$global:GameVersion = $matches['gameversion']
		}
		#Get Game Mode
		if ($line -match $acPattern){
			$global:GameMode = "AC"
		}
		if ($line -match $puPattern){
			$global:GameMode = "PU"
		}
		Write-Output "GameMode=$global:GameMode"

	}
    # If no match found, print "Logged In: False"
    if (-not $global:userName) {
		Write-Output "PlayerName=No Player Found..."
        Start-Sleep -Seconds 30
    }

    # Clear the log from memory
    $authLog = $null
} until ($null -ne $global:userName)


# Function to process new log entries and write to the host
function Read-LogEntry {
    param (
        [string]$line
    )
    # Look for vehicle events
    if ($line -match $vehiclePattern) {
        # Access the named capture groups from the regex match
        $global:vehicle_id = $matches['vehicle']
        $global:location = $matches['vehicle_zone']
    }

    # Apply the regex pattern to the line
    if ($line -match $killPattern) {
        # Access the named capture groups from the regex match
        $victimPilot = $matches['VictimPilot']
        $victimShip = $matches['VictimShip']
        $agressorPilot = $matches['AgressorPilot']
		$weapon = $matches['Weapon']
		$damageType = $matches['DamageType']
		$ship = $global:loadOut

		If ($victimShip -ne "vehicle_id"){
            
            $global:got_location = $location
        }
        else
        {
            $global:got_location = "NONE"
        }

		#Define the Type
		#kill
		IF ($agressorPilot -eq $global:userName -and $victimPilot -ne $global:userName){
			$type = "Kill"
			Try {
				$page1 = Invoke-WebRequest -uri "https://robertsspaceindustries.com/citizens/$victimPilot"
			} Catch {
				$page1 = $null
				$type = "none"
			}
		#death
		}elseif ($agressorPilot -ne $global:userName -and $victimPilot -eq $global:userName) {
			#acception for "unknown"
			if ($agressorPilot -eq "unknown" -and $weapon -eq "unknown"){
				#$damageType = "snusnu"
				$type = "Other"
			} else {
				$type = "Death"
			}
			$agressorShip = "unknown"
			Try {
				$page1 = Invoke-WebRequest -uri "https://robertsspaceindustries.com/citizens/$agressorPilot"
			} Catch {
				$page1 = $null
				$type = "none"
			}
		#other
		}elseif($agressorPilot -eq $global:userName -or $victimPilot -eq $global:userName) {
			$type = "Other"
		}else {
			$type = "none"
		}

		if ($type -ne "none") {
			# Check if the Autotrackr2 process is running
			if ($null -eq (Get-Process -ID $parentApp -ErrorAction SilentlyContinue)) {
				Stop-Process -Id $PID -Force
			}
			
			If ($victimShip -ne "Person"){
				If ($victimShip -eq $global:lastKill){
					$victimShip = "Passenger"
				} Else {
					$global:lastKill = $victimShip
				}
			}

			If ($victimShip -match $cleanupPattern){
				$victimShip = $matches[1]
			}
			If ($weapon -match $cleanupPattern){
				$weapon = $matches[1]
			}
			If ($weapon -eq "KLWE_MassDriver_S10"){
				if ($type -eq "Kill"){
					$global:loadOut = "AEGS_Idris"
				}
				$ship = "AEGS_Idris"
			}
			if ($damageType -eq "Bullet" -or $weapon -like "apar_special_ballistic*") {
				$ship = "Person"
				$victimShip = "Person"
				$agressorShip = "Person"
			}
			If ($ship -match $cleanupPattern){
				$ship = $matches[1]
			}
			if ($ship -notmatch $shipManPattern){
				$ship = "Person"
			}
			If ($victimShip -notmatch $shipManPattern -and $victimShip -notlike "Passenger" ) {
				$victimShip = "Person"
			}
		
			# Repeatedly remove all suffixes
			while ($victimShip -match '_(PU|AI|CIV|MIL|PIR)$') {
				$victimShip = $victimShip -replace '_(PU|AI|CIV|MIL|PIR)$', ''
			}
			while ($ship -match '_(PU|AI|CIV|MIL|PIR)$') {
				$ship = $ship -replace '_(PU|AI|CIV|MIL|PIR)$', ''
			}
			while ($victimShip -match '-00(1|2|3|4|5|6|7|8|9|0)$') {
				$victimShip = $victimShip -replace '-00(1|2|3|4|5|6|7|8|9|0)$', ''
			}
			while ($ship -match '-00(1|2|3|4|5|6|7|8|9|0)$') {
				$ship = $ship -replace '-00(1|2|3|4|5|6|7|8|9|0)$', ''
			}

			$KillTime = (Get-Date).ToUniversalTime().ToString("dd MMM yyyy HH:mm 'UTC'", [System.Globalization.CultureInfo]::InvariantCulture)
		
			#If get Enemy Player data
			If ($null -ne $page1){
				# Get Enlisted Date
				if ($($page1.content) -match $joinDatePattern) {
					$joinDate = $matches[1]
					$joinDate2 = $joinDate -replace ',', ''
				} else {
					$joinDate2 = "-"
				}

				# Check if there are any matches
				If ($null -eq $page1.links[0].innerHTML) {
					$enemyOrgs = $page1.links[4].innerHTML
				} Else {
					$enemyOrgs = $page1.links[3].innerHTML
				}

				# Get UEE Number
				if ($($page1.content) -match $ueePattern) {
					# The matched UEE Citizen Record number is in $matches[1]
					$citizenRecord = $matches[1]
				} else {
					$citizenRecord = "n/a"
				}
				If ($citizenRecord -eq "n/a") {
					$citizenRecordAPI = "-1"
					$citizenRecord = "-"
				} Else {
					$citizenRecordAPI = $citizenRecord
				}

				# Get PFP
				if ($page1.images[0].src -like "/media/*") {
					$enemyPFP = "https://robertsspaceindustries.com$($page1.images[0].src)"
				} Else {
					$enemyPFP = "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg"
				}
			}

			$global:GameMode = $global:GameMode.ToLower()
			# Send to API (Kill only)
			# Define the data to send
			If ($null -ne $apiUrl -and $offlineMode -eq $false -and $type -eq "Kill"){
				$data = @{
					victim_ship		= $victimShip
					victim			= $victimPilot
					enlisted		= $joinDate
					rsi				= $citizenRecordAPI
					weapon			= $weapon
					method			= $damageType
					loadout_ship	= $ship
					game_version	= $global:GameVersion
					gamemode		= $global:GameMode
					trackr_version	= $TrackRver
					location        = $got_location
				}

				# Headers which may or may not be necessary
				$headers = @{
					"Authorization" = "Bearer $apiKey"
					"Content-Type" = "application/json"
					"User-Agent" = "AutoTrackR2"
				}

				try {
					# Send the POST request with JSON data
					$null = Invoke-RestMethod -Uri $apiURL -Method Post -Body ($data | ConvertTo-Json -Depth 5) -Headers $headers
					$logMode = "API"
					$global:got_location = "NONE"
				} catch {
					# Catch and display errors
					$apiError = $_
					# Add to output file
					$logMode = "Err-Local"
				}
			} Else {
				$logMode = "Local"
			}
			
			#process Kill data
			if ($type -eq "Kill" -and $killLog -eq $true) {
				# Create an object to hold the data for a kill
				$csvData = [PSCustomObject]@{
					Type             = $type
					KillTime         = $killTime
					EnemyPilot       = $victimPilot
					EnemyShip        = $victimShip
					Enlisted         = $joinDate2
					RecordNumber     = $citizenRecord
					OrgAffiliation   = $enemyOrgs
					Player           = $agressorPilot
					Weapon           = $weapon
					Ship             = $ship
					Method           = $damageType
					Mode             = $global:GameMode
					GameVersion      = $global:GameVersion
					TrackRver		 = $TrackRver
					Logged			 = $logMode
					PFP				 = $enemyPFP
				}			
				# Remove commas from all properties
				foreach ($property in $csvData.PSObject.Properties) {
					if ($property.Value -is [string]) {
						$property.Value = $property.Value -replace ',', ''
					}
				}					
				#write Kill
				$global:killTally++
				Write-Output "KillTally=$global:killTally"
				Write-Output "NewKill=throwaway,$victimPilot,$victimShip,$enemyOrgs,$joinDate2,$citizenRecord,$killTime,$enemyPFP"

			#process Death data
			} elseif ($type -eq "Death" -and $deathLog -eq $true) {
				# Create an object to hold the data for a death	
				$csvData = [PSCustomObject]@{
					Type             = $type
					KillTime         = $killTime
					EnemyPilot       = $agressorPilot
					EnemyShip        = $agressorShip
					Enlisted         = $joinDate2
					RecordNumber     = $citizenRecord
					OrgAffiliation   = $enemyOrgs
					Player           = $victimPilot
					Weapon           = $weapon
					Ship             = $Ship
					Method           = $damageType
					Mode             = $global:GameMode
					GameVersion      = $global:GameVersion
					TrackRver		 = $TrackRver
					Logged			 = $logMode
					PFP				 = $enemyPFP
				}
				# Remove commas from all properties
				foreach ($property in $csvData.PSObject.Properties) {
					if ($property.Value -is [string]) {
						$property.Value = $property.Value -replace ',', ''
					}
				}					
				#write Death
				$global:deathTally++
				Write-Output "DeathTally=$global:deathTally"
				Write-Output "NewDeath=throwaway,$agressorPilot,$agressorShip,$enemyOrgs,$joinDate2,$citizenRecord,$killTime,$enemyPFP"

			#process Other data
			} elseif ($type -eq "Other" -and $otherLog -eq $true) {
				# Create an object to hold the data for a other
				$csvData = [PSCustomObject]@{
					Type             = $type
					KillTime         = $killTime
					EnemyPilot       = ""
					EnemyShip        = ""
					Enlisted         = ""
					RecordNumber     = ""
					OrgAffiliation   = ""
					Player           = $global:userName
					Weapon           = $weapon
					Ship             = $victimShip
					Method           = $damageType
					Mode             = $global:GameMode
					GameVersion      = $global:GameVersion
					TrackRver		 = $TrackRver
					Logged			 = $logMode
					PFP				 = ""
				}
				# Remove commas from all properties
				foreach ($property in $csvData.PSObject.Properties) {
					if ($property.Value -is [string]) {
						$property.Value = $property.Value -replace ',', ''
					}
				}	
				#write Other
				$global:otherTally++
				Write-Output "OtherTally=$global:otherTally"
				Write-Output "NewOther=throwaway,$damageType,$killTime"
			}

			
			if ($killLog -eq $true -or $deathLog -eq $true -or $otherLog -eq $true) {

				# Export to CSV
				if (-Not (Test-Path $CSVPath)) {
					# If file doesn't exist, create it with headers
					$csvData | Export-Csv -Path $CSVPath -NoTypeInformation
				} else {
					# Append data to the existing file without adding headers
					$csvData | ConvertTo-Csv -NoTypeInformation | Select-Object -Skip 1 | Out-File -Append -Encoding utf8 -FilePath $CSVPath
				}
			
				$sleeptimer = 10

				# VisorWipe
				If ($visorWipe -eq $true -and $victimShip -ne "Passenger" -and $damageType -notlike "*Bullet*" -and $type -ne "Other"){ 
					# send keybind for visorwipe
					start-sleep 1
					$sleeptimer = $sleeptimer -1
					&"$scriptFolder\visorwipe.ahk"
				}
			
				# Record video
				if ($videoRecord -eq $true -and $victimShip -ne "Passenger" -and $damageType -ne "Suicide"){
					# send keybind for windows game bar recording
					Start-Sleep 2
					$sleeptimer = $sleeptimer -9
					&"$scriptFolder\videorecord.ahk"
					Start-Sleep 7

					$latestFile = Get-ChildItem -Path $videoPath | Where-Object { -not $_.PSIsContainer } | Sort-Object CreationTime -Descending | Select-Object -First 1
					# Check if the latest file is no more than 30 seconds old
					if ($latestFile) {
						$fileAgeInSeconds = (New-TimeSpan -Start $latestFile.CreationTime -End (Get-Date)).TotalSeconds
						if ($fileAgeInSeconds -le 30) {
							# Generate a timestamp in ddMMMyyyy-HH:mm format
							$timestamp = (Get-Date).ToString("ddMMMyyyy-HHmm")
		
							# Extract the file extension to preserve it
							$fileExtension = $latestFile.Extension

							# Rename the file, preserving the original file extension
							Rename-Item -Path $latestFile.FullName -NewName "$type.$victimPilot.$victimShip.$timestamp$fileExtension"
						} else {}
					} else {}
				}
				Start-Sleep $sleeptimer
			}
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
			Write-Output "PlayerName=$global:userName"
		}
	}

	# Detect PU or AC
	if ($line -match $puPattern) {
		$global:GameMode = "PU"
		Write-Output "GameMode=$global:GameMode"
	}
	if ($line -match $acPattern) {
		$global:GameMode = "AC"
		Write-Output "GameMode=$global:GameMode"
	}

	#Set loadout 
	if ($line -match $loadoutPattern) {
		$entity = $matches['Entity']
		$ownerGEID = $matches['OwnerGEID']

        If ($ownerGEID -eq $global:userName -and $entity -match $shipManPattern) {
			$tryloadOut = $entity
			If ($tryloadOut -match $cleanupPattern){
				$global:loadOut = $matches[1]
			}
			Write-Output "PlayerShip=$global:loadOut"
		}
	}
}

# Monitor the log file and process new lines as they are added
Get-Content -Path $logFilePath -Wait -Tail 0 | ForEach-Object {
    Read-LogEntry $_
}

<#
# Open the log file with shared access for reading and writing
$fileStream = [System.IO.FileStream]::new($logFilePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$reader = [System.IO.StreamReader]::new($fileStream, [System.Text.Encoding]::UTF8)  # Ensure we're reading as UTF-8

try {
    # Move to the end of the file to start monitoring new entries
    $reader.BaseStream.Seek(0, [System.IO.SeekOrigin]::End)

    while ($true) {
        # Read the next line from the file
        $line = $reader.ReadLine()

        # Ensure we have new content to process
        if ($line) {
            # Process the line (this is where your log entry handler would go)
            Read-LogEntry $line
        }

        # Sleep for a brief moment to avoid high CPU usage
        Start-Sleep -Milliseconds 100
    }
}
finally {
    # Ensure we close the reader and file stream properly when done
    $reader.Close()
    $fileStream.Close()
}
#>