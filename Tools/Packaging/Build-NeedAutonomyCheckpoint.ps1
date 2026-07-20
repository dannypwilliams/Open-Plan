[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe',
    [int]$MinimumEditModeTests = 138,
    [int]$MinimumPlayModeTests = 110,
    [int]$SmokeTimeoutSeconds = 420
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Quote-Argument([string]$Value) {
    if ($Value -notmatch '[\s"]') { return $Value }
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Invoke-CheckedProcess {
    param(
        [Parameter(Mandatory=$true)][string]$FilePath,
        [Parameter(Mandatory=$true)][string[]]$Arguments,
        [int]$TimeoutSeconds = 0,
        [switch]$Visible
    )
    $argumentLine = (($Arguments | ForEach-Object { Quote-Argument $_ }) -join ' ')
    $parameters = @{ FilePath=$FilePath; ArgumentList=$argumentLine; PassThru=$true }
    if (-not $Visible) { $parameters.WindowStyle = 'Hidden' }
    $process = Start-Process @parameters
    if ($TimeoutSeconds -gt 0) {
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            Stop-Process -Id $process.Id -Force
            throw "Process timed out after $TimeoutSeconds seconds: $FilePath"
        }
    } else {
        $process.WaitForExit()
    }
    $process.Refresh()
    if ($process.ExitCode -ne 0) { throw "Process exited with code $($process.ExitCode): $FilePath $argumentLine" }
}

function Read-TestResult([string]$Path, [int]$Minimum, [string]$Name) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Name result is missing: $Path" }
    [xml]$document = Get-Content -LiteralPath $Path
    $run = $document.'test-run'
    if ($run.result -ne 'Passed' -or [int]$run.failed -ne 0 -or [int]$run.passed -lt $Minimum) {
        throw "$Name failed its package gate: passed=$($run.passed) failed=$($run.failed) result=$($run.result)"
    }
    return [ordered]@{
        passed=[int]$run.passed
        failed=[int]$run.failed
        skipped=[int]$run.skipped
        durationSeconds=[double]$run.duration
    }
}

