param(
    [string]$Version = "0.1.0",
    [string]$CommitSha = "",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

function Resolve-RepositoryRoot {
    $directory = Get-Item -LiteralPath $PSScriptRoot
    while ($null -ne $directory) {
        if (Test-Path -LiteralPath (Join-Path $directory.FullName "ChaosChess.AI.sln")) {
            return $directory.FullName
        }

        $directory = $directory.Parent
    }

    throw "Could not locate ChaosChess.AI.sln."
}

function Get-UpperSha256 {
    param([string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToUpperInvariant()
}

function Write-Utf8NoBom {
    param(
        [string]$Path,
        [string]$Content
    )

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

if ($Version -notmatch '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z.-]+)?$') {
    throw "Version must be SemVer without the leading v. Actual: '$Version'"
}

$repoRoot = Resolve-RepositoryRoot
if ([string]::IsNullOrWhiteSpace($CommitSha)) {
    $CommitSha = (git -C $repoRoot rev-parse HEAD).Trim()
}

if ($CommitSha -notmatch '^[0-9a-fA-F]{40}$') {
    throw "CommitSha must be a 40-character Git SHA. Actual: '$CommitSha'"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts/release"
}

$projectPath = Join-Path $repoRoot "src/ChaosChess.AI/ChaosChess.AI.csproj"
$informationalVersion = "$Version+$CommitSha"

if (-not $NoBuild) {
    dotnet build $projectPath --configuration $Configuration "/p:Version=$Version" "/p:InformationalVersion=$informationalVersion"
}

$targetFramework = "netstandard2.1"
$buildOutput = Join-Path $repoRoot "src/ChaosChess.AI/bin/$Configuration/$targetFramework"
$requiredFiles = @(
    "ChaosChess.AI.dll",
    "ChaosChess.AI.pdb",
    "ChaosChess.AI.xml"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $buildOutput $file
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required build output is missing: $path"
    }
}

$tag = "v$Version"
$packageName = "ChaosChess.AI-$tag-unity"
$packageDirectory = Join-Path $OutputRoot $packageName
$zipPath = Join-Path $OutputRoot "$packageName.zip"

if (Test-Path -LiteralPath $packageDirectory) {
    Remove-Item -LiteralPath $packageDirectory -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null

foreach ($file in $requiredFiles) {
    Copy-Item -LiteralPath (Join-Path $buildOutput $file) -Destination (Join-Path $packageDirectory $file)
}

$assemblyPath = Join-Path $packageDirectory "ChaosChess.AI.dll"
$pdbPath = Join-Path $packageDirectory "ChaosChess.AI.pdb"
$xmlPath = Join-Path $packageDirectory "ChaosChess.AI.xml"

$manifest = [ordered]@{
    schemaVersion = 1
    name = "ChaosChess.AI"
    version = $Version
    tag = $tag
    commitSha = $CommitSha.ToLowerInvariant()
    targetFramework = $targetFramework
    assembly = "ChaosChess.AI.dll"
    files = @(
        [ordered]@{
            path = "ChaosChess.AI.dll"
            sha256 = Get-UpperSha256 $assemblyPath
            size = (Get-Item -LiteralPath $assemblyPath).Length
        },
        [ordered]@{
            path = "ChaosChess.AI.pdb"
            sha256 = Get-UpperSha256 $pdbPath
            size = (Get-Item -LiteralPath $pdbPath).Length
        },
        [ordered]@{
            path = "ChaosChess.AI.xml"
            sha256 = Get-UpperSha256 $xmlPath
            size = (Get-Item -LiteralPath $xmlPath).Length
        }
    )
}

$manifestPath = Join-Path $packageDirectory "manifest.json"
$manifestJson = $manifest | ConvertTo-Json -Depth 6
Write-Utf8NoBom $manifestPath ($manifestJson + "`n")

$sumEntries = @()
foreach ($file in @("ChaosChess.AI.dll", "ChaosChess.AI.pdb", "ChaosChess.AI.xml", "manifest.json")) {
    $path = Join-Path $packageDirectory $file
    $sumEntries += "$(Get-UpperSha256 $path)  $file"
}

$sumsPath = Join-Path $packageDirectory "SHA256SUMS.txt"
Write-Utf8NoBom $sumsPath (($sumEntries -join "`n") + "`n")

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $packageDirectory "*") -DestinationPath $zipPath -CompressionLevel Optimal

Write-Output "PackageDirectory=$packageDirectory"
Write-Output "PackageZip=$zipPath"
Write-Output "Manifest=$manifestPath"
Write-Output "SHA256SUMS=$sumsPath"
