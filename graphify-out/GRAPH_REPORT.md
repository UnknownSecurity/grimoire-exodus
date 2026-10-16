# Graph Report - grimoire-exodus  (2026-10-16)

## Corpus Check
- 323 files · ~195,547 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 65 file(s) not represented in the graph (top: .resx 22, .as 17, .dll 9)

## Summary
- 3529 nodes · 8003 edges · 237 communities (125 shown, 112 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 512 edges (avg confidence: 0.78)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `9197e937`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Bot Manager Editing
- Bot Command Engine
- StatementCommand
- CmdEquipSet
- Main Window Navigation
- Flash Game Data
- system_threading_tasks
- Shared Game Utilities
- Bank Item Serialization
- IBotCommand
- Bot Manager Interface
- Hotkeys
- Bot Runtime Configuration
- UpdateService
- PlayerInfo
- Account Locker Bridge
- IJsonMessageHandler
- Core Application Utilities
- Quest Data Management
- Bot Execution Lifecycle
- Bot Options and World
- StartupDiagnostics
- Inventory Drop Handling
- Travel and Movement
- Set
- Combined Inventory Serialization
- Avatar Progression
- Command Node Editor
- IContainer
- Extended Avatar Factions
- XT Proxy Messaging
- World State Scanning
- JSON Quest Conversion
- JsonMessage
- Bot File Loading
- Miscellaneous Bot Commands
- .HandleAsync
- Skill Configuration UI
- Required Quest Items
- Selectable Quest Items
- UserFriendlyCommandEditor
- CmdBlank3
- Map Cell Data
- Temporary Inventory
- Item
- CmdUseSkill2
- Non Closable Tab Controls
- Portable Update Application
- Application Path Management
- Startup Recovery State
- Packet Proxy Logging
- Packet Command Handling
- CosmeticForm
- Monster State Data
- .IsActionAvailable
- Plugin Contracts
- .IsActionAvailable
- AutoRelogin
- Release Build Tooling
- Game Action Locks
- Command Color Editor
- Packet Spammer Interface
- JavaScript Tooling
- .ExecuteCommand
- Grabber
- Flash
- Type
- IGrimoirePlugin
- Shop Packet Handling
- NonClosableVSTabControl
- LogForm
- BankForm
- Performance Audit Findings
- IContainer
- CmdToggleProvokeInMap
- Base Item Data
- Item Drop Stack
- Application Settings
- Purple Menu Rendering
- Flash Runtime Architecture
- Kill For Item Logic
- Aura State Handling
- CmdRest
- .RecordException
- Bot Library Experience
- PluginManager
- Equipment Type Definitions
- Quest Completion Data
- Integer Command Values
- GrimoireHomeBridge
- Release Publishing Pipeline
- Dictionary
- Loaders
- Indexed Bot Commands
- Bot Configuration Import
- Resource Localization
- CmdToggleProvoke
- Download Security Policy
- Skill Command Execution
- CmdCompleteQuest2
- Avatar Access Levels
- List
- .RefreshHomeState
- Client Status Indicators
- Improvement Research Roadmap
- State
- BotManager.cs
- Item Whitelist State
- Cell Movement Command
- CmdAcceptQuest
- CmdBotDelay
- Message
- AQW Header Artwork
- Ashfall Dragon Artwork
- Community Contribution Policies
- Dark Fantasy Branding
- CmdBuy2
- CmdBuyFastByID
- Load Bot Command
- Packet Spam Command
- CmdSetFPS
- CmdPlayersInCellGreaterThan
- Diagnostics Path Tests
- SettingsForm
- CmdAvailableMonstersLessThan
- KeyboardHook
- CmdGoldGreaterThan
- CmdAcceptQuest2
- CmdIntGreaterThan
- CmdIntLessThan
- CmdJoin2
- Inventory
- BotUtilities
- Avatar State Values
- CmdWhitelist
- AutoBuyBack.cs
- AppPreferenceSnapshot
- Grimoire Emblem Artwork
- Updater Merge Tests
- CmdIsMaxStack
- CmdItemPickupable
- CmdLoad
- CmdWalk
- CmdMapItem
- CmdBuyByID
- CmdManaGreaterThan
- CmdLog
- CmdQuestlist
- CmdMapIs
- CmdNotEquipped
- CmdPlayerAuraIsNotActive
- CmdBankSwap
- CmdPlayerAuraValIsGreaterThan
- CmdClientMessage
- CmdBankTransfer
- Grimoire Exodus v1.3.0
- Cell Inequality Condition
- GrimoireForm
- Player Cell Exclusion
- CmdPlayerNotEquippedClass
- Player Count Lesser Condition
- CmdBuy
- CmdTargetAuraStringValIsNot
- Bug Reporting Workflow
- Feature Request Workflow
- Pull Request Checks
- Graphify Navigation Policy
- CmdDelay
- CmdBuyBack
- Gold Lesser Condition
- Health Greater Condition
- Health Lesser Condition
- CmdBuyFast
- CmdCellIs.cs
- CmdEquip
- CmdTargetAuraValIsGreaterThan
- CmdChange
- CmdGotoLabel
- CmdVisibleMonstersLessThan
- .GamePopup
- ReflectUtils
- Mana Lesser Condition
- Monster Health Condition
- Monster Absence Condition
- CmdPlayerIsInCell
- CmdAttack
- CmdSkillSet
- CmdIsMember
- CmdIsNotMaxStack
- CmdHealthPercentageGreaterThan
- CmdLoad2
- CmdMapItem2
- CmdSell
- CmdUseBoost
- CmdLoadMapSwf
- CmdBeep
- CmdBlank2
- CmdIsNotMember
- CmdLevelGreaterThan
- CmdPlayersLessThan
- CmdBotDelay2
- Home Host Bridge
- CmdNotInBank
- CmdNotInHouse
- CmdPlayerIsNotInMyCell
- CmdPlayerAuraStringValIs
- CmdQuestInProgress
- CmdQuestNotCompleted
- CmdPlayerAuraValIsLessThan
- CmdVisibleMonstersGreaterThan
- CmdPlayerNotInRoom
- QuestInfo.cs
- CmdPlayersInMyCellLessThan
- CmdQuestAvailable
- CmdQuestNotAvailable
- CmdSkillIsNotAvailable
- CmdTargetAuraIsActive

## God Nodes (most connected - your core abstractions)
1. `BotManager` - 252 edges
2. `Root` - 190 edges
3. `IBotEngine` - 186 edges
4. `IBotCommand` - 175 edges
5. `Grimoire.Game` - 155 edges
6. `StatementCommand` - 103 edges
7. `Grimoire.Botting.Commands.Misc.Statements` - 97 edges
8. `Grimoire.UI` - 87 edges
9. `Player` - 80 edges
10. `Configuration` - 79 edges

## Surprising Connections (you probably didn't know these)
- `Private Vulnerability Reporting` --semantically_similar_to--> `Private Security Reporting`  [INFERRED] [semantically similar]
  SECURITY.md → .github/ISSUE_TEMPLATE/config.yml
- `Legacy Compatibility Guardrails` --semantically_similar_to--> `Legacy GBot Compatibility`  [INFERRED] [semantically similar]
  docs/code-performance-audit-v1.1.0.md → README.md
- `User Data and Legacy Format Preservation` --semantically_similar_to--> `Legacy GBot Compatibility`  [INFERRED] [semantically similar]
  docs/releases/v1.2.0.md → README.md
- `Release Supply-Chain Hardening` --conceptually_related_to--> `Tagged Windows Build`  [INFERRED]
  docs/code-performance-audit-v1.1.0.md → .github/workflows/tagged-build.yml
- `Private Registration-Free Flash Runtime` --semantically_similar_to--> `App-Local Flash Runtime`  [INFERRED] [semantically similar]
  app/Runtime/Flash/README.md → README.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Opposed Fantasy Factions** — app_resources_home_images_aqworlds_master_header_heroic_adventurer_party, app_resources_home_images_aqworlds_master_header_undead_monster_horde, app_resources_home_images_aqworlds_master_header_good_versus_evil_confrontation [INFERRED 0.85]
- **Volcanic Dragon Battle Tableau** — app_resources_home_images_ashfall_dragons_black_fire_dragon, app_resources_home_images_ashfall_dragons_multi_headed_brown_hydra, app_resources_home_images_ashfall_dragons_foreground_armored_dragon, app_resources_home_images_ashfall_dragons_volcanic_ashfall_landscape [INFERRED 0.85]
- **Fantasy Brand Composition** — docs_assets_grimoire_github_banner_grimoire_exodus_wordmark, docs_assets_grimoire_github_banner_enchanted_grimoire_emblem, docs_assets_grimoire_github_banner_dragon_guardians, docs_assets_grimoire_github_banner_celestial_arcane_symbols, docs_assets_grimoire_github_banner_dark_fantasy_identity [INFERRED 0.85]
- **Arcane Grimoire Brand Composition** — app_resources_brand_grimoire_emblem_grimoire_emblem, app_resources_brand_grimoire_emblem_open_spellbook, app_resources_brand_grimoire_emblem_celestial_magic, app_resources_brand_grimoire_emblem_arcane_energy [INFERRED 0.85]
- **Legacy Compatibility Contract** — readme_legacy_gbot_compatibility, docs_code_performance_audit_v1_1_0_legacy_guardrails, docs_improvement_research_v1_2_0_compatibility_rules, docs_releases_v1_2_0_compatibility_preservation [INFERRED 0.95]
- **Private Flash Compatibility Stack** — readme_app_local_flash_runtime, app_runtime_flash_readme_private_flash_runtime, docs_embedded_flash_runtime_private_activex_selection, docs_embedded_flash_runtime_runtime_safeguards, security_trusted_aqw_content_only [INFERRED 0.95]
- **Verified Release Delivery** — _github_workflows_tagged_build_tag_validation, _github_workflows_tagged_build_package_build, _github_workflows_tagged_build_smoke_tests, _github_workflows_tagged_build_release_publication, readme_official_release_distribution, security_official_downloads_only [INFERRED 0.95]

## Communities (237 total, 112 thin omitted)

### Community 0 - "Bot Manager Editing"
Cohesion: 0.03
Nodes (9): BotManager, ActiveBotEngine, CurrentBotName, CurrentBotPath, CustomClassName, CustomGuild, CustomName, Instance (+1 more)

### Community 1 - "Bot Command Engine"
Cohesion: 0.05
Nodes (5): IBotEngine, Configuration, CurrentConfiguration, Index, IsRunning

### Community 2 - "StatementCommand"
Cohesion: 0.03
Nodes (40): CmdAvailableMonstersGreaterThan, CmdEquipped, CmdFactionRankGreaterThan, CmdInCombat, CmdInHouse, CmdInInventory, CmdInTemp, CmdIntEqualInt (+32 more)

### Community 3 - "CmdEquipSet"
Cohesion: 0.18
Nodes (4): BotJson, CmdEquipSet, ItemName, LegacyGbotCompatibilitySmoke

### Community 4 - "Main Window Navigation"
Cohesion: 0.03
Nodes (18): QuickBotStatusMenuItem, Root, AppVersion, Client, CreateParams, customTravel, enableCapture, GameLoadProgress (+10 more)

### Community 5 - "Flash Game Data"
Cohesion: 0.03
Nodes (41): Bank, AvailableSlots, HasBankOpen, IsBankLoaded, Items, TotalSlots, UsedSlots, Player (+33 more)

### Community 6 - "system_threading_tasks"
Cohesion: 0.05
Nodes (3): Grimoire.Botting.Commands.Misc.Statements, Grimoire.Botting.Commands.Combat, Grimoire.Game

### Community 7 - "Shared Game Utilities"
Cohesion: 0.09
Nodes (4): Grimoire.Networking.Handlers, Grimoire.Tools, Grimoire.Botting, Grimoire.Game.Data

### Community 8 - "Bank Item Serialization"
Cohesion: 0.03
Nodes (54): BankItem, Bank, Category, CharItemId, CharItemID, Coins, Cost, Description (+46 more)

### Community 9 - "IBotCommand"
Cohesion: 0.04
Nodes (29): CmdCancelAutoAttack, CmdCancelTarget, CmdGetDrop, ItemName, CmdSetSpawn, CmdBankList, CmdBlank, CmdClearTemp (+21 more)

### Community 11 - "Hotkeys"
Cohesion: 0.10
Nodes (8): Hotkey, ActionIndex, Key, Text, Hotkeys, configPath, Instance, InputBox

### Community 12 - "Bot Runtime Configuration"
Cohesion: 0.04
Nodes (50): Configuration, AFK, Author, AutoRelogin, AutoSaveState, BankOnStop, Boosts, BotDelay (+42 more)

### Community 13 - "UpdateService"
Cohesion: 0.05
Nodes (31): AutoBuyBack, Password, Username, BuyBackPage, EventArgument, EventValidation, ViewState, ViewStateGenerator (+23 more)

### Community 14 - "PlayerInfo"
Cohesion: 0.05
Nodes (36): BotData, BotCell, BotMap, BotPad, BotQuests, BotSkill, BotState, SkillSet (+28 more)

### Community 15 - "Account Locker Bridge"
Cohesion: 0.09
Nodes (16): AccountRecord, Id, Label, ProtectedPassword, Username, AccountSummary, Id, Label (+8 more)

### Community 16 - "IJsonMessageHandler"
Cohesion: 0.11
Nodes (14): HandlerGetQuests, HandledCommands, HandlerArea, HandledCommands, HandlerBuyItem, HandledCommands, HandlerLoadShop, HandledCommands (+6 more)

### Community 17 - "Core Application Utilities"
Cohesion: 0.10
Nodes (5): GrimoirePluginEntry, Grimoire.Updater, Grimoire.Tools.Plugins, Grimoire, Grimoire.Utils

### Community 18 - "Quest Data Management"
Cohesion: 0.05
Nodes (35): Quest, CanComplete, ClassPointsReward, Description, EndText, ExperienceReward, Faction, FactionId (+27 more)

### Community 19 - "Bot Execution Lifecycle"
Cohesion: 0.07
Nodes (14): Bot, Configuration, CurrentConfiguration, DropDelay, DropsInInventory, Index, IsRunning, selectedIndex (+6 more)

### Community 20 - "Bot Options and World"
Cohesion: 0.05
Nodes (27): OptionsManager, AFK, _antiAfk, ChangeChat, DisableAnimations, EnemyMagnet, HandlerAFK1, HandlerHideRoom (+19 more)

### Community 21 - "StartupDiagnostics"
Cohesion: 0.19
Nodes (4): StartupDiagnostics, PreviousSession, StartupLogFile, StartupDiagnosticsSmoke

### Community 22 - "Inventory Drop Handling"
Cohesion: 0.05
Nodes (32): InventoryItem, Category, CharItemId, Cost, Description, DropChance, EquipmentSlot, File (+24 more)

### Community 23 - "Travel and Movement"
Cohesion: 0.12
Nodes (4): Travel, Instance, travelNames, travels

### Community 25 - "Combined Inventory Serialization"
Cohesion: 0.05
Nodes (33): InventoryItemCombined, Category, CharItemId, Coins, Cost, Description, DropChance, File (+25 more)

### Community 26 - "Avatar Progression"
Cohesion: 0.06
Nodes (32): Avatar, AccessLevel, AccessType, AFK, Auras, Cell, Class, ClassRank (+24 more)

### Community 27 - "Command Node Editor"
Cohesion: 0.10
Nodes (3): NodeEditor, TypedCommand, Notepad

### Community 28 - "IContainer"
Cohesion: 0.05
Nodes (9): EyeDropper, Instance, PacketLogger, Instance, PacketTamperer, Instance, RawCommandEditor, Content (+1 more)

### Community 29 - "Extended Avatar Factions"
Cohesion: 0.06
Nodes (29): ExtendedAvatar, ActivationFlag, AdventureCoin, Bank, BankSlots, BoostCP, BoostGold, BoostRep (+21 more)

### Community 30 - "XT Proxy Messaging"
Cohesion: 0.12
Nodes (16): HandlerAFK, HandledCommands, HandlerChat, HandledCommands, HandlerWarningsXt, HandledCommands, HandlerConnection, HandledCommands (+8 more)

### Community 31 - "World State Scanning"
Cohesion: 0.06
Nodes (27): World, ActiveCutscene, AllCellInfo, AuraSyncRoot, AvailableMonsters, AvatarsInMap, AvatarsInMyCell, Cells (+19 more)

### Community 32 - "JSON Quest Conversion"
Cohesion: 0.13
Nodes (6): BoolConverter, IntStringConverter, QuestReqConverter, QuestRewardConverter, StringBoolConverter, TrimConverter

### Community 34 - "JsonMessage"
Cohesion: 0.13
Nodes (12): HandlerDrops, HandledCommands, HandlerAura, HandledCommands, HandlerInfo, HandledCommands, HandlerSkills, HandledCommands (+4 more)

### Community 36 - "Miscellaneous Bot Commands"
Cohesion: 0.06
Nodes (3): Grimoire.Botting.Commands.Misc, Grimoire.Networking, Grimoire.Botting.Commands.Map

### Community 38 - "Skill Configuration UI"
Cohesion: 0.09
Nodes (12): Skill, ComparisonType, Index, SafeHp, SafeMp, SafePercentage, Text, Type (+4 more)

### Community 39 - "Required Quest Items"
Cohesion: 0.08
Nodes (25): itemsR, bPTR, Category, Coins, Cost, Description, HouseItem, Icon (+17 more)

### Community 40 - "Selectable Quest Items"
Cohesion: 0.08
Nodes (25): itemsS, bPTR, Category, Coins, Cost, Description, HouseItem, Icon (+17 more)

### Community 41 - "UserFriendlyCommandEditor"
Cohesion: 0.07
Nodes (4): UserFriendlyCommandEditor, cmd, cmdObj, commandEditor

### Community 42 - "CmdBlank3"
Cohesion: 0.11
Nodes (7): CmdBlank3, Alpha, B, G, R, Text, FontInfo

### Community 43 - "Map Cell Data"
Cohesion: 0.09
Nodes (22): Cell, MapItems, Name, Pads, Map, Area, Cells, EmptyCells (+14 more)

### Community 44 - "Temporary Inventory"
Cohesion: 0.09
Nodes (14): TempInventory, Items, TempItem, Cost, Description, Id, IsAcItem, IsMemberOnly (+6 more)

### Community 46 - "Item"
Cohesion: 0.24
Nodes (5): ISetInterface, Item, Name, Type, SetItem

### Community 47 - "CmdUseSkill2"
Cohesion: 0.25
Nodes (7): CmdUseSkill2, Index, Monster, SafeHp, SafeMp, Skill, Wait

### Community 48 - "Non Closable Tab Controls"
Cohesion: 0.15
Nodes (9): ResizableUserControl, Bottom, BottomLeft, BottomRight, Left, Right, Top, TopLeft (+1 more)

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

### Community 55 - "CosmeticForm"
Cohesion: 0.09
Nodes (9): CosmeticEquipment, ID, Link, Meta, Slot, SWFFile, Type, CosmeticForm (+1 more)

### Community 56 - "Monster State Data"
Cohesion: 0.11
Nodes (18): Monster, Alive, Auras, Cell, CurrentState, Health, Id, Level (+10 more)

### Community 57 - ".IsActionAvailable"
Cohesion: 0.08
Nodes (6): Shop, Id, IsShopLoaded, Items, Location, Name

### Community 58 - "Plugin Contracts"
Cohesion: 0.31
Nodes (5): GrimoirePlugin, Author, Description, LastError, Name

### Community 59 - ".IsActionAvailable"
Cohesion: 0.15
Nodes (4): CmdJoin, Cell, Map, Pad

### Community 60 - "AutoRelogin"
Cohesion: 0.08
Nodes (18): Server, Ip, IsChatRestricted, IsMemberOnly, IsOnline, Language, List, MaxCount (+10 more)

### Community 61 - "Release Build Tooling"
Cohesion: 0.23
Nodes (14): Assert-FileHash(), Complete-BrandedOutput(), Get-BundledFlashRuntime(), Get-ExodusProductVersion(), Get-MSBuildPath(), Get-WixToolset(), Invoke-ExodusBuild(), Invoke-NuGetRestore() (+6 more)

### Community 62 - "Game Action Locks"
Cohesion: 0.12
Nodes (15): LockActions, AcceptQuest, BuyItem, DoIA, EquipItem, GetMapItem, LoadEnhShop, LoadHairShop (+7 more)

### Community 65 - "JavaScript Tooling"
Cohesion: 0.12
Nodes (15): description, devDependencies, @commitlint/cli, @commitlint/config-conventional, husky, name, private, scripts (+7 more)

### Community 67 - ".ExecuteCommand"
Cohesion: 0.20
Nodes (6): CmdKill, Monster, Packet, ChatCommand, cmdCombat, Engine

### Community 69 - "Flash"
Cohesion: 0.05
Nodes (6): Extensions, Line, Flash, Instance, ControlUtils, TimeLimiter

### Community 71 - "IGrimoirePlugin"
Cohesion: 0.25
Nodes (3): IGrimoirePlugin, Author, Description

### Community 72 - "Shop Packet Handling"
Cohesion: 0.18
Nodes (10): ShopInfo, Field, House, Id, Index, Items, Location, Member (+2 more)

### Community 74 - "LogForm"
Cohesion: 0.09
Nodes (5): DebugLogger, LogForm, Instance, SelectedLog, BoundedTextBuffer

### Community 76 - "Performance Audit Findings"
Cohesion: 0.18
Nodes (7): Grimoire Exodus v1.1.0 Code and Performance Audit, 2026-10-15 Implementation Outcome, Legacy Compatibility Guardrails, Future-Work Compatibility Rules, Grimoire Exodus v1.2.0 Release Notes, Bot Runtime Responsiveness and Recovery, Legacy GBot Compatibility

### Community 78 - "CmdToggleProvokeInMap"
Cohesion: 0.33
Nodes (3): CmdToggleProvokeInMap, ProvokePacket, Type

### Community 79 - "Base Item Data"
Cohesion: 0.15
Nodes (12): ItemBase, Category, Coins, Description, File, ID, Link, MaxStack (+4 more)

### Community 80 - "Item Drop Stack"
Cohesion: 0.18
Nodes (4): DropStack, Count, InProcess, MarkedDrops

### Community 81 - "Application Settings"
Cohesion: 0.17
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

### Community 88 - ".RecordException"
Cohesion: 0.10
Nodes (3): Program, AppDataReset, AppPreferences

### Community 89 - "Bot Library Experience"
Cohesion: 0.20
Nodes (10): Bundled Official GBots, Community GBot Distribution Guide, Encrypted Account Locker UI, applyState, AQW Loading Progress UI, View-Only Bot Manager UI, Grimoire Home Surface, renderAccounts (+2 more)

### Community 90 - "PluginManager"
Cohesion: 0.19
Nodes (3): PluginManager, Instance, LastError

### Community 91 - "Equipment Type Definitions"
Cohesion: 0.18
Nodes (10): EquipType, Armor, Cape, Class, Ground, Helm, None, Offhand (+2 more)

### Community 92 - "Quest Completion Data"
Cohesion: 0.18
Nodes (10): CompletedQuest, Id, Name, Reward, Success, QuestReward, ClassPoints, Experience (+2 more)

### Community 94 - "Integer Command Values"
Cohesion: 0.15
Nodes (8): CmdInt, Int, type, Value, Types, Lower, Set, Upper

### Community 96 - "Release Publishing Pipeline"
Cohesion: 0.22
Nodes (7): MSI and Portable Package Build, Checksummed GitHub Release Publication, Release Compatibility Smoke Tests, Release Tag Validation, Tagged Windows Build, Modern Installation and Update Foundation, Grimoire Exodus v1.1.0 Release Notes

### Community 97 - "Dictionary"
Cohesion: 0.07
Nodes (10): BotClientConfig, Contents, file, Instance, Config, Contents, file, Instance (+2 more)

### Community 98 - "Loaders"
Cohesion: 0.10
Nodes (11): Loaders, Instance, TreeType, Type, BankItems, InventoryItems, Monsters, QuestIDs (+3 more)

### Community 99 - "Indexed Bot Commands"
Cohesion: 0.22
Nodes (7): CmdIndex, Index, Type, IndexCommand, Down, Goto, Up

### Community 101 - "Resource Localization"
Cohesion: 0.22
Nodes (6): Resources, Culture, defaulttext, GrimoireIcon, ResourceManager, statementcmds

### Community 103 - "Download Security Policy"
Cohesion: 0.29
Nodes (4): Issue Intake Configuration, Private Security Reporting, Security Policy, Trusted AQW Content Only

### Community 104 - "Skill Command Execution"
Cohesion: 0.29
Nodes (6): CmdUseSkill, Index, SafeHp, SafeMp, Skill, Wait

### Community 105 - "CmdCompleteQuest2"
Cohesion: 0.33
Nodes (5): CmdCompleteQuest2, ItemID, _itemID, QuestID, _questID

### Community 106 - "Avatar Access Levels"
Cohesion: 0.29
Nodes (7): Access, Developer, LeadDeveloper, Moderator, Player, Staff, Tester

### Community 107 - "List"
Cohesion: 0.10
Nodes (11): House, Items, TotalSlots, Quests, AcceptedQuests, CompletedQuests, LoadedQuests, QuestTree (+3 more)

### Community 108 - ".RefreshHomeState"
Cohesion: 0.15
Nodes (5): AccountCredentials, Id, Label, Password, Username

### Community 109 - "Client Status Indicators"
Cohesion: 0.25
Nodes (8): StatusIcon, Cpu, EyeClosed, EyeOpen, Network, Settings, UpdateAvailable, Uptime

### Community 110 - "Improvement Research Roadmap"
Cohesion: 0.33
Nodes (3): Bot Doctor, Compatibility Center, Grimoire Exodus v1.2.0 Improvement and Feature Research

### Community 111 - "State"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

### Community 113 - "Item Whitelist State"
Cohesion: 0.33
Nodes (6): state, Add, Clear, Off, On, Remove

### Community 114 - "Cell Movement Command"
Cohesion: 0.33
Nodes (5): CmdMoveToCell, Cell, _cell, Pad, _pad

### Community 115 - "CmdAcceptQuest"
Cohesion: 0.29
Nodes (4): CmdAcceptQuest, Quest, CmdCompleteQuest, Quest

### Community 117 - "Message"
Cohesion: 0.20
Nodes (6): Message, Command, RawContent, Send, XmlMessage, Body

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

### Community 122 - "CmdBuy2"
Cohesion: 0.40
Nodes (4): CmdBuy2, ItemName, Manual, ShopId

### Community 123 - "CmdBuyFastByID"
Cohesion: 0.33
Nodes (5): CmdBuyFastByID, ItemID, Manual, ShopID, ShopItemID

### Community 124 - "Load Bot Command"
Cohesion: 0.40
Nodes (4): CmdLoadBot, BotFileName, BotFilePath, _path

### Community 125 - "Packet Spam Command"
Cohesion: 0.29
Nodes (4): CmdPacketlist2, Delay, Packet, Type

### Community 128 - "Diagnostics Path Tests"
Cohesion: 0.40
Nodes (4): AppPaths, CrashReportsDirectory, LogsDirectory, RecoveryDirectory

### Community 129 - "SettingsForm"
Cohesion: 0.12
Nodes (5): HotkeyBindingDefinition, ActionIndex, DefaultKey, DisplayName, SettingsForm

### Community 133 - "CmdAcceptQuest2"
Cohesion: 0.50
Nodes (3): CmdAcceptQuest2, QuestID, _questID

### Community 136 - "CmdJoin2"
Cohesion: 0.18
Nodes (9): CmdJoin2, Cell, _cell, Map, _map, Pad, _pad, Room (+1 more)

### Community 137 - "Inventory"
Cohesion: 0.29
Nodes (5): Inventory, AvailableSlots, Items, MaxSlots, UsedSlots

### Community 138 - "BotUtilities"
Cohesion: 0.22
Nodes (5): BotUtilities, ActiveBot, BusyClient, HasLoadedQuests, ShouldUseSkill

### Community 139 - "Avatar State Values"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

### Community 140 - "CmdWhitelist"
Cohesion: 0.22
Nodes (3): CmdWhitelist, Item, State

### Community 142 - "AppPreferenceSnapshot"
Cohesion: 0.33
Nodes (5): AppPreferenceSnapshot, ConfirmOnClose, HideDeviceStats, MinimizeToTray, SkipHomePage

### Community 143 - "Grimoire Emblem Artwork"
Cohesion: 0.67
Nodes (4): Arcane Energy, Celestial Magic, Grimoire Emblem, Open Spellbook

### Community 150 - "CmdWalk"
Cohesion: 0.22
Nodes (6): CmdWalk, Type, X, _x, Y, _y

### Community 152 - "CmdBuyByID"
Cohesion: 0.33
Nodes (5): CmdBuyByID, ItemID, Manual, ShopID, ShopItemID

### Community 154 - "CmdLog"
Cohesion: 0.25
Nodes (4): CmdLog, Clear, Debug, Text

### Community 155 - "CmdQuestlist"
Cohesion: 0.14
Nodes (10): CmdQuestlist, ItemID, _itemID, QuestID, _questID, state, State, Add (+2 more)

### Community 159 - "CmdBankSwap"
Cohesion: 0.50
Nodes (3): CmdBankSwap, BankItemName, InventoryItemName

### Community 161 - "CmdClientMessage"
Cohesion: 0.50
Nodes (3): CmdClientMessage, IsWarning, Messages

### Community 162 - "CmdBankTransfer"
Cohesion: 0.50
Nodes (3): CmdBankTransfer, ItemName, TransferFromBank

### Community 163 - "Grimoire Exodus v1.3.0"
Cohesion: 0.40
Nodes (4): Compatibility, Downloads, Grimoire Exodus v1.3.0, Highlights

### Community 166 - "GrimoireForm"
Cohesion: 0.14
Nodes (3): GrimoireForm, SafeWinEventHookHandle, TitleBarThemeSmoke

### Community 170 - "CmdBuy"
Cohesion: 0.50
Nodes (3): CmdBuy, ItemName, ShopId

### Community 177 - "CmdBuyBack"
Cohesion: 0.50
Nodes (3): CmdBuyBack, ItemName, PageNumberCap

### Community 181 - "CmdBuyFast"
Cohesion: 0.50
Nodes (3): CmdBuyFast, ItemName, Manual

### Community 183 - "CmdEquip"
Cohesion: 0.50
Nodes (3): CmdEquip, ItemName, Safe

### Community 185 - "CmdChange"
Cohesion: 0.50
Nodes (3): CmdChange, Guild, Text

## Knowledge Gaps
- **1010 isolated node(s):** `Monster`, `Monster`, `Packet`, `Monster`, `ItemName` (+1005 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1445 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **112 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `BotManager` connect `Bot Manager Editing` to `Bot Command Engine`, `StatementCommand`, `SettingsForm`, `CmdEquipSet`, `Main Window Navigation`, `CmdWhitelist`, `Quest Data Management`, `CmdWalk`, `Travel and Movement`, `CmdLog`, `CmdQuestlist`, `IContainer`, `Command Node Editor`, `.SetDRM`, `Bot File Loading`, `Skill Configuration UI`, `GrimoireForm`, `UserFriendlyCommandEditor`, `CmdBlank3`, `Packet Proxy Logging`, `Toolbar Status Styling`, `AutoRelogin`, `Packet Spammer Interface`, `Grabber`, `Flash`, `NonClosableVSTabControl`, `LogForm`, `BankForm`, `CmdToggleProvokeInMap`, `.MoveToSelfCell`, `CmdRest`, `PluginManager`, `Bot Manager Runtime Controls`, `Integer Command Values`, `Loaders`, `Bot Configuration Import`, `CmdToggleProvoke`, `List`, `BotManager.cs`, `.lstCommands_KeyPress`, `Packet Spam Command`, `.rtbInfo_LinkClicked`?**
  _High betweenness centrality (0.140) - this node is a cross-community bridge._
- **What connects `Monster`, `Monster`, `Packet` to the rest of the system?**
  _1010 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Bot Manager Editing` be split into smaller, more focused modules?**
  _Cohesion score 0.03296321998612075 - nodes in this community are weakly interconnected._
- **Why does `Root` connect `Main Window Navigation` to `SettingsForm`, `UpdateService`, `Account Locker Bridge`, `Core Application Utilities`, `CancellationToken`, `StartupDiagnostics`, `Travel and Movement`, `Command Node Editor`, `IContainer`, `.SetDRM`, `.MenuMain_MouseDown`, `GrimoireForm`, `UserFriendlyCommandEditor`, `CmdBlank3`, `.ApplyPreferences`, `Startup Recovery State`, `Toolbar Status Styling`, `AutoRelogin`, `.HomeBrowser_Navigating`, `.QuickBotDirectoryChanged`, `.QuickBotWatcherError`, `Flash`, `Grabber`, `NonClosableVSTabControl`, `BankForm`, `IContainer`, `Purple Menu Rendering`, `.MoveToSelfCell`, `Aura State Handling`, `.RecordException`, `PluginManager`, `GrimoireHomeBridge`, `Loaders`, `List`, `.RefreshHomeState`, `Client Status Indicators`?**
  _High betweenness centrality (0.120) - this node is a cross-community bridge._
- **Should `Bot Command Engine` be split into smaller, more focused modules?**
  _Cohesion score 0.04971264367816092 - nodes in this community are weakly interconnected._
- **Why does `IBotCommand` connect `IBotCommand` to `Bot Manager Editing`, `Bot Command Engine`, `StatementCommand`, `CmdEquipSet`, `Shared Game Utilities`, `Hotkeys`, `Bot Runtime Configuration`, `Bot Execution Lifecycle`, `Travel and Movement`, `Bot File Loading`, `Skill Configuration UI`, `CmdBlank3`, `CmdUseSkill2`, `Packet Command Handling`, `.IsActionAvailable`, `.ExecuteCommand`, `CmdToggleProvokeInMap`, `Kill For Item Logic`, `CmdRest`, `Bot Manager Runtime Controls`, `Integer Command Values`, `Indexed Bot Commands`, `CmdToggleProvoke`, `Skill Command Execution`, `CmdCompleteQuest2`, `Cell Movement Command`, `CmdAcceptQuest`, `CmdBotDelay`, `CmdBuy2`, `CmdBuyFastByID`, `Load Bot Command`, `Packet Spam Command`, `CmdSetFPS`, `CmdPlayersInCellGreaterThan`, `CmdAvailableMonstersLessThan`, `CmdGoldGreaterThan`, `CmdAcceptQuest2`, `CmdIntGreaterThan`, `CmdIntLessThan`, `CmdJoin2`, `BotUtilities`, `CmdWhitelist`, `CmdIsMaxStack`, `CmdItemPickupable`, `CmdLoad`, `CmdWalk`, `CmdMapItem`, `CmdBuyByID`, `CmdManaGreaterThan`, `CmdLog`, `CmdQuestlist`, `CmdMapIs`, `CmdNotEquipped`, `CmdPlayerAuraIsNotActive`, `CmdBankSwap`, `CmdPlayerAuraValIsGreaterThan`, `CmdClientMessage`, `CmdBankTransfer`, `Cell Inequality Condition`, `Player Cell Exclusion`, `CmdPlayerNotEquippedClass`, `Player Count Lesser Condition`, `CmdBuy`, `CmdTargetAuraStringValIsNot`, `CmdDelay`, `CmdBuyBack`, `Gold Lesser Condition`, `Health Greater Condition`, `Health Lesser Condition`, `CmdBuyFast`, `CmdCellIs.cs`, `CmdEquip`, `CmdTargetAuraValIsGreaterThan`, `CmdChange`, `CmdGotoLabel`, `CmdVisibleMonstersLessThan`, `Mana Lesser Condition`, `Monster Health Condition`, `Monster Absence Condition`, `CmdPlayerIsInCell`, `CmdAttack`, `CmdSkillSet`, `CmdIsMember`, `CmdIsNotMaxStack`, `CmdHealthPercentageGreaterThan`, `CmdLoad2`, `CmdMapItem2`, `CmdSell`, `CmdUseBoost`, `CmdLoadMapSwf`, `CmdBeep`, `CmdBlank2`, `CmdIsNotMember`, `CmdLevelGreaterThan`, `CmdPlayersLessThan`, `CmdBotDelay2`, `CmdNotInBank`, `CmdNotInHouse`, `CmdPlayerIsNotInMyCell`, `CmdPlayerAuraStringValIs`, `CmdQuestInProgress`, `CmdQuestNotCompleted`, `CmdPlayerAuraValIsLessThan`, `CmdVisibleMonstersGreaterThan`, `CmdPlayerNotInRoom`, `CmdPlayersInMyCellLessThan`, `CmdQuestAvailable`, `CmdQuestNotAvailable`, `CmdSkillIsNotAvailable`, `CmdTargetAuraIsActive`?**
  _High betweenness centrality (0.109) - this node is a cross-community bridge._
- **Should `StatementCommand` be split into smaller, more focused modules?**
  _Cohesion score 0.02702702702702703 - nodes in this community are weakly interconnected._