package com.unknownsecurity.grimoireexodus.model

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertIs
import kotlin.test.assertTrue

class BridgeHandshakeValidatorTest {
    @Test
    fun `accepts a supported bridge and maps capabilities`() {
        val result = BridgeHandshakeValidator.validate(
            BridgeHandshake(
                protocol = BridgeHandshakeValidator.PROTOCOL,
                protocolVersion = BridgeHandshakeValidator.PROTOCOL_VERSION,
                clientVersion = "test-client",
                bridgeVersion = "test-bridge",
                capabilities = listOf("state.read", "session.observe"),
            ),
        )

        val compatible = assertIs<BridgeCompatibility.Compatible>(result)
        assertEquals(ConnectionState.Ready, compatible.status.connection)
        assertTrue(compatible.status.canRunBots)
    }

    @Test
    fun `rejects unknown and duplicate capabilities`() {
        val unknown = validHandshake(capabilities = listOf("state.read", "unsafe.reflect"))
        val duplicate = validHandshake(capabilities = listOf("state.read", "state.read"))

        assertIs<BridgeCompatibility.Incompatible>(BridgeHandshakeValidator.validate(unknown))
        assertIs<BridgeCompatibility.Incompatible>(BridgeHandshakeValidator.validate(duplicate))
    }

    @Test
    fun `compatible bridge without observation cannot run bots`() {
        val result = BridgeHandshakeValidator.validate(
            validHandshake(capabilities = listOf("state.read")),
        )

        val compatible = assertIs<BridgeCompatibility.Compatible>(result)
        assertFalse(compatible.status.canRunBots)
    }

    private fun validHandshake(capabilities: List<String>) = BridgeHandshake(
        protocol = BridgeHandshakeValidator.PROTOCOL,
        protocolVersion = BridgeHandshakeValidator.PROTOCOL_VERSION,
        clientVersion = "test-client",
        bridgeVersion = "test-bridge",
        capabilities = capabilities,
    )
}
