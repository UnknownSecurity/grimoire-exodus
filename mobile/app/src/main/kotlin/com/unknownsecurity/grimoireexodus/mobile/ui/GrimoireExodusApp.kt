package com.unknownsecurity.grimoireexodus.mobile.ui

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxWithConstraints
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.safeDrawingPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.outlined.InsertDriveFile
import androidx.compose.material.icons.outlined.AccountCircle
import androidx.compose.material.icons.outlined.CheckCircle
import androidx.compose.material.icons.outlined.Code
import androidx.compose.material.icons.outlined.ErrorOutline
import androidx.compose.material.icons.outlined.FolderOpen
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.PlayArrow
import androidx.compose.material.icons.outlined.Settings
import androidx.compose.material.icons.outlined.Terminal
import androidx.compose.material.icons.outlined.Visibility
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.unknownsecurity.grimoireexodus.designsystem.ArcanePanel
import com.unknownsecurity.grimoireexodus.designsystem.ExodusColors
import com.unknownsecurity.grimoireexodus.domain.GameTransport
import com.unknownsecurity.grimoireexodus.domain.OfflineGameTransport
import com.unknownsecurity.grimoireexodus.legacygbot.LegacyGbotDocument
import com.unknownsecurity.grimoireexodus.legacygbot.LegacyGbotReader
import com.unknownsecurity.grimoireexodus.mobile.R
import com.unknownsecurity.grimoireexodus.model.ConnectionState
import com.unknownsecurity.grimoireexodus.model.TransportStatus
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

private enum class Destination(
    val title: String,
    val icon: ImageVector,
) {
    Home("Command Center", Icons.Outlined.Home),
    Bots("Bots", Icons.Outlined.FolderOpen),
    Editor("Editor", Icons.Outlined.Code),
    Run("Run", Icons.Outlined.Terminal),
    Settings("Settings", Icons.Outlined.Settings),
}

private sealed interface BotImportState {
    data object Empty : BotImportState
    data object Loading : BotImportState
    data class Loaded(val document: LegacyGbotDocument) : BotImportState
    data class Failed(val message: String) : BotImportState
}

@Composable
fun GrimoireExodusApp(
    transport: GameTransport,
    modifier: Modifier = Modifier,
) {
    val status by transport.status.collectAsStateWithLifecycle()
    val context = LocalContext.current
    val coroutineScope = rememberCoroutineScope()
    val botReader = remember { LegacyGbotReader() }
    var destinationName by rememberSaveable { mutableStateOf(Destination.Home.name) }
    var botImportState by remember { mutableStateOf<BotImportState>(BotImportState.Empty) }
    val destination = remember(destinationName) {
        Destination.entries.firstOrNull { it.name == destinationName } ?: Destination.Home
    }
    val botPicker = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.OpenDocument(),
    ) { uri ->
        if (uri != null) {
            botImportState = BotImportState.Loading
            coroutineScope.launch {
                botImportState = withContext(Dispatchers.IO) {
                    runCatching {
                        context.contentResolver.openInputStream(uri)?.use(botReader::read)
                            ?: error("The selected document could not be opened.")
                    }.fold(
                        onSuccess = { BotImportState.Loaded(it) },
                        onFailure = {
                            BotImportState.Failed(
                                it.message ?: "The selected file is not a supported legacy GBot.",
                            )
                        },
                    )
                }
            }
        }
    }

    BoxWithConstraints(
        modifier = modifier
            .fillMaxSize()
            .background(ExodusColors.Canvas)
            .safeDrawingPadding(),
    ) {
        LandscapeShell(
            status = status,
            destination = destination,
            compact = maxWidth < 900.dp,
            botImportState = botImportState,
            onImportBot = { botPicker.launch(arrayOf("application/json", "text/plain", "*/*")) },
            onDestinationSelected = { destinationName = it.name },
        )
    }
}

