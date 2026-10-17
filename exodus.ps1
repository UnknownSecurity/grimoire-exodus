[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("build", "dev", "runtime", "installer", "version", "plugin", "help", "man")]
    [string]$Command = "help",
    [ValidateSet("Debug", "Release", "All")]
    [string]$Configuration = "Debug",
    [ValidateSet("major", "minor", "patch", "revision")]
    [string]$Part = "patch",
    [string]$Name = "ExodusPlugin",
    [string]$OutputDirectory,
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"
$script:RootDirectory = $PSScriptRoot
$script:SolutionPath = Join-Path $script:RootDirectory "app\Grimoire Exodus.sln"
$script:AssemblyInfoPath = Join-Path $script:RootDirectory "app\Properties\AssemblyInfo.cs"
$script:UpdaterAssemblyInfoPath = Join-Path $script:RootDirectory "app\Grimoire.Updater\Properties\AssemblyInfo.cs"
$script:ProjectPath = Join-Path $script:RootDirectory "app\Grimoire.csproj"
$script:ManifestPath = Join-Path $script:RootDirectory "app\Grimoire.manifest"
$script:ProgramPath = Join-Path $script:RootDirectory "app\Grimoire\Program.cs"
$script:FlashRuntimeUrl = "https://github.com/darktohka/clean-flash-builds/releases/download/v1.54/ChineseFlash-Patched-Win-34.0.0.376.7z"
$script:FlashArchiveHash = "19A8D1036110AF024DC877CA96A9184835C3E944FB14CA8D509662F00BF1BD31"
$script:FlashOcxHash = "4E1CAA0B6805E5D4E4F737C4389B8E94E92CA49E7D7AEF17C3773A9D744DCC42"
$script:FlashRuntimePath = Join-Path $script:RootDirectory "app\Runtime\Flash\Flash.ocx"
$script:DesktopSwfPath = Join-Path $script:RootDirectory "game-client\grimoire-exodus.swf"
$script:DesktopSwfHash = "39E5689398AF0DB32854BB55F281683A8B0B0E085817D91102A54C0186762BE6"
$script:WixUrl = "https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314-binaries.zip"
$script:WixArchiveHash = "6AC824E1642D6F7277D0ED7EA09411A508F6116BA6FAE0AA5F2C7DAA2FF43D31"

function Show-ExodusBanner {
    $banner = @'
  ____ ____  ___ __  __  ___ ___ ____  _____
 / ___|  _ \|_ _|  \/  |/ _ \_ _|  _ \| ____|
| |  _| |_) || || |\/| | | | | || |_) |  _|
| |_| |  _ < | || |  | | |_| | ||  _ <| |___
 \____|_| \_\___|_|  |_|\___/___|_| \_\_____|

 _____ __  _____  ____  _   _ ____
| ____|\ \/ / _ \|  _ \| | | / ___|
|  _|   \  / | | | | | | | | \___ \
| |___  /  \ |_| | |_| | |_| |___) |
|_____|/_/\_\___/|____/ \___/|____/
'@
    Write-Host $banner -ForegroundColor Magenta
    Write-Host "Grimoire Exodus developer CLI" -ForegroundColor DarkMagenta
    Write-Host ""
}

function Show-ExodusHelp {
    Show-ExodusBanner
    Write-Host "Usage: .\exodus.ps1 <command> [options]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  build    Restore and build Debug, Release, or both configurations."
    Write-Host "  dev      Build Debug with detailed output, then launch the client."
    Write-Host "  runtime  Acquire and verify the private app-local Flash runtime."
    Write-Host "  installer Build the Release configuration and native Windows MSI."
    Write-Host "  version  Increment the four-part application version."
    Write-Host "  plugin   Create an empty plugin development workspace."
    Write-Host "  help     Show this quick reference."
    Write-Host "  man      Show the detailed command manual."
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\exodus.ps1 build -Configuration Debug"
    Write-Host "  .\exodus.ps1 build -Configuration Release"
    Write-Host "  .\exodus.ps1 build -Configuration All"
    Write-Host "  .\exodus.ps1 dev"
    Write-Host "  .\exodus.ps1 runtime"
    Write-Host "  .\exodus.ps1 installer"
    Write-Host "  .\exodus.ps1 version -Part patch"
    Write-Host "  .\exodus.ps1 plugin -Name MyPlugin"
}

