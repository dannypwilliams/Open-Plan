[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe',
    [int]$MinimumEditModeTests = 98,
    [int]$MinimumPlayModeTests = 70,
    [int]$SmokeTimeoutSeconds = 300
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
    if ($status.Count -gt 0) { throw "Prompt 01 packaging requires clean committed source:`n$($status -join "`n")" }
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$playtestRoot = Join-Path $repoRoot 'outputs\Playtests\EndlessOfficeAlpha'
$finalRoot = Join-Path $playtestRoot '01_FiveNeeds'
$stagingRoot = Join-Path $playtestRoot ('.staging-01_FiveNeeds-' + [Guid]::NewGuid().ToString('N'))
$windowsRoot = Join-Path $stagingRoot 'Windows'
$verifiedRoot = Join-Path $stagingRoot 'VerifiedExtract'
$testRoot = Join-Path $stagingRoot 'test-results'
$captureRoot = Join-Path $stagingRoot 'captures'
$zipName = 'SillyOfficeSim_01_FiveNeeds_Windows.zip'
$zipPath = Join-Path $stagingRoot $zipName

Push-Location $repoRoot
try {
    if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) { throw "Unity not found: $UnityPath" }
    Assert-CleanSource
    if (Test-Path -LiteralPath $finalRoot) { throw "Refusing to overwrite checkpoint: $finalRoot" }
    New-Item -ItemType Directory -Path $windowsRoot,$verifiedRoot,$testRoot,$captureRoot -Force | Out-Null

    $editXml = Join-Path $testRoot '01_FiveNeeds-EditMode.xml'
    $editLog = Join-Path $testRoot '01_FiveNeeds-EditMode.log'
    $playXml = Join-Path $testRoot '01_FiveNeeds-PlayMode.xml'
    $playLog = Join-Path $testRoot '01_FiveNeeds-PlayMode.log'
    $reportLog = Join-Path $testRoot '01_FiveNeeds-Deterministic.log'
    $buildLog = Join-Path $stagingRoot 'build.log'
    $smokeLog = Join-Path $testRoot '01_FiveNeeds-Extracted-Smoke.log'

    Invoke-CheckedProcess -FilePath $UnityPath -Arguments @('-batchmode','-nographics','-projectPath',$repoRoot,
        '-runTests','-testPlatform','EditMode','-testResults',$editXml,'-logFile',$editLog)
    $edit = Read-TestResult $editXml $MinimumEditModeTests 'EditMode'

    Invoke-CheckedProcess -FilePath $UnityPath -Arguments @('-batchmode','-nographics','-projectPath',$repoRoot,
        '-runTests','-testPlatform','PlayMode','-testResults',$playXml,'-logFile',$playLog)
    $play = Read-TestResult $playXml $MinimumPlayModeTests 'PlayMode'

    Invoke-CheckedProcess -FilePath $UnityPath -Arguments @('-batchmode','-nographics','-projectPath',$repoRoot,
        '-executeMethod','OpenPlan.Editor.FiveNeedsReportGenerator.GenerateFromCommandLine','-logFile',$reportLog)
    $generatedReport = Join-Path $repoRoot 'outputs\TestResults\01_FiveNeeds_Deterministic_Report.md'
    if (-not (Test-Path -LiteralPath $generatedReport -PathType Leaf)) { throw 'Deterministic report was not generated.' }
    Copy-Item -LiteralPath $generatedReport -Destination (Join-Path $testRoot '01_FiveNeeds_Deterministic_Report.md')

    $executable = Join-Path $windowsRoot 'OpenPlan.exe'
    Invoke-CheckedProcess -FilePath $UnityPath -Arguments @('-batchmode','-nographics','-quit','-projectPath',$repoRoot,
        '-executeMethod','OpenPlan.Editor.CheckpointBuildPipeline.BuildWindowsPlayer',
        '-checkpointBuildPath',$executable,'-logFile',$buildLog)
    if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw 'Windows player was not created.' }

    $guideSource = Join-Path $repoRoot 'Docs\Playtests\01_FiveNeeds_PLAYTEST_GUIDE.md'
    $issuesSource = Join-Path $repoRoot 'Docs\Playtests\01_FiveNeeds_KNOWN_ISSUES.md'
    Copy-Item -LiteralPath $guideSource -Destination (Join-Path $stagingRoot 'playtest-guide.md')
    Copy-Item -LiteralPath $issuesSource -Destination (Join-Path $stagingRoot 'known-issues.md')

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($windowsRoot,$zipPath,[IO.Compression.CompressionLevel]::Optimal,$false)
    [IO.Compression.ZipFile]::ExtractToDirectory($zipPath,$verifiedRoot)
    $verifiedExecutable = Join-Path $verifiedRoot 'OpenPlan.exe'
    if (-not (Test-Path -LiteralPath $verifiedExecutable -PathType Leaf)) { throw 'Exact extracted executable is missing.' }

    Invoke-CheckedProcess -FilePath $verifiedExecutable -TimeoutSeconds $SmokeTimeoutSeconds -Visible -Arguments @(
        '-screen-width','1920','-screen-height','1080','-screen-fullscreen','0',
        '-logFile',$smokeLog,'-openplan-five-needs-smoke','-openplan-evidence-root',$stagingRoot)
    $smokeReport = Join-Path $stagingRoot 'FIVE_NEEDS_SMOKE.txt'
    if (-not (Test-Path -LiteralPath $smokeReport -PathType Leaf) -or
        (Get-Content -LiteralPath $smokeReport -First 1) -ne 'STATUS PASS') { throw 'Exact extracted smoke did not pass.' }

    $captures = @(Get-ChildItem -LiteralPath $captureRoot -Filter '*.png' -File)
    if ($captures.Count -lt 9) { throw "Expected nine gameplay captures, found $($captures.Count)." }
    Add-Type -AssemblyName System.Drawing
    foreach ($capture in $captures) {
        $image = [Drawing.Image]::FromFile($capture.FullName)
        try {
            $width = if ($capture.Name -like '*1280x720*') { 1280 } else { 1920 }
            $height = if ($capture.Name -like '*1280x720*') { 720 } else { 1080 }
            if ($image.Width -ne $width -or $image.Height -ne $height) {
                throw "Unexpected capture size $($image.Width)x$($image.Height): $($capture.Name)"
            }
        } finally { $image.Dispose() }
    }

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
# Endless Office Alpha - Checkpoint 01 Five Needs

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
- Exact verification copy: ``VerifiedExtract/OpenPlan.exe``
- Exact extracted launch/smoke: PASS
- Gameplay captures: $($captures.Count), dimension-checked
- Deterministic matrix: PASS; 3/10/30 workers, 20 seeds, six contexts, 100 simulated minutes per row

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
    } | ConvertTo-Json -Depth 4
}
finally {
    Pop-Location
}
