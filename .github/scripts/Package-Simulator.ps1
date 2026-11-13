#requires -Version 7.0
<#
.SYNOPSIS
  Validate a built Windows simulator player directory and package it as a ZIP.

.DESCRIPTION
  Fail-closed packaging for the Windows simulator build. Verifies that every file a
  runnable player needs is present and non-empty, recursively rejects license
  artifacts (.lfx/.lic/.rtlfx/.ulf and .meta companions) and reparse points,
  then writes the whole player directory plus a short Japanese README.txt into
  a ZIP archive via System.IO.Compression (ZIP64-capable).

  This script never launches Unity and never copies license material into the
  archive.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PlayerDirectory,

    [Parameter(Mandatory = $true)]
    [string]$ArchivePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Stop-Packaging {
    param([string]$Message)
    throw "Package-Simulator: $Message"
}

# --- Resolve and sanity-check paths -----------------------------------------

if (-not (Test-Path -LiteralPath $PlayerDirectory -PathType Container)) {
    Stop-Packaging "player directory not found: $PlayerDirectory"
}
$playerRoot = (Resolve-Path -LiteralPath $PlayerDirectory).Path

$archiveFull = [IO.Path]::GetFullPath($ArchivePath)
$playerFullWithSep = $playerRoot.TrimEnd([IO.Path]::DirectorySeparatorChar,
                                       [IO.Path]::AltDirectorySeparatorChar) +
                    [IO.Path]::DirectorySeparatorChar
if ($archiveFull.StartsWith($playerFullWithSep, [StringComparison]::OrdinalIgnoreCase)) {
    Stop-Packaging "archive path must not be inside the player directory: $archiveFull"
}
if (Test-Path -LiteralPath $archiveFull -PathType Leaf) {
    Stop-Packaging "archive already exists; refusing to overwrite: $archiveFull"
}

# --- Required player files (must exist and be non-empty) ---------------------

$requiredFiles = @(
    'OperaSim.exe',
    'UnityPlayer.dll',
    'OperaSim_Data/Managed/Assembly-CSharp.dll',
    'OperaSim_Data/Plugins/x86_64/agxCore.dll',
    'OperaSim_Data/Plugins/x86_64/agxPhysics.dll',
    'OperaSim_Data/Plugins/x86_64/agxTerrain.dll',
    'OperaSim_Data/Plugins/x86_64/agxDotNetRuntime.dll',
    'OperaSim_Data/Plugins/x86_64/agx/Components/agx/Referenced.agxEntity'
)

foreach ($rel in $requiredFiles) {
    $candidate = Join-Path $playerRoot ($rel -replace '/', [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        Stop-Packaging "required file missing: $rel"
    }
    if ((Get-Item -LiteralPath $candidate).Length -le 0) {
        Stop-Packaging "required file is empty: $rel"
    }
}

# --- Recursive safety scan: license artifacts and reparse points -------------

$licensePattern = '\.(lfx|lic|rtlfx|ulf)(\.meta)?$'
$allFiles = @(Get-ChildItem -LiteralPath $playerRoot -Recurse -Force -File)

$reparse = @(Get-ChildItem -LiteralPath $playerRoot -Recurse -Force |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint })
if ($reparse.Count -gt 0) {
    Stop-Packaging "reparse point (symlink/junction) found in player: $($reparse[0].FullName)"
}

$licenseHits = @($allFiles | Where-Object { $_.Name -match $licensePattern })
if ($licenseHits.Count -gt 0) {
    $names = ($licenseHits | Select-Object -First 5 | ForEach-Object { $_.Name }) -join ', '
    Stop-Packaging "license artifact detected in player output ($($licenseHits.Count) file(s), e.g. $names)"
}

# --- Japanese README injected at archive root -------------------------------

$readmePath = Join-Path $playerRoot 'README.txt'
if (Test-Path -LiteralPath $readmePath -PathType Leaf) {
    Stop-Packaging "player already contains README.txt; refusing to overwrite: $readmePath"
}
$readmeText = @'
OperaSim Windows プレイヤー (GitHub Actions ビルド)

使い方:
- このフォルダは分解・移動せず、フォルダごと保持してください。
- Unity Editor は不要です。OperaSim.exe をこのフォルダ内で
  そのまま起動してください (カレントディレクトリをこの
  フォルダにして実行するのが確実です)。
- AGX Dynamics のライセンスは実行する各 Windows PC で
  有効なものが必要です。この ZIP にはライセンスを同梱
  していません。
- 起動後 HUD の ROS 接続設定で Linux 側 IP アドレスと
  ポート 10000 を入力して接続してください。

注意:
- これは未検証のビルドです。配布前にライセンス済み
  Windows + Linux ROS のスモークテストが必要です。
'@

# --- Archive -----------------------------------------------------------------

$archiveDir = Split-Path $archiveFull -Parent
if (-not [string]::IsNullOrEmpty($archiveDir)) {
    New-Item -ItemType Directory -Force -Path $archiveDir | Out-Null
}

$utf8NoBom = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($readmePath, $readmeText, $utf8NoBom)
try {
    [IO.Compression.ZipFile]::CreateFromDirectory(
        $playerRoot, $archiveFull,
        [IO.Compression.CompressionLevel]::Optimal, $false)
} finally {
    Remove-Item -LiteralPath $readmePath -Force -ErrorAction SilentlyContinue
}

Write-Host "Package-Simulator: wrote $archiveFull"
