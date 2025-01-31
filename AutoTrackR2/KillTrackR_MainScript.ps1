# Script to extract informations from StarCitizen Game.log in realtime
# GitHub: https://github.com/BubbaGumpShrump/AutoTrackR2

# Script version
$script:TrackRver = "2.06-koda-mod-opt"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# ================================= Configuration =================================
$script:DebugLvl=0

$script:AppName = "AutoTrackR2"
$script:ScriptFolder = Join-Path -Path $env:LOCALAPPDATA -ChildPath $AppName
$script:ConfigFileName= "config.ini"
$script:ConfigFile = "$script:ScriptFolder\$script:ConfigFileName" 

# File Data
$script:CSVFileName = "Kill-log.csv"
$script:CSVFile = "$script:ScriptFolder\$script:CSVFileName"

$script:CsvBackupFileName = "_backup_Kill-log.csv"
$script:CsvBackupFile = "$script:ScriptFolder\$script:CsvBackupFileName"

$script:VisorWipeFileName = "visorwipe.ahk"
$script:VisorWipeFile = "$script:ScriptFolder\$script:VisorWipeFileName"
$script:VideoRecordFileName = "videorecord.ahk"
$script:VideoRecordFile = "$script:ScriptFolder\$script:VideoRecordFileName"

# Definition of script variables
$script:DateFormat = "dd MMM yyyy HH:mm UTC"
$script:KillTally = 0
$script:DeathTally = 0
$script:OtherTally = 0
$script:FpsLoadout = "Person"
$script:LastKill = $null
$script:PlayerCache = @{}
$script:UserName = $null
$script:Loadout = $script:FpsLoadout
$script:GameMode = $null
$script:GameVersion = $null

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
$script:KillPattern = "<Actor Death> CActor::Kill: '(?<VictimPilot>[^']+)' \[\d+\] in zone '(?<VictimShip>[^']+)' killed by '(?<AgressorPilot>[^']+)' \[[^']+\] using '(?<Weapon>[^']+)' \[Class (?<Class>[^\]]+)\] with damage type '(?<DamageType>[^']+)'"
$script:PuPattern = '<\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z> \[Notice\] <ContextEstablisherTaskFinished> establisher="CReplicationModel" message="CET completed" taskname="StopLoadingScreen" state=[^ ]+ status="Finished" runningTime=\d+\.\d+ numRuns=\d+ map="megamap" gamerules="SC_Default" sessionId="[a-f0-9\-]+" \[Team_Network\]\[Network\]\[Replication\]\[Loading\]\[Persistence\]'
$script:AcPattern = "ArenaCommanderFeature"
$script:LoadoutPattern = '\[InstancedInterior\] OnEntityLeaveZone - InstancedInterior \[(?<InstancedInterior>[^\]]+)\] \[\d+\] -> Entity \[(?<Entity>[^\]]+)\] \[\d+\] -- m_openDoors\[\d+\], m_managerGEID\[(?<ManagerGEID>\d+)\], m_ownerGEID\[(?<OwnerGEID>[^\[]+)\]'
$script:ShipManPattern = "^(" + ($prefixes -join "|") + ")"
# $script:LoginPattern = "\[Notice\] <AccountLoginCharacterStatus_Character> Character: createdAt [A-Za-z0-9]+ - updatedAt [A-Za-z0-9]+ - geid [A-Za-z0-9]+ - accountId [A-Za-z0-9]+ - name (?<Player>[A-Za-z0-9_-]+) - state STATE_CURRENT" # KEEP THIS INCASE LEGACY LOGIN IS REMOVED 
$script:LoginPattern = "\[Notice\] <Legacy login response> \[CIG-net\] User Login Success - Handle\[(?<Player>[A-Za-z0-9_-]+)\]"
$script:CleanupPattern = '^(.+?)_\d+$'
$script:VersionPattern = "--system-trace-env-id='pub-sc-alpha-(?<gameversion>\d{3,4}-\d{7})'"
$script:VehiclePattern = "<(?<timestamp>[^>]+)> \[Notice\] <Vehicle Destruction> CVehicle::OnAdvanceDestroyLevel: " +
    "Vehicle '(?<vehicle>[^']+)' \[\d+\] in zone '(?<vehicle_zone>[^']+)' " +
    "\[pos x: (?<pos_x>[-\d\.]+), y: (?<pos_y>[-\d\.]+), z: (?<pos_z>[-\d\.]+) " +
    "vel x: [^,]+, y: [^,]+, z: [^\]]+\] driven by '(?<driver>[^']+)' \[\d+\] " +
    "advanced from destroy level (?<destroy_level_from>\d+) to (?<destroy_level_to>\d+) " +
    "caused by '(?<caused_by>[^']+)' \[\d+\] with '(?<damage_type>[^']+)'"


