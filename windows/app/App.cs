using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

[assembly: AssemblyTitle("键盘小黑猫")]
[assembly: AssemblyDescription("会跟着键盘和鼠标一起动的小黑猫桌宠")]
[assembly: AssemblyVersion("1.7.1.0")]
internal static class Program
{
    [STAThread]
    public static void Main()
    {
        bool first;
        using (var mutex = new Mutex(true, @"Local\KeycatDesktop.B48D8B", out first))
        using (var show = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\KeycatDesktop.B48D8B.Show"))
        {
            if (!first) { show.Set(); return; }
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            var pet = new PetWindow();
            RegisteredWaitHandle wake = ThreadPool.RegisterWaitForSingleObject(show,
                delegate { pet.Dispatcher.BeginInvoke(new Action(pet.Reveal)); }, null, Timeout.Infinite, false);
            try { app.Run(pet); }
            finally { wake.Unregister(null); mutex.ReleaseMutex(); }
        }
    }
}

internal sealed class PetWindow : Window
{
    private readonly Preferences settings = Preferences.Load();
    private readonly CatScene cat = new CatScene();
    private readonly Canvas stage = new Canvas();
    private readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);
    private static readonly Stopwatch clock = Stopwatch.StartNew();
    // The scene's clock, shared with the gamepad poller so its event
    // timestamps line up with the keyboard / mouse ones.
    internal static double Now { get { return clock.Elapsed.TotalSeconds; } }
    private readonly Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
    private readonly StyleMenu styleMenu = new StyleMenu();
    private readonly Forms.NotifyIcon tray = new Forms.NotifyIcon();
    private readonly Drawing.Icon icon;
    private IntPtr hwnd, inputBuffer;
    private Drawing.Rectangle cursorMonitor;
    private bool listening, menuOpen;
    private Point? dragStart;
    private Point dragOrigin;
    private bool dragging;
    private HwndSource source;

