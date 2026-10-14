using DarkUI.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Grimoire.UI
{
    /// <summary>
    /// Keeps VSTabControl's dark styling and tab selection while removing its
    /// built-in close affordance. Bot Manager pages are permanent application
    /// sections and must never be removable by a stray click.
    /// </summary>
    internal sealed class NonClosableVSTabControl : VSTabControl
    {
        public NonClosableVSTabControl()
        {
            DoubleBuffered = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            for (int index = 0; index < TabCount; index++)
            {
                Rectangle tabBounds = GetTabRect(index);
                Rectangle formerCloseButton = new Rectangle(
                    tabBounds.Right - 16,
                    tabBounds.Top + 3,
                    15,
                    15);
                if (!formerCloseButton.Contains(e.Location))
                    continue;

                SelectedIndex = index;
                Focus();
                return;
            }

            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Point mousePosition = PointToClient(Cursor.Position);
            bool formIsActive = FindForm() == Form.ActiveForm;
            for (int index = 0; index < TabCount; index++)
            {
                Rectangle tabBounds = GetTabRect(index);
                tabBounds.X += 2;
                if (tabBounds.Width <= 0 || tabBounds.Height <= 0)
                    continue;

                Color background = index == SelectedIndex
                    ? (formIsActive ? SelectedTabColor : UnselectedTabColor)
                    : tabBounds.Contains(mousePosition)
                        ? HoverTabColor
                        : BackgroundColor;

                using (SolidBrush brush = new SolidBrush(background))
                {
                    e.Graphics.FillRectangle(brush, tabBounds);
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    TabPages[index].Text,
                    Font,
                    tabBounds,
                    TextColor,
                    TextFormatFlags.EndEllipsis
                        | TextFormatFlags.HorizontalCenter
                        | TextFormatFlags.NoPadding
                        | TextFormatFlags.SingleLine
                        | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