function Show-ExodusManual {
    Show-ExodusHelp
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration Debug|Release|All"
    Write-Host "      Select the build configuration. The final directory contains only"
    Write-Host "      Grimoire Exodus.exe; its legacy Grimoire assembly identity is kept"
    Write-Host "      internally for binary compatibility."
    Write-Host ""
    Write-Host "  -NoRestore"
    Write-Host "      Skip NuGet package restore when dependencies are already available."
    Write-Host ""
    Write-Host "  runtime"
    Write-Host "      Downloads the pinned 32-bit Flash ActiveX runtime, verifies the"
    Write-Host "      archive and OCX SHA-256 hashes, and stages it for registration-free"
    Write-Host "      activation. Normal build and installer commands do this automatically."
    Write-Host ""
    Write-Host "  installer"
    Write-Host "      Produces dist\Grimoire-Exodus-Setup-<version>.msi. WiX 3.14.1 is"
    Write-Host "      downloaded as a hash-pinned local build tool when needed."
    Write-Host ""
    Write-Host "  -Part major|minor|patch|revision"
    Write-Host "      Choose the version component to increment. Components to its right"
    Write-Host "      are reset to zero. AssemblyVersion and AssemblyFileVersion are kept"
    Write-Host "      in sync, and the version date is updated."
    Write-Host ""
    Write-Host "  -Name <plugin-name>"
    Write-Host "      Name the generated plugin project and entry class."
    Write-Host ""
    Write-Host "  -OutputDirectory <path>"
    Write-Host "      Override the default plugin-workspaces\<name> destination."
}

function Assert-FileHash([string]$Path, [string]$ExpectedHash, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description was not found: $Path"
    }

    $actualHash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    if ($actualHash -ne $ExpectedHash) {
        throw "$Description failed SHA-256 verification. Expected $ExpectedHash but found $actualHash."
    }
}

function Invoke-VerifiedDownload([string]$Uri, [string]$Destination, [string]$ExpectedHash, [string]$Description) {
    $parent = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Path $parent -Force | Out-Null
    $partial = "$Destination.download"

    if (Test-Path -LiteralPath $partial) {
        Remove-Item -LiteralPath $partial -Force
    }

    Write-Host "Downloading $Description..." -ForegroundColor Cyan
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $client = New-Object Net.WebClient
    $client.Headers["User-Agent"] = "Grimoire-Exodus-Build/1.4.0"
    try {
        $client.DownloadFile($Uri, $partial)
    }
    finally {
        $client.Dispose()
    }

    Assert-FileHash $partial $ExpectedHash $Description
    if (Test-Path -LiteralPath $Destination) {
        Remove-Item -LiteralPath $Destination -Force
    }
    Move-Item -LiteralPath $partial -Destination $Destination
}

