using DarkUI.Forms;
using Grimoire.Clients;
using System;
using System.Drawing;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Grimoire.UI
{
    /// <summary>
    /// Provides consistent native title-bar theming for Grimoire windows.
    /// </summary>
    public class GrimoireForm : DarkForm
    {
        private const int DwmUseImmersiveDarkModeBefore20H1 = 19;
        private const int DwmUseImmersiveDarkMode = 20;
        private const int DwmCaptionColor = 35;
        private const int DwmTextColor = 36;
        private const int DwmColorDefault = -1;
        private const uint EventObjectCreate = 0x8000;
        private const uint EventObjectShow = 0x8002;
        private const int ObjectIdWindow = 0;
        private const int ChildIdSelf = 0;
        private const uint GetAncestorRoot = 2;
        private const uint WinEventOutOfContext = 0;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void WinEventDelegate(
            IntPtr hook,
            uint eventType,
            IntPtr windowHandle,
            int objectId,
            int childId,
            uint eventThread,
            uint eventTime);

        private static readonly WinEventDelegate TitleBarWindowEventHandler = HandleWindowEvent;
        private static SafeWinEventHookHandle titleBarWindowEventHook;
        private static uint currentProcessId;
        private ClientSession boundClientSession;
        private string unboundTitle;

        internal virtual bool SupportsClientSessionBinding => false;

        protected ClientSession BoundClientSession
        {
            get
            {
                return boundClientSession != null && !boundClientSession.IsDisposed
                    ? boundClientSession
                    : null;
            }
        }

        protected virtual bool CanChangeClientSessionBinding => true;

        internal void BindClientSession(ClientSession session)
        {
            if (!SupportsClientSessionBinding || !CanChangeClientSessionBinding)
                return;

            if (session != null && session.IsDisposed)
                session = null;
            if (ReferenceEquals(boundClientSession, session))
                return;

            ClientSession previous = boundClientSession;
            boundClientSession = session;
            if (unboundTitle == null)
                unboundTitle = Text;
            Text = session == null
                ? unboundTitle
                : unboundTitle + " — " + session.DisplayName;
            OnClientSessionBindingChanged(previous, session);
        }

        protected virtual void OnClientSessionBindingChanged(
            ClientSession previous,
            ClientSession current)
        {
        }

        protected ClientSession CaptureBoundClientSession()
        {
            return BoundClientSession ?? ClientSessionContext.Resolve();
        }

        protected IDisposable PushBoundClientSession()
        {
            ClientSession session = CaptureBoundClientSession();
            return session == null ? null : ClientSessionContext.Push(session);
        }

        [DllImport("dwmapi.dll", CallingConvention = CallingConvention.Winapi, PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr windowHandle,
            int attribute,
            ref int attributeValue,
            int attributeSize);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        private static extern SafeWinEventHookHandle SetWinEventHook(
            uint eventMinimum,
            uint eventMaximum,
            IntPtr eventHookModule,
            WinEventDelegate eventHookDelegate,
            uint processId,
            uint threadId,
            uint flags);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWinEvent(IntPtr eventHook);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern IntPtr GetAncestor(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern uint GetCurrentProcessId();

        private sealed class SafeWinEventHookHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeWinEventHookHandle()
                : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return UnhookWinEvent(handle);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyNativeTitleBarTheme(this);
        }

        internal static void InitializeNativeTitleBarTheming()
        {
            if ((titleBarWindowEventHook != null
                    && !titleBarWindowEventHook.IsInvalid
                    && !titleBarWindowEventHook.IsClosed)
                || Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return;
            }

            currentProcessId = GetCurrentProcessId();
            titleBarWindowEventHook = SetWinEventHook(
                EventObjectCreate,
                EventObjectShow,
                IntPtr.Zero,
                TitleBarWindowEventHandler,
                currentProcessId,
                0,
                WinEventOutOfContext);

            if (titleBarWindowEventHook != null && !titleBarWindowEventHook.IsInvalid)
            {
                Application.ApplicationExit += Application_ApplicationExit;
            }
            else
            {
                titleBarWindowEventHook?.Dispose();
                titleBarWindowEventHook = null;
            }
        }

        internal static void ApplyNativeTitleBarThemeToOpenForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                ApplyNativeTitleBarTheme(form);
            }
        }

        internal static void ApplyNativeTitleBarTheme(Form form)
        {
            if (form == null || !form.IsHandleCreated)
                return;

            ApplyNativeTitleBarTheme(form.Handle);
        }

        private static void HandleWindowEvent(
            IntPtr hook,
            uint eventType,
            IntPtr windowHandle,
            int objectId,
            int childId,
            uint eventThread,
            uint eventTime)
        {
            try
            {
                if ((eventType != EventObjectCreate && eventType != EventObjectShow)
                    || windowHandle == IntPtr.Zero
                    || objectId != ObjectIdWindow
                    || childId != ChildIdSelf
                    || GetAncestor(windowHandle, GetAncestorRoot) != windowHandle)
                {
                    return;
                }

                uint windowProcessId;
                GetWindowThreadProcessId(windowHandle, out windowProcessId);
                if (windowProcessId == currentProcessId)
                {
                    ApplyNativeTitleBarTheme(windowHandle);
                }
            }
            catch
            {
                // Exceptions must never cross the unmanaged WinEvent callback.
            }
        }

        private static void ApplyNativeTitleBarTheme(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero || Environment.OSVersion.Platform != PlatformID.Win32NT)
                return;

            try
            {
                int useDarkMode = Root.LightMode ? 0 : 1;
                int result = DwmSetWindowAttribute(
                    windowHandle,
                    DwmUseImmersiveDarkMode,
                    ref useDarkMode,
                    sizeof(int));
                if (result != 0)
                {
                    DwmSetWindowAttribute(
                        windowHandle,
                        DwmUseImmersiveDarkModeBefore20H1,
                        ref useDarkMode,
                        sizeof(int));
                }

                // Windows 11 supports explicit caption and text colors. Older
                // Windows versions safely ignore these unsupported attributes.
                int captionColor = Root.LightMode
                    ? DwmColorDefault
                    : ToNativeColor(Color.FromArgb(28, 32, 40));
                int textColor = Root.LightMode
                    ? DwmColorDefault
                    : ToNativeColor(Color.Gainsboro);
                DwmSetWindowAttribute(windowHandle, DwmCaptionColor, ref captionColor, sizeof(int));
                DwmSetWindowAttribute(windowHandle, DwmTextColor, ref textColor, sizeof(int));
            }
            catch (DllNotFoundException)
            {
                // Pre-DWM Windows keeps its system title-bar appearance.
            }
            catch (EntryPointNotFoundException)
            {
                // Older DWM versions do not expose this API.
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            Application.ApplicationExit -= Application_ApplicationExit;
            if (titleBarWindowEventHook != null)
            {
                titleBarWindowEventHook.Dispose();
                titleBarWindowEventHook = null;
            }
        }

        private static int ToNativeColor(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }
    }
}
