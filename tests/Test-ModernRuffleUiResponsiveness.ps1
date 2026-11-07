[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
$outputDirectory = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    Join-Path $rootDirectory "app\bin\$Configuration"
}
else {
    [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($OutputDirectory)) {
        $OutputDirectory
    } else {
        Join-Path $rootDirectory $OutputDirectory
    }))
}
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
$compiler = $null
if (Test-Path -LiteralPath $vswhere) {
    $visualStudio = (& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath | Select-Object -First 1)
    if (-not [string]::IsNullOrWhiteSpace($visualStudio)) {
        $roslynCompiler = Join-Path $visualStudio "MSBuild\Current\Bin\Roslyn\csc.exe"
        if (Test-Path -LiteralPath $roslynCompiler) {
            $compiler = $roslynCompiler
        }
    }
}
if ([string]::IsNullOrWhiteSpace($compiler)) {
    throw "A Visual Studio Roslyn compiler is required for the Modern Ruffle responsiveness test."
}
$testSource = Join-Path $PSScriptRoot "ModernRuffleUiResponsivenessSmoke.cs"
$hostSource = Join-Path $rootDirectory "app\Grimoire.UI.Modern\ModernShellHost.cs"
$protocolSource = Join-Path $rootDirectory "app\Grimoire.UI.Modern\ModernUiProtocol.cs"
$applicationSource = Join-Path $rootDirectory "app\Grimoire.UI.Modern\IModernUiApplication.cs"
$frameSource = Join-Path $rootDirectory "app\Grimoire.Clients\IRuffleFrameHost.cs"
$webViewCore = Join-Path $outputDirectory "Microsoft.Web.WebView2.Core.dll"
$webViewWinForms = Join-Path $outputDirectory "Microsoft.Web.WebView2.WinForms.dll"
$json = Join-Path $outputDirectory "Newtonsoft.Json.dll"
$smokeExecutable = Join-Path $outputDirectory "ModernRuffleUiResponsivenessSmoke-$PID.exe"
$profileDirectory = Join-Path ([IO.Path]::GetTempPath()) "ModernRuffleSmokeProfile-$PID"
$previousProfile = $env:GRIMOIRE_MODERN_SMOKE_PROFILE

foreach ($requiredPath in @($compiler, $testSource, $hostSource, $protocolSource, $applicationSource, $frameSource, $webViewCore, $webViewWinForms, $json)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required Modern Ruffle responsiveness-test input was not found: $requiredPath"
    }
}

try {
    $env:GRIMOIRE_MODERN_SMOKE_PROFILE = $profileDirectory
    & $compiler /nologo /target:exe /platform:x86 /langversion:7.3 `
        "/reference:$webViewCore" `
        "/reference:$webViewWinForms" `
        "/reference:$json" `
        /reference:System.Core.dll `
        /reference:System.Drawing.dll `
        /reference:System.Windows.Forms.dll `
        "/out:$smokeExecutable" `
        $testSource $hostSource $protocolSource $applicationSource $frameSource
    if ($LASTEXITCODE -ne 0) {
        throw "Compiling the Modern Ruffle responsiveness smoke test failed with exit code $LASTEXITCODE."
    }

    Push-Location $outputDirectory
    try {
        & $smokeExecutable
        if ($LASTEXITCODE -ne 0) {
            throw "Modern Ruffle responsiveness smoke test failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    $env:GRIMOIRE_MODERN_SMOKE_PROFILE = $previousProfile
    if (Test-Path -LiteralPath $smokeExecutable) {
        Remove-Item -LiteralPath $smokeExecutable -Force
    }
    if (Test-Path -LiteralPath $profileDirectory) {
        $resolvedOutput = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $resolvedProfile = [IO.Path]::GetFullPath($profileDirectory)
        if ($resolvedProfile.StartsWith($resolvedOutput, [StringComparison]::OrdinalIgnoreCase) -and
            [IO.Path]::GetFileName($resolvedProfile).StartsWith("ModernRuffleSmokeProfile-", [StringComparison]::Ordinal)) {
            for ($attempt = 0; $attempt -lt 10 -and (Test-Path -LiteralPath $resolvedProfile); $attempt++) {
                try {
                    Remove-Item -LiteralPath $resolvedProfile -Recurse -Force -ErrorAction Stop
                }
                catch {
                    if ($attempt -eq 9) {
                        Write-Warning "The isolated Modern Ruffle test profile is still in use and could not be removed."
                    }
                    else {
                        Start-Sleep -Milliseconds 200
                    }
                }
            }
        }
    }
}
