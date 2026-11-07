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
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
$source = Join-Path $PSScriptRoot "ModernUiProtocolSmoke.cs"
$client = Join-Path $outputDirectory "Grimoire Exodus.exe"
$clientProbe = Join-Path $outputDirectory "Grimoire.dll"
$json = Join-Path $outputDirectory "Newtonsoft.Json.dll"
$smokeExecutable = Join-Path $outputDirectory "ModernUiProtocolSmoke-$PID.exe"
$shellSource = Join-Path $rootDirectory "desktop-ui\src\App.vue"
$shellIndexSource = Join-Path $rootDirectory "desktop-ui\index.html"
$shellStoreSource = Join-Path $rootDirectory "desktop-ui\src\stores\app.ts"
$shellStylesSource = Join-Path $rootDirectory "desktop-ui\src\styles.css"
$shellPackageSource = Join-Path $rootDirectory "desktop-ui\package.json"
$rootShellSource = Join-Path $rootDirectory "app\Grimoire.UI\Root.ModernShell.cs"
$shellHostSource = Join-Path $rootDirectory "app\Grimoire.UI.Modern\ModernShellHost.cs"
$protocolSource = Join-Path $rootDirectory "app\Grimoire.UI.Modern\ModernUiProtocol.cs"
$rootMultiClientSource = Join-Path $rootDirectory "app\Grimoire.UI\Root.MultiClient.cs"
$ruffleBridgeSource = Join-Path $rootDirectory "app\Grimoire.Clients\RuffleFlashBridge.cs"
$flashBadgeSource = Join-Path $rootDirectory "app\Resources\Brand\flash-logo.png"
$ruffleBadgeSource = Join-Path $rootDirectory "app\Resources\Brand\ruffle-logo.png"
$flashBadgeOutput = Join-Path $outputDirectory "Resources\Brand\flash-logo.png"
$ruffleBadgeOutput = Join-Path $outputDirectory "Resources\Brand\ruffle-logo.png"

foreach ($requiredPath in @($compiler, $source, $client, $json, $shellSource, $shellIndexSource, $shellStoreSource, $shellStylesSource, $shellPackageSource, $rootShellSource, $shellHostSource, $protocolSource, $rootMultiClientSource, $ruffleBridgeSource, $flashBadgeSource, $ruffleBadgeSource, $flashBadgeOutput, $ruffleBadgeOutput)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required modern UI smoke-test input was not found: $requiredPath"
    }
}

$shellText = Get-Content -LiteralPath $shellSource -Raw
$shellIndexText = Get-Content -LiteralPath $shellIndexSource -Raw
$shellStoreText = Get-Content -LiteralPath $shellStoreSource -Raw
$shellStylesText = Get-Content -LiteralPath $shellStylesSource -Raw
$shellPackageText = Get-Content -LiteralPath $shellPackageSource -Raw
$rootShellText = Get-Content -LiteralPath $rootShellSource -Raw
$shellHostText = Get-Content -LiteralPath $shellHostSource -Raw
$protocolText = Get-Content -LiteralPath $protocolSource -Raw
$rootMultiClientText = Get-Content -LiteralPath $rootMultiClientSource -Raw
$ruffleBridgeText = Get-Content -LiteralPath $ruffleBridgeSource -Raw
$quickLoadLabels = [regex]::Matches($shellText, "Quick Load\s*<ChevronDown").Count
if ($quickLoadLabels -ne 1) {
    throw "The modern shell must expose exactly one Quick Load label; found $quickLoadLabels."
}

