package com.unknownsecurity.grimoireexodus.model

/**
 * Transport-neutral representation of the first message expected from an
 * authorized game bridge. It intentionally carries no credentials, account
 * identifiers, packet data, or implementation-specific handles.
 */
data class BridgeHandshake(
    val protocol: String,
    val protocolVersion: Int,
    val clientVersion: String,
    val bridgeVersion: String,
    val capabilities: List<String>,
)

sealed interface BridgeCompatibility {
    data class Compatible(val status: TransportStatus) : BridgeCompatibility
    data class Incompatible(val reason: String) : BridgeCompatibility
}

/**
 * Converts a raw v1 handshake into trusted application state. Unknown or
 * malformed values fail closed before bot logic receives a transport.
 */
object BridgeHandshakeValidator {
    const val PROTOCOL = "grimoire-game-bridge"
    const val PROTOCOL_VERSION = 1

    fun validate(handshake: BridgeHandshake): BridgeCompatibility {
        if (handshake.protocol != PROTOCOL) {
            return BridgeCompatibility.Incompatible("Unsupported game bridge protocol.")
        }
        if (handshake.protocolVersion != PROTOCOL_VERSION) {
            return BridgeCompatibility.Incompatible("Unsupported game bridge protocol version.")
        }
        if (!handshake.clientVersion.isSafeVersion() || !handshake.bridgeVersion.isSafeVersion()) {
            return BridgeCompatibility.Incompatible("Game or bridge version is missing or invalid.")
        }
        if (handshake.capabilities.size > MAXIMUM_CAPABILITIES ||
            handshake.capabilities.distinct().size != handshake.capabilities.size
        ) {
            return BridgeCompatibility.Incompatible("Bridge capabilities are invalid.")
        }

        val capabilitiesByWireName = GameCapability.entries.associateBy(GameCapability::wireName)
        val capabilities = handshake.capabilities.map { wireName ->
            capabilitiesByWireName[wireName]
                ?: return BridgeCompatibility.Incompatible("Bridge advertised an unknown capability.")
        }.toSet()

        return BridgeCompatibility.Compatible(
            TransportStatus(
                connection = ConnectionState.Ready,
                summary = "Authorized game bridge connected",
                clientVersion = handshake.clientVersion,
                bridgeVersion = handshake.bridgeVersion,
                capabilities = capabilities,
            ),
        )
    }

    private fun String.isSafeVersion(): Boolean =
        isNotBlank() && length <= MAXIMUM_VERSION_LENGTH && all { !it.isISOControl() }

    private const val MAXIMUM_VERSION_LENGTH = 64
    private const val MAXIMUM_CAPABILITIES = 64
}
