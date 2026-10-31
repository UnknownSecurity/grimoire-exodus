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

$compiler = Get-ChildItem "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022" -Filter csc.exe -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\Roslyn\\bincore\\csc\.exe$|\\Roslyn\\csc\.exe$' } |
    Select-Object -First 1 -ExpandProperty FullName
if ([string]::IsNullOrWhiteSpace($compiler)) {
    throw "A Visual Studio Roslyn compiler is required for the Account Locker profile test. Install the .NET desktop build tools."
}
$vaultSource = Join-Path $rootDirectory "app\Grimoire.Tools\AccountVault.cs"
$visualSource = Join-Path $rootDirectory "app\Grimoire.UI\AccountCharacterVisual.cs"
$testSource = Join-Path $PSScriptRoot "AccountVaultProfileSmoke.cs"
$newtonsoftJson = Join-Path $OutputDirectory "Newtonsoft.Json.dll"
$smokeExecutable = Join-Path $OutputDirectory "AccountVaultProfileSmoke.exe"

foreach ($requiredPath in @($compiler, $vaultSource, $visualSource, $testSource, $newtonsoftJson, $OutputDirectory)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required Account Locker profile smoke-test input was not found: $requiredPath"
    }
}

& $compiler /nologo /target:exe /platform:x86 /langversion:7.3 /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Security.dll /reference:System.Windows.Forms.dll "/reference:$newtonsoftJson" "/out:$smokeExecutable" $vaultSource $visualSource $testSource
if ($LASTEXITCODE -ne 0) {
    throw "Compiling the Account Locker profile smoke test failed with exit code $LASTEXITCODE."
}

try {
    & $smokeExecutable
    if ($LASTEXITCODE -ne 0) {
        throw "Account Locker profile smoke test failed with exit code $LASTEXITCODE."
    }
}
finally {
    if (Test-Path -LiteralPath $smokeExecutable) {
        Remove-Item -LiteralPath $smokeExecutable -Force
    }
}
