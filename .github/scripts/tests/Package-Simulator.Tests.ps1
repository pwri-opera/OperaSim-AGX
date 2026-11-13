#requires -Version 7.0
<#
.SYNOPSIS
  Fixture-based tests for .github/scripts/Package-Simulator.ps1.
  Uses real temp player directories and real ZIP output; no Unity required.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

$testsDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $testsDir '..\..\..')).Path
$packageScript = Join-Path $repoRoot '.github/scripts/Package-Simulator.ps1'

$script:Passed = 0
$script:Failed = 0
$script:Failures = [System.Collections.Generic.List[string]]::new()

function Assert-That {
    param([bool]$Condition, [string]$Name, [string]$Detail = '')
    if ($Condition) {
        $script:Passed++
        Write-Host "  PASS  $Name"
    } else {
        $script:Failed++
        $script:Failures.Add("$Name$(if ($Detail) { " — $Detail" })")
        Write-Host "  FAIL  $Name$(if ($Detail) { " — $Detail" })"
    }
}

function New-TestRoot {
    $root = Join-Path ([IO.Path]::GetTempPath()) ("opera-pkg-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    return $root
}

function Write-FixtureFile {
    param([string]$Root, [string]$RelativePath, [string]$Content = 'fixture-bytes')
    $path = Join-Path $Root ($RelativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
    New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent) | Out-Null
    [IO.File]::WriteAllText($path, $Content)
}

function New-ValidPlayer {
    param([string]$Root)
    $required = @(
        'OperaSim.exe',
        'UnityPlayer.dll',
        'OperaSim_Data/Managed/Assembly-CSharp.dll',
        'OperaSim_Data/Plugins/x86_64/agxCore.dll',
        'OperaSim_Data/Plugins/x86_64/agxPhysics.dll',
        'OperaSim_Data/Plugins/x86_64/agxTerrain.dll',
        'OperaSim_Data/Plugins/x86_64/agxDotNetRuntime.dll',
        'OperaSim_Data/Plugins/x86_64/agx/Components/agx/Referenced.agxEntity'
    )
    foreach ($rel in $required) {
        Write-FixtureFile -Root $Root -RelativePath $rel -Content "content:$rel"
    }
    # nested arbitrary file and hidden file must also be packaged
    Write-FixtureFile -Root $Root -RelativePath 'OperaSim_Data/Plugins/x86_64/agx/Resources/data.bin' -Content 'nested-data'
    Write-FixtureFile -Root $Root -RelativePath 'OperaSim_Data/.hiddenfile' -Content 'hidden-data'
}

function Invoke-Packager {
    param([string]$Player, [string]$Archive)
    $threw = $false
    $errorText = ''
    try {
        & $packageScript -PlayerDirectory $Player -ArchivePath $Archive
    } catch {
        $threw = $true
        $errorText = $_.Exception.Message
    }
    return [pscustomobject]@{ Threw = $threw; Error = $errorText }
}

function Get-ZipEntries {
    param([string]$ZipPath)
    $zip = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        return ,@($zip.Entries | ForEach-Object { $_.FullName })
    } finally {
        $zip.Dispose()
    }
}

