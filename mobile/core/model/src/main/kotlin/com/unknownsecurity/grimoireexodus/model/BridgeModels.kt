package com.unknownsecurity.grimoireexodus.model

/**
 * Stable states exposed by every game transport. Platform adapters can have
 * richer internal state, but UI and bot logic must fail closed on these values.
 */
enum class ConnectionState {
    Offline,
    Connecting,
    Ready,
    Incompatible,
    Failed,
}

enum class GameCapability(val wireName: String) {
    StateRead("state.read"),
    SessionObserve("session.observe"),
    NavigationMove("navigation.move"),
    CombatAct("combat.act"),
    InventoryRead("inventory.read"),
    InventoryAct("inventory.act"),
    QuestRead("quest.read"),
    QuestAct("quest.act"),
    ShopRead("shop.read"),
    ShopAct("shop.act"),
}

data class TransportStatus(
    val connection: ConnectionState,
    val summary: String,
    val clientVersion: String? = null,
    val bridgeVersion: String? = null,
    val capabilities: Set<GameCapability> = emptySet(),
) {
    val canRunBots: Boolean
        get() = connection == ConnectionState.Ready &&
            GameCapability.StateRead in capabilities &&
            GameCapability.SessionObserve in capabilities
}

sealed interface GameAction {
    data class MoveToCell(val cell: String, val pad: String) : GameAction
    data class UseSkill(val slot: Int) : GameAction
    data object Stop : GameAction
}

sealed interface ActionResult {
    data object Accepted : ActionResult
    data class Rejected(val reason: String) : ActionResult
}
