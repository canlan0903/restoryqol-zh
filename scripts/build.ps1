<#
    build.ps1 - rebuild dist\RestoryQOL.dll from src/.

    Needs: .NET SDK 8+, a legitimate install of the game (its managed assemblies
    are referenced), and MelonLoader installed in that folder.

    The project also references BepInEx 5 at compile time (the upstream source
    ships a universal BepInEx + MelonLoader entry point). BepInEx is NOT needed
    at runtime for this mod; if you don't already have BepInEx 5 unpacked, this
    script fetches the official release into a cache folder and uses it purely
    as a compile-time reference.

    Usage:
      powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -GamePath "E:\SteamLibrary\steamapps\common\Restory"
      powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -GamePath "..." -BepInExPath "D:\bep5\BepInEx"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GamePath,
    [string]$BepInExPath,
    [string]$BepInExVersion = "5.4.23.6"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "src"
$dist = Join-Path $root "dist"

if (-not (Test-Path (Join-Path $GamePath "Restory.exe"))) {
    throw "Restory.exe not found under '$GamePath'."
}
if (-not (Test-Path (Join-Path $GamePath "MelonLoader\net35\MelonLoader.dll"))) {
    throw "MelonLoader is not installed in '$GamePath'. Run scripts/install.ps1 once."
}

if (-not $BepInExPath) {
    $candidate = Join-Path $GamePath "BepInEx"
    if (Test-Path (Join-Path $candidate "core\BepInEx.dll")) { $BepInExPath = $candidate }
}

if (-not $BepInExPath) {
    $cache = Join-Path $root ".cache\bepinex-$BepInExVersion"
    if (-not (Test-Path (Join-Path $cache "BepInEx\core\BepInEx.dll"))) {
        Write-Host "Fetching BepInEx $BepInExVersion (compile-time reference only)..."
        $zip = Join-Path $env:TEMP "BepInEx_win_x64_$BepInExVersion.zip"
        $url = "https://github.com/BepInEx/BepInEx/releases/download/v$BepInExVersion/BepInEx_win_x64_$BepInExVersion.zip"
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
        New-Item -ItemType Directory -Path $cache -Force | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($zip, $cache, $true)
        Remove-Item -LiteralPath $zip -Force
    }
    $BepInExPath = Join-Path $cache "BepInEx"
}

Write-Host "GamePath    : $GamePath"
Write-Host "BepInExPath : $BepInExPath (compile only)"

$buildArgs = @("build", "-c", "Release", "-p:GamePath=$GamePath", "-p:BepInExPath=$BepInExPath")

Push-Location $src
try {
    & dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}

$built = Join-Path $src "bin\Release\netstandard2.1\RestoryQOL.dll"
if (-not (Test-Path $built)) { throw "Build output missing: $built" }
if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }
Copy-Item -LiteralPath $built -Destination (Join-Path $dist "RestoryQOL.dll") -Force
Write-Host "OK -> dist\RestoryQOL.dll"
