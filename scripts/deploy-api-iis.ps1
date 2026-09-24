
[CmdletBinding()]
param(

    [string]$SiteName    = "VoltLinkApi",

    [int]$Port           = 8080,


    [string]$TargetPath  = "C:\inetpub\VoltLinkApi",


    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"


$RepoRoot    = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $RepoRoot "backend\src\VoltLink.Api\VoltLink.Api.csproj"
$PublishPath = Join-Path $RepoRoot "publish\api"
$AppPoolName = $SiteName

function Write-Step { param([string]$Message) Write-Host "`n==> $Message" -ForegroundColor Cyan }
function Write-Ok   { param([string]$Message) Write-Host "    $Message" -ForegroundColor Green }
function Write-Warn { param([string]$Message) Write-Host "    $Message" -ForegroundColor Yellow }


Write-Step "Checking prerequisites"

$identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "This script must be run from an elevated PowerShell window (Run as administrator)."
}
Write-Ok "Running elevated."


$ancm = "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (-not (Test-Path $ancm)) {
    throw ("The ASP.NET Core Hosting Bundle is not installed, so IIS cannot host this " +
           "application. Install it from https://dotnet.microsoft.com/download/dotnet/10.0 " +
           "(the 'Hosting Bundle' link under Run server apps), then run 'iisreset' and try again.")
}
Write-Ok ("ASP.NET Core Module V2 found, version " +
          (Get-Item $ancm).VersionInfo.ProductVersion + ".")

Import-Module WebAdministration -ErrorAction Stop
Write-Ok "IIS WebAdministration module loaded."

$prodSettings = Join-Path $RepoRoot "backend\src\VoltLink.Api\appsettings.Production.json"
if (-not (Test-Path $prodSettings)) {
    throw ("appsettings.Production.json is missing. It holds the MongoDB connection " +
           "string and signing keys and is not committed to Git. Create it before deploying.")
}
Write-Ok "Production settings file present."


if (-not $SkipPublish) {
    Write-Step "Publishing the API in Release configuration"
    if (Test-Path $PublishPath) { Remove-Item $PublishPath -Recurse -Force }
    dotnet publish $ProjectPath -c Release -o $PublishPath
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
    Write-Ok "Published to $PublishPath."
} else {
    Write-Step "Skipping publish, using existing output"
    if (-not (Test-Path (Join-Path $PublishPath "VoltLink.Api.dll"))) {
        throw "No published output found at $PublishPath. Run without -SkipPublish."
    }
}


Write-Step "Configuring the application pool"

if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
    Write-Ok "Created application pool '$AppPoolName'."
} else {
    Write-Ok "Application pool '$AppPoolName' already exists."
}

# "No Managed Code" is required: the .NET runtime is loaded by the ASP.NET Core
# Module itself, not by the old .NET Framework pipeline. Leaving this set to a
# CLR version is the most common cause of a failed ASP.NET Core deployment.
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value "AlwaysRunning"
Write-Ok "Set managed runtime to 'No Managed Code'."

$siteExists = Test-Path "IIS:\Sites\$SiteName"
if ($siteExists) {
    Write-Step "Stopping the running site before copying files"
    try { Stop-Website -Name $SiteName } catch { Write-Warn "Site was not running." }
    try { Stop-WebAppPool -Name $AppPoolName } catch { Write-Warn "Pool was not running." }


    Start-Sleep -Seconds 2
    Write-Ok "Stopped."
}


Write-Step "Copying published files to $TargetPath"

if (-not (Test-Path $TargetPath)) {
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}


robocopy $PublishPath $TargetPath /MIR /NFL /NDL /NJH /NJS /NP /XD logs | Out-Null


if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE." }
Write-Ok "Files copied."

$logPath = Join-Path $TargetPath "logs"
if (-not (Test-Path $logPath)) { New-Item -ItemType Directory -Path $logPath -Force | Out-Null }


Write-Step "Setting folder permissions"


$poolIdentity = "IIS AppPool\$AppPoolName"
icacls $TargetPath /grant "${poolIdentity}:(OI)(CI)(RX)" /T /C /Q | Out-Null
icacls $logPath    /grant "${poolIdentity}:(OI)(CI)(M)"  /T /C /Q | Out-Null
Write-Ok "Granted read and execute on the application, and write on logs."


Write-Step "Configuring the website"

if (-not $siteExists) {
    New-Website -Name $SiteName -Port $Port -PhysicalPath $TargetPath `
                -ApplicationPool $AppPoolName | Out-Null
    Write-Ok "Created site '$SiteName' on port $Port."
} else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $TargetPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName
    Write-Ok "Updated existing site '$SiteName'."
}


Write-Step "Opening the firewall port"


$ruleName = "VoltLink API (TCP $Port)"
if (-not (Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP `
                        -LocalPort $Port -Action Allow -Profile Private | Out-Null
    Write-Ok "Created inbound rule for TCP $Port on private networks."
} else {
    Write-Ok "Firewall rule already present."
}


Write-Step "Starting the site"

Start-WebAppPool -Name $AppPoolName
Start-Website -Name $SiteName
Write-Ok "Started."

Write-Step "Verifying the deployment"

$healthUrl = "http://localhost:$Port/api/v1/health"
$ok = $false


foreach ($attempt in 1..10) {
    try {
        $response = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 15
        Write-Ok ("Health check passed: status=" + $response.status + ", database=" + $response.database)
        $ok = $true
        break
    } catch {
        Start-Sleep -Seconds 3
    }
}

if (-not $ok) {
    Write-Warn "Health check did not succeed."
    Write-Warn "Check the start-up log written to: $logPath"
    throw "Deployment completed but the API did not respond at $healthUrl."
}

$lanIp = (Get-NetIPAddress -AddressFamily IPv4 |
          Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } |
          Select-Object -First 1 -ExpandProperty IPAddress)

Write-Host "`n=============================================================" -ForegroundColor Green
Write-Host " VoltLink API deployed successfully" -ForegroundColor Green
Write-Host "=============================================================" -ForegroundColor Green
Write-Host " Swagger UI    : http://localhost:$Port/swagger"
Write-Host " Health check  : $healthUrl"
Write-Host " From a phone  : http://${lanIp}:$Port/api/v1"
Write-Host " Emulator host : http://10.0.2.2:$Port/api/v1"
Write-Host " Files         : $TargetPath"
Write-Host " Start-up logs : $logPath"
Write-Host "=============================================================`n" -ForegroundColor Green
