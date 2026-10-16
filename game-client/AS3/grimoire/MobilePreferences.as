package grimoire
{
    import flash.desktop.NativeApplication;
    import flash.desktop.SystemIdleMode;
    import flash.display.Stage;
    import flash.display.StageQuality;
    import flash.net.SharedObject;
    import flash.ui.Keyboard;

    /**
     * Small, mobile-only preference store. Nothing in this class is shared
     * with the desktop configuration or Account Locker.
     */
    public final class MobilePreferences
    {
        private static const STORE_NAME:String = "grimoire_exodus_mobile";

        private var store:SharedObject;

        public var startFocused:Boolean;
        public var hideStatus:Boolean;
        public var confirmExit:Boolean;
        public var reducedMotion:Boolean;
        public var keepAwake:Boolean;
        public var frameRate:int;
        public var quality:String;
        public var bindings:Object;

        public function MobilePreferences()
        {
            this.store = SharedObject.getLocal(STORE_NAME);
            this.load();
        }

        public function load():void
        {
            var data:Object = this.store.data;
            this.startFocused = this.readBoolean(data.startFocused, false);
            this.hideStatus = this.readBoolean(data.hideStatus, false);
            this.confirmExit = this.readBoolean(data.confirmExit, true);
            this.reducedMotion = this.readBoolean(data.reducedMotion, false);
            this.keepAwake = this.readBoolean(data.keepAwake, true);
            this.frameRate = this.readInteger(data.frameRate, 30, 20, 60);
            this.quality = this.readQuality(data.quality);
            this.bindings = this.readBindings(data.bindings);
        }

        public function save():void
        {
            var data:Object = this.store.data;
            data.startFocused = this.startFocused;
            data.hideStatus = this.hideStatus;
            data.confirmExit = this.confirmExit;
            data.reducedMotion = this.reducedMotion;
            data.keepAwake = this.keepAwake;
            data.frameRate = this.frameRate;
            data.quality = this.quality;
            data.bindings = this.bindings;
            try
            {
                this.store.flush();
            }
            catch (error:Error)
            {
                // The current session still uses the selected values when the
                // platform declines persistent storage.
            }
        }

        public function reset():void
        {
            try
            {
                this.store.clear();
            }
            catch (error:Error)
            {
            }
            this.load();
        }

        public function apply(stage:Stage):void
        {
            if (stage == null)
            {
                return;
            }
            stage.frameRate = this.frameRate;
            stage.quality = this.quality;
            try
            {
                NativeApplication.nativeApplication.systemIdleMode = this.keepAwake
                    ? SystemIdleMode.KEEP_AWAKE : SystemIdleMode.NORMAL;
            }
            catch (error:Error)
            {
            }
        }

        public function bindingFor(action:String):uint
        {
            return this.bindings != null && this.bindings[action] != null
                ? uint(this.bindings[action])
                : 0;
        }

        public function cycleBinding(action:String):void
        {
            var choices:Array = [0, Keyboard.F5, Keyboard.F6, Keyboard.F7,
                Keyboard.F8, Keyboard.F9, Keyboard.F10, Keyboard.F11,
                Keyboard.F12];
            var current:uint = this.bindingFor(action);
            var index:int = choices.indexOf(current);
            this.bindings[action] = choices[(index + 1) % choices.length];
            this.save();
        }

        public static function keyLabel(value:uint):String
        {
            if (value == 0)
            {
                return "NONE";
            }
            if (value >= Keyboard.F1 && value <= Keyboard.F12)
            {
                return "F" + String((value - Keyboard.F1) + 1);
            }
            return String(value);
        }

        private function readBindings(value:*):Object
        {
            var result:Object = {
                startBot: Keyboard.F5,
                stopBot: Keyboard.F6,
                toggleChrome: Keyboard.F7,
                openSettings: Keyboard.F8,
                lagKiller: Keyboard.F9,
                hidePlayers: Keyboard.F10
            };
            if (value == null)
            {
                return result;
            }
            for (var key:String in result)
            {
                if (value[key] != null)
                {
                    result[key] = uint(value[key]);
                }
            }
            return result;
        }

        private function readBoolean(value:*, fallback:Boolean):Boolean
        {
            return value is Boolean ? Boolean(value) : fallback;
        }

        private function readInteger(value:*, fallback:int, minimum:int, maximum:int):int
        {
            var parsed:Number = Number(value);
            if (isNaN(parsed))
            {
                return fallback;
            }
            return Math.max(minimum, Math.min(maximum, int(parsed)));
        }

        private function readQuality(value:*):String
        {
            var normalized:String = value == null ? "" : String(value).toLowerCase();
            if (normalized == StageQuality.LOW || normalized == StageQuality.MEDIUM
                || normalized == StageQuality.HIGH)
            {
                return normalized;
            }
            return StageQuality.MEDIUM;
        }
    }
}