if ($shellText -match 'v-else-if="routeWorkspace"') {
    throw "Modern navigation must keep the AQW client mounted instead of replacing it with route launcher pages."
}
if ($shellText -notmatch 'class="window-controls"') {
    throw "The modern shell is missing its custom window controls."
}
if (($shellText -notmatch 'const\s+isFlashActive\s*=\s*computed\(\(\)\s*=>\s*activeSession\.value\?\.engine\s*===\s*"FlashActiveX"\)') -or
    ($shellText -notmatch 'v-else-if="isFlashActive"\s+class="engine-badge flash"') -or
    ($shellText -notmatch 'flashBadgeUrl\s*=\s*"/Brand/flash-logo\.png"') -or
    ($shellText -notmatch 'v-if="isRuffleActive"\s+class="engine-badge"') -or
    ($shellStylesText -notmatch '\.engine-badge\.flash')) {
    throw "The modern title bar must show packaged, engine-specific badges for FlashActiveX and Ruffle sessions."
}

$requiredWindowActions = @(
    "navigation.client", "window.beginDrag", "window.minimize", "window.toggleMaximize", "window.close"
)
foreach ($action in $requiredWindowActions) {
    if ($rootShellText -notmatch ('case\s+"' + [regex]::Escape($action) + '"')) {
        throw "The native modern-shell adapter is missing the '$action' action."
    }
    if ($shellText -notmatch ('["'']' + [regex]::Escape($action) + '["'']')) {
        throw "The Vue modern shell is missing the '$action' action."
    }
}
if (($rootShellText -notmatch 'FormBorderStyle\s*=\s*FormBorderStyle\.None') -or ($rootShellText -notmatch 'override\s+void\s+WndProc')) {
    throw "The modern shell must provide borderless chrome with native resize hit testing."
}
if (($rootShellText -notmatch 'GetModernMaximizedBounds') -or ($rootShellText -notmatch 'bounds\.Inflate\(edgeCompensation, edgeCompensation\)')) {
    throw "The modern shell must compensate for the thin borderless maximize edge gap."
}
if ($rootShellText -notmatch 'new\s+Padding\(\s*ModernResizeBorderWidth,\s*0,\s*ModernResizeBorderWidth,\s*ModernResizeBorderWidth\s*\)') {
    throw "The modern shell must keep the custom title bar flush with the top edge while retaining resize padding on the other edges."
}
if (($rootShellText -notmatch 'WmNcCalcSize') -or
    ($rootShellText -notmatch 'AdjustModernClientRect') -or
    ($rootShellText -match 'message\.Msg\s*==\s*WmNcCalcSize[\s\S]{0,500}message\.Result\s*=\s*IntPtr\.Zero;\s*return;')) {
    throw "The modern shell must remove only the top resize inset while preserving native frame rendering."
}
if (($rootShellText -notmatch 'ApplyModernWindowShadow') -or
    ($rootShellText -notmatch 'DwmSetWindowAttribute') -or
    ($rootShellText -notmatch 'DwmExtendFrameIntoClientArea')) {
    throw "The modern shell must explicitly retain the Windows frame shadow."
}
if (($rootShellText -notmatch 'HtTop') -or
    ($rootShellText -notmatch 'HtBottom') -or
    ($rootShellText -notmatch 'HtLeft') -or
    ($rootShellText -notmatch 'HtRight')) {
    throw "The modern shell must keep all four resize hit-test edges."
}
if ($shellStylesText -notmatch '\.app-shell\s*\{[\s\S]{0,900}box-shadow:\s*inset\s+0\s+0\s+0\s+1px') {
    throw "The modern shell must draw a visible one-pixel frame inside the borderless window."
}
if (($rootShellText -notmatch 'ModernMinimumWidthLogicalPixels\s*=\s*960') -or
    ($rootShellText -notmatch 'ModernMinimumHeightLogicalPixels\s*=\s*620') -or
    ($rootShellText -notmatch 'MinimumSize\s*=\s*GetModernMinimumSize\(\)')) {
    throw "The modern shell must enforce a DPI-aware minimum window size."
}
if (($rootShellText -notmatch 'modernStartupPresentationPending') -or
    ($rootShellText -notmatch 'Opacity\s*=\s*0d') -or
    ($rootShellText -notmatch 'CompleteModernStartupPresentation\(\)') -or
    ($shellHostText -notmatch 'await\s+WaitForShellReadyAsync\(\)') -or
    ($shellHostText -notmatch 'readyCompletion\.TrySetResult\(true\)')) {
    throw "Modern startup must keep the legacy surface hidden until the embedded shell reports that it is ready."
}
if (($rootShellText -notmatch 'modernShellInitializationAttempted') -or
    ($rootShellText -notmatch 'modernShellInitializationAttempted\s*=\s*true')) {
    throw "Modern startup must not race duplicate shell initialization between Load and Shown."
}
if (($rootShellText -notmatch 'FallbackToLegacyShell[\s\S]{0,1800}CompleteModernStartupPresentation\(\)') -or
    ($shellHostText -notmatch 'StartupTimeout')) {
    throw "Modern startup must restore a visible legacy shell when initialization fails or times out."
}
if ($rootShellText -notmatch 'ShouldApplyViewport') {
    throw "The native modern shell must ignore duplicate viewport reports so the AQW surface is not repeatedly rebound."
}
if ($shellText -notmatch 'v-if="store\.snapshot\.multiClientEnabled"\s+class="client-tabs"') {
    throw "The modern shell must hide client tabs when experimental multi-client mode is disabled."
}
if ($shellText -match 'handleBridgeEvent[\s\S]{0,500}nextTick\(reportViewport\)') {
    throw "State publications must not re-report the unchanged native game viewport."
}
if ($shellText -match 'Native engine preserved') {
    throw "The modern status bar must not show the obsolete native-engine label."
}
if (($shellText -notmatch 'class="stats-toggle"') -or ($shellText -notmatch "'stats-hidden': !showStatusCards")) {
    throw "The modern status bar must provide an arrow toggle for the operations cards."
}
if (($rootShellText -notmatch '\["hideDeviceStats"\]\s*=\s*GetDesktopPreferences\(\)\.HideDeviceStats') -or
    ($shellStoreText -notmatch 'hideDeviceStats:\s*boolean') -or
    ($shellText -notmatch 'const\s+statsVisible\s*=\s*ref\(false\)') -or
    ($shellText -notmatch 'v-if="showGame\s*&&\s*!store\.snapshot\.hideDeviceStats"') -or
    ($shellText -notmatch 'v-if="showStatusCards"\s+class="operations-dock"')) {
    throw "The modern status cards must start collapsed and honor the Hide Device Stats preference."
}
if (($shellText -notmatch 'from\s+"motion/mini"') -or
    ($shellPackageText -notmatch '"motion"\s*:') -or
    ($shellText -notmatch '<Transition\s+name="dock"') -or
    ($shellText -notmatch '<Transition\s+name="accordion"') -or
    ($shellStylesText -notmatch '@media\s*\(prefers-reduced-motion:\s*reduce\)')) {
    throw "The modern shell must provide Motion-powered, reduced-motion-safe component transitions."
}
if (($shellText -notmatch '<span>Cell</span>') -or ($shellText -notmatch '<span>Pad</span>') -or ($shellText -match '<span>Position</span>')) {
    throw "The modern location controls must use Cell and Pad labels."
}
if ($shellText -notmatch 'v-if="store\.snapshot\.location\.available"\s+class="location-controls"') {
    throw "The modern Cell and Pad controls must stay hidden until the active player is logged in."
}
if ($shellText -notmatch 'autoLocationSessionId[\s\S]{0,500}refreshLocation') {
    throw "The modern shell must populate Cell and Pad automatically for a logged-in client."
}
if (($shellText -notmatch 'actionNotice') -or
    ($shellText -notmatch 'showActionNotice\(`\$\{action\.label\} \$\{willEnable \? "enabled" : "disabled"\}`\)') -or
    ($shellText -notmatch 'showActionNotice\("Bot started"\)') -or
    ($shellText -notmatch 'showActionNotice\("Bot stopped"\)') -or
    ($shellText -notmatch 'class="chrome-toasts"') -or
    ($shellStylesText -notmatch '\.success-toast')) {
    throw "Modern option and bot lifecycle actions must show feedback in the native-viewport-safe chrome toast layer."
}
$topbarMarkup = [regex]::Match(
    $shellText,
    '<header class="topbar"[\s\S]*?</header>',
    [Text.RegularExpressions.RegexOptions]::Singleline).Value
