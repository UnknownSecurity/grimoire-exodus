package com.unknownsecurity.grimoireexodus.domain

import com.unknownsecurity.grimoireexodus.model.ActionResult
import com.unknownsecurity.grimoireexodus.model.GameAction
import com.unknownsecurity.grimoireexodus.model.TransportStatus
import kotlinx.coroutines.flow.StateFlow

/**
 * The only boundary through which portable bot logic may observe or act on a
 * game client. Implementations must be capability-gated and explicitly
 * authorized for their target client.
 */
interface GameTransport {
    val status: StateFlow<TransportStatus>

    suspend fun connect()

    suspend fun execute(action: GameAction): ActionResult

    suspend fun disconnect()
}
