[CmdletBinding()]
param(
    [string]$CheckpointId = '00_Foundation',
    [string]$CheckpointSlug = '00-Foundation',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe',
    [string]$GuideMarkdownPath = 'Docs/Playtests/00_Foundation_PLAYTEST_GUIDE.md',
    [string]$GuideTextPath = 'Docs/Playtests/00_Foundation_PLAYTEST_GUIDE.txt',
    [string]$KnownLimitationsPath = 'Docs/Playtests/00_Foundation_KNOWN_LIMITATIONS.txt',
    [int]$MinimumEditModeTests = 53,
    [int]$MinimumPlayModeTests = 56,
    [int]$SmokeTimeoutSeconds = 240
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Quote-ProcessArgument([string]$Value) {
    if ($Value -notmatch '[\s"]') { return $Value }
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Invoke-ProcessChecked {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [int]$TimeoutSeconds = 0,
        [switch]$Visible
    )
    $argumentLine = (($Arguments | ForEach-Object { Quote-ProcessArgument $_ }) -join ' ')
    $startParameters = @{
        FilePath = $FilePath
        ArgumentList = $argumentLine
        PassThru = $true
    }
    if (-not $Visible) { $startParameters.WindowStyle = 'Hidden' }
    $process = Start-Process @startParameters
    if ($TimeoutSeconds -gt 0) {
        if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
            Stop-Process -Id $process.Id -Force
            throw "Process timed out after $TimeoutSeconds seconds: $FilePath"
        }
    }
    else {
        $process.WaitForExit()
    }
    $process.Refresh()
    if ($process.ExitCode -ne 0) {
        throw "Process exited with code $($process.ExitCode): $FilePath $argumentLine"
    }
}

function Read-TestResult([string]$Path, [int]$Minimum, [string]$Suite) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Suite did not produce a test result: $Path"
    }
    [xml]$document = Get-Content -LiteralPath $Path
    $run = $document.'test-run'
    $result = [ordered]@{
        passed = [int]$run.passed
        failed = [int]$run.failed
        skipped = [int]$run.skipped
        total = [int]$run.total
        durationSeconds = [double]$run.duration
    }
    if ($run.result -ne 'Passed' -or $result.failed -ne 0 -or $result.passed -lt $Minimum) {
        throw "$Suite gate failed: result=$($run.result) passed=$($result.passed) failed=$($result.failed) minimum=$Minimum"
    }
    return $result
}

