#requires -Version 7.0
<#
.SYNOPSIS
  Tests for .github/scripts/Invoke-UnityBuild.ps1 using temp fixtures and a
  fake Unity editor executable that records its arguments. No real Unity,
  licenses, or network are used.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$testsDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $testsDir '..\..\..')).Path
$invokeScript = Join-Path $repoRoot '.github/scripts/Invoke-UnityBuild.ps1'
$pwshExe = (Get-Process -Id $PID).Path

$script:Passed = 0
$script:Failed = 0
$script:Failures = [System.Collections.Generic.List[string]]::new()

$envNames = @(
    'UNITY_EDITOR_PATH', 'OPERA_CI_DIRECTORY', 'OPERA_PLAYER_DIRECTORY',
    'OPERA_DIAGNOSTICS_DIRECTORY', 'OPERA_UNITY_ACTIVATION',
    'OPERA_USE_AGX_LICENSE', 'GITHUB_WORKSPACE',
    'UNITY_SERIAL', 'UNITY_EMAIL', 'UNITY_PASSWORD', 'UNITY_LICENSE',
    'AGX_LICENSE_BASE64',
    'LOCALAPPDATA', 'USERPROFILE',
    'FAKE_UNITY_ARGS_FILE', 'FAKE_UNITY_CHECK_MARKER',
    'FAKE_UNITY_MARKER_RESULT', 'FAKE_UNITY_EXIT_CODE'
)

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

# Fake Unity editor: records argv (one per line), emits fixed stdout/stderr,
# optionally reports whether the serial-activation marker existed at spawn.
$fakeEditorBody = @'
#!/usr/bin/env bash
set -u
if [ -n "${FAKE_UNITY_ARGS_FILE:-}" ]; then
  printf '%s\n' "$@" >> "$FAKE_UNITY_ARGS_FILE"
fi
# Echo args on stdout too: if the orchestrator ever relays raw process
# output, supplied secrets would leak into captured console output.
echo "FAKE-UNITY-STDOUT args=[$*]"
echo "fake-unity stderr marker" >&2
if [ "${FAKE_UNITY_CHECK_MARKER:-0}" = "1" ] && [ -n "${OPERA_CI_DIRECTORY:-}" ]; then
  if [ -f "$OPERA_CI_DIRECTORY/ownership/serial-activation-attempted" ]; then
    echo "marker=present" > "${FAKE_UNITY_MARKER_RESULT:-/dev/null}"
  else
    echo "marker=absent" > "${FAKE_UNITY_MARKER_RESULT:-/dev/null}"
  fi
fi
exit "${FAKE_UNITY_EXIT_CODE:-0}"
'@

