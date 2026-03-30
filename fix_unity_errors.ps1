# Unity Project Error Fix Script
# This script fixes the TypeLoadExceptions and other import errors

$projectRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$libraryFolder = Join-Path $projectRoot "Library"
$tempFolder = Join-Path $projectRoot "Temp"
$logsFolder = Join-Path $projectRoot "Logs"

Write-Host "Starting Unity Project Cleanup and Recovery..." -ForegroundColor Green
Write-Host "Project Root: $projectRoot" -ForegroundColor Cyan

# Close Unity if running (optional - manual step needed)
Write-Host "`n[MANUAL STEP] Please close Unity Editor if it's running before proceeding." -ForegroundColor Yellow
Write-Host "Press Enter to continue..." -ForegroundColor Yellow
Read-Host

# Remove cache folders
Write-Host "`nRemoving cache folders..." -ForegroundColor Yellow

if (Test-Path $libraryFolder) {
    Write-Host "Removing Library folder..." -ForegroundColor Gray
    Remove-Item $libraryFolder -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Library folder removed" -ForegroundColor Green
}

if (Test-Path $tempFolder) {
    Write-Host "Removing Temp folder..." -ForegroundColor Gray
    Remove-Item $tempFolder -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Temp folder removed" -ForegroundColor Green
}

if (Test-Path $logsFolder) {
    Write-Host "Removing Logs folder..." -ForegroundColor Gray
    Remove-Item $logsFolder -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Logs folder removed" -ForegroundColor Green
}

Write-Host "`n[NEXT STEPS]" -ForegroundColor Green
Write-Host "1. Open the project in Unity Editor again"
Write-Host "2. Allow Unity to regenerate the Library folder (this may take several minutes)"
Write-Host "3. Check the Console for any remaining errors"
Write-Host "`nIf errors persist, try reducing Splines package reference..."
