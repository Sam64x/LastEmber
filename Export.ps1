$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
& (Join-Path $taskRoot 'Build.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$distribution = Join-Path $taskRoot 'dist/LastEmber-Windows'
New-Item -ItemType Directory -Force $distribution | Out-Null
$godot = Join-Path $taskRoot '.tools/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$ErrorActionPreference = 'Continue'
& $godot --headless --path (Join-Path $taskRoot 'last-ember') --export-release 'Windows Desktop' (Join-Path $distribution 'LastEmber.exe')
exit $LASTEXITCODE
