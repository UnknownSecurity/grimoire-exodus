package com.unknownsecurity.grimoireexodus.domain

import com.unknownsecurity.grimoireexodus.model.ActionResult
import com.unknownsecurity.grimoireexodus.model.ConnectionState
import com.unknownsecurity.grimoireexodus.model.GameAction
import com.unknownsecurity.grimoireexodus.model.TransportStatus
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

/**
 * Safe development transport. It never touches a game client, network socket,
 * account, or filesystem and rejects every action.
 */
class OfflineGameTransport : GameTransport {
    private val mutableStatus = MutableStateFlow(offlineStatus())

    override val status: StateFlow<TransportStatus> = mutableStatus.asStateFlow()

    override suspend fun connect() {
        mutableStatus.value = offlineStatus()
    }

    override suspend fun execute(action: GameAction): ActionResult =
        ActionResult.Rejected("No approved game bridge is configured.")

    override suspend fun disconnect() {
        mutableStatus.value = offlineStatus()
    }

    private fun offlineStatus() = TransportStatus(
        connection = ConnectionState.Offline,
        summary = "Awaiting an approved game bridge",
    )
}