function Assert-SourceClean {
    $status = @(& git status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'git status failed.' }
    if ($status.Count -gt 0) {
        throw "Checkpoint packaging requires a clean source tree. Current status:`n$($status -join "`n")"
    }
}

function Remove-SafeTemporaryDirectory([string]$Path, [string]$AllowedRoot) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    $resolvedRoot = [IO.Path]::GetFullPath($AllowedRoot).TrimEnd('\') + '\'
    $leaf = Split-Path -Leaf $resolvedPath
    if (-not $resolvedPath.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase) -or
        ($leaf -notlike '.staging-*' -and $leaf -notlike '.verify-*' -and $leaf -notlike '.publish-*')) {
        throw "Refusing to remove an unverified temporary path: $resolvedPath"
    }
    Remove-Item -LiteralPath $resolvedPath -Recurse -Force
}

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$playtestsRoot = Join-Path $repoRoot 'outputs\Playtests\EndlessOfficeAlpha'
$finalDirectory = Join-Path $playtestsRoot $CheckpointId
$token = [Guid]::NewGuid().ToString('N')
$stagingDirectory = Join-Path $playtestsRoot ".staging-$CheckpointId-$token"
$verificationDirectory = Join-Path $playtestsRoot ".verify-$CheckpointId-$token"
$publishDirectory = Join-Path $playtestsRoot ".publish-$CheckpointId-$token"
$buildDirectory = Join-Path $stagingDirectory 'Build'
$evidenceDirectory = Join-Path $stagingDirectory 'Evidence'
$logsDirectory = Join-Path $stagingDirectory 'Logs'
$testDirectory = Join-Path $stagingDirectory 'TestResults'
$zipFilename = "EndlessOfficeAlpha-$CheckpointSlug.zip"
$stagingZip = Join-Path $stagingDirectory $zipFilename

Push-Location $repoRoot
try {
    if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
        throw "Unity editor not found: $UnityPath"
    }
    Assert-SourceClean
    if (Test-Path -LiteralPath $finalDirectory) {
        throw "Checkpoint already exists and will not be overwritten: $finalDirectory"
    }

    New-Item -ItemType Directory -Path $buildDirectory, $evidenceDirectory, $logsDirectory, $testDirectory -Force | Out-Null
    $editResultPath = Join-Path $testDirectory "$CheckpointId-EditMode.xml"
    $playResultPath = Join-Path $testDirectory "$CheckpointId-PlayMode.xml"
    $editLogPath = Join-Path $logsDirectory "$CheckpointId-EditMode.log"
    $playLogPath = Join-Path $logsDirectory "$CheckpointId-PlayMode.log"
    $buildLogPath = Join-Path $logsDirectory "$CheckpointId-build.log"
    $smokeLogPath = Join-Path $evidenceDirectory "$CheckpointId-smoke.log"

    Invoke-ProcessChecked -FilePath $UnityPath -Arguments @(
        '-batchmode', '-nographics', '-projectPath', $repoRoot,
        '-runTests', '-testPlatform', 'EditMode', '-testResults', $editResultPath,
        '-logFile', $editLogPath
    )
    $edit = Read-TestResult -Path $editResultPath -Minimum $MinimumEditModeTests -Suite 'EditMode'

    Invoke-ProcessChecked -FilePath $UnityPath -Arguments @(
        '-batchmode', '-nographics', '-projectPath', $repoRoot,
        '-runTests', '-testPlatform', 'PlayMode', '-testResults', $playResultPath,
        '-logFile', $playLogPath
    )
    $play = Read-TestResult -Path $playResultPath -Minimum $MinimumPlayModeTests -Suite 'PlayMode'

    $executablePath = Join-Path $buildDirectory 'OpenPlan.exe'
    Invoke-ProcessChecked -FilePath $UnityPath -Arguments @(
        '-batchmode', '-nographics', '-quit', '-projectPath', $repoRoot,
        '-executeMethod', 'OpenPlan.Editor.CheckpointBuildPipeline.BuildWindowsPlayer',
        '-checkpointBuildPath', $executablePath, '-logFile', $buildLogPath
    )
    if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
        throw "Windows build did not create $executablePath"
    }
    $buildTimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')

    $guideMarkdown = Join-Path $repoRoot $GuideMarkdownPath
    $guideText = Join-Path $repoRoot $GuideTextPath
    $limitationsSource = Join-Path $repoRoot $KnownLimitationsPath
    foreach ($source in @($guideMarkdown, $guideText, $limitationsSource)) {
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing checkpoint source document: $source" }
    }
    Copy-Item -LiteralPath $guideMarkdown -Destination (Join-Path $buildDirectory 'PLAYTEST_GUIDE.md')
    Copy-Item -LiteralPath $guideText -Destination (Join-Path $buildDirectory 'PLAYTEST_GUIDE.txt')

    $branch = (& git branch --show-current).Trim()
    $sourceCommit = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve source commit.' }
    $unityVersionLine = Get-Content -LiteralPath (Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt') -First 1
    $unityVersion = $unityVersionLine.Split(':', 2)[1].Trim()
    $testSummary = @"
# Checkpoint 00 Test Summary

- Source commit: ``$sourceCommit``
- Unity: ``$unityVersion``
- Configuration: Windows x64, non-development, Mono scripting backend

| Suite | Passed | Failed | Skipped | Duration |
|---|---:|---:|---:|---:|
| EditMode | $($edit.passed) | $($edit.failed) | $($edit.skipped) | $([Math]::Round($edit.durationSeconds, 3)) s |
| PlayMode | $($play.passed) | $($play.failed) | $($play.skipped) | $([Math]::Round($play.durationSeconds, 3)) s |

The exact ZIP was extracted into a separate verification directory and its ``OpenPlan.exe`` completed the public-API Checkpoint 00 smoke flow. See ``FOUNDATION_SMOKE.txt`` and the checkpoint screenshots for the standalone evidence.
"@
    [IO.File]::WriteAllText((Join-Path $buildDirectory 'TEST_SUMMARY.md'), $testSummary, [Text.UTF8Encoding]::new($false))

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $stagingZip) { throw "ZIP unexpectedly already exists: $stagingZip" }
    [IO.Compression.ZipFile]::CreateFromDirectory(
        $buildDirectory, $stagingZip, [IO.Compression.CompressionLevel]::Optimal, $false)
    New-Item -ItemType Directory -Path $verificationDirectory | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($stagingZip, $verificationDirectory)
    $extractedExecutable = Join-Path $verificationDirectory 'OpenPlan.exe'
    if (-not (Test-Path -LiteralPath $extractedExecutable -PathType Leaf)) {
        throw 'Extracted package is missing OpenPlan.exe.'
    }

    Invoke-ProcessChecked -FilePath $extractedExecutable -TimeoutSeconds $SmokeTimeoutSeconds -Visible -Arguments @(
        '-screen-width', '1920', '-screen-height', '1080', '-screen-fullscreen', '0',
        '-logFile', $smokeLogPath, '-openplan-foundation-smoke',
        '-openplan-evidence-root', $evidenceDirectory
    )
    $smokeReportPath = Join-Path $evidenceDirectory 'FOUNDATION_SMOKE.txt'
    if (-not (Test-Path -LiteralPath $smokeReportPath -PathType Leaf) -or
        (Get-Content -LiteralPath $smokeReportPath -First 1) -ne 'STATUS PASS') {
        throw 'Extracted-package smoke report did not pass.'
    }

    $screenshotsDirectory = Join-Path $evidenceDirectory 'Screenshots'
    $screenshots = @(Get-ChildItem -LiteralPath $screenshotsDirectory -Filter '*.png' -File)
    if ($screenshots.Count -lt 11) { throw "Expected at least 11 checkpoint screenshots, found $($screenshots.Count)." }
    Add-Type -AssemblyName System.Drawing
    foreach ($screenshot in $screenshots) {
        $image = [Drawing.Image]::FromFile($screenshot.FullName)
        try {
            $expectedWidth = if ($screenshot.Name -like '*1280x720*') { 1280 } else { 1920 }
            $expectedHeight = if ($screenshot.Name -like '*1280x720*') { 720 } else { 1080 }
            if ($image.Width -ne $expectedWidth -or $image.Height -ne $expectedHeight) {
                throw "Unexpected screenshot dimensions for $($screenshot.Name): $($image.Width)x$($image.Height)"
            }
        }
        finally { $image.Dispose() }
    }

    Assert-SourceClean
    $zipHash = (Get-FileHash -LiteralPath $stagingZip -Algorithm SHA256).Hash.ToUpperInvariant()
    $buildSizeBytes = [long](Get-ChildItem -LiteralPath $buildDirectory -File -Recurse |
        Measure-Object -Property Length -Sum).Sum
    $knownLimitations = @(Get-Content -LiteralPath $limitationsSource |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.TrimStart().StartsWith('#') } |
        ForEach-Object { [string]::Concat($_) })
    $manifest = [ordered]@{
        productName = 'OPEN PLAN'
        checkpointId = $CheckpointId
        branch = $branch
        sourceCommit = $sourceCommit
        unityVersion = $unityVersion
        buildConfiguration = 'Windows x64; non-development; Mono scripting backend'
        buildTimestampUtc = $buildTimestampUtc
        buildSizeBytes = $buildSizeBytes
        editMode = $edit
        playMode = $play
        zipFilename = $zipFilename
        zipSha256 = $zipHash
        knownLimitations = $knownLimitations
        extractedPackageSmokeTestPassed = $true
    }
    $manifestPath = Join-Path $stagingDirectory 'CHECKPOINT_MANIFEST.json'
    $manifestJson = $manifest | ConvertTo-Json -Depth 6
    if ($manifestJson.Length -gt 65536) {
        throw "Checkpoint manifest unexpectedly exceeded 64 KiB: $($manifestJson.Length) characters."
    }
    [IO.File]::WriteAllText($manifestPath, $manifestJson, [Text.UTF8Encoding]::new($false))

    New-Item -ItemType Directory -Path $publishDirectory | Out-Null
    Copy-Item -Path (Join-Path $buildDirectory '*') -Destination $publishDirectory -Recurse
    Copy-Item -LiteralPath $stagingZip -Destination (Join-Path $publishDirectory $zipFilename)
    Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $publishDirectory 'CHECKPOINT_MANIFEST.json')
    Copy-Item -LiteralPath $buildLogPath -Destination (Join-Path $publishDirectory "$CheckpointId-build.log")
    Copy-Item -LiteralPath $editLogPath -Destination (Join-Path $publishDirectory "$CheckpointId-EditMode.log")
    Copy-Item -LiteralPath $playLogPath -Destination (Join-Path $publishDirectory "$CheckpointId-PlayMode.log")
    Copy-Item -LiteralPath $smokeLogPath -Destination (Join-Path $publishDirectory "$CheckpointId-smoke.log")
    Copy-Item -LiteralPath $smokeReportPath -Destination (Join-Path $publishDirectory 'FOUNDATION_SMOKE.txt')
    Copy-Item -LiteralPath $screenshotsDirectory -Destination (Join-Path $publishDirectory 'Screenshots') -Recurse
    Copy-Item -LiteralPath $testDirectory -Destination (Join-Path $publishDirectory 'TestResults') -Recurse

    if (Test-Path -LiteralPath $finalDirectory) {
        throw "Checkpoint appeared during packaging and will not be overwritten: $finalDirectory"
    }
    [IO.Directory]::Move($publishDirectory, $finalDirectory)
    Remove-SafeTemporaryDirectory -Path $verificationDirectory -AllowedRoot $playtestsRoot
    Remove-SafeTemporaryDirectory -Path $stagingDirectory -AllowedRoot $playtestsRoot

    [ordered]@{
        checkpointDirectory = $finalDirectory
        executable = Join-Path $finalDirectory 'OpenPlan.exe'
        zip = Join-Path $finalDirectory $zipFilename
        manifest = Join-Path $finalDirectory 'CHECKPOINT_MANIFEST.json'
        editModePassed = $edit.passed
        playModePassed = $play.passed
        buildSizeBytes = $buildSizeBytes
        zipSha256 = $zipHash
        extractedSmokePassed = $true
    } | ConvertTo-Json -Depth 4
}
finally {
    Pop-Location
}
