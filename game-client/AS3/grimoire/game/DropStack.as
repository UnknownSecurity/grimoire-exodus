//grimoire.game.DropStack
package grimoire.game
{
	import flash.events.MouseEvent;
	import grimoire.Root;
	
	public class DropStack 
	{
		private static var game:* = Root.Game;
		private static var pref:* = Root.Game.userPreference;		  
		
		// This function returns a boolean for whether the player has the corresponding drop or not.
		public static function hasDrop(itemName:String):String
		{
			var source:*;
            var i:int;
            var child:*;
            var children:int;
            var item:String;
            var dItemName:String;
			var bool:String = Root.falseString;
            if (game.litePreference.data.bCustomDrops)
            {
                source = ((game.cDropsUI.mcDraggable) ? game.cDropsUI.mcDraggable.menu : game.cDropsUI);
                i = 0;
                while (i < source.numChildren)
                {
                    child = source.getChildAt(i);
                    if (child.itemObj)
                    {
                        dItemName = child.itemObj.sName.toLowerCase();
                        if (dItemName == itemName.toLowerCase())
                        {
                            bool = Root.trueString;
							break;
                        };
                    };
                    i++;
                };
            }
			else
            {
                children = game.ui.dropStack.numChildren;
				while (i < children)
				{
					child = game.ui.dropStack.getChildAt(i);
					if (child.cnt.strName.text.toLowerCase() == itemName.toLowerCase())
					{
						bool = Root.trueString;
						break;
					};
					i++;
				};
            };
			return (bool);
		}
		
		// This function executes by accepting the corresponding drop.
		public static function acceptDrop(itemID:int):void
		{
			game.sfc.sendXtMessage("zm", "getDrop", [itemID], "str", game.world.curRoom)
		}

		// Accepts a visible drop by name for the standalone mobile bot runner.
		// It stays bounded to the existing AQW drop widgets and never walks the
		// broader display tree.
		public static function getDrop(itemName:String):Boolean
		{
			var source:*;
			var child:*;
			var i:int;
			var normalized:String = (itemName || "").toLowerCase();
			if (normalized.length == 0)
			{
				return false;
			}
			if (game.litePreference.data.bCustomDrops)
			{
				source = ((game.cDropsUI.mcDraggable) ? game.cDropsUI.mcDraggable.menu : game.cDropsUI);
				for (i = 0; i < source.numChildren; i++)
				{
					child = source.getChildAt(i);
					if (child.itemObj && String(child.itemObj.sName).toLowerCase() == normalized)
					{
						child.btYes.dispatchEvent(new MouseEvent(MouseEvent.CLICK));
						return true;
					}
				}
			}
			else
			{
				for (i = 0; i < game.ui.dropStack.numChildren; i++)
				{
					child = game.ui.dropStack.getChildAt(i);
					if (String(child.cnt.strName.text).toLowerCase() == normalized)
					{
						child.cnt.ybtn.dispatchEvent(new MouseEvent(MouseEvent.CLICK));
						return true;
					}
				}
			}
			return false;
		}
		
		// This function executes by rejecting a drop based on the corresponding list.
		public static function rejectDrop(list:String):void
		{
			var source:*;
            var i:int;
            var child:*;
			var itemName:String;
			var keep:Boolean;
			var item:String;
            var pickup:Array = (list || "").toLowerCase().split(",");
			if (game.litePreference.data.bCustomDrops)
            {
                source = ((game.cDropsUI.mcDraggable) ? game.cDropsUI.mcDraggable.menu : game.cDropsUI);
				i = source.numChildren - 1;
				while (i >= 0)
                {
                    child = source.getChildAt(i);
                    if (child.itemObj)
                    {
                        itemName = child.itemObj.sName.toLowerCase();
						keep = false;
						for each (item in pickup)
						{
							if (itemName == item.replace(/^\s+|\s+$/g, ""))
							{
								keep = true;
								break;
							};
						};
                        if (!keep)
                        {
                            child.btNo.dispatchEvent(new MouseEvent(MouseEvent.CLICK));
                        };
                    };
                    i++;
                };
            }
            else
            {
				i = game.ui.dropStack.numChildren - 1;
				while (i >= 0)
				{
					child = game.ui.dropStack.getChildAt(i);
					itemName = child.cnt.strName.text.toLowerCase();
					keep = false;
					for each (item in pickup)
					{
						if (itemName == item.replace(/^\s+|\s+$/g, ""))
						{
							keep = true;
							break;
						};
					};
					if (!keep)
					{
						child.cnt.nbtn.dispatchEvent(new MouseEvent(MouseEvent.CLICK));
					};
					i--;
				};
            };
		}	
		
	}
}
