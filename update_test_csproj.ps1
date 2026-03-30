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
    # targetLine points to the HintPath line (1-indexed), so the closing </Reference> is at targetLine + 1
    # and the </ItemGroup> is at targetLine + 3
    $itemGroupLine = $targetLine + 2

    # Insert the new Fusion.Runtime reference before the closing </ItemGroup>
    $fusionRef = @"
    <Reference Include="Fusion.Runtime">
      <HintPath>Assets\Photon\Fusion\Assemblies\Fusion.Runtime.dll</HintPath>
      <Private>False</Private>
    </Reference>
"@

    $newContent = @()
    for ($i = 0; $i -lt $content.Count; $i++) {
        $newContent += $content[$i]
        if ($i -eq $itemGroupLine) {
            $newContent += $fusionRef
        }
    }

    $newContent | Set-Content $filePath
    Write-Host "Fusion.Runtime reference added successfully"
} else {
    Write-Host "UnityEngine.UI reference not found"
}
