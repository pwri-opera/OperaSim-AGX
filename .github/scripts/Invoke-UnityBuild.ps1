#requires -Version 7.0
<#
.SYNOPSIS
  Orchestrate one step of the Windows simulator build on a GitHub runner.

.DESCRIPTION
  Operations: Preflight, Activate, Build, ReturnLicense, ExportLogs, Cleanup.

  All configuration arrives via environment variables (see the workflow):
    UNITY_EDITOR_PATH, OPERA_CI_DIRECTORY, OPERA_PLAYER_DIRECTORY,
    OPERA_DIAGNOSTICS_DIRECTORY, OPERA_UNITY_ACTIVATION (serial|license-file),
    OPERA_USE_AGX_LICENSE (boolean-ish), GITHUB_WORKSPACE.
  Secrets arrive only in the steps that need them: UNITY_SERIAL,
  UNITY_EMAIL, UNITY_PASSWORD, UNITY_LICENSE (.ulf text),
  AGX_LICENSE_BASE64 (optional).

  Unity process output is captured under OPERA_CI_DIRECTORY/logs and is
  never printed to the console or uploaded raw. ExportLogs writes only
  scrubbed copies to OPERA_DIAGNOSTICS_DIRECTORY. Ownership of helper-created
  files (temporary .ulf, decoded agx.lfx) is recorded under
  OPERA_CI_DIRECTORY/ownership so Cleanup removes exactly those files.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Preflight', 'Activate', 'Build', 'ReturnLicense', 'ExportLogs', 'Cleanup')]
    [string]$Operation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TimeoutSeconds = @{
    Activate       = 600   # 10 minutes
    Build          = 5400  # 90 minutes
    ReturnLicense  = 300   # 5 minutes
}
$script:Utf8NoBom = [Text.UTF8Encoding]::new($false)

function Get-EnvValue {
    param([string]$Name)
    return [Environment]::GetEnvironmentVariable($Name)
}

function Require-Env {
    param([string]$Name)
    $value = Get-EnvValue $Name
    if ([string]::IsNullOrWhiteSpace("$value")) {
        throw "Invoke-UnityBuild: required environment variable $Name is not set."
    }
    return "$value"
}

function ConvertTo-AgxFlag {
    param([string]$Raw)
    if ([string]::IsNullOrWhiteSpace($Raw)) { return $false }
    $trimmed = $Raw.Trim()
    if ($trimmed -match '^(?i:true|1|yes|on)$') { return $true }
    if ($trimmed -match '^(?i:false|0|no|off)$') { return $false }
    throw "Invoke-UnityBuild: OPERA_USE_AGX_LICENSE has unsupported value."
}

function Get-OwnershipDir {
    $ci = Require-Env 'OPERA_CI_DIRECTORY'
    return (Join-Path $ci 'ownership')
}

function Register-OwnedPath {
    param([string]$Path)
    $dir = Get-OwnershipDir
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    Add-Content -LiteralPath (Join-Path $dir 'owned-paths.txt') -Value $Path -Encoding utf8
}

function Get-UnityLogPath {
    param([string]$CiDir, [string]$Name)
    return (Join-Path $CiDir "logs/$Name")
}

