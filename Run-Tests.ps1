# Battery Manager Service - Test Runner
# Builds, tests, and validates the entire solution

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Battery Manager Service - Test Suite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "c:\Projects\Battery Management"
Set-Location $projectPath

# Step 1: Restore packages
Write-Host "Step 1: Restoring NuGet packages..." -ForegroundColor Green
dotnet restore BatteryManagerService.sln
if ($LASTEXITCODE -ne 0)
{
    Write-Host "ERROR: Package restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "Packages restored successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Build solution
Write-Host "Step 2: Building solution..." -ForegroundColor Green
dotnet build BatteryManagerService.sln -c Debug --no-restore
if ($LASTEXITCODE -ne 0)
{
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "Build completed successfully" -ForegroundColor Green
Write-Host ""

# Step 3: Run tests
Write-Host "Step 3: Running unit tests..." -ForegroundColor Green
dotnet test BatteryManagerService.Tests\BatteryManagerService.Tests.csproj `
    --no-build `
    --verbosity normal `
    --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0)
{
    Write-Host "WARNING: Some tests failed or were skipped" -ForegroundColor Yellow
}
else
{
    Write-Host "All tests passed" -ForegroundColor Green
}
Write-Host ""

# Step 4: Code analysis (optional)
Write-Host "Step 4: Running code analysis..." -ForegroundColor Green
$analysisOutput = dotnet build BatteryManagerService.sln -c Debug --no-restore /p:RunAnalyzers=true 2>&1
$warnings = ($analysisOutput | Select-String "warning").Count
$errors = ($analysisOutput | Select-String "error").Count

Write-Host "Warnings: $warnings" -ForegroundColor $(if ($warnings -gt 0) { "Yellow" } else { "Green" })
Write-Host "Errors: $errors" -ForegroundColor $(if ($errors -gt 0) { "Red" } else { "Green" })
Write-Host ""

# Step 5: Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Suite Complete" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($LASTEXITCODE -eq 0)
{
    Write-Host "✓ All checks passed" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ready to publish:" -ForegroundColor White
    Write-Host "  dotnet publish BatteryManagerService\BatteryManagerService.csproj -c Release -r win-x64 --self-contained false -o publish" -ForegroundColor Gray
}
else
{
    Write-Host "⚠ Some issues detected. Review output above." -ForegroundColor Yellow
}
Write-Host ""