if ($topbarMarkup -match 'class="chrome-toasts"') {
    throw "Modern toast feedback must be mounted in the workspace below the custom title bar."
}
if ($shellText -notmatch '<main class="workspace"[\s\S]{0,500}<div class="chrome-toasts"') {
    throw "Modern toast feedback is missing from the workspace above the AQW viewport."
}
if (($shellHostText -notmatch 'IRuffleFrameHost') -or
    ($shellHostText -notmatch 'CoreWebView2_FrameCreated') -or
    ($shellHostText -notmatch 'AddHostObjectToScript')) {
    throw "Modern Ruffle must reuse the shell WebView2 through a frame host instead of creating a second browser control."
}
if (($rootMultiClientText -notmatch 'new\s+RuffleFlashBridge\([\s\S]{0,180}modernShellHost') -or
    ($rootMultiClientText -notmatch 'UsesSharedHost') -or
    ($rootMultiClientText -notmatch 'GameEngineKind\.Ruffle\s*\?\s*240\s*:\s*60')) {
    throw "Modern client creation must attach Ruffle to the existing shell WebView2 and keep its native placeholder hidden."
}
if (($shellText -notmatch 'engine\.ruffle\.mount') -or
    ($shellText -notmatch '<iframe[\s\S]{0,350}:name="frame\.name"')) {
    throw "The modern shell must mount Ruffle sessions as isolated frames inside its existing WebView2 surface."
}
if (($shellIndexText -notmatch 'frame-src\s+http://127\.0\.0\.1:\*') -or
    ($shellIndexText -match 'frame-src\s+''none''')) {
    throw "The modern shell CSP must allow only loopback Ruffle frames instead of blocking the shared engine surface."
}
if (($ruffleBridgeText -notmatch 'PersistentProfile') -or
    ($ruffleBridgeText -match 'Profile\.Delete\(\)') -or
    (($ruffleBridgeText -match 'TryDeleteProfileDirectory\(\)') -and
        ($ruffleBridgeText -notmatch 'if\s*\(!deleteProfileOnDispose\)'))) {
    throw "Ruffle remember-login storage must use an app-owned persistent profile instead of deleting it at shutdown."
}
if (($shellText -notmatch 'https://github\.com/UnknownSecurity/grimoire-mobile/releases/latest') -or
    ($rootShellText -notmatch 'https://github\.com/UnknownSecurity/grimoire-mobile/releases/latest') -or
    ($shellText -notmatch 'class="android-promo"')) {
    throw "Grimoire Home must advertise the official Android release through the native link allowlist."
}
if (($shellText -notmatch 'document\.addEventListener\("scroll", revealScrollbars, true\)') -or
    ($shellStylesText -notmatch 'html\.is-scrolling') -or
    ($shellStylesText -notmatch 'scrollbar-color:\s*transparent transparent')) {
    throw "Modern scrollbars must stay hidden until their surface is hovered or actively scrolling."
}

