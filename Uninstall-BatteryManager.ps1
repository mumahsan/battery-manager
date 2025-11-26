# Battery Manager Service - Uninstallation Script
# Run as Administrator

$serviceName = "BatteryManagerService"
$projectPath = "c:\Projects\Battery Management"
$publishPath = Join-Path $projectPath "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Battery Manager Service - Uninstaller" -ForegroundColor Cyan
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

Write-Host "Step 1: Checking if service exists..." -ForegroundColor Green
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if (-not $service)
{
    Write-Host "Service not found. Nothing to uninstall." -ForegroundColor Yellow
}
else
{
    Write-Host "Service found. Status: $($service.Status)" -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "Step 2: Stopping service..." -ForegroundColor Green
    try
    {
        if ($service.Status -eq "Running")
        {
            Stop-Service -Name $serviceName -Force
            Start-Sleep -Seconds 3
            Write-Host "Service stopped" -ForegroundColor Green
        }
        else
        {
            Write-Host "Service already stopped" -ForegroundColor Gray
        }
    }
    catch
    {
        Write-Host "WARNING: Failed to stop service: $_" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Step 3: Removing service..." -ForegroundColor Green
    try
    {
        sc.exe delete $serviceName
        
        if ($LASTEXITCODE -eq 0)
        {
            Write-Host "Service removed successfully" -ForegroundColor Green
        }
        else
        {
            Write-Host "WARNING: Service deletion returned exit code $LASTEXITCODE" -ForegroundColor Yellow
        }
    }
    catch
    {
        Write-Host "ERROR: Failed to remove service: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Step 4: Cleaning up files..." -ForegroundColor Green
$deleteFiles = Read-Host "Do you want to delete the application files at '$publishPath'? (y/n)"

if ($deleteFiles -eq "y" -or $deleteFiles -eq "Y")
{
    try
    {
        if (Test-Path $publishPath)
        {
            Remove-Item $publishPath -Recurse -Force
            Write-Host "Files deleted" -ForegroundColor Green
        }
        else
        {
            Write-Host "Publish directory not found" -ForegroundColor Gray
        }
    }
    catch
    {
        Write-Host "ERROR: Failed to delete files: $_" -ForegroundColor Red
    }
}
else
{
    Write-Host "Files kept at: $publishPath" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Uninstallation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
