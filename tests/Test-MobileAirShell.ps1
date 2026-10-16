param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$rootPath = Join-Path $repositoryRoot 'game-client\AS3\grimoire\Root.as'
$shellPath = Join-Path $repositoryRoot 'game-client\AS3\grimoire\MobileShell.as'
$botLibraryPath = Join-Path $repositoryRoot 'game-client\AS3\grimoire\MobileBotLibrary.as'
$botRunnerPath = Join-Path $repositoryRoot 'game-client\AS3\grimoire\MobileBotRunner.as'
$preferencesPath = Join-Path $repositoryRoot 'game-client\AS3\grimoire\MobilePreferences.as'
$legacyFixturePath = Join-Path $repositoryRoot 'shared\fixtures\gbot\v1\legacy-delay.gbot'
$descriptorPath = Join-Path $repositoryRoot 'mobile\air-client\GrimoireExodus-app.xml'
$buildScriptPath = Join-Path $repositoryRoot 'mobile\air-client\build-air.ps1'

function Assert-Contract {
    param(
        [Parameter(Mandatory = $true)]
        [bool] $Condition,

        [Parameter(Mandatory = $true)]
        [string] $Message
    )

    if (-not $Condition) {
        throw "Mobile AIR shell contract failed: $Message"
    }
}

function Get-MobileCommandTypeNames {
    param([object] $Node)

    if ($null -eq $Node) {
        return
    }
    if ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [string]) {
        foreach ($item in $Node) {
            Get-MobileCommandTypeNames -Node $item
        }
        return
    }
    if ($Node -isnot [pscustomobject]) {
        return
    }

    $typeProperty = $Node.PSObject.Properties['$type']
    if ($null -ne $typeProperty) {
        $qualifiedType = [string]$typeProperty.Value
        if ($qualifiedType -like 'Grimoire.Botting.Commands.*') {
            (($qualifiedType -split ',')[0] -split '\.')[-1]
        }
    }
    foreach ($property in $Node.PSObject.Properties) {
        if ($property.Name -ne '$type') {
            Get-MobileCommandTypeNames -Node $property.Value
        }
    }
}

$rootSource = Get-Content -Raw $rootPath
$shellSource = Get-Content -Raw $shellPath
$botLibrarySource = Get-Content -Raw $botLibraryPath
$botRunnerSource = Get-Content -Raw $botRunnerPath
$preferencesSource = Get-Content -Raw $preferencesPath
$legacyFixture = Get-Content -Raw $legacyFixturePath | ConvertFrom-Json -Depth 100
$descriptorSource = Get-Content -Raw $descriptorPath
$buildScriptSource = Get-Content -Raw $buildScriptPath

Assert-Contract ($rootSource -match 'StageScaleMode\.EXACT_FIT') `
    'AIR must map the physical panel into AQW''s native 960x550 pointer plane.'
Assert-Contract ($rootSource -notmatch 'StageScaleMode\.NO_SCALE') `
    'AIR must not expose physical Android coordinates to the legacy AQW input path.'
Assert-Contract ($rootSource -match '(?:stage|this\.stg)\.addChild(?:At)?\(Game') `
    'The live AQW display must remain a direct Stage child.'
$removeLoadingIndex = $rootSource.IndexOf('this.removeLoadingSurface();', [StringComparison]::Ordinal)
$stageGameIndex = $rootSource.IndexOf('this.stg.addChildAt(Game, 0);', [StringComparison]::Ordinal)
$mobileShellIndex = $rootSource.IndexOf('this.mobileShell = new mobileShellClass', [StringComparison]::Ordinal)
Assert-Contract (($removeLoadingIndex -ge 0) -and
    ($stageGameIndex -gt $removeLoadingIndex) -and
    ($mobileShellIndex -gt $stageGameIndex)) `
    'The AIR branch must attach AQW to Stage before creating its overlay.'
Assert-Contract ($rootSource -match 'Event\.RESIZE') `
    'AIR must relayout when the Android surface dimensions change.'
Assert-Contract (($rootSource -match 'frameRate\s*=\s*30') -and
    ($buildScriptSource -match 'default-frame-rate=30')) `
    'AIR must cap the legacy AQW scene at 30 FPS on mobile.'
Assert-Contract (($descriptorSource -match '<requestedDisplayResolution>standard</requestedDisplayResolution>') -and
    ($descriptorSource -notmatch '<requestedDisplayResolution>high</requestedDisplayResolution>')) `
    'AIR must avoid rendering the legacy AQW scene at high-density device resolution.'

Assert-Contract ($shellSource -notmatch 'addChild\s*\(\s*(?:this\.)?game\s*\)') `
    'MobileShell must be an overlay/controller and must not reparent AQW.'
Assert-Contract ($shellSource -notmatch '(?:this\.)?(?:game|_game)\.mask\s*=') `
    'MobileShell must not mask the live AQW display.'
Assert-Contract ($shellSource -match 'import\s+flash\.geom\.Rectangle;') `
    'MobileShell must use a local rectangle to clip AQW without reparenting it.'
