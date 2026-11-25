using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using Accessibility;

// Polls foreground focus on a background thread using 5 layers of password detection
// (Win32 ES_PASSWORD, EM_GETPASSWORDCHAR, MSAA STATE_SYSTEM_PROTECTED, UIA IsPassword
// on self/parent, and focused editable-control keyword heuristics) so both browsers and
// native .exe desktop clients are detected reliably without stalling the UI thread.
internal static class PasswordMonitor
{
    private const int GWL_STYLE = -16;
    private const int ES_PASSWORD = 0x0020;
    private const uint EM_GETPASSWORDCHAR = 0x00D2;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const uint OBJID_CLIENT = 0xFFFFFFFC;
    private const int CHILDID_SELF = 0;
    private const int STATE_SYSTEM_PROTECTED = 0x20000000;
    private static readonly Guid IID_IAccessible = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");

    private static readonly string[] PasswordHints = new string[]
    {
        "password", "passcode", "passphrase", "passwd", "pwd",
        "密码", "口令", "支付密码", "登录密码", "安全码", "pin码", "通行密钥"
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
        uint flags, uint timeout, out IntPtr result);

    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromWindow(
        IntPtr hwnd, uint id, ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out object ppvObject);

    private static readonly object Sync = new object();
    private static Thread worker;
    private static AutoResetEvent stopSignal;
    private static volatile bool running;
    private static volatile bool passwordFocused;

    public static bool IsPasswordFocused
    {
        get { return passwordFocused; }
    }

    public static void Start()
    {
        lock (Sync)
        {
            if (running) return;
            running = true;
            stopSignal = new AutoResetEvent(false);
            worker = new Thread(Run);
            worker.IsBackground = true;
            worker.Name = "KeycatPasswordMonitor";
            worker.SetApartmentState(ApartmentState.MTA);
            worker.Start();
        }
    }

    public static void Stop()
    {
        Thread toJoin;
        AutoResetEvent signal;
        lock (Sync)
        {
            if (!running) return;
            running = false;
            signal = stopSignal;
            toJoin = worker;
            stopSignal = null;
            worker = null;
        }
        if (signal != null)
        {
            try { signal.Set(); } catch (ObjectDisposedException) { }
        }
        if (toJoin != null)
        {
            try { toJoin.Join(300); } catch (Exception) { }
        }
        if (signal != null)
        {
            try { signal.Dispose(); } catch (ObjectDisposedException) { }
        }
        passwordFocused = false;
    }

    private static void Run()
    {
        while (running)
        {
            bool detected = false;
            try { detected = DetectFocusedPassword(); }
            catch (Exception) { detected = false; }
            passwordFocused = detected;

            AutoResetEvent signal = stopSignal;
            if (signal == null) break;
            try
            {
                if (signal.WaitOne(160)) break;
            }
            catch (ObjectDisposedException) { break; }
        }
    }

    private static bool DetectFocusedPassword()
    {
        GUITHREADINFO info = new GUITHREADINFO();
        info.cbSize = Marshal.SizeOf(typeof(GUITHREADINFO));
        IntPtr hwndFocus = IntPtr.Zero;
        if (GetGUIThreadInfo(0, ref info))
        {
            hwndFocus = info.hwndFocus != IntPtr.Zero ? info.hwndFocus : info.hwndActive;
        }

        // Layer 1 & 2: Native Win32 ES_PASSWORD style & EM_GETPASSWORDCHAR (covers Win32/WinForms/MFC/Delphi).
        if (hwndFocus != IntPtr.Zero)
        {
            int style = GetWindowLong(hwndFocus, GWL_STYLE);
            if ((style & ES_PASSWORD) != 0) return true;

            IntPtr pwChar;
            if (SendMessageTimeout(hwndFocus, EM_GETPASSWORDCHAR, IntPtr.Zero, IntPtr.Zero,
                                   SMTO_ABORTIFHUNG, 50, out pwChar) != IntPtr.Zero && pwChar != IntPtr.Zero)
            {
                return true;
            }

            // Layer 3: MSAA STATE_SYSTEM_PROTECTED (covers QQ, WeChat, Qt, DirectUI, custom .exe clients).
            if (CheckMsaaProtected(hwndFocus)) return true;
        }

        // Layer 4 & 5: UI Automation IsPassword (self & parent) + focused editable keyword heuristic.
        AutomationElement focused = AutomationElement.FocusedElement;
        if (focused == null) return false;
        if (IsUiaPassword(focused)) return true;

        try
        {
            AutomationElement parent = TreeWalker.ControlViewWalker.GetParent(focused);
            if (parent != null && IsUiaPasswordFlag(parent)) return true;
        }
        catch (Exception) { }

        return false;
    }

