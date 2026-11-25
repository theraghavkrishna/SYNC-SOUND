param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    dotnet test SyncSound.sln -c $Configuration --no-build --verbosity normal
}
finally {
    Pop-Location
}