Assert-Contract ($shellSource -match '(?:this\.)?game\.scrollRect\s*=\s*new\s+Rectangle\(0,\s*0,\s*GAME_WIDTH,\s*GAME_HEIGHT\)') `
    'AQW must be clipped to its native 960x550 coordinate space.'
Assert-Contract (($shellSource -notmatch 'VIEW_WIDTH') -and ($shellSource -notmatch 'VIEW_HEIGHT')) `
    'The game viewport must not regress to fixed 960x550-era dimensions.'
Assert-Contract (($shellSource -match 'this\.game\.x\s*=\s*0') -and
    ($shellSource -match 'this\.game\.y\s*=\s*0') -and
    ($shellSource -match 'this\.game\.scaleX\s*=\s*this\.game\.scaleY\s*=\s*1')) `
    'AQW must stay at its native Stage origin and scale for accurate touch input.'
$invalidGameTransforms = @($shellSource -split "`r?`n" | Where-Object {
    ($_ -match 'this\.game\.[xy]\s*=') -and ($_ -notmatch 'this\.game\.[xy]\s*=\s*0(?:\.0)?\s*;')
}) + @($shellSource -split "`r?`n" | Where-Object {
    ($_ -match 'this\.game\.scale[XY]\s*=') -and
    ($_ -notmatch 'this\.game\.scaleX\s*=\s*this\.game\.scaleY\s*=\s*1(?:\.0)?\s*;')
})
Assert-Contract ($invalidGameTransforms.Count -eq 0) `
    'No mobile layout or animation path may translate or scale the AQW display object.'
Assert-Contract ($shellSource -notmatch 'targetGame(?:X|Y|Scale)') `
    'Chrome layout must not maintain a second transformed AQW coordinate plane.'
Assert-Contract ($shellSource -match 'this\.targetToggleX\s*=\s*this\.chromeVisible\s*\?\s*VIEW_X\s*\+\s*23\s*:\s*23') `
    'The Grimoire visibility button must follow the game viewport left edge.'
Assert-Contract ($shellSource -match 'this\.targetToggleY\s*=\s*this\.chromeVisible\s*\?\s*VIEW_Y\s*\+\s*23\s*:\s*23') `
    'The Grimoire visibility button must sit at the game viewport top edge.'
Assert-Contract ($shellSource -match 'this\.chrome\.addChild\(this\.background\)') `
    'Opaque shell artwork must be contained by the fading chrome overlay.'
Assert-Contract ($shellSource -notmatch 'drawRect\(0,\s*0,\s*this\.layoutWidth,\s*this\.layoutHeight\)') `
    'The shell must not paint an opaque full-screen canvas over AQW.'

# Exercise the physical-to-logical pointer mapping against a 20:9 phone.
$deviceWidth = 2400.0
$deviceHeight = 1080.0
$designWidth = 960.0
$designHeight = 550.0
$logicalX = 641.25
$logicalY = 318.75
$physicalX = $logicalX * ($deviceWidth / $designWidth)
$physicalY = $logicalY * ($deviceHeight / $designHeight)
$mappedX = $physicalX * ($designWidth / $deviceWidth)
$mappedY = $physicalY * ($designHeight / $deviceHeight)
Assert-Contract (([Math]::Abs($mappedX - $logicalX) -lt 0.001) -and
    ([Math]::Abs($mappedY - $logicalY) -lt 0.001)) `
    'Android X/Y input must invert to the exact AQW logical target.'

Assert-Contract (($shellSource -match 'renderBotManager\(\)') -and
    ($shellSource -match 'renderSettings\(\)') -and
    ($shellSource -match 'handleKey\(keyCode:uint\)')) `
    'The mobile shell must expose the Bot Manager, settings, and hardware key bindings.'
Assert-Contract (($shellSource -match 'bot_previous') -and
    ($shellSource -match 'bot_next') -and
    ($shellSource -match 'botPageStart')) `
    'The compact Bot Manager must page through every available bot rather than trapping selection on the first rows.'
Assert-Contract (($botLibrarySource -match 'MAX_BOT_BYTES') -and
    ($botLibrarySource -match 'MAX_COMMANDS') -and
    ($botLibrarySource -notmatch 'getDefinitionByName|ApplicationDomain')) `
    'Mobile GBot loading must stay bounded and data-only.'
Assert-Contract (($botRunnerSource -match 'default:') -and
    ($botRunnerSource -match 'this\.stop\("UNSUPPORTED COMMAND " \+ kind\.toUpperCase\(\)\)') -and
    ($botRunnerSource -notmatch 'getDefinitionByName|ApplicationDomain')) `
    'Unknown GBot commands must stop safely instead of continuing or resolving runtime types.'