    private static bool CheckMsaaProtected(IntPtr hwnd)
    {
        try
        {
            Guid iid = IID_IAccessible;
            object raw;
            if (AccessibleObjectFromWindow(hwnd, OBJID_CLIENT, ref iid, out raw) != 0 || raw == null) return false;
            IAccessible acc = raw as IAccessible;
            if (acc == null) return false;

            if (HasProtectedState(acc, CHILDID_SELF)) return true;

            object focusObj = null;
            try { focusObj = acc.accFocus; } catch (Exception) { }
            if (focusObj == null) return false;

            IAccessible childAcc = focusObj as IAccessible;
            if (childAcc != null)
            {
                return HasProtectedState(childAcc, CHILDID_SELF);
            }
            if (focusObj is int)
            {
                return HasProtectedState(acc, (int)focusObj);
            }
        }
        catch (Exception) { }
        return false;
    }

    private static bool HasProtectedState(IAccessible acc, object childId)
    {
        try
        {
            object stateObj = acc.get_accState(childId);
            if (stateObj is int && (((int)stateObj) & STATE_SYSTEM_PROTECTED) != 0) return true;
        }
        catch (Exception) { }
        return false;
    }

    private static bool IsUiaPasswordFlag(AutomationElement element)
    {
        try
        {
            object value = element.GetCurrentPropertyValue(AutomationElement.IsPasswordProperty, true);
            return value is bool && (bool)value;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsUiaPassword(AutomationElement focused)
    {
        if (IsUiaPasswordFlag(focused)) return true;

        // Heuristic fallback for custom-drawn .exe password inputs (DirectUI/Qt/Electron/Steam/WeChat)
        // that omit IsPasswordProperty but expose an editable control named "Password" / "密码".
        try
        {
            ControlType ct = focused.GetCurrentPropertyValue(AutomationElement.ControlTypeProperty, true) as ControlType;
            bool isEdit = ct == ControlType.Edit;
            if (!isEdit && ct == ControlType.Custom)
            {
                object focusable = focused.GetCurrentPropertyValue(AutomationElement.IsKeyboardFocusableProperty, true);
                object hasValue = focused.GetCurrentPropertyValue(AutomationElement.IsValuePatternAvailableProperty, true);
                isEdit = (focusable is bool && (bool)focusable) && (hasValue is bool && (bool)hasValue);
            }
            if (!isEdit) return false;

            string name = focused.GetCurrentPropertyValue(AutomationElement.NameProperty, true) as string;
            string autoId = focused.GetCurrentPropertyValue(AutomationElement.AutomationIdProperty, true) as string;
            string help = focused.GetCurrentPropertyValue(AutomationElement.HelpTextProperty, true) as string;
            return MatchesPasswordHint(name) || MatchesPasswordHint(autoId) || MatchesPasswordHint(help);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool MatchesPasswordHint(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        string lower = text.ToLowerInvariant();
        for (int i = 0; i < PasswordHints.Length; ++i)
        {
            if (lower.IndexOf(PasswordHints[i], StringComparison.Ordinal) >= 0) return true;
        }
        return false;
    }
}