$sharedRoot = Join-Path ([IO.Path]::GetTempPath()) ("opera-invoke-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $sharedRoot | Out-Null
$fakeEditor = Join-Path $sharedRoot 'fake-unity.sh'
[IO.File]::WriteAllText($fakeEditor, ($fakeEditorBody -replace "`r`n", "`n"))
chmod +x $fakeEditor 2>$null

function New-TestRoot {
    $root = Join-Path $sharedRoot ([guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    return $root
}

function New-BaseEnv {
    param([string]$Root)
    $ws = Join-Path $Root 'ws'
    New-Item -ItemType Directory -Force -Path (Join-Path $ws 'Assets/AGXUnity/Plugins/x86_64') | Out-Null
    return @{
        GITHUB_WORKSPACE = $ws
        OPERA_CI_DIRECTORY = Join-Path $Root 'ci'
        OPERA_PLAYER_DIRECTORY = Join-Path $Root 'player'
        OPERA_DIAGNOSTICS_DIRECTORY = Join-Path $Root 'diag'
        OPERA_UNITY_ACTIVATION = 'serial'
        OPERA_USE_AGX_LICENSE = 'false'
        UNITY_EDITOR_PATH = $fakeEditor
        UNITY_SERIAL = 'ABCD-1234-EFGH-5678'
        UNITY_EMAIL = 'ci-user@example.com'
        UNITY_PASSWORD = 'Sup3r!FakePass'
        UNITY_LICENSE = $null
        AGX_LICENSE_BASE64 = $null
        FAKE_UNITY_ARGS_FILE = Join-Path $Root 'unity-args.txt'
        FAKE_UNITY_CHECK_MARKER = '1'
        FAKE_UNITY_MARKER_RESULT = Join-Path $Root 'marker-result.txt'
        FAKE_UNITY_EXIT_CODE = $null
    }
}

function Invoke-Op {
    param([string]$Operation, [hashtable]$EnvMap)
    $saved = @{}
    foreach ($n in $envNames) { $saved[$n] = [Environment]::GetEnvironmentVariable($n) }
    try {
        foreach ($k in $EnvMap.Keys) {
            $v = $EnvMap[$k]
            if ($null -eq $v) { Remove-Item -LiteralPath "env:$k" -ErrorAction SilentlyContinue }
            else { Set-Item -LiteralPath "env:$k" -Value $v }
        }
        $out = & $pwshExe -NoProfile -File $invokeScript -Operation $Operation 2>&1 | Out-String
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $out }
    } finally {
        foreach ($n in $envNames) {
            if ($null -eq $saved[$n]) { Remove-Item -LiteralPath "env:$n" -ErrorAction SilentlyContinue }
            else { Set-Item -LiteralPath "env:$n" -Value $saved[$n] }
        }
    }
}

function Get-RecordedArgs {
    param([hashtable]$EnvMap)
    $path = $EnvMap['FAKE_UNITY_ARGS_FILE']
    $result = [System.Collections.Generic.List[string]]::new()
    if (Test-Path -LiteralPath $path) {
        foreach ($l in (Get-Content -LiteralPath $path)) { $result.Add([string]$l) }
    }
    return ,$result.ToArray()
}

function Assert-ArgValue {
    param([string[]]$Lines, [string]$Flag, [string]$Expected, [string]$Name)
    $idx = [Array]::IndexOf($Lines, $Flag)
    $ok = ($idx -ge 0 -and $idx + 1 -lt $Lines.Count -and $Lines[$idx + 1] -eq $Expected)
    Assert-That $ok $Name "flag $Flag -> '$Expected'"
}

try {
    Write-Host '== Invoke-UnityBuild: Preflight =='
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        foreach ($k in 'UNITY_SERIAL', 'UNITY_EMAIL', 'UNITY_PASSWORD') { $envMap[$k] = $null }
        $r = Invoke-Op 'Preflight' $envMap
        Assert-That ($r.ExitCode -ne 0) 'preflight fails without serial credentials' $r.Output
        Assert-That (-not (Test-Path -LiteralPath $envMap['FAKE_UNITY_ARGS_FILE'])) 'no Unity launch on failed preflight'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_UNITY_ACTIVATION'] = 'license-file'
        $r = Invoke-Op 'Preflight' $envMap
        Assert-That ($r.ExitCode -ne 0) 'preflight license-file fails without UNITY_LICENSE' $r.Output
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_UNITY_ACTIVATION'] = 'bogus-mode'
        $r = Invoke-Op 'Preflight' $envMap
        Assert-That ($r.ExitCode -ne 0) 'preflight fails on unknown activation mode' $r.Output
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_USE_AGX_LICENSE'] = 'true'
        $envMap['AGX_LICENSE_BASE64'] = $null
        $r = Invoke-Op 'Preflight' $envMap
        Assert-That ($r.ExitCode -ne 0) 'preflight fails when selected AGX secret missing' $r.Output
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_USE_AGX_LICENSE'] = 'true'
        $envMap['AGX_LICENSE_BASE64'] = '!!!not-valid-base64!!!'
        $r = Invoke-Op 'Preflight' $envMap
        Assert-That ($r.ExitCode -ne 0) 'preflight fails on malformed AGX base64' $r.Output
        Assert-That ($r.Output -notmatch [regex]::Escape('!!!not-valid-base64!!!')) 'preflight error does not echo secret value'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_USE_AGX_LICENSE'] = 'true'
        $envMap['AGX_LICENSE_BASE64'] = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('fake-agx-license-body'))
        $r = Invoke-Op 'Preflight' $envMap
        Assert-That ($r.ExitCode -eq 0) 'preflight passes with all credentials and valid AGX secret' $r.Output
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['UNITY_EDITOR_PATH'] = (Join-Path $root 'no-such-unity.exe')
        $r = Invoke-Op 'Preflight' $envMap
        Assert-That ($r.ExitCode -eq 0) 'preflight does not require installed Editor' $r.Output
    }.Invoke()

    Write-Host '== Invoke-UnityBuild: Activate =='
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $r = Invoke-Op 'Activate' $envMap
        Assert-That ($r.ExitCode -eq 0) 'serial activate exits 0' $r.Output
        $lines = Get-RecordedArgs $envMap
        Assert-That ($lines -contains '-batchmode') 'activate passes -batchmode' ($lines -join '|')
        Assert-That ($lines -contains '-quit') 'activate passes -quit' ($lines -join '|')
        Assert-ArgValue $lines '-serial' $envMap['UNITY_SERIAL'] 'activate passes serial value'
        Assert-ArgValue $lines '-username' $envMap['UNITY_EMAIL'] 'activate passes username'
        Assert-ArgValue $lines '-password' $envMap['UNITY_PASSWORD'] 'activate passes password'
        $markerAtSpawn = if (Test-Path -LiteralPath $envMap['FAKE_UNITY_MARKER_RESULT']) { (Get-Content -LiteralPath $envMap['FAKE_UNITY_MARKER_RESULT'] -Raw).Trim() } else { 'missing' }
        Assert-That ($markerAtSpawn -eq 'marker=present') 'activation marker recorded before Unity spawn' "got $markerAtSpawn"
        Assert-That ($r.Output -notmatch [regex]::Escape($envMap['UNITY_PASSWORD'])) 'activate output does not leak password'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['FAKE_UNITY_EXIT_CODE'] = '3'
        $r = Invoke-Op 'Activate' $envMap
        Assert-That ($r.ExitCode -ne 0) 'nonzero Unity exit fails activate' $r.Output
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_UNITY_ACTIVATION'] = 'license-file'
        $envMap['UNITY_LICENSE'] = "LICENSE-LINE-1`nLICENSE-LINE-2"
        $r = Invoke-Op 'Activate' $envMap
        Assert-That ($r.ExitCode -eq 0) 'license-file activate exits 0' $r.Output
        $lines = Get-RecordedArgs $envMap
        $idx = [Array]::IndexOf($lines, '-manualLicenseFile')
        $ulfPath = if ($idx -ge 0 -and $idx + 1 -lt $lines.Count) { $lines[$idx + 1] } else { '' }
        Assert-That ($ulfPath -ne '') 'license-file passes -manualLicenseFile path' ($lines -join '|')
        Assert-That ($ulfPath -match '\.ulf$') 'license file uses .ulf extension' $ulfPath
        Assert-That ($ulfPath -and (Test-Path -LiteralPath $ulfPath -PathType Leaf)) 'temporary .ulf exists after activate'
        if ($ulfPath -and (Test-Path -LiteralPath $ulfPath -PathType Leaf)) {
            Assert-That ((Get-Content -LiteralPath $ulfPath -Raw) -match 'LICENSE-LINE-1') '.ulf contains license text'
        }
        $serialMarker = Join-Path $envMap['OPERA_CI_DIRECTORY'] 'ownership/serial-activation-attempted'
        Assert-That (-not (Test-Path -LiteralPath $serialMarker)) 'license-file mode does not mark serial activation'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['UNITY_EDITOR_PATH'] = (Join-Path $root 'missing-unity.exe')
        $r = Invoke-Op 'Activate' $envMap
        Assert-That ($r.ExitCode -ne 0) 'activate fails when Editor missing' $r.Output
    }.Invoke()

    Write-Host '== Invoke-UnityBuild: ReturnLicense =='
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $r = Invoke-Op 'ReturnLicense' $envMap
        Assert-That ($r.ExitCode -eq 0) 'return license no-ops without attempt' $r.Output
        Assert-That (-not (Test-Path -LiteralPath $envMap['FAKE_UNITY_ARGS_FILE'])) 'no Unity launch on no-op return'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $null = Invoke-Op 'Activate' $envMap
        $r = Invoke-Op 'ReturnLicense' $envMap
        Assert-That ($r.ExitCode -eq 0) 'return license after serial activate exits 0' $r.Output
        $lines = Get-RecordedArgs $envMap
        $retIdx = [Array]::IndexOf($lines, '-returnlicense')
        Assert-That ($retIdx -ge 0) 'return license invokes -returnlicense' ($lines -join '|')
        if ($retIdx -ge 0) {
            $tail = $lines[$retIdx..($lines.Count - 1)]
            Assert-That (($tail -contains '-username') -and ($tail -contains '-password')) 'return license passes credentials'
        }
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_UNITY_ACTIVATION'] = 'license-file'
        $envMap['UNITY_LICENSE'] = 'SOME-LICENSE'
        $null = Invoke-Op 'Activate' $envMap
        $r = Invoke-Op 'ReturnLicense' $envMap
        Assert-That ($r.ExitCode -eq 0) 'return license exits 0 after license-file activate' $r.Output
        $lines = Get-RecordedArgs $envMap
        Assert-That (-not ($lines -contains '-returnlicense')) 'return license no-ops for license-file mode'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $null = Invoke-Op 'Activate' $envMap
        $envMap['UNITY_EDITOR_PATH'] = (Join-Path $root 'gone-unity.exe')
        $r = Invoke-Op 'ReturnLicense' $envMap
        Assert-That ($r.ExitCode -eq 0) 'return license no-ops when Editor missing' $r.Output
    }.Invoke()

    Write-Host '== Invoke-UnityBuild: Build =='
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $r = Invoke-Op 'Build' $envMap
        Assert-That ($r.ExitCode -eq 0) 'build without AGX exits 0' $r.Output
        $lines = Get-RecordedArgs $envMap
        foreach ($flag in '-batchmode', '-quit', '-projectPath', '-buildTarget', 'Win64', '-executeMethod', 'PWRISimulator.Editor.HackathonBuild.BuildWindows') {
            Assert-That ($lines -contains $flag) "build args contain $flag" ($lines -join '|')
        }
        Assert-That (-not ($lines -contains '-nographics')) 'build does not pass -nographics'
        $ws = $envMap['GITHUB_WORKSPACE']
        Assert-That (-not (Test-Path -LiteralPath (Join-Path $ws 'Assets/AGXUnity/Plugins/x86_64/agx.lfx'))) 'no agx.lfx written without AGX secret'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_USE_AGX_LICENSE'] = 'true'
        $envMap['AGX_LICENSE_BASE64'] = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('AGX-DECODED-LICENSE-BODY'))
        $r = Invoke-Op 'Build' $envMap
        Assert-That ($r.ExitCode -eq 0) 'build with AGX exits 0' $r.Output
        $lfx = Join-Path $envMap['GITHUB_WORKSPACE'] 'Assets/AGXUnity/Plugins/x86_64/agx.lfx'
        Assert-That (Test-Path -LiteralPath $lfx -PathType Leaf) 'decoded agx.lfx written'
        if (Test-Path -LiteralPath $lfx -PathType Leaf) {
            Assert-That ((Get-Content -LiteralPath $lfx -Raw) -eq 'AGX-DECODED-LICENSE-BODY') 'agx.lfx content is decoded secret'
        }
        $r2 = Invoke-Op 'Build' $envMap
        Assert-That ($r2.ExitCode -ne 0) 'second build refuses to overwrite existing agx.lfx' $r2.Output
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_USE_AGX_LICENSE'] = 'true'
        $envMap['AGX_LICENSE_BASE64'] = $null
        $r = Invoke-Op 'Build' $envMap
        Assert-That ($r.ExitCode -ne 0) 'build fails when selected AGX secret missing' $r.Output
        Assert-That (-not (Test-Path -LiteralPath $envMap['FAKE_UNITY_ARGS_FILE'])) 'no Unity launch on failed AGX check'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_USE_AGX_LICENSE'] = 'true'
        $envMap['AGX_LICENSE_BASE64'] = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('META-TEST'))
        $meta = Join-Path $envMap['GITHUB_WORKSPACE'] 'Assets/AGXUnity/Plugins/x86_64/agx.lfx.meta'
        [IO.File]::WriteAllText($meta, 'PRE-EXISTING-UNOWNED-META')
        $r = Invoke-Op 'Build' $envMap
        Assert-That ($r.ExitCode -ne 0) 'build refuses when agx.lfx.meta preexists' $r.Output
        $lfx = Join-Path $envMap['GITHUB_WORKSPACE'] 'Assets/AGXUnity/Plugins/x86_64/agx.lfx'
        Assert-That (-not (Test-Path -LiteralPath $lfx)) 'no agx.lfx written when meta blocks build'
        Assert-That ((Get-Content -LiteralPath $meta -Raw) -eq 'PRE-EXISTING-UNOWNED-META') 'preexisting agx.lfx.meta preserved'
        $null = Invoke-Op 'Cleanup' $envMap
        Assert-That ((Get-Content -LiteralPath $meta -Raw) -eq 'PRE-EXISTING-UNOWNED-META') 'cleanup preserves unowned agx.lfx.meta'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_USE_AGX_LICENSE'] = 'true'
        $envMap['AGX_LICENSE_BASE64'] = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('OWNED-META-TEST'))
        $null = Invoke-Op 'Build' $envMap
        $dir = Join-Path $envMap['GITHUB_WORKSPACE'] 'Assets/AGXUnity/Plugins/x86_64'
        $lfx = Join-Path $dir 'agx.lfx'
        $meta = "$lfx.meta"
        # Simulate Unity generating the companion .meta during the real build.
        [IO.File]::WriteAllText($meta, 'unity-generated-meta')
        $rc = Invoke-Op 'Cleanup' $envMap
        Assert-That ($rc.ExitCode -eq 0) 'cleanup exits 0 with owned meta' $rc.Output
        Assert-That (-not (Test-Path -LiteralPath $lfx)) 'cleanup removes owned agx.lfx'
        Assert-That (-not (Test-Path -LiteralPath $meta)) 'cleanup removes owned Unity-generated agx.lfx.meta'
    }.Invoke()

    Write-Host '== Invoke-UnityBuild: Cleanup =='
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $lfx = Join-Path $envMap['GITHUB_WORKSPACE'] 'Assets/AGXUnity/Plugins/x86_64/agx.lfx'
        [IO.File]::WriteAllText($lfx, 'PRE-EXISTING-UNOWNED')
        $r = Invoke-Op 'Cleanup' $envMap
        Assert-That ($r.ExitCode -eq 0) 'cleanup exits 0' $r.Output
        Assert-That ((Get-Content -LiteralPath $lfx -Raw) -eq 'PRE-EXISTING-UNOWNED') 'cleanup preserves unowned project license'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['OPERA_USE_AGX_LICENSE'] = 'true'
        $envMap['AGX_LICENSE_BASE64'] = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('OWNED-BODY'))
        $null = Invoke-Op 'Build' $envMap
        $lfx = Join-Path $envMap['GITHUB_WORKSPACE'] 'Assets/AGXUnity/Plugins/x86_64/agx.lfx'
        Assert-That (Test-Path -LiteralPath $lfx) 'helper-owned agx.lfx exists before cleanup'
        $r = Invoke-Op 'Cleanup' $envMap
        Assert-That ($r.ExitCode -eq 0) 'cleanup after build exits 0' $r.Output
        Assert-That (-not (Test-Path -LiteralPath $lfx)) 'cleanup removes helper-owned agx.lfx'
    }.Invoke()

    Write-Host '== Invoke-UnityBuild: ExportLogs =='
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $ci = $envMap['OPERA_CI_DIRECTORY']
        $logDir = Join-Path $ci 'logs'
        New-Item -ItemType Directory -Force -Path $logDir | Out-Null
        $agxB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('AGX-DECODED-SECRET-TEXT'))
        $secretText = @(
            'build started',
            "password=$($envMap['UNITY_PASSWORD'])",
            "email=$($envMap['UNITY_EMAIL'])",
            "serial=$($envMap['UNITY_SERIAL'])",
            'other-serial=ZZZZ-9999-YYYY-8888',
            'ulf-begin', 'ULF-SECRET-LINE-1', 'ULF-SECRET-LINE-2', 'ulf-end',
            "agx-b64=$agxB64",
            'AGX-DECODED-SECRET-TEXT',
            'done'
        ) -join "`n"
        [IO.File]::WriteAllText((Join-Path $logDir 'unity-build.log'), $secretText)
        $envMap['AGX_LICENSE_BASE64'] = $agxB64
        $envMap['UNITY_LICENSE'] = "ULF-SECRET-LINE-1`nULF-SECRET-LINE-2"
        $r = Invoke-Op 'ExportLogs' $envMap
        Assert-That ($r.ExitCode -eq 0) 'export logs exits 0' $r.Output
        $diag = $envMap['OPERA_DIAGNOSTICS_DIRECTORY']
        $exported = @()
        if (Test-Path -LiteralPath $diag) { $exported = @(Get-ChildItem -LiteralPath $diag -Recurse -File) }
        Assert-That ($exported.Count -gt 0) 'exported at least one log'
        $allText = ($exported | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n"
        foreach ($secret in @($envMap['UNITY_PASSWORD'], $envMap['UNITY_EMAIL'], $envMap['UNITY_SERIAL'],
                             'ZZZZ-9999-YYYY-8888', 'ULF-SECRET-LINE-1', 'ULF-SECRET-LINE-2',
                             $agxB64, 'AGX-DECODED-SECRET-TEXT')) {
            Assert-That ($allText -notmatch [regex]::Escape($secret)) "exported log omits secret '$secret'"
        }
        Assert-That ($allText -match 'REDACTED') 'exported log contains redaction markers'
        Assert-That (-not ($exported | Where-Object { $_.Name -match '\.(lfx|lic|rtlfx|ulf)(\.meta)?$' })) 'no license files exported'
        Assert-That (($exported | ForEach-Object Name) -contains 'unity-build.log' -or ($exported | ForEach-Object Name) -match 'unity-build') 'build log exported under scrubbed name'
    }.Invoke()
    {
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['UNITY_EDITOR_PATH'] = (Join-Path $root 'missing-unity.exe')
        $r = Invoke-Op 'ExportLogs' $envMap
        Assert-That ($r.ExitCode -eq 0) 'export logs with no logs exits 0' $r.Output
    }.Invoke()
    {
        # Two distinct secrets of identical length plus equal-length license
        # lines must all be scrubbed; a length-keyed dedup would drop one.
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $envMap['UNITY_SERIAL'] = 'EqualLenBB!'
        $envMap['UNITY_PASSWORD'] = 'EqualLenAA!'
        $envMap['UNITY_LICENSE'] = "LICLINEAAA`nLICLINEBBB"
        $logDir = Join-Path $envMap['OPERA_CI_DIRECTORY'] 'logs'
        New-Item -ItemType Directory -Force -Path $logDir | Out-Null
        $logText = @(
            'pw=EqualLenAA! and sn=EqualLenBB! on one line',
            'lic section',
            'first=LICLINEAAA filler LICLINEBBB second'
        ) -join "`n"
        [IO.File]::WriteAllText((Join-Path $logDir 'unity-build.log'), $logText)
        $r = Invoke-Op 'ExportLogs' $envMap
        Assert-That ($r.ExitCode -eq 0) 'export logs same-length secrets exits 0' $r.Output
        $diag = $envMap['OPERA_DIAGNOSTICS_DIRECTORY']
        $allText = ''
        if (Test-Path -LiteralPath $diag) {
            $allText = ((Get-ChildItem -LiteralPath $diag -Recurse -File |
                ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n")
        }
        foreach ($s in 'EqualLenAA!', 'EqualLenBB!', 'LICLINEAAA', 'LICLINEBBB') {
            Assert-That ($allText -notmatch [regex]::Escape($s)) "same-length secret scrubbed: $s"
        }
    }.Invoke()
    {
        # Two genuinely different user-profile Editor.log sources must export
        # under distinct names instead of overwriting each other.
        $root = New-TestRoot
        $envMap = New-BaseEnv $root
        $ed1 = Join-Path $root 'localappdata/Unity/Editor'
        $ed2 = Join-Path $root 'userprofile/AppData/Local/Unity/Editor'
        New-Item -ItemType Directory -Force -Path $ed1, $ed2 | Out-Null
        [IO.File]::WriteAllText((Join-Path $ed1 'Editor.log'), "localappdata editor log`nci-user@example.com")
        [IO.File]::WriteAllText((Join-Path $ed2 'Editor.log'), "userprofile editor log`nserial=ABCD-1234-EFGH-5678")
        $envMap['LOCALAPPDATA'] = Join-Path $root 'localappdata'
        $envMap['USERPROFILE'] = Join-Path $root 'userprofile'
        $envMap['OPERA_CI_DIRECTORY'] = $null
        $r = Invoke-Op 'ExportLogs' $envMap
        Assert-That ($r.ExitCode -eq 0) 'export logs with editor sources exits 0' $r.Output
        $diag = $envMap['OPERA_DIAGNOSTICS_DIRECTORY']
        $names = @()
        if (Test-Path -LiteralPath $diag) { $names = @(Get-ChildItem -LiteralPath $diag -File | ForEach-Object Name) }
        Assert-That ($names -contains 'editor-0.log') 'first editor log exported as editor-0.log' ($names -join ',')
        Assert-That ($names -contains 'editor-1.log') 'second editor log exported as editor-1.log' ($names -join ',')
        $t0 = if ($names -contains 'editor-0.log') { Get-Content -LiteralPath (Join-Path $diag 'editor-0.log') -Raw } else { '' }
        $t1 = if ($names -contains 'editor-1.log') { Get-Content -LiteralPath (Join-Path $diag 'editor-1.log') -Raw } else { '' }
        Assert-That ($t0 -match 'localappdata editor log') 'editor-0 has LOCALAPPDATA content'
        Assert-That ($t1 -match 'userprofile editor log') 'editor-1 has USERPROFILE content'
        Assert-That ($t0 -notmatch 'ci-user@example\.com') 'editor-0 scrubbed email'
        Assert-That ($t1 -notmatch 'ABCD-1234-EFGH-5678') 'editor-1 scrubbed serial'
    }.Invoke()
} finally {
    Remove-Item -LiteralPath $sharedRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host ("Invoke-UnityBuild.Tests: {0} passed, {1} failed" -f $script:Passed, $script:Failed)
if ($script:Failures.Count -gt 0) {
    Write-Host 'Failures:'
    $script:Failures | ForEach-Object { Write-Host "  - $_" }
}
exit ([int]($script:Failed -gt 0))