$requiredHomeActions = @(
    "home.getState", "home.accounts.save", "home.accounts.remove", "home.accounts.open",
    "home.botFolder", "home.doctor.begin", "home.doctor.status", "home.doctor.copy",
    "home.report.begin", "home.report.status", "home.link.open"
)
foreach ($action in $requiredHomeActions) {
    if ($rootShellText -notmatch ('case\s+"' + [regex]::Escape($action) + '"')) {
        throw "The native modern Home adapter is missing the '$action' action."
    }
    if ($shellText -notmatch ('["'']' + [regex]::Escape($action) + '["'']')) {
        throw "The Vue modern Home surface is missing the '$action' action."
    }
}

foreach ($action in @("location.refresh", "location.pads", "location.move")) {
    if ($rootShellText -notmatch ('case\s+"' + [regex]::Escape($action) + '"')) {
        throw "The native modern location adapter is missing the '$action' action."
    }
    if ($shellText -notmatch ('["'']' + [regex]::Escape($action) + '["'']')) {
        throw "The Vue modern location surface is missing the '$action' action."
    }
    if ($protocolText -notmatch ('["'']' + [regex]::Escape($action) + '["'']')) {
        throw "The modern UI protocol allowlist is missing the '$action' action."
    }
}

if (($rootShellText -notmatch 'World\.CellInfo\(requestedCell\)') -or
    ($rootShellText -notmatch 'CacheModernPadChoices') -or
    ($rootShellText -notmatch 'GetCachedModernPadChoices') -or
    ($shellText -notmatch '@change="refreshPadsForCell"') -or
    ($shellText -notmatch 'bridge\.request<[^>]+>\("location\.pads"')) {
    throw "The modern Pad selector must load the actual pads for the selected Cell."
}