@Composable
private fun LandscapeShell(
    status: TransportStatus,
    destination: Destination,
    compact: Boolean,
    botImportState: BotImportState,
    onImportBot: () -> Unit,
    onDestinationSelected: (Destination) -> Unit,
) {
    Column(modifier = Modifier.fillMaxSize()) {
        BrandBar(
            destination = destination,
            compact = compact,
            onDestinationSelected = onDestinationSelected,
        )
        Row(
            modifier = Modifier
                .weight(1f)
                .fillMaxWidth()
                .padding(horizontal = 10.dp, vertical = 8.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            if (!compact) {
                ControlRail(
                    status = status,
                    modifier = Modifier
                        .width(224.dp)
                        .fillMaxHeight(),
                )
            }
            Column(
                modifier = Modifier
                    .weight(1f)
                    .fillMaxHeight(),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                Row(
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    MainDestination(
                        destination = destination,
                        status = status,
                        botImportState = botImportState,
                        onImportBot = onImportBot,
                        modifier = Modifier
                            .weight(1f)
                            .fillMaxHeight(),
                    )
                    QuickActions(
                        modifier = Modifier
                            .width(54.dp)
                            .fillMaxHeight(),
                    )
                }
                if (!compact) {
                    ActivityDock(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(148.dp),
                    )
                }
            }
        }
        StatusStrip(status = status)
    }
}

@Composable
private fun CompactShell(
    status: TransportStatus,
    destination: Destination,
    onDestinationSelected: (Destination) -> Unit,
) {
    Column(modifier = Modifier.fillMaxSize()) {
        CompactBrandBar(status = status)
        LazyColumn(
            modifier = Modifier
                .weight(1f)
                .fillMaxWidth(),
            contentPadding = androidx.compose.foundation.layout.PaddingValues(12.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            if (destination == Destination.Home) {
                item {
                    GameSurface(
                        status = status,
                        modifier = Modifier
                            .fillMaxWidth()
                            .aspectRatio(16f / 9f),
                    )
                }
                item { CompactAccountCard(status = status) }
                item { CompactBotCard() }
                item { ActivityDock(modifier = Modifier.fillMaxWidth().height(172.dp)) }
            } else {
                item {
                    FeaturePlaceholder(
                        destination = destination,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(320.dp),
                    )
                }
            }
        }
        CompactNavigation(
            destination = destination,
            onDestinationSelected = onDestinationSelected,
        )
    }
}

@Composable
private fun BrandBar(
    destination: Destination,
    compact: Boolean,
    onDestinationSelected: (Destination) -> Unit,
) {
    Surface(color = ExodusColors.Surface, tonalElevation = 0.dp) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .height(62.dp)
                .padding(horizontal = 18.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            BrandMark()
            Spacer(modifier = Modifier.width(if (compact) 8.dp else 22.dp))
            Row(
                modifier = Modifier.weight(1f),
                horizontalArrangement = Arrangement.spacedBy(4.dp),
            ) {
                Destination.entries.forEach { item ->
                    TopNavigationItem(
                        item = item,
                        selected = item == destination,
                        showLabel = !compact,
                        onClick = { onDestinationSelected(item) },
                    )
                }
            }
            if (!compact) {
                Text(
                    text = "ANDROID 0.2 DEV",
                    color = ExodusColors.SecondaryText,
                    style = MaterialTheme.typography.labelSmall,
                )
            }
        }
    }
    HorizontalDivider(color = ExodusColors.Divider)
}

@Composable
private fun CompactBrandBar(status: TransportStatus) {
    Surface(color = ExodusColors.Surface) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .height(60.dp)
                .padding(horizontal = 14.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            BrandMark(modifier = Modifier.weight(1f))
            StatusBadge(status = status)
        }
    }
}

