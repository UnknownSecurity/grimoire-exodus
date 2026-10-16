//grimoire.Root
package grimoire
{
	import flash.display.DisplayObject;
	import flash.display.MovieClip;
    import flash.display.Loader;
    import flash.display.Shape;
    import flash.system.LoaderContext;
    import flash.display.LoaderInfo;
    import flash.display.Stage;
    import flash.display.StageAlign;
    import flash.display.StageScaleMode;
    import flash.events.Event;
    import flash.events.MouseEvent;
    import flash.events.KeyboardEvent;
    import flash.events.ProgressEvent;
	import flash.events.IOErrorEvent;
	import flash.events.SecurityErrorEvent;
	import flash.events.TimerEvent;
	import flash.net.SharedObject;
    import flash.net.URLLoader;
    import flash.net.URLLoaderDataFormat;
    import flash.net.URLRequest;
    import flash.net.URLVariables;
    import flash.system.ApplicationDomain;
    import flash.system.Security;
    import flash.utils.getQualifiedClassName;
	import flash.utils.getDefinitionByName;
	import flash.utils.ByteArray;
	import flash.utils.Timer;
    import flash.ui.Keyboard;
    import flash.text.TextField;
    import flash.text.TextFormat;
    import grimoire.Externalizer;
    import grimoire.game.Aura;
    import grimoire.game.Bank;
	import grimoire.game.Client;
	import grimoire.game.DropStack;
    import grimoire.game.House;
    import grimoire.game.Inventory;
    import grimoire.game.Network;
    import grimoire.game.Player;
    import grimoire.game.Quest;
    import grimoire.game.Setting;
    import grimoire.game.Shop;
	import grimoire.game.Skill;
    import grimoire.game.TempInventory;
    import grimoire.game.World;
    import grimoire.tools.SFSEvent;
	  
    public class Root extends MovieClip 
    {
		private static var _handler:*;
		private static var objLogin:Object = new Object();
		private const sURL:String = "https://game.aq.com/game/";
		private const versionURL:String = (sURL + "api/data/gameversion");
        private var loginURL:* = (sURL + "api/login/now");
		private var external:Externalizer;
		private var loader:Loader;
		private var urlLoader:URLLoader;
		private var gameBytesLoader:URLLoader;
		private var brandingLoader:Loader;
		private var loadingBar:Shape;
		private var loadingLabel:TextField;
		// Kept dynamic so the desktop Flash Player bridge does not link AIR-only
		// filesystem and application classes. The Android build explicitly
		// includes MobileShell and constructs it only without ExternalInterface.
		private var mobileShell:*;
		private var lastReportedProgress:int = -1;
        private var doSignup:Boolean;
        private var isEU:Boolean;
        private var isWeb:Boolean;
        private var loaderVars:Object;
        private var sBG:String;
        private var sFile:*;
        private var stg:*;
		
		public static const trueString:String = '"True"';
        public static const falseString:String = '"False"';
		public static var username:String;
        public static var password:String;
        public static var Game:Object;
        public static var instance:Root;
		public static var isLoggedIn:Boolean = false;
		public static var gameDomain:ApplicationDomain;
		
		/// This function creates the instance and the loader.
        public function Root()
        {
			Root.instance = this;
            addEventListener(Event.ADDED_TO_STAGE, this.onAddedToStage);
        }
		
		///
		/// Functions below are the whole process of managing the loader.
		///
        private function onAddedToStage(e:Event):void
        {
            removeEventListener(Event.ADDED_TO_STAGE, this.onAddedToStage);
			stage.align = StageAlign.TOP_LEFT;
			if (HostBridge.hasExternalHost)
			{
				stage.scaleMode = StageScaleMode.SHOW_ALL;
			}
			else
			{
				// Let AIR own the physical-to-logical transform. AQW continues to
				// see its native 960x550 Stage coordinate plane, so Android touches
				// land on the same coordinates used by the legacy client.
				stage.scaleMode = StageScaleMode.EXACT_FIT;
				stage.frameRate = 30;
				stage.addEventListener(Event.RESIZE, this.onStageResize);
				stage.addEventListener(KeyboardEvent.KEY_DOWN, this.onMobileStageKeyboard, true, 1000);
			}
			this.showLoadingProgress(0, "Checking AQW version");
			if (HostBridge.hasExternalHost)
			{
				Security.allowDomain("game.aq.com", "*.aq.com", "*.artix.com");
			}
            this.urlLoader = new URLLoader();
            this.urlLoader.addEventListener(Event.COMPLETE, this.onDataComplete);
			this.urlLoader.addEventListener(IOErrorEvent.IO_ERROR, this.onLoaderError);
			this.urlLoader.addEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoaderError);
            this.urlLoader.load(new URLRequest(this.versionURL));
        }

        private function onDataComplete(e:Event):void
        {
            this.urlLoader.removeEventListener(Event.COMPLETE, this.onDataComplete);
			this.urlLoader.removeEventListener(IOErrorEvent.IO_ERROR, this.onLoaderError);
			this.urlLoader.removeEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoaderError);
			var loader:Object = JSON.parse(e.target.data);
            this.sFile = ((loader.sFile + "?ver=") + loader.sVersion);
            this.sBG = loader.sBG;
            this.loaderVars = loader;
            this.loadGame();
        }

        private function loadGame():void
        {
			var request:URLRequest = new URLRequest(((this.sURL + "gamefiles/") + this.sFile));
			if (!HostBridge.hasExternalHost)
			{
				// AIR application content cannot import a remote SWF directly into
				// its security domain. Downloading the bytes first and explicitly
				// allowing code import is the supported AIR path.
				this.gameBytesLoader = new URLLoader();
				this.gameBytesLoader.dataFormat = URLLoaderDataFormat.BINARY;
				this.gameBytesLoader.addEventListener(ProgressEvent.PROGRESS, this.onProgress);
				this.gameBytesLoader.addEventListener(Event.COMPLETE, this.onGameBytesComplete);
				this.gameBytesLoader.addEventListener(IOErrorEvent.IO_ERROR, this.onLoaderError);
				this.gameBytesLoader.addEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoaderError);
				this.gameBytesLoader.load(request);
				return;
			}

            this.loader = new Loader();
            this.loader.contentLoaderInfo.addEventListener(ProgressEvent.PROGRESS, this.onProgress);
            this.loader.contentLoaderInfo.addEventListener(Event.COMPLETE, this.onComplete);
			this.loader.contentLoaderInfo.addEventListener(IOErrorEvent.IO_ERROR, this.onLoaderError);
			this.loader.contentLoaderInfo.addEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoaderError);
			this.loader.load(request);
        }

        private function onProgress(e:ProgressEvent):void
        {
			var percentage:int = e.bytesTotal > 0 ? Math.round((Number(e.bytesLoaded / e.bytesTotal) * 100)) : 0;
			this.showLoadingProgress(percentage, "Loading AQW");
            HostBridge.call("progress", percentage);
        }

		private function onGameBytesComplete(e:Event):void
		{
			this.gameBytesLoader.removeEventListener(ProgressEvent.PROGRESS, this.onProgress);
			this.gameBytesLoader.removeEventListener(Event.COMPLETE, this.onGameBytesComplete);
			this.gameBytesLoader.removeEventListener(IOErrorEvent.IO_ERROR, this.onLoaderError);
			this.gameBytesLoader.removeEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoaderError);

			var context:LoaderContext = new LoaderContext(false, ApplicationDomain.currentDomain);
			context.allowCodeImport = true;
			this.loader = new Loader();
			this.loader.contentLoaderInfo.addEventListener(Event.COMPLETE, this.onComplete);
			this.loader.contentLoaderInfo.addEventListener(IOErrorEvent.IO_ERROR, this.onLoaderError);
			this.loader.contentLoaderInfo.addEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoaderError);
			this.loader.loadBytes(ByteArray(e.currentTarget.data), context);
		}

        private function onComplete(e:Event):void
        {
            var info:*;
			var gameDefinition:Object;
			var gameFilePath:String;
            this.loader.contentLoaderInfo.removeEventListener(ProgressEvent.PROGRESS, this.onProgress);
            this.loader.contentLoaderInfo.removeEventListener(Event.COMPLETE, this.onComplete);
			this.loader.contentLoaderInfo.removeEventListener(IOErrorEvent.IO_ERROR, this.onLoaderError);
			this.loader.contentLoaderInfo.removeEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onLoaderError);
            this.stg = stage;
			Game = e.currentTarget.content;
			gameDomain = LoaderInfo(e.target).applicationDomain;
			for (info in this.loaderInfo.parameters)
            {
				Game.params[info] = this.loaderInfo.parameters[info];
            };
			Game.params.vars = this.loaderVars;
            Game.params.sURL = this.sURL;
            Game.params.sTitle = '<font color="#9370DB">Grimoire Exodus</font>';
			Game.params.sBG = this.sBG;
            Game.params.isWeb = this.isWeb;
            Game.params.isEU = this.isEU;
            Game.params.doSignup = this.doSignup;
			Game.params.test = false;
			Game.params.loginURL = this.loginURL;
			if (!HostBridge.hasExternalHost)
			{
				// loadBytes gives the remote AQW client an app:/[[DYNAMIC]] URL.
				// AQW derives its post-login Assets_*.swf and API locations from
				// that URL unless sFilePath and its static paths are restored.
				gameFilePath = this.sURL + "gamefiles/";
				Game.params.sFilePath = gameFilePath;
				Game.sFilePath = gameFilePath;
				gameDefinition = gameDomain.getDefinition("Game");
				gameDefinition.serverFilePath = gameFilePath;
				gameDefinition.serverGamePath = this.sURL;
				trace("[Grimoire AIR] AQW asset origin restored: " + gameFilePath);
			}
			if (HostBridge.hasExternalHost)
			{
				if (this.parent != null)
				{
					this.parent.removeChild(this);
				}
			}
			else
			{
				this.removeLoadingSurface();
			}
			// AQW relies on the display hierarchy it uses in the desktop client.
			// Keep the live game directly on Stage and place the AIR chrome in
			// this Root overlay instead of reparenting or masking the game.
			this.stg.addChildAt(Game, 0);
			if (!HostBridge.hasExternalHost)
			{
				var mobileShellClass:Class = getDefinitionByName("grimoire.MobileShell") as Class;
				this.mobileShell = new mobileShellClass(DisplayObject(Game));
				addChild(this.mobileShell);
				this.mobileShell.layout(stage.stageWidth, stage.stageHeight);
			}
			Game.addEventListener(MouseEvent.CLICK, this.onGameClick);
			Game.addEventListener(KeyboardEvent.KEY_DOWN, this.onGameKeyboard);
			Game.loginLoader.addEventListener(Event.COMPLETE, this.onLoginComplete);
            Game.sfc.addEventListener(SFSEvent.onConnectionLost, this.onDisconnect);
			Game.sfc.addEventListener(SFSEvent.onExtensionResponse, this.onExtensionResponse);
			Game.sfc.addEventListener(SFSEvent.onDebugMessage, this.onPacketDebug);
			// AQW R0048 now creates SmartFoxClient with debug explicitly disabled.
			// Packet callbacks are only dispatched while this flag is enabled.
			Game.sfc.debug = true;
			this.external = new Externalizer();
			this.external.init(this);
			trace("[Grimoire AIR] AQW client initialized");
        }
		
        private function onDisconnect(e:*):void
        {
			if (this.mobileShell != null)
			{
				this.mobileShell.setConnected(false);
				this.mobileShell.resetPlayerProfile();
			}
            HostBridge.call("disconnect");
        }

        private function onLoginComplete(e:Event):void
        {
            trace("Login Complete");
			var original:String = String(e.target.data);
            objLogin = JSON.parse(original);
			var modified:* = HostBridge.call("modifyServers", original);
			e.target.data = modified == null || String(modified).length == 0 ? original : String(modified);
			if (Game.mcCharSelect)
            {
                Game.mcCharSelect.Game.objLogin = objLogin;
            };
        }

		private function onGameClick(e:MouseEvent):void
		{
			switch (e.target.name)
			{
				case "btnLogin":
					if (!Game.mcCharSelect)
					{
						username = Game.mcLogin.ni.text;
						password = Game.mcLogin.pi.text;
					}
					else
					{
						Game.mcCharSelect.btnLogin.removeEventListener(MouseEvent.CLICK, Game.mcCharSelect.onBtnLogin);
						onCharBtnClick("Login");
					}
					return;
				case "btnServer":
					if (Game.mcCharSelect)
					{
						Game.mcCharSelect.btnServer.removeEventListener(MouseEvent.CLICK, Game.mcCharSelect.onBtnServer);
						onCharBtnClick("Server");
					}
					return;
				case "charPage":
					HostBridge.call("openWebsite", ("https://www.aq.com/aw-character.asp?id=" + gameDomain.getDefinition("Game").loginInfo.strUsername));
					return;
				case "txtClassName":
				case "txtWeapon":
				case "txtArmor":
				case "txtHelm":
				case "txtCape":
				case "txtPet":
				case "txtMisc":
					if (e.target.text != "None")
					{
						HostBridge.call("openWebsite", ("http://aqwwiki.wikidot.com/" + e.target.text));
					};
					return;
				case "btCharPage":
					HostBridge.call("openWebsite", ("https://account.aq.com/CharPage?id=" + e.target.parent.txtUserName.text));
					return;
				case "hit":
					if (getQualifiedClassName(e.target.parent) != "LPFElementScrollBar" && e.target.parent.ti && e.target.parent.ti.text == "Wiki Monster")
					{
						HostBridge.call("openWebsite", ("http://aqwwiki.wikidot.com/" + Game.ui.mcPortraitTarget.strName.text));
					}
					return;
				case "btnWiki":
					var className:String = getQualifiedClassName(e.target.parent);
					if (className.indexOf("LPFFrameItemPreview") > -1)
					{
						HostBridge.call("openWebsite", ("http://aqwwiki.wikidot.com/" + e.target.parent.tInfo.getLineText(0)));
					}
					else if (className.indexOf("LPFFrameHousePreview") > -1)
					{
						HostBridge.call("openWebsite", ("http://aqwwiki.wikidot.com/" + Game.ui.mcPopup.getChildByName("mcInventory").previewPanel.frames[3].mc.tInfo.getLineText(0)));
					}
					else if (className.indexOf("mcQFrame") > -1)
					{
						HostBridge.call("openWebsite", ("http://aqwwiki.wikidot.com/search:site/a/p/q/" + Game.getInstanceFromModalStack("QFrameMC").qData.sName));
					}
					return;
			}
		}
		
		private function onGameKeyboard(e:KeyboardEvent):void
		{
			switch (e.target.name)
			{
				case "ni":
				case "pi":
					if (e.keyCode == Keyboard.ENTER)
					{
						username = Game.mcLogin.ni.text;
						password = Game.mcLogin.pi.text;
					}
					break;
			}
		}
		
		private function onCharBtnClick(btn:String):void
		{
			if (btn == "Login")
			{
				var server:String = Game.mcCharSelect.mngr.displayAvts[Game.mcCharSelect.pos].server;
			};
			username = Game.mcCharSelect.mngr.displayAvts[Game.mcCharSelect.pos].loginInfo.strUsername;
			password = Game.mcCharSelect.mngr.displayAvts[Game.mcCharSelect.pos].loginInfo.strPassword;
            var loginData:* = Game.mcCharSelect.mngr.displayAvts[Game.mcCharSelect.pos].loginInfo;
            if (loginData.bAsk)
            {
                Game.mcCharSelect.utl.close(1);
                Game.mcCharSelect.passwordui.pos = Game.mcCharSelect.pos;
                Game.mcCharSelect.passwordui.bCharOpts = false;
                Game.mcCharSelect.passwordui.visible = true;
            }
            else
            {
				Game.removeAllChildren();
				Game.gotoAndPlay("Login");
				Game.login(username, password);
				if (btn == "Login")
				{
					waitFor(Network.areServersLoaded, Network.connect, [server])
				};
			};
		}
		
		private function filter(e:String):String
        {
            if (e.indexOf("[Sending - STR]: ") > -1)
            {
                e = e.replace("[Sending - STR]: ", "");
            };
			if (e.indexOf("[Sending - JSON]: ") > -1)
			{
				e = e.replace("[Sending - JSON]: ", "");
			};
			if (e.indexOf("[Sending - XML]: ") > -1)
			{
				e = e.replace("[Sending - XML]: ", "");
			};
            if (e.indexOf("[ RECEIVED ]: ") > -1)
            {
                e = e.replace("[ RECEIVED ]: ", "");
            };
			if (e.indexOf("[Sending]: ") > -1)
            {
                e = e.replace("[Sending]: ", "");
            };
			if (e.indexOf(", (len: ") > -1)
			{
				var index:int = e.indexOf(", (len: ");
				e = e.slice(0, index);
			};
            return (e);
        }

		private function onPacketDebug(e:*):void
		{
			if (e == null || e.params == null || e.params.message == null)
			{
				return;
			}

			var raw:String = String(e.params.message);
			var packet:String = this.filter(raw);
			if (packet.length == 0 || packet.indexOf("%xt%zm%hi%") > -1 || packet.indexOf("Please slow down. Last action was too soon!") > -1)
			{
				return;
			}

			var outgoing:Boolean = raw.indexOf("[Sending") > -1;
			HostBridge.call(outgoing ? "packetFromClient" : "packetFromServer", packet);
		}
		
        public function onXtPacketResponse(e:*):void
        {
            if ((e.params.message.indexOf("%xt%zm%") > -1) && (e.params.message.indexOf("%hi%") < 0))
            {
				HostBridge.call("xtPacket", this.filter(e.params.message));
            };
        }
		
		public function onPacketResponse(e:*):void
        {
			if (e.params.message.indexOf("%xt%zm%hi%") > -1)
			{
				return;
			}
			if ((e.params.message.indexOf("%xt%hi%") > -1) || (e.params.message.indexOf("Please slow down. Last action was too soon!") > -1))
			{
				grimoire.game.Client.shouldPing = true;
				return;
			}
			else if (e.params.message.indexOf("%xt%loginResponse%") > -1)
            {
				grimoire.game.Bank.loaded = false;
			}
			else if (e.params.message.indexOf('"cmd":"loadInventoryBig"') > -1)
			{
				isLoggedIn = true;
				grimoire.game.Client.shouldPing = true;
				if (this.mobileShell != null)
				{
					this.refreshMobileProfile();
					this.mobileShell.setConnected(true);
				}
			}
			else if (e.params.message.indexOf("<msg t='sys'><body action='logout' r='0'></body></msg>") > -1)
			{
				isLoggedIn = false;
				grimoire.game.Client.shouldPing = false;
				if (this.mobileShell != null)
				{
					this.mobileShell.setConnected(false);
					this.mobileShell.resetPlayerProfile();
				}
			}
			HostBridge.call("packet", this.filter(e.params.message));
        }

		private function refreshMobileProfile():void
		{
			if (this.mobileShell == null)
			{
				return;
			}

			var authenticatedName:String = "";
			var level:* = null;
			var characterClass:String = "";
			var avatarData:*;
			var gameDefinition:Object;

			try
			{
				if (Game.world != null && Game.world.myAvatar != null && Game.world.myAvatar.objData != null)
				{
					avatarData = Game.world.myAvatar.objData;
					if (avatarData.strUsername != null)
					{
						authenticatedName = String(avatarData.strUsername);
					}
					if (avatarData.intLevel != null)
					{
						level = avatarData.intLevel;
					}
					if (avatarData.strClassName != null)
					{
						characterClass = String(avatarData.strClassName);
					}
				}
			}
			catch (avatarError:Error)
			{
				// The inventory packet can precede avatar display construction.
			}

			if (authenticatedName.replace(/^\s+|\s+$/g, "").length == 0)
			{
				try
				{
					gameDefinition = gameDomain.getDefinition("Game");
					if (gameDefinition.loginInfo != null && gameDefinition.loginInfo.strUsername != null)
					{
						authenticatedName = String(gameDefinition.loginInfo.strUsername);
					}
				}
				catch (loginInfoError:Error)
				{
				}
			}

			this.mobileShell.setPlayerProfile(authenticatedName, level, characterClass);
		}
		
		public function onExtensionResponse(packet:*):void
        {
            if (packet.params.type == "str")
            {
                if (packet.params.dataObj[0] == "hi")
                {
					Client.latency = Client.latency - new Date().getTime();
                    Client.latency = Math.abs(Client.latency);
					HostBridge.call("ping", Client.latency);
					if (Game.ui.mcPopup.getChildByName("mcO") != null)
					{
						Game.ui.mcPopup.getChildByName("mcO").latency = Client.latency;
						Game.ui.mcPopup.getChildByName("mcO").updateLatency();
					}
                }
            }
        }
		///
		/// Functions above are the whole process of managing the loader.
		///
		
		// This function calls a delay action of milliseconds, waiting for the corresponding function to return True boolean, and then executes the second corresponding function.
		public static function waitFor(bool:Function, func:Function, funcParams:Array, delay:int=300, boolString:String='"True"'):void
		{
			var f:Function;
			var timer:Timer = new Timer(delay);
			timer.addEventListener(TimerEvent.TIMER, f = function():void
			{
				if (bool() == boolString)
				{
					timer.removeEventListener(TimerEvent.TIMER, f);
					timer.stop();
					func.apply(null, funcParams);
				}
			});
			timer.start();
		}

		private function showLoadingProgress(percentage:int, message:String):void
		{
			if (HostBridge.hasExternalHost || stage == null)
			{
				return;
			}

			if (this.loadingBar == null)
			{
				this.brandingLoader = new Loader();
				this.brandingLoader.contentLoaderInfo.addEventListener(Event.COMPLETE, this.onBrandingLoaded);
				this.brandingLoader.contentLoaderInfo.addEventListener(IOErrorEvent.IO_ERROR, this.onBrandingError);
				this.brandingLoader.load(new URLRequest("app:/assets/grimoire_banner.png"));

				this.loadingBar = new Shape();
				addChild(this.loadingBar);

				this.loadingLabel = new TextField();
				this.loadingLabel.defaultTextFormat = new TextFormat("_sans", 22, 0xF7F5FF, true, null, null, null, null, "center");
				this.loadingLabel.width = 600;
				this.loadingLabel.height = 42;
				this.loadingLabel.selectable = false;
				addChild(this.loadingLabel);
				this.layoutLoadingSurface();
			}

			percentage = Math.max(0, Math.min(100, percentage));
			if (percentage != this.lastReportedProgress)
			{
				this.lastReportedProgress = percentage;
				trace("[Grimoire AIR] " + message + " " + percentage + "%");
			}
			this.loadingLabel.text = message + "  " + percentage + "%";
			this.loadingBar.graphics.clear();
			this.loadingBar.graphics.beginFill(0x191C2C);
			this.loadingBar.graphics.drawRoundRect(0, 0, 600, 18, 9, 9);
			this.loadingBar.graphics.endFill();
			this.loadingBar.graphics.beginFill(0x8B5CF6);
			this.loadingBar.graphics.drawRoundRect(0, 0, 6 * percentage, 18, 9, 9);
			this.loadingBar.graphics.endFill();
		}

		private function onBrandingLoaded(e:Event):void
		{
			this.brandingLoader.contentLoaderInfo.removeEventListener(Event.COMPLETE, this.onBrandingLoaded);
			this.brandingLoader.contentLoaderInfo.removeEventListener(IOErrorEvent.IO_ERROR, this.onBrandingError);
			this.brandingLoader.alpha = 0.36;
			addChildAt(this.brandingLoader, 0);
			this.layoutLoadingSurface();
		}

		private function onBrandingError(e:IOErrorEvent):void
		{
			this.brandingLoader.contentLoaderInfo.removeEventListener(Event.COMPLETE, this.onBrandingLoaded);
			this.brandingLoader.contentLoaderInfo.removeEventListener(IOErrorEvent.IO_ERROR, this.onBrandingError);
		}

		private function removeLoadingSurface():void
		{
			if (this.brandingLoader != null)
			{
				this.brandingLoader.contentLoaderInfo.removeEventListener(Event.COMPLETE, this.onBrandingLoaded);
				this.brandingLoader.contentLoaderInfo.removeEventListener(IOErrorEvent.IO_ERROR, this.onBrandingError);
				try
				{
					this.brandingLoader.close();
				}
				catch (error:Error)
				{
				}
			}
			while (numChildren > 0)
			{
				removeChildAt(numChildren - 1);
			}
			this.graphics.clear();
			this.brandingLoader = null;
			this.loadingBar = null;
			this.loadingLabel = null;
		}

		private function onStageResize(e:Event):void
		{
			if (HostBridge.hasExternalHost || stage == null)
			{
				return;
			}

			this.layoutLoadingSurface();
			if (this.mobileShell != null)
			{
				this.mobileShell.layout(stage.stageWidth, stage.stageHeight);
			}
		}

		private function onMobileStageKeyboard(e:KeyboardEvent):void
		{
			if (this.mobileShell == null)
			{
				return;
			}
			if (e.keyCode == Keyboard.BACK)
			{
				if (this.mobileShell.handleBack())
				{
					e.preventDefault();
					e.stopImmediatePropagation();
				}
				return;
			}
			this.mobileShell.handleKey(e.keyCode);
		}

		private function layoutLoadingSurface():void
		{
			if (stage == null || HostBridge.hasExternalHost)
			{
				return;
			}

			var stageWidth:Number = Math.max(1, stage.stageWidth);
			var stageHeight:Number = Math.max(1, stage.stageHeight);
			this.graphics.clear();
			this.graphics.beginFill(0x080A12);
			this.graphics.drawRect(0, 0, stageWidth, stageHeight);
			this.graphics.endFill();

			if (this.brandingLoader != null && this.brandingLoader.parent != null)
			{
				this.brandingLoader.x = 0;
				this.brandingLoader.y = 0;
				this.brandingLoader.width = stageWidth;
				this.brandingLoader.height = stageHeight;
			}

			var contentScale:Number = Math.min(stageWidth / 960, stageHeight / 550);
			if (this.loadingBar != null)
			{
				this.loadingBar.scaleX = contentScale;
				this.loadingBar.scaleY = contentScale;
				this.loadingBar.x = (stageWidth - (600 * contentScale)) / 2;
				this.loadingBar.y = (stageHeight / 2) + (35 * contentScale);
			}
			if (this.loadingLabel != null)
			{
				this.loadingLabel.scaleX = contentScale;
				this.loadingLabel.scaleY = contentScale;
				this.loadingLabel.x = (stageWidth - (600 * contentScale)) / 2;
				this.loadingLabel.y = (stageHeight / 2) - (25 * contentScale);
			}
		}

		private function onLoaderError(e:Event):void
		{
			trace("[Grimoire AIR] Load failure: " + e.toString());
			this.showLoadingProgress(0, "Unable to load AQW");
			HostBridge.call("loadError", e.toString());
		}
		
		// This function returns the username of the player.
        public static function Username():String
        {
            return (JSON.stringify(username));
        }

		// This function returns the password of the player.
        public static function Password():String
        {
            return (JSON.stringify(password));
        }
		
		// This function sets the game's FPS to a corresponding number.
        public static function setFPS(rate:int):void
        {
			Game.addUpdate("Frames Per Second (FPS) cap has been changed from " + instance.stg.frameRate + " FPS to " + rate + " FPS.");
            instance.stg.frameRate = rate;
        }

		// This function sets the login screen's title to a corresponding string.
        public static function setTitle(title:String):void
        {
            Game.mcLogin.mcLogo.txtTitle.htmlText = ('<font color="#CC1F41">Release:</font>: ' + title);
			Game.params.sTitle = title;
        }
		
		// This function executes the game's Notify function.
		public static function gameMessage(text:String):void
		{
			Game.MsgBox.notify(text);
		}			
		
		// This function executes the game's AddUpdate function.
		public static function gamePopup(text:String, red:Boolean=false):void
		{
			Game.addUpdate(text, red);
		}
		
		// This function sends a packet to the game client.
        public static function sendClientPacket(packet:String, type:String):void
        {
            var cls:Class;
            if (_handler == null)
            {
                cls = Class(gameDomain.getDefinition("it.gotoandplay.smartfoxserver.handlers.ExtHandler"));
                _handler = new cls(Game.sfc);
            };
            if (type == "xml") 
			{ 
				xmlReceived(packet); 
			}
            else if (type == "json") 
			{ 
				jsonReceived(packet); 
			}
			else if (type == "str") 
			{ 
				strReceived(packet); 
			}
        }

		// This function sends a packet in XML-type.
        public static function xmlReceived(packet:String):void
        {
            _handler.handleMessage(new XML(packet), "xml");
        }

		// This function sends a packet in JSON-type.
        public static function jsonReceived(packet:String):void
        {
            _handler.handleMessage(JSON.parse(packet)["b"], "json");
        }

		// This function sends a packet in XT-type.
        public static function strReceived(packet:String):void
        {
            var array:Array = packet.substr(1, (packet.length - 2)).split("%");
            _handler.handleMessage(array.splice(1, (array.length - 1)), "str");
        }
		
		// This function calls a game function.
        public static function callGameFunction(path:String, ... args):String
        {
            var parts:Array = path.split(".");
            var funcName:String = parts.pop();
            var obj:* = _getObjectA(Root.Game, parts);
            var func:Function = (obj[funcName] as Function);
            return (JSON.stringify(func.apply(null, args)));
        }		
		
		///
		/// Functions below are the whole process of managing the game objects.
		///
		public static function selectArrayObjects(path:String, selector:String):String
        {
            var obj:* = _getObjectS(Root.Game, path);
            if ((!(obj is Array)))
            {
                return ("");
            };
            var array:Array = (obj as Array);
            var narray:Array = new Array();
            var j:int;
            while (j < array.length)
            {
                narray.push(_getObjectS(array[j], selector));
                j++;
            };
            return (JSON.stringify(narray));
        }

        public static function _getObjectA(root:*, parts:Array):*
        {
            var i:int;
            var obj:* = root;
            i = 0;
            while (i < parts.length)
            {
                obj = obj[parts[i]];
                i++;
            };
            return (obj);
        }		
		
		public static function _getObjectS(root:*, path:String):*
        {
            return (_getObjectA(root, path.split(".")));
        }
		
		public static function getGameObject(path:String):String
        {
            var obj:* = _getObjectS(Root.Game, path);
            return (JSON.stringify(obj));
        }
		
        public static function setGameObject(path:String, value:*):void
        {
            var parts:Array = path.split(".");
            var varName:String = parts.pop();
            var obj:* = _getObjectA(Root.Game, parts);
            obj[varName] = value;
        }
		
        public static function isNull(path:String):String
        {
            try
            {
                return ((_getObjectS(Root.Game, path) == null).toString());
            }
            catch(ex:Error) {};
            return ("true");
        }		
		///
		/// Functions above are the whole process of managing the game objects.
		///			
		
    }
}//package grimoire

