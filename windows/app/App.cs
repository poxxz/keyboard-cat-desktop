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
[assembly: AssemblyVersion("1.8.1.0")]
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
    private const double LongPressDragSeconds = 0.20;
    private const double HoldCancelDistance = 12.0;

    private readonly Preferences settings = Preferences.Load();
    private readonly CatScene cat = new CatScene();
    private readonly Canvas stage = new Canvas();
    private readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);
    private static readonly Stopwatch clock = Stopwatch.StartNew();
    // The scene's clock, shared with the gamepad poller so its event
    // timestamps line up with the keyboard / mouse ones.
    internal static double Now { get { return clock.Elapsed.TotalSeconds; } }
    private readonly Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
    private readonly Forms.NotifyIcon tray = new Forms.NotifyIcon();
    private readonly Drawing.Icon icon;
    private SettingsWindow settingsWindow;
    private UpdateManager.CheckResult pendingUpdate;
    private IntPtr hwnd, inputBuffer;
    private Drawing.Rectangle cursorMonitor;
    private bool listening, menuOpen, disposed;
    private bool holdingForDrag, dragging;
    private double pressStartAt, lastPetMouseOverAt = -10;
    private Point pressStartDip;
    private Point? dragStart;
    private Point dragOrigin;
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
        cat.PasswordModeEnabled = settings.PasswordMode;
        cat.IdleMinutes = settings.IdleMinutes;
        cat.Cat = settings.Cat;
        ApplyStoredCustomSkin();
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
            if (settingsWindow != null && settingsWindow.IsVisible) settingsWindow.SyncFromState();
        };
        DualSenseInput.Start(cat);
        PasswordMonitor.Start();
        UpdateManager.CleanupLeftovers();
        ApplySize();
        icon = Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        tray.Icon = icon;
        tray.Text = "键盘小黑猫 · 点击打开设置";
        tray.ContextMenuStrip = menu;
        tray.MouseClick += delegate(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Left) OpenSettings(null, false);
        };
        tray.DoubleClick += delegate { Reveal(); OpenSettings(null, false); };
        tray.BalloonTipClicked += delegate { Reveal(); OpenSettings(3, false); };
        menu.Font = new Drawing.Font("Microsoft YaHei UI", 9f);
        menu.Opening += BuildMenu;
        menu.Closed += delegate { menuOpen = false; };
        MouseLeftButtonDown += BeginDrag;
        MouseMove += ContinueDrag;
        MouseLeftButtonUp += delegate { EndDrag(true); };
        LostMouseCapture += delegate { if (holdingForDrag || dragging) EndDrag(false); };
        MouseLeave += delegate
        {
            if (!dragging && !holdingForDrag)
            {
                cat.SetDragVisual(false, 0, false);
                if (Cursor != Cursors.Arrow) Cursor = Cursors.Arrow;
            }
        };
        MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs e)
        {
            OpenSettings(null, true); e.Handled = true;
        };
        Cursor = Cursors.Arrow;
        timer.Interval = TimeSpan.FromMilliseconds(1000.0 / 40);
        timer.Tick += delegate { Tick(); };
        Closed += delegate { CleanUp(); };
    }

    private void ApplyStoredCustomSkin()
    {
        if (string.IsNullOrEmpty(settings.CustomSkinId))
        {
            cat.CustomSkin = null;
            return;
        }
        CatSkinData found = CustomSkinStore.FindById(settings.CustomSkinId);
        if (found != null)
        {
            cat.CustomSkin = new CompiledSkin(found);
        }
        else
        {
            settings.CustomSkinId = "";
            cat.CustomSkin = null;
        }
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
        if (settings.AutoCheckUpdate) StartBackgroundUpdateCheck();
    }

    private void StartBackgroundUpdateCheck()
    {
        ThreadPool.QueueUserWorkItem(delegate
        {
            Thread.Sleep(4500);
            if (disposed) return;
            UpdateManager.CheckResult res = UpdateManager.CheckLatest();
            if (disposed || res == null || !string.IsNullOrEmpty(res.Error) || !res.HasUpdate) return;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (disposed) return;
                pendingUpdate = res;
                if (settingsWindow != null) settingsWindow.NotifyUpdateAvailable(res);
                try
                {
                    tray.ShowBalloonTip(5500,
                        "发现新版本 v" + res.LatestVersion,
                        "已从 " + res.SourceName + " 检测到正式版更新，点击此通知打开设置一键升级",
                        Forms.ToolTipIcon.Info);
                }
                catch (Exception) { }
            }));
        });
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
        if (settings.ClickThrough)
        {
            if (holdingForDrag || dragging) EndDrag(false);
            cat.SetDragVisual(false, 0, false);
            Cursor = Cursors.Arrow;
        }
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
        bool leftDown = WindowsInput.GetAsyncKeyState(1) < 0;
        bool rightDown = WindowsInput.GetAsyncKeyState(2) < 0;

        double holdProgress = 0.0;
        if (holdingForDrag && !dragging)
        {
            if (!leftDown)
            {
                EndDrag(false);
            }
            else
            {
                double elapsed = now - pressStartAt;
                holdProgress = Math.Max(0.0, Math.Min(1.0, elapsed / LongPressDragSeconds));
                if (elapsed >= LongPressDragSeconds)
                {
                    holdingForDrag = false;
                    dragging = true;
                    dragStart = CursorInDips();
                    dragOrigin = new Point(Left, Top);
                }
            }
        }
        else if (dragging && !leftDown)
        {
            EndDrag(false);
        }

        if (IsMouseOver) lastPetMouseOverAt = now;
        bool petGrace = cat.CurrentPetZone != PetZone.None && (now - lastPetMouseOverAt) < 0.08;
        bool allowHand = !settings.ClickThrough && IsVisible && (IsMouseOver || dragging || holdingForDrag || petGrace);
        cat.SetDragVisual(allowHand, dragging ? 1.0 : holdProgress, dragging);

        if (WindowsInput.GetCursorPos(out screen))
        {
            Point p = cat.PointFromScreen(new Point(screen.X, screen.Y));
            if (!cursorMonitor.Contains(screen.X, screen.Y))
                cursorMonitor = Forms.Screen.FromPoint(new Drawing.Point(screen.X, screen.Y)).Bounds;
            Vector onMonitor = new Vector(
                2.0 * (screen.X - cursorMonitor.Left) / Math.Max(1, cursorMonitor.Width - 1) - 1,
                2.0 * (screen.Y - cursorMonitor.Top) / Math.Max(1, cursorMonitor.Height - 1) - 1);
            cat.Pointer(new Point(p.X / settings.Scale, p.Y / settings.Scale), leftDown, rightDown, now, onMonitor);
        }
        cat.SetPasswordFocused(PasswordMonitor.IsPasswordFocused);
        cat.Advance(now);

        Cursor desired = (!settings.ClickThrough && cat.IsPetHandVisible) ? Cursors.None : Cursors.Arrow;
        if (Cursor != desired) Cursor = desired;
    }

    private Point CursorInDips()
    {
        var p = Forms.Cursor.Position;
        return source.CompositionTarget.TransformFromDevice.Transform(new Point(p.X, p.Y));
    }
    private void BeginDrag(object sender, MouseButtonEventArgs e)
    {
        if (settings.ClickThrough) return;
        pressStartAt = Now;
        pressStartDip = CursorInDips();
        dragOrigin = new Point(Left, Top);
        dragStart = null;
        holdingForDrag = true;
        dragging = false;
        CaptureMouse();
        e.Handled = true;
    }
    private void ContinueDrag(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            if (holdingForDrag || dragging) EndDrag(false);
            return;
        }
        if (dragging && dragStart.HasValue)
        {
            Vector delta = CursorInDips() - dragStart.Value;
            double nextLeft = dragOrigin.X + delta.X;
            double dx = nextLeft - Left;
            Left = nextLeft;
            Top = dragOrigin.Y + delta.Y;
            if (Math.Abs(dx) > 0.05) cat.NotifyDragMove(dx);
            return;
        }
        if (holdingForDrag)
        {
            Vector drift = CursorInDips() - pressStartDip;
            if (drift.Length > HoldCancelDistance)
            {
                // Moving rapidly before 0.35s cancels long-press drag so rubbing works naturally.
                holdingForDrag = false;
                if (IsMouseCaptured) ReleaseMouseCapture();
            }
        }
    }
    private void EndDrag(bool fromMouseUp)
    {
        bool wasDragging = dragging;
        bool wasHolding = holdingForDrag;
        double heldFor = Now - pressStartAt;
        holdingForDrag = false;
        dragging = false;
        dragStart = null;
        if (IsMouseCaptured) ReleaseMouseCapture();
        cat.SetDragVisual(!settings.ClickThrough && IsMouseOver, 0, false);
        if (wasDragging)
        {
            if (!InView()) DockToCorner();
            Save();
        }
        else if (fromMouseUp && wasHolding && heldFor < LongPressDragSeconds && cat.CurrentPetZone != PetZone.None)
        {
            cat.QuickPat(Now);
        }
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
    private void OpenSettings(int? tabIndex, bool anchorToPet)
    {
        if (settingsWindow == null)
        {
            settingsWindow = new SettingsWindow(new SettingsWindow.Callbacks
            {
                GetCat = delegate { return settings.Cat; },
                SetCat = delegate(CatStyle next)
                {
                    settings.Cat = cat.Cat = next;
                    settings.CustomSkinId = "";
                    cat.CustomSkin = null;
                    cat.InvalidateVisual(); Save();
                },
                GetCustomSkinId = delegate { return settings.CustomSkinId; },
                ApplyCustomSkin = delegate(CatSkinData skin)
                {
                    if (skin == null)
                    {
                        settings.CustomSkinId = "";
                        cat.CustomSkin = null;
                    }
                    else
                    {
                        settings.CustomSkinId = skin.Id;
                        cat.CustomSkin = new CompiledSkin(skin);
                    }
                    cat.InvalidateVisual(); Save();
                },
                GetScale = delegate { return settings.Scale; },
                SetScale = delegate(double next)
                {
                    Point anchor = new Point(Left + Width, Top + Height);
                    settings.Scale = Math.Max(Preferences.MinPercent / 100.0, Math.Min(Preferences.MaxPercent / 100.0, next));
                    ApplySize();
                    Left = anchor.X - Width; Top = anchor.Y - Height;
                },
                CommitScale = delegate
                {
                    if (!InView()) DockToCorner();
                    Save();
                },
                GetKeyboard = delegate { return settings.Keyboard; },
                SetKeyboard = delegate(KeyboardStyle next)
                {
                    settings.Keyboard = cat.Keyboard = next;
                    cat.InvalidateVisual(); Save();
                },
                GetMouse = delegate { return settings.Mouse; },
                SetMouse = delegate(MouseStyle next)
                {
                    settings.Mouse = cat.Mouse = next;
                    cat.InvalidateVisual(); Save();
                },
                GetGamepadMode = delegate { return settings.Mode; },
                SetGamepadMode = delegate(GamepadMode next)
                {
                    settings.Mode = next;
                    cat.SetGamepadMode(next);
                    Save();
                },
                GetGamepad = delegate { return settings.Gamepad; },
                SetGamepad = delegate(GamepadStyle next)
                {
                    settings.Gamepad = cat.Gamepad = next;
                    cat.InvalidateVisual(); Save();
                },
                GetPasswordMode = delegate { return settings.PasswordMode; },
                SetPasswordMode = delegate(bool on)
                {
                    settings.PasswordMode = cat.PasswordModeEnabled = on;
                    cat.InvalidateVisual(); Save();
                },
                GetIdleMinutes = delegate { return settings.IdleMinutes; },
                SetIdleMinutes = delegate(int mins)
                {
                    settings.IdleMinutes = cat.IdleMinutes = mins;
                    Save();
                },
                GetLabels = delegate { return settings.Labels; },
                SetLabels = delegate(bool on)
                {
                    settings.Labels = cat.LabelsEnabled = on;
                    cat.ClearLabels(); Save();
                },
                GetAutoCheckUpdate = delegate { return settings.AutoCheckUpdate; },
                SetAutoCheckUpdate = delegate(bool on)
                {
                    settings.AutoCheckUpdate = on;
                    Save();
                },
                GetOnTop = delegate { return settings.OnTop; },
                SetOnTop = delegate(bool on)
                {
                    Topmost = settings.OnTop = on;
                    Save();
                },
                GetClickThrough = delegate { return settings.ClickThrough; },
                SetClickThrough = delegate(bool on)
                {
                    settings.ClickThrough = on;
                    ApplyStyle(); Save();
                },
                GetPetVisible = delegate { return IsVisible; },
                TogglePetVisible = delegate
                {
                    if (IsVisible) { timer.Stop(); Hide(); cat.ClearLabels(); } else Reveal();
                },
                DockToCorner = delegate { DockToCorner(); Reveal(); Save(); },
                ExitApp = delegate { Close(); }
            });
            if (pendingUpdate != null) settingsWindow.NotifyUpdateAvailable(pendingUpdate);
        }
        settingsWindow.SyncFromState();
        if (tabIndex.HasValue) settingsWindow.OpenTab(tabIndex.Value);
        if (anchorToPet || !settingsWindow.IsVisible)
        {
            double w = settingsWindow.Width;
            double h = settingsWindow.ActualHeight > 120 ? settingsWindow.ActualHeight : 576;
            if (anchorToPet)
            {
                Point petCenterDip = new Point(Left + Width * 0.5, Top + Height * 0.5);
                Point petCenterDev = source.CompositionTarget.TransformToDevice.Transform(petCenterDip);
                Rect area = LogicalArea(Forms.Screen.FromPoint(new Drawing.Point((int)petCenterDev.X, (int)petCenterDev.Y)).WorkingArea);
                double x;
                if (Left - w + 10 >= area.Left + 8) x = Left - w + 10;
                else if (Left + Width - 10 + w <= area.Right - 8) x = Left + Width - 10;
                else x = petCenterDip.X - w * 0.5;
                double y = petCenterDip.Y - h * 0.5;
                settingsWindow.Left = Math.Max(area.Left + 8, Math.Min(area.Right - w - 8, x));
                settingsWindow.Top = Math.Max(area.Top + 8, Math.Min(area.Bottom - h - 8, y));
            }
            else
            {
                Point curDip = CursorInDips();
                Rect area = LogicalArea(Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea);
                double x = curDip.X - w * 0.5;
                double y = curDip.Y - h - 14;
                if (y < area.Top + 8) y = curDip.Y + 14;
                settingsWindow.Left = Math.Max(area.Left + 8, Math.Min(area.Right - w - 8, x));
                settingsWindow.Top = Math.Max(area.Top + 8, Math.Min(area.Bottom - h - 8, y));
            }
            if (!settingsWindow.IsVisible) settingsWindow.Show();
        }
        settingsWindow.Activate();
    }
    private void BuildMenu(object sender, CancelEventArgs e)
    {
        menuOpen = true;
        while (menu.Items.Count > 0) menu.Items[0].Dispose();
        var openItem = Item("打开设置…", false, delegate { OpenSettings(null, false); });
        openItem.Font = new Drawing.Font(menu.Font, Drawing.FontStyle.Bold);
        menu.Items.Add(openItem);
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
        disposed = true;
        timer.Stop(); Save();
        if (settingsWindow != null)
        {
            SettingsWindow w = settingsWindow;
            settingsWindow = null;
            w.AllowClose = true;
            try { w.Close(); } catch (Exception) { }
        }
        PasswordMonitor.Stop();
        GamepadInput.Stop();
        DualSenseInput.Stop();
        if (listening) { listening = false; try { WindowsInput.Listen(hwnd, false); } catch (Win32Exception) { } }
        if (source != null) source.RemoveHook(Messages);
        if (inputBuffer != IntPtr.Zero) { Marshal.FreeHGlobal(inputBuffer); inputBuffer = IntPtr.Zero; }
        tray.Visible = false; tray.Dispose(); menu.Dispose(); if (icon != null) icon.Dispose();
    }
}
