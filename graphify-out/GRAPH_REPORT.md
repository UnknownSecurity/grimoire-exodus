# Graph Report - grimoire-exodus  (2026-10-19)

## Corpus Check
- 400 files · ~295,050 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 89 file(s) not represented in the graph (top: .as 23, .resx 22, .dll 9)

## Summary
- 4631 nodes · 10274 edges · 293 communities (167 shown, 126 thin omitted)
- Extraction: 93% EXTRACTED · 7% INFERRED · 0% AMBIGUOUS · INFERRED: 757 edges (avg confidence: 0.81)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f5243264`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Bot Manager Editing
- system_threading_tasks
- StatementCommand
- Grimoire.UI
- .Call
- IBotCommand
- system_linq
- .MoveOutOfCombat
- Grimoire.Botting
- BotUtilities
- UpdateService.cs
- Type
- ClientOptionState
- UpdateService
- CancellationTokenSource
- GrimoirePlugin
- Grimoire.Utils
- Bot
- Quest Data Management
- Bot Execution Lifecycle
- SessionState
- StartupDiagnostics
- Inventory Drop Handling
- Travel and Movement
- Set
- Combined Inventory Serialization
- Avatar Progression
- NodeEditor
- PacketSpammer
- Extended Avatar Factions
- Grimoire.UI
- World State Scanning
- Grimoire.Botting.Commands.Misc
- BotData
- JsonMessage
- Bot File Loading
- Type
- BotData
- CmdBlank3
- UpdateRelease
- .LoadBotFromFile
- ClientWorkspaceState
- CmdBlank3
- Map Cell Data
- Temporary Inventory
- GrimoireExodusApp.kt
- Set
- STAThread
- Portable Update Application
- Application Path Management
- IJsonMessageHandler
- CosmeticForm
- Packet Command Handling
- Color
- Monster State Data
- ClientSession
- CommandNode
- .Execute
- app.vue
- StartupDiagnostics
- Game Action Locks
- Command Color Editor
- Composable
- GrimoireForm
- App-Local Flash Runtime
- .ExecuteCommand
- Grabber
- UpdateService
- CancellationToken
- .Run
- website/package.json
- LegacyGbotReader.kt
- IJsonMessageHandler
- .Execute
- Grimoire Exodus v1.1.0 Code and Performance Audit
- PacketSpammer
- DllImport
- Base Item Data
- Item Drop Stack
- GameCapability
- Dictionary
- Kill For Item Logic
- Skill
- AppPreferenceSnapshot
- ClientSessionManager
- Bot Library Experience
- .HandleAsync
- Equipment Type Definitions
- ActiveXFlashBridge
- index.mjs
- Integer Command Values
- Extensions
- CmdKillFor
- Dictionary
- ItemBase
- CommandColorForm
- CmdAcceptQuest
- BotManager.cs
- 21. Sources
- .UseSkills
- BotUtilities
- Aura
- CmdAcceptQuest
- Grimoire Exodus v1.3.0 Release Notes
- Config
- .FullReset_Click
- State
- BotDocumentState
- ActiveXFlashBridge
- .BindToSession
- .InputDialog
- Spammer
- Message
- AutoBuyBack
- CmdLog
- GrimoireHomeBridge
- Dark Fantasy Branding
- CmdBuy2
- JsonMessage
- .Run
- CmdBuy2
- NodeEditor
- Grimoire Exodus AIR client
- CmdPacketlist2
- CommandNode
- Action
- KeyboardHook
- PluginManager
- ReflectUtils
- PreparedUpdate
- CmdEquipSet
- ClientSessionState
- Inventory
- CmdWalk
- CmdBuyByID
- CmdBuyFastByID
- CmdBankSwap
- CosmeticEquipment
- SingleInstanceBroker
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
- Message
- CmdBankTransfer
- CmdEquip
- CmdBuyBack
- .SelectUpdate
- TitleBarThemeSmoke
- CmdAcceptQuest2
- countWords
- UserFriendlyCommandEditor
- CmdUseSkill2
- Feature Request Workflow
- KeyboardHook
- CmdAcceptQuest2
- AxShockwaveFlash
- .Main
- CmdChange
- CmdClientMessage
- NonClosableVSTabControl
- CmdMapItem
- BotRunContext
- devDependencies
- CmdSkillSet
- scripts
- CmdMapItem2
- ShopInfo
- Player
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
- ActiveClientSessionChangedEventArgs
- CmdBotDelay2
- CmdBotDelay
- Q: Why is Grimoire stuck at Loading AQW 0%?
- dependencies
- CmdDelay
- Inventory
- .GamePopup
- Line
- FlashRenderQuality
- CmdGotoLabel
- CmdStopBotWithMessage
- Grimoire Exodus website
- CmdGetDrop
- Desktop rendering performance research for v1.4.0
- CmdPlayersInCellLessThan
- CmdFactionRankGreaterThan
- CmdLoad2
- Task
- CmdPlayerIsNotInCell
- tsconfig.json
- StatementCommand
- CmdGoldGreaterThan
- CmdHealthGreaterThan
- CmdIntIs
- CmdInBank
- CmdInInventory
- LogForm
- House
- CmdIntNotEqualInt
- CmdItemNotPickupable
- CmdManaGreaterThan
- CmdMapIs
- QuestInfo.cs
- CmdNotInBank
- CmdPlayerInCombat
- CmdQuestInProgress
- Test-TitleBarTheme.ps1
- CmdIntGreaterThan
- CmdPlayerIsInMyCell
- CmdNotInCombat
- CmdPlayerAuraIsActive
- CmdPlayerAuraIsNotActive
- CmdPlayerAuraStringValIs
- CmdPlayerAuraValIsLessThan
- CmdPlayerIsNotInMyCell
- CmdPlayerNotInCombat
- CmdQuestNotCompleted
- CmdSkillIsAvailable
- game-client/README.md
- CmdSetVar
- CmdSkillIsNotAvailable
- CmdTargetAuraIsNotActive
- CmdTargetAuraValIsLessThan
- v1.4.0.md
- CmdNotInTemp
- CmdVisibleMonstersGreaterThan
- CmdPlayerEquippedClass
- CmdPlayerInRoom
- CmdPlayerNotEquippedClass
- CmdPlayerNotInRoom
- CmdPlayersInMyCellLessThan
- CmdQuestInProgress
- GitHub issue relay Worker
- buildComposerUrl

## God Nodes (most connected - your core abstractions)
1. `BotManager` - 273 edges
2. `Root` - 260 edges
3. `IBotEngine` - 194 edges
4. `IBotCommand` - 180 edges
5. `Grimoire.Game` - 163 edges
6. `ClientSession` - 111 edges
7. `StatementCommand` - 103 edges
8. `Configuration` - 98 edges
9. `Grimoire.Botting.Commands.Misc.Statements` - 97 edges
10. `Grimoire.UI` - 87 edges

## Surprising Connections (you probably didn't know these)
- `4.1 `ClientSession`` --references--> `ClientSession`  [INFERRED]
  docs/desktop-multi-client-research-plan.md → app/Grimoire.Clients/ClientSession.cs
- `4.2 `ClientSessionManager`` --references--> `ClientSessionManager`  [INFERRED]
  docs/desktop-multi-client-research-plan.md → app/Grimoire.Clients/ClientSessionManager.cs
- `Scan execution checklist` --references--> `Root`  [INFERRED]
  docs/desktop-rendering-performance-research-v1.4.0.md → app/Grimoire.UI/Root.MultiClient.cs
- `User Data and Legacy Format Preservation` --semantically_similar_to--> `Legacy GBot Compatibility`  [INFERRED] [semantically similar]
  docs/releases/v1.2.0.md → README.md
- `What runs where today` --references--> `CmdSetFPS`  [INFERRED]
  docs/desktop-rendering-performance-research-v1.4.0.md → app/Grimoire.Botting.Commands.Misc/CmdSetFPS.cs

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

## Communities (293 total, 126 thin omitted)

### Community 0 - "Bot Manager Editing"
Cohesion: 0.03
Nodes (20): CmdRest, Full, CmdWhitelist, Item, State, CmdToggleProvoke, Type, CmdToggleProvokeInMap (+12 more)

### Community 1 - "system_threading_tasks"
Cohesion: 0.05
Nodes (4): Grimoire.Botting.Commands.Misc.Statements, Grimoire.Botting.Commands.Item, Grimoire.Botting.Commands.Combat, Grimoire.Game

### Community 2 - "StatementCommand"
Cohesion: 0.03
Nodes (22): QuickBotStatusMenuItem, AppVersion, Client, ClientSessions, CreateParams, customTravel, enableCapture, GameLoadProgress (+14 more)

### Community 3 - "Grimoire.UI"
Cohesion: 0.08
Nodes (6): GrimoirePluginEntry, NetworkUtils, Grimoire.Updater, Grimoire.Tools.Plugins, Grimoire, Grimoire.Utils

### Community 4 - ".Call"
Cohesion: 0.05
Nodes (20): Bank, AvailableSlots, HasBankOpen, IsBankLoaded, Items, TotalSlots, UsedSlots, Quests (+12 more)

### Community 5 - "IBotCommand"
Cohesion: 0.03
Nodes (34): CmdAttack, Monster, CmdCancelAutoAttack, CmdCancelTarget, CmdLoad, ShopId, CmdSell, ItemName (+26 more)

### Community 6 - "system_linq"
Cohesion: 0.10
Nodes (4): Grimoire.Networking.Handlers, Grimoire.Tools, Grimoire.Botting, Grimoire.Game.Data

### Community 7 - ".MoveOutOfCombat"
Cohesion: 0.11
Nodes (6): Shop, Id, IsShopLoaded, Items, Location, Name

### Community 9 - "BotUtilities"
Cohesion: 0.03
Nodes (3): BotImportMessage(), LogLine(), QuickActionButton()

### Community 10 - "UpdateService.cs"
Cohesion: 0.03
Nodes (54): BankItem, Bank, Category, CharItemId, CharItemID, Coins, Cost, Description (+46 more)

### Community 11 - "Type"
Cohesion: 0.25
Nodes (8): Type, BankItems, InventoryItems, Monsters, QuestIDs, Quests, ShopItems, TempItems

### Community 12 - "ClientOptionState"
Cohesion: 0.05
Nodes (38): ClientOptionState, Afk, AntiAfkCancellation, AntiAfkEnabled, AntiAfkTask, ChangeChat, DesiredFramesPerSecond, DisableAnimations (+30 more)

### Community 13 - "UpdateService"
Cohesion: 0.04
Nodes (53): Configuration, AFK, Author, AutoRelogin, AutoSaveState, BankOnStop, Boosts, BotDelay (+45 more)

### Community 14 - "CancellationTokenSource"
Cohesion: 0.07
Nodes (20): ClientLoginState, IsBusy, Server, Ip, IsChatRestricted, IsMemberOnly, IsOnline, Language (+12 more)

### Community 15 - "GrimoirePlugin"
Cohesion: 0.12
Nodes (12): GrimoirePlugin, Author, Description, LastError, Name, IGrimoirePlugin, Author, Description (+4 more)

### Community 16 - "Grimoire.Utils"
Cohesion: 0.10
Nodes (16): AccountRecord, Id, Label, ProtectedPassword, Username, AccountSummary, Id, Label (+8 more)

### Community 17 - "Bot"
Cohesion: 0.09
Nodes (11): Bot, Configuration, Configurations, CurrentConfiguration, DropDelay, DropsInInventory, Index, Instance (+3 more)

### Community 18 - "Quest Data Management"
Cohesion: 0.06
Nodes (26): OptionsManager, AFK, _antiAfk, ChangeChat, DisableAnimations, EnemyMagnet, HidePlayers, HideRoom (+18 more)

### Community 19 - "Bot Execution Lifecycle"
Cohesion: 0.05
Nodes (35): Quest, CanComplete, ClassPointsReward, Description, EndText, ExperienceReward, Faction, FactionId (+27 more)

### Community 20 - "SessionState"
Cohesion: 0.10
Nodes (18): RecoveryInfo, BotWasRunning, LastBotPath, LastStage, LastUpdatedUtc, StartedUtc, WasHiddenToTray, SessionState (+10 more)

### Community 21 - "StartupDiagnostics"
Cohesion: 0.06
Nodes (26): 1. Executive summary, 6.1 Product shape, 6.2 Game transport contract, 6. Recommended product architecture, Implementation checkpoint 0, GameTransport, OfflineGameTransport, OfflineGameTransportTest (+18 more)

### Community 22 - "Inventory Drop Handling"
Cohesion: 0.05
Nodes (32): InventoryItem, Category, CharItemId, Cost, Description, DropChance, EquipmentSlot, File (+24 more)

### Community 23 - "Travel and Movement"
Cohesion: 0.05
Nodes (33): InventoryItemCombined, Category, CharItemId, Coins, Cost, Description, DropChance, File (+25 more)

### Community 24 - "Set"
Cohesion: 0.15
Nodes (9): ResizableUserControl, Bottom, BottomLeft, BottomRight, Left, Right, Top, TopLeft (+1 more)

### Community 25 - "Combined Inventory Serialization"
Cohesion: 0.13
Nodes (4): Travel, Instance, travelNames, travels

### Community 26 - "Avatar Progression"
Cohesion: 0.08
Nodes (5): LegacyGbotCommand, LegacyGbotDocument, LegacyGbotException, LegacyGbotReader, LegacyGbotReaderTest

### Community 27 - "NodeEditor"
Cohesion: 0.18
Nodes (31): ActivityDock(), BotDocumentDetails(), BotImportEmptyState(), BotManagerSurface(), BotMetadata(), BrandBar(), BrandMark(), CompactAccountCard() (+23 more)

### Community 28 - "PacketSpammer"
Cohesion: 0.12
Nodes (16): HandlerAFK, HandledCommands, HandlerChat, HandledCommands, HandlerWarningsXt, HandledCommands, HandlerConnection, HandledCommands (+8 more)

### Community 29 - "Extended Avatar Factions"
Cohesion: 0.06
Nodes (32): Avatar, AccessLevel, AccessType, AFK, Auras, Cell, Class, ClassRank (+24 more)

### Community 30 - "Grimoire.UI"
Cohesion: 0.11
Nodes (3): Grimoire.UI, Grimoire.Botting.Commands.Map, Grimoire.Botting.Commands.Quest

### Community 31 - "World State Scanning"
Cohesion: 0.10
Nodes (11): Hotkey, ActionIndex, Key, Text, HotkeyBindingDefinition, ActionIndex, DefaultKey, DisplayName (+3 more)

### Community 33 - "BotData"
Cohesion: 0.07
Nodes (29): 11. Accounts, security, and privacy, 12. Lifecycle, recovery, and performance rules, 13. Updates and distribution, 14. Repository layout, 15. Testing and compatibility gates, 18. Decisions needed from the project owner, 19. Release-policy questions, 20. Recommendation (+21 more)

### Community 34 - "JsonMessage"
Cohesion: 0.05
Nodes (37): World, ActiveCutscene, AllCellInfo, AuraSyncRoot, AvailableMonsters, AvatarsInMap, AvatarsInMyCell, Cells (+29 more)

### Community 35 - "Bot File Loading"
Cohesion: 0.14
Nodes (7): IXmlMessageHandler, HandledCommands, Proxy, _catchAllPackets, _catchXtPackets, firstJoin, Instance

### Community 37 - "BotData"
Cohesion: 0.10
Nodes (21): BotData, BotCell, BotMap, BotPad, BotQuests, BotSkill, BotState, SkillSet (+13 more)

### Community 38 - "CmdBlank3"
Cohesion: 0.10
Nodes (7): CmdBlank3, Alpha, B, G, R, Text, FontInfo

### Community 39 - "UpdateRelease"
Cohesion: 0.22
Nodes (7): UpdateRelease, HtmlUrl, InstallerAsset, IsPrerelease, Name, PortableAsset, TagName

### Community 41 - "ClientWorkspaceState"
Cohesion: 0.18
Nodes (6): AccountCredentials, Id, Label, Password, Username, ClientWorkspaceState

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
Cohesion: 0.08
Nodes (14): Settings, Default, Style, Resources, Culture, defaulttext, GrimoireIcon, ResourceManager (+6 more)

### Community 46 - "Set"
Cohesion: 0.11
Nodes (6): Set, ISetInterface, Item, Name, Type, SetItem

### Community 47 - "STAThread"
Cohesion: 0.28
Nodes (3): FlashActivationSmoke, FlashLoadProgressSmoke, RenderingProfileSmoke

### Community 49 - "Portable Update Application"
Cohesion: 0.09
Nodes (14): TempInventory, Items, TempItem, Cost, Description, Id, IsAcItem, IsMemberOnly (+6 more)

### Community 51 - "IJsonMessageHandler"
Cohesion: 0.12
Nodes (12): HandlerGetQuests, HandledCommands, HandlerArea, HandledCommands, HandlerBuyItem, HandledCommands, HandlerLoadShop, HandledCommands (+4 more)

### Community 52 - "CosmeticForm"
Cohesion: 0.03
Nodes (18): BankForm, CosmeticForm, Instance, DPSForm, EyeDropper, Instance, Loaders, Instance (+10 more)

### Community 54 - "Color"
Cohesion: 0.16
Nodes (7): BridgeCompatibility, BridgeHandshake, BridgeHandshakeValidator, Compatible, Incompatible, BridgeHandshakeValidatorTest, TransportStatusTest

### Community 56 - "Monster State Data"
Cohesion: 0.06
Nodes (29): ExtendedAvatar, ActivationFlag, AdventureCoin, Bank, BankSlots, BoostCP, BoostGold, BoostRep (+21 more)

### Community 57 - "ClientSession"
Cohesion: 0.07
Nodes (22): ClientSession, BotDocument, BotEngine, DefaultDisplayName, DisplayName, FlashBridge, GameState, Id (+14 more)

### Community 59 - ".Execute"
Cohesion: 0.12
Nodes (15): AppPaths, ApplicationDirectory, BotClientConfigFile, BotsDirectory, CommunityBotsDirectory, ConfigFile, CrashReportsDirectory, DataDirectory (+7 more)

### Community 60 - "app.vue"
Cohesion: 0.06
Nodes (27): attachment, canSubmit, categories, customVersion, detailsLength, downloadUrl, fallbackIssueUrl, form (+19 more)

### Community 61 - "StartupDiagnostics"
Cohesion: 0.10
Nodes (5): Program, StartupDiagnostics, PreviousSession, StartupLogFile, StartupDiagnosticsSmoke

### Community 62 - "Game Action Locks"
Cohesion: 0.18
Nodes (7): Grimoire Exodus Agent Guide, Desktop Architecture Map, Flash and ActionScript Bridge Contract, Graphify Project Policy, Update and Release Contract, User Data and Recovery Contract, Build and Validation Policy

### Community 63 - "Command Color Editor"
Cohesion: 0.11
Nodes (13): CmdFollow, Name, CmdGotoPlayer, PlayerName, _playerName, CmdPacket, CmdPacket2, Client (+5 more)

### Community 64 - "Composable"
Cohesion: 0.06
Nodes (31): CmdQuestlist, ItemID, _itemID, QuestID, _questID, state, State, Add (+23 more)

### Community 65 - "GrimoireForm"
Cohesion: 0.13
Nodes (5): GrimoireForm, BoundClientSession, CanChangeClientSessionBinding, SupportsClientSessionBinding, SafeWinEventHookHandle

### Community 66 - "App-Local Flash Runtime"
Cohesion: 0.20
Nodes (6): Private Registration-Free Flash Runtime, App-Local Flash Runtime Guide, Flash Host ABI Requirements, Embedded Flash Runtime Research, Embedded Runtime Safeguards, Modern Installation and Update Foundation

### Community 67 - ".ExecuteCommand"
Cohesion: 0.18
Nodes (9): CmdJoin2, Cell, _cell, Map, _map, Pad, _pad, Room (+1 more)

### Community 68 - "Grabber"
Cohesion: 0.11
Nodes (18): Monster, Alive, Auras, Cell, CurrentState, Health, Id, Level (+10 more)

### Community 71 - ".Run"
Cohesion: 0.22
Nodes (5): MultiClientBotIsolationSmoke, RecordingCommand, ExecutionAfterAwait, ExecutionCount, FirstExecution

### Community 72 - "website/package.json"
Cohesion: 0.12
Nodes (14): @lucide/vue, nuxt, @nuxtjs/tailwindcss, tailwindcss, @types/node, typescript, vue, vue-router (+6 more)

### Community 73 - "LegacyGbotReader.kt"
Cohesion: 0.24
Nodes (14): Assert-FileHash(), Complete-BrandedOutput(), Get-BundledFlashRuntime(), Get-ExodusProductVersion(), Get-MSBuildPath(), Get-WixToolset(), Invoke-ExodusBuild(), Invoke-NuGetRestore() (+6 more)

### Community 74 - "IJsonMessageHandler"
Cohesion: 0.12
Nodes (11): HARMAN AIR Game Runtime, Dual Desktop and AIR Host Abstraction, Grimoire Exodus Mobile Overview, Android Module Architecture, Android Foundation, Shared Cross-Platform Contracts, Standalone HARMAN AIR Client, Cross-Platform Contracts (+3 more)

### Community 75 - ".Execute"
Cohesion: 0.08
Nodes (4): CmdJoin, Cell, Map, Pad

### Community 76 - "Grimoire Exodus v1.1.0 Code and Performance Audit"
Cohesion: 0.12
Nodes (15): LockActions, AcceptQuest, BuyItem, DoIA, EquipItem, GetMapItem, LoadEnhShop, LoadHairShop (+7 more)

### Community 77 - "PacketSpammer"
Cohesion: 0.13
Nodes (4): PacketSpammer, CanChangeClientSessionBinding, Instance, SupportsClientSessionBinding

### Community 78 - "DllImport"
Cohesion: 0.12
Nodes (11): Android Stop-Go Gates, Build, Captive-Runtime AQW Client, Physical Android Device Validation Gap, Direct-Stage Command-Center Overlay, Grimoire Exodus AIR client, AIR Local Callback Registry, Prerequisites (+3 more)

### Community 79 - "Base Item Data"
Cohesion: 0.09
Nodes (15): MSI and Portable Package Build, Checksummed GitHub Release Publication, Release Compatibility Smoke Tests, Release Tag Validation, Tagged Windows Build, Grimoire Exodus v1.1.0 Code and Performance Audit, 2026-10-15 Implementation Outcome, Legacy Compatibility Guardrails (+7 more)

### Community 80 - "Item Drop Stack"
Cohesion: 0.12
Nodes (15): description, devDependencies, @commitlint/cli, @commitlint/config-conventional, husky, name, private, scripts (+7 more)

### Community 82 - "GameCapability"
Cohesion: 0.18
Nodes (11): GameCapability, CombatAct, InventoryAct, InventoryRead, NavigationMove, QuestAct, QuestRead, SessionObserve (+3 more)

### Community 83 - "Dictionary"
Cohesion: 0.13
Nodes (6): BoolConverter, IntStringConverter, QuestReqConverter, QuestRewardConverter, StringBoolConverter, TrimConverter

### Community 84 - "Kill For Item Logic"
Cohesion: 0.14
Nodes (12): CompletedQuest, Id, Name, Reward, Success, QuestReward, ClassPoints, Experience (+4 more)

### Community 85 - "Skill"
Cohesion: 0.07
Nodes (13): Skill, ComparisonType, Index, SafeHp, SafeMp, SafePercentage, Text, Type (+5 more)

### Community 86 - "AppPreferenceSnapshot"
Cohesion: 0.10
Nodes (19): AppPreferences, AppPreferenceSnapshot, ActiveClientFramesPerSecond, AllowUnlimitedClients, ConfirmOnClose, HideDeviceStats, InactiveClientFramesPerSecond, MinimizeToTray (+11 more)

### Community 87 - "ClientSessionManager"
Cohesion: 0.26
Nodes (7): ClientSessionEventArgs, Session, ClientSessionManager, ActiveSession, Default, MaximumSessions, Sessions

### Community 89 - "Bot Library Experience"
Cohesion: 0.14
Nodes (6): Android Landscape Command Center, Typed GameTransport Contract, Platform-Neutral Bot Engine, Jetpack Compose Management Shell, Game bridge protocol v1, Secret-Free Bridge Handshake

### Community 90 - ".HandleAsync"
Cohesion: 0.12
Nodes (12): Issue Intake Configuration, Private Security Reporting, Contributor Covenant Code of Conduct, Harassment-Free Community, Contribution Guide, Contribution Testing Policy, Maintained AQW Automation Client, Grimoire Exodus (+4 more)

### Community 91 - "Equipment Type Definitions"
Cohesion: 0.13
Nodes (10): BotClientConfig, Contents, file, Instance, 8. Persistence, recovery, shutdown, and updates, Closing one client, Closing or updating the app, Recovery (+2 more)

### Community 92 - "ActiveXFlashBridge"
Cohesion: 0.06
Nodes (13): IFlashBridge, Control, 13. Recommended first implementation slice, ClientLimitSmoke, FakeFlashBridge, Control, FakeFlashBridge, Control (+5 more)

### Community 93 - "index.mjs"
Cohesion: 0.23
Nodes (11): allowedCategories, allowedOrigin(), buildIssueBody(), cleanInline(), cleanMultiline(), cleanRepositoryPart(), corsHeaders(), fetch() (+3 more)

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

### Community 102 - "21. Sources"
Cohesion: 0.40
Nodes (5): 21. Sources, AIR and alternative-stack sources, Android and distribution sources, Official Artix sources, Repository evidence used for this plan

### Community 103 - ".UseSkills"
Cohesion: 0.18
Nodes (3): CmdKill, Monster, Packet

### Community 104 - "BotUtilities"
Cohesion: 0.22
Nodes (5): BotUtilities, ActiveBot, BusyClient, HasLoadedQuests, ShouldUseSkill

### Community 105 - "Aura"
Cohesion: 0.17
Nodes (7): Aura, disableType, Duration, entityID, Name, realValue, Value

### Community 106 - "CmdAcceptQuest"
Cohesion: 0.29
Nodes (4): CmdAcceptQuest, Quest, CmdCompleteQuest, Quest

### Community 107 - "Grimoire Exodus v1.3.0 Release Notes"
Cohesion: 0.12
Nodes (12): Legacy GBot Compatibility Contract, Legacy-Compatible GBot Export Strategy, Centralized Application Settings, Configurable Global Shortcuts, Consistent Native Window Theming, Privacy-Safe Reset Controls, Grimoire Exodus v1.3.0 Release Notes, Legacy GBot Compatibility (+4 more)

### Community 108 - "Config"
Cohesion: 0.10
Nodes (6): Config, Contents, file, Instance, AtomicFile, EnumerableUtils

### Community 111 - "State"
Cohesion: 0.15
Nodes (8): CmdInt, Int, type, Value, Types, Lower, Set, Upper

### Community 112 - "BotDocumentState"
Cohesion: 0.05
Nodes (39): BotDocumentState, Configuration, DisplayName, HasSnapshot, IsDirty, SourcePath, 10. Validation plan, 11. Risk register (+31 more)

### Community 113 - "ActiveXFlashBridge"
Cohesion: 0.10
Nodes (4): ActiveXFlashBridge, Control, Flash, Instance

### Community 114 - ".BindToSession"
Cohesion: 0.09
Nodes (3): BotDocumentChangedEventArgs, ConfigurationChanged, MetadataChanged

### Community 116 - "Spammer"
Cohesion: 0.17
Nodes (4): Spammer, Instance, IsRunning, LastError

### Community 117 - "Message"
Cohesion: 0.22
Nodes (7): CmdIndex, Index, Type, IndexCommand, Down, Goto, Up

### Community 118 - "AutoBuyBack"
Cohesion: 0.20
Nodes (8): AutoBuyBack, Password, Username, BuyBackPage, EventArgument, EventValidation, ViewState, ViewStateGenerator

### Community 119 - "CmdLog"
Cohesion: 0.25
Nodes (4): CmdLog, Clear, Debug, Text

### Community 121 - "Dark Fantasy Branding"
Cohesion: 0.22
Nodes (9): 10. Android UX and navigation, Accessibility and ownership boundaries, Android equivalents, Color and component language, Design acceptance criteria, Landscape command center, Primary destinations, Typography (+1 more)

### Community 122 - "CmdBuy2"
Cohesion: 0.22
Nodes (9): 16. Phased roadmap, Overall range, Phase 0 — architecture and offline foundation (complete), Phase 1 — standalone AIR client proof (in progress), Phase 2 — integrated product foundation (6–10 weeks), Phase 3 — legacy GBot engine and core commands (10–20 weeks), Phase 4 — game integration and product parity (12–28 weeks), Phase 5 — advanced tools and plug-in replacement (12–24+ weeks) (+1 more)

### Community 123 - "JsonMessage"
Cohesion: 0.11
Nodes (12): HandlerDrops, HandledCommands, HandlerAura, HandledCommands, HandlerInfo, HandledCommands, HandlerSkills, HandledCommands (+4 more)

### Community 125 - "CmdBuy2"
Cohesion: 0.40
Nodes (4): CmdBuy2, ItemName, Manual, ShopId

### Community 126 - "NodeEditor"
Cohesion: 0.10
Nodes (3): NodeEditor, TypedCommand, Notepad

### Community 127 - "Grimoire Exodus AIR client"
Cohesion: 0.25
Nodes (8): StatusIcon, Cpu, EyeClosed, EyeOpen, Network, Settings, UpdateAvailable, Uptime

### Community 128 - "CmdPacketlist2"
Cohesion: 0.29
Nodes (4): CmdPacketlist2, Delay, Packet, Type

### Community 131 - "KeyboardHook"
Cohesion: 0.29
Nodes (7): Access, Developer, LeadDeveloper, Moderator, Player, Staff, Tester

### Community 132 - "PluginManager"
Cohesion: 0.19
Nodes (3): PluginManager, Instance, LastError

### Community 134 - "PreparedUpdate"
Cohesion: 0.20
Nodes (7): PreparedUpdate, InstallerMode, PackagePath, PayloadDirectory, Release, RequiresElevation, UpdaterPath

### Community 135 - "CmdEquipSet"
Cohesion: 0.17
Nodes (5): BotJson, CmdEquipSet, ItemName, 8. Reuse matrix, LegacyGbotCompatibilitySmoke

### Community 136 - "ClientSessionState"
Cohesion: 0.22
Nodes (8): ClientSessionState, Closing, Created, Disposed, Faulted, Initializing, Ready, Running

### Community 137 - "Inventory"
Cohesion: 0.33
Nodes (5): CmdMoveToCell, Cell, _cell, Pad, _pad

### Community 138 - "CmdWalk"
Cohesion: 0.29
Nodes (6): CmdWalk, Type, X, _x, Y, _y

### Community 139 - "CmdBuyByID"
Cohesion: 0.33
Nodes (5): CmdBuyByID, ItemID, Manual, ShopID, ShopItemID

### Community 140 - "CmdBuyFastByID"
Cohesion: 0.33
Nodes (5): CmdBuyFastByID, ItemID, Manual, ShopID, ShopItemID

### Community 141 - "CmdBankSwap"
Cohesion: 0.50
Nodes (3): CmdBankSwap, BankItemName, InventoryItemName

### Community 142 - "CosmeticEquipment"
Cohesion: 0.18
Nodes (7): CosmeticEquipment, ID, Link, Meta, Slot, SWFFile, Type

### Community 143 - "SingleInstanceBroker"
Cohesion: 0.16
Nodes (4): SingleInstanceBroker, IsPrimaryInstance, TimeLimiter, SingleInstanceBrokerSmoke

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

### Community 162 - "Message"
Cohesion: 0.20
Nodes (6): Message, Command, RawContent, Send, XmlMessage, Body

### Community 163 - "CmdBankTransfer"
Cohesion: 0.50
Nodes (3): CmdBankTransfer, ItemName, TransferFromBank

### Community 164 - "CmdEquip"
Cohesion: 0.50
Nodes (3): CmdEquip, ItemName, Safe

### Community 165 - "CmdBuyBack"
Cohesion: 0.50
Nodes (3): CmdBuyBack, ItemName, PageNumberCap

### Community 166 - ".SelectUpdate"
Cohesion: 0.20
Nodes (6): UpdateAsset, DownloadUrl, Name, Sha256, Size, Version

### Community 168 - "CmdAcceptQuest2"
Cohesion: 0.50
Nodes (3): CmdAcceptQuest2, QuestID, _questID

### Community 170 - "countWords"
Cohesion: 0.67
Nodes (3): countWords(), detailsWordCount, subjectWordCount

### Community 171 - "UserFriendlyCommandEditor"
Cohesion: 0.08
Nodes (4): UserFriendlyCommandEditor, cmd, cmdObj, commandEditor

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

### Community 185 - "BotRunContext"
Cohesion: 0.29
Nodes (6): BotRunContext, BotTask, Cancellation, Configuration, DropTask, QuestTask

### Community 186 - "devDependencies"
Cohesion: 0.29
Nodes (7): devDependencies, @nuxtjs/tailwindcss, tailwindcss, @types/node, typescript, vue-tsc, wrangler

### Community 188 - "scripts"
Cohesion: 0.20
Nodes (10): scripts, build, dev, generate, preview, typecheck, worker:check, worker:deploy (+2 more)

### Community 190 - "ShopInfo"
Cohesion: 0.20
Nodes (10): ShopInfo, Field, House, Id, Index, Items, Location, Member (+2 more)

### Community 191 - "Player"
Cohesion: 0.04
Nodes (38): Player, ActivationFlag, AllSkillsAvailable, Bank, Cell, CharUserID, Class, CurrentState (+30 more)

### Community 194 - "CmdUseSkill"
Cohesion: 0.29
Nodes (6): CmdUseSkill, Index, SafeHp, SafeMp, Skill, Wait

### Community 196 - "GameSessionState"
Cohesion: 0.04
Nodes (49): GameSessionState, ActiveBot, AuraSyncRoot, Bank, BotCell, BotConfigurations, BotDropsInInventory, BotMap (+41 more)

### Community 203 - "ActiveClientSessionChangedEventArgs"
Cohesion: 0.29
Nodes (3): ActiveClientSessionChangedEventArgs, Current, Previous

### Community 206 - "Q: Why is Grimoire stuck at Loading AQW 0%?"
Cohesion: 0.40
Nodes (4): Answer, Outcome, Q: Why is Grimoire stuck at Loading AQW 0%?, Source Nodes

### Community 207 - "dependencies"
Cohesion: 0.40
Nodes (5): dependencies, @lucide/vue, nuxt, vue, vue-router

### Community 209 - "Inventory"
Cohesion: 0.29
Nodes (5): Inventory, AvailableSlots, Items, MaxSlots, UsedSlots

### Community 214 - "FlashRenderQuality"
Cohesion: 0.40
Nodes (5): FlashRenderQuality, Automatic, High, Low, Medium

### Community 217 - "Grimoire Exodus website"
Cohesion: 0.50
Nodes (3): Grimoire Exodus website, Local development, Production output

### Community 219 - "Desktop rendering performance research for v1.4.0"
Cohesion: 0.07
Nodes (26): CmdSetFPS, FPS, Answers to the feature questions, Can AQW or the bot engine be moved to the GPU?, Can it be selected per client?, Compatibility spike and benchmark matrix, Decision, Desktop rendering performance research for v1.4.0 (+18 more)

### Community 223 - "Task"
Cohesion: 0.05
Nodes (5): IBotEngine, Configuration, CurrentConfiguration, Index, IsRunning

### Community 226 - "StatementCommand"
Cohesion: 0.02
Nodes (45): CmdAvailableMonstersGreaterThan, CmdAvailableMonstersLessThan, CmdEquipped, CmdFactionRankLessThan, CmdGoldLessThan, CmdHealthLessThan, CmdHealthPercentageGreaterThan, CmdInCombat (+37 more)

### Community 234 - "LogForm"
Cohesion: 0.09
Nodes (5): DebugLogger, LogForm, Instance, SelectedLog, BoundedTextBuffer

### Community 235 - "House"
Cohesion: 0.50
Nodes (3): House, Items, TotalSlots

### Community 291 - "GitHub issue relay Worker"
Cohesion: 0.50
Nodes (3): Credential, GitHub issue relay Worker, Validate and deploy

### Community 294 - "buildComposerUrl"
Cohesion: 0.67
Nodes (3): buildComposerUrl(), cleanInline(), submitIssue()

## Knowledge Gaps
- **1252 isolated node(s):** `Monster`, `Monster`, `Packet`, `Monster`, `ItemName` (+1247 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1996 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **126 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Root` connect `StatementCommand` to `CommandNode`, `Grimoire.UI`, `PluginManager`, `.Call`, `PreparedUpdate`, `Grimoire.Botting`, `CancellationTokenSource`, `Grimoire.Utils`, `SessionState`, `PurpleMenuRenderer`, `.MenuMain_MouseDown`, `CmdBlank3`, `UpdateRelease`, `.SelectUpdate`, `ClientWorkspaceState`, `Dictionary`, `Color`, `Application Path Management`, `AxShockwaveFlash`, `CosmeticForm`, `Packet Command Handling`, `.Push`, `NonClosableVSTabControl`, `ClientSession`, `StartupDiagnostics`, `GrimoireForm`, `ActiveClientSessionChangedEventArgs`, `AppPreferenceSnapshot`, `ClientSessionManager`, `Desktop rendering performance research for v1.4.0`, `Aura`, `.CreateClientSession`, `.FullReset_Click`, `GrimoireHomeBridge`, `Grimoire Exodus AIR client`?**
  _High betweenness centrality (0.166) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `BotManager` (e.g. with `Why not make .NET MAUI the default?` and `4.6 Per-session bot model`) actually correct?**
  _`BotManager` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `Monster`, `Monster`, `Packet` to the rest of the system?**
  _1252 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Bot Manager Editing` be split into smaller, more focused modules?**
  _Cohesion score 0.025734245794126035 - nodes in this community are weakly interconnected._
- **Why does `BotManager` connect `Bot Manager Editing` to `CmdPacketlist2`, `Action`, `StatementCommand`, `PluginManager`, `.Call`, `CmdEquipSet`, `UpdateService`, `CancellationTokenSource`, `SingleInstanceBroker`, `Bot Execution Lifecycle`, `Combined Inventory Serialization`, `Grimoire.UI`, `BotData`, `CmdBlank3`, `.LoadBotFromFile`, `Dictionary`, `ClientWorkspaceState`, `UserFriendlyCommandEditor`, `Color`, `Application Path Management`, `CosmeticForm`, `Packet Command Handling`, `NonClosableVSTabControl`, `ClientSession`, `GrimoireForm`, `Skill`, `ClientSessionManager`, `Task`, `StatementCommand`, `LogForm`, `State`, `BotDocumentState`, `.BindToSession`, `CmdLog`, `NodeEditor`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `Root` (e.g. with `.Main()` and `Scan execution checklist`) actually correct?**
  _`Root` has 2 INFERRED edges - model-reasoned connections that need verification._
- **Should `system_threading_tasks` be split into smaller, more focused modules?**
  _Cohesion score 0.04504864720778355 - nodes in this community are weakly interconnected._