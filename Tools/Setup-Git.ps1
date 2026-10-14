param([string]$UnityEditorPath)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath (Join-Path $repoRoot 'ProjectSettings/ProjectVersion.txt'))) {
    throw 'This script must be inside the VP515 Unity repository.'
}
if (-not (Test-Path -LiteralPath (Join-Path $repoRoot '.git'))) {
    throw 'Clone or initialise the Git repository before running this script.'
}
& git -C $repoRoot lfs install --local
if ($LASTEXITCODE -ne 0) { throw 'Git LFS is required.' }
& git -C $repoRoot config core.autocrlf false
& git -C $repoRoot config pull.ff only
& git -C $repoRoot config fetch.prune true
if ($UnityEditorPath) {
    $editorExecutable = Get-Item -LiteralPath $UnityEditorPath
    $mergeTool = Join-Path $editorExecutable.DirectoryName 'Data/Tools/UnityYAMLMerge.exe'
    if (-not (Test-Path -LiteralPath $mergeTool)) { throw 'UnityYAMLMerge was not found beside this Editor.' }
    $mergeDriver = '"' + $mergeTool.Replace('\', '/') + '" merge -p %O %B %A %A'
    & git -C $repoRoot config merge.unityyamlmerge.name 'Unity Smart Merge'
    & git -C $repoRoot config merge.unityyamlmerge.driver $mergeDriver
    & git -C $repoRoot config merge.unityyamlmerge.recursive binary
}
Write-Output 'Repository-local Git/LFS settings are ready. Global Git settings were not changed.'
