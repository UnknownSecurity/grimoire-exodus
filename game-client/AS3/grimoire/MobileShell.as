package grimoire
{
    import flash.display.DisplayObject;
    import flash.display.Loader;
    import flash.display.Shape;
    import flash.display.Sprite;
    import flash.events.Event;
    import flash.events.IOErrorEvent;
    import flash.events.MouseEvent;
    import flash.events.SecurityErrorEvent;
    import flash.geom.Rectangle;
    import flash.net.navigateToURL;
    import flash.net.URLLoader;
    import flash.net.URLRequest;
    import flash.text.TextField;
    import flash.text.TextFormat;
    import flash.text.TextFormatAlign;
    import flash.utils.getTimer;

    /**
     * AIR-only command-center overlay for the live AQW display. AQW remains a
     * direct Stage child; this class only lays it out and draws controls above
     * it so the desktop ExternalInterface host remains unchanged.
     */
    public final class MobileShell extends Sprite
    {
        public static const DESIGN_WIDTH:Number = 960;
        public static const DESIGN_HEIGHT:Number = 550;

        private static const GAME_WIDTH:Number = 960;
        private static const GAME_HEIGHT:Number = 550;
        private static const MARGIN:Number = 8;
        private static const TOP_HEIGHT:Number = 52;
        private static const SIDEBAR_WIDTH:Number = 300;
        private static const VIEW_X:Number = 308;
        private static const VIEW_Y:Number = 68;
        private static const TRANSITION_MS:Number = 180;

        private static const CANVAS:uint = 0x05060B;
        private static const SURFACE:uint = 0x0B0D15;
        private static const ELEVATED:uint = 0x121522;
        private static const CONTROL:uint = 0x191C2C;
        private static const VIOLET:uint = 0x8B5CF6;
        private static const BRIGHT_VIOLET:uint = 0xA855F7;
        private static const PALE_VIOLET:uint = 0xC4B5FD;
        private static const PRIMARY_TEXT:uint = 0xF7F5FF;
        private static const SECONDARY_TEXT:uint = 0x9B96B3;
        private static const DIVIDER:uint = 0x29263B;
        private static const SUCCESS:uint = 0x22C55E;
        private static const DANGER:uint = 0xEF4444;

        private var game:DisplayObject;
        private var background:Shape;
        private var chrome:Sprite;
        private var chromeCanvas:Shape;
        private var sidebarContent:Sprite;
        private var toggleButton:Sprite;
        private var settingsButton:Sprite;
        private var navigationButtons:Array = [];
        private var selectedNavigation:String = "Home";
        private var playerNameField:TextField;
        private var playerDetailField:TextField;
        private var connectionField:TextField;
        private var emblemLoader:Loader;
        private var chromeVisible:Boolean = true;
        private var preferences:MobilePreferences;
        private var botLibrary:MobileBotLibrary;
        private var botRunner:MobileBotRunner;
        private var botPageStart:int;
        private var settingsPage:int;
        private var resetArmedAt:int;
        private var exitArmedAt:int;
        private var updateLoader:URLLoader;
        private var availableUpdateUrl:String;
        private var lagKillerEnabled:Boolean;
        private var playersHidden:Boolean;

        private var targetToggleX:Number;
        private var targetToggleY:Number;
        private var targetChromeAlpha:Number = 1;

        private var layoutWidth:Number = DESIGN_WIDTH;
        private var layoutHeight:Number = DESIGN_HEIGHT;
        private var interfaceScale:Number = 1;
        private var viewportX:Number;
        private var viewportY:Number;
        private var viewportWidth:Number = DESIGN_WIDTH;
        private var viewportHeight:Number = DESIGN_HEIGHT;

        private var animationStartedAt:int;
        private var startToggleX:Number;
        private var startToggleY:Number;
        private var startChromeAlpha:Number;

        public function MobileShell(game:DisplayObject)
        {
            this.game = game;
            // AQW stays at its native origin and scale. The legacy client reads
            // Stage-space pointer coordinates, so translating or scaling this
            // display object makes touch targets diverge from their artwork.
            this.game.x = 0;
            this.game.y = 0;
            this.game.scaleX = this.game.scaleY = 1;
            this.game.scrollRect = new Rectangle(0, 0, GAME_WIDTH, GAME_HEIGHT);
            this.preferences = new MobilePreferences();
            this.botLibrary = new MobileBotLibrary();
            this.botRunner = new MobileBotRunner();
            this.botLibrary.addEventListener(MobileBotLibrary.CHANGED, this.onBotLibraryChanged);
            this.botRunner.addEventListener(MobileBotRunner.STATE_CHANGED, this.onBotStateChanged);
            this.chromeVisible = !this.preferences.startFocused;
            mouseEnabled = false;

            this.chrome = new Sprite();
            this.chrome.mouseEnabled = false;
            addChild(this.chrome);

            this.background = new Shape();
            this.chrome.addChild(this.background);

            this.chromeCanvas = new Shape();
            this.chrome.addChild(this.chromeCanvas);
            this.drawChrome();
            this.buildTopNavigation();
            this.buildProfileCard();

            this.sidebarContent = new Sprite();
            this.sidebarContent.x = 16;
            this.sidebarContent.y = 145;
            this.chrome.addChild(this.sidebarContent);
            this.renderSidebar(this.selectedNavigation);

            this.toggleButton = this.createToggleButton();
            addChild(this.toggleButton);

            this.calculateTargets();
            this.applyTargetsImmediately();
        }

        public function layout(stageWidth:Number, stageHeight:Number):void
        {
            if (stageWidth <= 0 || stageHeight <= 0)
            {
                return;
            }

            removeEventListener(Event.ENTER_FRAME, this.animateLayout);
            // EXACT_FIT keeps the logical AIR Stage at 960x550 and lets AIR map
            // it to the physical panel. Keeping both AQW and chrome in those
            // coordinates avoids the legacy Stage mouse-coordinate mismatch.
            this.interfaceScale = 1;
            this.layoutWidth = DESIGN_WIDTH;
            this.layoutHeight = DESIGN_HEIGHT;
            this.scaleX = this.scaleY = 1;
            this.x = 0;
            this.y = 0;

            this.drawChrome();
            if (this.settingsButton != null)
            {
                this.settingsButton.x = this.layoutWidth - 50;
            }
            this.calculateTargets();
            this.applyTargetsImmediately();
            this.preferences.apply(stage);
        }

        public function handleKey(keyCode:uint):void
        {
            if (keyCode == this.preferences.bindingFor("startBot"))
            {
                this.startSelectedBot();
            }
            else if (keyCode == this.preferences.bindingFor("stopBot"))
            {
                this.botRunner.stop();
            }
            else if (keyCode == this.preferences.bindingFor("toggleChrome"))
            {
                this.toggleChrome();
            }
            else if (keyCode == this.preferences.bindingFor("openSettings"))
            {
                this.showSection("Settings");
            }
            else if (keyCode == this.preferences.bindingFor("lagKiller"))
            {
                this.toggleLagKiller();
            }
            else if (keyCode == this.preferences.bindingFor("hidePlayers"))
            {
                this.toggleHidePlayers();
            }
        }

        public function handleBack():Boolean
        {
            if (!this.chromeVisible)
            {
                this.toggleChrome();
                return true;
            }
            if (!this.preferences.confirmExit)
            {
                return false;
            }
            if (getTimer() - this.exitArmedAt <= 2200)
            {
                return false;
            }
            this.exitArmedAt = getTimer();
            this.botLibraryStatus("PRESS BACK AGAIN TO EXIT");
            return true;
        }

        public function setPlayerProfile(value:String, level:*, characterClass:String):void
        {
            var normalized:String = value == null ? "" : value.replace(/^\s+|\s+$/g, "");
            var normalizedClass:String = characterClass == null ? "" : characterClass.replace(/^\s+|\s+$/g, "");
            var details:Array = [];
            this.playerNameField.text = normalized.length == 0 ? "AQW PLAYER" : normalized.toUpperCase();

            if (level !== null && level !== undefined && String(level).length > 0)
            {
                details.push("LVL " + String(level));
            }
            if (normalizedClass.length > 0)
            {
                details.push(normalizedClass.toUpperCase());
            }
            this.playerDetailField.text = details.length > 0 ? details.join(" / ") : "AUTHENTICATED PLAYER";
        }

        public function resetPlayerProfile():void
        {
            this.playerNameField.text = "AQW PLAYER";
            this.playerDetailField.text = "SIGN IN TO CONNECT";
        }

        public function setConnected(connected:Boolean):void
        {
            this.connectionField.text = connected ? "CLIENT ONLINE" : "CLIENT DISCONNECTED";
            this.connectionField.textColor = connected ? SUCCESS : DANGER;
            if (!connected && this.botRunner != null && this.botRunner.isRunning)
            {
                this.botRunner.stop("BOT STOPPED: CLIENT DISCONNECTED");
            }
        }

        private function drawChrome():void
        {
            this.background.graphics.clear();
            this.background.graphics.beginFill(CANVAS, 0.94);
            this.background.graphics.drawRect(0, 0, this.layoutWidth, VIEW_Y);
            this.background.graphics.drawRect(0, VIEW_Y, VIEW_X, this.layoutHeight - VIEW_Y);
            this.background.graphics.endFill();

            this.chromeCanvas.graphics.clear();
            this.drawPanel(this.chromeCanvas, MARGIN, MARGIN, this.layoutWidth - (MARGIN * 2), TOP_HEIGHT, 14, SURFACE, DIVIDER, 1);
            this.drawPanel(this.chromeCanvas, MARGIN, VIEW_Y, SIDEBAR_WIDTH, this.layoutHeight - VIEW_Y - MARGIN, 14, SURFACE, DIVIDER, 1);

            this.chromeCanvas.graphics.lineStyle(1, VIOLET, 0.45);
            this.chromeCanvas.graphics.moveTo(222, 18);
            this.chromeCanvas.graphics.lineTo(222, 56);
        }

        private function buildTopNavigation():void
        {
            var labels:Array = ["Home", "Bot", "Tools", "Settings"];
            var i:int;
            var button:Sprite;

            this.drawBrandMark();
            this.addText(this.chrome, "GRIMOIRE", 67, 17, 145, 27, 19, PRIMARY_TEXT, true, "Georgia");
            this.addText(this.chrome, "MOBILE COMMAND CENTER", 68, 40, 145, 14, 8, SECONDARY_TEXT, true);

            for (i = 0; i < labels.length; i++)
            {
                button = this.createNavigationButton(labels[i], 238 + (i * 82), 14, 76, 40);
                this.navigationButtons.push(button);
                this.chrome.addChild(button);
            }

            this.chrome.addChild(this.createHeaderAction("play", 584, 19, false));
            this.chrome.addChild(this.createHeaderAction("stop", 628, 19, true));
            this.settingsButton = this.createHeaderAction("settings", 910, 19, false);
            this.chrome.addChild(this.settingsButton);
        }

        private function drawBrandMark():void
        {
            var mark:Shape = new Shape();
            mark.x = 20;
            mark.y = 17;
            mark.graphics.lineStyle(2, BRIGHT_VIOLET, 1);
            mark.graphics.moveTo(3, 5);
            mark.graphics.lineTo(18, 0);
            mark.graphics.lineTo(18, 30);
            mark.graphics.lineTo(3, 25);
            mark.graphics.lineTo(3, 5);
            mark.graphics.moveTo(18, 0);
            mark.graphics.lineTo(33, 5);
            mark.graphics.lineTo(33, 25);
            mark.graphics.lineTo(18, 30);
            mark.graphics.moveTo(8, 9);
            mark.graphics.lineTo(14, 7);
            mark.graphics.moveTo(22, 7);
            mark.graphics.lineTo(28, 9);
            this.chrome.addChild(mark);

            this.emblemLoader = new Loader();
            this.emblemLoader.contentLoaderInfo.addEventListener(Event.COMPLETE, this.onEmblemLoaded);
            this.emblemLoader.contentLoaderInfo.addEventListener(IOErrorEvent.IO_ERROR, this.onEmblemError);
            this.emblemLoader.load(new URLRequest("app:/assets/grimoire_emblem.png"));
        }

        private function onEmblemLoaded(e:Event):void
        {
            this.emblemLoader.contentLoaderInfo.removeEventListener(Event.COMPLETE, this.onEmblemLoaded);
            this.emblemLoader.contentLoaderInfo.removeEventListener(IOErrorEvent.IO_ERROR, this.onEmblemError);
            this.emblemLoader.x = 17;
            this.emblemLoader.y = 12;
            this.emblemLoader.width = 42;
            this.emblemLoader.height = 42;
            this.chrome.addChild(this.emblemLoader);
        }

        private function onEmblemError(e:IOErrorEvent):void
        {
            this.emblemLoader.contentLoaderInfo.removeEventListener(Event.COMPLETE, this.onEmblemLoaded);
            this.emblemLoader.contentLoaderInfo.removeEventListener(IOErrorEvent.IO_ERROR, this.onEmblemError);
        }

        private function buildProfileCard():void
        {
            var card:Shape = new Shape();
            this.drawPanel(card, 16, 78, 284, 58, 12, ELEVATED, DIVIDER, 1);
            this.chrome.addChild(card);

            var avatar:Shape = new Shape();
            avatar.x = 25;
            avatar.y = 85;
            avatar.graphics.beginFill(0x241844);
            avatar.graphics.lineStyle(2, VIOLET, 1);
            avatar.graphics.drawCircle(20, 20, 20);
            avatar.graphics.endFill();
            avatar.graphics.lineStyle(2, PALE_VIOLET, 0.9);
            avatar.graphics.drawCircle(20, 15, 6);
            avatar.graphics.moveTo(9, 34);
            avatar.graphics.curveTo(20, 24, 31, 34);
            this.chrome.addChild(avatar);

            this.playerNameField = this.addText(this.chrome, "AQW PLAYER", 72, 84, 218, 20, 12, PRIMARY_TEXT, true);
            this.connectionField = this.addText(this.chrome, "CLIENT DISCONNECTED", 72, 103, 218, 16, 9, DANGER, true);
            this.playerDetailField = this.addText(this.chrome, "SIGN IN TO CONNECT", 72, 119, 218, 14, 8, SECONDARY_TEXT, false);
        }

        private function createNavigationButton(label:String, x:Number, y:Number, width:Number, height:Number):Sprite
        {
            var button:Sprite = new Sprite();
            button.name = "nav_" + label;
            button.x = x;
            button.y = y;
            button.buttonMode = true;
            button.useHandCursor = true;
            button.addEventListener(MouseEvent.CLICK, this.onNavigationClick);

            var backgroundShape:Shape = new Shape();
            backgroundShape.name = "background";
            button.addChild(backgroundShape);
            var labelField:TextField = this.addText(button, label, 0, 21, width, 18, 10, SECONDARY_TEXT, true, "_sans", TextFormatAlign.CENTER);
            labelField.name = "label";

            var icon:Shape = new Shape();
            icon.name = "icon";
            icon.x = width / 2;
            icon.y = 10;
            this.drawNavigationIcon(icon, label, false);
            button.addChild(icon);
            this.redrawNavigationButton(button, label == this.selectedNavigation, width, height);
            return button;
        }

        private function redrawNavigationButton(button:Sprite, selected:Boolean, width:Number, height:Number):void
        {
            var backgroundShape:Shape = button.getChildByName("background") as Shape;
            var labelField:TextField = button.getChildByName("label") as TextField;
            var icon:Shape = button.getChildByName("icon") as Shape;
            backgroundShape.graphics.clear();
            if (selected)
            {
                backgroundShape.graphics.beginFill(0x241844, 0.92);
                backgroundShape.graphics.lineStyle(1, VIOLET, 0.9);
                backgroundShape.graphics.drawRoundRect(0, 0, width, height, 12, 12);
                backgroundShape.graphics.endFill();
            }
            labelField.textColor = selected ? PALE_VIOLET : SECONDARY_TEXT;
            this.drawNavigationIcon(icon, button.name.substr(4), selected);
        }

        private function drawNavigationIcon(icon:Shape, label:String, selected:Boolean):void
        {
            var color:uint = selected ? BRIGHT_VIOLET : VIOLET;
            icon.graphics.clear();
            icon.graphics.lineStyle(1.8, color, 1);
            if (label == "Home")
            {
                icon.graphics.moveTo(-8, 2);
                icon.graphics.lineTo(0, -5);
                icon.graphics.lineTo(8, 2);
                icon.graphics.moveTo(-6, 1);
                icon.graphics.lineTo(-6, 8);
                icon.graphics.lineTo(6, 8);
                icon.graphics.lineTo(6, 1);
            }
            else if (label == "Bot")
            {
                icon.graphics.drawRoundRect(-7, -5, 14, 13, 4, 4);
                icon.graphics.moveTo(-3, 0);
                icon.graphics.lineTo(-3, 2);
                icon.graphics.moveTo(3, 0);
                icon.graphics.lineTo(3, 2);
                icon.graphics.moveTo(0, -5);
                icon.graphics.lineTo(0, -9);
            }
            else if (label == "Tools")
            {
                icon.graphics.moveTo(-7, -6);
                icon.graphics.lineTo(7, 8);
                icon.graphics.moveTo(5, -7);
                icon.graphics.lineTo(-7, 5);
                icon.graphics.drawCircle(6, -8, 2);
            }
            else if (label == "Packets")
            {
                icon.graphics.moveTo(-8, -4);
                icon.graphics.lineTo(0, -8);
                icon.graphics.lineTo(8, -4);
                icon.graphics.lineTo(0, 0);
                icon.graphics.lineTo(-8, -4);
                icon.graphics.moveTo(-8, 2);
                icon.graphics.lineTo(0, 6);
                icon.graphics.lineTo(8, 2);
            }
            else if (label == "Options" || label == "Settings")
            {
                icon.graphics.drawCircle(0, 0, 7);
                icon.graphics.drawCircle(0, 0, 2.5);
                icon.graphics.moveTo(0, -10);
                icon.graphics.lineTo(0, -7);
                icon.graphics.moveTo(0, 7);
                icon.graphics.lineTo(0, 10);
                icon.graphics.moveTo(-10, 0);
                icon.graphics.lineTo(-7, 0);
                icon.graphics.moveTo(7, 0);
                icon.graphics.lineTo(10, 0);
            }
            else
            {
                icon.graphics.drawCircle(-4, -3, 3);
                icon.graphics.drawCircle(4, -3, 3);
                icon.graphics.drawCircle(0, 5, 3);
                icon.graphics.moveTo(-2, 0);
                icon.graphics.lineTo(-1, 3);
                icon.graphics.moveTo(2, 0);
                icon.graphics.lineTo(1, 3);
            }
        }

        private function createHeaderAction(id:String, x:Number, y:Number, danger:Boolean):Sprite
        {
            var button:Sprite = new Sprite();
            button.name = "header_" + id;
            button.x = x;
            button.y = y;
            button.buttonMode = true;
            button.graphics.beginFill(CONTROL, 1);
            button.graphics.lineStyle(1, DIVIDER, 1);
            button.graphics.drawCircle(13, 13, 13);
            button.graphics.endFill();
            if (id == "play")
            {
                button.graphics.beginFill(PALE_VIOLET);
                button.graphics.moveTo(10, 7);
                button.graphics.lineTo(19, 13);
                button.graphics.lineTo(10, 19);
                button.graphics.lineTo(10, 7);
                button.graphics.endFill();
            }
            else if (id == "stop")
            {
                button.graphics.beginFill(danger ? DANGER : PALE_VIOLET);
                button.graphics.drawCircle(13, 13, 5);
                button.graphics.endFill();
            }
            else
            {
                button.graphics.lineStyle(2, PALE_VIOLET, 1);
                button.graphics.drawCircle(13, 13, 6);
                button.graphics.drawCircle(13, 13, 2);
                button.graphics.moveTo(13, 3);
                button.graphics.lineTo(13, 7);
                button.graphics.moveTo(13, 19);
                button.graphics.lineTo(13, 23);
                button.graphics.moveTo(3, 13);
                button.graphics.lineTo(7, 13);
                button.graphics.moveTo(19, 13);
                button.graphics.lineTo(23, 13);
            }
            button.addEventListener(MouseEvent.CLICK, this.onCommandClick);
            return button;
        }

        private function onNavigationClick(e:MouseEvent):void
        {
            this.selectedNavigation = Sprite(e.currentTarget).name.substr(4);
            var i:int;
            var button:Sprite;
            for (i = 0; i < this.navigationButtons.length; i++)
            {
                button = this.navigationButtons[i] as Sprite;
                this.redrawNavigationButton(button, button.name == "nav_" + this.selectedNavigation, 76, 40);
            }
            this.renderSidebar(this.selectedNavigation);
            HostBridge.call("mobileNavigation", this.selectedNavigation);
        }

        private function showSection(section:String):void
        {
            this.selectedNavigation = section;
            if (!this.chromeVisible)
            {
                this.toggleChrome();
            }
            var i:int;
            for (i = 0; i < this.navigationButtons.length; i++)
            {
                var button:Sprite = this.navigationButtons[i] as Sprite;
                this.redrawNavigationButton(button, button.name == "nav_" + section, 76, 40);
            }
            this.renderSidebar(section);
        }

        private function renderSidebar(section:String):void
        {
            while (this.sidebarContent.numChildren > 0)
            {
                this.sidebarContent.removeChildAt(0);
            }

            this.addText(this.sidebarContent, section.toUpperCase(), 3, 0, 274, 21, 11, PRIMARY_TEXT, true);
            this.addText(this.sidebarContent, this.sectionSubtitle(section), 3, 19, 274, 23, 9, SECONDARY_TEXT, false);

            if (section == "Home")
            {
                this.drawSectionCard("SESSION", "Authenticated AQW client", 43, ["Bot", "Settings", "Focus"]);
                this.drawSectionCard("QUICK CONTROLS", "Common live actions", 139, ["Start", "Stop", "Pause", "Rest", "Lag killer", "Hide players"]);
                this.drawInfoCard(278, "MOBILE STATUS", this.botRunner.status);
            }
            else if (section == "Bot")
            {
                this.renderBotManager();
            }
            else if (section == "Tools")
            {
                this.drawSectionCard("CLIENT TOOLS", "Useful mobile actions", 43, ["Rest", "Spawn", "Cancel target", "Cancel attack", "Lag killer", "Hide players"]);
                this.drawInfoCard(182, "SPACE EFFICIENT", "The drawer overlays AQW without moving its native pointer coordinate plane. Hide it for uninterrupted play.");
            }
            else
            {
                this.renderSettings();
            }

            if (!this.preferences.hideStatus)
            {
                this.addText(this.sidebarContent, "AIR  •  AQW LIVE  •  " + this.botRunner.status,
                    4, 378, 272, 17, 8, this.botRunner.isRunning ? SUCCESS : SECONDARY_TEXT,
                    true, "_sans", TextFormatAlign.CENTER);
            }
        }

        private function sectionSubtitle(section:String):String
        {
            if (section == "Home") return "Overview and player controls";
            if (section == "Bot") return "Load, inspect, and run legacy GBots";
            if (section == "Tools") return "In-game Grimoire utilities";
            return "General, keys, and advanced";
        }

        private function renderBotManager():void
        {
            var entry:Object = this.botLibrary.selectedEntry;
            var selectedName:String = entry == null ? "NO BOT SELECTED" : String(entry.name).toUpperCase();
            var source:String = entry == null ? "IMPORT OR SELECT A BOT"
                : (entry.community ? "COMMUNITY" : "IMPORTED");
            this.drawInfoCard(43, selectedName, source + "  •  " + this.botLibrary.status);

            var entries:Array = this.botLibrary.entries;
            var pageSize:int = 4;
            var maximumStart:int = Math.max(0, entries.length - pageSize);
            this.botPageStart = Math.max(0, Math.min(maximumStart, this.botPageStart));
            if (this.botLibrary.selectedIndex >= 0
                && (this.botLibrary.selectedIndex < this.botPageStart
                    || this.botLibrary.selectedIndex >= this.botPageStart + pageSize))
            {
                this.botPageStart = Math.min(maximumStart,
                    Math.floor(this.botLibrary.selectedIndex / pageSize) * pageSize);
            }

            var y:Number = 124;
            for (var i:int = this.botPageStart;
                i < entries.length && i < this.botPageStart + pageSize; i++)
            {
                var item:Object = entries[i];
                var marker:String = i == this.botLibrary.selectedIndex ? "● " : "○ ";
                this.sidebarContent.addChild(this.createAction("bot_select_" + i,
                    marker + this.ellipsize(String(item.name), 28), 0, y, 284, 31, TextFormatAlign.LEFT));
                y += 34;
            }
            if (entries.length == 0)
            {
                this.addText(this.sidebarContent, "No bundled or imported bots are available.",
                    8, y + 8, 268, 34, 9, SECONDARY_TEXT, false);
            }

            var firstShown:int = entries.length == 0 ? 0 : this.botPageStart + 1;
            var lastShown:int = Math.min(entries.length, this.botPageStart + pageSize);
            this.sidebarContent.addChild(this.createAction("bot_previous", "‹", 0, 263, 42, 28));
            this.addText(this.sidebarContent,
                entries.length == 0 ? "EMPTY" : firstShown + "–" + lastShown + " / " + entries.length,
                49, 269, 186, 17, 8, SECONDARY_TEXT, true, "_sans", TextFormatAlign.CENTER);
            this.sidebarContent.addChild(this.createAction("bot_next", "›", 242, 263, 42, 28));

            this.sidebarContent.addChild(this.createAction("bot_import", "IMPORT", 0, 296, 90, 32));
            this.sidebarContent.addChild(this.createAction("bot_refresh", "REFRESH", 97, 296, 90, 32));
            this.sidebarContent.addChild(this.createAction("bot_delete", "DELETE", 194, 296, 90, 32));
            this.sidebarContent.addChild(this.createAction("bot_start", "START", 0, 333, 90, 32));
            this.sidebarContent.addChild(this.createAction("bot_pause", this.botRunner.isPaused ? "RESUME" : "PAUSE", 97, 333, 90, 32));
            this.sidebarContent.addChild(this.createAction("bot_stop", "STOP", 194, 333, 90, 32));
        }

        private function renderSettings():void
        {
            this.sidebarContent.addChild(this.createAction("settings_general", "GENERAL", 0, 43, 90, 30));
            this.sidebarContent.addChild(this.createAction("settings_keys", "KEYS", 97, 43, 90, 30));
            this.sidebarContent.addChild(this.createAction("settings_advanced", "ADVANCED", 194, 43, 90, 30));

            if (this.settingsPage == 0)
            {
                this.addSettingToggle("pref_start_focused", "START IN FOCUS MODE", this.preferences.startFocused, 83);
                this.addSettingToggle("pref_hide_status", "HIDE STATUS STRIP", this.preferences.hideStatus, 125);
                this.addSettingToggle("pref_confirm_exit", "CONFIRM EXIT", this.preferences.confirmExit, 167);
                this.addSettingToggle("pref_reduced_motion", "REDUCED MOTION", this.preferences.reducedMotion, 209);
                this.addSettingToggle("pref_keep_awake", "KEEP SCREEN AWAKE", this.preferences.keepAwake, 251);
                this.addSettingValue("pref_fps", "FRAME RATE", String(this.preferences.frameRate) + " FPS", 293);
                this.addSettingValue("pref_quality", "RENDER QUALITY", this.preferences.quality.toUpperCase(), 335);
            }
            else if (this.settingsPage == 1)
            {
                this.addBindingRow("startBot", "START BOT", 83);
                this.addBindingRow("stopBot", "STOP BOT", 125);
                this.addBindingRow("toggleChrome", "SHOW / HIDE UI", 167);
                this.addBindingRow("openSettings", "OPEN SETTINGS", 209);
                this.addBindingRow("lagKiller", "LAG KILLER", 251);
                this.addBindingRow("hidePlayers", "HIDE PLAYERS", 293);
                this.addText(this.sidebarContent, "Tap a row to cycle None and F5–F12.",
                    7, 342, 270, 24, 8, SECONDARY_TEXT, false);
            }
            else
            {
                this.sidebarContent.addChild(this.createAction("advanced_credentials", "DELETE CHARACTER INFO", 0, 88, 284, 38));
                this.sidebarContent.addChild(this.createAction("advanced_update", "CHECK FOR UPDATE", 0, 136, 284, 38));
                var resetLabel:String = getTimer() - this.resetArmedAt <= 3000
                    ? "CONFIRM FULL RESET" : "FULL RESET";
                this.sidebarContent.addChild(this.createAction("advanced_reset", resetLabel, 0, 184, 284, 38));
                this.drawInfoCard(235, "MOBILE DATA", "Reset removes mobile settings and session data. Imported and Community bots, plus the desktop installation, are preserved.");
            }
        }

        private function addSettingToggle(id:String, label:String, enabled:Boolean, y:Number):void
        {
            this.addSettingValue(id, label, enabled ? "ON" : "OFF", y,
                enabled ? SUCCESS : SECONDARY_TEXT);
        }

        private function addSettingValue(id:String, label:String, value:String, y:Number, valueColor:uint = PALE_VIOLET):void
        {
            var button:Sprite = this.createAction(id, "", 0, y, 284, 36);
            this.addText(button, label, 10, 9, 194, 18, 9, PRIMARY_TEXT, true);
            this.addText(button, value, 203, 9, 70, 18, 9, valueColor, true, "_sans", TextFormatAlign.RIGHT);
            this.sidebarContent.addChild(button);
        }

        private function addBindingRow(id:String, label:String, y:Number):void
        {
            this.addSettingValue("key_" + id, label,
                MobilePreferences.keyLabel(this.preferences.bindingFor(id)), y);
        }

        private function drawSectionCard(title:String, subtitle:String, y:Number, labels:Array):void
        {
            var card:Shape = new Shape();
            var rows:int = Math.ceil(labels.length / 3);
            var height:Number = 52 + (rows * 43);
            this.drawPanel(card, 0, y, 284, height, 11, ELEVATED, DIVIDER, 1);
            this.sidebarContent.addChild(card);
            this.addText(this.sidebarContent, title, 10, y + 8, 264, 18, 10, PALE_VIOLET, true);
            this.addText(this.sidebarContent, subtitle, 10, y + 25, 264, 16, 8, SECONDARY_TEXT, false);

            var i:int;
            var x:Number;
            var buttonY:Number;
            for (i = 0; i < labels.length; i++)
            {
                x = 9 + ((i % 3) * 89);
                buttonY = y + 46 + (Math.floor(i / 3) * 43);
                this.sidebarContent.addChild(this.createSidebarAction(labels[i], x, buttonY, 82, 35));
            }
        }

        private function drawInfoCard(y:Number, title:String, body:String):void
        {
            var card:Shape = new Shape();
            this.drawPanel(card, 0, y, 284, 78, 11, ELEVATED, DIVIDER, 1);
            this.sidebarContent.addChild(card);
            this.addText(this.sidebarContent, title, 10, y + 9, 264, 18, 10, PALE_VIOLET, true);
            var bodyField:TextField = this.addText(this.sidebarContent, body, 10, y + 28, 264, 43, 8, SECONDARY_TEXT, false);
            bodyField.wordWrap = true;
        }

        private function createSidebarAction(label:String, x:Number, y:Number, width:Number, height:Number):Sprite
        {
            var button:Sprite = new Sprite();
            button.name = "command_" + label.toLowerCase();
            button.x = x;
            button.y = y;
            button.buttonMode = true;
            button.useHandCursor = true;
            button.graphics.beginFill(CONTROL, 1);
            button.graphics.lineStyle(1, DIVIDER, 1);
            button.graphics.drawRoundRect(0, 0, width, height, 9, 9);
            button.graphics.endFill();
            this.addText(button, label, 0, 9, width, 18, 9, PRIMARY_TEXT, true, "_sans", TextFormatAlign.CENTER);
            button.addEventListener(MouseEvent.CLICK, this.onCommandClick);
            return button;
        }

        private function createAction(id:String, label:String, x:Number, y:Number,
            width:Number, height:Number, align:String = TextFormatAlign.CENTER):Sprite
        {
            var button:Sprite = new Sprite();
            button.name = id;
            button.x = x;
            button.y = y;
            button.buttonMode = true;
            button.useHandCursor = true;
            button.graphics.beginFill(CONTROL, 0.98);
            button.graphics.lineStyle(1, DIVIDER, 1);
            button.graphics.drawRoundRect(0, 0, width, height, 9, 9);
            button.graphics.endFill();
            this.addText(button, label, align == TextFormatAlign.LEFT ? 10 : 0,
                Math.max(6, (height - 18) / 2), align == TextFormatAlign.LEFT ? width - 20 : width,
                18, 9, PRIMARY_TEXT, true, "_sans", align);
            button.addEventListener(MouseEvent.CLICK, this.onCommandClick);
            return button;
        }

        private function onCommandClick(e:MouseEvent):void
        {
            var id:String = Sprite(e.currentTarget).name;
            if (id == "header_play" || id == "command_start" || id == "bot_start")
            {
                this.startSelectedBot();
            }
            else if (id == "header_stop" || id == "command_stop" || id == "bot_stop")
            {
                this.botRunner.stop();
            }
            else if (id == "command_pause" || id == "bot_pause")
            {
                this.botRunner.togglePause();
            }
            else if (id == "header_settings" || id == "command_settings")
            {
                this.showSection("Settings");
            }
            else if (id == "command_bot")
            {
                this.showSection("Bot");
            }
            else if (id == "command_focus")
            {
                this.toggleChrome();
            }
            else if (id == "command_rest")
            {
                HostBridge.invoke("Rest");
            }
            else if (id == "command_spawn")
            {
                HostBridge.invoke("SetSpawnPoint");
            }
            else if (id == "command_cancel target")
            {
                HostBridge.invoke("CancelTarget");
            }
            else if (id == "command_cancel attack")
            {
                HostBridge.invoke("CancelAutoAttack");
            }
            else if (id == "command_lag killer")
            {
                this.toggleLagKiller();
            }
            else if (id == "command_hide players")
            {
                this.toggleHidePlayers();
            }
            else if (id == "bot_import")
            {
                this.botLibrary.browseForImport();
            }
            else if (id == "bot_refresh")
            {
                this.botLibrary.refresh();
            }
            else if (id == "bot_previous")
            {
                this.botPageStart = Math.max(0, this.botPageStart - 4);
                this.renderSidebar("Bot");
            }
            else if (id == "bot_next")
            {
                this.botPageStart = Math.min(Math.max(0, this.botLibrary.entries.length - 4),
                    this.botPageStart + 4);
                this.renderSidebar("Bot");
            }
            else if (id == "bot_delete")
            {
                this.botLibrary.deleteSelected();
            }
            else if (id.indexOf("bot_select_") == 0)
            {
                this.botLibrary.select(int(id.substr(11)));
            }
            else if (id.indexOf("settings_") == 0)
            {
                this.settingsPage = id == "settings_general" ? 0 : (id == "settings_keys" ? 1 : 2);
                this.renderSidebar("Settings");
            }
            else if (id.indexOf("pref_") == 0)
            {
                this.togglePreference(id);
            }
            else if (id.indexOf("key_") == 0)
            {
                this.preferences.cycleBinding(id.substr(4));
                this.renderSidebar("Settings");
            }
            else if (id == "advanced_credentials")
            {
                Root.username = null;
                Root.password = null;
                try
                {
                    if (Root.Game != null && Root.Game.mcLogin != null)
                    {
                        if (Root.Game.mcLogin.ni != null) Root.Game.mcLogin.ni.text = "";
                        if (Root.Game.mcLogin.pi != null) Root.Game.mcLogin.pi.text = "";
                    }
                }
                catch (credentialError:Error)
                {
                }
                this.botLibraryStatus("CACHED MOBILE LOGIN FIELDS CLEARED");
            }
            else if (id == "advanced_update")
            {
                if (this.availableUpdateUrl != null)
                {
                    navigateToURL(new URLRequest(this.availableUpdateUrl), "_blank");
                    this.botLibraryStatus("OPENED THE VERIFIED MOBILE RELEASE ASSET");
                }
                else
                {
                    this.checkForUpdate();
                }
            }
            else if (id == "advanced_reset")
            {
                this.fullReset();
            }
            else
            {
                HostBridge.call("mobileCommand", id);
            }
        }

        private function createToggleButton():Sprite
        {
            var button:Sprite = new Sprite();
            button.buttonMode = true;
            button.useHandCursor = true;
            button.addEventListener(MouseEvent.CLICK, this.onToggleClick);
            this.drawToggleButton(button);
            return button;
        }

        private function drawToggleButton(button:Sprite):void
        {
            button.graphics.clear();
            button.graphics.beginFill(0x090B13, 0.96);
            button.graphics.lineStyle(2, BRIGHT_VIOLET, 1);
            button.graphics.drawCircle(0, 0, 19);
            button.graphics.endFill();
            button.graphics.lineStyle(2, PRIMARY_TEXT, 0.95);

            if (this.chromeVisible)
            {
                // Full-screen glyph: the next tap zooms AQW outward.
                button.graphics.moveTo(-3, -9);
                button.graphics.lineTo(-9, -9);
                button.graphics.lineTo(-9, -3);
                button.graphics.moveTo(3, -9);
                button.graphics.lineTo(9, -9);
                button.graphics.lineTo(9, -3);
                button.graphics.moveTo(-3, 9);
                button.graphics.lineTo(-9, 9);
                button.graphics.lineTo(-9, 3);
                button.graphics.moveTo(3, 9);
                button.graphics.lineTo(9, 9);
                button.graphics.lineTo(9, 3);
            }
            else
            {
                // Restore glyph: the next tap pulls AQW back into the frame.
                button.graphics.moveTo(-9, -4);
                button.graphics.lineTo(-4, -4);
                button.graphics.lineTo(-4, -9);
                button.graphics.moveTo(9, -4);
                button.graphics.lineTo(4, -4);
                button.graphics.lineTo(4, -9);
                button.graphics.moveTo(-9, 4);
                button.graphics.lineTo(-4, 4);
                button.graphics.lineTo(-4, 9);
                button.graphics.moveTo(9, 4);
                button.graphics.lineTo(4, 4);
                button.graphics.lineTo(4, 9);
            }
        }

        private function onToggleClick(e:MouseEvent):void
        {
            this.toggleChrome();
        }

        private function toggleChrome():void
        {
            this.chromeVisible = !this.chromeVisible;
            this.chrome.mouseChildren = this.chromeVisible;
            this.chrome.visible = true;
            this.calculateTargets();
            this.drawToggleButton(this.toggleButton);

            this.animationStartedAt = getTimer();
            this.startToggleX = this.toggleButton.x;
            this.startToggleY = this.toggleButton.y;
            this.startChromeAlpha = this.chrome.alpha;
            if (this.preferences.reducedMotion)
            {
                this.applyTargetsImmediately();
            }
            else if (!hasEventListener(Event.ENTER_FRAME))
            {
                addEventListener(Event.ENTER_FRAME, this.animateLayout);
            }
            HostBridge.call("mobileChromeChanged", this.chromeVisible);
        }

        private function calculateTargets():void
        {
            this.viewportX = 0;
            this.viewportY = 0;
            this.viewportWidth = GAME_WIDTH;
            this.viewportHeight = GAME_HEIGHT;
            this.targetToggleX = this.chromeVisible ? VIEW_X + 23 : 23;
            this.targetToggleY = this.chromeVisible ? VIEW_Y + 23 : 23;
            this.targetChromeAlpha = this.chromeVisible ? 1 : 0;
        }

        private function applyTargetsImmediately():void
        {
            this.game.x = 0;
            this.game.y = 0;
            this.game.scaleX = this.game.scaleY = 1;
            this.chrome.alpha = this.targetChromeAlpha;
            this.chrome.visible = this.chromeVisible;
            this.toggleButton.x = this.targetToggleX;
            this.toggleButton.y = this.targetToggleY;
        }

        private function animateLayout(e:Event):void
        {
            var progress:Number = Math.min(1, (getTimer() - this.animationStartedAt) / TRANSITION_MS);
            var inverse:Number = 1 - progress;
            var eased:Number = 1 - (inverse * inverse * inverse);
            this.chrome.alpha = this.startChromeAlpha + ((this.targetChromeAlpha - this.startChromeAlpha) * eased);
            this.toggleButton.x = this.startToggleX + ((this.targetToggleX - this.startToggleX) * eased);
            this.toggleButton.y = this.startToggleY + ((this.targetToggleY - this.startToggleY) * eased);

            if (progress >= 1)
            {
                this.applyTargetsImmediately();
                removeEventListener(Event.ENTER_FRAME, this.animateLayout);
            }
        }

        private function startSelectedBot():void
        {
            this.botRunner.start(this.botLibrary.selectedDocument);
            if (this.selectedNavigation == "Bot" || this.selectedNavigation == "Home")
            {
                this.renderSidebar(this.selectedNavigation);
            }
        }

        private function onBotLibraryChanged(event:Event):void
        {
            if (this.selectedNavigation == "Bot")
            {
                this.renderSidebar("Bot");
            }
        }

        private function onBotStateChanged(event:Event):void
        {
            if (this.selectedNavigation == "Bot" || this.selectedNavigation == "Home")
            {
                this.renderSidebar(this.selectedNavigation);
            }
        }

        private function botLibraryStatus(message:String):void
        {
            if (this.selectedNavigation != "Home")
            {
                this.showSection("Home");
            }
            while (this.sidebarContent.numChildren > 0)
            {
                this.sidebarContent.removeChildAt(0);
            }
            this.addText(this.sidebarContent, "GRIMOIRE", 3, 0, 274, 21, 11, PRIMARY_TEXT, true);
            this.drawInfoCard(43, "STATUS", message);
            this.sidebarContent.addChild(this.createAction("command_bot", "BOT MANAGER", 0, 134, 138, 36));
            this.sidebarContent.addChild(this.createAction("command_settings", "SETTINGS", 146, 134, 138, 36));
        }

        private function togglePreference(id:String):void
        {
            if (id == "pref_start_focused") this.preferences.startFocused = !this.preferences.startFocused;
            else if (id == "pref_hide_status") this.preferences.hideStatus = !this.preferences.hideStatus;
            else if (id == "pref_confirm_exit") this.preferences.confirmExit = !this.preferences.confirmExit;
            else if (id == "pref_reduced_motion") this.preferences.reducedMotion = !this.preferences.reducedMotion;
            else if (id == "pref_keep_awake") this.preferences.keepAwake = !this.preferences.keepAwake;
            else if (id == "pref_fps") this.preferences.frameRate = this.preferences.frameRate >= 60 ? 30 : 60;
            else if (id == "pref_quality")
            {
                this.preferences.quality = this.preferences.quality == "low" ? "medium"
                    : (this.preferences.quality == "medium" ? "high" : "low");
            }
            this.preferences.save();
            this.preferences.apply(stage);
            this.renderSidebar("Settings");
        }

        private function fullReset():void
        {
            if (getTimer() - this.resetArmedAt > 3000)
            {
                this.resetArmedAt = getTimer();
                this.selectedNavigation = "Settings";
                this.settingsPage = 2;
                this.renderSidebar("Settings");
                return;
            }
            this.botRunner.stop();
            this.preferences.reset();
            this.preferences.apply(stage);
            Root.username = null;
            Root.password = null;
            this.chromeVisible = !this.preferences.startFocused;
            this.calculateTargets();
            this.applyTargetsImmediately();
            this.showSection("Settings");
        }

        private function checkForUpdate():void
        {
            if (this.updateLoader != null)
            {
                this.botLibraryStatus("UPDATE CHECK ALREADY RUNNING");
                return;
            }
            this.updateLoader = new URLLoader();
            this.updateLoader.addEventListener(Event.COMPLETE, this.onUpdateLoaded);
            this.updateLoader.addEventListener(IOErrorEvent.IO_ERROR, this.onUpdateFailed);
            this.updateLoader.addEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onUpdateFailed);
            this.botLibraryStatus("CHECKING OFFICIAL GITHUB RELEASES...");
            try
            {
                this.updateLoader.load(new URLRequest(
                    "https://api.github.com/repos/UnknownSecurity/grimoire-exodus/releases?per_page=10"));
            }
            catch (error:Error)
            {
                this.finishUpdateCheck("UPDATE CHECK COULD NOT START");
            }
        }

        private function toggleLagKiller():void
        {
            this.lagKillerEnabled = !this.lagKillerEnabled;
            HostBridge.invoke("SetLagKiller", !this.lagKillerEnabled);
            this.botLibraryStatus(this.lagKillerEnabled ? "LAG KILLER ENABLED" : "LAG KILLER DISABLED");
        }

        private function toggleHidePlayers():void
        {
            this.playersHidden = !this.playersHidden;
            HostBridge.invoke("DestroyPlayers", this.playersHidden);
            this.botLibraryStatus(this.playersHidden ? "OTHER PLAYERS HIDDEN" : "OTHER PLAYERS VISIBLE");
        }

        private function onUpdateLoaded(event:Event):void
        {
            var bestVersion:Array = [0, 4, 4];
            var bestUrl:String;
            try
            {
                var releases:Array = JSON.parse(String(this.updateLoader.data)) as Array;
                for each (var release:Object in releases)
                {
                    if (release == null || release.draft === true || !(release.assets is Array))
                    {
                        continue;
                    }
                    for each (var asset:Object in release.assets)
                    {
                        var name:String = asset == null || asset.name == null ? "" : String(asset.name);
                        var match:Array = /Grimoire-Exodus-Android-AIR-(\d+)\.(\d+)\.(\d+)[^\/]*\.apk$/i.exec(name);
                        var url:String = asset == null || asset.browser_download_url == null
                            ? "" : String(asset.browser_download_url);
                        if (match != null && this.isNewerVersion(match, bestVersion)
                            && url.indexOf("https://github.com/UnknownSecurity/grimoire-exodus/releases/download/") == 0)
                        {
                            bestVersion = [int(match[1]), int(match[2]), int(match[3])];
                            bestUrl = url;
                        }
                    }
                }
            }
            catch (error:Error)
            {
                this.finishUpdateCheck("OFFICIAL RELEASE RESPONSE WAS INVALID");
                return;
            }
            this.availableUpdateUrl = bestUrl;
            this.finishUpdateCheck(bestUrl == null
                ? "YOU HAVE THE LATEST PUBLIC MOBILE BUILD"
                : "MOBILE UPDATE " + bestVersion.join(".") + " AVAILABLE — CHECK AGAIN TO OPEN");
        }

        private function onUpdateFailed(event:Event):void
        {
            this.finishUpdateCheck("UPDATE CHECK FAILED — TRY AGAIN LATER");
        }

        private function finishUpdateCheck(message:String):void
        {
            if (this.updateLoader != null)
            {
                this.updateLoader.removeEventListener(Event.COMPLETE, this.onUpdateLoaded);
                this.updateLoader.removeEventListener(IOErrorEvent.IO_ERROR, this.onUpdateFailed);
                this.updateLoader.removeEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onUpdateFailed);
                try
                {
                    this.updateLoader.close();
                }
                catch (error:Error)
                {
                }
                this.updateLoader = null;
            }
            this.botLibraryStatus(message);
        }

        private function isNewerVersion(candidate:Array, current:Array):Boolean
        {
            for (var i:int = 0; i < 3; i++)
            {
                var candidatePart:int = int(candidate[i]);
                var currentPart:int = int(current[i]);
                if (candidatePart != currentPart)
                {
                    return candidatePart > currentPart;
                }
            }
            return false;
        }

        private function ellipsize(value:String, maximum:int):String
        {
            if (value == null)
            {
                return "";
            }
            return value.length <= maximum ? value : value.substr(0, maximum - 1) + "…";
        }

        private function drawPanel(target:Shape, x:Number, y:Number, width:Number, height:Number, radius:Number, fill:uint, border:uint, borderAlpha:Number):void
        {
            target.graphics.beginFill(fill, 0.98);
            target.graphics.lineStyle(1, border, borderAlpha);
            target.graphics.drawRoundRect(x, y, width, height, radius, radius);
            target.graphics.endFill();
        }

        private function addText(parent:Sprite, value:String, x:Number, y:Number, width:Number, height:Number, size:Number, color:uint, bold:Boolean, font:String = "_sans", align:String = TextFormatAlign.LEFT):TextField
        {
            var field:TextField = new TextField();
            field.defaultTextFormat = new TextFormat(font, size, color, bold, null, null, null, null, align);
            field.x = x;
            field.y = y;
            field.width = width;
            field.height = height;
            field.text = value;
            field.selectable = false;
            field.mouseEnabled = false;
            parent.addChild(field);
            return field;
        }
    }
}