# ================================= Functions =================================
function Get-ConfigurationSettings {
    param (
        [string]$configFile
    )
    # Get config
    $config = @{}
    Get-Content $configFile | Where-Object { $_ -notmatch '^#|^\s*$' } | ForEach-Object {
        $key, $value = $_ -split '=', 2
        $config[$key.Trim()] = $value.Trim() 
    }

    # Validate required settings
    $requiredSettings = @('Logfile')
    foreach ($setting in $requiredSettings) {
        if (-not $config.ContainsKey($setting)) {
            Write-Error "Missing required setting: $setting"
            return $null
        }
    } 

    # Set missing config
    $logSettings = @('OfflineMode', 'KillLog', 'DeathLog', 'OtherLog', 'VisorWipe', 'VideoRecord')
    foreach ($setting in $logSettings) {
        if (-not $config.ContainsKey($setting)) {
            $config.$($setting) = $false
            Write-OutputData "LogInfo=Missing setting: $($setting) Set to false"
        } elseif ($config.$($setting) -eq 1) {
            $config.$($setting) = $true
        } else {
            $config.$($setting) = $false
        }
    }

    # Check if the VisorWipe script exists
    if ($config.VisorWipe) {
        if (-Not (Test-Path $script:VisorWipeFile)) {
            Write-Warning "No VisorWipe script found at $script:VisorWipeFile disable VisorWipe"
            $config.VisorWipe = $false
        }
    }

    # Check if the VideoRecord script exists
    if ($config.VideoRecord) {
        if (-Not (Test-Path $script:VideoRecordFile)) {
            Write-Warning "No VideoRecord script found at $script:VideoRecordFile disable VideoRecord"
            $config.VideoRecord = $false
        }
    }

    # Get Debug Level
    if ($config.Debug) {
        $script:DebugLvl = $config.Debug
    }

    return $config
}