function Get-ZipEntryText {
    param([string]$ZipPath, [string]$EntryName)
    $zip = [IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $entry = $zip.GetEntry($EntryName)
        if ($null -eq $entry) { return $null }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally {
        $zip.Dispose()
    }
}

$workRoots = [System.Collections.Generic.List[string]]::new()

try {
    Write-Host '== Package-Simulator: valid fixture =='
    {
        $root = New-TestRoot; $workRoots.Add($root)
        $player = Join-Path $root 'player'
        New-Item -ItemType Directory -Force -Path $player | Out-Null
        New-ValidPlayer -Root $player
        $zip = Join-Path $root 'out/OperaSim-Windows.zip'
        $result = Invoke-Packager -Player $player -Archive $zip
        Assert-That (-not $result.Threw) "valid fixture packages without error" $result.Error
        Assert-That (Test-Path $zip -PathType Leaf) 'archive created'
        if (Test-Path $zip -PathType Leaf) {
            $entries = Get-ZipEntries $zip
            foreach ($need in @(
                'OperaSim.exe', 'UnityPlayer.dll',
                'OperaSim_Data/Managed/Assembly-CSharp.dll',
                'OperaSim_Data/Plugins/x86_64/agxCore.dll',
                'OperaSim_Data/Plugins/x86_64/agxPhysics.dll',
                'OperaSim_Data/Plugins/x86_64/agxTerrain.dll',
                'OperaSim_Data/Plugins/x86_64/agxDotNetRuntime.dll',
                'OperaSim_Data/Plugins/x86_64/agx/Components/agx/Referenced.agxEntity',
                'OperaSim_Data/Plugins/x86_64/agx/Resources/data.bin',
                'OperaSim_Data/.hiddenfile',
                'README.txt')) {
                Assert-That ($entries -contains $need) "zip contains $need"
            }
            Assert-That ((Get-ZipEntryText $zip 'OperaSim.exe') -eq 'content:OperaSim.exe') 'exe content preserved'
            Assert-That ((Get-ZipEntryText $zip 'OperaSim_Data/Plugins/x86_64/agx/Components/agx/Referenced.agxEntity') -eq 'content:OperaSim_Data/Plugins/x86_64/agx/Components/agx/Referenced.agxEntity') 'runtime resource content preserved'
            $readme = Get-ZipEntryText $zip 'README.txt'
            Assert-That ($null -ne $readme -and $readme.Length -gt 0) 'README.txt present and non-empty'
            Assert-That ($readme -match '10000') 'README mentions ROS port 10000'
        }
    }.Invoke()

    Write-Host '== Package-Simulator: missing required files =='
    foreach ($missing in @(
        'OperaSim_Data/Plugins/x86_64/agx/Components/agx/Referenced.agxEntity',
        'OperaSim_Data/Plugins/x86_64/agxCore.dll',
        'OperaSim.exe')) {
        $root = New-TestRoot; $workRoots.Add($root)
        $player = Join-Path $root 'player'
        New-Item -ItemType Directory -Force -Path $player | Out-Null
        New-ValidPlayer -Root $player
        $victim = Join-Path $player ($missing -replace '/', [IO.Path]::DirectorySeparatorChar)
        Remove-Item -LiteralPath $victim -Force
        $zip = Join-Path $root 'out.zip'
        $result = Invoke-Packager -Player $player -Archive $zip
        Assert-That $result.Threw "missing $missing throws" $result.Error
        Assert-That (-not (Test-Path $zip)) "missing $missing produces no ZIP"
    }

    Write-Host '== Package-Simulator: empty required file =='
    {
        $root = New-TestRoot; $workRoots.Add($root)
        $player = Join-Path $root 'player'
        New-Item -ItemType Directory -Force -Path $player | Out-Null
        New-ValidPlayer -Root $player
        [IO.File]::WriteAllBytes((Join-Path $player 'OperaSim.exe'), [byte[]]@())
        $zip = Join-Path $root 'out.zip'
        $result = Invoke-Packager -Player $player -Archive $zip
        Assert-That $result.Threw 'zero-byte OperaSim.exe throws' $result.Error
        Assert-That (-not (Test-Path $zip)) 'zero-byte exe produces no ZIP'
    }.Invoke()

    Write-Host '== Package-Simulator: license artifact rejection =='
    foreach ($bad in @(
        @{ Path = 'OperaSim_Data/nested/deep/Mixed.LiC'; Desc = 'nested mixed-case .LiC' },
        @{ Path = 'OperaSim_Data/.hidden/agx.lfx'; Desc = 'hidden .lfx' },
        @{ Path = 'OperaSim_Data/Plugins/x86_64/agx/Deep/Nested/FOO.ULF'; Desc = 'nested uppercase .ULF' },
        @{ Path = 'OperaSim_Data/x/y/z.rtlfx'; Desc = '.rtlfx' },
        @{ Path = 'OperaSim_Data/x/y/license.lic.meta'; Desc = '.lic.meta companion' },
        @{ Path = 'OperaSim_Data/x/y/lic.ULF.meta'; Desc = 'uppercase .ULF.meta companion' })) {
        $root = New-TestRoot; $workRoots.Add($root)
        $player = Join-Path $root 'player'
        New-Item -ItemType Directory -Force -Path $player | Out-Null
        New-ValidPlayer -Root $player
        Write-FixtureFile -Root $player -RelativePath $bad.Path -Content 'license-like'
        $zip = Join-Path $root 'out.zip'
        $result = Invoke-Packager -Player $player -Archive $zip
        Assert-That $result.Threw "$($bad.Desc) throws" $result.Error
        Assert-That (-not (Test-Path $zip)) "$($bad.Desc) produces no ZIP"
    }

    Write-Host '== Package-Simulator: archive inside player refused =='
    {
        $root = New-TestRoot; $workRoots.Add($root)
        $player = Join-Path $root 'player'
        New-Item -ItemType Directory -Force -Path $player | Out-Null
        New-ValidPlayer -Root $player
        $before = @(Get-ChildItem -LiteralPath $player -Recurse -Force | ForEach-Object { $_.FullName })
        $zip = Join-Path $player 'out.zip'
        $result = Invoke-Packager -Player $player -Archive $zip
        Assert-That $result.Threw 'archive inside player throws' $result.Error
        $after = @(Get-ChildItem -LiteralPath $player -Recurse -Force | ForEach-Object { $_.FullName })
        Assert-That ((Compare-Object $before $after) -eq $null) 'player tree unchanged after refusal'
        Assert-That (-not (Test-Path $zip)) 'no archive created inside player'
    }.Invoke()

    Write-Host '== Package-Simulator: existing archive not overwritten =='
    {
        $root = New-TestRoot; $workRoots.Add($root)
        $player = Join-Path $root 'player'
        New-Item -ItemType Directory -Force -Path $player | Out-Null
        New-ValidPlayer -Root $player
        $zip = Join-Path $root 'existing.zip'
        [IO.File]::WriteAllText($zip, 'PRE-EXISTING')
        $result = Invoke-Packager -Player $player -Archive $zip
        Assert-That $result.Threw 'existing archive throws' $result.Error
        Assert-That ((Get-Content -LiteralPath $zip -Raw) -eq 'PRE-EXISTING') 'existing archive bytes untouched'
    }.Invoke()

    Write-Host '== Package-Simulator: reparse point rejected =='
    {
        $root = New-TestRoot; $workRoots.Add($root)
        $player = Join-Path $root 'player'
        New-Item -ItemType Directory -Force -Path $player | Out-Null
        New-ValidPlayer -Root $player
        $link = Join-Path $player 'OperaSim_Data/link'
        $linkMade = $false
        try {
            New-Item -ItemType SymbolicLink -Path $link -Target $player -ErrorAction Stop | Out-Null
            $linkMade = $true
        } catch { $linkMade = $false }
        if ($linkMade) {
            $zip = Join-Path $root 'out.zip'
            $result = Invoke-Packager -Player $player -Archive $zip
            Assert-That $result.Threw 'reparse point throws' $result.Error
            Assert-That (-not (Test-Path $zip)) 'reparse point produces no ZIP'
        } else {
            Write-Host '  SKIP  cannot create symlink on this host'
            $script:Passed++
        }
    }.Invoke()
} finally {
    foreach ($r in $workRoots) {
        Remove-Item -LiteralPath $r -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ''
Write-Host ("Package-Simulator.Tests: {0} passed, {1} failed" -f $script:Passed, $script:Failed)
if ($script:Failures.Count -gt 0) {
    Write-Host 'Failures:'
    $script:Failures | ForEach-Object { Write-Host "  - $_" }
}
exit ([int]($script:Failed -gt 0))