function Get-BundledFlashRuntime {
    if (Test-Path -LiteralPath $script:FlashRuntimePath) {
        try {
            Assert-FileHash $script:FlashRuntimePath $script:FlashOcxHash "Bundled Flash runtime"
            Write-Host "Flash runtime: $($script:FlashRuntimePath)" -ForegroundColor DarkGray
            return $script:FlashRuntimePath
        }
        catch {
            Write-Host "Replacing an unverified Flash runtime." -ForegroundColor Yellow
            Remove-Item -LiteralPath $script:FlashRuntimePath -Force
        }
    }

    $cacheDirectory = Join-Path $script:RootDirectory ".tools\cache"
    $archive = Join-Path $cacheDirectory "ChineseFlash-Patched-Win-34.0.0.376.7z"
    if (Test-Path -LiteralPath $archive) {
        try {
            Assert-FileHash $archive $script:FlashArchiveHash "Flash runtime archive"
        }
        catch {
            Remove-Item -LiteralPath $archive -Force
        }
    }
    if (-not (Test-Path -LiteralPath $archive)) {
        Invoke-VerifiedDownload $script:FlashRuntimeUrl $archive $script:FlashArchiveHash "Flash runtime archive"
    }

    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    $extractDirectory = Join-Path $temporaryRoot ("grimoire-flash-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $extractDirectory | Out-Null
    try {
        $relativeOcx = "flash32/Flash32_34_0_0_376.ocx"
        $sevenZip = Get-Command 7z.exe -ErrorAction SilentlyContinue
        if ($sevenZip) {
            & $sevenZip.Source x $archive "-o$extractDirectory" $relativeOcx -y | Out-Null
        }
        else {
            $tar = Get-Command tar.exe -ErrorAction SilentlyContinue
            if (-not $tar) {
                throw "Neither 7z.exe nor tar.exe is available to extract the Flash runtime archive."
            }
            & $tar.Source -xf $archive -C $extractDirectory $relativeOcx
        }
        if ($LASTEXITCODE -ne 0) {
            throw "Extracting the Flash runtime failed with exit code $LASTEXITCODE."
        }

        $extractedOcx = Join-Path $extractDirectory "flash32\Flash32_34_0_0_376.ocx"
        Assert-FileHash $extractedOcx $script:FlashOcxHash "Extracted Flash runtime"
        New-Item -ItemType Directory -Path (Split-Path -Parent $script:FlashRuntimePath) -Force | Out-Null
        Copy-Item -LiteralPath $extractedOcx -Destination $script:FlashRuntimePath -Force
        Assert-FileHash $script:FlashRuntimePath $script:FlashOcxHash "Bundled Flash runtime"
    }
    finally {
        $resolvedExtractDirectory = [IO.Path]::GetFullPath($extractDirectory)
        if ($resolvedExtractDirectory.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -and
            (Test-Path -LiteralPath $resolvedExtractDirectory)) {
            Remove-Item -LiteralPath $resolvedExtractDirectory -Recurse -Force
        }
    }

    Write-Host "Flash runtime ready: $($script:FlashRuntimePath)" -ForegroundColor Green
    return $script:FlashRuntimePath
}

function Get-MSBuildPath {
    $programFilesX86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
    $msbuild = $null

    if (Test-Path -LiteralPath $vswhere) {
        $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1
    }

    if (-not $msbuild) {
        $fallbacks = @(
            (Join-Path $programFilesX86 "Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"),
            (Join-Path $programFilesX86 "Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"),
            (Join-Path $programFilesX86 "Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"),
            (Join-Path $programFilesX86 "Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe")
        )
        $msbuild = $fallbacks | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }

    if (-not $msbuild) {
        throw "MSBuild 17 was not found. Install Visual Studio 2022 Build Tools with the .NET desktop build tools workload."
    }
    return $msbuild
}

function Invoke-NuGetRestore([string]$MSBuild) {
    if ($NoRestore) {
        Write-Host "Skipping dependency restore." -ForegroundColor DarkGray
        return
    }
    Write-Host "Restoring dependencies..." -ForegroundColor Cyan
    & $MSBuild $script:SolutionPath /t:Restore /p:RestorePackagesConfig=true /m /v:minimal |
        ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet restore failed with exit code $LASTEXITCODE."
    }
}

function Complete-BrandedOutput([string]$BuildConfiguration, [string]$RequestedOutputDirectory) {
    $outputDirectory = if ([string]::IsNullOrWhiteSpace($RequestedOutputDirectory)) {
        Join-Path $script:RootDirectory "app\bin\$BuildConfiguration"
    }
    else {
        $RequestedOutputDirectory
    }
    $legacyExecutable = Join-Path $outputDirectory "Grimoire.exe"
    $brandedExecutable = Join-Path $outputDirectory "Grimoire Exodus.exe"
    if (-not (Test-Path -LiteralPath $legacyExecutable)) {
        throw "The build completed without producing $legacyExecutable."
    }
    $updaterExecutable = Join-Path $outputDirectory "Grimoire.Updater.exe"
    if (-not (Test-Path -LiteralPath $updaterExecutable)) {
        throw "The build completed without producing $updaterExecutable."
    }

    $pairs = @(
        @($legacyExecutable, $brandedExecutable),
        @("$legacyExecutable.config", "$brandedExecutable.config"),
        @((Join-Path $outputDirectory "Grimoire.pdb"), (Join-Path $outputDirectory "Grimoire Exodus.pdb"))
    )
    foreach ($pair in $pairs) {
        $source = $pair[0]
        $destination = $pair[1]
        if (Test-Path -LiteralPath $source) {
            if (Test-Path -LiteralPath $destination) {
                Remove-Item -LiteralPath $destination -Force
            }
            Move-Item -LiteralPath $source -Destination $destination
        }
    }

    Write-Host "Built: $brandedExecutable" -ForegroundColor Green
    return $brandedExecutable
}