@Composable
private fun BrandMark(modifier: Modifier = Modifier) {
    Row(
        modifier = modifier,
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(10.dp),
    ) {
        Image(
            painter = painterResource(R.drawable.grimoire_emblem),
            contentDescription = "Grimoire Exodus emblem",
            modifier = Modifier.size(42.dp),
            contentScale = ContentScale.Fit,
        )
        Column {
            Text(
                text = "GRIMOIRE",
                color = ExodusColors.PrimaryText,
                style = MaterialTheme.typography.titleLarge,
            )
            Text(
                text = "EXODUS",
                color = ExodusColors.PaleViolet,
                style = MaterialTheme.typography.labelSmall,
            )
        }
    }
}

@Composable
private fun TopNavigationItem(
    item: Destination,
    selected: Boolean,
    showLabel: Boolean,
    onClick: () -> Unit,
) {
    val contentColor = if (selected) ExodusColors.PrimaryText else ExodusColors.SecondaryText
    Row(
        modifier = Modifier
            .clip(RoundedCornerShape(9.dp))
            .height(48.dp)
            .background(if (selected) Color(0xFF231A38) else Color.Transparent)
            .clickable(role = Role.Tab, onClick = onClick)
            .padding(horizontal = if (showLabel) 12.dp else 14.dp, vertical = 9.dp)
            .semantics { contentDescription = item.title },
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(7.dp),
    ) {
        Icon(item.icon, contentDescription = null, tint = contentColor, modifier = Modifier.size(17.dp))
        if (showLabel) {
            Text(text = item.title, color = contentColor, style = MaterialTheme.typography.labelLarge)
        }
    }
}

@Composable
private fun ControlRail(
    status: TransportStatus,
    modifier: Modifier = Modifier,
) {
    ArcanePanel(modifier = modifier) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            SectionLabel("PLAYER")
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                Icon(
                    imageVector = Icons.Outlined.AccountCircle,
                    contentDescription = null,
                    tint = ExodusColors.PaleViolet,
                    modifier = Modifier.size(38.dp),
                )
                Column(modifier = Modifier.weight(1f)) {
                    Text("No account selected", style = MaterialTheme.typography.titleMedium)
                    Text("Credentials are not loaded", color = ExodusColors.SecondaryText, style = MaterialTheme.typography.labelSmall)
                }
            }
            HorizontalDivider(color = ExodusColors.Divider)
            SectionLabel("ACTIVE BOT")
            Text("No bot loaded", color = ExodusColors.PrimaryText, style = MaterialTheme.typography.titleMedium)
            Text(
                "Open the Bot Manager to inspect a legacy GBot. Execution stays disabled while the client is offline.",
                color = ExodusColors.SecondaryText,
                style = MaterialTheme.typography.bodyMedium,
            )
            Spacer(modifier = Modifier.weight(1f))
            StatusBadge(status = status, modifier = Modifier.fillMaxWidth())
            Button(
                onClick = {},
                enabled = false,
                modifier = Modifier.fillMaxWidth().height(48.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = ExodusColors.Violet,
                    disabledContainerColor = ExodusColors.ControlSurface,
                    disabledContentColor = ExodusColors.SecondaryText,
                ),
            ) {
                Icon(Icons.Outlined.PlayArrow, contentDescription = null)
                Spacer(modifier = Modifier.width(6.dp))
                Text("Start bot")
            }
        }
    }
}

@Composable
private fun MainDestination(
    destination: Destination,
    status: TransportStatus,
    botImportState: BotImportState,
    onImportBot: () -> Unit,
    modifier: Modifier = Modifier,
) {
    when (destination) {
        Destination.Home -> GameSurface(status = status, modifier = modifier)
        Destination.Bots -> BotManagerSurface(
            importState = botImportState,
            onImportBot = onImportBot,
            modifier = modifier,
        )
        else -> FeaturePlaceholder(destination = destination, modifier = modifier)
    }
}

