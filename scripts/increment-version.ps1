# Get the root of the git repository
$projectRoot = git rev-parse --show-toplevel
$csprojPath = Join-Path $projectRoot 'EasyState.Blazor\EasyState.Blazor.csproj'

# Read the project file content
$content = Get-Content $csprojPath -Raw

# Regex to find the version
$regex = '<Version>(\d+\.\d+\.\d+)</Version>'
$match = [regex]::Match($content, $regex)

if ($match.Success) {
    $oldVersion = $match.Groups[1].Value
    $versionParts = $oldVersion.Split('.')
    
    # Increment the patch version
    $newPatch = [int]$versionParts[2] + 1
    $newVersion = "$($versionParts[0]).$($versionParts[1]).$newPatch"
    
    # Replace the version in the content
    $oldVersionLine = $match.Value
    $newVersionLine = "<Version>$newVersion</Version>"
    $newContent = $content.Replace($oldVersionLine, $newVersionLine)
    
    # Write the updated content back to the file
    Set-Content -Path $csprojPath -Value $newContent
    
    # Stage the modified project file
    git add $csprojPath
    
    Write-Host "Version incremented to $newVersion"
}
else {
    Write-Host "Could not find version tag in $csprojPath"
    exit 1
}

exit 0
