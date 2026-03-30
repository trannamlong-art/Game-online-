$filePath = "Test.csproj"
$content = Get-Content $filePath
$lineNum = 0
$targetLine = -1

foreach ($line in $content) {
    $lineNum++
    if ($line -match 'Library\\ScriptAssemblies\\UnityEngine.UI.dll') {
        $targetLine = $lineNum
        break
    }
}

if ($targetLine -gt 0) {
    Write-Host "Found UnityEngine.UI reference at line $targetLine"
    for ($i = [Math]::Max(0, $targetLine - 2); $i -lt [Math]::Min($content.Count, $targetLine + 5); $i++) {
        Write-Host "$($i+1): $($content[$i])"
    }
} else {
    Write-Host "UnityEngine.UI reference not found"
}