@Composable
private fun GameSurface(
    status: TransportStatus,
    modifier: Modifier = Modifier,
) {
    ArcanePanel(modifier = modifier, emphasized = true) {
        Box(
            modifier = Modifier
                .fillMaxSize(),
            contentAlignment = Alignment.Center,
        ) {
            Image(
                painter = painterResource(R.drawable.grimoire_banner),
                contentDescription = null,
                modifier = Modifier.fillMaxSize(),
                contentScale = ContentScale.Crop,
                alpha = 0.78f,
            )
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(
                        Brush.verticalGradient(
                            listOf(
                                ExodusColors.Canvas.copy(alpha = 0.12f),
                                ExodusColors.Canvas.copy(alpha = 0.64f),
                                ExodusColors.Canvas.copy(alpha = 0.96f),
                            ),
                        ),
                    ),
            )
            Column(
                modifier = Modifier.widthIn(max = 420.dp).padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                Text(
                    text = "AQW CLIENT LINK",
                    color = ExodusColors.PaleViolet,
                    style = MaterialTheme.typography.labelLarge,
                )
                Text(
                    text = "Bridge required",
                    color = ExodusColors.PrimaryText,
                    style = MaterialTheme.typography.displaySmall,
                    textAlign = TextAlign.Center,
                )
                Text(
                    text = "HARMAN AIR is the selected runtime path. Live play unlocks only after an authorized AIR bridge passes the v1 compatibility handshake.",
                    color = ExodusColors.SecondaryText,
                    style = MaterialTheme.typography.bodyLarge,
                    textAlign = TextAlign.Center,
                )
                StatusBadge(status = status)
            }
        }
    }
}

