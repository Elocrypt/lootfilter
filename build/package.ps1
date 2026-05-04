#Requires -Version 7.0
<#
.SYNOPSIS
    Builds LootFilter and packages it into a distributable mod zip.

.DESCRIPTION
    Runs a Release build with the auto-deploy target disabled, then stages the
    mod files (DLL + modinfo.json + modicon.png + assets) into build/stage and
    zips them into build/dist/LootFilter_<Version>.zip.

.PARAMETER Configuration
    MSBuild configuration to build. Defaults to Release.

.PARAMETER Version
    Version string embedded in the output zip filename. Should match the version
    in modinfo.json and the git tag.

.EXAMPLE
    ./build/package.ps1 -Version 1.1.0
#>

param(
    [string]$Configuration = "Release",
    [Parameter(Mandatory = $true)][string]$Version
)

$ErrorActionPreference = "Stop"

$Root       = Split-Path -Parent $PSScriptRoot
$SrcProject = Join-Path $Root "src/LootFilter/LootFilter.csproj"
$StageRoot  = Join-Path $Root "build/stage"
$DistRoot   = Join-Path $Root "build/dist"
$Artifact   = Join-Path $DistRoot "LootFilter_$Version.zip"
$OutputPath = Join-Path $Root "src/LootFilter/bin/$Configuration/net10.0"

Write-Host "== LootFilter $Version ($Configuration) ==" -ForegroundColor Cyan

# Clean stage, ensure dist exists.
if (Test-Path $StageRoot) { Remove-Item $StageRoot -Recurse -Force }
if (-not (Test-Path $DistRoot)) { New-Item $DistRoot -ItemType Directory | Out-Null }
New-Item $StageRoot -ItemType Directory | Out-Null

# Build with deploy-to-game disabled — packaging is independent of the dev machine's VS install.
Write-Host "-- Building" -ForegroundColor Cyan
dotnet build $SrcProject --configuration $Configuration /p:DeployMod=false
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

# Stage required mod files.
Write-Host "-- Staging" -ForegroundColor Cyan
$requiredFiles = @("LootFilter.dll", "modinfo.json")
foreach ($file in $requiredFiles) {
    $source = Join-Path $OutputPath $file
    if (-not (Test-Path $source)) { throw "Missing required file: $source" }
    Copy-Item $source $StageRoot
}

$optionalIcon = Join-Path $OutputPath "modicon.png"
if (Test-Path $optionalIcon) { Copy-Item $optionalIcon $StageRoot }

$assetsDir = Join-Path $OutputPath "assets"
if (Test-Path $assetsDir) { Copy-Item $assetsDir $StageRoot -Recurse }

# Zip.
Write-Host "-- Zipping" -ForegroundColor Cyan
if (Test-Path $Artifact) { Remove-Item $Artifact -Force }
Compress-Archive -Path (Join-Path $StageRoot "*") -DestinationPath $Artifact

Write-Host "== Packaged: $Artifact ==" -ForegroundColor Green