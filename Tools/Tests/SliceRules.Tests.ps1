param([string]$ProjectRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference = 'Stop'
# Run in a fresh PowerShell process so Add-Type never reuses an earlier assembly.
Add-Type -Path @(
    (Join-Path $ProjectRoot 'Assets/_VP515/Scripts/Slice/Core/SliceRules.cs'),
    (Join-Path $ProjectRoot 'Assets/Editor/VP515Slice/SliceRuleTests.cs')
)
$report = [VP515.Slice.SliceRuleTests]::Run()
Write-Output $report
if ($report -notmatch 'RESULT \d+ passed, 0 failed$') { exit 1 }
exit 0
