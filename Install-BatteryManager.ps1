# Battery Manager Service - Installation Script
# Run as Administrator

$serviceName = "BatteryManagerService"
$displayName = "Battery Manager Service"
$description = "Monitors battery levels and manages charging thresholds to optimize battery health"
$projectPath = "c:\Projects\Battery Management"
$publishPath = Join-Path $projectPath "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Battery Manager Service - Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check for admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin)
{
    Write-Host "ERROR: This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Step 1: Building the application..." -ForegroundColor Green
try
{
    Set-Location $projectPath
    dotnet publish BatteryManagerService\BatteryManagerService.csproj -c Release -r win-x64 --self-contained false -o publish
    
    if ($LASTEXITCODE -ne 0)
    {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    
    Write-Host "Build completed successfully" -ForegroundColor Green
}
catch
{
    Write-Host "ERROR: Failed to build application: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Checking if service already exists..." -ForegroundColor Green
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($existingService)
{
    Write-Host "Service already exists. Stopping and removing..." -ForegroundColor Yellow
    
    if ($existingService.Status -eq "Running")
    {
        Stop-Service -Name $serviceName -Force
        Start-Sleep -Seconds 2
    }
    
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
    Write-Host "Existing service removed" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 3: Installing service..." -ForegroundColor Green
try
{
    $exePath = Join-Path $publishPath "BatteryManagerService.exe"
    
    if (-not (Test-Path $exePath))
    {
        throw "Executable not found at: $exePath"
    }
    
    sc.exe create $serviceName binPath= $exePath start= auto DisplayName= $displayName
    
    if ($LASTEXITCODE -ne 0)
    {
        throw "Service creation failed with exit code $LASTEXITCODE"
    }
    
    # Set service description
    sc.exe description $serviceName $description
    
    Write-Host "Service installed successfully" -ForegroundColor Green
}
catch
{
    Write-Host "ERROR: Failed to install service: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 4: Starting service..." -ForegroundColor Green
try
{
    Start-Service -Name $serviceName
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name $serviceName
    if ($service.Status -eq "Running")
    {
        Write-Host "Service started successfully" -ForegroundColor Green
    }
    else
    {
        Write-Host "WARNING: Service installed but not running. Status: $($service.Status)" -ForegroundColor Yellow
    }
}
catch
{
    Write-Host "ERROR: Failed to start service: $_" -ForegroundColor Red
    Write-Host "Check Event Viewer for details" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Service Information:" -ForegroundColor White
Write-Host "  Name: $serviceName" -ForegroundColor Gray
Write-Host "  Status: $((Get-Service -Name $serviceName).Status)" -ForegroundColor Gray
Write-Host "  Startup Type: Automatic" -ForegroundColor Gray
Write-Host "  Install Path: $publishPath" -ForegroundColor Gray
Write-Host ""
Write-Host "Configuration File:" -ForegroundColor White
Write-Host "  $publishPath\appsettings.json" -ForegroundColor Gray
Write-Host ""
Write-Host "Log Files:" -ForegroundColor White
Write-Host "  $publishPath\logs\battery-manager-*.log" -ForegroundColor Gray
Write-Host ""
Write-Host "To view logs:" -ForegroundColor White
Write-Host '  Get-Content "$publishPath\logs\battery-manager-*.log" -Tail 50' -ForegroundColor Gray
Write-Host ""
Write-Host "To stop service:" -ForegroundColor White
Write-Host "  Stop-Service -Name $serviceName" -ForegroundColor Gray
Write-Host ""
Write-Host "To start service:" -ForegroundColor White
Write-Host "  Start-Service -Name $serviceName" -ForegroundColor Gray
Write-Host ""
Write-Host "To uninstall:" -ForegroundColor White
Write-Host "  Run Uninstall-BatteryManager.ps1" -ForegroundColor Gray
Write-Host ""