function Invoke-ExodusBuild([string]$BuildConfiguration, [string]$Verbosity = "minimal", [string]$RequestedOutputDirectory) {
    Get-BundledFlashRuntime | Out-Null
    Assert-FileHash $script:DesktopSwfPath $script:DesktopSwfHash "Desktop Flash Player bootstrap"
    $msbuild = Get-MSBuildPath
    Invoke-NuGetRestore $msbuild
    $configurations = if ($BuildConfiguration -eq "All") { @("Debug", "Release") } else { @($BuildConfiguration) }
    $outputs = @()
    $resolvedOutputDirectory = if ([string]::IsNullOrWhiteSpace($RequestedOutputDirectory)) {
        $null
    }
    elseif ([IO.Path]::IsPathRooted($RequestedOutputDirectory)) {
        [IO.Path]::GetFullPath($RequestedOutputDirectory)
    }
    else {
        [IO.Path]::GetFullPath((Join-Path $script:RootDirectory $RequestedOutputDirectory))
    }

    foreach ($configurationName in $configurations) {
        Write-Host "Building $configurationName..." -ForegroundColor Cyan
        $msbuildArguments = @($script:SolutionPath, "/p:Configuration=$configurationName", "/p:Platform=Any CPU", "/m", "/v:$Verbosity")
        if (-not [string]::IsNullOrWhiteSpace($resolvedOutputDirectory)) {
            $msbuildArguments += "/p:OutputPath=$resolvedOutputDirectory"
        }
        & $msbuild @msbuildArguments |
            ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) {
            throw "$configurationName build failed with exit code $LASTEXITCODE."
        }
        $outputs += Complete-BrandedOutput $configurationName $resolvedOutputDirectory
    }
    return $outputs
}

function Get-ExodusProductVersion {
    $assemblyInfo = Get-Content -LiteralPath $script:AssemblyInfoPath -Raw
    $match = [regex]::Match($assemblyInfo, 'AssemblyFileVersion\("(?<version>\d+\.\d+\.\d+)\.\d+"\)')
    if (-not $match.Success) {
        throw "Could not read a four-part AssemblyFileVersion from $($script:AssemblyInfoPath)."
    }
    return $match.Groups["version"].Value
}