function Invoke-UnityEditor {
    <#
      Launch Unity.exe with ArgumentList (no shell interpolation), capture
      stdout/stderr to OPERA_CI_DIRECTORY/logs/<LogName>, kill the process
      tree on timeout, and never echo raw output or secrets.
    #>
    param(
        [Parameter(Mandatory = $true)][string]$ExePath,
        [Parameter(Mandatory = $true)][string[]]$UnityArgs,
        [Parameter(Mandatory = $true)][string]$LogName,
        [Parameter(Mandatory = $true)][int]$TimeoutSec
    )
    if (-not (Test-Path -LiteralPath $ExePath -PathType Leaf)) {
        throw "Invoke-UnityBuild: Unity Editor not found at $ExePath"
    }
    $ci = Require-Env 'OPERA_CI_DIRECTORY'
    $logDir = Join-Path $ci 'logs'
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    $capturePath = Join-Path $logDir $LogName

    $psi = [Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $ExePath
    foreach ($arg in $UnityArgs) { $psi.ArgumentList.Add($arg) }
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $proc = [Diagnostics.Process]::new()
    $proc.StartInfo = $psi
    [void]$proc.Start()
    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()

    $completed = $proc.WaitForExit($TimeoutSec * 1000)
    $timedOut = -not $completed
    if ($timedOut) {
        try { $proc.Kill($true) } catch { }
    }
    try { $proc.WaitForExit() } catch { }
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()

    # Raw output stays under the temporary CI directory only; it may contain
    # sensitive material and is never printed or uploaded unscrubbed. It is
    # saved before any failure is raised so timed-out builds keep diagnostics.
    $header = ''
    if ($timedOut) { $header = "=== TIMED OUT after $TimeoutSec seconds ===`n" }
    $raw = "$header=== stdout ===`n$stdout`n=== stderr ===`n$stderr"
    [IO.File]::WriteAllText($capturePath, $raw, $script:Utf8NoBom)

    if ($timedOut) {
        throw "Invoke-UnityBuild: Unity process timed out after $TimeoutSec seconds ($LogName); partial output in $capturePath"
    }
    if ($proc.ExitCode -ne 0) {
        throw "Invoke-UnityBuild: Unity exited with code $($proc.ExitCode); raw output in $capturePath"
    }
    Write-Host "Invoke-UnityBuild: Unity completed ($LogName)."
}

function Assert-ActivationInputs {
    $mode = Require-Env 'OPERA_UNITY_ACTIVATION'
    if ($mode -notin @('serial', 'license-file')) {
        throw "Invoke-UnityBuild: OPERA_UNITY_ACTIVATION must be 'serial' or 'license-file' (got '$mode')."
    }
    if ($mode -eq 'serial') {
        foreach ($name in 'UNITY_SERIAL', 'UNITY_EMAIL', 'UNITY_PASSWORD') {
            if ([string]::IsNullOrWhiteSpace("$(Get-EnvValue $name)")) {
                throw "Invoke-UnityBuild: missing required secret environment variable $name for serial activation."
            }
        }
    } else {
        if ([string]::IsNullOrWhiteSpace("$(Get-EnvValue 'UNITY_LICENSE')")) {
            throw 'Invoke-UnityBuild: missing required secret environment variable UNITY_LICENSE for license-file activation.'
        }
    }
    return $mode
}

function Invoke-Preflight {
    $mode = Assert-ActivationInputs
    $useAgx = ConvertTo-AgxFlag (Get-EnvValue 'OPERA_USE_AGX_LICENSE')
    if ($useAgx) {
        $b64 = Get-EnvValue 'AGX_LICENSE_BASE64'
        if ([string]::IsNullOrWhiteSpace("$b64")) {
            throw 'Invoke-UnityBuild: AGX license build selected but AGX_LICENSE_BASE64 is empty.'
        }
        try {
            [void][Convert]::FromBase64String($b64)
        } catch {
            throw 'Invoke-UnityBuild: AGX_LICENSE_BASE64 is not valid base64.'
        }
    }
    Write-Host "Invoke-UnityBuild: preflight OK (activation=$mode, agxLicense=$useAgx)."
}

function Invoke-Activate {
    $ci = Require-Env 'OPERA_CI_DIRECTORY'
    New-Item -ItemType Directory -Force -Path $ci | Out-Null
    $mode = Assert-ActivationInputs
    $editor = Require-Env 'UNITY_EDITOR_PATH'
    $editorLog = Get-UnityLogPath $ci 'editor-activate.log'

    if ($mode -eq 'serial') {
        # Record the attempt before spawning so ReturnLicense knows a serial
        # license may be checked out on this machine.
        $ownDir = Get-OwnershipDir
        New-Item -ItemType Directory -Force -Path $ownDir | Out-Null
        New-Item -ItemType File -Force -Path (Join-Path $ownDir 'serial-activation-attempted') | Out-Null
        $unityArgs = @(
            '-batchmode', '-quit',
            '-serial', (Require-Env 'UNITY_SERIAL'),
            '-username', (Require-Env 'UNITY_EMAIL'),
            '-password', (Require-Env 'UNITY_PASSWORD'),
            '-logFile', $editorLog
        )
    } else {
        $ulfPath = Join-Path $ci 'temp/unity-license.ulf'
        New-Item -ItemType Directory -Force -Path (Split-Path $ulfPath -Parent) | Out-Null
        Register-OwnedPath $ulfPath
        [IO.File]::WriteAllText($ulfPath, (Require-Env 'UNITY_LICENSE'), $script:Utf8NoBom)
        $unityArgs = @(
            '-batchmode', '-quit',
            '-manualLicenseFile', $ulfPath,
            '-logFile', $editorLog
        )
    }

    Invoke-UnityEditor -ExePath $editor -UnityArgs $unityArgs `
        -LogName 'unity-activate-capture.log' -TimeoutSec $script:TimeoutSeconds['Activate']
}

function Invoke-Build {
    $ci = Require-Env 'OPERA_CI_DIRECTORY'
    New-Item -ItemType Directory -Force -Path $ci | Out-Null
    $workspace = Require-Env 'GITHUB_WORKSPACE'
    $editor = Require-Env 'UNITY_EDITOR_PATH'
    $useAgx = ConvertTo-AgxFlag (Get-EnvValue 'OPERA_USE_AGX_LICENSE')

    if ($useAgx) {
        $b64 = Get-EnvValue 'AGX_LICENSE_BASE64'
        if ([string]::IsNullOrWhiteSpace("$b64")) {
            throw 'Invoke-UnityBuild: AGX license build selected but AGX_LICENSE_BASE64 is empty.'
        }
        try {
            $licenseBytes = [Convert]::FromBase64String($b64)
        } catch {
            throw 'Invoke-UnityBuild: AGX_LICENSE_BASE64 is not valid base64.'
        }
        $lfxPath = Join-Path $workspace 'Assets/AGXUnity/Plugins/x86_64/agx.lfx'
        $lfxMetaPath = "$lfxPath.meta"
        foreach ($candidate in @($lfxPath, $lfxMetaPath)) {
            if (Test-Path -LiteralPath $candidate) {
                throw "Invoke-UnityBuild: refusing to overwrite existing $candidate"
            }
        }
        Register-OwnedPath $lfxPath
        # Unity may generate the .meta during import/build; record it now so
        # Cleanup removes it, while unowned metadata is never touched.
        Register-OwnedPath $lfxMetaPath
        [IO.File]::WriteAllBytes($lfxPath, $licenseBytes)
        Write-Host 'Invoke-UnityBuild: decoded AGX license written for this build.'
    }

    $unityArgs = @(
        '-batchmode', '-quit',
        '-projectPath', $workspace,
        '-buildTarget', 'Win64',
        '-executeMethod', 'PWRISimulator.Editor.HackathonBuild.BuildWindows',
        '-logFile', (Get-UnityLogPath $ci 'editor-build.log')
    )
    Invoke-UnityEditor -ExePath $editor -UnityArgs $unityArgs `
        -LogName 'unity-build-capture.log' -TimeoutSec $script:TimeoutSeconds['Build']
}

function Invoke-ReturnLicense {
    $ci = Get-EnvValue 'OPERA_CI_DIRECTORY'
    $marker = if ([string]::IsNullOrWhiteSpace("$ci")) { $null } else { Join-Path $ci 'ownership/serial-activation-attempted' }
    if ($null -eq $marker -or -not (Test-Path -LiteralPath $marker -PathType Leaf)) {
        Write-Host 'Invoke-UnityBuild: no serial activation was attempted; nothing to return.'
        return
    }
    $editor = Get-EnvValue 'UNITY_EDITOR_PATH'
    if ([string]::IsNullOrWhiteSpace("$editor") -or -not (Test-Path -LiteralPath $editor -PathType Leaf)) {
        Write-Host 'Invoke-UnityBuild: Unity Editor not available; skipping license return.'
        return
    }
    $user = Get-EnvValue 'UNITY_EMAIL'
    $pass = Get-EnvValue 'UNITY_PASSWORD'
    if ([string]::IsNullOrWhiteSpace("$user") -or [string]::IsNullOrWhiteSpace("$pass")) {
        Write-Host 'Invoke-UnityBuild: Unity credentials unavailable; skipping license return.'
        return
    }
    $unityArgs = @(
        '-batchmode', '-quit',
        '-returnlicense',
        '-username', $user,
        '-password', $pass,
        '-logFile', (Get-UnityLogPath $ci 'editor-return.log')
    )
    Invoke-UnityEditor -ExePath $editor -UnityArgs $unityArgs `
        -LogName 'unity-return-capture.log' -TimeoutSec $script:TimeoutSeconds['ReturnLicense']
}

function Protect-LogText {
    param([AllowEmptyString()][string]$Text)
    if ([string]::IsNullOrEmpty($Text)) { return $Text }

    $secrets = [System.Collections.Generic.List[string]]::new()
    foreach ($name in 'UNITY_SERIAL', 'UNITY_EMAIL', 'UNITY_PASSWORD', 'UNITY_LICENSE') {
        $value = Get-EnvValue $name
        if (-not [string]::IsNullOrWhiteSpace("$value")) { $secrets.Add("$value") }
        if ($name -eq 'UNITY_LICENSE' -and -not [string]::IsNullOrWhiteSpace("$value")) {
            foreach ($line in ("$value" -split "`r?`n")) {
                if (-not [string]::IsNullOrWhiteSpace($line)) { $secrets.Add($line.Trim()) }
            }
        }
    }
    $agxB64 = Get-EnvValue 'AGX_LICENSE_BASE64'
    if (-not [string]::IsNullOrWhiteSpace("$agxB64")) {
        $secrets.Add("$agxB64")
        try {
            $decoded = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($agxB64))
            if (-not [string]::IsNullOrWhiteSpace($decoded)) {
                $secrets.Add($decoded)
                foreach ($line in ($decoded -split "`r?`n")) {
                    if (-not [string]::IsNullOrWhiteSpace($line)) { $secrets.Add($line.Trim()) }
                }
            }
        } catch { }
    }

    # Literal replacement first (longest first so overlapping secrets redact
    # completely). Deduplicate the strings themselves — Sort-Object -Unique
    # would key on length and silently drop distinct same-length secrets.
    # String.Replace is ordinal, so dedup is case-sensitive to match.
    $uniqueSecrets = @($secrets | Group-Object -CaseSensitive | ForEach-Object { $_.Name } |
        Sort-Object { $_.Length } -Descending)
    foreach ($secret in $uniqueSecrets) {
        if (-not [string]::IsNullOrWhiteSpace($secret)) {
            $Text = $Text.Replace($secret, '[REDACTED]')
        }
    }
    # Pattern scrub for anything resembling credentials that was not supplied.
    $Text = [regex]::Replace($Text, '(?i)[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}', '[REDACTED-EMAIL]')
    $Text = [regex]::Replace($Text, '(?i)\b[A-Z0-9]{4}(?:-[A-Z0-9]{4}){3}\b', '[REDACTED-SERIAL]')
    return $Text
}

function Invoke-ExportLogs {
    $ci = Get-EnvValue 'OPERA_CI_DIRECTORY'
    $diag = Require-Env 'OPERA_DIAGNOSTICS_DIRECTORY'
    New-Item -ItemType Directory -Force -Path $diag | Out-Null

    $sources = [System.Collections.Generic.List[IO.FileInfo]]::new()
    if (-not [string]::IsNullOrWhiteSpace("$ci") -and (Test-Path -LiteralPath $ci -PathType Container)) {
        foreach ($f in (Get-ChildItem -LiteralPath $ci -Recurse -Force -File -Filter '*.log' -ErrorAction SilentlyContinue)) {
            $sources.Add($f)
        }
    }
    # Unity's own editor log lives under the user profile on Windows runners.
    $editorLogCandidates = @()
    if (-not [string]::IsNullOrWhiteSpace("$env:LOCALAPPDATA")) {
        $editorLogCandidates += (Join-Path $env:LOCALAPPDATA 'Unity/Editor/Editor.log')
    }
    if (-not [string]::IsNullOrWhiteSpace("$env:USERPROFILE")) {
        $editorLogCandidates += (Join-Path $env:USERPROFILE 'AppData/Local/Unity/Editor/Editor.log')
    }
    foreach ($candidate in $editorLogCandidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            $sources.Add((Get-Item -LiteralPath $candidate))
        }
    }

    $ciFull = $null
    if (-not [string]::IsNullOrWhiteSpace("$ci")) {
        $ciFull = [IO.Path]::GetFullPath($ci).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) +
                  [IO.Path]::DirectorySeparatorChar
    }

    $written = 0
    $seen = @{}
    $editorIndex = 0
    foreach ($src in $sources) {
        $full = [IO.Path]::GetFullPath($src.FullName)
        if ($seen.ContainsKey($full)) { continue }
        $seen[$full] = $true
        if ($null -ne $ciFull -and $full.StartsWith($ciFull, [StringComparison]::OrdinalIgnoreCase)) {
            $name = 'ci-' + ($full.Substring($ciFull.Length) -replace '[\\/]', '_')
        } else {
            # Non-CI sources (e.g. the user-profile Editor.log) get distinct
            # names so multiple candidates cannot overwrite each other.
            $name = "editor-$editorIndex.log"
            $editorIndex++
        }
        if ($name -notmatch '\.log$') { $name += '.log' }
        $outPath = Join-Path $diag $name
        $scrubbed = Protect-LogText -Text ([IO.File]::ReadAllText($full))
        [IO.File]::WriteAllText($outPath, $scrubbed, $script:Utf8NoBom)
        $written++
    }

    if ($written -eq 0) {
        Write-Host 'Invoke-UnityBuild: no Unity logs found to export.'
    } else {
        Write-Host "Invoke-UnityBuild: exported $written scrubbed log file(s) to $diag."
    }
}

function Invoke-Cleanup {
    $ci = Get-EnvValue 'OPERA_CI_DIRECTORY'
    if ([string]::IsNullOrWhiteSpace("$ci")) {
        Write-Host 'Invoke-UnityBuild: OPERA_CI_DIRECTORY not set; nothing to clean.'
        return
    }
    $ownedList = Join-Path $ci 'ownership/owned-paths.txt'
    $removed = 0
    if (Test-Path -LiteralPath $ownedList -PathType Leaf) {
        foreach ($line in (Get-Content -LiteralPath $ownedList)) {
            $path = "$line".Trim()
            if ([string]::IsNullOrEmpty($path)) { continue }
            # Only files we recorded are removed; directories and unlisted
            # project/user files are never touched.
            if (Test-Path -LiteralPath $path -PathType Leaf) {
                Remove-Item -LiteralPath $path -Force
                $removed++
            }
        }
    }
    Write-Host "Invoke-UnityBuild: cleanup complete ($removed helper-owned file(s) removed)."
}

switch ($Operation) {
    'Preflight'     { Invoke-Preflight }
    'Activate'      { Invoke-Activate }
    'Build'         { Invoke-Build }
    'ReturnLicense' { Invoke-ReturnLicense }
    'ExportLogs'    { Invoke-ExportLogs }
    'Cleanup'       { Invoke-Cleanup }
}