@Composable
private fun BotManagerSurface(
    importState: BotImportState,
    onImportBot: () -> Unit,
    modifier: Modifier = Modifier,
) {
    ArcanePanel(modifier = modifier, emphasized = true) {
        Column(modifier = Modifier.fillMaxSize()) {
            Row(
                modifier = Modifier.fillMaxWidth().padding(horizontal = 18.dp, vertical = 12.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Image(
                    painter = painterResource(R.drawable.grimoire_emblem),
                    contentDescription = null,
                    modifier = Modifier.size(44.dp),
                )
                Spacer(modifier = Modifier.width(10.dp))
                Column(modifier = Modifier.weight(1f)) {
                    SectionLabel("BOT MANAGER")
                    Text("Legacy GBot inspector", style = MaterialTheme.typography.titleLarge)
                }
                Button(
                    onClick = onImportBot,
                    colors = ButtonDefaults.buttonColors(containerColor = ExodusColors.Violet),
                ) {
                    Icon(Icons.Outlined.FolderOpen, contentDescription = null)
                    Spacer(modifier = Modifier.width(6.dp))
                    Text("Import GBot")
                }
            }
            HorizontalDivider(color = ExodusColors.Divider)
            when (importState) {
                BotImportState.Empty -> BotImportEmptyState(onImportBot)
                BotImportState.Loading -> BotImportMessage(
                    icon = Icons.AutoMirrored.Outlined.InsertDriveFile,
                    title = "Inspecting GBot",
                    message = "Reading the selected document as bounded inert data…",
                    color = ExodusColors.Info,
                )
                is BotImportState.Failed -> BotImportMessage(
                    icon = Icons.Outlined.ErrorOutline,
                    title = "Import rejected",
                    message = importState.message,
                    color = ExodusColors.Danger,
                )
                is BotImportState.Loaded -> BotDocumentDetails(importState.document)
            }
        }
    }
}

@Composable
private fun BotImportEmptyState(onImportBot: () -> Unit) {
    Column(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Icon(
            Icons.AutoMirrored.Outlined.InsertDriveFile,
            contentDescription = null,
            tint = ExodusColors.PaleViolet,
            modifier = Modifier.size(46.dp),
        )
        Spacer(modifier = Modifier.height(12.dp))
        Text("Inspect a desktop GBot", style = MaterialTheme.typography.headlineSmall)
        Spacer(modifier = Modifier.height(6.dp))
        Text(
            "Choose a .gbot through Android's document picker. The file is validated as data only; commands are never resolved or executed.",
            color = ExodusColors.SecondaryText,
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.bodyMedium,
            modifier = Modifier.widthIn(max = 520.dp),
        )
        Spacer(modifier = Modifier.height(16.dp))
        Button(onClick = onImportBot) { Text("Choose GBot") }
    }
}

@Composable
private fun BotImportMessage(
    icon: ImageVector,
    title: String,
    message: String,
    color: Color,
) {
    Column(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Icon(icon, contentDescription = null, tint = color, modifier = Modifier.size(44.dp))
        Spacer(modifier = Modifier.height(10.dp))
        Text(title, style = MaterialTheme.typography.headlineSmall)
        Spacer(modifier = Modifier.height(6.dp))
        Text(
            message,
            color = ExodusColors.SecondaryText,
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.bodyMedium,
            modifier = Modifier.widthIn(max = 560.dp),
        )
    }
}

@Composable
private fun BotDocumentDetails(document: LegacyGbotDocument) {
    Column(modifier = Modifier.fillMaxSize().padding(16.dp)) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Icon(Icons.Outlined.CheckCircle, contentDescription = null, tint = ExodusColors.Success)
            Column {
                Text("Compatible legacy document", style = MaterialTheme.typography.titleMedium)
                Text(
                    "Loaded for inspection only",
                    color = ExodusColors.SecondaryText,
                    style = MaterialTheme.typography.labelSmall,
                )
            }
        }
        Spacer(modifier = Modifier.height(12.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
            BotMetadata("AUTHOR", document.author ?: "Not specified", Modifier.weight(1f))
            BotMetadata("DELAY", document.botDelay?.let { "$it ms" } ?: "Default", Modifier.weight(1f))
            BotMetadata("COMMANDS", document.commands.size.toString(), Modifier.weight(1f))
        }
        Spacer(modifier = Modifier.height(12.dp))
        SectionLabel("COMMAND SEQUENCE")
        Spacer(modifier = Modifier.height(6.dp))
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(6.dp),
        ) {
            itemsIndexed(document.commands) { index, command ->
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(9.dp))
                        .background(ExodusColors.ControlSurface)
                        .padding(horizontal = 12.dp, vertical = 9.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(
                        text = (index + 1).toString().padStart(2, '0'),
                        color = ExodusColors.PaleViolet,
                        style = MaterialTheme.typography.labelSmall,
                        modifier = Modifier.width(34.dp),
                    )
                    Text(
                        text = command.typeName.substringAfterLast('.'),
                        style = MaterialTheme.typography.bodyMedium,
                        modifier = Modifier.weight(1f),
                    )
                    Text(
                        text = "Validated",
                        color = ExodusColors.SecondaryText,
                        style = MaterialTheme.typography.labelSmall,
                    )
                }
            }
        }
    }
}

@Composable
private fun BotMetadata(label: String, value: String, modifier: Modifier = Modifier) {
    Column(
        modifier = modifier
            .clip(RoundedCornerShape(9.dp))
            .background(ExodusColors.ControlSurface)
            .padding(10.dp),
    ) {
        SectionLabel(label)
        Text(value, maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.bodyMedium)
    }
}