function Import-CsvData {
    param (
        [bool]$killLog = $false,
        [bool]$deathLog = $false,
        [bool]$otherLog = $false,
        [string]$csvFile
    )
    # Define the header for the new CSV file 
    $csvHeader = "Type,KillTime,EnemyPilot,EnemyShip,Enlisted,RecordNumber,OrgAffiliation,Player,Weapon,Ship,Method,Mode,GameVersion,TrackRver,Logged,PFP"

    # Check if the CSV file exists
    if (-Not (Test-Path $csvFile)) {
        Write-OutputData "LogInfo=CSV file not found. Creating a new file with headers..."     
        # Create a new CSV file with the defined headers
        $csvHeader | Out-File -FilePath $csvFile -Encoding utf8
    }

	$historicKills = Import-CSV $csvFile
	$currentDate = Get-Date
    $header = (Get-Content $csvFile -First 1)

	#Convert old csv to new
    if ($header -notmatch "Type") {
        Write-OutputData "LogInfo=Old CSV-format! Converting to new"
		#Move old file to a Backup
        Move-Item -Path $csvFile -Destination $script:CsvBackupFile -Force

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
            # Export to CSV
            if (-Not (Test-Path $csvFile)) {
                # If file doesn't exist, create it with headers
                $csvHeader | Out-File -FilePath $csvFile -Encoding utf8
            }
            # Append data to the existing file without adding headers
            $csvData | ConvertTo-Csv -NoTypeInformation | Select-Object -Skip 1 | Out-File -Append -Encoding utf8 -FilePath $csvFile
			
        }
		#load new File
		$historicKills = Import-CSV $csvFile
	}

    # Procressing CSV-Data
    foreach ($row in $historicKills) {
        try {
            # Parse date and filter by current month/year
            $killDate = [datetime]::ParseExact($row.KillTime.Trim(), $script:DateFormat, [System.Globalization.CultureInfo]::InvariantCulture)

            if ($killDate.Year -eq $currentDate.Year -and $killDate.Month -eq $currentDate.Month) {
                switch ($row.Type) {
                    "Kill" {
                        if ($killLog) {
                            Update-Tally $row.Type
                            Write-OutputData "NewKill=throwaway,$($row.EnemyPilot),$($row.EnemyShip),$($row.OrgAffiliation),$($row.Enlisted),$($row.RecordNumber),$($row.KillTime),$($row.PFP)"
                        }
                    }
                    "Death" {
                        if ($deathLog) {
                            Update-Tally $row.Type
                            Write-OutputData "NewDeath=throwaway,$($row.EnemyPilot),$($row.EnemyShip),$($row.OrgAffiliation),$($row.Enlisted),$($row.RecordNumber),$($row.KillTime),$($row.PFP)"
                        }
                    }
                    default {
                        if ($otherLog) {
                            Update-Tally $row.Type
                            Write-OutputData "NewOther=throwaway,$($row.Method),$($row.KillTime)"
                        }
                    }
                }
            }
        } catch {
            Write-OutputData "LogInfo=Error when processing a CSV entry: $_"
        }
    }

    return $historicKills
}

function Read-LogEntry {
    param (
        [string]$line,
        [hashtable]$config,
		[switch]$initialised
    )

	#Get SC Version
	If ($initialised -and $line -match $script:VersionPattern){
		$script:GameVersion = $matches['gameversion']
		Write-OutputData "LogInfo=GameVersion: $script:GameVersion"
	}

    <# why sould this nessesary?
	# Get Logged-in User
	if ($initialised -and $line -match $script:LoginPattern) {
        $script:UserName = $matches['Player']
		Write-OutputData "PlayerName=$script:UserName"
    }
	If ($initialised -eq $false -and $line -match $script:LoginPattern) {
		# Load gamelog into memory
		$authLog = Get-Content -Path $script:logFilePath
		$authLog = $authlog -match $script:LoginPattern
		$authLog = $authLog | Out-String

		# Extract User Name
		$nameExtract = "name\s+(?<PlayerName>[^\s-]+)"

		If ($authLog -match $nameExtract -and $script:UserName -ne $nameExtract){
			$script:UserName = $matches['PlayerName']
			Write-OutputData "PlayerName=$script:UserName"
		}
	}
    #>

	# Get Logged-in User
	if ($line -match $script:LoginPattern) {
		$script:UserName = $matches['Player']
		Write-OutputData "PlayerName=$script:UserName"
	}

    # Vehicle events
    if ($line -match $script:VehiclePattern) {
        # Access the named capture groups from the regex match
        $vehicle_id = $matches['vehicle']
        $location = $matches['vehicle_zone']
    }

    # Get Loadout
	if ($line -match $script:LoadoutPattern) {
		$entity = $matches['Entity']
		$ownerGEID = $matches['OwnerGEID']

        If ($ownerGEID -eq $script:UserName -and $entity -match $script:ShipManPattern) {
			$tryloadOut = $entity
			If ($tryloadOut -match $script:CleanupPattern){
				$script:Loadout = $matches[1]
			}
		}
		Write-OutputData "PlayerShip=$script:Loadout"
	}

    #Get Game Mode
	if ($line -match $script:PuPattern) {
		$script:GameMode = "PU"
		Write-OutputData "GameMode=$script:GameMode"		
	}
	if ($line -match $script:AcPattern) {
		$script:GameMode = "AC"
		Write-OutputData "GameMode=$script:GameMode"
	}

	# Apply the regex pattern to the line
	if (-not $initialised -and $line -match $script:KillPattern) {
        Write-OutputData "LogInfo=KillPattern detected!"
		$eventData = New-KillEvent -data $matches -location $location -vehicle_id $vehicle_id
		$type = New-EventType -eventData $eventData -userName $script:UserName -killLog $config.KillLog -deathLog $config.DeathLog -otherLog $config.OtherLog
	
		if ($type -ne "none") {
            if ($type -ne "Other") {
                $playerInfo = Get-PlayerInfo $(if ($type -eq "Kill") { $eventData.VictimPilot } else { $eventData.AgressorPilot })
            }
            
			$eventData.victimShip = Format-ShipName $eventData.victimShip
            $eventData.playerShip = Format-ShipName $script:Loadout    
	
			$csvData = New-CsvData $type $eventData $playerInfo
	
			if ($type -eq "Kill" -and $null -ne $apiUrl -and -not $config.OfflineMode) {
                Write-OutputData "LogInfo=Send $type data to server"
				Send-ApiData $csvData
			}
	
			Write-CSVData -csvData $csvData -csvFile $script:CSVFile
	
			Update-Tally $type
			Write-OutputData "New$type=throwaway,$($csvData.EnemyPilot),$($csvData.EnemyShip),$($csvData.OrgAffiliation),$($csvData.Enlisted),$($csvData.RecordNumber),$($csvData.KillTime),$($csvData.PFP)"
	
			Invoke-PostEventActions -config $config -type $type -victimShip $eventData.victimShip -damageType $eventData.damageType
		}
	}
	
}

