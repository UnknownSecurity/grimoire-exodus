# Graph Report - grimoire-exodus  (2026-10-17)

## Corpus Check
- 384 files · ~284,585 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 87 file(s) not represented in the graph (top: .as 23, .resx 22, .dll 9)

## Summary
- 4440 nodes · 9967 edges · 280 communities (156 shown, 124 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 741 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `8090f2f1`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Bot Manager Editing
- system_threading_tasks
- StatementCommand
- system_threading
- PluginManager
- IBotCommand
- system_threading_tasks
- UpdateService.cs
- system
- BotUtilities
- UpdateService.cs
- Loaders
- ClientOptionState
- UpdateService
- AutoRelogin
- PacketTamperer
- Grimoire.Utils
- IJsonMessageHandler
- Quest Data Management
- Bot Execution Lifecycle
- Bot
- StartupDiagnostics
- Inventory Drop Handling
- Travel and Movement
- Set
- Combined Inventory Serialization
- Avatar Progression
- NodeEditor
- PacketSpammer
- Extended Avatar Factions
- .IsActionAvailable
- World State Scanning
- BotManager.cs
- BotData
- JsonMessage
- Bot File Loading
- JsonSerializer
- BotData
- AxShockwaveFlash
- GrimoireForm
- .LoadBotFromFile
- .Call
- CmdBlank3
- Map Cell Data
- Temporary Inventory
- GrimoireExodusApp.kt
- Set
- ClientWorkspaceState
- Portable Update Application
- Application Path Management
- Grimoire Exodus desktop multi-client research and implementation plan
- UserFriendlyCommandEditor
- Packet Command Handling
- Color
- Monster State Data
- ClientSession
- CommandNode
- .Execute
- CmdBlank3
- Release Build Tooling
- Game Action Locks
- Command Color Editor
- Composable
- Flash
- Grimoire Exodus v1.3.0 Release Notes
- .ExecuteCommand
- Grabber
- LogForm
- CancellationToken
- .Run
- .RecordException
- LegacyGbotReader.kt
- IJsonMessageHandler
- .Execute
- Grimoire Exodus v1.1.0 Code and Performance Audit
- .Activate
- DllImport
- Base Item Data
- Item Drop Stack
- AutoBuyBack
- Config
- Kill For Item Logic
- Skill
- .Load
- .Run
- .RecordException
- Bot Library Experience
- .HandleAsync
- Equipment Type Definitions
- IFlashBridge
- CmdEquipSet
- Integer Command Values
- CmdKillFor
- Dictionary
- ItemBase
- IEnumerable
- CmdAcceptQuest
- BotManager.cs
- Server
- Message
- BotUtilities
- Aura
- CmdAcceptQuest
- Extensions
- .RefreshHomeState
- .Run
- State
- BotDocumentState
- ActiveXFlashBridge
- .BindToSession
- FakeFlashBridge
- 9. Implementation phases and gates
- Message
- CommandColorForm
- CmdToggleProvokeInMap
- MainActivity.kt
- Dark Fantasy Branding
- CmdBuy2
- GrimoireHomeBridge
- Resources
- CmdBuy2
- IContainer
- Grimoire Exodus AIR client
- ClientSessionContext
- ClientSessionState
- NonClosableVSTabControl
- KeyboardHook
- CmdGoldGreaterThan
- PluginManager
- OfflineGameTransportTest.kt
- Faction
- Type
- Inventory
- CmdLog
- CmdBuyByID
- CmdBuyFastByID
- CmdBankSwap
- CosmeticForm
- Grimoire Emblem Artwork
- Updater Merge Tests
- Solution Project Layout
- BridgeContractTest.kt
- CmdIsMaxStack
- IBotCommand
- PurpleMenuRenderer
- CmdWalk
- State
- CmdCompleteQuest2
- CmdCellIs
- CmdLog
- CmdQuestlist
- gradlew
- CmdBuyFast
- CmdBuy
- CmdBuyFast
- EquipType
- PacketSpammer
- CmdBankTransfer
- CmdEquip
- CmdBuyBack
- LogForm
- CmdPlayerAuraStringValIs
- CmdAcceptQuest2
- CmdPacketlist2
- ReflectUtils
- TitleBarThemeSmoke
- CmdUseSkill2
- Feature Request Workflow
- KeyboardHook
- CmdAcceptQuest2
- .Main
- BankForm
- CmdPlayersInMyCellLessThan.cs
- CmdChange
- CmdClientMessage
- .GamePopup
- CmdMapItem
- HandlerDrops
- CmdSkillSet
- House
- CmdMapItem2
- ShopInfo
- State
- CmdUseBoost
- CmdCellIsNot
- CmdUseSkill
- CmdPlayerIsInCell
- GameSessionState
- CmdBeep
- CmdBlank2
- CmdBotDelay
- UpdateAsset
- 17. Stop/go gates
- CmdLoadMapSwf
- CmdAttack
- CmdBotDelay2
- CmdBotDelay
- Q: Why is Grimoire stuck at Loading AQW 0%?
- CmdLoad
- CmdDelay
- CmdNotInTemp
- CmdSell
- CmdEquipped
- .IsActionAvailable
- CmdGotoLabel
- CmdStopBotWithMessage
- CmdPlayerIsNotInCell
- .UseSkills
- CmdSetFPS
- CmdPlayersInCellLessThan
- CmdFactionRankLessThan
- Task
- CmdGoldLessThan
- CmdHealthLessThan
- StatementCommand
- CmdInCombat
- CmdInTemp
- CmdIntIs
- CmdIntIsNot
- CmdIntGreaterThan
- CmdIsMaxStack
- CmdItemPickupable
- CmdManaLessThan
- CmdMapIsNot
- CmdMonsterHealthGreaterThan
- CmdMonsterInRoom
- CmdMonsterNotInRoom
- CmdPlayerEquippedClass
- CmdNotEquipped
- CmdPlayerInCombat
- CmdQuestInProgress
- Test-TitleBarTheme.ps1
- CmdPlayerInRoom
- CmdPlayerIsInMyCell
- CmdNotInHouse
- CmdPlayerAuraStringValIsNot
- CmdPlayerAuraValIs
- CmdPlayerNotEquippedClass
- CmdPlayerNotInRoom
- QuestInfo.cs
- CmdQuestInProgress
- CmdPlayerAuraValIsNot
- CmdSkillIsAvailable
- game-client/README.md
- CmdQuestNotAvailable
- CmdTargetAuraStringValIs
- CmdTargetAuraValIsGreaterThan
- CmdUpdateVar
- v1.4.0.md

## God Nodes (most connected - your core abstractions)
1. `BotManager` - 273 edges
2. `Root` - 247 edges
3. `IBotEngine` - 194 edges
4. `IBotCommand` - 180 edges
5. `Grimoire.Game` - 163 edges
6. `StatementCommand` - 103 edges
7. `ClientSession` - 103 edges
8. `Configuration` - 98 edges
9. `Grimoire.Botting.Commands.Misc.Statements` - 97 edges
10. `Grimoire.UI` - 87 edges

## Surprising Connections (you probably didn't know these)
- `Phase 4 — isolate bot execution and options` --references--> `BotDocumentState`  [INFERRED]
  docs/desktop-multi-client-research-plan.md → app/Grimoire.Botting/BotDocumentState.cs
- `4.1 `ClientSession`` --references--> `ClientSession`  [INFERRED]
  docs/desktop-multi-client-research-plan.md → app/Grimoire.Clients/ClientSession.cs
- `4.2 `ClientSessionManager`` --references--> `ClientSessionManager`  [INFERRED]
  docs/desktop-multi-client-research-plan.md → app/Grimoire.Clients/ClientSessionManager.cs
- `15. Implementation status` --references--> `Bot`  [INFERRED]
  docs/desktop-multi-client-research-plan.md → app/Grimoire.Botting/Bot.cs
- `3.4 The bot engine already has useful lifecycle groundwork` --references--> `Bot`  [INFERRED]
  docs/desktop-multi-client-research-plan.md → app/Grimoire.Botting/Bot.cs

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

## Communities (280 total, 124 thin omitted)

### Community 0 - "Bot Manager Editing"
Cohesion: 0.03
Nodes (17): CmdRest, Full, CmdWhitelist, Item, State, CmdToggleProvoke, Type, BotManager (+9 more)

### Community 1 - "system_threading_tasks"
Cohesion: 0.04
Nodes (4): Grimoire.Botting.Commands.Misc, Grimoire.Botting.Commands.Misc.Statements, Grimoire.Botting.Commands.Combat, Grimoire.Game

### Community 2 - "StatementCommand"
Cohesion: 0.03
Nodes (21): QuickBotStatusMenuItem, AppVersion, Client, ClientSessions, CreateParams, customTravel, enableCapture, GameLoadProgress (+13 more)

### Community 4 - "PluginManager"
Cohesion: 0.07
Nodes (4): DPSForm, PluginManager, Instance, LastError

### Community 5 - "IBotCommand"
Cohesion: 0.03
Nodes (32): CmdCancelAutoAttack, CmdCancelTarget, CmdGetDrop, ItemName, CmdLoad2, ShopId, CmdSetSpawn, CmdBankList (+24 more)

### Community 6 - "system_threading_tasks"
Cohesion: 0.09
Nodes (4): Grimoire.Networking.Handlers, Grimoire.Tools, Grimoire.Botting, Grimoire.Game.Data

### Community 8 - "system"
Cohesion: 0.08
Nodes (6): GrimoirePluginEntry, Grimoire.Updater, Grimoire.Tools.Plugins, Grimoire.UI, Grimoire, Grimoire.Utils

### Community 10 - "UpdateService.cs"
Cohesion: 0.03
Nodes (54): BankItem, Bank, Category, CharItemId, CharItemID, Coins, Cost, Description (+46 more)

### Community 11 - "Loaders"
Cohesion: 0.10
Nodes (11): Loaders, Instance, TreeType, Type, BankItems, InventoryItems, Monsters, QuestIDs (+3 more)

### Community 12 - "ClientOptionState"
Cohesion: 0.06
Nodes (36): ClientOptionState, Afk, AntiAfkCancellation, AntiAfkEnabled, AntiAfkTask, ChangeChat, DesiredFramesPerSecond, DisableAnimations (+28 more)

### Community 13 - "UpdateService"
Cohesion: 0.04
Nodes (53): Configuration, AFK, Author, AutoRelogin, AutoSaveState, BankOnStop, Boosts, BotDelay (+45 more)

### Community 14 - "AutoRelogin"
Cohesion: 0.13
Nodes (9): ClientLoginState, IsBusy, AutoRelogin, AreServersLoaded, GameLabel, IsTemporarilyKicked, LoginLabel, ServerIP (+1 more)

### Community 15 - "PacketTamperer"
Cohesion: 0.08
Nodes (7): PacketLogger, Instance, SupportsClientSessionBinding, PacketTamperer, Instance, SupportsClientSessionBinding, BoundedTextBuffer

### Community 16 - "Grimoire.Utils"
Cohesion: 0.11
Nodes (16): AccountRecord, Id, Label, ProtectedPassword, Username, AccountSummary, Id, Label (+8 more)

### Community 17 - "IJsonMessageHandler"
Cohesion: 0.09
Nodes (21): HandlerGetQuests, HandledCommands, HandlerArea, HandledCommands, HandlerBuyItem, HandledCommands, HandlerLoadShop, HandledCommands (+13 more)

### Community 18 - "Quest Data Management"
Cohesion: 0.06
Nodes (26): OptionsManager, AFK, _antiAfk, ChangeChat, DisableAnimations, EnemyMagnet, HidePlayers, HideRoom (+18 more)

### Community 19 - "Bot Execution Lifecycle"
Cohesion: 0.05
Nodes (35): Quest, CanComplete, ClassPointsReward, Description, EndText, ExperienceReward, Faction, FactionId (+27 more)

### Community 20 - "Bot"
Cohesion: 0.08
Nodes (11): Bot, Configuration, Configurations, CurrentConfiguration, DropDelay, DropsInInventory, Index, Instance (+3 more)

### Community 21 - "StartupDiagnostics"
Cohesion: 0.06
Nodes (31): 1. Executive summary, 6.1 Product shape, 6.2 Game transport contract, 6. Recommended product architecture, Implementation checkpoint 0, GameTransport, OfflineGameTransport, Accepted (+23 more)

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
Cohesion: 0.13
Nodes (4): Travel, Instance, travelNames, travels

### Community 26 - "Avatar Progression"
Cohesion: 0.08
Nodes (5): LegacyGbotCommand, LegacyGbotDocument, LegacyGbotException, LegacyGbotReader, LegacyGbotReaderTest

### Community 27 - "NodeEditor"
Cohesion: 0.13
Nodes (39): ActivityDock(), BotDocumentDetails(), BotImportEmptyState(), BotImportState, BotManagerSurface(), BotMetadata(), BrandBar(), BrandMark() (+31 more)

### Community 28 - "PacketSpammer"
Cohesion: 0.12
Nodes (16): HandlerAFK, HandledCommands, HandlerChat, HandledCommands, HandlerWarningsXt, HandledCommands, HandlerConnection, HandledCommands (+8 more)

### Community 29 - "Extended Avatar Factions"
Cohesion: 0.06
Nodes (32): Avatar, AccessLevel, AccessType, AFK, Auras, Cell, Class, ClassRank (+24 more)

### Community 30 - ".IsActionAvailable"
Cohesion: 0.12
Nodes (6): Shop, Id, IsShopLoaded, Items, Location, Name

### Community 31 - "World State Scanning"
Cohesion: 0.09
Nodes (12): Hotkey, ActionIndex, Key, Text, HotkeyBindingDefinition, ActionIndex, DefaultKey, DisplayName (+4 more)

### Community 32 - "BotManager.cs"
Cohesion: 0.09
Nodes (4): Grimoire.Botting.Commands.Item, Grimoire.Botting.Commands.Map, Properties, Grimoire.Botting.Commands.Quest

### Community 33 - "BotData"
Cohesion: 0.06
Nodes (34): 11. Accounts, security, and privacy, 12. Lifecycle, recovery, and performance rules, 13. Updates and distribution, 14. Repository layout, 15. Testing and compatibility gates, 18. Decisions needed from the project owner, 19. Release-policy questions, 20. Recommendation (+26 more)

### Community 34 - "JsonMessage"
Cohesion: 0.05
Nodes (37): World, ActiveCutscene, AllCellInfo, AuraSyncRoot, AvailableMonsters, AvatarsInMap, AvatarsInMyCell, Cells (+29 more)

### Community 35 - "Bot File Loading"
Cohesion: 0.13
Nodes (7): IXmlMessageHandler, HandledCommands, Proxy, _catchAllPackets, _catchXtPackets, firstJoin, Instance

### Community 36 - "JsonSerializer"
Cohesion: 0.13
Nodes (6): BoolConverter, IntStringConverter, QuestReqConverter, QuestRewardConverter, StringBoolConverter, TrimConverter

### Community 37 - "BotData"
Cohesion: 0.10
Nodes (21): BotData, BotCell, BotMap, BotPad, BotQuests, BotSkill, BotState, SkillSet (+13 more)

### Community 39 - "GrimoireForm"
Cohesion: 0.12
Nodes (5): GrimoireForm, BoundClientSession, CanChangeClientSessionBinding, SupportsClientSessionBinding, SafeWinEventHookHandle

### Community 41 - ".Call"
Cohesion: 0.03
Nodes (49): Bank, AvailableSlots, HasBankOpen, IsBankLoaded, Items, TotalSlots, UsedSlots, Quests (+41 more)

### Community 42 - "CmdBlank3"
Cohesion: 0.08
Nodes (25): itemsR, bPTR, Category, Coins, Cost, Description, HouseItem, Icon (+17 more)

### Community 43 - "Map Cell Data"
Cohesion: 0.08
Nodes (25): itemsS, bPTR, Category, Coins, Cost, Description, HouseItem, Icon (+17 more)

### Community 45 - "GrimoireExodusApp.kt"
Cohesion: 0.17
Nodes (7): Settings, Default, Style, Settings, Default, Style, Grimoire.Properties

### Community 46 - "Set"
Cohesion: 0.08
Nodes (6): Set, ISetInterface, Item, Name, Type, SetItem

### Community 47 - "ClientWorkspaceState"
Cohesion: 0.22
Nodes (6): AccountCredentials, Id, Label, Password, Username, ClientWorkspaceState

### Community 49 - "Portable Update Application"
Cohesion: 0.09
Nodes (14): TempInventory, Items, TempItem, Cost, Description, Id, IsAcItem, IsMemberOnly (+6 more)

### Community 51 - "Grimoire Exodus desktop multi-client research and implementation plan"
Cohesion: 0.12
Nodes (15): 10. Validation plan, 11. Risk register, 12. Compatibility and rollback strategy, 14. Final recommendation, 1. Executive summary, 2. Product outcome, 5. Toolbar isolation contract, 7. Single-application behavior (+7 more)

### Community 52 - "UserFriendlyCommandEditor"
Cohesion: 0.06
Nodes (7): RawCommandEditor, Content, Input, UserFriendlyCommandEditor, cmd, cmdObj, commandEditor

### Community 54 - "Color"
Cohesion: 0.29
Nodes (6): BridgeCompatibility, BridgeHandshake, BridgeHandshakeValidator, Compatible, Incompatible, BridgeHandshakeValidatorTest

### Community 56 - "Monster State Data"
Cohesion: 0.10
Nodes (21): ExtendedAvatar, ActivationFlag, AdventureCoin, Bank, BankSlots, BoostCP, BoostGold, BoostRep (+13 more)

### Community 57 - "ClientSession"
Cohesion: 0.08
Nodes (20): ClientSession, BotDocument, BotEngine, DisplayName, FlashBridge, GameState, Id, IsDisposed (+12 more)

### Community 59 - ".Execute"
Cohesion: 0.12
Nodes (15): AppPaths, ApplicationDirectory, BotClientConfigFile, BotsDirectory, CommunityBotsDirectory, ConfigFile, CrashReportsDirectory, DataDirectory (+7 more)

### Community 60 - "CmdBlank3"
Cohesion: 0.10
Nodes (7): CmdBlank3, Alpha, B, G, R, Text, FontInfo

### Community 61 - "Release Build Tooling"
Cohesion: 0.19
Nodes (4): StartupDiagnostics, PreviousSession, StartupLogFile, StartupDiagnosticsSmoke

### Community 62 - "Game Action Locks"
Cohesion: 0.18
Nodes (7): Grimoire Exodus Agent Guide, Desktop Architecture Map, Flash and ActionScript Bridge Contract, Graphify Project Policy, Update and Release Contract, User Data and Recovery Contract, Build and Validation Policy

### Community 63 - "Command Color Editor"
Cohesion: 0.11
Nodes (13): CmdFollow, Name, CmdGotoPlayer, PlayerName, _playerName, CmdPacket, CmdPacket2, Client (+5 more)

### Community 64 - "Composable"
Cohesion: 0.06
Nodes (31): CmdQuestlist, ItemID, _itemID, QuestID, _questID, state, State, Add (+23 more)

### Community 66 - "Grimoire Exodus v1.3.0 Release Notes"
Cohesion: 0.12
Nodes (12): Legacy GBot Compatibility Contract, Legacy-Compatible GBot Export Strategy, Centralized Application Settings, Configurable Global Shortcuts, Consistent Native Window Theming, Privacy-Safe Reset Controls, Grimoire Exodus v1.3.0 Release Notes, Legacy GBot Compatibility (+4 more)

### Community 67 - ".ExecuteCommand"
Cohesion: 0.18
Nodes (9): CmdJoin2, Cell, _cell, Map, _map, Pad, _pad, Room (+1 more)

### Community 68 - "Grabber"
Cohesion: 0.11
Nodes (18): Monster, Alive, Auras, Cell, CurrentState, Health, Id, Level (+10 more)

### Community 71 - ".Run"
Cohesion: 0.20
Nodes (5): MultiClientBotIsolationSmoke, RecordingCommand, ExecutionAfterAwait, ExecutionCount, FirstExecution

### Community 73 - "LegacyGbotReader.kt"
Cohesion: 0.24
Nodes (14): Assert-FileHash(), Complete-BrandedOutput(), Get-BundledFlashRuntime(), Get-ExodusProductVersion(), Get-MSBuildPath(), Get-WixToolset(), Invoke-ExodusBuild(), Invoke-NuGetRestore() (+6 more)

### Community 74 - "IJsonMessageHandler"
Cohesion: 0.12
Nodes (11): HARMAN AIR Game Runtime, Dual Desktop and AIR Host Abstraction, Grimoire Exodus Mobile Overview, Android Module Architecture, Android Foundation, Shared Cross-Platform Contracts, Standalone HARMAN AIR Client, Cross-Platform Contracts (+3 more)

### Community 75 - ".Execute"
Cohesion: 0.15
Nodes (4): CmdJoin, Cell, Map, Pad

### Community 76 - "Grimoire Exodus v1.1.0 Code and Performance Audit"
Cohesion: 0.12
Nodes (15): LockActions, AcceptQuest, BuyItem, DoIA, EquipItem, GetMapItem, LoadEnhShop, LoadHairShop (+7 more)

### Community 77 - ".Activate"
Cohesion: 0.17
Nodes (6): BotRunContext, BotTask, Cancellation, Configuration, DropTask, QuestTask

### Community 78 - "DllImport"
Cohesion: 0.12
Nodes (11): Android Stop-Go Gates, Build, Captive-Runtime AQW Client, Physical Android Device Validation Gap, Direct-Stage Command-Center Overlay, Grimoire Exodus AIR client, AIR Local Callback Registry, Prerequisites (+3 more)

### Community 79 - "Base Item Data"
Cohesion: 0.09
Nodes (16): MSI and Portable Package Build, Checksummed GitHub Release Publication, Release Compatibility Smoke Tests, Release Tag Validation, Tagged Windows Build, Grimoire Exodus v1.1.0 Code and Performance Audit, 2026-10-15 Implementation Outcome, Legacy Compatibility Guardrails (+8 more)

### Community 80 - "Item Drop Stack"
Cohesion: 0.12
Nodes (15): description, devDependencies, @commitlint/cli, @commitlint/config-conventional, husky, name, private, scripts (+7 more)

### Community 82 - "AutoBuyBack"
Cohesion: 0.20
Nodes (8): AutoBuyBack, Password, Username, BuyBackPage, EventArgument, EventValidation, ViewState, ViewStateGenerator

### Community 83 - "Config"
Cohesion: 0.15
Nodes (5): Config, Contents, file, Instance, AtomicFile

### Community 84 - "Kill For Item Logic"
Cohesion: 0.18
Nodes (10): CompletedQuest, Id, Name, Reward, Success, QuestReward, ClassPoints, Experience (+2 more)

### Community 85 - "Skill"
Cohesion: 0.10
Nodes (12): Skill, ComparisonType, Index, SafeHp, SafeMp, SafePercentage, Text, Type (+4 more)

### Community 86 - ".Load"
Cohesion: 0.14
Nodes (7): AppPreferences, AppPreferenceSnapshot, AllowUnlimitedClients, ConfirmOnClose, HideDeviceStats, MinimizeToTray, SkipHomePage

### Community 87 - ".Run"
Cohesion: 0.22
Nodes (8): ClientSessionEventArgs, Session, ClientSessionManager, ActiveSession, Default, MaximumSessions, Sessions, ClientLimitSmoke

### Community 88 - ".RecordException"
Cohesion: 0.17
Nodes (8): UpdateRelease, HtmlUrl, InstallerAsset, IsPrerelease, Name, PortableAsset, TagName, Version

### Community 89 - "Bot Library Experience"
Cohesion: 0.14
Nodes (6): Android Landscape Command Center, Typed GameTransport Contract, Platform-Neutral Bot Engine, Jetpack Compose Management Shell, Game bridge protocol v1, Secret-Free Bridge Handshake

### Community 90 - ".HandleAsync"
Cohesion: 0.07
Nodes (22): Issue Intake Configuration, Private Security Reporting, Private Registration-Free Flash Runtime, App-Local Flash Runtime Guide, Contributor Covenant Code of Conduct, Harassment-Free Community, Contribution Guide, Contribution Testing Policy (+14 more)

### Community 91 - "Equipment Type Definitions"
Cohesion: 0.13
Nodes (10): BotClientConfig, Contents, file, Instance, 8. Persistence, recovery, shutdown, and updates, Closing one client, Closing or updating the app, Recovery (+2 more)

### Community 92 - "IFlashBridge"
Cohesion: 0.09
Nodes (7): IFlashBridge, Control, 13. Recommended first implementation slice, FakeFlashBridge, Control, FakeFlashBridge, Control

### Community 93 - "CmdEquipSet"
Cohesion: 0.17
Nodes (5): BotJson, CmdEquipSet, ItemName, 8. Reuse matrix, LegacyGbotCompatibilitySmoke

### Community 94 - "Integer Command Values"
Cohesion: 0.16
Nodes (4): DropStack, Count, InProcess, MarkedDrops

### Community 96 - "CmdKillFor"
Cohesion: 0.17
Nodes (9): CmdKillFor, ItemName, ItemType, Monster, Quantity, ItemType, Items, TempItems (+1 more)

### Community 97 - "Dictionary"
Cohesion: 0.09
Nodes (22): Cell, MapItems, Name, Pads, Map, Area, Cells, EmptyCells (+14 more)

### Community 98 - "ItemBase"
Cohesion: 0.15
Nodes (12): ItemBase, Category, Coins, Description, File, ID, Link, MaxStack (+4 more)

### Community 100 - "CmdAcceptQuest"
Cohesion: 0.20
Nodes (10): Bundled Official GBots, Community GBot Distribution Guide, Encrypted Account Locker UI, applyState, AQW Loading Progress UI, View-Only Bot Manager UI, Grimoire Home Surface, renderAccounts (+2 more)

### Community 101 - "BotManager.cs"
Cohesion: 0.33
Nodes (6): state, Add, Clear, Off, On, Remove

### Community 102 - "Server"
Cohesion: 0.12
Nodes (11): Server, Ip, IsChatRestricted, IsMemberOnly, IsOnline, Language, List, MaxCount (+3 more)

### Community 103 - "Message"
Cohesion: 0.22
Nodes (6): Message, Command, RawContent, Send, XmlMessage, Body

### Community 104 - "BotUtilities"
Cohesion: 0.22
Nodes (5): BotUtilities, ActiveBot, BusyClient, HasLoadedQuests, ShouldUseSkill

### Community 105 - "Aura"
Cohesion: 0.17
Nodes (7): Aura, disableType, Duration, entityID, Name, realValue, Value

### Community 106 - "CmdAcceptQuest"
Cohesion: 0.29
Nodes (4): CmdAcceptQuest, Quest, CmdCompleteQuest, Quest

### Community 107 - "Extensions"
Cohesion: 0.06
Nodes (7): Extensions, Line, ControlUtils, SingleInstanceBroker, IsPrimaryInstance, TimeLimiter, SingleInstanceBrokerSmoke

### Community 108 - ".RefreshHomeState"
Cohesion: 0.10
Nodes (18): RecoveryInfo, BotWasRunning, LastBotPath, LastStage, LastUpdatedUtc, StartedUtc, WasHiddenToTray, SessionState (+10 more)

### Community 111 - "State"
Cohesion: 0.15
Nodes (8): CmdInt, Int, type, Value, Types, Lower, Set, Upper

### Community 112 - "BotDocumentState"
Cohesion: 0.23
Nodes (7): BotDocumentState, Configuration, DisplayName, HasSnapshot, IsDirty, SourcePath, 15. Implementation status

### Community 114 - ".BindToSession"
Cohesion: 0.08
Nodes (4): BotDocumentChangedEventArgs, ConfigurationChanged, MetadataChanged, DocConvert

### Community 115 - "FakeFlashBridge"
Cohesion: 0.18
Nodes (4): FakeFlashBridge, Control, Disposed, GameFunctionCallCount

### Community 116 - "9. Implementation phases and gates"
Cohesion: 0.20
Nodes (10): 9. Implementation phases and gates, Phase 0 — establish test seams and baselines, Phase 1 — two-Flash-control feasibility spike, Phase 2 — introduce session ownership with one live client, Phase 3 — isolate Flash, packets, and game state, Phase 4 — isolate bot execution and options, Phase 5 — add client UI and toolbar binding, Phase 6 — bind Home, tools, recovery, tray, and updater (+2 more)

### Community 117 - "Message"
Cohesion: 0.22
Nodes (7): CmdIndex, Index, Type, IndexCommand, Down, Goto, Up

### Community 118 - "CommandColorForm"
Cohesion: 0.05
Nodes (3): CommandColorForm, EyeDropper, Instance

### Community 119 - "CmdToggleProvokeInMap"
Cohesion: 0.33
Nodes (3): CmdToggleProvokeInMap, ProvokePacket, Type

### Community 120 - "MainActivity.kt"
Cohesion: 0.20
Nodes (7): PreparedUpdate, InstallerMode, PackagePath, PayloadDirectory, Release, RequiresElevation, UpdaterPath

### Community 121 - "Dark Fantasy Branding"
Cohesion: 0.22
Nodes (9): 10. Android UX and navigation, Accessibility and ownership boundaries, Android equivalents, Color and component language, Design acceptance criteria, Landscape command center, Primary destinations, Typography (+1 more)

### Community 122 - "CmdBuy2"
Cohesion: 0.22
Nodes (9): 16. Phased roadmap, Overall range, Phase 0 — architecture and offline foundation (complete), Phase 1 — standalone AIR client proof (in progress), Phase 2 — integrated product foundation (6–10 weeks), Phase 3 — legacy GBot engine and core commands (10–20 weeks), Phase 4 — game integration and product parity (12–28 weeks), Phase 5 — advanced tools and plug-in replacement (12–24+ weeks) (+1 more)

### Community 124 - "Resources"
Cohesion: 0.22
Nodes (6): Resources, Culture, defaulttext, GrimoireIcon, ResourceManager, statementcmds

### Community 125 - "CmdBuy2"
Cohesion: 0.40
Nodes (4): CmdBuy2, ItemName, Manual, ShopId

### Community 126 - "IContainer"
Cohesion: 0.07
Nodes (5): CommandNode, activeControl, NodeEditor, TypedCommand, Notepad

### Community 127 - "Grimoire Exodus AIR client"
Cohesion: 0.25
Nodes (8): StatusIcon, Cpu, EyeClosed, EyeOpen, Network, Settings, UpdateAvailable, Uptime

### Community 128 - "ClientSessionContext"
Cohesion: 0.14
Nodes (11): ClientSessionContext, Current, SessionScope, 4.1 `ClientSession`, 4.2 `ClientSessionManager`, 4.3 Instance Flash bridge, 4.4 Session context and legacy compatibility facades, 4.5 Per-session game state and packet dispatch (+3 more)

### Community 129 - "ClientSessionState"
Cohesion: 0.22
Nodes (8): ClientSessionState, Closing, Created, Disposed, Faulted, Initializing, Ready, Running

### Community 130 - "NonClosableVSTabControl"
Cohesion: 0.22
Nodes (6): CmdWalk, Type, X, _x, Y, _y

### Community 131 - "KeyboardHook"
Cohesion: 0.29
Nodes (7): Access, Developer, LeadDeveloper, Moderator, Player, Staff, Tester

### Community 132 - "CmdGoldGreaterThan"
Cohesion: 0.29
Nodes (5): Inventory, AvailableSlots, Items, MaxSlots, UsedSlots

### Community 133 - "PluginManager"
Cohesion: 0.12
Nodes (12): GrimoirePlugin, Author, Description, LastError, Name, IGrimoirePlugin, Author, Description (+4 more)

### Community 135 - "Faction"
Cohesion: 0.20
Nodes (8): Faction, ID, Name, Rank, RemainingRep, Rep, RequiredRep, TotalRep

### Community 137 - "Inventory"
Cohesion: 0.33
Nodes (5): CmdMoveToCell, Cell, _cell, Pad, _pad

### Community 138 - "CmdLog"
Cohesion: 0.25
Nodes (4): CmdLog, Clear, Debug, Text

### Community 139 - "CmdBuyByID"
Cohesion: 0.33
Nodes (5): CmdBuyByID, ItemID, Manual, ShopID, ShopItemID

### Community 140 - "CmdBuyFastByID"
Cohesion: 0.33
Nodes (5): CmdBuyFastByID, ItemID, Manual, ShopID, ShopItemID

### Community 141 - "CmdBankSwap"
Cohesion: 0.50
Nodes (3): CmdBankSwap, BankItemName, InventoryItemName

### Community 142 - "CosmeticForm"
Cohesion: 0.09
Nodes (9): CosmeticEquipment, ID, Link, Meta, Slot, SWFFile, Type, CosmeticForm (+1 more)

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

### Community 150 - "CmdWalk"
Cohesion: 0.40
Nodes (4): CmdLoadBot, BotFileName, BotFilePath, _path

### Community 151 - "State"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

### Community 152 - "CmdCompleteQuest2"
Cohesion: 0.33
Nodes (5): CmdCompleteQuest2, ItemID, _itemID, QuestID, _questID

### Community 155 - "CmdQuestlist"
Cohesion: 0.40
Nodes (4): Compatibility, Downloads, Grimoire Exodus v1.3.0, Highlights

### Community 156 - "gradlew"
Cohesion: 0.60
Nodes (5): Arcane Magic Identity, Star and Crescent Celestial Glyphs, Grimoire Emblem Artwork, Rising Magical Aura, Open Grimoire

### Community 157 - "CmdBuyFast"
Cohesion: 0.40
Nodes (4): AppPaths, CrashReportsDirectory, LogsDirectory, RecoveryDirectory

### Community 159 - "CmdBuy"
Cohesion: 0.50
Nodes (3): CmdBuy, ItemName, ShopId

### Community 160 - "CmdBuyFast"
Cohesion: 0.50
Nodes (3): CmdBuyFast, ItemName, Manual

### Community 161 - "EquipType"
Cohesion: 0.18
Nodes (10): EquipType, Armor, Cape, Class, Ground, Helm, None, Offhand (+2 more)

### Community 162 - "PacketSpammer"
Cohesion: 0.08
Nodes (8): Spammer, Instance, IsRunning, LastError, PacketSpammer, CanChangeClientSessionBinding, Instance, SupportsClientSessionBinding

### Community 163 - "CmdBankTransfer"
Cohesion: 0.50
Nodes (3): CmdBankTransfer, ItemName, TransferFromBank

### Community 164 - "CmdEquip"
Cohesion: 0.50
Nodes (3): CmdEquip, ItemName, Safe

### Community 165 - "CmdBuyBack"
Cohesion: 0.50
Nodes (3): CmdBuyBack, ItemName, PageNumberCap

### Community 166 - "LogForm"
Cohesion: 0.13
Nodes (4): DebugLogger, LogForm, Instance, SelectedLog

### Community 168 - "CmdAcceptQuest2"
Cohesion: 0.50
Nodes (3): CmdAcceptQuest2, QuestID, _questID

### Community 169 - "CmdPacketlist2"
Cohesion: 0.29
Nodes (4): CmdPacketlist2, Delay, Packet, Type

### Community 172 - "CmdUseSkill2"
Cohesion: 0.25
Nodes (7): CmdUseSkill2, Index, Monster, SafeHp, SafeMp, Skill, Wait

### Community 173 - "Feature Request Workflow"
Cohesion: 0.67
Nodes (4): Arcane Energy, Celestial Magic, Grimoire Emblem, Open Spellbook

### Community 181 - "CmdChange"
Cohesion: 0.50
Nodes (3): CmdChange, Guild, Text

### Community 182 - "CmdClientMessage"
Cohesion: 0.50
Nodes (3): CmdClientMessage, IsWarning, Messages

### Community 185 - "HandlerDrops"
Cohesion: 0.18
Nodes (5): HandlerDrops, HandledCommands, HandlerAura, HandledCommands, HandlerTask

### Community 188 - "House"
Cohesion: 0.50
Nodes (3): House, Items, TotalSlots

### Community 190 - "ShopInfo"
Cohesion: 0.20
Nodes (10): ShopInfo, Field, House, Id, Index, Items, Location, Member (+2 more)

### Community 191 - "State"
Cohesion: 0.50
Nodes (4): State, Dead, Idle, InCombat

### Community 194 - "CmdUseSkill"
Cohesion: 0.29
Nodes (6): CmdUseSkill, Index, SafeHp, SafeMp, Skill, Wait

### Community 196 - "GameSessionState"
Cohesion: 0.04
Nodes (49): GameSessionState, ActiveBot, AuraSyncRoot, Bank, BotCell, BotConfigurations, BotDropsInInventory, BotMap (+41 more)

### Community 206 - "Q: Why is Grimoire stuck at Loading AQW 0%?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Why is Grimoire stuck at Loading AQW 0%?, Source Nodes

### Community 214 - ".IsActionAvailable"
Cohesion: 0.10
Nodes (5): ChatCommand, cmdCombat, Engine, ShouldCombat, ShouldProvoke

### Community 218 - ".UseSkills"
Cohesion: 0.18
Nodes (3): CmdKill, Monster, Packet

### Community 223 - "Task"
Cohesion: 0.05
Nodes (5): IBotEngine, Configuration, CurrentConfiguration, Index, IsRunning

### Community 226 - "StatementCommand"
Cohesion: 0.02
Nodes (44): CmdAvailableMonstersLessThan, CmdFactionRankGreaterThan, CmdGetClassWithVar, CmdGoldGreaterThan, CmdHealthGreaterThan, CmdHealthPercentageLessThan, CmdInBank, CmdInHouse (+36 more)

## Knowledge Gaps
- **1224 isolated node(s):** `Monster`, `Monster`, `Packet`, `Monster`, `ItemName` (+1219 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1881 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **124 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Root` connect `StatementCommand` to `system_threading`, `PluginManager`, `system`, `AutoRelogin`, `PacketTamperer`, `Grimoire Emblem Artwork`, `PurpleMenuRenderer`, `Set`, `.MenuMain_MouseDown`, `Bot File Loading`, `AxShockwaveFlash`, `GrimoireForm`, `.Call`, `ClientWorkspaceState`, `Application Path Management`, `Packet Command Handling`, `.Push`, `ClientSession`, `.Root_Resize`, `CmdBlank3`, `Release Build Tooling`, `.RecordException`, `.Load`, `.Run`, `.RecordException`, `Color`, `Aura`, `.RefreshHomeState`, `.CreateClientSession`, `CommandColorForm`, `MainActivity.kt`, `GrimoireHomeBridge`, `IContainer`, `Grimoire Exodus AIR client`?**
  _High betweenness centrality (0.179) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `BotManager` (e.g. with `Why not make .NET MAUI the default?` and `4.6 Per-session bot model`) actually correct?**
  _`BotManager` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Monster`, `Monster`, `Packet` to the rest of the system?**
  _1224 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Bot Manager Editing` be split into smaller, more focused modules?**
  _Cohesion score 0.027227131597802722 - nodes in this community are weakly interconnected._
- **Why does `BotManager` connect `Bot Manager Editing` to `ClientSessionContext`, `NonClosableVSTabControl`, `StatementCommand`, `PluginManager`, `CmdLog`, `UpdateService`, `PacketTamperer`, `Bot Execution Lifecycle`, `Set`, `Combined Inventory Serialization`, `BotManager.cs`, `BotData`, `PacketSpammer`, `Bot File Loading`, `LogForm`, `GrimoireForm`, `.LoadBotFromFile`, `CmdPacketlist2`, `.Call`, `Set`, `ClientWorkspaceState`, `Application Path Management`, `UserFriendlyCommandEditor`, `Packet Command Handling`, `ClientSession`, `CmdBlank3`, `.Activate`, `Skill`, `.Run`, `CmdEquipSet`, `Task`, `Color`, `StatementCommand`, `Server`, `Extensions`, `State`, `.BindToSession`, `CommandColorForm`, `CmdToggleProvokeInMap`, `IContainer`?**
  _High betweenness centrality (0.116) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `IBotEngine` (e.g. with `12. Compatibility and rollback strategy` and `1. Executive summary`) actually correct?**
  _`IBotEngine` has 2 INFERRED edges - model-reasoned connections that need verification._
- **Should `system_threading_tasks` be split into smaller, more focused modules?**
  _Cohesion score 0.03797736168412144 - nodes in this community are weakly interconnected._