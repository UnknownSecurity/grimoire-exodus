package com.unknownsecurity.grimoireexodus.model

import kotlin.test.Test
import kotlin.test.assertContains

class BridgeContractTest {
    @Test
    fun `model capability names remain represented in the shared v1 schema`() {
        val schema = checkNotNull(
            javaClass.getResource("/game-bridge/v1/bridge-handshake.schema.json"),
        ).readText()

        GameCapability.entries.forEach { capability ->
            assertContains(schema, "\"${capability.wireName}\"")
        }
    }
}