function New-CsvData {
    param (
        [string]$type,
        [hashtable]$eventData,
        [hashtable]$playerInfo
    )

    $csvData = [PSCustomObject]@{
        Type           = $type
        KillTime       = (Get-Date).ToUniversalTime().ToString($script:DateFormat, [System.Globalization.CultureInfo]::InvariantCulture)
        EnemyPilot     = if ($type -eq "Death") { $eventData.AgressorPilot } else { $eventData.VictimPilot }
        EnemyShip      = if ($type -eq "Death") { $eventData.AgressorShip } else { $eventData.VictimShip }
        Enlisted       = $playerInfo.JoinDate
        RecordNumber   = $playerInfo.CitizenRecord
        OrgAffiliation = $playerInfo.Orgs
        Player         = $script:UserName
        Weapon         = $eventData.Weapon
        Ship           = $eventData.playerShip
        Method         = $eventData.DamageType
        Mode           = $script:GameMode
        GameVersion    = $script:GameVersion
        TrackRver      = $script:TrackRver
        Logged         = $eventData.LogMode
        PFP            = $playerInfo.PFP
    }

    # Remove commas from all string properties
    foreach ($property in $csvData.PSObject.Properties) {
        if ($property.Value -is [string]) {
            $property.Value = $property.Value -replace ',', ''
        }
    }

    return $csvData
}

function New-KillEvent {
    param (
        [hashtable]$data,
        [string] $location,
        [string] $vehicle_id
    )

    $victimShip = $data['VictimShip']
    $weapon = $data['Weapon']
    $damageType = $data['DamageType']
    $agressorShip = $null

    # Clean Weapon pattern
    If ($weapon -match $script:CleanupPattern){
        $weapon = $matches[1]
    }
    # Agressor is using an Idris?
    If ($weapon -eq "KLWE_MassDriver_S10"){
        $agressorShip = "AEGS_Idris"
    }

    # FPS evnet?
    if ($damageType -eq "Bullet" -or $weapon -like "apar_special_ballistic*") {
        $agressorShip = $script:FpsLoadout
        $victimShip = $script:FpsLoadout
    }

    # Do we have a Passenger?
    If ($victimShip -ne $script:FpsLoadout){
        If ($victimShip -eq $script:LastKill){
            $victimShip = "Passenger"
        } Else {
            $script:LastKill = $victimShip
        }
    }
    
    return @{
        VictimPilot = $data['VictimPilot']
        VictimShip = $victimShip
        AgressorPilot = $data['AgressorPilot']
        AgressorShip = $agressorShip
        Weapon = $weapon
        DamageType = $damageType
        #Location = if ($data['VictimShip'] -ne "vehicle_id") { $location } else { "NONE" } #I think this was an error
        Location = if ($data['VictimShip'] -ne $vehicle_id) { $location } else { "NONE" }
    }
}

