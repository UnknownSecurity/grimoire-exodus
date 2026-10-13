[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("build", "dev", "version", "plugin", "help", "man")]
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
$script:ProgramPath = Join-Path $script:RootDirectory "app\Grimoire\Program.cs"

function Show-ExodusBanner {
    $banner = @'
   ____      _                 _            ______              __
  / ___|_ __(_)_ __ ___   ___(_)_ __ ___  |  ____|_  _____   / /_  ___
 | |  _| '__| | '_ ` _ \ / _ \ | '__/ _ \ |  _| \ \/ / _ \ / /| |/ / |
 | |_| | |  | | | | | | | (_) | | | |  __/ | |___ >  < (_) / / |   <| |
  \____|_|  |_|_| |_| |_|\___/|_|_|  \___| |_____/_/\_\___/_/  |_|\_\_|
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

function Complete-BrandedOutput([string]$BuildConfiguration) {
    $outputDirectory = Join-Path $script:RootDirectory "app\bin\$BuildConfiguration"
    $legacyExecutable = Join-Path $outputDirectory "Grimoire.exe"
    $brandedExecutable = Join-Path $outputDirectory "Grimoire Exodus.exe"
    if (-not (Test-Path -LiteralPath $legacyExecutable)) {
        throw "The build completed without producing $legacyExecutable."
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

function Invoke-ExodusBuild([string]$BuildConfiguration, [string]$Verbosity = "minimal") {
    $msbuild = Get-MSBuildPath
    Invoke-NuGetRestore $msbuild
    $configurations = if ($BuildConfiguration -eq "All") { @("Debug", "Release") } else { @($BuildConfiguration) }
    $outputs = @()

    foreach ($configurationName in $configurations) {
        Write-Host "Building $configurationName..." -ForegroundColor Cyan
        & $msbuild $script:SolutionPath /p:Configuration=$configurationName '/p:Platform=Any CPU' /m "/v:$Verbosity" |
            ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) {
            throw "$configurationName build failed with exit code $LASTEXITCODE."
        }
        $outputs += Complete-BrandedOutput $configurationName
    }
    return $outputs
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
    Set-Content -LiteralPath $script:AssemblyInfoPath -Value $assemblyInfo -Encoding UTF8

    $program = Get-Content -LiteralPath $script:ProgramPath -Raw
    $today = Get-Date -Format "yyyy-MM-dd"
    $program = [regex]::Replace($program, 'versionDate\s*=\s*"[^"]+"', "versionDate = `"$today`"")
    Set-Content -LiteralPath $script:ProgramPath -Value $program -Encoding UTF8
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
        Invoke-ExodusBuild $Configuration | Out-Null
    }
    "dev" {
        Show-ExodusBanner
        $executable = Invoke-ExodusBuild "Debug" "detailed" | Select-Object -Last 1
        Write-Host "Launching development client..." -ForegroundColor Cyan
        Start-Process -FilePath $executable -WorkingDirectory (Split-Path -Parent $executable)
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