function Assert-CleanSource {
    $status = @(& git status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'git status failed.' }
    if ($status.Count -gt 0) { throw "Prompt 02 packaging requires clean committed source:`n$($status -join "`n")" }
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$playtestRoot = Join-Path $repoRoot 'outputs\Playtests\EndlessOfficeAlpha'
$finalRoot = Join-Path $playtestRoot '02_NeedAutonomy'
$stagingRoot = Join-Path $playtestRoot ('.staging-02_NeedAutonomy-' + [Guid]::NewGuid().ToString('N'))
$windowsRoot = Join-Path $stagingRoot 'Windows'
$verifiedRoot = Join-Path $stagingRoot 'VerifiedExtract'
$testRoot = Join-Path $stagingRoot 'test-results'
$captureRoot = Join-Path $stagingRoot 'captures'
$zipName = 'SillyOfficeSim_02_NeedAutonomy_Windows.zip'
$zipPath = Join-Path $stagingRoot $zipName
$expectedCaptures = @(
    '01_Employee_Urgent_Need_1920x1080.png',
    '02_Autonomous_Restroom_Walk_1920x1080.png',
    '03_Autonomous_Food_Trip_1920x1080.png',
    '04_Navigation_Around_Partition_1920x1080.png',
    '05_Shared_Station_Demand_1920x1080.png',
    '06_Desk_Work_Resumed_1920x1080.png',
    '07_Phone_Work_Resumed_1920x1080.png',
    '08_Inspector_Autonomous_Reason_1920x1080.png',
    '09_Player_Issued_Command_1920x1080.png',
    '10_Critical_Override_1920x1080.png',
    '11_Passive_Mixed_Behaviors_1920x1080.png',
    '12_Overview_After_Ten_Minutes_1920x1080.png'
)

Push-Location $repoRoot
try {
    if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Unity not found: $UnityPath" }
    Assert-CleanSource
    if (Test-Path -LiteralPath $finalRoot) { throw "Refusing to overwrite checkpoint: $finalRoot" }
    New-Item -ItemType Directory -Path $windowsRoot,$verifiedRoot,$testRoot,$captureRoot -Force | Out-Null

    $editXml = Join-Path $testRoot '02_NeedAutonomy-EditMode.xml'
    $editLog = Join-Path $testRoot '02_NeedAutonomy-EditMode.log'
    $playXml = Join-Path $testRoot '02_NeedAutonomy-PlayMode.xml'
    $playLog = Join-Path $testRoot '02_NeedAutonomy-PlayMode.log'
    $reportLog = Join-Path $testRoot '02_NeedAutonomy-Deterministic.log'
    $buildLog = Join-Path $stagingRoot 'build.log'
    $smokeLog = Join-Path $testRoot '02_NeedAutonomy-Extracted-Smoke.log'

    Invoke-CheckedProcess -FilePath $UnityPath -Arguments @('-batchmode','-nographics','-projectPath',$repoRoot,
        '-runTests','-testPlatform','EditMode','-testResults',$editXml,'-logFile',$editLog)
    $edit = Read-TestResult $editXml $MinimumEditModeTests 'EditMode'

    Invoke-CheckedProcess -FilePath $UnityPath -Arguments @('-batchmode','-nographics','-projectPath',$repoRoot,
        '-runTests','-testPlatform','PlayMode','-testResults',$playXml,'-logFile',$playLog)
    $play = Read-TestResult $playXml $MinimumPlayModeTests 'PlayMode'

    Invoke-CheckedProcess -FilePath $UnityPath -Arguments @('-batchmode','-nographics','-quit','-projectPath',$repoRoot,
        '-executeMethod','OpenPlan.Editor.NeedAutonomyReportGenerator.GenerateFromCommandLine','-logFile',$reportLog)
    $generatedReport = Join-Path $repoRoot 'outputs\TestResults\02_NeedAutonomy_Simulation_Report.md'
    if (-not (Test-Path -LiteralPath $generatedReport -PathType Leaf)) { throw 'Autonomy simulation report was not generated.' }
    if ((Get-Content -LiteralPath $generatedReport -Raw) -notmatch 'Outcome: \*\*PASS\*\*') { throw 'Autonomy simulation matrix did not pass.' }
    Copy-Item -LiteralPath $generatedReport -Destination (Join-Path $testRoot '02_NeedAutonomy_Simulation_Report.md')
    Copy-Item -LiteralPath $generatedReport -Destination (Join-Path $stagingRoot 'autonomy-simulation-report.md')

    $executable = Join-Path $windowsRoot 'OpenPlan.exe'
    Invoke-CheckedProcess -FilePath $UnityPath -Arguments @('-batchmode','-nographics','-quit','-projectPath',$repoRoot,
        '-executeMethod','OpenPlan.Editor.CheckpointBuildPipeline.BuildWindowsPlayer',
        '-checkpointBuildPath',$executable,'-logFile',$buildLog)
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'Windows player was not created.' }

    $guideSource = Join-Path $repoRoot 'Docs\Playtests\02_NeedAutonomy_PLAYTEST_GUIDE.md'
    $issuesSource = Join-Path $repoRoot 'Docs\Playtests\02_NeedAutonomy_KNOWN_ISSUES.md'
    Copy-Item -LiteralPath $guideSource -Destination (Join-Path $stagingRoot 'playtest-guide.md')
    Copy-Item -LiteralPath $issuesSource -Destination (Join-Path $stagingRoot 'known-issues.md')

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($windowsRoot,$zipPath,[IO.Compression.CompressionLevel]::Optimal,$false)
    [IO.Compression.ZipFile]::ExtractToDirectory($zipPath,$verifiedRoot)
    $verifiedExecutable = Join-Path $verifiedRoot 'OpenPlan.exe'
    if (-not (Test-Path -LiteralPath $verifiedExecutable -PathType Leaf)) { throw 'Exact extracted executable is missing.' }

    Invoke-CheckedProcess -FilePath $verifiedExecutable -TimeoutSeconds $SmokeTimeoutSeconds -Visible -Arguments @(
        '-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
        '-logFile',$smokeLog,'-openplan-need-autonomy-smoke','-openplan-evidence-root',$stagingRoot)
    $smokeReport = Join-Path $stagingRoot 'NEED_AUTONOMY_SMOKE.txt'
    if (-not (Test-Path -LiteralPath $smokeReport -PathType Leaf) -or
        (Get-Content -LiteralPath $smokeReport -First 1) -ne 'STATUS PASS') { throw 'Exact extracted smoke did not pass.' }

    Add-Type -AssemblyName System.Drawing
    foreach ($name in $expectedCaptures) {
        $capturePath = Join-Path $captureRoot $name
        if (-not (Test-Path -LiteralPath $capturePath -PathType Leaf)) { throw "Missing required gameplay capture: $name" }
        $image = [Drawing.Image]::FromFile($capturePath)
        try {
            if ($image.Width -ne 1920 -or $image.Height -ne 1080) {
                throw "Unexpected capture size $($image.Width)x$($image.Height): $name"
            }
        } finally { $image.Dispose() }
    }
    $captures = @(Get-ChildItem -LiteralPath $captureRoot -Filter '*.png' -File)
    if ($captures.Count -ne $expectedCaptures.Count) { throw "Expected exactly 12 gameplay captures, found $($captures.Count)." }

    Assert-CleanSource
    $branch = (& git branch --show-current).Trim()
    $commit = (& git rev-parse HEAD).Trim()
    $unityVersion = (Get-Content 'ProjectSettings\ProjectVersion.txt' -First 1).Split(':',2)[1].Trim()
    $zipInfo = Get-Item -LiteralPath $zipPath
    $zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
    $buildBytes = [long](Get-ChildItem -LiteralPath $windowsRoot -File -Recurse | Measure-Object Length -Sum).Sum
    $timestamp = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')
    $knownIssues = (Get-Content -LiteralPath $issuesSource | Where-Object { $_ -like '- *' }) -join "`n"
    $manifest = @"
# Endless Office Alpha - Checkpoint 02 Need Autonomy

- Built UTC: $timestamp
- Branch: ``$branch``
- Source commit: ``$commit``
- Unity: ``$unityVersion``
- Configuration: Windows x64, non-development, Mono scripting backend
- Executable: ``Windows/OpenPlan.exe``
- Windows build size: $buildBytes bytes
- ZIP: ``$zipName``
- ZIP size: $($zipInfo.Length) bytes
- ZIP SHA-256: ``$zipHash``
- EditMode: $($edit.passed) passed, $($edit.failed) failed, $($edit.skipped) skipped
- PlayMode: $($play.passed) passed, $($play.failed) failed, $($play.skipped) skipped
- Total automated: $($edit.passed + $play.passed) passed, 0 failed
- Seeded autonomy simulation: PASS; 903 runs; 20 seeds; 3/10/30 workers; 15 scenarios; three extended 100-minute runs
- Exact verification copy: ``VerifiedExtract/OpenPlan.exe``
- Exact extracted launch/smoke: PASS
- Playtest/capture resolution: 1920x1080 windowed
- Gameplay captures: $($captures.Count), exact-name and dimension checked
- Navigation: deterministic 0.45 m four-neighbor A* grid, 0.28 m clearance, registered-obstacle/locked-space rejection, line-of-sight smoothing, explicit invalidation
- Performance: staggered scheduled evaluation, stable station cache, reused candidate/path buffers, no complete per-frame destination scan; matrix setup allocations are not profiler-grade runtime evidence
- Human playtest: explicitly waived by project owner; no manual acceptance claimed

## Known limitations

$knownIssues

## Source documents updated

- ``README.md``
- ``Docs/SIMULATION_RULES.md``
- ``Docs/FINAL_TUNING_VALUES.md``
- ``Docs/KNOWN_ISSUES.md``
- ``Docs/TEST_REPORT.md``
- ``Docs/BUILD_AND_RUN.md``
- ``Docs/RUN_STATE.md``
- ``Docs/MASTER_ROADMAP.md``
- ``Docs/NEXT_30_DAYS_ROADMAP.md``
- ``Docs/DECISION_LOG.md``
- Tutorial and Help runtime copy
"@
    [IO.File]::WriteAllText((Join-Path $stagingRoot 'manifest.md'),$manifest,[Text.UTF8Encoding]::new($false))

    if (Test-Path -LiteralPath $finalRoot) { throw "Checkpoint appeared during packaging: $finalRoot" }
    [IO.Directory]::Move($stagingRoot,$finalRoot)

    [ordered]@{
        checkpoint=$finalRoot
        executable=(Join-Path $finalRoot 'Windows\OpenPlan.exe')
        zip=(Join-Path $finalRoot $zipName)
        verifiedExecutable=(Join-Path $finalRoot 'VerifiedExtract\OpenPlan.exe')
        manifest=(Join-Path $finalRoot 'manifest.md')
        editModePassed=$edit.passed
        playModePassed=$play.passed
        totalPassed=$edit.passed + $play.passed
        buildSizeBytes=$buildBytes
        zipSizeBytes=$zipInfo.Length
        zipSha256=$zipHash
        extractedSmoke='PASS'
        captures=$captures.Count
    } | ConvertTo-Json -Depth 4
}
finally {
    Pop-Location
}
