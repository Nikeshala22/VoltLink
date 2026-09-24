
[CmdletBinding()]
param(

    [string]$SiteName   = "VoltLinkWeb",

    
    [int]$Port          = 8081,

   
    [string]$TargetPath = "C:\inetpub\VoltLinkWeb",

  
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$RepoRoot    = Split-Path -Parent $PSScriptRoot
$WebRoot     = Join-Path $RepoRoot "web"
$DistPath    = Join-Path $WebRoot "dist"
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

Import-Module WebAdministration -ErrorAction Stop
Write-Ok "IIS WebAdministration module loaded."


if (-not $SkipBuild) {
    Write-Step "Building the React application"

    Push-Location $WebRoot
    try {
      
        npm run build
        if ($LASTEXITCODE -ne 0) { throw "npm run build failed with exit code $LASTEXITCODE." }
    } finally {
        Pop-Location
    }

    Write-Ok "Built to $DistPath."
} else {
    Write-Step "Skipping build, using existing output"
}

if (-not (Test-Path (Join-Path $DistPath "index.html"))) {
    throw "No build output found at $DistPath. Run without -SkipBuild."
}


if (-not (Test-Path (Join-Path $DistPath "web.config"))) {
    Write-Warn "web.config is missing from the build output; deep links may return 404."
}


Write-Step "Configuring the application pool"

if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
    Write-Ok "Created application pool '$AppPoolName'."
} else {
    Write-Ok "Application pool '$AppPoolName' already exists."
}


Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
Write-Ok "Set managed runtime to 'No Managed Code'."


$siteExists = Test-Path "IIS:\Sites\$SiteName"
if ($siteExists) {
    Write-Step "Stopping the running site before copying files"
    try { Stop-Website -Name $SiteName } catch { Write-Warn "Site was not running." }
    Start-Sleep -Seconds 1
    Write-Ok "Stopped."
}

Write-Step "Copying the built site to $TargetPath"

if (-not (Test-Path $TargetPath)) {
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}


robocopy $DistPath $TargetPath /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE." }
Write-Ok "Files copied."


Write-Step "Setting folder permissions"

$poolIdentity = "IIS AppPool\$AppPoolName"
icacls $TargetPath /grant "${poolIdentity}:(OI)(CI)(RX)" /T /C /Q | Out-Null
Write-Ok "Granted read and execute to the application pool identity."


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

$ruleName = "VoltLink Web (TCP $Port)"
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

$url = "http://localhost:$Port/"
$ok = $false

foreach ($attempt in 1..8) {
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Ok "Site responded with HTTP 200."
            $ok = $true
            break
        }
    } catch {
        Start-Sleep -Seconds 2
    }
}

if (-not $ok) { throw "Deployment completed but the site did not respond at $url." }

$lanIp = (Get-NetIPAddress -AddressFamily IPv4 |
          Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } |
          Select-Object -First 1 -ExpandProperty IPAddress)

Write-Host "`n=============================================================" -ForegroundColor Green
Write-Host " VoltLink Web deployed successfully" -ForegroundColor Green
Write-Host "=============================================================" -ForegroundColor Green
Write-Host " Web application : http://localhost:$Port"
Write-Host " On the network  : http://${lanIp}:$Port"
Write-Host " API it calls    : see web\.env.production"
Write-Host " Files           : $TargetPath"
Write-Host ""
Write-Host " IIS runs as a Windows service, so both sites now start"
Write-Host " automatically with the machine. Nothing to launch by hand."
Write-Host "=============================================================`n" -ForegroundColor Green