if (($rootShellText -notmatch '\["update"\]\s*=') -or
    ($rootShellText -notmatch 'case\s+"updates\.install"') -or
    ($protocolText -notmatch '["'']updates\.install["'']') -or
    ($shellStoreText -notmatch 'update:\s*UpdateSnapshot') -or
    ($shellText -notmatch 'v-if="store\.snapshot\.update\.available"') -or
    ($shellText -notmatch '["'']updates\.install["'']')) {
    throw "The modern top bar must expose the verified application update when one is available."
}

$requiredTools = @(
    "bot-manager", "settings", "quick-load", "load-bot", "travel", "loaders", "hotkeys",
    "packet-logger", "packet-spammer", "packet-tamperer", "plugin-manager", "bank", "bank-items",
    "bank-reload", "cosmetics", "eye-dropper", "logs", "notepad", "ping-monitor", "immediate-login",
    "ultra-kit"
)
foreach ($tool in $requiredTools) {
    if ($rootShellText -notmatch ('case\s+"' + [regex]::Escape($tool) + '"')) {
        throw "The native modern-shell adapter is missing the '$tool' action."
    }
    if ($shellText -notmatch ('["'']' + [regex]::Escape($tool) + '["'']')) {
        throw "The Vue modern shell is missing the '$tool' action."
    }
}

$requiredOptions = @(
    "infinite-range", "provoke-cell", "provoke-map", "target-magnet", "lag-killer", "hide-players",
    "skip-cutscenes", "disable-animations", "untarget-self", "auto-save-state", "fast-walk"
)
foreach ($option in $requiredOptions) {
    if ($rootShellText -notmatch ('case\s+"' + [regex]::Escape($option) + '"')) {
        throw "The native modern-shell adapter is missing the '$option' option."
    }
    if ($shellText -notmatch ('id:\s*"' + [regex]::Escape($option) + '"')) {
        throw "The Vue modern shell is missing the '$option' option."
    }
}

Write-Host "PASS: modern shell keeps the client mounted, exposes one Quick Load action, provides custom window chrome, and retains the complete native adapters."

if (Test-Path -LiteralPath $clientProbe) {
    throw "The smoke test will not overwrite the existing assembly probe: $clientProbe"
}

Copy-Item -LiteralPath $client -Destination $clientProbe
try {
    & $compiler /nologo /target:exe /platform:x86 /reference:System.Drawing.dll "/reference:$client" "/reference:$json" "/out:$smokeExecutable" $source
    if ($LASTEXITCODE -ne 0) {
        throw "Compiling the modern UI smoke test failed with exit code $LASTEXITCODE."
    }

    Push-Location $outputDirectory
    try {
        & $smokeExecutable
        if ($LASTEXITCODE -ne 0) {
            throw "Modern UI smoke test failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    foreach ($temporaryPath in @($smokeExecutable, $clientProbe)) {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}
