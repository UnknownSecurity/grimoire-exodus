package grimoire
{
    import flash.events.Event;
    import flash.events.EventDispatcher;
    import flash.events.TimerEvent;
    import flash.utils.Timer;
    import grimoire.game.Network;
    import grimoire.game.DropStack;
    import grimoire.game.Inventory;
    import grimoire.game.Player;
    import grimoire.game.Quest;
    import grimoire.game.Setting;
    import grimoire.game.Shop;
    import grimoire.game.Skill;
    import grimoire.game.TempInventory;
    import grimoire.game.World;

    /**
     * Cooperative mobile runner for the safe foundational GBot commands. It
     * stops on unknown commands instead of resolving arbitrary Newtonsoft
     * $type values, executing embedded code, or continuing in an invalid state.
     */
    public final class MobileBotRunner extends EventDispatcher
    {
        public static const STATE_CHANGED:String = "mobileBotStateChanged";

        private var timer:Timer = new Timer(1, 1);
        private var commands:Array = [];
        private var index:int;
        private var running:Boolean;
        private var paused:Boolean;
        private var waitingForKill:Boolean;
        private var killEngaged:Boolean;
        private var killTarget:String;
        private var killForItem:String;
        private var killForQuantity:int;
        private var killForTemporary:Boolean;
        private var labels:Object = {};
        private var statusText:String = "BOT IDLE";
        private var botDelay:int = 500;

        public function MobileBotRunner()
        {
            this.timer.addEventListener(TimerEvent.TIMER_COMPLETE, this.onTimer);
        }

        public function get isRunning():Boolean
        {
            return this.running;
        }

        public function get isPaused():Boolean
        {
            return this.paused;
        }

        public function get status():String
        {
            return this.statusText;
        }

        public function start(document:Object):void
        {
            if (!Root.isLoggedIn)
            {
                this.setStatus("SIGN IN BEFORE STARTING A BOT");
                return;
            }
            var list:Array = document == null ? null : document.mobileCommands as Array;
            if (list == null || list.length == 0)
            {
                this.setStatus("SELECT A BOT WITH COMMANDS");
                return;
            }
            this.stopTimer();
            this.commands = list.concat();
            this.botDelay = this.numberValue(document.BotDelay, 0, 60000);
            this.labels = {};
            for (var labelIndex:int = 0; labelIndex < this.commands.length; labelIndex++)
            {
                var labelCommand:Object = this.commands[labelIndex];
                if (this.commandName(labelCommand) == "CmdLabel")
                {
                    this.labels[this.textValue(labelCommand.Name).toLowerCase()] = labelIndex;
                }
            }
            this.index = 0;
            this.running = true;
            this.paused = false;
            this.waitingForKill = false;
            this.killEngaged = false;
            this.killTarget = null;
            this.killForItem = null;
            this.setStatus("BOT RUNNING");
            this.schedule(1);
        }

        public function stop(message:String = "BOT STOPPED"):void
        {
            this.stopTimer();
            this.running = false;
            this.paused = false;
            this.waitingForKill = false;
            this.killEngaged = false;
            this.killTarget = null;
            this.killForItem = null;
            try
            {
                Player.cancelTarget();
            }
            catch (error:Error)
            {
            }
            this.setStatus(message);
        }

        public function togglePause():void
        {
            if (!this.running)
            {
                this.setStatus("BOT IS NOT RUNNING");
                return;
            }
            this.paused = !this.paused;
            this.stopTimer();
            this.setStatus(this.paused ? "BOT PAUSED" : "BOT RUNNING");
            if (!this.paused)
            {
                this.schedule(1);
            }
        }

        private function onTimer(event:TimerEvent):void
        {
            if (!this.running || this.paused)
            {
                return;
            }
            if (!Root.isLoggedIn)
            {
                this.stop("BOT STOPPED: CLIENT DISCONNECTED");
                return;
            }
            if (this.waitingForKill)
            {
                this.continueKill();
                return;
            }
            if (this.index >= this.commands.length)
            {
                this.stop("BOT COMPLETED");
                return;
            }

            var command:Object = this.commands[this.index++];
            var kind:String = this.commandName(command);
            var delay:int = this.botDelay;
            try
            {
                switch (kind)
                {
                    case "CmdDelay":
                        delay = this.numberValue(command.Delay, 0, 600000);
                        break;
                    case "CmdBotDelay":
                    case "CmdBotDelay2":
                        this.botDelay = this.numberValue(command.Delay, 0, 60000);
                        delay = this.botDelay;
                        break;
                    case "CmdJoin":
                        Player.join(this.textValue(command.Map),
                            this.textValue(command.Cell, "Enter"),
                            this.textValue(command.Pad, "Spawn"));
                        delay = 1800;
                        break;
                    case "CmdJoin2":
                        var map:String = this.textValue(command.Map);
                        var room:String = this.textValue(command.Room);
                        Player.join(map + (room.length > 0 ? "-" + room : ""),
                            this.textValue(command.Cell, "Enter"),
                            this.textValue(command.Pad, "Spawn"));
                        delay = 1800;
                        break;
                    case "CmdMoveToCell":
                    case "CmdJump":
                        Player.jump(this.textValue(command.Cell, "Enter"),
                            this.textValue(command.Pad, "Spawn"));
                        delay = 350;
                        break;
                    case "CmdWalk":
                        Player.walkToPoint(this.textValue(command.X), this.textValue(command.Y));
                        delay = 200;
                        break;
                    case "CmdAttack":
                        Player.attackMonster(this.textValue(command.Monster, "*"));
                        delay = 250;
                        break;
                    case "CmdKill":
                        this.killTarget = this.textValue(command.Monster, "*");
                        this.killForItem = null;
                        this.killEngaged = false;
                        this.waitingForKill = true;
                        this.continueKill();
                        return;
                    case "CmdKillFor":
                        this.killTarget = this.textValue(command.Monster, "*");
                        this.killForItem = this.textValue(command.ItemName);
                        this.killForQuantity = this.numberValue(command.Quantity, 1, 999999);
                        this.killForTemporary = String(command.ItemType).toLowerCase().indexOf("temp") >= 0
                            || int(command.ItemType) != 0;
                        if (this.inventoryHas(this.killForItem, this.killForQuantity, this.killForTemporary))
                        {
                            this.killForItem = null;
                            break;
                        }
                        this.killEngaged = false;
                        this.waitingForKill = true;
                        this.continueKill();
                        return;
                    case "CmdUseSkill":
                    case "CmdUseSkill2":
                        Skill.skillAction(this.textValue(command.Index, "1"));
                        delay = 250;
                        break;
                    case "CmdRest":
                        Player.rest();
                        delay = command.Full === true ? 1400 : 350;
                        break;
                    case "CmdAcceptQuest":
                    case "CmdAcceptQuest2":
                        Quest.acceptQuest(this.questId(command));
                        delay = 750;
                        break;
                    case "CmdCompleteQuest":
                    case "CmdCompleteQuest2":
                        Quest.completeQuest(this.questId(command),
                            this.textValue(command.ItemID, "-1"));
                        delay = 750;
                        break;
                    case "CmdSetSpawn":
                        World.setSpawnPoint();
                        break;
                    case "CmdGetDrop":
                        DropStack.getDrop(this.textValue(command.ItemName));
                        delay = 350;
                        break;
                    case "CmdEquip":
                        var itemId:String = this.inventoryItemId(this.textValue(command.ItemName));
                        if (itemId.length > 0)
                        {
                            Player.equipItem(itemId);
                        }
                        delay = 500;
                        break;
                    case "CmdBuy":
                    case "CmdBuy2":
                        Shop.buyItem(this.textValue(command.ItemName));
                        delay = 500;
                        break;
                    case "CmdGotoPlayer":
                        Player.goTo(this.textValue(command.PlayerName));
                        delay = 1500;
                        break;
                    case "CmdCancelTarget":
                        Player.cancelTarget();
                        break;
                    case "CmdCancelAutoAttack":
                        Player.cancelAutoAttack();
                        break;
                    case "CmdSetFPS":
                        Root.setFPS(this.numberValue(command.FPS, 20, 60));
                        break;
                    case "CmdToggleProvoke":
                        if (int(command.Type) != 0)
                        {
                            Setting.provokeMonsters();
                        }
                        break;
                    case "CmdClientMessage":
                        Root.gameMessage(this.textValue(command.Messages));
                        break;
                    case "CmdLogout":
                        Network.logout();
                        delay = 1000;
                        break;
                    case "CmdRestart":
                        this.index = 0;
                        break;
                    case "CmdGotoLabel":
                        var targetLabel:String = this.textValue(command.Label).toLowerCase();
                        if (this.labels[targetLabel] != null)
                        {
                            this.index = int(this.labels[targetLabel]);
                        }
                        break;
                    case "CmdIndex":
                        var indexType:String = String(command.Type).toLowerCase();
                        var amount:int = this.numberValue(command.Index, 0, this.commands.length);
                        if (indexType == "0" || indexType.indexOf("up") >= 0)
                        {
                            this.index = Math.max(0, this.index - amount - 1);
                        }
                        else if (indexType == "1" || indexType.indexOf("down") >= 0)
                        {
                            this.index = Math.min(this.commands.length, this.index + amount - 1);
                        }
                        else
                        {
                            this.index = Math.max(0, Math.min(this.commands.length, amount - 1));
                        }
                        break;
                    case "CmdStop":
                        this.stop("BOT STOPPED");
                        return;
                    case "CmdStopBotWithMessage":
                        this.stop(this.textValue(command.Message, "BOT STOPPED"));
                        return;
                    case "CmdBlank":
                    case "CmdBlank2":
                    case "CmdBlank3":
                    case "CmdLabel":
                    case "CmdLog":
                        break;
                    default:
                        // Continuing after a missing condition or control-flow
                        // command can make later actions unsafe. Stop at the
                        // exact unsupported command and preserve ordering.
                        this.stop("UNSUPPORTED COMMAND " + kind.toUpperCase());
                        return;
                }
            }
            catch (error:Error)
            {
                this.stop("BOT ERROR AT COMMAND " + String(this.index));
                return;
            }
            this.schedule(delay);
        }

        private function continueKill():void
        {
            try
            {
                if (Player.hasTarget() == Root.trueString)
                {
                    this.killEngaged = true;
                    for (var skill:int = 1; skill <= 4; skill++)
                    {
                        Skill.skillAction(String(skill));
                    }
                    this.schedule(500);
                    return;
                }
                if (!this.killEngaged && World.isMonsterAvailable(this.killTarget) == Root.trueString)
                {
                    Player.attackMonster(this.killTarget);
                    this.schedule(350);
                    return;
                }
            }
            catch (error:Error)
            {
                this.stop("BOT ERROR WHILE FIGHTING");
                return;
            }
            if (this.killForItem != null
                && !this.inventoryHas(this.killForItem, this.killForQuantity, this.killForTemporary))
            {
                DropStack.getDrop(this.killForItem);
                this.killEngaged = false;
                this.schedule(650);
                return;
            }
            this.waitingForKill = false;
            this.killEngaged = false;
            this.killTarget = null;
            this.killForItem = null;
            this.schedule(120);
        }

        private function inventoryHas(name:String, quantity:int, temporary:Boolean):Boolean
        {
            try
            {
                var raw:String = temporary ? TempInventory.tempItems() : Inventory.inventoryItems();
                var items:Array = JSON.parse(raw) as Array;
                for each (var item:Object in items)
                {
                    if (item != null && item.sName != null
                        && String(item.sName).toLowerCase() == name.toLowerCase()
                        && int(item.iQty) >= quantity)
                    {
                        return true;
                    }
                }
            }
            catch (error:Error)
            {
            }
            return false;
        }

        private function inventoryItemId(name:String):String
        {
            try
            {
                var items:Array = JSON.parse(Inventory.inventoryItems()) as Array;
                for each (var item:Object in items)
                {
                    if (item != null && item.sName != null && item.ItemID != null
                        && String(item.sName).toLowerCase() == name.toLowerCase())
                    {
                        return String(item.ItemID);
                    }
                }
            }
            catch (error:Error)
            {
            }
            return "";
        }

        private function schedule(delay:int):void
        {
            if (!this.running || this.paused)
            {
                return;
            }
            this.stopTimer();
            this.timer.delay = Math.max(1, delay);
            this.timer.repeatCount = 1;
            this.timer.start();
        }

        private function stopTimer():void
        {
            this.timer.stop();
            this.timer.reset();
        }

        private function setStatus(value:String):void
        {
            this.statusText = value;
            this.dispatchEvent(new Event(STATE_CHANGED));
        }

        private function commandName(command:Object):String
        {
            var qualified:String = command == null || command["$type"] == null
                ? "Unknown" : String(command["$type"]).split(",")[0];
            return qualified.substr(qualified.lastIndexOf(".") + 1);
        }

        private function questId(command:Object):String
        {
            if (command.QuestID != null)
            {
                return String(command.QuestID);
            }
            if (command.Quest != null && command.Quest.Id != null)
            {
                return String(command.Quest.Id);
            }
            return "0";
        }

        private function textValue(value:*, fallback:String = ""):String
        {
            return value == null ? fallback : String(value);
        }

        private function numberValue(value:*, minimum:int, maximum:int):int
        {
            var parsed:Number = Number(value);
            if (isNaN(parsed))
            {
                return minimum;
            }
            return Math.max(minimum, Math.min(maximum, int(parsed)));
        }
    }
}
