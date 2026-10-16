package grimoire
{
    import flash.events.Event;

    /**
     * Synchronous event emitted when the game is hosted without the browser/
     * ActiveX ExternalInterface boundary. AIR listeners can consume these
     * events without changing the legacy desktop callback contract.
     */
    public final class HostBridgeEvent extends Event
    {
        public static const MESSAGE:String = "grimoireHostMessage";

        public var method:String;
        public var arguments:Array;

        public function HostBridgeEvent(method:String, arguments:Array)
        {
            super(MESSAGE, false, false);
            this.method = method;
            this.arguments = arguments == null ? [] : arguments.concat();
        }

        override public function clone():Event
        {
            return new HostBridgeEvent(this.method, this.arguments);
        }
    }
}