    public PetWindow()
    {
        Title = "键盘小黑猫";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = settings.OnTop;
        ShowActivated = false;
        ShowInTaskbar = false;
        Focusable = false;
        Content = stage;
        stage.ClipToBounds = true;
        stage.Children.Add(cat);
        cat.LabelsEnabled = settings.Labels;
        cat.Cat = settings.Cat;
        cat.Keyboard = settings.Keyboard;
        cat.Mouse = settings.Mouse;
        cat.Gamepad = settings.Gamepad;
        cat.SetGamepadMode(settings.Mode);
        GamepadInput.Start(cat);
        DualSenseInput.OnDualSenseConnected = delegate
        {
            settings.Gamepad = cat.Gamepad = GamepadStyle.PS5;
            cat.InvalidateVisual();
            Save();
        };
        DualSenseInput.Start(cat);
        ApplySize();
        icon = Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        tray.Icon = icon;
        tray.Text = "键盘小黑猫 · 右键设置";
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += delegate { Reveal(); };
        menu.Font = new Drawing.Font("Microsoft YaHei UI", 9f);
        menu.Opening += BuildMenu;
        menu.Closed += delegate { menuOpen = false; };
        MouseLeftButtonDown += BeginDrag;
        MouseMove += ContinueDrag;
        MouseLeftButtonUp += delegate { EndDrag(); };
        LostMouseCapture += delegate { if (dragStart.HasValue) EndDrag(); };
        MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e)
        {
            menu.Show(Forms.Cursor.Position); e.Handled = true;
        };
        Cursor = Cursors.Hand;
        timer.Interval = TimeSpan.FromMilliseconds(1000.0 / 40);
        timer.Tick += delegate { Tick(); };
        Closed += delegate { CleanUp(); };
    }

    protected override void OnSourceInitialized(EventArgs args)
    {
        base.OnSourceInitialized(args);
        hwnd = new WindowInteropHelper(this).Handle;
        source = HwndSource.FromHwnd(hwnd);
        source.AddHook(Messages);
        ApplyStyle();
        inputBuffer = Marshal.AllocHGlobal(128);
        tray.Visible = true;
        try { WindowsInput.Listen(hwnd, true); listening = true; }
        catch (Win32Exception e)
        {
            tray.ShowBalloonTip(4500, Title, "键鼠输入监听未能启动。请退出后重开。\n" + e.Message, Forms.ToolTipIcon.Warning);
        }
        if (Preferences.Finite(settings.X) && Preferences.Finite(settings.Y))
        {
            Left = settings.X; Top = settings.Y;
            if (!InView()) DockToCorner();
        }
        else DockToCorner();
        timer.Start();
    }

    private IntPtr Messages(IntPtr window, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0xFF && listening && IsVisible && !menuOpen && inputBuffer != IntPtr.Zero)
        {
            WindowsInput.Process(lParam, inputBuffer, cat, clock.Elapsed.TotalSeconds);
        }
        if (msg == 0x21) { handled = true; return new IntPtr(3); } // MA_NOACTIVATE
        if (msg == 0x7E) Dispatcher.BeginInvoke(new Action(delegate
        {
            cursorMonitor = Drawing.Rectangle.Empty;
            if (!InView()) DockToCorner();
        }));
        return IntPtr.Zero;
    }

    private void ApplyStyle()
    {
        int style = WindowsInput.GetWindowLong(hwnd, -20) | 0x08000000 | 0x80;
        style = settings.ClickThrough ? style | 0x20 : style & ~0x20;
        WindowsInput.SetWindowLong(hwnd, -20, style);
    }
    private void ApplySize()
    {
        double s = settings.Scale;
        Width = 306 * s; Height = (284 + DeskLayout.Forward) * s;
        cat.Width = 480 * s; cat.Height = 320 * s;
        Canvas.SetLeft(cat, -75 * s); Canvas.SetTop(cat, -9 * s);
    }
    private void Tick()
    {
        WindowsInput.ScreenPoint screen;
        double now = clock.Elapsed.TotalSeconds;
        if (WindowsInput.GetCursorPos(out screen))
        {
            Point p = cat.PointFromScreen(new Point(screen.X, screen.Y));
            if (!cursorMonitor.Contains(screen.X, screen.Y))
                cursorMonitor = Forms.Screen.FromPoint(new Drawing.Point(screen.X, screen.Y)).Bounds;
            Vector onMonitor = new Vector(
                2.0 * (screen.X - cursorMonitor.Left) / Math.Max(1, cursorMonitor.Width - 1) - 1,
                2.0 * (screen.Y - cursorMonitor.Top) / Math.Max(1, cursorMonitor.Height - 1) - 1);
            bool leftDown = WindowsInput.GetAsyncKeyState(1) < 0;
            bool rightDown = WindowsInput.GetAsyncKeyState(2) < 0;
            cat.Pointer(new Point(p.X / settings.Scale, p.Y / settings.Scale), leftDown, rightDown, now, onMonitor);
        }
        cat.Advance(now);
    }

    private Point CursorInDips()
    {
        var p = Forms.Cursor.Position;
        return source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y));
    }
    private void BeginDrag(object sender, MouseButtonEventArgs e)
    {
        if (settings.ClickThrough) return;
        dragStart = CursorInDips();
        dragOrigin = new Point(Left, Top);
        CaptureMouse(); e.Handled = true;
    }
    private void ContinueDrag(object sender, MouseEventArgs e)
    {
        if (!dragStart.HasValue || e.LeftButton != MouseButtonState.Pressed) return;
        Vector delta = CursorInDips() - dragStart.Value;
        if (!dragging && delta.Length < 4) return;
        dragging = true;
        Left = dragOrigin.X + delta.X; Top = dragOrigin.Y + delta.Y;
    }
    private void EndDrag()
    {
        dragStart = null;
        if (IsMouseCaptured) ReleaseMouseCapture();
        if (dragging) { dragging = false; if (!InView()) DockToCorner(); Save(); }
    }
    private Rect LogicalArea(Drawing.Rectangle r)
    {
        var transform = source.CompositionTarget.TransformFromDevice;
        return new Rect(transform.Transform(new Point(r.Left, r.Top)), transform.Transform(new Point(r.Right, r.Bottom)));
    }
    private bool InView()
    {
        if (!Preferences.Finite(Left) || !Preferences.Finite(Top)) return false;
        var center = new Point(Left + Width * .45, Top + Height * .48);
        foreach (Forms.Screen screen in Forms.Screen.AllScreens)
            if (LogicalArea(screen.WorkingArea).Contains(center)) return true;
        return false;
    }
    private void DockToCorner()
    {
        Rect area = LogicalArea(Forms.Screen.PrimaryScreen.WorkingArea);
        Left = area.Right - Width - 16; Top = area.Bottom - Height - 12;
    }
    public void Reveal()
    {
        if (!IsVisible) Show();
        if (!InView()) DockToCorner();
        timer.Start();
    }
    private void Save()
    {
        settings.X = Left; settings.Y = Top; settings.Save();
    }
    private static Forms.ToolStripMenuItem Item(string text, bool selected, Action action)
    {
        var item = new Forms.ToolStripMenuItem(text) { Checked = selected };
        item.Click += delegate { action(); };
        return item;
    }
    private Forms.ToolStripMenuItem SizeMenu()
    {
        var anchor = new Point(Left + Width, Top + Height);
        bool resized = false;
        var size = new Forms.ToolStripMenuItem("小猫大小");
        var slider = new ScaleTrackBar(settings.Scale);
        var valueLabel = new Forms.ToolStripLabel(slider.PercentageText);
        size.DropDownItems.Add(valueLabel);
        size.DropDownItems.Add(new Forms.ToolStripControlHost(slider));
        size.DropDownOpened += delegate
        {
            anchor = new Point(Left + Width, Top + Height);
            slider.Focus();
        };
        size.DropDown.Closing += delegate(object owner, Forms.ToolStripDropDownClosingEventArgs args)
        {
            if (args.CloseReason == Forms.ToolStripDropDownCloseReason.ItemClicked) args.Cancel = true;
        };
        slider.ValueChanged += delegate
        {
            valueLabel.Text = slider.PercentageText;
            settings.Scale = slider.Value / 1000.0;
            ApplySize();
            Left = anchor.X - Width; Top = anchor.Y - Height;
            resized = true;
        };
        size.DropDownClosed += delegate
        {
            if (!resized) return;
            if (!InView()) DockToCorner();
            Save(); resized = false;
        };
        return size;
    }
    private Forms.ToolStripMenuItem GamepadModeMenu()
    {
        var mode = new Forms.ToolStripMenuItem("手柄模式");
        foreach (var pair in new[]
        {
            new { Title = "自动", Value = GamepadMode.Auto },
            new { Title = "键盘", Value = GamepadMode.Keyboard },
            new { Title = "手柄", Value = GamepadMode.Gamepad },
        })
        {
            var item = new Forms.ToolStripMenuItem(pair.Title);
            item.Checked = settings.Mode == pair.Value;
            GamepadMode value = pair.Value;
            item.Click += delegate
            {
                settings.Mode = value;
                cat.SetGamepadMode(value);
                Save();
            };
            mode.DropDownItems.Add(item);
        }
        return mode;
    }
    private void BuildMenu(object sender, CancelEventArgs e)
    {
        menuOpen = true;
        while (menu.Items.Count > 0) menu.Items[0].Dispose();
        menu.Items.Add(styleMenu.Cats(settings.Cat, delegate(CatStyle next)
        {
            settings.Cat = cat.Cat = next;
            cat.InvalidateVisual(); Save();
        }));
        menu.Items.Add(SizeMenu());
        menu.Items.Add(styleMenu.Keyboards(settings.Keyboard, delegate(KeyboardStyle next)
        {
            settings.Keyboard = cat.Keyboard = next;
            cat.InvalidateVisual(); Save();
        }));
        menu.Items.Add(styleMenu.Mice(settings.Mouse, delegate(MouseStyle next)
        {
            settings.Mouse = cat.Mouse = next;
            cat.InvalidateVisual(); Save();
        }));
        menu.Items.Add(GamepadModeMenu());
        menu.Items.Add(styleMenu.Gamepads(settings.Gamepad, delegate(GamepadStyle next)
        {
            settings.Gamepad = cat.Gamepad = next;
            cat.InvalidateVisual(); Save();
        }));
        menu.Items.Add(Item("显示浮动键帽", settings.Labels, delegate
        {
            settings.Labels = cat.LabelsEnabled = !settings.Labels;
            cat.ClearLabels(); Save();
        }));
        menu.Items.Add(Item("保持置顶", settings.OnTop, delegate { Topmost = settings.OnTop = !settings.OnTop; Save(); }));
        menu.Items.Add(Item("鼠标穿透", settings.ClickThrough, delegate
        {
            settings.ClickThrough = !settings.ClickThrough; ApplyStyle(); Save();
        }));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Item("回到屏幕右下角", false, delegate { DockToCorner(); Reveal(); Save(); }));
        menu.Items.Add(Item(IsVisible ? "暂时隐藏" : "显示小猫", false, delegate
        {
            if (IsVisible) { timer.Stop(); Hide(); cat.ClearLabels(); } else Reveal();
        }));
        menu.Items.Add(Item("退出小黑猫", false, Close));
        e.Cancel = false;
    }
    private void CleanUp()
    {
        timer.Stop(); Save();
        GamepadInput.Stop();
        DualSenseInput.Stop();
        if (listening) { listening = false; try { WindowsInput.Listen(hwnd, false); } catch (Win32Exception) { } }
        if (source != null) source.RemoveHook(Messages);
        if (inputBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(inputBuffer); inputBuffer = IntPtr.Zero; }
        tray.Visible = false; tray.Dispose(); menu.Dispose(); styleMenu.Dispose(); if (icon != null) icon.Dispose();
    }
}