function New-EventType {
    param (
        [hashtable]$eventData,
        [string]$userName,
        [bool]$killLog = $false,
        [bool]$deathLog = $false,
        [bool]$otherLog = $false
    )
    
    if ($eventData.AgressorPilot -eq $userName -and $eventData.VictimPilot -ne $userName) {
        if($killLog){return "Kill"}else{return "none"}
    } elseif ($eventData.AgressorPilot -ne $userName -and $eventData.VictimPilot -eq $userName) {
        if ($eventData.AgressorPilot -eq "unknown" -and $eventData.Weapon -eq "unknown") {
            if($otherLog){return "Other"}else{return "none"}
        }
        if($deathLog){return "Death"}else{return "none"}
    } elseif ($eventData.AgressorPilot -eq $userName -or $eventData.VictimPilot -eq $userName) {
        return "Other"
    }
    return "none"
}

function Update-Tally {
    param (
        [string]$type
    )

    switch ($type) {
        "Kill" {
            $script:KillTally++
			Write-OutputData "KillTally=$($script:KillTally)"
        }
        "Death" {
            $script:DeathTally++
			Write-OutputData "DeathTally=$($script:DeathTally)"
        }
        "Other" {
            $script:OtherTally++
			Write-OutputData "OtherTally=$($script:OtherTally)"
        }
    }
}

function Get-PlayerInfo {
    param (
        [string]$playerName
    )
    
	# Check cache
    if ($script:PlayerCache.ContainsKey($playerName)) {
        return $script:PlayerCache[$playerName]
    }
    
    try {
        $page = Invoke-WebRequest -Uri "https://robertsspaceindustries.com/citizens/$playerName"
        
        $joinDate = if ($page.Content -match $joinDatePattern) { $matches[1] -replace ',', '' } else { "-" }
        $enemyOrgs = if ($null -eq $page.Links[0].innerHTML) { $page.Links[4].innerHTML } else { $page.Links[3].innerHTML }
        $citizenRecord = if ($page.Content -match $ueePattern) { $matches[1] } else { "-" }
        $enemyPFP = if ($page.Images[0].src -like "/media/*") { "https://robertsspaceindustries.com$($page.Images[0].src)" } else { "https://cdn.robertsspaceindustries.com/static/images/account/avatar_default_big.jpg" }
        
        $playerInfo = @{
            JoinDate = $joinDate
            Orgs = $enemyOrgs
            CitizenRecord = $citizenRecord
            PFP = $enemyPFP
        }
        
		# Whrite cache
        $script:PlayerCache[$playerName] = $playerInfo
        return $playerInfo
    } catch {
		Write-Warning "Error retrieving player information for PlayerName: $_"
        return $null
    }
}

function Format-ShipName {
    param (
        [string]$shipName
    )

    $shipName = $shipName -replace $script:CleanupPattern, '$1'
    
    while ($shipName -match '_(PU|AI|CIV|MIL|PIR)$') {
        $shipName = $shipName -replace '_(PU|AI|CIV|MIL|PIR)$', ''
    }
    
    while ($shipName -match '-00(1|2|3|4|5|6|7|8|9|0)$') {
        $shipName = $shipName -replace '-00(1|2|3|4|5|6|7|8|9|0)$', ''
    }

    if ($shipName -notmatch $script:ShipManPattern -and $shipName -ne "Passenger") {
        $shipName = $script:FpsLoadout
    }

    return $shipName
}

