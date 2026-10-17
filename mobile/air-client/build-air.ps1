[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$AirSdkHome = $env:AIR_HOME,

    [string]$KeystorePath = $env:GRIMOIRE_ANDROID_KEYSTORE,

    [string]$KeystorePassword = $env:GRIMOIRE_ANDROID_KEYSTORE_PASSWORD
)

$ErrorActionPreference = "Stop"

$airClientRoot = $PSScriptRoot
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $airClientRoot "..\.."))
$buildRoot = [IO.Path]::GetFullPath((Join-Path $airClientRoot "build"))
$stagingRoot = Join-Path $buildRoot "staging"
$distRoot = Join-Path $repositoryRoot "dist"
$actionScriptRoot = Join-Path $repositoryRoot "game-client\AS3"
$mainSource = Join-Path $actionScriptRoot "grimoire\Root.as"
$descriptorSource = Join-Path $airClientRoot "GrimoireExodus-app.xml"
$emblemSource = Join-Path $repositoryRoot "mobile\app\src\main\res\drawable-nodpi\grimoire_emblem.png"
$bannerSource = Join-Path $repositoryRoot "mobile\app\src\main\res\drawable-nodpi\grimoire_banner.png"
$communityBotSource = Join-Path $repositoryRoot "app\Bot"
$versionLabel = "0.4.4-dev"

function Test-PathWithin([string]$Path, [string]$Parent) {
    $resolvedPath = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    $resolvedParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\') + '\'
    return $resolvedPath.StartsWith($resolvedParent, [StringComparison]::OrdinalIgnoreCase)
}

if ([string]::IsNullOrWhiteSpace($AirSdkHome)) {
    $repositorySdk = Join-Path $repositoryRoot ".tools\AIRSDK_51.4.1.2"
    if (Test-Path $repositorySdk) {
        $AirSdkHome = $repositorySdk
    }
}

if ([string]::IsNullOrWhiteSpace($AirSdkHome)) {
    throw "HARMAN AIR SDK 51.4 is required. Set AIR_HOME or install it under the ignored .tools directory."
}

$AirSdkHome = [IO.Path]::GetFullPath($AirSdkHome)
$compiler = Join-Path $AirSdkHome "bin\amxmlc.bat"
$packager = Join-Path $AirSdkHome "bin\adt.bat"
foreach ($requiredPath in @($compiler, $packager, $mainSource, $descriptorSource, $emblemSource, $bannerSource)) {
    if (-not (Test-Path $requiredPath -PathType Leaf)) {
        throw "Required build input is missing: $requiredPath"
    }
}

if ((Test-Path $stagingRoot) -and (Test-PathWithin $stagingRoot $buildRoot)) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stagingRoot, (Join-Path $stagingRoot "icons"), (Join-Path $stagingRoot "assets"), (Join-Path $stagingRoot "bots\community"), $distRoot | Out-Null

# AIR output is intentionally isolated from game-client/grimoire-exodus.swf.
# The checked-in desktop SWF is a Flash Player publish artifact and an AIR SWF
# will appear loaded in ActiveX without executing its ExternalInterface bridge.
$swfPath = Join-Path $stagingRoot "GrimoireExodus.swf"
$debugBuild = $Configuration -eq "Debug"
& $compiler `
    "-source-path+=$actionScriptRoot" `
    "-includes=grimoire.MobileShell" `
    "-debug=$($debugBuild.ToString().ToLowerInvariant())" `
    "-default-size=960,550" `
    "-default-frame-rate=30" `
    "-default-background-color=#05060B" `
    "-output=$swfPath" `
    $mainSource
if ($LASTEXITCODE -ne 0) {
    throw "The AIR ActionScript compilation failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath $descriptorSource -Destination (Join-Path $stagingRoot "GrimoireExodus-app.xml")
Copy-Item -LiteralPath $bannerSource -Destination (Join-Path $stagingRoot "assets\grimoire_banner.png")
Copy-Item -LiteralPath $emblemSource -Destination (Join-Path $stagingRoot "assets\grimoire_emblem.png")
if (Test-Path -LiteralPath $communityBotSource -PathType Container) {
    Get-ChildItem -LiteralPath $communityBotSource -Filter "*.gbot" -File |
        Copy-Item -Destination (Join-Path $stagingRoot "bots\community")
}

Add-Type -AssemblyName System.Drawing
$sourceImage = [Drawing.Image]::FromFile($emblemSource)
try {
    foreach ($size in @(48, 72, 96, 144, 192)) {
        $bitmap = New-Object Drawing.Bitmap $size, $size
        try {
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.DrawImage($sourceImage, 0, 0, $size, $size)
            }
            finally {
                $graphics.Dispose()
            }
            $bitmap.Save((Join-Path $stagingRoot "icons\icon$size.png"), [Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $bitmap.Dispose()
        }
    }
}
finally {
    $sourceImage.Dispose()
}

if ($Configuration -eq "Debug") {
    if ([string]::IsNullOrWhiteSpace($KeystorePath)) {
        $KeystorePath = Join-Path $repositoryRoot ".tools\grimoire-android-development.p12"
    }
    if ([string]::IsNullOrWhiteSpace($KeystorePassword)) {
        $passwordPath = Join-Path $repositoryRoot ".tools\grimoire-android-development.password"
        if (Test-Path $passwordPath -PathType Leaf) {
            $KeystorePassword = (Get-Content -LiteralPath $passwordPath -Raw).Trim()
        }
        else {
            $KeystorePassword = ([Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N"))
            Set-Content -LiteralPath $passwordPath -Value $KeystorePassword -NoNewline
        }
    }
    if (-not (Test-Path $KeystorePath)) {
        $keystoreParent = Split-Path -Parent $KeystorePath
        New-Item -ItemType Directory -Force -Path $keystoreParent | Out-Null
        & $packager -certificate -cn "Grimoire Exodus Development" 2048-RSA $KeystorePath $KeystorePassword
        if ($LASTEXITCODE -ne 0) {
            throw "The AIR development certificate could not be created."
        }
    }
    $target = "apk-debug"
}
else {
    if ([string]::IsNullOrWhiteSpace($KeystorePath) -or [string]::IsNullOrWhiteSpace($KeystorePassword)) {
        throw "Release builds require GRIMOIRE_ANDROID_KEYSTORE and GRIMOIRE_ANDROID_KEYSTORE_PASSWORD."
    }
    if (-not (Test-Path $KeystorePath -PathType Leaf)) {
        throw "The configured release keystore does not exist."
    }
    $target = "apk"
}

$outputName = "Grimoire-Exodus-Android-AIR-$versionLabel-arm64.apk"
$outputPath = Join-Path $distRoot $outputName
Push-Location $stagingRoot
try {
    & $packager `
        -package `
        -target $target `
        -arch armv8 `
        -storetype pkcs12 `
        -keystore $KeystorePath `
        -storepass $KeystorePassword `
        $outputPath `
        "GrimoireExodus-app.xml" `
        "GrimoireExodus.swf" `
        "icons" `
        "assets" `
        "bots"
    if ($LASTEXITCODE -ne 0) {
        throw "The AIR Android package failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

$hash = (Get-FileHash -LiteralPath $outputPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Built $outputName"
Write-Host "SHA-256: $hash"
