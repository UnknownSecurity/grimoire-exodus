package com.unknownsecurity.grimoireexodus.model

import kotlin.test.Test
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class TransportStatusTest {
    @Test
    fun `bot execution requires a ready observable state transport`() {
        val ready = TransportStatus(
            connection = ConnectionState.Ready,
            summary = "Ready",
            capabilities = setOf(
                GameCapability.StateRead,
                GameCapability.SessionObserve,
            ),
        )

        assertTrue(ready.canRunBots)
        assertFalse(ready.copy(capabilities = setOf(GameCapability.StateRead)).canRunBots)
        assertFalse(ready.copy(connection = ConnectionState.Incompatible).canRunBots)
    }
}
