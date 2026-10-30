<#
    install.ps1 - install MelonLoader + the Chinese-UI RestoryQOL build into
    "ReStory: Chill Electronics Repairs" (Steam app 3812600).

    ASCII only on purpose: Windows PowerShell 5.1 reads .ps1 as ANSI unless the
    file has a BOM, so non-ASCII source would break on Chinese systems.

    Usage:
      powershell -ExecutionPolicy Bypass -File scripts/install.ps1
      powershell -ExecutionPolicy Bypass -File scripts/install.ps1 -GamePath "E:\SteamLibrary\steamapps\common\Restory"
#>
[CmdletBinding()]
param(
    [string]$GamePath,
    [string]$MelonLoaderVersion = "0.7.3"
)

$ErrorActionPreference = "Stop"

function Find-RestoryInstall {
    $candidates = New-Object System.Collections.Generic.List[string]

    foreach ($key in @(
        "HKCU:\Software\Valve\Steam",
        "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam",
        "HKLM:\SOFTWARE\Valve\Steam"
    )) {
        try {
            $props = Get-ItemProperty -Path $key -ErrorAction Stop
            foreach ($name in @("SteamPath", "InstallPath")) {
                $p = $props.$name
                if ($p) { $candidates.Add($p.Replace("/", "\")) | Out-Null }
            }
        } catch { }
    }

    $libraries = New-Object System.Collections.Generic.List[string]
    foreach ($root in $candidates) {
        if ($libraries -notcontains $root) { $libraries.Add($root) | Out-Null }
        $vdf = Join-Path $root "steamapps\libraryfolders.vdf"
        if (Test-Path $vdf) {
            $text = Get-Content -Raw -LiteralPath $vdf
            foreach ($m in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
                $lib = $m.Groups[1].Value.Replace("\\\\", "\")
                if ($libraries -notcontains $lib) { $libraries.Add($lib) | Out-Null }
            }
        }
    }

    foreach ($lib in $libraries) {
        $candidate = Join-Path $lib "steamapps\common\Restory"
        if (Test-Path (Join-Path $candidate "Restory.exe")) { return $candidate }
    }
    return $null
}

if (-not $GamePath) {
    Write-Host "[1/4] Locating the ReStory install..."
    $GamePath = Find-RestoryInstall
    if (-not $GamePath) {
        throw "ReStory not found. Pass -GamePath '<...>\steamapps\common\Restory'."
    }
} else {
    $GamePath = (Resolve-Path -LiteralPath $GamePath).Path
}

$exe = Join-Path $GamePath "Restory.exe"
if (-not (Test-Path $exe)) { throw "Restory.exe not found in '$GamePath'." }
Write-Host "      game dir: $GamePath"

Write-Host "[2/4] Installing MelonLoader v$MelonLoaderVersion if needed..."
$loaderDir = Join-Path $GamePath "MelonLoader"
$loaderDll = Join-Path $GamePath "version.dll"
if ((Test-Path $loaderDir) -and (Test-Path $loaderDll)) {
    Write-Host "      already present, skipping"
} else {
    $zip = Join-Path $env:TEMP "MelonLoader.x64.$MelonLoaderVersion.zip"
    $url = "https://github.com/LavaGang/MelonLoader/releases/download/v$MelonLoaderVersion/MelonLoader.x64.zip"
    Write-Host "      downloading $url"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::ExtractToDirectory($zip, $GamePath, $true)
    Remove-Item -LiteralPath $zip -Force
}

Write-Host "[3/4] Copying RestoryQOL.dll into Mods\ ..."
$mods = Join-Path $GamePath "Mods"
if (-not (Test-Path $mods)) { New-Item -ItemType Directory -Path $mods | Out-Null }
$srcDll = Join-Path (Split-Path -Parent $PSScriptRoot) "dist\RestoryQOL.dll"
if (-not (Test-Path $srcDll)) { throw "dist\RestoryQOL.dll missing - run scripts/build.ps1 first." }
Copy-Item -LiteralPath $srcDll -Destination (Join-Path $mods "RestoryQOL.dll") -Force

Write-Host "[4/4] Done."
Write-Host ""
Write-Host "  Launch the game FROM STEAM (a bare Restory.exe run fails with SteamAPI_Init)."
Write-Host "  In game: press F8 to toggle the menu (title: Chinese UI build)."
Write-Host "  Verify:  $GamePath\MelonLoader\Latest.log  should contain '1 Mod loaded.'"
Write-Host ""
Write-Host "  Note: the game logs 'External interference detected' when it sees the loader."
Write-Host "  Uninstall: delete 'MelonLoader' folder, 'Mods' folder and 'version.dll'."
