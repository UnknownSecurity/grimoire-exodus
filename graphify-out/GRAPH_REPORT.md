# Graph Report - grimoire-exodus  (2026-10-16)

## Corpus Check
- 356 files · ~262,310 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 87 file(s) not represented in the graph (top: .as 23, .resx 22, .dll 9)

## Summary
- 3955 nodes · 8663 edges · 278 communities (146 shown, 132 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 553 edges (avg confidence: 0.79)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `38c89db0`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Bot Manager Editing
- Task
- StatementCommand
- BoundedTextBuffer
- LogForm
- StatementCommand
- system_threading_tasks
- IBotCommand
- Bank Item Serialization
- UpdateService.cs
- Hotkeys
- Bot Runtime Configuration
- UpdateService
- AutoRelogin
- .LoadBotFromFile
- Grimoire.Utils
- Flash
- Quest Data Management
- Bot Execution Lifecycle
- .ExecuteCommand
- StartupDiagnostics
- Inventory Drop Handling
- Travel and Movement
- Set
- Combined Inventory Serialization
- Avatar Progression
- NodeEditor
- PacketSpammer
- Extended Avatar Factions
- XT Proxy Messaging
- World State Scanning
- Type
- BotData
- JsonMessage
- Bot File Loading
- Grimoire.Botting.Commands.Misc
- .RenderQuickBots
- CommandColorForm
- GrimoireForm
- Selectable Quest Items
- UserFriendlyCommandEditor
- CmdBlank3
- Map Cell Data
- Temporary Inventory
- GrimoireExodusApp.kt
- Set
- .IsActionAvailable
- Portable Update Application
- Application Path Management
- DarkButton
- Packet Proxy Logging
- Packet Command Handling
- Color
- GrimoirePlugin
- Monster State Data
- PacketSpammer
- CommandNode
- .Execute
- CmdBlank3
- Release Build Tooling
- Game Action Locks
- Command Color Editor
- Composable
- JavaScript Tooling
- Grimoire Exodus for Android — feasibility and delivery plan
- .ExecuteCommand
- Grabber
- LogForm
- CancellationToken
- LogForm
- .Execute
- LegacyGbotReader.kt
- IJsonMessageHandler
- JsonMessage
- Grimoire Exodus v1.1.0 Code and Performance Audit
- ExodusTheme.kt
- DllImport
- Base Item Data
- Item Drop Stack
- BotManager.cs
- Purple Menu Rendering
- Flash Runtime Architecture
- Kill For Item Logic
- GameCapability
- AppPreferenceSnapshot
- .RecordException
- Bot Library Experience
- .HandleAsync
- Equipment Type Definitions
- Quest Completion Data
- .ApplyConfiguration
- Integer Command Values
- ResizableUserControl
- Release Publishing Pipeline
- Dictionary
- Type
- IContainer
- CmdAcceptQuest
- BotManager.cs
- BankForm
- Grimoire Exodus
- PluginManager
- Resources
- CmdQuestlist
- List
- .RefreshHomeState
- Grimoire Exodus v1.3.0 Release Notes
- State
- Type
- IEnumerable
- Message
- OfflineGameTransportTest.kt
- Message
- AQW Header Artwork
- CmdWhitelist
- MainActivity.kt
- Dark Fantasy Branding
- CmdBuy2
- BotUtilities
- Resources
- AutoBuyBack.cs
- Loaders
- Grimoire Exodus AIR client
- TitleBarThemeSmoke
- SettingsForm
- NonClosableVSTabControl
- KeyboardHook
- CmdGoldGreaterThan
- CmdBankSwap
- state
- CmdJoin2
- Inventory
- CmdPacketlist2
- Avatar State Values
- .ApplySettings
- CmdToggleProvoke
- Grimoire Emblem Artwork
- Updater Merge Tests
- Solution Project Layout
- BridgeContractTest.kt
- CmdIsMaxStack
- IBotCommand
- 21. Sources
- CmdWalk
- CmdPacketlist2
- CmdGetDrop
- CmdCellIs
- CmdLog
- CmdQuestlist
- gradlew
- CmdBuyFast
- Cell
- RawCommandEditor
- CmdItemNotPickupable
- .Main
- .MenuMain_MouseDown
- CmdIntEqualInt
- GrimoireForm
- DPSForm
- .CreateSettings
- CmdIntIsNot
- CmdListInInv
- PacketLogger
- CmdUseSkill2
- Feature Request Workflow
- KeyboardHook
- CmdAcceptQuest2
- .SelectUpdate
- CmdAcceptQuest
- CmdQuestNotCompleted
- CmdQuestNotInProgress
- State
- CmdRest
- CmdBuy2
- CmdEquipSet
- CmdBankSwap
- CmdBankTransfer
- CmdBuy
- CmdBuyFast
- CmdEquip
- CmdClientMessage
- CmdAcceptQuest2
- CmdSell
- CmdPlayerIsNotInCell
- House
- State
- .InputDialog
- CmdBotDelay
- UpdateAsset
- 17. Stop/go gates
- CmdAvailableMonstersGreaterThan
- ReflectUtils
- CmdAttack
- CmdSkillSet
- CmdLoad
- CmdLoad2
- CmdMapItem
- CmdMapItem2
- CmdSell
- CmdUseBoost
- CmdLoadMapSwf
- CmdAvailableMonstersLessThan
- CmdBeep
- CmdMonsterHealthLessThan
- CmdBlank2
- CmdBotDelay
- CmdBotDelay2
- 3. Verified research findings
- CmdPlayerAuraIsActive
- CmdTargetAuraStringValIs
- CmdSetFPS
- CmdStopBotWithMessage
- CmdPlayersInCellGreaterThan
- CmdPlayersInCellLessThan
- CmdAvailableMonstersLessThan
- CmdGetClassWithVar
- CmdGoldGreaterThan
- CmdGoldLessThan
- CmdHealthGreaterThan
- CmdHealthLessThan
- Visual design direction
- CmdIntNotEqualInt
- CmdSkillIsAvailable
- CmdIsMaxStack
- CmdIsMember
- CmdIsNotMaxStack
- CmdItemNotPickupable
- CmdListInInv
- CmdVisibleMonstersGreaterThan
- CmdManaLessThan
- CmdPlayersLessThan
- CmdQuestInProgress
- Test-TitleBarTheme.ps1
- CmdMonsterNotInRoom
- CmdNotEquipped
- CmdNotInCombat
- CmdPlayerAuraIsNotActive
- CmdPlayerAuraValIs
- CmdPlayerAuraValIsGreaterThan
- CmdPlayerIsInMyCell
- CmdPlayerIsNotInMyCell
- CmdPlayerNotEquippedClass
- CmdQuestInProgress
- CmdTargetAuraIsNotActive
- CmdTargetAuraValIs
- CmdTargetAuraValIsLessThan
- CmdVisibleMonstersLessThan

## God Nodes (most connected - your core abstractions)
1. `BotManager` - 253 edges
2. `Root` - 191 edges
3. `IBotEngine` - 186 edges
4. `IBotCommand` - 176 edges
5. `Grimoire.Game` - 155 edges
6. `StatementCommand` - 103 edges
7. `Grimoire.Botting.Commands.Misc.Statements` - 97 edges
8. `Grimoire.UI` - 87 edges
9. `Configuration` - 80 edges
10. `Player` - 80 edges

## Surprising Connections (you probably didn't know these)
- `User Data and Legacy Format Preservation` --semantically_similar_to--> `Legacy GBot Compatibility`  [INFERRED] [semantically similar]
  docs/releases/v1.2.0.md → README.md
- `8. Reuse matrix` --references--> `Configuration`  [INFERRED]
  docs/android-port-plan.md → app/Grimoire.Botting/Configuration.cs
- `8. Reuse matrix` --references--> `IBotCommand`  [INFERRED]
  docs/android-port-plan.md → app/Grimoire.Botting/IBotCommand.cs
- `Why not make .NET MAUI the default?` --references--> `BotManager`  [INFERRED]
  docs/android-port-plan.md → app/Grimoire.UI/BotManager.cs
- `8. Reuse matrix` --references--> `GrimoireHomeBridge`  [INFERRED]
  docs/android-port-plan.md → app/Grimoire.UI/GrimoireHomeBridge.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Opposed Fantasy Factions** — app_resources_home_images_aqworlds_master_header_heroic_adventurer_party, app_resources_home_images_aqworlds_master_header_undead_monster_horde, app_resources_home_images_aqworlds_master_header_good_versus_evil_confrontation [INFERRED 0.85]
- **Volcanic Dragon Battle Tableau** — app_resources_home_images_ashfall_dragons_black_fire_dragon, app_resources_home_images_ashfall_dragons_multi_headed_brown_hydra, app_resources_home_images_ashfall_dragons_foreground_armored_dragon, app_resources_home_images_ashfall_dragons_volcanic_ashfall_landscape [INFERRED 0.85]
- **Fantasy Brand Composition** — docs_assets_grimoire_github_banner_grimoire_exodus_wordmark, docs_assets_grimoire_github_banner_enchanted_grimoire_emblem, docs_assets_grimoire_github_banner_dragon_guardians, docs_assets_grimoire_github_banner_celestial_arcane_symbols, docs_assets_grimoire_github_banner_dark_fantasy_identity [INFERRED 0.85]
- **Arcane Grimoire Brand Composition** — app_resources_brand_grimoire_emblem_grimoire_emblem, app_resources_brand_grimoire_emblem_open_spellbook, app_resources_brand_grimoire_emblem_celestial_magic, app_resources_brand_grimoire_emblem_arcane_energy [INFERRED 0.85]
- **Grimoire Exodus Brand Composition** — mobile_app_src_main_res_drawable_nodpi_grimoire_banner_grimoire_exodus_title, mobile_app_src_main_res_drawable_nodpi_grimoire_banner_central_magical_grimoire, mobile_app_src_main_res_drawable_nodpi_grimoire_banner_flanking_dragons, mobile_app_src_main_res_drawable_nodpi_grimoire_banner_celestial_sun_and_moon_symbols [INFERRED 0.85]
- **Arcane Emblem Composition** — mobile_app_src_main_res_drawable_nodpi_grimoire_emblem_open_grimoire, mobile_app_src_main_res_drawable_nodpi_grimoire_emblem_magical_aura, mobile_app_src_main_res_drawable_nodpi_grimoire_emblem_celestial_glyphs, mobile_app_src_main_res_drawable_nodpi_grimoire_emblem_arcane_magic_identity [INFERRED 0.85]
- **Android Cross-Platform Architecture** — docs_android_port_plan_standalone_air_architecture, mobile_readme_compose_management_shell, mobile_readme_air_game_runtime, mobile_air_client_readme_local_callback_registry, shared_readme_language_neutral_boundary, shared_contracts_game_bridge_v1_readme_game_bridge_protocol_v1 [INFERRED 0.95]
- **Fail-Closed Mobile Integration** — mobile_readme_offline_transport_default, docs_android_port_plan_game_transport_contract, docs_android_port_plan_stop_go_gates, shared_contracts_game_bridge_v1_readme_fail_closed_handshake, shared_contracts_game_bridge_v1_readme_provisional_approval_boundary [INFERRED 0.95]
- **Legacy Compatibility Contract** — readme_legacy_gbot_compatibility, docs_code_performance_audit_v1_1_0_legacy_guardrails, docs_improvement_research_v1_2_0_compatibility_rules, docs_releases_v1_2_0_compatibility_preservation [INFERRED 0.95]
- **Legacy GBot Compatibility Chain** — agents_legacy_gbot_contract, readme_legacy_gbot_compatibility, docs_android_port_plan_safe_gbot_import, docs_android_port_plan_legacy_export_strategy, shared_contracts_gbot_v1_readme_legacy_gbot_contract, shared_contracts_gbot_v1_readme_synthetic_compatibility_fixture [INFERRED 0.95]
- **Private Flash Compatibility Stack** — app_runtime_flash_readme_private_flash_runtime, docs_embedded_flash_runtime_private_activex_selection, docs_embedded_flash_runtime_runtime_safeguards, security_trusted_aqw_content_only [INFERRED 0.95]
- **Verified Release Delivery** — _github_workflows_tagged_build_tag_validation, _github_workflows_tagged_build_package_build, _github_workflows_tagged_build_smoke_tests, _github_workflows_tagged_build_release_publication, readme_official_release_distribution, security_official_downloads_only [INFERRED 0.95]

## Communities (278 total, 132 thin omitted)

### Community 0 - "Bot Manager Editing"
Cohesion: 0.03
Nodes (9): BotManager, ActiveBotEngine, CurrentBotName, CurrentBotPath, CustomClassName, CustomGuild, CustomName, Instance (+1 more)

### Community 1 - "Task"
Cohesion: 0.05
Nodes (5): IBotEngine, Configuration, CurrentConfiguration, Index, IsRunning

### Community 2 - "StatementCommand"
Cohesion: 0.03
Nodes (17): Root, AppVersion, Client, CreateParams, customTravel, enableCapture, GameLoadProgress, Instance (+9 more)

### Community 3 - "BoundedTextBuffer"
Cohesion: 0.05
Nodes (3): Grimoire.Botting.Commands.Misc.Statements, Grimoire.Botting.Commands.Combat, Grimoire.Game

### Community 4 - "LogForm"
Cohesion: 0.12
Nodes (3): PacketTamperer, Instance, BoundedTextBuffer

### Community 5 - "StatementCommand"
Cohesion: 0.03
Nodes (42): CmdFactionRankLessThan, CmdHealthPercentageGreaterThan, CmdHealthPercentageLessThan, CmdInBank, CmdInCombat, CmdInHouse, CmdIntGreaterThan, CmdIntIs (+34 more)

### Community 6 - "system_threading_tasks"
Cohesion: 0.09
Nodes (4): Grimoire.Networking.Handlers, Grimoire.Tools, Grimoire.Botting, Grimoire.Game.Data

### Community 7 - "IBotCommand"
Cohesion: 0.04
Nodes (30): CmdCancelAutoAttack, CmdCancelTarget, CmdSetSpawn, CmdBankList, CmdBlank, CmdClearTemp, CmdDelay, Delay (+22 more)

### Community 8 - "Bank Item Serialization"
Cohesion: 0.12
Nodes (4): Grimoire.UI, Grimoire, Grimoire.Botting.Commands.Map, Properties

### Community 10 - "UpdateService.cs"
Cohesion: 0.03
Nodes (54): BankItem, Bank, Category, CharItemId, CharItemID, Coins, Cost, Description (+46 more)

### Community 11 - "Hotkeys"
Cohesion: 0.03
Nodes (41): Bank, AvailableSlots, HasBankOpen, IsBankLoaded, Items, TotalSlots, UsedSlots, Player (+33 more)

### Community 12 - "Bot Runtime Configuration"
Cohesion: 0.08
Nodes (5): GrimoirePluginEntry, NetworkUtils, Grimoire.Updater, Grimoire.Tools.Plugins, Grimoire.Utils

### Community 13 - "UpdateService"
Cohesion: 0.04
Nodes (50): Configuration, AFK, Author, AutoRelogin, AutoSaveState, BankOnStop, Boosts, BotDelay (+42 more)

### Community 14 - "AutoRelogin"
Cohesion: 0.08
Nodes (18): Server, Ip, IsChatRestricted, IsMemberOnly, IsOnline, Language, List, MaxCount (+10 more)

### Community 16 - "Grimoire.Utils"
Cohesion: 0.07
Nodes (22): AccountCredentials, Id, Label, Password, Username, AccountRecord, Id, Label (+14 more)

### Community 17 - "Flash"
Cohesion: 0.05
Nodes (6): Extensions, Line, Flash, Instance, ControlUtils, TimeLimiter

### Community 18 - "Quest Data Management"
Cohesion: 0.04
Nodes (27): OptionsManager, AFK, _antiAfk, ChangeChat, DisableAnimations, EnemyMagnet, HandlerAFK1, HandlerHideRoom (+19 more)

### Community 19 - "Bot Execution Lifecycle"
Cohesion: 0.05
Nodes (35): Quest, CanComplete, ClassPointsReward, Description, EndText, ExperienceReward, Faction, FactionId (+27 more)

### Community 20 - ".ExecuteCommand"
Cohesion: 0.20
Nodes (6): CmdKill, Monster, Packet, ChatCommand, cmdCombat, Engine

### Community 21 - "StartupDiagnostics"
Cohesion: 0.07
Nodes (25): OfflineGameTransport, Accepted, ActionResult, ConnectionState, Connecting, Failed, Incompatible, Offline (+17 more)

### Community 22 - "Inventory Drop Handling"
Cohesion: 0.05
Nodes (32): InventoryItem, Category, CharItemId, Cost, Description, DropChance, EquipmentSlot, File (+24 more)

### Community 23 - "Travel and Movement"
Cohesion: 0.05
Nodes (33): InventoryItemCombined, Category, CharItemId, Coins, Cost, Description, DropChance, File (+25 more)

### Community 24 - "Set"
Cohesion: 0.10
Nodes (10): ResizableUserControl, Bottom, BottomLeft, BottomRight, Left, Right, Top, TopLeft (+2 more)

### Community 25 - "Combined Inventory Serialization"
Cohesion: 0.12
Nodes (4): Travel, Instance, travelNames, travels

### Community 26 - "Avatar Progression"
Cohesion: 0.08
Nodes (5): LegacyGbotCommand, LegacyGbotDocument, LegacyGbotException, LegacyGbotReader, LegacyGbotReaderTest

### Community 27 - "NodeEditor"
Cohesion: 0.15
Nodes (34): ActivityDock(), BotDocumentDetails(), BotImportEmptyState(), BotImportMessage(), BotManagerSurface(), BotMetadata(), BrandBar(), BrandMark() (+26 more)

### Community 28 - "PacketSpammer"
Cohesion: 0.10
Nodes (18): HandlerAFK, HandledCommands, HandlerChat, HandledCommands, HandlerLogin, HandledCommands, HandlerWarningsXt, HandledCommands (+10 more)

### Community 29 - "Extended Avatar Factions"
Cohesion: 0.06
Nodes (32): Avatar, AccessLevel, AccessType, AFK, Auras, Cell, Class, ClassRank (+24 more)

### Community 30 - "XT Proxy Messaging"
Cohesion: 0.12
Nodes (12): HandlerGetQuests, HandledCommands, HandlerArea, HandledCommands, HandlerBuyItem, HandledCommands, HandlerLoadShop, HandledCommands (+4 more)

### Community 31 - "World State Scanning"
Cohesion: 0.12
Nodes (11): Hotkey, ActionIndex, Key, Text, HotkeyBindingDefinition, ActionIndex, DefaultKey, DisplayName (+3 more)

### Community 33 - "BotData"
Cohesion: 0.07
Nodes (29): 11. Accounts, security, and privacy, 12. Lifecycle, recovery, and performance rules, 13. Updates and distribution, 14. Repository layout, 15. Testing and compatibility gates, 18. Decisions needed from the project owner, 19. Release-policy questions, 20. Recommendation (+21 more)

### Community 34 - "JsonMessage"
Cohesion: 0.07
Nodes (27): World, ActiveCutscene, AllCellInfo, AuraSyncRoot, AvailableMonsters, AvatarsInMap, AvatarsInMyCell, Cells (+19 more)

### Community 35 - "Bot File Loading"
Cohesion: 0.17
Nodes (7): IXmlMessageHandler, HandledCommands, Proxy, _catchAllPackets, _catchXtPackets, firstJoin, Instance

### Community 36 - "Grimoire.Botting.Commands.Misc"
Cohesion: 0.13
Nodes (6): BoolConverter, IntStringConverter, QuestReqConverter, QuestRewardConverter, StringBoolConverter, TrimConverter

### Community 37 - ".RenderQuickBots"
Cohesion: 0.22
Nodes (8): BotData, BotCell, BotMap, BotPad, BotQuests, BotSkill, BotState, SkillSet

### Community 40 - "Selectable Quest Items"
Cohesion: 0.08
Nodes (14): Bot, Configuration, CurrentConfiguration, DropDelay, DropsInInventory, Index, IsRunning, selectedIndex (+6 more)

### Community 41 - "UserFriendlyCommandEditor"
Cohesion: 0.12
Nodes (8): Quests, AcceptedQuests, CompletedQuests, LoadedQuests, QuestTree, UnacceptedQuests, Spammer, Instance

### Community 42 - "CmdBlank3"
Cohesion: 0.08
Nodes (25): itemsR, bPTR, Category, Coins, Cost, Description, HouseItem, Icon (+17 more)

### Community 43 - "Map Cell Data"
Cohesion: 0.08
Nodes (25): itemsS, bPTR, Category, Coins, Cost, Description, HouseItem, Icon (+17 more)

### Community 44 - "Temporary Inventory"
Cohesion: 0.11
Nodes (8): MainActivity, BotImportState, Empty, Failed, GrimoireExodusApp(), LandscapePhonePreview(), Loaded, Loading

### Community 45 - "GrimoireExodusApp.kt"
Cohesion: 0.15
Nodes (7): Settings, Default, Style, Settings, Default, Style, Grimoire.Properties

### Community 46 - "Set"
Cohesion: 0.11
Nodes (6): Set, ISetInterface, Item, Name, Type, SetItem

### Community 48 - ".IsActionAvailable"
Cohesion: 0.08
Nodes (6): Shop, Id, IsShopLoaded, Items, Location, Name

### Community 49 - "Portable Update Application"
Cohesion: 0.09
Nodes (14): TempInventory, Items, TempItem, Cost, Description, Id, IsAcItem, IsMemberOnly (+6 more)

### Community 52 - "Packet Proxy Logging"
Cohesion: 0.11
Nodes (4): UserFriendlyCommandEditor, cmd, cmdObj, commandEditor

### Community 54 - "Color"
Cohesion: 0.29
Nodes (6): BridgeCompatibility, BridgeHandshake, BridgeHandshakeValidator, Compatible, Incompatible, BridgeHandshakeValidatorTest

### Community 55 - "GrimoirePlugin"
Cohesion: 0.15
Nodes (8): GrimoirePlugin, Author, Description, LastError, Name, IGrimoirePlugin, Author, Description

### Community 56 - "Monster State Data"
Cohesion: 0.06
Nodes (29): ExtendedAvatar, ActivationFlag, AdventureCoin, Bank, BankSlots, BoostCP, BoostGold, BoostRep (+21 more)

### Community 57 - "PacketSpammer"
Cohesion: 0.07
Nodes (4): EyeDropper, Instance, PacketSpammer, Instance

### Community 59 - ".Execute"
Cohesion: 0.12
Nodes (15): AppPaths, ApplicationDirectory, BotClientConfigFile, BotsDirectory, CommunityBotsDirectory, ConfigFile, CrashReportsDirectory, DataDirectory (+7 more)

### Community 60 - "CmdBlank3"
Cohesion: 0.11
Nodes (7): CmdBlank3, Alpha, B, G, R, Text, FontInfo

### Community 61 - "Release Build Tooling"
Cohesion: 0.14
Nodes (4): StartupDiagnostics, PreviousSession, StartupLogFile, StartupDiagnosticsSmoke

### Community 62 - "Game Action Locks"
Cohesion: 0.18
Nodes (7): Grimoire Exodus Agent Guide, Desktop Architecture Map, Flash and ActionScript Bridge Contract, Graphify Project Policy, Update and Release Contract, User Data and Recovery Contract, Build and Validation Policy

### Community 63 - "Command Color Editor"
Cohesion: 0.11
Nodes (13): CmdFollow, Name, CmdGotoPlayer, PlayerName, _playerName, CmdPacket, CmdPacket2, Client (+5 more)

### Community 64 - "Composable"
Cohesion: 0.11
Nodes (17): PlayerInfo, AFK, Cell, Class, CurrentState, EntID, HP, Level (+9 more)

### Community 65 - "JavaScript Tooling"
Cohesion: 0.10
Nodes (3): NodeEditor, TypedCommand, Notepad

### Community 67 - ".ExecuteCommand"
Cohesion: 0.18
Nodes (9): CmdJoin2, Cell, _cell, Map, _map, Pad, _pad, Room (+1 more)

### Community 68 - "Grabber"
Cohesion: 0.11
Nodes (18): Monster, Alive, Auras, Cell, CurrentState, Health, Id, Level (+10 more)

### Community 71 - "LogForm"
Cohesion: 0.13
Nodes (4): DebugLogger, LogForm, Instance, SelectedLog

### Community 72 - ".Execute"
Cohesion: 0.17
Nodes (4): CmdJoin, Cell, Map, Pad

### Community 73 - "LegacyGbotReader.kt"
Cohesion: 0.23
Nodes (14): Assert-FileHash(), Complete-BrandedOutput(), Get-BundledFlashRuntime(), Get-ExodusProductVersion(), Get-MSBuildPath(), Get-WixToolset(), Invoke-ExodusBuild(), Invoke-NuGetRestore() (+6 more)

### Community 74 - "IJsonMessageHandler"
Cohesion: 0.12
Nodes (11): HARMAN AIR Game Runtime, Dual Desktop and AIR Host Abstraction, Grimoire Exodus Mobile Overview, Android Module Architecture, Android Foundation, Shared Cross-Platform Contracts, Standalone HARMAN AIR Client, Cross-Platform Contracts (+3 more)

### Community 75 - "JsonMessage"
Cohesion: 0.11
Nodes (12): HandlerDrops, HandledCommands, HandlerAura, HandledCommands, HandlerInfo, HandledCommands, HandlerSkills, HandledCommands (+4 more)

### Community 76 - "Grimoire Exodus v1.1.0 Code and Performance Audit"
Cohesion: 0.12
Nodes (15): LockActions, AcceptQuest, BuyItem, DoIA, EquipItem, GetMapItem, LoadEnhShop, LoadHairShop (+7 more)

### Community 77 - "ExodusTheme.kt"
Cohesion: 0.13
Nodes (11): 1. Executive summary, 6.1 Product shape, 6.2 Game transport contract, 6. Recommended product architecture, Implementation checkpoint 0, GameTransport, Build, Current milestone (0.4.3-dev) (+3 more)

### Community 78 - "DllImport"
Cohesion: 0.12
Nodes (11): Android Stop-Go Gates, Build, Captive-Runtime AQW Client, Physical Android Device Validation Gap, Direct-Stage Command-Center Overlay, Grimoire Exodus AIR client, AIR Local Callback Registry, Prerequisites (+3 more)

### Community 79 - "Base Item Data"
Cohesion: 0.09
Nodes (15): MSI and Portable Package Build, Checksummed GitHub Release Publication, Release Compatibility Smoke Tests, Release Tag Validation, Tagged Windows Build, Grimoire Exodus v1.1.0 Code and Performance Audit, 2026-10-15 Implementation Outcome, Legacy Compatibility Guardrails (+7 more)

### Community 80 - "Item Drop Stack"
Cohesion: 0.12
Nodes (15): description, devDependencies, @commitlint/cli, @commitlint/config-conventional, husky, name, private, scripts (+7 more)

### Community 82 - "Purple Menu Rendering"
Cohesion: 0.18
Nodes (8): AutoBuyBack, Password, Username, BuyBackPage, EventArgument, EventValidation, ViewState, ViewStateGenerator

### Community 83 - "Flash Runtime Architecture"
Cohesion: 0.20
Nodes (6): Private Registration-Free Flash Runtime, App-Local Flash Runtime Guide, Flash Host ABI Requirements, Embedded Flash Runtime Research, Embedded Runtime Safeguards, Modern Installation and Update Foundation

### Community 84 - "Kill For Item Logic"
Cohesion: 0.14
Nodes (12): CompletedQuest, Id, Name, Reward, Success, QuestReward, ClassPoints, Experience (+4 more)

### Community 85 - "GameCapability"
Cohesion: 0.09
Nodes (12): Skill, ComparisonType, Index, SafeHp, SafeMp, SafePercentage, Text, Type (+4 more)

### Community 86 - "AppPreferenceSnapshot"
Cohesion: 0.24
Nodes (6): AppPreferences, AppPreferenceSnapshot, ConfirmOnClose, HideDeviceStats, MinimizeToTray, SkipHomePage

### Community 88 - ".RecordException"
Cohesion: 0.29
Nodes (7): UpdateRelease, HtmlUrl, InstallerAsset, IsPrerelease, Name, PortableAsset, TagName

### Community 89 - "Bot Library Experience"
Cohesion: 0.14
Nodes (6): Android Landscape Command Center, Typed GameTransport Contract, Platform-Neutral Bot Engine, Jetpack Compose Management Shell, Game bridge protocol v1, Secret-Free Bridge Handshake

### Community 90 - ".HandleAsync"
Cohesion: 0.12
Nodes (12): Issue Intake Configuration, Private Security Reporting, Contributor Covenant Code of Conduct, Harassment-Free Community, Contribution Guide, Contribution Testing Policy, Maintained AQW Automation Client, Grimoire Exodus (+4 more)

### Community 91 - "Equipment Type Definitions"
Cohesion: 0.15
Nodes (5): BotClientConfig, Contents, file, Instance, AtomicFile

### Community 92 - "Quest Completion Data"
Cohesion: 0.29
Nodes (3): BotJson, 8. Reuse matrix, LegacyGbotCompatibilitySmoke

### Community 93 - ".ApplyConfiguration"
Cohesion: 0.15
Nodes (12): ItemBase, Category, Coins, Description, File, ID, Link, MaxStack (+4 more)

### Community 94 - "Integer Command Values"
Cohesion: 0.18
Nodes (4): DropStack, Count, InProcess, MarkedDrops

### Community 96 - "Release Publishing Pipeline"
Cohesion: 0.17
Nodes (9): CmdKillFor, ItemName, ItemType, Monster, Quantity, ItemType, Items, TempItems (+1 more)

### Community 97 - "Dictionary"
Cohesion: 0.17
Nodes (12): Map, Area, Cells, EmptyCells, File, FilePath, ID, Instance (+4 more)

### Community 98 - "Type"
Cohesion: 0.20
Nodes (10): ShopInfo, Field, House, Id, Index, Items, Location, Member (+2 more)

### Community 100 - "CmdAcceptQuest"
Cohesion: 0.20
Nodes (10): Bundled Official GBots, Community GBot Distribution Guide, Encrypted Account Locker UI, applyState, AQW Loading Progress UI, View-Only Bot Manager UI, Grimoire Home Surface, renderAccounts (+2 more)

### Community 101 - "BotManager.cs"
Cohesion: 0.33
Nodes (6): state, Add, Clear, Off, On, Remove

### Community 102 - "BankForm"
Cohesion: 0.18
Nodes (7): CosmeticEquipment, ID, Link, Meta, Slot, SWFFile, Type

### Community 103 - "Grimoire Exodus"
Cohesion: 0.18
Nodes (10): EquipType, Armor, Cape, Class, Ground, Helm, None, Offhand (+2 more)

### Community 104 - "PluginManager"
Cohesion: 0.17
Nodes (3): PluginManager, Instance, LastError

### Community 105 - "Resources"
Cohesion: 0.20
Nodes (7): Aura, disableType, Duration, entityID, Name, realValue, Value

### Community 106 - "CmdQuestlist"
Cohesion: 0.14
Nodes (10): CmdQuestlist, ItemID, _itemID, QuestID, _questID, state, State, Add (+2 more)

### Community 108 - ".RefreshHomeState"
Cohesion: 0.10
Nodes (18): RecoveryInfo, BotWasRunning, LastBotPath, LastStage, LastUpdatedUtc, StartedUtc, WasHiddenToTray, SessionState (+10 more)

### Community 109 - "Grimoire Exodus v1.3.0 Release Notes"
Cohesion: 0.12
Nodes (12): Legacy GBot Compatibility Contract, Legacy-Compatible GBot Export Strategy, Centralized Application Settings, Configurable Global Shortcuts, Consistent Native Window Theming, Privacy-Safe Reset Controls, Grimoire Exodus v1.3.0 Release Notes, Legacy GBot Compatibility (+4 more)

### Community 111 - "State"
Cohesion: 0.15
Nodes (8): CmdInt, Int, type, Value, Types, Lower, Set, Upper

### Community 115 - "Message"
Cohesion: 0.20
Nodes (6): Message, Command, RawContent, Send, XmlMessage, Body

### Community 117 - "Message"
Cohesion: 0.22
Nodes (7): CmdIndex, Index, Type, IndexCommand, Down, Goto, Up

### Community 118 - "AQW Header Artwork"
Cohesion: 0.22
Nodes (4): Config, Contents, file, Instance

### Community 119 - "CmdWhitelist"
Cohesion: 0.22
Nodes (3): CmdWhitelist, Item, State

### Community 120 - "MainActivity.kt"
Cohesion: 0.20
Nodes (7): PreparedUpdate, InstallerMode, PackagePath, PayloadDirectory, Release, RequiresElevation, UpdaterPath

### Community 121 - "Dark Fantasy Branding"
Cohesion: 0.22
Nodes (9): 10. Android UX and navigation, Accessibility and ownership boundaries, Android equivalents, Color and component language, Design acceptance criteria, Landscape command center, Primary destinations, Typography (+1 more)

### Community 122 - "CmdBuy2"
Cohesion: 0.22
Nodes (9): 16. Phased roadmap, Overall range, Phase 0 — architecture and offline foundation (complete), Phase 1 — standalone AIR client proof (in progress), Phase 2 — integrated product foundation (6–10 weeks), Phase 3 — legacy GBot engine and core commands (10–20 weeks), Phase 4 — game integration and product parity (12–28 weeks), Phase 5 — advanced tools and plug-in replacement (12–24+ weeks) (+1 more)

### Community 123 - "BotUtilities"
Cohesion: 0.22
Nodes (5): BotUtilities, ActiveBot, BusyClient, HasLoadedQuests, ShouldUseSkill

### Community 124 - "Resources"
Cohesion: 0.22
Nodes (6): Resources, Culture, defaulttext, GrimoireIcon, ResourceManager, statementcmds

### Community 126 - "Loaders"
Cohesion: 0.10
Nodes (11): Loaders, Instance, TreeType, Type, BankItems, InventoryItems, Monsters, QuestIDs (+3 more)

### Community 127 - "Grimoire Exodus AIR client"
Cohesion: 0.25
Nodes (8): StatusIcon, Cpu, EyeClosed, EyeOpen, Network, Settings, UpdateAvailable, Uptime

### Community 129 - "SettingsForm"
Cohesion: 0.29
Nodes (6): CmdUseSkill, Index, SafeHp, SafeMp, Skill, Wait

### Community 130 - "NonClosableVSTabControl"
Cohesion: 0.22
Nodes (6): CmdWalk, Type, X, _x, Y, _y

### Community 131 - "KeyboardHook"
Cohesion: 0.29
Nodes (7): Access, Developer, LeadDeveloper, Moderator, Player, Staff, Tester

### Community 132 - "CmdGoldGreaterThan"
Cohesion: 0.29
Nodes (5): Inventory, AvailableSlots, Items, MaxSlots, UsedSlots

### Community 135 - "state"
Cohesion: 0.33
Nodes (5): CmdBuyByID, ItemID, Manual, ShopID, ShopItemID

### Community 136 - "CmdJoin2"
Cohesion: 0.33
Nodes (5): CmdBuyFastByID, ItemID, Manual, ShopID, ShopItemID

### Community 137 - "Inventory"
Cohesion: 0.33
Nodes (5): CmdMoveToCell, Cell, _cell, Pad, _pad

### Community 138 - "CmdPacketlist2"
Cohesion: 0.25
Nodes (4): CmdLog, Clear, Debug, Text

### Community 139 - "Avatar State Values"
Cohesion: 0.33
Nodes (5): CmdCompleteQuest2, ItemID, _itemID, QuestID, _questID

### Community 143 - "Grimoire Emblem Artwork"
Cohesion: 0.33
Nodes (5): UpdateAsset, DownloadUrl, Name, Sha256, Size

### Community 144 - "Updater Merge Tests"
Cohesion: 0.47
Nodes (6): AQWorlds Master Header, Fantasy Castle Landscape, Fantasy Role-Playing Adventure, Good Versus Evil Confrontation, Heroic Adventurer Party, Undead Monster Horde

### Community 145 - "Solution Project Layout"
Cohesion: 0.53
Nodes (6): Ashfall Dragons Battle, Black Fire Dragon, Dragon Confrontation, Foreground Armored Dragon, Multi-Headed Brown Hydra, Volcanic Ashfall Landscape

### Community 146 - "BridgeContractTest.kt"
Cohesion: 0.33
Nodes (6): 17. Stop/go gates, Gate A — standalone AIR runtime, Gate B — supported technical bridge, Gate C — legacy compatibility, Gate D — Android lifecycle, Gate E — public distribution

### Community 147 - "CmdIsMaxStack"
Cohesion: 0.53
Nodes (6): Grimoire Exodus GitHub Banner, Celestial and Arcane Symbols, Dark Fantasy Identity, Opposing Dragon Guardians, Enchanted Grimoire Emblem, Grimoire Exodus Wordmark

### Community 148 - "IBotCommand"
Cohesion: 0.53
Nodes (6): Celestial Sun and Moon Symbols, Central Magical Grimoire, Dark Fantasy Brand Identity, Flanking Dragons, Grimoire Exodus Banner, Grimoire Exodus Title

### Community 149 - "21. Sources"
Cohesion: 0.40
Nodes (5): 21. Sources, AIR and alternative-stack sources, Android and distribution sources, Official Artix sources, Repository evidence used for this plan

### Community 150 - "CmdWalk"
Cohesion: 0.40
Nodes (4): CmdLoadBot, BotFileName, BotFilePath, _path

### Community 151 - "CmdPacketlist2"
Cohesion: 0.29
Nodes (4): CmdPacketlist2, Delay, Packet, Type

### Community 155 - "CmdQuestlist"
Cohesion: 0.40
Nodes (4): Compatibility, Downloads, Grimoire Exodus v1.3.0, Highlights

### Community 156 - "gradlew"
Cohesion: 0.60
Nodes (5): Arcane Magic Identity, Star and Crescent Celestial Glyphs, Grimoire Emblem Artwork, Rising Magical Aura, Open Grimoire

### Community 157 - "CmdBuyFast"
Cohesion: 0.40
Nodes (4): AppPaths, CrashReportsDirectory, LogsDirectory, RecoveryDirectory

### Community 159 - "Cell"
Cohesion: 0.18
Nodes (10): Cell, MapItems, Name, Pads, MapItem, CollectMsg, ID, Name (+2 more)

### Community 160 - "RawCommandEditor"
Cohesion: 0.18
Nodes (3): RawCommandEditor, Content, Input

### Community 161 - "CmdItemNotPickupable"
Cohesion: 0.50
Nodes (3): CmdBuyBack, ItemName, PageNumberCap

### Community 164 - ".MenuMain_MouseDown"
Cohesion: 0.50
Nodes (3): CmdChange, Guild, Text

### Community 166 - "GrimoireForm"
Cohesion: 0.33
Nodes (3): CmdToggleProvokeInMap, ProvokePacket, Type

### Community 168 - ".CreateSettings"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

### Community 170 - "CmdListInInv"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

### Community 172 - "CmdUseSkill2"
Cohesion: 0.25
Nodes (7): CmdUseSkill2, Index, Monster, SafeHp, SafeMp, Skill, Wait

### Community 173 - "Feature Request Workflow"
Cohesion: 0.67
Nodes (4): Arcane Energy, Celestial Magic, Grimoire Emblem, Open Spellbook

### Community 179 - "CmdAcceptQuest"
Cohesion: 0.29
Nodes (4): CmdAcceptQuest, Quest, CmdCompleteQuest, Quest

### Community 182 - "State"
Cohesion: 0.29
Nodes (7): State, Combat, Move, Others, Quest, Rest, Transaction

### Community 185 - "CmdBuy2"
Cohesion: 0.40
Nodes (4): CmdBuy2, ItemName, Manual, ShopId

### Community 187 - "CmdBankSwap"
Cohesion: 0.50
Nodes (3): CmdBankSwap, BankItemName, InventoryItemName

### Community 188 - "CmdBankTransfer"
Cohesion: 0.50
Nodes (3): CmdBankTransfer, ItemName, TransferFromBank

### Community 189 - "CmdBuy"
Cohesion: 0.50
Nodes (3): CmdBuy, ItemName, ShopId

### Community 190 - "CmdBuyFast"
Cohesion: 0.50
Nodes (3): CmdBuyFast, ItemName, Manual

### Community 191 - "CmdEquip"
Cohesion: 0.50
Nodes (3): CmdEquip, ItemName, Safe

### Community 192 - "CmdClientMessage"
Cohesion: 0.50
Nodes (3): CmdClientMessage, IsWarning, Messages

### Community 193 - "CmdAcceptQuest2"
Cohesion: 0.50
Nodes (3): CmdAcceptQuest2, QuestID, _questID

### Community 196 - "House"
Cohesion: 0.50
Nodes (3): House, Items, TotalSlots

### Community 197 - "State"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

## Knowledge Gaps
- **1060 isolated node(s):** `Monster`, `Monster`, `Packet`, `Monster`, `ItemName` (+1055 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1670 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **132 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `BotManager` connect `Bot Manager Editing` to `Task`, `NonClosableVSTabControl`, `LogForm`, `StatementCommand`, `CmdBankSwap`, `Color`, `CmdPacketlist2`, `.MoveToSelfCell`, `CmdToggleProvoke`, `.LoadBotFromFile`, `AutoRelogin`, `Flash`, `.lstCommands_KeyPress`, `Bot Execution Lifecycle`, `CmdPacketlist2`, `Set`, `Combined Inventory Serialization`, `BotData`, `FormClosingEventArgs`, `GrimoireForm`, `GrimoireForm`, `DPSForm`, `UserFriendlyCommandEditor`, `Application Path Management`, `DarkButton`, `Packet Proxy Logging`, `Packet Command Handling`, `CmdRest`, `PacketSpammer`, `CmdEquipSet`, `CmdBlank3`, `JavaScript Tooling`, `Grimoire Exodus for Android — feasibility and delivery plan`, `LogForm`, `BotManager.cs`, `GameCapability`, `IContainer`, `PluginManager`, `CmdQuestlist`, `State`, `IEnumerable`, `Action`, `CmdWhitelist`?**
  _High betweenness centrality (0.195) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `Root` (e.g. with `.Main()` and `Why not make .NET MAUI the default?`) actually correct?**
  _`Root` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Monster`, `Monster`, `Packet` to the rest of the system?**
  _1060 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Bot Manager Editing` be split into smaller, more focused modules?**
  _Cohesion score 0.030966531122927746 - nodes in this community are weakly interconnected._
- **Why does `IBotCommand` connect `IBotCommand` to `Bot Manager Editing`, `Task`, `StatementCommand`, `system_threading_tasks`, `UpdateService`, `.LoadBotFromFile`, `.ExecuteCommand`, `Combined Inventory Serialization`, `World State Scanning`, `Selectable Quest Items`, `CmdBlank3`, `Command Color Editor`, `.ExecuteCommand`, `.Execute`, `GameCapability`, `Quest Completion Data`, `Release Publishing Pipeline`, `CmdQuestlist`, `State`, `Action`, `Message`, `CmdWhitelist`, `BotUtilities`, `SettingsForm`, `NonClosableVSTabControl`, `state`, `CmdJoin2`, `Inventory`, `CmdPacketlist2`, `Avatar State Values`, `CmdToggleProvoke`, `CmdWalk`, `CmdPacketlist2`, `CmdGetDrop`, `CmdCellIs`, `CmdItemNotPickupable`, `.MenuMain_MouseDown`, `CmdIntEqualInt`, `GrimoireForm`, `CmdIntIsNot`, `CmdUseSkill2`, `CmdAcceptQuest`, `CmdQuestNotCompleted`, `CmdQuestNotInProgress`, `CmdRest`, `CmdBuy2`, `CmdEquipSet`, `CmdBankSwap`, `CmdBankTransfer`, `CmdBuy`, `CmdBuyFast`, `CmdEquip`, `CmdClientMessage`, `CmdAcceptQuest2`, `CmdSell`, `CmdPlayerIsNotInCell`, `CmdAvailableMonstersGreaterThan`, `CmdAttack`, `CmdSkillSet`, `CmdLoad`, `CmdLoad2`, `CmdMapItem`, `CmdMapItem2`, `CmdSell`, `CmdUseBoost`, `CmdLoadMapSwf`, `CmdAvailableMonstersLessThan`, `CmdBeep`, `CmdMonsterHealthLessThan`, `CmdBlank2`, `CmdBotDelay`, `CmdBotDelay2`, `3. Verified research findings`, `CmdPlayerAuraIsActive`, `CmdTargetAuraStringValIs`, `CmdSetFPS`, `CmdStopBotWithMessage`, `CmdPlayersInCellGreaterThan`, `CmdPlayersInCellLessThan`, `CmdAvailableMonstersLessThan`, `CmdGetClassWithVar`, `CmdGoldGreaterThan`, `CmdGoldLessThan`, `CmdHealthGreaterThan`, `CmdHealthLessThan`, `Visual design direction`, `CmdIntNotEqualInt`, `CmdSkillIsAvailable`, `CmdIsMaxStack`, `CmdIsMember`, `CmdIsNotMaxStack`, `CmdItemNotPickupable`, `CmdListInInv`, `CmdVisibleMonstersGreaterThan`, `CmdManaLessThan`, `CmdMonsterNotInRoom`, `CmdNotEquipped`, `CmdNotInCombat`, `CmdPlayerAuraIsNotActive`, `CmdPlayerAuraValIs`, `CmdPlayerAuraValIsGreaterThan`, `CmdPlayerIsInMyCell`, `CmdPlayerIsNotInMyCell`, `CmdPlayerNotEquippedClass`, `CmdQuestInProgress`, `CmdTargetAuraIsNotActive`, `CmdTargetAuraValIs`, `CmdTargetAuraValIsLessThan`, `CmdVisibleMonstersLessThan`?**
  _High betweenness centrality (0.124) - this node is a cross-community bridge._
- **Should `Task` be split into smaller, more focused modules?**
  _Cohesion score 0.048430981316887355 - nodes in this community are weakly interconnected._
- **Why does `Root` connect `StatementCommand` to `LogForm`, `Color`, `Bot Runtime Configuration`, `.MoveToSelfCell`, `AutoRelogin`, `Grimoire Emblem Artwork`, `Grimoire.Utils`, `Flash`, `.HomeBrowser_Navigating`, `.QuickBotDirectoryChanged`, `.QuickBotWatcherError`, `Set`, `Combined Inventory Serialization`, `.MenuMain_MouseDown`, `BotData`, `.Main`, `GrimoireForm`, `DPSForm`, `UserFriendlyCommandEditor`, `Application Path Management`, `DarkButton`, `Packet Proxy Logging`, `Packet Command Handling`, `.pluginAdded`, `PacketSpammer`, `CmdBlank3`, `Release Build Tooling`, `JavaScript Tooling`, `AppPreferenceSnapshot`, `.Load`, `.RecordException`, `ResizableUserControl`, `IContainer`, `PluginManager`, `Resources`, `List`, `.RefreshHomeState`, `.RefreshHomeState`, `IEnumerable`, `MainActivity.kt`, `Grimoire Exodus AIR client`?**
  _High betweenness centrality (0.116) - this node is a cross-community bridge._