@Composable
private fun QuickActions(modifier: Modifier = Modifier) {
    ArcanePanel(modifier = modifier) {
        Column(
            modifier = Modifier.fillMaxSize().padding(vertical = 8.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(6.dp),
        ) {
            QuickActionButton(Icons.Outlined.Visibility, "Toggle panels")
            QuickActionButton(Icons.Outlined.FolderOpen, "Open Bot Manager")
            Spacer(modifier = Modifier.weight(1f))
            QuickActionButton(Icons.Outlined.Settings, "Open Settings")
        }
    }
}

@Composable
private fun QuickActionButton(icon: ImageVector, description: String) {
    IconButton(
        onClick = {},
        enabled = false,
        modifier = Modifier.semantics { contentDescription = description },
    ) {
        Icon(icon, contentDescription = null, tint = ExodusColors.SecondaryText)
    }
}

@Composable
private fun ActivityDock(modifier: Modifier = Modifier) {
    ArcanePanel(modifier = modifier) {
        Column(modifier = Modifier.fillMaxSize()) {
            Row(
                modifier = Modifier.fillMaxWidth().padding(horizontal = 14.dp, vertical = 10.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text("ACTIVITY", color = ExodusColors.PaleViolet, style = MaterialTheme.typography.labelLarge)
                Spacer(modifier = Modifier.weight(1f))
                Text("bounded session log", color = ExodusColors.SecondaryText, style = MaterialTheme.typography.labelSmall)
            }
            HorizontalDivider(color = ExodusColors.Divider)
            Column(
                modifier = Modifier.fillMaxSize().padding(horizontal = 14.dp, vertical = 9.dp),
                verticalArrangement = Arrangement.spacedBy(5.dp),
            ) {
                LogLine("SYSTEM", "Android foundation initialized", ExodusColors.Info)
                LogLine("BRIDGE", "No approved game transport configured", ExodusColors.Warning)
                LogLine("BOT", "Execution controls remain locked", ExodusColors.SecondaryText)
            }
        }
    }
}

@Composable
private fun LogLine(source: String, message: String, color: Color) {
    Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
        Text(source, color = color, style = MaterialTheme.typography.labelSmall, modifier = Modifier.width(54.dp))
        Text(
            message,
            color = ExodusColors.SecondaryText,
            style = MaterialTheme.typography.bodyMedium,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
        )
    }
}

@Composable
private fun StatusStrip(status: TransportStatus) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .height(34.dp)
            .background(ExodusColors.Surface)
            .padding(horizontal = 14.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        StatusDot(status.connection)
        Text(status.connection.name.uppercase(), style = MaterialTheme.typography.labelSmall)
        Text(status.summary, color = ExodusColors.SecondaryText, style = MaterialTheme.typography.labelSmall)
        Spacer(modifier = Modifier.weight(1f))
        Text("CPU —", color = ExodusColors.SecondaryText, style = MaterialTheme.typography.labelSmall)
        Text("NET —", color = ExodusColors.SecondaryText, style = MaterialTheme.typography.labelSmall)
        Text("CLIENT —", color = ExodusColors.SecondaryText, style = MaterialTheme.typography.labelSmall)
    }
}

