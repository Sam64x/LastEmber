param([switch]$Test, [switch]$MusicTest, [switch]$Playtest, [switch]$Capture, [switch]$Editor, [switch]$Run)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$env:DOTNET_ROOT = Join-Path $taskRoot '.tools/dotnet'
$env:DOTNET_CLI_HOME = Join-Path $taskRoot '.tools'
$env:APPDATA = Join-Path $taskRoot '.tools/appdata'
$env:LOCALAPPDATA = $env:APPDATA
$env:NUGET_PACKAGES = Join-Path $taskRoot '.tools/nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$godot = Join-Path $taskRoot '.tools/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
$project = Join-Path $taskRoot 'last-ember'
& "$env:DOTNET_ROOT/dotnet.exe" build "$project/LastEmber.csproj" --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$ErrorActionPreference = 'Continue'
& $godot --headless --editor --path $project --import --quit
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($Test) {
    & $godot --headless --path $project --fixed-fps 60 -- --self-test
    exit $LASTEXITCODE
}
if ($Capture) {
    & $godot --path $project --fixed-fps 60 -- --capture
    exit $LASTEXITCODE
}
if ($MusicTest) {
    & $godot --headless --path $project --fixed-fps 60 -- --music-test
    exit $LASTEXITCODE
}
if ($Playtest) {
    & $godot --headless --path $project --fixed-fps 60 -- --playtest
    exit $LASTEXITCODE
}
if ($Editor) { & $godot --editor --path $project }
elseif ($Run) { & $godot --path $project }
