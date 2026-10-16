[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$rootDirectory = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $rootDirectory "app\bin\$Configuration"
}
else {
    $OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
}

$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
$source = Join-Path $PSScriptRoot "TitleBarThemeSmoke.cs"
$client = Join-Path $OutputDirectory "Grimoire Exodus.exe"
$darkUi = Join-Path $OutputDirectory "DarkUI.dll"
$smokeExecutable = Join-Path $OutputDirectory "TitleBarThemeSmoke.exe"

foreach ($requiredPath in @($compiler, $source, $client, $darkUi)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required title-bar smoke-test input was not found: $requiredPath"
    }
}

& $compiler /nologo /target:exe /platform:x86 /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "/reference:$darkUi" "/out:$smokeExecutable" $source
if ($LASTEXITCODE -ne 0) {
    throw "Compiling the title-bar smoke test failed with exit code $LASTEXITCODE."
}

try {
    & $smokeExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "Title-bar theming smoke test failed with exit code $LASTEXITCODE."
    }
}
finally {
    if (Test-Path -LiteralPath $smokeExecutable) {
        Remove-Item -LiteralPath $smokeExecutable -Force
    }
}
