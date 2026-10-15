# Graph Report - grimoire-exodus  (2026-10-15)

## Corpus Check
- 326 files · ~189,818 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 65 file(s) not represented in the graph (top: .resx 22, .as 17, .dll 9)

## Summary
- 3428 nodes · 7723 edges · 226 communities (115 shown, 111 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 499 edges (avg confidence: 0.78)
- Token cost: 782 input · 762 output

## Community Hubs (Navigation)
- Bot Manager Editing
- Bot Command Engine
- Async Game Commands
- UI Loaders and Tampering
- Main Window Navigation
- Flash Game Data
- Bot Condition Commands
- Shared Game Utilities
- Bank Item Serialization
- Bot Command Contracts
- Bot Manager Interface
- Hotkeys and Map Joining
- Bot Runtime Configuration
- Application Update Service
- Player Quest Data
- Account Locker Bridge
- JSON Message Handling
- Core Application Utilities
- Quest Data Management
- Bot Execution Lifecycle
- Bot Options and World
- Startup Diagnostics
- Inventory Drop Handling
- Travel and Movement
- Equipment Set Management
- Combined Inventory Serialization
- Avatar Progression
- Command Node Editor
- Bounded Logging Interface
- Extended Avatar Factions
- XT Proxy Messaging
- World State Scanning
- JSON Quest Conversion
- Quick Bot Indexing
- Bot File Loading
- Miscellaneous Bot Commands
- Quest Collections and Spamming
- Skill Configuration UI
- Required Quest Items
- Selectable Quest Items
- Friendly Command Editor
- Command List Rendering
- Map Cell Data
- Temporary Inventory
- Wiki Data Grabber
- Flash Bot Utilities
- Movement Skill Commands
- Non Closable Tab Controls
- Portable Update Application
- Application Path Management
- Startup Recovery State
- Packet Proxy Logging
- Packet Command Handling
- Cosmetic Equipment
- Monster State Data
- Shop Data Loading
- Plugin Contracts
- Map Join Execution
- Server Data Handling
- Release Build Tooling
- Game Action Locks
- Command Color Editor
- Packet Spammer Interface
- JavaScript Tooling
- Chat Command Execution
- Automatic Relogin
- Flash Bridge Utilities
- Safe Bot Deserialization
- Path and String Extensions
- Shop Packet Handling
- Buyback Automation
- Plugin Manager Interface
- Performance Audit Findings
- Bot Configuration Persistence
- Base Item Data
- Item Drop Stack
- Application Settings
- Purple Menu Rendering
- Flash Runtime Architecture
- Kill For Item Logic
- Aura State Handling
- XML Message Handling
- Thread Safe UI Helpers
- Bot Library Experience
- Cosmetic Form Actions
- Equipment Type Definitions
- Quest Completion Data
- Integer Command Values
- Login Packet Handling
- Release Publishing Pipeline
- Bot Client Configuration
- Bot Movement Utilities
- Indexed Bot Commands
- Bot Configuration Import
- Resource Localization
- Bot Run Context
- Download Security Policy
- Skill Command Execution
- Quest Command Lifecycle
- Avatar Access Levels
- Inventory Queries
- Client Status Indicators
- Improvement Research Roadmap
- Buy By ID Command
- Fast Buy By ID
- Item Whitelist State
- Cell Movement Command
- Quest Completion Command
- Bot Skill Messaging
- AQW Header Artwork
- Ashfall Dragon Artwork
- Community Contribution Policies
- Dark Fantasy Branding
- Alternate Buy Command
- Item Whitelist Command
- Load Bot Command
- Packet Spam Command
- Line Comparison Utilities
- Diagnostics Path Tests
- Bank Swap Command
- Bank Transfer Command
- Standard Buy Command
- Buyback Item Command
- Fast Buy Command
- Equip Item Command
- Character Change Command
- Client Message Command
- Map Provoke Toggle
- Alternate Quest Acceptance
- Avatar State Values
- Player State Values
- Reflection Utilities
- Grimoire Emblem Artwork
- Updater Merge Tests
- Attack Monster Command
- Rest Command Behavior
- Skill Set Command
- Load Shop Command
- Alternate Shop Loader
- Map Item Command
- Alternate Map Item Command
- Sell Item Command
- Use Boost Command
- Load Map SWF Command
- Beep Notification Command
- Blank Text Command
- Bot Delay Command
- Alternate Bot Delay
- Delay Command
- Go To Label Command
- Frame Rate Command
- Provoke Toggle Command
- Cell Equality Condition
- Cell Inequality Condition
- Player Cell Condition
- Player Cell Exclusion
- Player Count Greater Condition
- Player Count Lesser Condition
- Bug Reporting Workflow
- Feature Request Workflow
- Pull Request Checks
- Graphify Navigation Policy
- Available Monsters Lesser
- Gold Greater Condition
- Gold Lesser Condition
- Health Greater Condition
- Health Lesser Condition
- Health Percentage Greater
- Integer Greater Condition
- Integer Lesser Condition
- Maximum Stack Condition
- Membership Condition
- Not Maximum Stack
- Non Membership Condition
- Item Pickup Condition
- Level Greater Condition
- Mana Greater Condition
- Mana Lesser Condition
- Map Equality Condition
- Monster Health Condition
- Monster Absence Condition
- Equipment Absence Condition
- Bank Item Absence
- House Item Absence
- Player Aura Inactive
- Player Aura String Match
- Player Aura Value Greater
- Player Aura Value Lesser
- Player Cell Mismatch
- Player Class Mismatch
- Player Room Absence
- Cell Player Count Lesser
- Room Player Count Lesser
- Quest Availability Condition
- Quest Progress Condition
- Quest Unavailability Condition
- Quest Incompletion Condition
- Skill Unavailability Condition
- Target Aura Active
- Target Aura String Mismatch
- Target Aura Value Greater
- Visible Monsters Greater
- Visible Monsters Lesser
- Quest Information Model
- Home Host Bridge

## God Nodes (most connected - your core abstractions)
1. `BotManager` - 252 edges
2. `IBotEngine` - 186 edges
3. `Root` - 184 edges
4. `IBotCommand` - 175 edges
5. `Grimoire.Game` - 155 edges
6. `StatementCommand` - 103 edges
7. `Grimoire.Botting.Commands.Misc.Statements` - 97 edges
8. `Grimoire.UI` - 85 edges
9. `Player` - 80 edges
10. `Configuration` - 79 edges

## Surprising Connections (you probably didn't know these)
- `Private Vulnerability Reporting` --semantically_similar_to--> `Private Security Reporting`  [INFERRED] [semantically similar]
  SECURITY.md → .github/ISSUE_TEMPLATE/config.yml
- `Release Supply-Chain Hardening` --conceptually_related_to--> `Tagged Windows Build`  [INFERRED]
  docs/code-performance-audit-v1.1.0.md → .github/workflows/tagged-build.yml
- `Legacy Compatibility Guardrails` --semantically_similar_to--> `Legacy GBot Compatibility`  [INFERRED] [semantically similar]
  docs/code-performance-audit-v1.1.0.md → README.md
- `User Data and Legacy Format Preservation` --semantically_similar_to--> `Legacy GBot Compatibility`  [INFERRED] [semantically similar]
  docs/releases/v1.2.0.md → README.md
- `Private Registration-Free Flash Runtime` --semantically_similar_to--> `App-Local Flash Runtime`  [INFERRED] [semantically similar]
  app/Runtime/Flash/README.md → README.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Legacy Compatibility Contract** — readme_legacy_gbot_compatibility, docs_code_performance_audit_v1_1_0_legacy_guardrails, docs_improvement_research_v1_2_0_compatibility_rules, docs_releases_v1_2_0_compatibility_preservation [INFERRED 0.95]
- **Private Flash Compatibility Stack** — readme_app_local_flash_runtime, app_runtime_flash_readme_private_flash_runtime, docs_embedded_flash_runtime_private_activex_selection, docs_embedded_flash_runtime_runtime_safeguards, security_trusted_aqw_content_only [INFERRED 0.95]
- **Verified Release Delivery** — _github_workflows_tagged_build_tag_validation, _github_workflows_tagged_build_package_build, _github_workflows_tagged_build_smoke_tests, _github_workflows_tagged_build_release_publication, readme_official_release_distribution, security_official_downloads_only [INFERRED 0.95]
- **Arcane Grimoire Brand Composition** — app_resources_brand_grimoire_emblem_grimoire_emblem, app_resources_brand_grimoire_emblem_open_spellbook, app_resources_brand_grimoire_emblem_celestial_magic, app_resources_brand_grimoire_emblem_arcane_energy [INFERRED 0.85]
- **Opposed Fantasy Factions** — app_resources_home_images_aqworlds_master_header_heroic_adventurer_party, app_resources_home_images_aqworlds_master_header_undead_monster_horde, app_resources_home_images_aqworlds_master_header_good_versus_evil_confrontation [INFERRED 0.85]
- **Volcanic Dragon Battle Tableau** — app_resources_home_images_ashfall_dragons_black_fire_dragon, app_resources_home_images_ashfall_dragons_multi_headed_brown_hydra, app_resources_home_images_ashfall_dragons_foreground_armored_dragon, app_resources_home_images_ashfall_dragons_volcanic_ashfall_landscape [INFERRED 0.85]
- **Fantasy Brand Composition** — docs_assets_grimoire_github_banner_grimoire_exodus_wordmark, docs_assets_grimoire_github_banner_enchanted_grimoire_emblem, docs_assets_grimoire_github_banner_dragon_guardians, docs_assets_grimoire_github_banner_celestial_arcane_symbols, docs_assets_grimoire_github_banner_dark_fantasy_identity [INFERRED 0.85]

## Communities (226 total, 111 thin omitted)

### Community 0 - "Bot Manager Editing"
Cohesion: 0.03
Nodes (9): BotManager, ActiveBotEngine, CurrentBotName, CurrentBotPath, CustomClassName, CustomGuild, CustomName, Instance (+1 more)

### Community 1 - "Bot Command Engine"
Cohesion: 0.05
Nodes (5): IBotEngine, Configuration, CurrentConfiguration, Index, IsRunning

### Community 2 - "Async Game Commands"
Cohesion: 0.05
Nodes (4): Grimoire.Botting.Commands.Misc.Statements, Grimoire.Botting.Commands.Item, Grimoire.Botting.Commands.Combat, Grimoire.Game

### Community 3 - "UI Loaders and Tampering"
Cohesion: 0.03
Nodes (14): BankForm, DPSForm, EyeDropper, Instance, Loaders, Instance, TreeType, PacketLogger (+6 more)

### Community 4 - "Main Window Navigation"
Cohesion: 0.03
Nodes (17): Root, AppVersion, Client, CreateParams, customTravel, enableCapture, GameLoadProgress, Instance (+9 more)

### Community 5 - "Flash Game Data"
Cohesion: 0.03
Nodes (41): Bank, AvailableSlots, HasBankOpen, IsBankLoaded, Items, TotalSlots, UsedSlots, Player (+33 more)

### Community 6 - "Bot Condition Commands"
Cohesion: 0.03
Nodes (40): CmdAvailableMonstersGreaterThan, CmdEquipped, CmdFactionRankGreaterThan, CmdInCombat, CmdInHouse, CmdInInventory, CmdInTemp, CmdIntEqualInt (+32 more)

### Community 7 - "Shared Game Utilities"
Cohesion: 0.10
Nodes (4): Grimoire.Networking.Handlers, Grimoire.Tools, Grimoire.Botting, Grimoire.Game.Data

### Community 8 - "Bank Item Serialization"
Cohesion: 0.03
Nodes (54): BankItem, Bank, Category, CharItemId, CharItemID, Coins, Cost, Description (+46 more)

### Community 9 - "Bot Command Contracts"
Cohesion: 0.04
Nodes (29): CmdCancelAutoAttack, CmdCancelTarget, CmdGetDrop, ItemName, CmdSetSpawn, CmdBankList, CmdBlank, CmdClearTemp (+21 more)

### Community 10 - "Bot Manager Interface"
Cohesion: 0.11
Nodes (5): Grimoire.Tools.Plugins, Grimoire.Networking, Grimoire.UI, Grimoire.Botting.Commands.Map, Properties

### Community 11 - "Hotkeys and Map Joining"
Cohesion: 0.05
Nodes (19): CmdJoin2, Cell, _cell, Map, _map, Pad, _pad, Room (+11 more)

### Community 12 - "Bot Runtime Configuration"
Cohesion: 0.04
Nodes (50): Configuration, AFK, Author, AutoRelogin, AutoSaveState, BankOnStop, Boosts, BotDelay (+42 more)

### Community 13 - "Application Update Service"
Cohesion: 0.07
Nodes (23): PreparedUpdate, InstallerMode, PackagePath, PayloadDirectory, Release, RequiresElevation, UpdaterPath, UpdateAsset (+15 more)

### Community 14 - "Player Quest Data"
Cohesion: 0.04
Nodes (46): CmdQuestlist, ItemID, _itemID, QuestID, _questID, state, State, Add (+38 more)

### Community 15 - "Account Locker Bridge"
Cohesion: 0.07
Nodes (22): AccountCredentials, Id, Label, Password, Username, AccountRecord, Id, Label (+14 more)

### Community 16 - "JSON Message Handling"
Cohesion: 0.08
Nodes (22): HandlerDrops, HandledCommands, HandlerArea, HandledCommands, HandlerBuyItem, HandledCommands, HandlerMapJoin, HandledCommands (+14 more)

### Community 17 - "Core Application Utilities"
Cohesion: 0.10
Nodes (4): GrimoirePluginEntry, Grimoire.Updater, Grimoire, Grimoire.Utils

### Community 18 - "Quest Data Management"
Cohesion: 0.05
Nodes (35): Quest, CanComplete, ClassPointsReward, Description, EndText, ExperienceReward, Faction, FactionId (+27 more)

### Community 19 - "Bot Execution Lifecycle"
Cohesion: 0.09
Nodes (8): Bot, Configuration, CurrentConfiguration, DropDelay, DropsInInventory, Index, IsRunning, selectedIndex

### Community 20 - "Bot Options and World"
Cohesion: 0.05
Nodes (27): OptionsManager, AFK, _antiAfk, ChangeChat, DisableAnimations, EnemyMagnet, HandlerAFK1, HandlerHideRoom (+19 more)

### Community 21 - "Startup Diagnostics"
Cohesion: 0.12
Nodes (5): Program, StartupDiagnostics, PreviousSession, StartupLogFile, StartupDiagnosticsSmoke

### Community 22 - "Inventory Drop Handling"
Cohesion: 0.05
Nodes (32): InventoryItem, Category, CharItemId, Cost, Description, DropChance, EquipmentSlot, File (+24 more)

### Community 23 - "Travel and Movement"
Cohesion: 0.12
Nodes (4): Travel, Instance, travelNames, travels

### Community 24 - "Equipment Set Management"
Cohesion: 0.07
Nodes (9): CmdEquipSet, ItemName, Set, ISetInterface, Item, Name, Type, SetItem (+1 more)

### Community 25 - "Combined Inventory Serialization"
Cohesion: 0.05
Nodes (33): InventoryItemCombined, Category, CharItemId, Coins, Cost, Description, DropChance, File (+25 more)

### Community 26 - "Avatar Progression"
Cohesion: 0.06
Nodes (32): Avatar, AccessLevel, AccessType, AFK, Auras, Cell, Class, ClassRank (+24 more)

### Community 27 - "Command Node Editor"
Cohesion: 0.08
Nodes (5): CommandNode, activeControl, NodeEditor, TypedCommand, Notepad

### Community 28 - "Bounded Logging Interface"
Cohesion: 0.09
Nodes (5): DebugLogger, LogForm, Instance, SelectedLog, BoundedTextBuffer

### Community 29 - "Extended Avatar Factions"
Cohesion: 0.06
Nodes (29): ExtendedAvatar, ActivationFlag, AdventureCoin, Bank, BankSlots, BoostCP, BoostGold, BoostRep (+21 more)

### Community 30 - "XT Proxy Messaging"
Cohesion: 0.11
Nodes (18): HandlerAFK, HandledCommands, HandlerChat, HandledCommands, HandlerGetQuests, HandledCommands, HandlerWarningsXt, HandledCommands (+10 more)

### Community 31 - "World State Scanning"
Cohesion: 0.07
Nodes (27): World, ActiveCutscene, AllCellInfo, AuraSyncRoot, AvailableMonsters, AvatarsInMap, AvatarsInMyCell, Cells (+19 more)

### Community 32 - "JSON Quest Conversion"
Cohesion: 0.12
Nodes (6): BoolConverter, IntStringConverter, QuestReqConverter, QuestRewardConverter, StringBoolConverter, TrimConverter

### Community 37 - "Quest Collections and Spamming"
Cohesion: 0.10
Nodes (11): House, Items, TotalSlots, Quests, AcceptedQuests, CompletedQuests, LoadedQuests, QuestTree (+3 more)

### Community 38 - "Skill Configuration UI"
Cohesion: 0.10
Nodes (12): Skill, ComparisonType, Index, SafeHp, SafeMp, SafePercentage, Text, Type (+4 more)

### Community 39 - "Required Quest Items"
Cohesion: 0.08
Nodes (25): itemsR, bPTR, Category, Coins, Cost, Description, HouseItem, Icon (+17 more)

### Community 40 - "Selectable Quest Items"
Cohesion: 0.08
Nodes (25): itemsS, bPTR, Category, Coins, Cost, Description, HouseItem, Icon (+17 more)

### Community 41 - "Friendly Command Editor"
Cohesion: 0.08
Nodes (4): UserFriendlyCommandEditor, cmd, cmdObj, commandEditor

### Community 42 - "Command List Rendering"
Cohesion: 0.11
Nodes (7): CmdBlank3, Alpha, B, G, R, Text, FontInfo

### Community 43 - "Map Cell Data"
Cohesion: 0.09
Nodes (22): Cell, MapItems, Name, Pads, Map, Area, Cells, EmptyCells (+14 more)

### Community 44 - "Temporary Inventory"
Cohesion: 0.09
Nodes (14): TempInventory, Items, TempItem, Cost, Description, Id, IsAcItem, IsMemberOnly (+6 more)

### Community 46 - "Flash Bot Utilities"
Cohesion: 0.10
Nodes (3): NetworkUtils, Grimoire.Tools.Buyback, Grimoire.Botting.Commands.Quest

### Community 47 - "Movement Skill Commands"
Cohesion: 0.09
Nodes (17): CmdUseSkill2, Index, Monster, SafeHp, SafeMp, Skill, Wait, CmdWalk (+9 more)

### Community 48 - "Non Closable Tab Controls"
Cohesion: 0.10
Nodes (10): ResizableUserControl, Bottom, BottomLeft, BottomRight, Left, Right, Top, TopLeft (+2 more)

### Community 50 - "Application Path Management"
Cohesion: 0.12
Nodes (15): AppPaths, ApplicationDirectory, BotClientConfigFile, BotsDirectory, CommunityBotsDirectory, ConfigFile, CrashReportsDirectory, DataDirectory (+7 more)

### Community 51 - "Startup Recovery State"
Cohesion: 0.10
Nodes (18): RecoveryInfo, BotWasRunning, LastBotPath, LastStage, LastUpdatedUtc, StartedUtc, WasHiddenToTray, SessionState (+10 more)

### Community 52 - "Packet Proxy Logging"
Cohesion: 0.16
Nodes (7): IXmlMessageHandler, HandledCommands, Proxy, _catchAllPackets, _catchXtPackets, firstJoin, Instance

### Community 53 - "Packet Command Handling"
Cohesion: 0.11
Nodes (13): CmdFollow, Name, CmdGotoPlayer, PlayerName, _playerName, CmdPacket, CmdPacket2, Client (+5 more)

### Community 56 - "Monster State Data"
Cohesion: 0.11
Nodes (18): Monster, Alive, Auras, Cell, CurrentState, Health, Id, Level (+10 more)

### Community 57 - "Shop Data Loading"
Cohesion: 0.11
Nodes (14): Shop, Id, IsShopLoaded, Items, Location, Name, Type, BankItems (+6 more)

### Community 58 - "Plugin Contracts"
Cohesion: 0.14
Nodes (8): GrimoirePlugin, Author, Description, LastError, Name, IGrimoirePlugin, Author, Description

### Community 59 - "Map Join Execution"
Cohesion: 0.15
Nodes (4): CmdJoin, Cell, Map, Pad

### Community 60 - "Server Data Handling"
Cohesion: 0.12
Nodes (11): Server, Ip, IsChatRestricted, IsMemberOnly, IsOnline, Language, List, MaxCount (+3 more)

### Community 61 - "Release Build Tooling"
Cohesion: 0.23
Nodes (14): Assert-FileHash(), Complete-BrandedOutput(), Get-BundledFlashRuntime(), Get-ExodusProductVersion(), Get-MSBuildPath(), Get-WixToolset(), Invoke-ExodusBuild(), Invoke-NuGetRestore() (+6 more)

### Community 62 - "Game Action Locks"
Cohesion: 0.12
Nodes (15): LockActions, AcceptQuest, BuyItem, DoIA, EquipItem, GetMapItem, LoadEnhShop, LoadHairShop (+7 more)

### Community 65 - "JavaScript Tooling"
Cohesion: 0.12
Nodes (15): description, devDependencies, @commitlint/cli, @commitlint/config-conventional, husky, name, private, scripts (+7 more)

### Community 67 - "Chat Command Execution"
Cohesion: 0.20
Nodes (6): CmdKill, Monster, Packet, ChatCommand, cmdCombat, Engine

### Community 68 - "Automatic Relogin"
Cohesion: 0.18
Nodes (7): AutoRelogin, AreServersLoaded, GameLabel, IsTemporarilyKicked, LoginLabel, ServerIP, ServerLabel

### Community 72 - "Shop Packet Handling"
Cohesion: 0.15
Nodes (12): ShopInfo, Field, House, Id, Index, Items, Location, Member (+4 more)

### Community 73 - "Buyback Automation"
Cohesion: 0.20
Nodes (8): AutoBuyBack, Password, Username, BuyBackPage, EventArgument, EventValidation, ViewState, ViewStateGenerator

### Community 75 - "Plugin Manager Interface"
Cohesion: 0.18
Nodes (3): PluginManager, Instance, LastError

### Community 76 - "Performance Audit Findings"
Cohesion: 0.18
Nodes (7): Grimoire Exodus v1.1.0 Code and Performance Audit, 2026-10-15 Implementation Outcome, Legacy Compatibility Guardrails, Future-Work Compatibility Rules, Grimoire Exodus v1.2.0 Release Notes, Bot Runtime Responsiveness and Recovery, Legacy GBot Compatibility

### Community 78 - "Bot Configuration Persistence"
Cohesion: 0.15
Nodes (5): Config, Contents, file, Instance, AtomicFile

### Community 79 - "Base Item Data"
Cohesion: 0.15
Nodes (12): ItemBase, Category, Coins, Description, File, ID, Link, MaxStack (+4 more)

### Community 80 - "Item Drop Stack"
Cohesion: 0.18
Nodes (4): DropStack, Count, InProcess, MarkedDrops

### Community 81 - "Application Settings"
Cohesion: 0.15
Nodes (7): Settings, Default, Style, Settings, Default, Style, Grimoire.Properties

### Community 83 - "Flash Runtime Architecture"
Cohesion: 0.18
Nodes (9): Private Registration-Free Flash Runtime, App-Local Flash Runtime Guide, Flash Host ABI Requirements, Embedded Flash Runtime Research, Embedded Runtime Safeguards, Maintained AQW Automation Client, Grimoire Exodus, Installer and Writable Storage Model (+1 more)

### Community 84 - "Kill For Item Logic"
Cohesion: 0.17
Nodes (9): CmdKillFor, ItemName, ItemType, Monster, Quantity, ItemType, Items, TempItems (+1 more)

### Community 86 - "Aura State Handling"
Cohesion: 0.18
Nodes (7): Aura, disableType, Duration, entityID, Name, realValue, Value

### Community 87 - "XML Message Handling"
Cohesion: 0.20
Nodes (6): Message, Command, RawContent, Send, XmlMessage, Body

### Community 89 - "Bot Library Experience"
Cohesion: 0.20
Nodes (10): Bundled Official GBots, Community GBot Distribution Guide, Encrypted Account Locker UI, applyState, AQW Loading Progress UI, View-Only Bot Manager UI, Grimoire Home Surface, renderAccounts (+2 more)

### Community 90 - "Cosmetic Form Actions"
Cohesion: 0.18
Nodes (7): CosmeticEquipment, ID, Link, Meta, Slot, SWFFile, Type

### Community 91 - "Equipment Type Definitions"
Cohesion: 0.18
Nodes (10): EquipType, Armor, Cape, Class, Ground, Helm, None, Offhand (+2 more)

### Community 92 - "Quest Completion Data"
Cohesion: 0.18
Nodes (10): CompletedQuest, Id, Name, Reward, Success, QuestReward, ClassPoints, Experience (+2 more)

### Community 94 - "Integer Command Values"
Cohesion: 0.20
Nodes (8): CmdInt, Int, type, Value, Types, Lower, Set, Upper

### Community 96 - "Release Publishing Pipeline"
Cohesion: 0.22
Nodes (7): MSI and Portable Package Build, Checksummed GitHub Release Publication, Release Compatibility Smoke Tests, Release Tag Validation, Tagged Windows Build, Modern Installation and Update Foundation, Grimoire Exodus v1.1.0 Release Notes

### Community 97 - "Bot Client Configuration"
Cohesion: 0.22
Nodes (4): BotClientConfig, Contents, file, Instance

### Community 98 - "Bot Movement Utilities"
Cohesion: 0.22
Nodes (5): BotUtilities, ActiveBot, BusyClient, HasLoadedQuests, ShouldUseSkill

### Community 99 - "Indexed Bot Commands"
Cohesion: 0.22
Nodes (7): CmdIndex, Index, Type, IndexCommand, Down, Goto, Up

### Community 101 - "Resource Localization"
Cohesion: 0.22
Nodes (6): Resources, Culture, defaulttext, GrimoireIcon, ResourceManager, statementcmds

### Community 102 - "Bot Run Context"
Cohesion: 0.25
Nodes (6): BotRunContext, BotTask, Cancellation, Configuration, DropTask, QuestTask

### Community 103 - "Download Security Policy"
Cohesion: 0.29
Nodes (4): Issue Intake Configuration, Private Security Reporting, Security Policy, Trusted AQW Content Only

### Community 104 - "Skill Command Execution"
Cohesion: 0.29
Nodes (6): CmdUseSkill, Index, SafeHp, SafeMp, Skill, Wait

### Community 105 - "Quest Command Lifecycle"
Cohesion: 0.29
Nodes (4): CmdAcceptQuest, Quest, CmdCompleteQuest, Quest

### Community 106 - "Avatar Access Levels"
Cohesion: 0.29
Nodes (7): Access, Developer, LeadDeveloper, Moderator, Player, Staff, Tester

### Community 107 - "Inventory Queries"
Cohesion: 0.29
Nodes (5): Inventory, AvailableSlots, Items, MaxSlots, UsedSlots

### Community 109 - "Client Status Indicators"
Cohesion: 0.29
Nodes (7): StatusIcon, Cpu, EyeClosed, EyeOpen, Network, UpdateAvailable, Uptime

### Community 110 - "Improvement Research Roadmap"
Cohesion: 0.33
Nodes (3): Bot Doctor, Compatibility Center, Grimoire Exodus v1.2.0 Improvement and Feature Research

### Community 111 - "Buy By ID Command"
Cohesion: 0.33
Nodes (5): CmdBuyByID, ItemID, Manual, ShopID, ShopItemID

### Community 112 - "Fast Buy By ID"
Cohesion: 0.33
Nodes (5): CmdBuyFastByID, ItemID, Manual, ShopID, ShopItemID

### Community 113 - "Item Whitelist State"
Cohesion: 0.33
Nodes (6): state, Add, Clear, Off, On, Remove

### Community 114 - "Cell Movement Command"
Cohesion: 0.33
Nodes (5): CmdMoveToCell, Cell, _cell, Pad, _pad

### Community 115 - "Quest Completion Command"
Cohesion: 0.33
Nodes (5): CmdCompleteQuest2, ItemID, _itemID, QuestID, _questID

### Community 118 - "AQW Header Artwork"
Cohesion: 0.47
Nodes (6): AQWorlds Master Header, Fantasy Castle Landscape, Fantasy Role-Playing Adventure, Good Versus Evil Confrontation, Heroic Adventurer Party, Undead Monster Horde

### Community 119 - "Ashfall Dragon Artwork"
Cohesion: 0.53
Nodes (6): Ashfall Dragons Battle, Black Fire Dragon, Dragon Confrontation, Foreground Armored Dragon, Multi-Headed Brown Hydra, Volcanic Ashfall Landscape

### Community 120 - "Community Contribution Policies"
Cohesion: 0.33
Nodes (4): Contributor Covenant Code of Conduct, Harassment-Free Community, Contribution Guide, Contribution Testing Policy

### Community 121 - "Dark Fantasy Branding"
Cohesion: 0.53
Nodes (6): Grimoire Exodus GitHub Banner, Celestial and Arcane Symbols, Dark Fantasy Identity, Opposing Dragon Guardians, Enchanted Grimoire Emblem, Grimoire Exodus Wordmark

### Community 122 - "Alternate Buy Command"
Cohesion: 0.40
Nodes (4): CmdBuy2, ItemName, Manual, ShopId

### Community 123 - "Item Whitelist Command"
Cohesion: 0.40
Nodes (3): CmdWhitelist, Item, State

### Community 124 - "Load Bot Command"
Cohesion: 0.40
Nodes (4): CmdLoadBot, BotFileName, BotFilePath, _path

### Community 125 - "Packet Spam Command"
Cohesion: 0.40
Nodes (4): CmdPacketlist2, Delay, Packet, Type

### Community 128 - "Diagnostics Path Tests"
Cohesion: 0.40
Nodes (4): AppPaths, CrashReportsDirectory, LogsDirectory, RecoveryDirectory

### Community 129 - "Bank Swap Command"
Cohesion: 0.50
Nodes (3): CmdBankSwap, BankItemName, InventoryItemName

### Community 130 - "Bank Transfer Command"
Cohesion: 0.50
Nodes (3): CmdBankTransfer, ItemName, TransferFromBank

### Community 131 - "Standard Buy Command"
Cohesion: 0.50
Nodes (3): CmdBuy, ItemName, ShopId

### Community 132 - "Buyback Item Command"
Cohesion: 0.50
Nodes (3): CmdBuyBack, ItemName, PageNumberCap

### Community 133 - "Fast Buy Command"
Cohesion: 0.50
Nodes (3): CmdBuyFast, ItemName, Manual

### Community 134 - "Equip Item Command"
Cohesion: 0.50
Nodes (3): CmdEquip, ItemName, Safe

### Community 135 - "Character Change Command"
Cohesion: 0.50
Nodes (3): CmdChange, Guild, Text

### Community 136 - "Client Message Command"
Cohesion: 0.50
Nodes (3): CmdClientMessage, IsWarning, Messages

### Community 137 - "Map Provoke Toggle"
Cohesion: 0.50
Nodes (3): CmdToggleProvokeInMap, ProvokePacket, Type

### Community 138 - "Alternate Quest Acceptance"
Cohesion: 0.50
Nodes (3): CmdAcceptQuest2, QuestID, _questID

### Community 139 - "Avatar State Values"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

### Community 140 - "Player State Values"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

### Community 143 - "Grimoire Emblem Artwork"
Cohesion: 0.67
Nodes (4): Arcane Energy, Celestial Magic, Grimoire Emblem, Open Spellbook

## Knowledge Gaps
- **1002 isolated node(s):** `Monster`, `Monster`, `Packet`, `Monster`, `ItemName` (+997 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1424 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **111 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `BotManager` connect `Bot Manager Editing` to `Bot Command Engine`, `UI Loaders and Tampering`, `Bot Condition Commands`, `Bot Manager Interface`, `Quest Data Management`, `Travel and Movement`, `Equipment Set Management`, `Command Node Editor`, `Bounded Logging Interface`, `JSON Quest Conversion`, `Quick Bot Indexing`, `Bot File Loading`, `Quest Collections and Spamming`, `Skill Configuration UI`, `Friendly Command Editor`, `Command List Rendering`, `Wiki Data Grabber`, `Non Closable Tab Controls`, `Toolbar Status Styling`, `Server Data Handling`, `Packet Spammer Interface`, `Plugin Manager Interface`, `Thread Safe UI Helpers`, `Command List Keyboard Input`, `Bot Info Link Handling`, `Bot Manager Runtime Controls`, `Bot Configuration Import`?**
  _High betweenness centrality (0.145) - this node is a cross-community bridge._
- **What connects `Monster`, `Monster`, `Packet` to the rest of the system?**
  _1002 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Bot Manager Editing` be split into smaller, more focused modules?**
  _Cohesion score 0.028616352201257862 - nodes in this community are weakly interconnected._
- **Why does `IBotCommand` connect `Bot Command Contracts` to `Bot Manager Editing`, `Bot Command Engine`, `Bot Condition Commands`, `Shared Game Utilities`, `Hotkeys and Map Joining`, `Bot Runtime Configuration`, `Player Quest Data`, `Bot Execution Lifecycle`, `Travel and Movement`, `Equipment Set Management`, `Bot File Loading`, `Skill Configuration UI`, `Command List Rendering`, `Movement Skill Commands`, `Packet Command Handling`, `Map Join Execution`, `Chat Command Execution`, `Kill For Item Logic`, `Bot Manager Runtime Controls`, `Integer Command Values`, `Bot Movement Utilities`, `Indexed Bot Commands`, `Skill Command Execution`, `Quest Command Lifecycle`, `Buy By ID Command`, `Fast Buy By ID`, `Cell Movement Command`, `Quest Completion Command`, `Alternate Buy Command`, `Item Whitelist Command`, `Load Bot Command`, `Packet Spam Command`, `Bank Swap Command`, `Bank Transfer Command`, `Standard Buy Command`, `Buyback Item Command`, `Fast Buy Command`, `Equip Item Command`, `Character Change Command`, `Client Message Command`, `Map Provoke Toggle`, `Alternate Quest Acceptance`, `Attack Monster Command`, `Rest Command Behavior`, `Skill Set Command`, `Load Shop Command`, `Alternate Shop Loader`, `Map Item Command`, `Alternate Map Item Command`, `Sell Item Command`, `Use Boost Command`, `Load Map SWF Command`, `Beep Notification Command`, `Blank Text Command`, `Bot Delay Command`, `Alternate Bot Delay`, `Delay Command`, `Go To Label Command`, `Frame Rate Command`, `Provoke Toggle Command`, `Cell Equality Condition`, `Cell Inequality Condition`, `Player Cell Condition`, `Player Cell Exclusion`, `Player Count Greater Condition`, `Player Count Lesser Condition`, `Available Monsters Lesser`, `Gold Greater Condition`, `Gold Lesser Condition`, `Health Greater Condition`, `Health Lesser Condition`, `Health Percentage Greater`, `Integer Greater Condition`, `Integer Lesser Condition`, `Maximum Stack Condition`, `Membership Condition`, `Not Maximum Stack`, `Non Membership Condition`, `Item Pickup Condition`, `Level Greater Condition`, `Mana Greater Condition`, `Mana Lesser Condition`, `Map Equality Condition`, `Monster Health Condition`, `Monster Absence Condition`, `Equipment Absence Condition`, `Bank Item Absence`, `House Item Absence`, `Player Aura Inactive`, `Player Aura String Match`, `Player Aura Value Greater`, `Player Aura Value Lesser`, `Player Cell Mismatch`, `Player Class Mismatch`, `Player Room Absence`, `Cell Player Count Lesser`, `Room Player Count Lesser`, `Quest Availability Condition`, `Quest Progress Condition`, `Quest Unavailability Condition`, `Quest Incompletion Condition`, `Skill Unavailability Condition`, `Target Aura Active`, `Target Aura String Mismatch`, `Target Aura Value Greater`, `Visible Monsters Greater`, `Visible Monsters Lesser`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **Should `Bot Command Engine` be split into smaller, more focused modules?**
  _Cohesion score 0.04922059518186112 - nodes in this community are weakly interconnected._
- **Why does `Root` connect `Main Window Navigation` to `Bot Manager Editing`, `UI Loaders and Tampering`, `Bot Manager Interface`, `Hotkeys and Map Joining`, `Application Update Service`, `Account Locker Bridge`, `Startup Diagnostics`, `Travel and Movement`, `Command Node Editor`, `Quick Bot Indexing`, `Quest Collections and Spamming`, `Friendly Command Editor`, `Command List Rendering`, `Plugin Menu Events`, `Wiki Data Grabber`, `Non Closable Tab Controls`, `Startup Recovery State`, `Toolbar Status Styling`, `Flash Bridge Utilities`, `System Tray Behavior`, `Plugin Manager Interface`, `Client Login Recovery`, `Purple Menu Rendering`, `Cancellation and Update Checks`, `Aura State Handling`, `Home Credential Workflow`, `Client Status Indicators`, `Game Option Toggles`, `Performance Metrics UI`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Should `Async Game Commands` be split into smaller, more focused modules?**
  _Cohesion score 0.04635854341736695 - nodes in this community are weakly interconnected._