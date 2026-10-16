package com.unknownsecurity.grimoireexodus.legacygbot

import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonArray
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.intOrNull
import kotlinx.serialization.json.jsonObject
import java.io.ByteArrayOutputStream
import java.io.InputStream
import java.nio.charset.CharacterCodingException

/**
 * Reads legacy Newtonsoft-style GBots as inert JSON data. It never resolves a
 * CLR type name, loads a class, executes a command, or touches the filesystem.
 */
class LegacyGbotReader(
    private val maximumBytes: Int = DEFAULT_MAXIMUM_BYTES,
    private val maximumDepth: Int = DEFAULT_MAXIMUM_DEPTH,
) {
    private val json = Json

    /**
     * Reads from a user-selected document without first trusting its reported
     * size. The extra byte lets us reject oversized streams before parsing.
     */
    fun read(input: InputStream): LegacyGbotDocument {
        val output = ByteArrayOutputStream(minOf(maximumBytes, STREAM_BUFFER_SIZE))
        val buffer = ByteArray(STREAM_BUFFER_SIZE)
        var total = 0

        while (true) {
            val count = input.read(buffer)
            if (count < 0) break
            if (count == 0) continue

            total += count
            if (total > maximumBytes) {
                throw LegacyGbotException("GBot exceeds the $maximumBytes byte safety limit.")
            }
            output.write(buffer, 0, count)
        }

        return read(output.toByteArray())
    }

    fun read(bytes: ByteArray): LegacyGbotDocument {
        if (bytes.size > maximumBytes) {
            throw LegacyGbotException("GBot exceeds the $maximumBytes byte safety limit.")
        }

        val source = try {
            bytes.decodeToString(throwOnInvalidSequence = true)
        } catch (exception: CharacterCodingException) {
            throw LegacyGbotException("GBot is not valid UTF-8.", exception)
        }

        validateNesting(source)

        val root = try {
            json.parseToJsonElement(source).jsonObject
        } catch (exception: SerializationException) {
            throw LegacyGbotException("GBot is not valid JSON.", exception)
        } catch (exception: IllegalArgumentException) {
            throw LegacyGbotException("GBot root must be a JSON object.", exception)
        }

        val rootType = readType(root, "root")
        if (rootType != CONFIGURATION_TYPE) {
            throw LegacyGbotException("Unsupported GBot root type: $rootType")
        }

        val commandContainer = root["Commands"] as? JsonObject
            ?: throw LegacyGbotException("GBot Commands must be an object.")
        validateCommandListType(commandContainer)

        val commandValues = commandContainer["\$values"] as? JsonArray
            ?: throw LegacyGbotException("GBot Commands.\$values must be an array.")
        val commands = commandValues.mapIndexed { index, element ->
            val command = element as? JsonObject
                ?: throw LegacyGbotException("GBot command at index $index must be an object.")
            val typeName = readType(command, "command at index $index")
            if (!typeName.startsWith(COMMAND_NAMESPACE)) {
                throw LegacyGbotException("Disallowed GBot command type: $typeName")
            }
            LegacyGbotCommand(
                typeName = typeName,
                properties = JsonObject(command.filterKeys { it != TYPE_PROPERTY }),
            )
        }

        return LegacyGbotDocument(
            rootType = rootType,
            author = root.stringOrNull("Author"),
            description = root.stringOrNull("Description"),
            botDelay = (root["BotDelay"] as? JsonPrimitive)?.intOrNull,
            commands = commands,
            source = root,
        )
    }

    /**
     * Produces a semantic JSON round trip of the inspected source. Editing and
     * legacy-compatible export are deliberately deferred until command parity
     * tests exist.
     */
    internal fun writeInspected(document: LegacyGbotDocument): ByteArray =
        json.encodeToString(JsonElement.serializer(), document.source).encodeToByteArray()

    private fun readType(value: JsonObject, location: String): String {
        val rawType = (value[TYPE_PROPERTY] as? JsonPrimitive)?.contentOrNull
            ?: throw LegacyGbotException("GBot $location is missing \$type metadata.")
        return rawType.substringBefore(',').trim().ifEmpty {
            throw LegacyGbotException("GBot $location has empty \$type metadata.")
        }
    }

    private fun validateCommandListType(container: JsonObject) {
        val rawType = (container[TYPE_PROPERTY] as? JsonPrimitive)?.contentOrNull
            ?: throw LegacyGbotException("GBot command list is missing \$type metadata.")
        if (!rawType.startsWith(LIST_TYPE_PREFIX) || !rawType.contains(BOT_COMMAND_TYPE)) {
            throw LegacyGbotException("Unsupported GBot command list type.")
        }
    }

    private fun validateNesting(source: String) {
        var depth = 0
        var insideString = false
        var escaped = false

        source.forEach { character ->
            if (insideString) {
                when {
                    escaped -> escaped = false
                    character == '\\' -> escaped = true
                    character == '"' -> insideString = false
                }
            } else {
                when (character) {
                    '"' -> insideString = true
                    '{', '[' -> {
                        depth += 1
                        if (depth > maximumDepth) {
                            throw LegacyGbotException(
                                "GBot exceeds the maximum nesting depth of $maximumDepth.",
                            )
                        }
                    }
                    '}', ']' -> {
                        depth -= 1
                        if (depth < 0) {
                            throw LegacyGbotException("GBot contains unbalanced JSON delimiters.")
                        }
                    }
                }
            }
        }

        if (insideString || depth != 0) {
            throw LegacyGbotException("GBot contains incomplete JSON.")
        }
    }

    private fun JsonObject.stringOrNull(name: String): String? =
        (this[name] as? JsonPrimitive)?.contentOrNull

    companion object {
        const val DEFAULT_MAXIMUM_BYTES: Int = 32 * 1024 * 1024
        const val DEFAULT_MAXIMUM_DEPTH: Int = 128

        private const val STREAM_BUFFER_SIZE = 8 * 1024

        private const val TYPE_PROPERTY = "\$type"
        private const val CONFIGURATION_TYPE = "Grimoire.Botting.Configuration"
        private const val COMMAND_NAMESPACE = "Grimoire.Botting.Commands."
        private const val BOT_COMMAND_TYPE = "Grimoire.Botting.IBotCommand"
        private const val LIST_TYPE_PREFIX = "System.Collections.Generic.List`1"
    }
}
