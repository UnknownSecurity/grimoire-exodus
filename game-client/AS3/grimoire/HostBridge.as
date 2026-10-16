package grimoire
{
    import flash.events.EventDispatcher;
    import flash.events.IEventDispatcher;
    import flash.external.ExternalInterface;
    import flash.net.URLRequest;
    import flash.net.navigateToURL;

    /**
     * Keeps the legacy ExternalInterface ABI intact while allowing the same
     * game bridge to run inside a standalone AIR application.
     */
    public final class HostBridge
    {
        private static const callbacks:Object = {};
        private static const events:EventDispatcher = new EventDispatcher();

        public static function get hasExternalHost():Boolean
        {
            try
            {
                return ExternalInterface.available;
            }
            catch (error:Error)
            {
                return false;
            }
        }

        public static function addCallback(name:String, callback:Function):void
        {
            if (name == null || name.length == 0 || callback == null)
            {
                return;
            }

            callbacks[name] = callback;
            if (hasExternalHost)
            {
                ExternalInterface.addCallback(name, callback);
            }
        }

        /**
         * Invokes a registered game callback from an AIR-side controller.
         * This is also the single entry point a future ANE adapter will use.
         */
        public static function invoke(name:String, ... arguments):*
        {
            var callback:Function = callbacks[name] as Function;
            return callback == null ? null : callback.apply(null, arguments);
        }

        public static function call(name:String, ... arguments):*
        {
            if (hasExternalHost)
            {
                return ExternalInterface.call.apply(null, [name].concat(arguments));
            }

            // The desktop host can filter the server response. A standalone
            // AIR client has no filter, so preserving the original payload is
            // required for the AQW login flow to continue.
            if (name == "modifyServers")
            {
                return arguments.length > 0 ? arguments[0] : null;
            }

            if (name == "openWebsite")
            {
                if (arguments.length > 0 && arguments[0] != null)
                {
                    navigateToURL(new URLRequest(String(arguments[0])), "_blank");
                }
                return null;
            }

            events.dispatchEvent(new HostBridgeEvent(name, arguments));
            return null;
        }

        public static function addEventListener(type:String, listener:Function):void
        {
            events.addEventListener(type, listener, false, 0, true);
        }

        public static function removeEventListener(type:String, listener:Function):void
        {
            events.removeEventListener(type, listener);
        }
    }
}