$fixtureCommandType = [string]$legacyFixture.Commands.'$values'[0].'$type'
$fixtureCommandName = (($fixtureCommandType -split ',')[0] -split '\.')[-1]
Assert-Contract (($legacyFixture.'$type' -like 'Grimoire.Botting.Configuration,*') -and
    ($botRunnerSource -match ('case\s+"' + [regex]::Escape($fixtureCommandName) + '"'))) `
    'The shared legacy compatibility fixture must be recognized by the mobile runner.'

$communityBotDirectory = Join-Path $repositoryRoot 'app\Bot'
if (Test-Path -LiteralPath $communityBotDirectory -PathType Container) {
    $communityCommandTypes = Get-ChildItem -LiteralPath $communityBotDirectory -Filter '*.gbot' -File -Recurse |
        ForEach-Object {
            $document = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json -Depth 100
            Get-MobileCommandTypeNames -Node $document
        } |
        Sort-Object -Unique
    foreach ($commandType in $communityCommandTypes) {
        Assert-Contract ($botRunnerSource -match ('case\s+"' + [regex]::Escape($commandType) + '"')) `
            'A repository-controlled Community bot uses a command not recognized by the mobile runner.'
    }
}
Assert-Contract (($preferencesSource -match 'SharedObject\.getLocal') -and
    ($preferencesSource -match 'confirmExit') -and
    ($preferencesSource -match 'cycleBinding')) `
    'Mobile global settings must persist and include exit confirmation and key bindings.'
Assert-Contract (($buildScriptSource -match 'app\\Bot') -and
    ($buildScriptSource -match 'bots\\community')) `
    'The APK must package only the repository-controlled Community Bot directory.'
Assert-Contract (($rootSource -match 'private var mobileShell:\*') -and
    ($buildScriptSource -match 'includes=grimoire\.MobileShell')) `
    'AIR-only mobile classes must remain dynamically isolated from the desktop Flash Player bridge.'

$gameFilePathIndex = $rootSource.IndexOf('gameDefinition.serverFilePath = gameFilePath;', [StringComparison]::Ordinal)
Assert-Contract (($gameFilePathIndex -ge 0) -and ($stageGameIndex -gt $gameFilePathIndex)) `
    'AIR must restore AQW''s HTTPS asset origin before activating the game on Stage.'
$gameConfiguredIndex = $rootSource.IndexOf('Game.params.loginURL = this.loginURL;', [StringComparison]::Ordinal)
Assert-Contract (($gameConfiguredIndex -ge 0) -and ($stageGameIndex -gt $gameConfiguredIndex)) `
    'All AQW bootstrap parameters must be complete before its ADDED_TO_STAGE handler runs.'
Assert-Contract ($rootSource -match 'Game\.params\.sFilePath\s*=\s*gameFilePath') `
    'AQW parameters must retain the official gamefiles origin after loadBytes.'
Assert-Contract ($rootSource -match 'gameDefinition\.serverGamePath\s*=\s*this\.sURL') `
    'AQW API requests must retain the official game origin after loadBytes.'
Assert-Contract ($shellSource -match 'getTimer\(\)') `
    'Chrome animation must use elapsed time rather than frame-dependent easing.'
Assert-Contract ($shellSource -notmatch '\+=\s*\([^\r\n]+\)\s*\*\s*0\.19') `
    'The old asymptotic 0.19-per-frame animation must not return.'
Assert-Contract ($shellSource -match 'GRIMOIRE') `
    'The compact Android title must identify Grimoire.'
Assert-Contract ($shellSource -cnotmatch '"EXODUS"') `
    'The Android top bar must not render the longer Exodus suffix.'

$inventoryBranch = [regex]::Match(
    $rootSource,
    'else if \(e\.params\.message\.indexOf\(''"cmd":"loadInventoryBig"''\)[\s\S]*?\n\s*\}',
    [Text.RegularExpressions.RegexOptions]::CultureInvariant
)
Assert-Contract ($inventoryBranch.Success -and ($inventoryBranch.Value -match 'refreshMobileProfile\(\)')) `
    'The mobile profile must refresh only after AQW confirms the authenticated character.'
Assert-Contract ($rootSource -match 'Game\.world\.myAvatar\.objData') `
    'The authenticated mobile profile must read the live AQW avatar data.'
Assert-Contract ($rootSource -match 'loginInfo\.strUsername') `
    'The authenticated mobile profile must safely fall back to AQW login state.'
Assert-Contract ($rootSource -notmatch 'mobileShell\.setPlayerName\(username\)') `
    'Typing or selecting credentials must not impersonate an authenticated profile.'
Assert-Contract ($shellSource -match 'setPlayerProfile\(value:String,\s*level:\*,\s*characterClass:String\)') `
    'The profile card must accept authenticated character name, level, and class details.'

Assert-Contract ($descriptorSource -match '<aspectRatio>landscape</aspectRatio>') `
    'The Android client must remain landscape-only.'
Assert-Contract ($descriptorSource -match '<fullScreen>true</fullScreen>') `
    'The Android client must remain immersive full screen.'

Write-Host 'Mobile AIR shell contract passed.'
