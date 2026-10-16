package grimoire
{
    import flash.events.Event;
    import flash.events.EventDispatcher;
    import flash.events.IOErrorEvent;
    import flash.events.SecurityErrorEvent;
    import flash.filesystem.File;
    import flash.filesystem.FileMode;
    import flash.filesystem.FileStream;
    import flash.net.FileFilter;
    import flash.net.FileReference;
    import flash.utils.ByteArray;

    /**
     * Bounded, data-only GBot library for AIR. It never resolves $type values
     * into runtime classes. Community files are read-only and imported bots
     * remain inside application storage.
     */
    public final class MobileBotLibrary extends EventDispatcher
    {
        public static const CHANGED:String = "mobileBotLibraryChanged";
        public static const MAX_BOT_BYTES:uint = 2 * 1024 * 1024;
        public static const MAX_COMMANDS:int = 5000;

        private var items:Array = [];
        private var picker:FileReference;
        private var currentIndex:int = -1;
        private var currentDocument:Object;
        private var currentStatus:String = "Select a bot to inspect it.";

        public function MobileBotLibrary()
        {
            this.refresh();
        }

        public function get entries():Array
        {
            return this.items.concat();
        }

        public function get selectedIndex():int
        {
            return this.currentIndex;
        }

        public function get selectedEntry():Object
        {
            return this.currentIndex >= 0 && this.currentIndex < this.items.length
                ? this.items[this.currentIndex]
                : null;
        }

        public function get selectedDocument():Object
        {
            return this.currentDocument;
        }

        public function get status():String
        {
            return this.currentStatus;
        }

        public function refresh():void
        {
            var previousPath:String = this.selectedEntry == null ? "" : this.selectedEntry.path;
            var refreshed:Array = [];
            this.appendDirectory(refreshed,
                File.applicationDirectory.resolvePath("bots/community"), true);
            this.appendDirectory(refreshed,
                File.applicationStorageDirectory.resolvePath("Bots"), false);
            refreshed.sortOn("sortKey", Array.CASEINSENSITIVE);
            this.items = refreshed;
            this.currentIndex = -1;
            this.currentDocument = null;

            for (var i:int = 0; i < this.items.length; i++)
            {
                if (this.items[i].path == previousPath)
                {
                    this.select(i);
                    return;
                }
            }
            this.currentStatus = this.items.length == 0
                ? "No bots yet. Import a legacy .gbot file."
                : "Select a bot to inspect it.";
            this.dispatchEvent(new Event(CHANGED));
        }

        public function select(index:int):void
        {
            if (index < 0 || index >= this.items.length)
            {
                this.currentIndex = -1;
                this.currentDocument = null;
                this.currentStatus = "Select a bot to inspect it.";
                this.dispatchEvent(new Event(CHANGED));
                return;
            }

            this.currentIndex = index;
            try
            {
                this.currentDocument = this.readDocument(this.items[index].file as File);
                this.currentStatus = "Loaded safely for mobile inspection.";
            }
            catch (error:Error)
            {
                this.currentDocument = null;
                this.currentStatus = error.message;
            }
            this.dispatchEvent(new Event(CHANGED));
        }

        public function browseForImport():void
        {
            if (this.picker != null)
            {
                return;
            }
            this.picker = new FileReference();
            this.picker.addEventListener(Event.SELECT, this.onFileSelected);
            this.picker.addEventListener(Event.CANCEL, this.onFileCancelled);
            this.picker.addEventListener(Event.COMPLETE, this.onFileLoaded);
            this.picker.addEventListener(IOErrorEvent.IO_ERROR, this.onImportError);
            this.picker.addEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onImportError);
            try
            {
                this.picker.browse([new FileFilter("Grimoire bot (*.gbot)", "*.gbot")]);
            }
            catch (error:Error)
            {
                this.finishPicker("The Android document picker could not be opened.");
            }
        }

        public function deleteSelected():void
        {
            var entry:Object = this.selectedEntry;
            if (entry == null)
            {
                this.currentStatus = "Select an imported bot first.";
            }
            else if (entry.community)
            {
                this.currentStatus = "Community bots are read-only.";
            }
            else
            {
                try
                {
                    File(entry.file).deleteFile();
                    this.currentStatus = "Imported bot removed.";
                    this.currentIndex = -1;
                    this.currentDocument = null;
                    this.refresh();
                    return;
                }
                catch (error:Error)
                {
                    this.currentStatus = "The imported bot could not be removed.";
                }
            }
            this.dispatchEvent(new Event(CHANGED));
        }

        public function clearImportedBots():void
        {
            var directory:File = File.applicationStorageDirectory.resolvePath("Bots");
            if (directory.exists && directory.isDirectory)
            {
                try
                {
                    for each (var file:File in directory.getDirectoryListing())
                    {
                        if (!file.isDirectory && file.extension != null
                            && file.extension.toLowerCase() == "gbot")
                        {
                            file.deleteFile();
                        }
                    }
                }
                catch (error:Error)
                {
                    this.currentStatus = "Some imported bots could not be removed.";
                    this.dispatchEvent(new Event(CHANGED));
                    return;
                }
            }
            this.currentStatus = "Mobile data reset. Community bots were preserved.";
            this.refresh();
        }

        private function appendDirectory(target:Array, directory:File, community:Boolean):void
        {
            if (directory == null || !directory.exists || !directory.isDirectory)
            {
                return;
            }
            try
            {
                var files:Array = directory.getDirectoryListing();
                for each (var file:File in files)
                {
                    if (!file.isDirectory && file.extension != null
                        && file.extension.toLowerCase() == "gbot")
                    {
                        target.push({
                            name: this.displayName(file.name),
                            sortKey: this.displayName(file.name).toLowerCase(),
                            community: community,
                            path: file.url,
                            file: file
                        });
                    }
                }
            }
            catch (error:Error)
            {
                // A missing or unreadable optional directory is simply empty.
            }
        }

        private function readDocument(file:File):Object
        {
            if (file == null || !file.exists || file.isDirectory)
            {
                throw new Error("The selected bot is no longer available.");
            }
            if (file.size <= 0 || file.size > MAX_BOT_BYTES)
            {
                throw new Error("This bot exceeds the safe mobile file limit.");
            }
            var stream:FileStream = new FileStream();
            var raw:String;
            try
            {
                stream.open(file, FileMode.READ);
                raw = stream.readUTFBytes(stream.bytesAvailable);
            }
            finally
            {
                stream.close();
            }
            return this.parseDocument(raw);
        }

        private function parseDocument(raw:String):Object
        {
            var document:Object;
            try
            {
                document = JSON.parse(raw);
            }
            catch (error:Error)
            {
                throw new Error("This file is not valid GBot JSON.");
            }

            var rootType:String = document == null || document["$type"] == null
                ? "" : String(document["$type"]);
            if (rootType.indexOf("Grimoire.Botting.Configuration,") != 0)
            {
                throw new Error("Unsupported GBot root type.");
            }
            var commands:Array = document.Commands == null
                ? null : document.Commands["$values"] as Array;
            if (commands == null)
            {
                throw new Error("The GBot command list is missing.");
            }
            if (commands.length > MAX_COMMANDS)
            {
                throw new Error("This GBot has too many commands for mobile.");
            }
            document.mobileCommands = commands;
            return document;
        }

        private function onFileSelected(event:Event):void
        {
            if (this.picker == null || this.picker.size <= 0
                || this.picker.size > MAX_BOT_BYTES)
            {
                this.finishPicker("This bot exceeds the safe mobile file limit.");
                return;
            }
            this.currentStatus = "Importing bot...";
            this.dispatchEvent(new Event(CHANGED));
            this.picker.load();
        }

        private function onFileCancelled(event:Event):void
        {
            this.finishPicker("Import cancelled.");
        }

        private function onFileLoaded(event:Event):void
        {
            try
            {
                var bytes:ByteArray = this.picker.data;
                bytes.position = 0;
                var raw:String = bytes.readUTFBytes(bytes.length);
                this.parseDocument(raw);

                var folder:File = File.applicationStorageDirectory.resolvePath("Bots");
                if (!folder.exists)
                {
                    folder.createDirectory();
                }
                var fileName:String = this.safeFileName(this.picker.name);
                var destination:File = this.uniqueDestination(folder, fileName);
                var stream:FileStream = new FileStream();
                try
                {
                    stream.open(destination, FileMode.WRITE);
                    stream.writeUTFBytes(raw);
                }
                finally
                {
                    stream.close();
                }
                this.finishPicker("Bot imported.");
                this.refresh();
            }
            catch (error:Error)
            {
                this.finishPicker(error.message.length > 0
                    ? error.message : "The selected bot could not be imported.");
            }
        }

        private function onImportError(event:Event):void
        {
            this.finishPicker("The selected bot could not be read.");
        }

        private function finishPicker(message:String):void
        {
            if (this.picker != null)
            {
                this.picker.removeEventListener(Event.SELECT, this.onFileSelected);
                this.picker.removeEventListener(Event.CANCEL, this.onFileCancelled);
                this.picker.removeEventListener(Event.COMPLETE, this.onFileLoaded);
                this.picker.removeEventListener(IOErrorEvent.IO_ERROR, this.onImportError);
                this.picker.removeEventListener(SecurityErrorEvent.SECURITY_ERROR, this.onImportError);
                this.picker = null;
            }
            this.currentStatus = message;
            this.dispatchEvent(new Event(CHANGED));
        }

        private function uniqueDestination(folder:File, fileName:String):File
        {
            var stem:String = fileName.substr(0, fileName.length - 5);
            var candidate:File = folder.resolvePath(fileName);
            var suffix:int = 2;
            while (candidate.exists && suffix < 1000)
            {
                candidate = folder.resolvePath(stem + " (" + suffix + ").gbot");
                suffix++;
            }
            return candidate;
        }

        private function safeFileName(value:String):String
        {
            var result:String = value == null ? "bot.gbot" : value;
            result = result.replace(/[\\\/:*?"<>|]/g, "_");
            if (result.toLowerCase().lastIndexOf(".gbot") != result.length - 5)
            {
                result += ".gbot";
            }
            return result.length > 96 ? result.substr(0, 91) + ".gbot" : result;
        }

        private function displayName(value:String):String
        {
            if (value == null)
            {
                return "Untitled bot";
            }
            return value.toLowerCase().lastIndexOf(".gbot") == value.length - 5
                ? value.substr(0, value.length - 5)
                : value;
        }
    }
}
