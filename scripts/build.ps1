param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    dotnet restore SyncSound.sln
    dotnet build SyncSound.sln -c $Configuration --no-restore
}
finally {
    Pop-Location
}