@Composable
private fun StatusBadge(
    status: TransportStatus,
    modifier: Modifier = Modifier,
) {
    Row(
        modifier = modifier
            .clip(RoundedCornerShape(999.dp))
            .background(ExodusColors.ControlSurface)
            .border(1.dp, ExodusColors.Divider, RoundedCornerShape(999.dp))
            .padding(horizontal = 10.dp, vertical = 7.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(7.dp),
    ) {
        StatusDot(status.connection)
        Text(
            text = status.connection.name,
            color = ExodusColors.PrimaryText,
            style = MaterialTheme.typography.labelSmall,
            maxLines = 1,
        )
    }
}

@Composable
private fun StatusDot(connection: ConnectionState) {
    val color = when (connection) {
        ConnectionState.Ready -> ExodusColors.Success
        ConnectionState.Connecting -> ExodusColors.Warning
        ConnectionState.Incompatible,
        ConnectionState.Failed,
        -> ExodusColors.Danger
        ConnectionState.Offline -> ExodusColors.SecondaryText
    }
    Box(
        modifier = Modifier
            .size(8.dp)
            .clip(CircleShape)
            .background(color)
            .semantics { contentDescription = "Connection ${connection.name}" },
    )
}

@Composable
private fun FeaturePlaceholder(
    destination: Destination,
    modifier: Modifier = Modifier,
) {
    ArcanePanel(modifier = modifier, emphasized = true) {
        Column(
            modifier = Modifier.fillMaxSize().padding(28.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
        ) {
            Icon(destination.icon, contentDescription = null, tint = ExodusColors.PaleViolet, modifier = Modifier.size(42.dp))
            Spacer(modifier = Modifier.height(12.dp))
            Text(destination.title, style = MaterialTheme.typography.headlineSmall)
            Spacer(modifier = Modifier.height(6.dp))
            Text(
                "The navigation contract is ready. This feature will be implemented in an isolated module after the offline foundation passes its compatibility gates.",
                color = ExodusColors.SecondaryText,
                textAlign = TextAlign.Center,
                style = MaterialTheme.typography.bodyMedium,
            )
        }
    }
}

@Composable
private fun CompactAccountCard(status: TransportStatus) {
    ArcanePanel(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(14.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Icon(Icons.Outlined.AccountCircle, contentDescription = null, tint = ExodusColors.PaleViolet, modifier = Modifier.size(38.dp))
            Column(modifier = Modifier.weight(1f)) {
                SectionLabel("PLAYER")
                Text("No account selected", style = MaterialTheme.typography.titleMedium)
            }
            StatusBadge(status = status)
        }
    }
}

@Composable
private fun CompactBotCard() {
    ArcanePanel(modifier = Modifier.fillMaxWidth()) {
        Row(
            modifier = Modifier.fillMaxWidth().padding(14.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            Icon(Icons.Outlined.FolderOpen, contentDescription = null, tint = ExodusColors.PaleViolet)
            Column(modifier = Modifier.weight(1f)) {
                SectionLabel("ACTIVE BOT")
                Text("No bot loaded", style = MaterialTheme.typography.titleMedium)
            }
            Text("Inspect only", color = ExodusColors.Warning, style = MaterialTheme.typography.labelSmall)
        }
    }
}

@Composable
private fun CompactNavigation(
    destination: Destination,
    onDestinationSelected: (Destination) -> Unit,
) {
    NavigationBar(containerColor = ExodusColors.Surface, tonalElevation = 0.dp) {
        Destination.entries.forEach { item ->
            NavigationBarItem(
                selected = item == destination,
                onClick = { onDestinationSelected(item) },
                icon = { Icon(item.icon, contentDescription = item.title) },
                label = { Text(item.title, maxLines = 1) },
                colors = NavigationBarItemDefaults.colors(
                    selectedIconColor = ExodusColors.PrimaryText,
                    selectedTextColor = ExodusColors.PrimaryText,
                    indicatorColor = Color(0xFF2A1E4A),
                    unselectedIconColor = ExodusColors.SecondaryText,
                    unselectedTextColor = ExodusColors.SecondaryText,
                ),
            )
        }
    }
}

@Composable
private fun SectionLabel(text: String) {
    Text(
        text = text,
        color = ExodusColors.PaleViolet,
        style = MaterialTheme.typography.labelSmall,
        fontWeight = FontWeight.Bold,
    )
}

@Preview(name = "Landscape command center", widthDp = 1280, heightDp = 800)
@Composable
private fun LandscapeCommandCenterPreview() {
    val transport = remember { OfflineGameTransport() }
    com.unknownsecurity.grimoireexodus.designsystem.GrimoireExodusTheme {
        GrimoireExodusApp(transport = transport)
    }
}

@Preview(name = "Landscape phone", widthDp = 800, heightDp = 360)
@Composable
private fun LandscapePhonePreview() {
    val transport = remember { OfflineGameTransport() }
    com.unknownsecurity.grimoireexodus.designsystem.GrimoireExodusTheme {
        GrimoireExodusApp(transport = transport)
    }
}
