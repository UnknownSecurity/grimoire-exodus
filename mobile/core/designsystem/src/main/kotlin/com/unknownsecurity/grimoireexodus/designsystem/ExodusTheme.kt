package com.unknownsecurity.grimoireexodus.designsystem

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.Typography
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

object ExodusColors {
    val Canvas = Color(0xFF05060B)
    val Surface = Color(0xFF0B0D15)
    val ElevatedSurface = Color(0xFF121522)
    val ControlSurface = Color(0xFF191C2C)
    val Violet = Color(0xFF8B5CF6)
    val BrightViolet = Color(0xFFA855F7)
    val PaleViolet = Color(0xFFC4B5FD)
    val PrimaryText = Color(0xFFF7F5FF)
    val SecondaryText = Color(0xFF9B96B3)
    val Success = Color(0xFF22C55E)
    val Warning = Color(0xFFF59E0B)
    val Danger = Color(0xFFEF4444)
    val Info = Color(0xFF22D3EE)
    val Divider = Color(0xFF29263B)
}

private val DarkColors = darkColorScheme(
    primary = ExodusColors.Violet,
    onPrimary = Color.White,
    primaryContainer = Color(0xFF2A1E4A),
    onPrimaryContainer = ExodusColors.PaleViolet,
    secondary = ExodusColors.PaleViolet,
    background = ExodusColors.Canvas,
    onBackground = ExodusColors.PrimaryText,
    surface = ExodusColors.Surface,
    onSurface = ExodusColors.PrimaryText,
    surfaceVariant = ExodusColors.ElevatedSurface,
    onSurfaceVariant = ExodusColors.SecondaryText,
    outline = ExodusColors.Divider,
    error = ExodusColors.Danger,
)

private val HighContrastColors = darkColorScheme(
    primary = Color(0xFF4C1D95),
    onPrimary = Color.White,
    background = Color.Black,
    onBackground = Color.White,
    surface = Color(0xFF090909),
    onSurface = Color.White,
    surfaceVariant = Color(0xFF161616),
    onSurfaceVariant = Color(0xFFE9E9E9),
    outline = Color.White,
    error = Color(0xFFFF6B6B),
)

private val ExodusTypography = Typography(
    displaySmall = TextStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 28.sp,
        letterSpacing = 1.2.sp,
    ),
    headlineSmall = TextStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 22.sp,
        letterSpacing = 0.8.sp,
    ),
    titleLarge = TextStyle(
        fontFamily = FontFamily.Serif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 18.sp,
        letterSpacing = 0.5.sp,
    ),
    titleMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 14.sp,
    ),
    bodyLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 15.sp,
    ),
    bodyMedium = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontSize = 13.sp,
    ),
    labelLarge = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.SemiBold,
        fontSize = 12.sp,
        letterSpacing = 0.4.sp,
    ),
    labelSmall = TextStyle(
        fontFamily = FontFamily.SansSerif,
        fontWeight = FontWeight.Medium,
        fontSize = 10.sp,
        letterSpacing = 0.5.sp,
    ),
)

@Composable
fun GrimoireExodusTheme(
    highContrast: Boolean = false,
    content: @Composable () -> Unit,
) {
    MaterialTheme(
        colorScheme = if (highContrast) HighContrastColors else DarkColors,
        typography = ExodusTypography,
        shapes = MaterialTheme.shapes.copy(
            small = RoundedCornerShape(8.dp),
            medium = RoundedCornerShape(12.dp),
            large = RoundedCornerShape(18.dp),
        ),
        content = content,
    )
}

@Composable
fun ArcanePanel(
    modifier: Modifier = Modifier,
    emphasized: Boolean = false,
    content: @Composable () -> Unit,
) {
    val borderColor = if (emphasized) ExodusColors.Violet else ExodusColors.Divider
    Box(
        modifier = modifier
            .background(
                brush = Brush.verticalGradient(
                    colors = listOf(
                        ExodusColors.ElevatedSurface,
                        ExodusColors.Surface,
                    ),
                ),
                shape = MaterialTheme.shapes.medium,
            )
            .border(BorderStroke(1.dp, borderColor), MaterialTheme.shapes.medium),
    ) {
        content()
    }
}
