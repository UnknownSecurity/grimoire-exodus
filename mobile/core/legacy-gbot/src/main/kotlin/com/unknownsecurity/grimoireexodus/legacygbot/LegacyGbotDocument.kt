package com.unknownsecurity.grimoireexodus.legacygbot

import kotlinx.serialization.json.JsonObject

@ConsistentCopyVisibility
data class LegacyGbotDocument internal constructor(
    val rootType: String,
    val author: String?,
    val description: String?,
    val botDelay: Int?,
    val commands: List<LegacyGbotCommand>,
    internal val source: JsonObject,
)

data class LegacyGbotCommand(
    val typeName: String,
    val properties: JsonObject,
)