function Send-ApiData {
    param (
        [hashtable]$data,
        [string]$apiUrl,
        [string]$apiKey
    )

    $headers = @{
        "Authorization" = "Bearer $apiKey"
        "Content-Type" = "application/json"
        "User-Agent" = "AutoTrackR2"
    }

    try {
        $null = Invoke-RestMethod -Uri $apiUrl -Method Post -Body ($data | ConvertTo-Json -Depth 5) -Headers $headers
        return "API"
    } catch {
        Write-Warning "LogInfo=API-Error: $_"
        return "Err-Local"
    }
}

function Write-CSVData {
    param (
        [PSCustomObject]$csvData,
        [string]$csvFile
    )

    foreach ($property in $csvData.PSObject.Properties) {
        if ($property.Value -is [string]) {
            $property.Value = $property.Value -replace ',', ''
        }
    }

    if (-Not (Test-Path $csvFile)) {
        $csvData | Export-Csv -Path $csvFile -NoTypeInformation
    } else {
        $csvData | ConvertTo-Csv -NoTypeInformation | Select-Object -Skip 1 | Out-File -Append -Encoding utf8 -FilePath $csvFile
    }
}

function Invoke-PostEventActions{
    param (
        [hashtable]$config,
        [string]$type,
        [string]$victimShip,
        [string]$damageType
    )

    $sleepTimer = 10

    if ($config.VisorWipe -and $victimShip -ne "Passenger" -and $damageType -notlike "*Bullet*" -and $type -ne "Other") {
        Start-Sleep 1
        $sleepTimer--
        & "$script:VisorWipeFile"
    }

    if ($config.VideoRecord -and $victimShip -ne "Passenger" -and $damageType -ne "Suicide") {
        Start-Sleep 2
        $sleepTimer -= 9
        & "$script:VideoRecordFile"
        Start-Sleep 7

        $latestFile = Get-ChildItem -Path $videoPath | Where-Object { -not $_.PSIsContainer } | Sort-Object CreationTime -Descending | Select-Object -First 1
        if ($latestFile -and ((New-TimeSpan -Start $latestFile.CreationTime -End (Get-Date)).TotalSeconds -le 30)) {
            $timestamp = (Get-Date).ToString("ddMMMyyyy-HHmm")
            $fileExtension = $latestFile.Extension
            Rename-Item -Path $latestFile.FullName -NewName "$type.$victimPilot.$victimShip.$timestamp$fileExtension"
        }
    }

    Start-Sleep $sleepTimer
}

function Write-OutputData {
    param (
        [string]$data
    )
    if ($data -ne $script:WritheCache) {
        if($script:DebugLVL -eq 1){
            Write-Host $data
        }else{
            if($data -notmatch "LogInfo=" ){
                Write-Host $data
            }
        }    
    }
    # Set cache
    $script:WritheCache = $data
}


# ================================= Main script =================================
# Check if the config file exists
if (-not (Test-Path $script:ConfigFile)) {
    Write-Error "$script:CSVFileName not found."
    return $null
}
# Get configurations
$config = Get-ConfigurationSettings $script:ConfigFile

# Check if the log file exists
if (-not (Test-Path $config.LogFile)) {
    Write-Error "Log file not found: $($config.LogPath)"
    return $null
}

# Import history
$null = Import-CsvData -killLog $config.KillLog -deathLog $config.DeathLog -otherLog $config.OtherLog -csvFile $script:CSVFile
# Procress log file for initialising
Do {
    Get-Content -Path $config.LogFile | ForEach-Object {
        Read-LogEntry -line $_ -config $config -initialised
    }
    # If no match found, print "Logged In: False"
    if (-not $script:UserName) {
        Write-OutputData "LogInfo=No Player Found. Waiting."
        Start-Sleep -Seconds 30
    }
} until ($null -ne $script:UserName)

Write-OutputData "LogInfo=Start monitoring $($configLogFile)"
# Monitor the log file and process new lines as they are added
Get-Content -Path $config.LogFile -Wait -Tail 0| ForEach-Object {
    Read-LogEntry -line $_ -config $config
     
}