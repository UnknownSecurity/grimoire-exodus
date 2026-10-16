package com.unknownsecurity.grimoireexodus.domain

import com.unknownsecurity.grimoireexodus.model.ActionResult
import com.unknownsecurity.grimoireexodus.model.ConnectionState
import com.unknownsecurity.grimoireexodus.model.GameAction
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertIs

class OfflineGameTransportTest {
    @Test
    fun `offline transport rejects actions without changing state`() = runTest {
        val transport = OfflineGameTransport()

        val result = transport.execute(GameAction.UseSkill(slot = 1))

        assertIs<ActionResult.Rejected>(result)
        assertEquals(ConnectionState.Offline, transport.status.value.connection)
        assertEquals(emptySet(), transport.status.value.capabilities)
    }
}