function Get-WixToolset {
    $toolDirectory = Join-Path $script:RootDirectory ".tools\wix314"
    $heat = Join-Path $toolDirectory "heat.exe"
    $candle = Join-Path $toolDirectory "candle.exe"
    $light = Join-Path $toolDirectory "light.exe"
    if ((Test-Path -LiteralPath $heat) -and (Test-Path -LiteralPath $candle) -and (Test-Path -LiteralPath $light)) {
        return $toolDirectory
    }

    $cacheDirectory = Join-Path $script:RootDirectory ".tools\cache"
    $archive = Join-Path $cacheDirectory "wix314-binaries.zip"
    if (Test-Path -LiteralPath $archive) {
        try {
            Assert-FileHash $archive $script:WixArchiveHash "WiX Toolset archive"
        }
        catch {
            Remove-Item -LiteralPath $archive -Force
        }
    }
    if (-not (Test-Path -LiteralPath $archive)) {
        Invoke-VerifiedDownload $script:WixUrl $archive $script:WixArchiveHash "WiX Toolset 3.14.1"
    }

    if (Test-Path -LiteralPath $toolDirectory) {
        Remove-Item -LiteralPath $toolDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $toolDirectory -Force | Out-Null
    Expand-Archive -LiteralPath $archive -DestinationPath $toolDirectory -Force

    foreach ($tool in @($heat, $candle, $light)) {
        if (-not (Test-Path -LiteralPath $tool)) {
            throw "The WiX Toolset archive did not contain $tool."
        }
    }
    return $toolDirectory
}

function New-InstallerStage([string]$SourceDirectory, [string]$StageDirectory) {
    if (Test-Path -LiteralPath $StageDirectory) {
        Remove-Item -LiteralPath $StageDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $StageDirectory -Force | Out-Null

    $runtimeExtensions = @(".exe", ".dll", ".config", ".cfg", ".swf", ".ocx")
    Get-ChildItem -LiteralPath $SourceDirectory -File |
        Where-Object { $runtimeExtensions -contains $_.Extension.ToLowerInvariant() } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $StageDirectory }

    $resources = Join-Path $SourceDirectory "Resources"
    if (Test-Path -LiteralPath $resources) {
        Copy-Item -LiteralPath $resources -Destination $StageDirectory -Recurse
    }
    $communityBots = Join-Path $SourceDirectory "Bot"
    if (-not (Test-Path -LiteralPath $communityBots)) {
        throw "Release output is missing the bundled Community Bot directory."
    }
    Copy-Item -LiteralPath $communityBots -Destination $StageDirectory -Recurse
    Copy-Item -LiteralPath (Join-Path $script:RootDirectory "LICENSE") -Destination $StageDirectory
    Copy-Item -LiteralPath (Join-Path $script:RootDirectory "README.md") -Destination $StageDirectory

    foreach ($requiredFile in @("Grimoire Exodus.exe", "Grimoire.Updater.exe", "Flash.ocx", "grimoire-exodus.swf")) {
        if (-not (Test-Path -LiteralPath (Join-Path $StageDirectory $requiredFile))) {
            throw "Installer staging is missing required file: $requiredFile"
        }
    }
}

function New-ExodusInstaller {
    $isolatedBuildDirectory = Join-Path $script:RootDirectory "installer\obj\build"
    if (Test-Path -LiteralPath $isolatedBuildDirectory) {
        Remove-Item -LiteralPath $isolatedBuildDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $isolatedBuildDirectory -Force | Out-Null
    $executable = Invoke-ExodusBuild "Release" "minimal" $isolatedBuildDirectory | Select-Object -Last 1
    $sourceDirectory = Split-Path -Parent $executable
    $productVersion = Get-ExodusProductVersion
    $wixDirectory = Get-WixToolset

    $objectDirectory = Join-Path $script:RootDirectory "installer\obj"
    $stageDirectory = Join-Path $objectDirectory "stage"
    New-InstallerStage $sourceDirectory $stageDirectory

    $harvestSource = Join-Path $objectDirectory "ApplicationFiles.wxs"
    $productSource = Join-Path $script:RootDirectory "installer\Product.wxs"
    $productObject = Join-Path $objectDirectory "Product.wixobj"
    $harvestObject = Join-Path $objectDirectory "ApplicationFiles.wixobj"
    New-Item -ItemType Directory -Path $objectDirectory -Force | Out-Null

    Write-Host "Harvesting installer payload..." -ForegroundColor Cyan
    & (Join-Path $wixDirectory "heat.exe") dir $stageDirectory -nologo -ag -cg ApplicationFiles -dr INSTALLFOLDER -srd -sfrag -sreg -scom -var var.SourceDir -out $harvestSource
    if ($LASTEXITCODE -ne 0) {
        throw "WiX heat failed with exit code $LASTEXITCODE."
    }

    Write-Host "Compiling installer..." -ForegroundColor Cyan
    $definitions = @("-dSourceDir=$stageDirectory", "-dProductVersion=$productVersion", "-dProjectDir=$($script:RootDirectory)")
    & (Join-Path $wixDirectory "candle.exe") -nologo -ext WixNetFxExtension -ext WixUtilExtension @definitions -out $productObject $productSource
    if ($LASTEXITCODE -ne 0) {
        throw "WiX candle failed while compiling Product.wxs (exit code $LASTEXITCODE)."
    }
    & (Join-Path $wixDirectory "candle.exe") -nologo @definitions -out $harvestObject $harvestSource
    if ($LASTEXITCODE -ne 0) {
        throw "WiX candle failed while compiling the application payload (exit code $LASTEXITCODE)."
    }

    $distDirectory = Join-Path $script:RootDirectory "dist"
    New-Item -ItemType Directory -Path $distDirectory -Force | Out-Null
    $installerPath = Join-Path $distDirectory "Grimoire-Exodus-Setup-$productVersion.msi"
    $installerPdb = Join-Path $objectDirectory "Grimoire-Exodus-Setup-$productVersion.wixpdb"
    $light = Join-Path $wixDirectory "light.exe"
    $lightArguments = @(
        "-nologo",
        "-ext", "WixNetFxExtension",
        "-ext", "WixUIExtension",
        "-ext", "WixUtilExtension",
        "-cultures:en-us",
        "-pdbout", $installerPdb,
        "-out", $installerPath,
        $productObject,
        $harvestObject
    )
    & $light @lightArguments
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "WiX ICE validation was unavailable (exit code $LASTEXITCODE). Retrying the same link with ICE validation suppressed."
        if (Test-Path -LiteralPath $installerPath) {
            Remove-Item -LiteralPath $installerPath -Force
        }
        if (Test-Path -LiteralPath $installerPdb) {
            Remove-Item -LiteralPath $installerPdb -Force
        }
        & $light -sval @lightArguments
    }
    if ($LASTEXITCODE -ne 0) {
        throw "WiX light failed with exit code $LASTEXITCODE."
    }
    if (-not (Test-Path -LiteralPath $installerPath)) {
        throw "WiX completed without producing $installerPath."
    }

    Write-Host "Installer: $installerPath" -ForegroundColor Green
    return $installerPath
}

function Update-ExodusVersion([string]$VersionPart) {
    $assemblyInfo = Get-Content -LiteralPath $script:AssemblyInfoPath -Raw
    $match = [regex]::Match($assemblyInfo, 'AssemblyVersion\("(?<version>\d+\.\d+\.\d+\.\d+)"\)')
    if (-not $match.Success) {
        throw "Could not find a four-part AssemblyVersion in $($script:AssemblyInfoPath)."
    }

    $parts = @($match.Groups["version"].Value.Split('.') | ForEach-Object { [int]$_ })
    $index = @{ major = 0; minor = 1; patch = 2; revision = 3 }[$VersionPart]
    $parts[$index]++
    for ($position = $index + 1; $position -lt $parts.Count; $position++) {
        $parts[$position] = 0
    }
    $newVersion = $parts -join "."

    $assemblyInfo = [regex]::Replace($assemblyInfo, 'AssemblyVersion\("\d+\.\d+\.\d+\.\d+"\)', "AssemblyVersion(`"$newVersion`")")
    $assemblyInfo = [regex]::Replace($assemblyInfo, 'AssemblyFileVersion\("\d+\.\d+\.\d+\.\d+"\)', "AssemblyFileVersion(`"$newVersion`")")
    Set-Content -LiteralPath $script:AssemblyInfoPath -Value $assemblyInfo -Encoding UTF8 -NoNewline

    $updaterAssemblyInfo = Get-Content -LiteralPath $script:UpdaterAssemblyInfoPath -Raw
    $updaterAssemblyInfo = [regex]::Replace($updaterAssemblyInfo, 'AssemblyVersion\("\d+\.\d+\.\d+\.\d+"\)', "AssemblyVersion(`"$newVersion`")")
    $updaterAssemblyInfo = [regex]::Replace($updaterAssemblyInfo, 'AssemblyFileVersion\("\d+\.\d+\.\d+\.\d+"\)', "AssemblyFileVersion(`"$newVersion`")")
    Set-Content -LiteralPath $script:UpdaterAssemblyInfoPath -Value $updaterAssemblyInfo -Encoding UTF8 -NoNewline

    $project = Get-Content -LiteralPath $script:ProjectPath -Raw
    if ($project -notmatch '<ApplicationVersion>\d+\.\d+\.\d+\.\d+</ApplicationVersion>') {
        throw "Could not find a four-part ApplicationVersion in $($script:ProjectPath)."
    }
    $project = [regex]::Replace(
        $project,
        '<ApplicationVersion>\d+\.\d+\.\d+\.\d+</ApplicationVersion>',
        "<ApplicationVersion>$newVersion</ApplicationVersion>")
    Set-Content -LiteralPath $script:ProjectPath -Value $project -Encoding UTF8 -NoNewline

    $manifest = Get-Content -LiteralPath $script:ManifestPath -Raw
    $applicationIdentityPattern = '(?s)(?<prefix><assemblyIdentity\b(?=[^>]*\bname="Grimoire\.Exodus")[^>]*\bversion=")\d+\.\d+\.\d+\.\d+(?<suffix>")'
    if ([regex]::Matches($manifest, $applicationIdentityPattern).Count -ne 1) {
        throw "Could not find exactly one Grimoire.Exodus assembly version in $($script:ManifestPath)."
    }
    $manifestReplacement = '${prefix}' + $newVersion + '${suffix}'
    $manifest = [regex]::Replace($manifest, $applicationIdentityPattern, $manifestReplacement)
    Set-Content -LiteralPath $script:ManifestPath -Value $manifest -Encoding UTF8 -NoNewline

    $program = Get-Content -LiteralPath $script:ProgramPath -Raw
    $today = Get-Date -Format "yyyy-MM-dd"
    $program = [regex]::Replace($program, 'versionDate\s*=\s*"[^"]+"', "versionDate = `"$today`"")
    Set-Content -LiteralPath $script:ProgramPath -Value $program -Encoding UTF8 -NoNewline
    Write-Host "Version updated to $newVersion ($today)." -ForegroundColor Green
}

function New-PluginWorkspace([string]$PluginName, [string]$RequestedOutputDirectory) {
    $safeName = [regex]::Replace($PluginName, '[^A-Za-z0-9_.-]', '')
    if ([string]::IsNullOrWhiteSpace($safeName)) {
        throw "The plugin name must contain at least one letter or number."
    }
    $namespaceName = [regex]::Replace($safeName, '[^A-Za-z0-9_]', '')
    if ($namespaceName -match '^\d') {
        $namespaceName = "Plugin$namespaceName"
    }

    $destination = if ([string]::IsNullOrWhiteSpace($RequestedOutputDirectory)) {
        Join-Path $script:RootDirectory "plugin-workspaces\$safeName"
    } elseif ([IO.Path]::IsPathRooted($RequestedOutputDirectory)) {
        $RequestedOutputDirectory
    } else {
        Join-Path $script:RootDirectory $RequestedOutputDirectory
    }
    $destination = [IO.Path]::GetFullPath($destination)
    if (Test-Path -LiteralPath $destination) {
        throw "The plugin workspace already exists: $destination"
    }

    $debugDirectory = Join-Path $script:RootDirectory "app\bin\Debug"
    $grimoireReference = [Security.SecurityElement]::Escape((Join-Path $debugDirectory "Grimoire Exodus.exe"))
    $darkUiReference = [Security.SecurityElement]::Escape((Join-Path $debugDirectory "DarkUI.dll"))

    New-Item -ItemType Directory -Path $destination | Out-Null
    $projectContents = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Library</OutputType>
    <AssemblyName>$safeName</AssemblyName>
    <RootNamespace>$namespaceName</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Grimoire">
      <HintPath>$grimoireReference</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="DarkUI">
      <HintPath>$darkUiReference</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
"@
    Set-Content -LiteralPath (Join-Path $destination "$safeName.csproj") -Value $projectContents -Encoding UTF8

    $sourceContents = @"
using Grimoire.Tools.Plugins;

namespace $namespaceName
{
    [GrimoirePluginEntry]
    public sealed class Plugin : IGrimoirePlugin
    {
        public string Author => "dsenin";
        public string Description => "$safeName plugin for Grimoire Exodus.";

        public void Load()
        {
            // Register plugin behavior here.
        }

        public void Unload()
        {
            // Release resources and unregister handlers here.
        }
    }
}
"@
    Set-Content -LiteralPath (Join-Path $destination "Plugin.cs") -Value $sourceContents -Encoding UTF8

    $readmeContents = @"
# $safeName

Plugin workspace generated by the Grimoire Exodus developer CLI.

1. Run ``.\exodus.ps1 build -Configuration Debug`` from the repository root.
2. Build this project.
3. Copy ``bin\Debug\net48\$safeName.dll`` into the Grimoire Exodus Plugins folder.
"@
    Set-Content -LiteralPath (Join-Path $destination "README.md") -Value $readmeContents -Encoding UTF8
    Write-Host "Plugin workspace created: $destination" -ForegroundColor Green
}

switch ($Command) {
    "build" {
        Show-ExodusBanner
        Invoke-ExodusBuild $Configuration "minimal" $OutputDirectory | Out-Null
    }
    "dev" {
        Show-ExodusBanner
        $executable = Invoke-ExodusBuild "Debug" "detailed" $OutputDirectory | Select-Object -Last 1
        Write-Host "Launching development client..." -ForegroundColor Cyan
        Start-Process -FilePath $executable -WorkingDirectory (Split-Path -Parent $executable)
    }
    "runtime" {
        Show-ExodusBanner
        Get-BundledFlashRuntime | Out-Null
    }
    "installer" {
        Show-ExodusBanner
        New-ExodusInstaller | Out-Null
    }
    "version" {
        Show-ExodusBanner
        Update-ExodusVersion $Part
    }
    "plugin" {
        Show-ExodusBanner
        New-PluginWorkspace $Name $OutputDirectory
    }
    "man" { Show-ExodusManual }
    default { Show-ExodusHelp }
}
