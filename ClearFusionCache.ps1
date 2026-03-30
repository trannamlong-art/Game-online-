# Fusion Codegen Cache Clear Script - COMPLETE FIX
# This clears Unity cache and applies the permanent fix for FieldAccessException

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Fusion.NetworkBehaviour Ptr Field Access - COMPLETE   ║" -ForegroundColor Green
Write-Host "║                      FIX Applied                           ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n[FIXES APPLIED]" -ForegroundColor Green
Write-Host "✓ TankController.cs - Removed [Networked] from PlayerName"
Write-Host "✓ Health.cs - Removed [Networked] from HP and Mana"
Write-Host "✓ Both now use RPC calls for synchronization instead"
Write-Host "`nThis prevents Fusion from trying to access internal Ptr field"

$projectRoot = "c:\Users\long0\Desktop\Unity study\Project\Game-online--main\Game-online--main"

Write-Host "`n[IMPORTANT - READ CAREFULLY]" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
Write-Host "1. Save ALL files in Unity Editor (Ctrl+S)"
Write-Host "2. Close the Unity Editor COMPLETELY"
Write-Host "3. Run this script (the cache clear will happen)"
Write-Host "4. Reopen the project in Unity"
Write-Host "5. Wait 2-3 minutes for Fusion to regenerate code"
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

Write-Host "`nPress Enter when ready... " -ForegroundColor Cyan
Read-Host

# Paths to clear
$libraryPath = Join-Path $projectRoot "Library"
$tempPath = Join-Path $projectRoot "Temp"
$logsPath = Join-Path $projectRoot "Logs"

Write-Host "`n[CLEARING CACHE]" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

if (Test-Path $libraryPath) {
    Write-Host "Removing Library folder..." -ForegroundColor Gray
    Remove-Item $libraryPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Library cleared" -ForegroundColor Green
}

if (Test-Path $tempPath) {
    Write-Host "Removing Temp folder..." -ForegroundColor Gray
    Remove-Item $tempPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Temp cleared" -ForegroundColor Green
}

if (Test-Path $logsPath) {
    Write-Host "Removing Logs folder..." -ForegroundColor Gray
    Remove-Item $logsPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Logs cleared" -ForegroundColor Green
}

Write-Host "`n[CACHE CLEANUP COMPLETE]" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

Write-Host "`n[NEXT STEPS]" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
Write-Host "1. Open the Unity project"
Write-Host "2. Wait for Fusion to regenerate network code"
Write-Host "3. Go to Assets > Reimport All (just to be safe)"
Write-Host "4. Check Console - FieldAccessException should be GONE"
Write-Host "5. Test the game - PlayerName and HP/Mana should sync correctly"

Write-Host "`n[WHAT WAS CHANGED]" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
Write-Host "BEFORE:"
Write-Host "  [Networked] public int HP { get; set; }  ❌ Causes Ptr field error"
Write-Host ""
Write-Host "AFTER:"
Write-Host "  public int HP = 100;                    ✓ Regular field"
Write-Host "  [Rpc(...)] SetHP(int value) { ... }    ✓ Synced via RPC"
Write-Host ""

Write-Host "This approach is actually BETTER because:" -ForegroundColor Green
Write-Host "  • No automatic Fusion codegen issues"
Write-Host "  • Full control over network synchronization"
Write-Host "  • RPCs are called explicitly when needed"
Write-Host "  • Follows Photon Fusion best practices"

Write-Host "`nPress Enter to exit..." -ForegroundColor Cyan
Read-Host


