using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

// Advanced PNG Texture Skin Workshop window matching the 2K cream UI palette.
internal sealed class SkinWorkshopWindow : Window
{
    internal sealed class Callbacks
    {
        public Func<List<CustomSkin>> GetSkins;
        public Func<string> GetActiveSkinId;
        public Func<CatStyle> GetBaseCatFallback;
        public Func<KeyboardStyle> GetKeyboard;
        public Func<MouseStyle> GetMouse;
        public Func<GamepadStyle> GetGamepad;
        public Action<CustomSkin> ApplySkin;
        public Action ClearCustomSkin;
        public Action<CustomSkin> OnSkinMutated;
        public Action OnSkinListChanged;
    }

    private enum PreviewAnimMode
    {
        Idle,
        Typing,
        Blink,
        Sweat,
        Anger,
        Sleep,
        HuntBug,
        Gamepad
    }

    private enum PreviewBgMode
    {
        Cream,
        Checker,
        Dark
    }

    // Live 40fps interactive preview canvas supporting cursor gaze, drag-to-nudge, wheel-to-scale, and all mood tests.
    private sealed class SkinPreviewStage : FrameworkElement
    {
        public CustomSkin Skin;
        public SkinSlot SelectedSlot = SkinSlot.Head;
        public bool ShowGuide = true;
        public PreviewAnimMode AnimMode = PreviewAnimMode.Idle;
        public PreviewBgMode BgMode = PreviewBgMode.Cream;
        public KeyboardStyle Keyboard = KeyboardStyle.Classic;
        public MouseStyle Mouse = MouseStyle.Classic;
        public GamepadStyle Gamepad = GamepadStyle.Xbox;
        public Action<double, double> OnNudgeLayer;
        public Action<double> OnScaleLayerDelta;

        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly GamepadState previewPad = new GamepadState();
        private readonly Dictionary<int, double> previewKeys = new Dictionary<int, double>();
        private double gazeX, gazeY, targetGazeX, targetGazeY;
        private double nextBlink = 2.4;
        private bool draggingLayer;
        private Point dragLast;
        private static readonly Brush CheckerBrush = BuildCheckerBrush();
        private static readonly Brush CreamBgBrush = MakeSolid(0xFAF6EC);
        private static readonly Brush DarkBgBrush = MakeSolid(0x2B2826);

        public SkinPreviewStage()
        {
            ClipToBounds = true;
            Cursor = Cursors.Hand;
        }

        private static Brush MakeSolid(uint rgb)
        {
            SolidColorBrush b = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
            b.Freeze();
            return b;
        }

        private static Brush BuildCheckerBrush()
        {
            DrawingGroup dg = new DrawingGroup();
            using (DrawingContext dc = dg.Open())
            {
                Brush a = MakeSolid(0xF6F2E8);
                Brush b = MakeSolid(0xE4DDD0);
                dc.DrawRectangle(a, null, new Rect(0, 0, 16, 16));
                dc.DrawRectangle(b, null, new Rect(0, 0, 8, 8));
                dc.DrawRectangle(b, null, new Rect(8, 8, 8, 8));
            }
            dg.Freeze();
            DrawingBrush db = new DrawingBrush(dg)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 16, 16),
                ViewportUnits = BrushMappingMode.Absolute
            };
            db.Freeze();
            return db;
        }

        public void Tick()
        {
            gazeX += (targetGazeX - gazeX) * 0.22;
            gazeY += (targetGazeY - gazeY) * 0.22;
            InvalidateVisual();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point p = e.GetPosition(this);
            if (draggingLayer && e.LeftButton == MouseButtonState.Pressed)
            {
                double scale = Math.Min(ActualWidth / 306.0, ActualHeight / 298.0);
                if (scale > 0.05 && OnNudgeLayer != null)
                {
                    double dx = (p.X - dragLast.X) / scale;
                    double dy = (p.Y - dragLast.Y) / scale;
                    dragLast = p;
                    OnNudgeLayer(dx, dy);
                }
                return;
            }
            if (ActualWidth > 1 && ActualHeight > 1)
            {
                targetGazeX = Math.Max(-1, Math.Min(1, (p.X / ActualWidth - 0.5) * 2.2));
                targetGazeY = Math.Max(-1, Math.Min(1, (p.Y / ActualHeight - 0.42) * 2.2));
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (!draggingLayer)
            {
                targetGazeX = 0;
                targetGazeY = 0;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (ShowGuide && Skin != null && Skin.HasImage(SelectedSlot))
            {
                draggingLayer = true;
                dragLast = e.GetPosition(this);
                CaptureMouse();
                e.Handled = true;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (draggingLayer)
            {
                draggingLayer = false;
                if (IsMouseCaptured) ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (ShowGuide && Skin != null && Skin.HasImage(SelectedSlot) && OnScaleLayerDelta != null)
            {
                OnScaleLayerDelta(e.Delta > 0 ? 0.05 : -0.05);
                e.Handled = true;
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth, h = ActualHeight;
            if (w <= 1 || h <= 1) return;

            Brush bg = BgMode == PreviewBgMode.Checker ? CheckerBrush : BgMode == PreviewBgMode.Dark ? DarkBgBrush : CreamBgBrush;
            dc.DrawRectangle(bg, null, new Rect(0, 0, w, h));

            double t = clock.Elapsed.TotalSeconds;
            if (t > nextBlink + 0.30) nextBlink = t + 2.6 + (Math.Sin(t * 1.7) * 0.5 + 0.5) * 2.5;
            double autoBlink = 0;
            double bAge = t - nextBlink;
            if (bAge >= 0 && bAge <= 0.23) autoBlink = Math.Min(1.0, Math.Sin(Math.PI * bAge / 0.23) * 1.4);

            CatMood mood = default(CatMood);
            double blink = autoBlink;
            Point leftPaw = new Point(192, 233);
            Point rightPaw = new Point(229, 233);
            double leftAct = 0, rightAct = 0;
            double gx = gazeX, gy = gazeY;
            bool showPad = AnimMode == PreviewAnimMode.Gamepad;
            double bugX = 148, bugY = 82, bugHeading = -1, swatFlash = 0;
            int swatSide = 0;

            previewKeys.Clear();
            if (AnimMode == PreviewAnimMode.Typing || AnimMode == PreviewAnimMode.Sweat || AnimMode == PreviewAnimMode.Anger)
            {
                double speed = AnimMode == PreviewAnimMode.Typing ? 5.2 : 9.5;
                double ph = t * speed;
                int step = (int)Math.Floor(ph);
                double frac = ph - step;
                double strike = frac < 0.35 ? 1.0 : Math.Max(0, 1.0 - (frac - 0.35) / 0.45);
                int[] leftVks = { 65, 83, 68, 70, 81, 87, 69, 82 };
                int[] rightVks = { 74, 75, 76, 85, 73, 79, 80, 78 };
                bool leftTurn = (step % 2) == 0;
                int lvk = leftVks[(step / 2) % leftVks.Length];
                int rvk = rightVks[(step / 2) % rightVks.Length];
                Point lKey = DeskArt.KeyPoint(lvk);
                Point rKey = DeskArt.KeyPoint(rvk);
                leftPaw = Mix(new Point(188, 189), lKey, leftTurn ? strike : 0.15);
                rightPaw = Mix(new Point(233, 193), rKey, !leftTurn ? strike : 0.15);
                leftAct = leftTurn ? strike : 0.4;
                rightAct = !leftTurn ? strike : 0.4;
                if (leftTurn) previewKeys[lvk] = strike;
                else previewKeys[rvk] = strike;
                if (AnimMode == PreviewAnimMode.Sweat) mood.Sweat = 1.0;
                if (AnimMode == PreviewAnimMode.Anger) mood.Anger = 1.0;
            }
            else if (AnimMode == PreviewAnimMode.Blink)
            {
                blink = 1.0;
            }
            else if (AnimMode == PreviewAnimMode.Sleep)
            {
                mood.Sleep = 1.0;
                blink = 1.0;
                leftPaw = new Point(186, 230);
                rightPaw = new Point(240, 230);
                gx = 0; gy = 0;
            }
            else if (AnimMode == PreviewAnimMode.HuntBug)
            {
                double phase = t % 2.4;
                if (phase < 1.2)
                {
                    double p = phase / 1.2;
                    double k = Math.Sin(p * Math.PI);
                    leftPaw = Mix(new Point(186, 212), new Point(126, 78), k);
                    rightPaw = new Point(240, 216);
                    bugX = 128 + (1 - k) * 35;
                    bugY = 76 + (1 - k) * 14;
                    bugHeading = -1;
                    swatFlash = k;
                    swatSide = k > 0.3 ? -1 : 0;
                    leftAct = Math.Max(0.35, k);
                    mood.Crouch = 0.75 - 0.35 * k;
                }
                else
                {
                    double p = (phase - 1.2) / 1.2;
                    double k = Math.Sin(p * Math.PI);
                    rightPaw = Mix(new Point(240, 212), new Point(304, 78), k);
                    leftPaw = new Point(186, 216);
                    bugX = 302 - (1 - k) * 35;
                    bugY = 76 + (1 - k) * 14;
                    bugHeading = 1;
                    swatFlash = k;
                    swatSide = k > 0.3 ? 1 : 0;
                    rightAct = Math.Max(0.35, k);
                    mood.Crouch = 0.75 - 0.35 * k;
                }
                gx = Math.Max(-1, Math.Min(1, (bugX - 214.0) / 95.0));
                gy = Math.Max(-1, Math.Min(1, (bugY - 105.0) / 75.0));
            }
            else if (AnimMode == PreviewAnimMode.Gamepad)
            {
                previewPad.Clear();
                previewPad.LeftStick = new Vector(Math.Sin(t * 2.6) * 0.75, Math.Cos(t * 2.1) * 0.55);
                previewPad.RightStick = new Vector(Math.Cos(t * 3.0) * 0.65, Math.Sin(t * 2.4) * 0.55);
                bool pressA = Math.Sin(t * 5.0) > 0.3;
                if (pressA) previewPad.Set(GamepadButton.A, 1.0);
                leftPaw = new Point(190.68, 175.49) + GamepadArt.StickCapOffset(Gamepad, false, previewPad.RightStick) * 1.2;
                rightPaw = new Point(237.32, 175.49) + GamepadArt.StickCapOffset(Gamepad, true, previewPad.LeftStick) * 1.2;
                leftAct = 0.55;
                rightAct = 0.55;
            }

            // Map the 306 x 298 visible scene window (origin 75, 9 inside 480x320) cleanly to the preview box.
            double scale = Math.Min(w / 306.0, h / 298.0);
            double ox = (w - 306.0 * scale) * 0.5 - 75.0 * scale;
            double oy = (h - 298.0 * scale) * 0.5 - 9.0 * scale;

            dc.PushTransform(new TranslateTransform(ox, oy));
            dc.PushTransform(new ScaleTransform(scale, scale));

            if (showPad)
            {
                CustomSkinRenderer.Draw(dc, Skin, t, gx, gy, blink, leftPaw, rightPaw, leftAct, rightAct, 0.25,
                    delegate(DrawingContext g) { GamepadArt.Draw(g, Gamepad, previewPad); }, mood);
            }
            else
            {
                DeskArt.DrawDesk(dc, previewKeys.Count > 0 ? previewKeys : null, 0, 0, Keyboard, Mouse, new Vector(0, 0));
                CustomSkinRenderer.Draw(dc, Skin, t, gx, gy, blink, leftPaw, rightPaw, leftAct, rightAct, -1, null, mood);
            }

            if (AnimMode == PreviewAnimMode.Sleep)
            {
                CatArt.DrawZzz(dc, t, 258, 144, 1.0);
            }
            if (AnimMode == PreviewAnimMode.HuntBug)
            {
                if (swatSide < 0) CatArt.DrawSwatTrail(dc, leftPaw.X, leftPaw.Y, -1, swatFlash);
                else if (swatSide > 0) CatArt.DrawSwatTrail(dc, rightPaw.X, rightPaw.Y, 1, swatFlash);
                CatArt.DrawBug(dc, bugX, bugY, t, bugHeading, swatFlash);
            }

            if (ShowGuide && Skin != null)
            {
                CustomSkinRenderer.DrawSlotGuideOverlay(dc, Skin, SelectedSlot, t, gx, gy, leftPaw, rightPaw, mood);
            }

            dc.Pop();
            dc.Pop();
        }

        private static Point Mix(Point a, Point b, double k) { return a + (b - a) * k; }
    }

    private sealed class PillItem
    {
        public Border Root;
        public TextBlock Label;
        public object Value;
    }

    private sealed class SlotButton
    {
        public Border Root;
        public Ellipse StatusDot;
        public TextBlock TagText;
        public SkinSlot Slot;
    }

    private sealed class MiniToggle
    {
        public Border Track;
        public Border Knob;
        private bool isOn;
        public Action<bool> Changed;

        public MiniToggle(Action<bool> onChanged)
        {
            Changed = onChanged;
            Track = new Border
            {
                Width = 38,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            Knob = new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(7),
                Background = BrushOf(0xFFFDF8),
                BorderBrush = BrushOf(0xD3C6AE),
                BorderThickness = new Thickness(0.8),
                Margin = new Thickness(3, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Track.Child = Knob;
            Track.MouseLeftButtonUp += delegate
            {
                SetValue(!isOn);
                if (Changed != null) Changed(isOn);
            };
            SetValue(false);
        }

        public void SetValue(bool value)
        {
            isOn = value;
            if (isOn)
            {
                Track.Background = Gradient(0xFFCA52, 0xF2A018, 0, 0, 0, 1);
                Track.BorderBrush = BrushOf(0xDF9112);
                Knob.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                Track.Background = BrushOf(0xDFD6C3);
                Track.BorderBrush = BrushOf(0xCFC5B0);
                Knob.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }
    }

    private static readonly FontFamily UiFont = new FontFamily("Microsoft YaHei UI, Segoe UI");
    private static readonly Brush CreamBg = BrushOf(0xFAF6EC);
    private static readonly Brush SidebarBg = BrushOf(0xF3ECE0);
    private static readonly Brush CardBg = BrushOf(0xFFFDF7);
    private static readonly Brush CardBorder = BrushOf(0xE7DFCE);
    private static readonly Brush ItemBg = BrushOf(0xFAF6EC);
    private static readonly Brush ItemBorder = BrushOf(0xEAE1CE);
    private static readonly Brush ActiveItemBg = BrushOf(0xFFF7DF);
    private static readonly Brush ActiveBorder = BrushOf(0xEB9F1D);
    private static readonly Brush TextPrimary = BrushOf(0x423529);
    private static readonly Brush TextSecondary = BrushOf(0x857564);
    private static readonly Brush TextAmber = BrushOf(0x6C440C);
    private static readonly Brush LoadedDotBrush = BrushOf(0x4CA965);
    private static readonly Brush MirroredDotBrush = BrushOf(0xE6A324);
    private static readonly Brush EmptyDotBrush = BrushOf(0xD2C7B4);

    private readonly Callbacks cb;
    private readonly DispatcherTimer previewTimer = new DispatcherTimer(DispatcherPriority.Render);
    private readonly DispatcherTimer saveDebounce = new DispatcherTimer();

    private CustomSkin currentSkin;
    private SkinSlot selectedSlot = SkinSlot.Head;
    private bool syncing;
    public bool AllowClose;

    // Left panel
    private StackPanel skinListPanel;
    // Center panel
    private SkinPreviewStage previewStage;
    private Border dropOverlay;
    private TextBlock dropOverlayText;
    private readonly List<PillItem> bgPills = new List<PillItem>();
    private readonly List<PillItem> animPills = new List<PillItem>();
    private Border guideToggleBtn;
    private TextBlock guideToggleText;
    private TextBox nameTextBox;
    private ComboBox baseCatCombo;
    private Border applyBtn;
    private TextBlock applyBtnText;
    private TextBlock statusText;

    // Right panel
    private readonly List<PillItem> inspectorTabPills = new List<PillItem>();
    private UIElement layersTabPanel, animStyleTabPanel;
    private readonly List<SlotButton> slotButtons = new List<SlotButton>();
    private TextBlock slotHintText;
    private Border removeSlotBtn;
    private UIElement starterAccessoryRow;
    private UIElement extractFullKitRow;

    // Layer transform sliders
    private Slider offXSlider, offYSlider, scaleSlider, rotSlider, opacitySlider;
    private TextBlock offXVal, offYVal, scaleVal, rotVal, opacityVal;
    private Border flipXBtn;
    private TextBlock flipXText;

    // Animation & style tab controls
    private readonly List<PillItem> eyeModePills = new List<PillItem>();
    private UIElement dynamicEyesBox;
    private Slider eyeOffYSlider, eyeSpacingSlider, eyeScaleSlider;
    private TextBlock eyeOffYVal, eyeSpacingVal, eyeScaleVal;
    private TextBox eyeIrisHexBox;
    private Border eyeIrisSwatch;

    private readonly List<PillItem> armModePills = new List<PillItem>();
    private MiniToggle mirrorPawToggle, customArmColorToggle, pixelArtToggle, moodFxToggle, whiskersToggle;
    private TextBox armColorHexBox;
    private Border armColorSwatch;
    private UIElement customArmColorRow;

    private static readonly CatStyle[] AllBaseCats = new CatStyle[]
    {
        CatStyle.Black, CatStyle.Ginger, CatStyle.JapaneseBobtail, CatStyle.PhotoTabby,
        CatStyle.Wednesday, CatStyle.Coffee, CatStyle.SilverShaded, CatStyle.Siamese
    };
    private static readonly string[] AllBaseCatNames = new string[]
    {
        "经典黑猫", "奶油橘猫", "短尾三花", "长毛虎斑", "星期三", "咖啡", "银渐层", "暹罗猫"
    };

    public SkinWorkshopWindow(Callbacks callbacks)
    {
        cb = callbacks;
        Title = "键盘小黑猫 · 皮肤工坊 (PNG 进阶版)";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = true;
        Topmost = true;
        Width = 896;
        Height = 648;
        FontFamily = UiFont;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);

        previewTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / 36.0);
        previewTimer.Tick += delegate { if (previewStage != null) previewStage.Tick(); };

        saveDebounce.Interval = TimeSpan.FromMilliseconds(180);
        saveDebounce.Tick += delegate
        {
            saveDebounce.Stop();
            CommitCurrentSkin(false);
        };

        IsVisibleChanged += delegate
        {
            if (IsVisible) previewTimer.Start();
            else previewTimer.Stop();
        };
        PreviewKeyDown += delegate(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { Hide(); e.Handled = true; }
        };
        Closing += delegate(object s, System.ComponentModel.CancelEventArgs e)
        {
            saveDebounce.Stop();
            if (currentSkin != null) CommitCurrentSkin(false);
            if (!AllowClose)
            {
                e.Cancel = true;
                Hide();
            }
        };

        Content = BuildRoot();
        SelectInspectorTab(0);
    }

    public void SyncPeripherals(KeyboardStyle keyboard, MouseStyle mouse, GamepadStyle gamepad)
    {
        if (previewStage == null) return;
        previewStage.Keyboard = keyboard;
        previewStage.Mouse = mouse;
        previewStage.Gamepad = gamepad;
        previewStage.InvalidateVisual();
    }

    private void RebindCurrentSkinFromList(string targetId)
    {
        if (string.IsNullOrEmpty(targetId)) return;
        List<CustomSkin> skins = cb.GetSkins != null ? cb.GetSkins() : null;
        if (skins == null) return;
        for (int i = 0; i < skins.Count; ++i)
        {
            if (string.Equals(skins[i].Id, targetId, StringComparison.OrdinalIgnoreCase))
            {
                currentSkin = skins[i];
                if (previewStage != null) previewStage.Skin = currentSkin;
                return;
            }
        }
    }

    public void OpenAndRefresh(string preferredSkinId)
    {
        List<CustomSkin> skins = cb.GetSkins != null ? cb.GetSkins() : SkinManager.LoadAll();
        if (skins.Count == 0)
        {
            CatStyle fallbackCat = cb.GetBaseCatFallback != null ? cb.GetBaseCatFallback() : CatStyle.Black;
            CustomSkin starter = SkinManager.CreateNew("我的贴图猫", fallbackCat);
            if (cb.OnSkinListChanged != null) cb.OnSkinListChanged();
            skins = cb.GetSkins != null ? cb.GetSkins() : SkinManager.LoadAll();
            preferredSkinId = starter.Id;
            SetStatus("已自动创建新皮肤，可直接拖拽 PNG 或点击右侧「导入 PNG…」开始制作");
        }

        CustomSkin chosen = null;
        string targetId = !string.IsNullOrEmpty(preferredSkinId)
            ? preferredSkinId
            : (cb.GetActiveSkinId != null ? cb.GetActiveSkinId() : "");
        if (!string.IsNullOrEmpty(targetId))
        {
            for (int i = 0; i < skins.Count; ++i)
            {
                if (string.Equals(skins[i].Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    chosen = skins[i];
                    break;
                }
            }
        }
        if (chosen == null && currentSkin != null)
        {
            for (int i = 0; i < skins.Count; ++i)
            {
                if (string.Equals(skins[i].Id, currentSkin.Id, StringComparison.OrdinalIgnoreCase))
                {
                    chosen = skins[i];
                    break;
                }
            }
        }
        if (chosen == null && skins.Count > 0) chosen = skins[0];

        if (cb.GetKeyboard != null) previewStage.Keyboard = cb.GetKeyboard();
        if (cb.GetMouse != null) previewStage.Mouse = cb.GetMouse();
        if (cb.GetGamepad != null) previewStage.Gamepad = cb.GetGamepad();

        currentSkin = chosen;
        RebuildSkinList();
        SyncControlsFromSkin();
    }

    private static SolidColorBrush BrushOf(uint rgb)
    {
        SolidColorBrush b = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        b.Freeze();
        return b;
    }

    private static LinearGradientBrush Gradient(uint top, uint bot, double x1, double y1, double x2, double y2)
    {
        LinearGradientBrush b = new LinearGradientBrush(
            Color.FromRgb((byte)(top >> 16), (byte)(top >> 8), (byte)top),
            Color.FromRgb((byte)(bot >> 16), (byte)(bot >> 8), (byte)bot),
            new Point(x1, y1), new Point(x2, y2));
        b.Freeze();
        return b;
    }

    private UIElement BuildRoot()
    {
        Grid root = new Grid();
        Border shadowLayer = new Border
        {
            Margin = new Thickness(14),
            Background = CreamBg,
            CornerRadius = new CornerRadius(16),
            IsHitTestVisible = false,
            Effect = new DropShadowEffect
            {
                BlurRadius = 22,
                ShadowDepth = 5,
                Direction = 270,
                Opacity = 0.24,
                Color = Color.FromRgb(66, 53, 41)
            }
        };
        root.Children.Add(shadowLayer);

        Border frame = new Border
        {
            Margin = new Thickness(14),
            Background = CreamBg,
            BorderBrush = BrushOf(0xDED5C3),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(16),
            AllowDrop = true
        };
        frame.PreviewDragOver += HandleDragOver;
        frame.PreviewDragLeave += HandleDragLeave;
        frame.PreviewDrop += HandleDrop;

        DockPanel shell = new DockPanel { LastChildFill = true };
        shell.Children.Add(BuildTitleBar());

        Grid body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(192) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(324) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        UIElement leftCol = BuildSkinLibraryColumn();
        UIElement midCol = BuildPreviewColumn();
        UIElement rightCol = BuildInspectorColumn();

        Grid.SetColumn(leftCol, 0);
        Grid.SetColumn(midCol, 1);
        Grid.SetColumn(rightCol, 2);
        body.Children.Add(leftCol);
        body.Children.Add(midCol);
        body.Children.Add(rightCol);

        shell.Children.Add(body);
        frame.Child = shell;
        root.Children.Add(frame);
        return root;
    }

    private UIElement BuildTitleBar()
    {
        Border bar = new Border
        {
            Height = 46,
            Padding = new Thickness(16, 0, 12, 0),
            CornerRadius = new CornerRadius(15, 15, 0, 0),
            Background = Gradient(0xFFFDF6, 0xF4ECDC, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xE6DEC9),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };
        DockPanel.SetDock(bar, Dock.Top);

        DockPanel row = new DockPanel { LastChildFill = false };
        StackPanel left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Border iconBox = new Border
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(8),
            Background = BrushOf(0xFFF3CE),
            BorderBrush = BrushOf(0xECD59E),
            BorderThickness = new Thickness(1),
            Child = RenderCatIcon(CatStyle.Ginger, 19, 19)
        };
        left.Children.Add(iconBox);
        left.Children.Add(new TextBlock
        {
            Text = "皮肤工坊",
            FontSize = 14.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary,
            Margin = new Thickness(8, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        left.Children.Add(new Border
        {
            Background = BrushOf(0xFFE9AE),
            BorderBrush = BrushOf(0xE6B95A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "PNG 贴图进阶版",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = BrushOf(0x70450A)
            }
        });
        DockPanel.SetDock(left, Dock.Left);
        row.Children.Add(left);

        StackPanel right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Border importPackBtn = MakeHeaderActionBtn("导入皮肤包 (.kcat)", HandleImportSkinPack);
        Border exportTplBtn = MakeHeaderActionBtn("导出 PNG 模板", HandleExportTemplatePack);
        importPackBtn.Margin = new Thickness(0, 0, 6, 0);
        exportTplBtn.Margin = new Thickness(0, 0, 10, 0);
        right.Children.Add(importPackBtn);
        right.Children.Add(exportTplBtn);

        Border closeBtn = new Border
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(8),
            Background = BrushOf(0xEFE8D8),
            BorderBrush = BrushOf(0xE1D8C4),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "关闭皮肤工坊",
            Child = new TextBlock
            {
                Text = "✕",
                FontSize = 11.5,
                Foreground = BrushOf(0x7A6A58),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        closeBtn.MouseEnter += delegate { closeBtn.Background = BrushOf(0xE6DCC7); };
        closeBtn.MouseLeave += delegate { closeBtn.Background = BrushOf(0xEFE8D8); };
        closeBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; };
        closeBtn.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; Hide(); };
        right.Children.Add(closeBtn);

        DockPanel.SetDock(right, Dock.Right);
        row.Children.Add(right);

        bar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
        {
            if (closeBtn.IsMouseOver || importPackBtn.IsMouseOver || exportTplBtn.IsMouseOver) return;
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch (InvalidOperationException) { }
            }
        };

        bar.Child = row;
        return bar;
    }

    private Border MakeHeaderActionBtn(string text, Action click)
    {
        Border b = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 4, 10, 4),
            Background = BrushOf(0xFFFDF6),
            BorderBrush = BrushOf(0xDFD5BF),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x5A4837),
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        b.MouseEnter += delegate { b.Background = BrushOf(0xFFF3D4); b.BorderBrush = BrushOf(0xE6B95A); };
        b.MouseLeave += delegate { b.Background = BrushOf(0xFFFDF6); b.BorderBrush = BrushOf(0xDFD5BF); };
        b.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; };
        b.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e) { e.Handled = true; click(); };
        return b;
    }

    private UIElement BuildSkinLibraryColumn()
    {
        Border side = new Border
        {
            Background = SidebarBg,
            BorderBrush = BrushOf(0xE6DEC9),
            BorderThickness = new Thickness(0, 0, 1, 0),
            CornerRadius = new CornerRadius(0, 0, 0, 15),
            Padding = new Thickness(10, 12, 10, 12)
        };
        DockPanel layout = new DockPanel { LastChildFill = true };

        // Top: New Skin button
        Border newBtn = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 10),
            Background = Gradient(0xFFCF58, 0xF0A018, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xDC8C10),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = "＋ 新建贴图皮肤",
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = BrushOf(0x472905),
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        newBtn.MouseEnter += delegate { newBtn.Background = Gradient(0xFFD972, 0xF5AB28, 0, 0, 0, 1); };
        newBtn.MouseLeave += delegate { newBtn.Background = Gradient(0xFFCF58, 0xF0A018, 0, 0, 0, 1); };
        newBtn.MouseLeftButtonUp += delegate { HandleCreateNewSkin(); };
        DockPanel.SetDock(newBtn, Dock.Top);
        layout.Children.Add(newBtn);

        // Bottom: 2x2 Skin Management Actions
        Border foot = new Border
        {
            BorderBrush = BrushOf(0xE2D9C6),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 8, 0, 0),
            Margin = new Thickness(0, 8, 0, 0)
        };
        UniformGrid actGrid = new UniformGrid { Columns = 2 };
        actGrid.Children.Add(MakeCompactBtn("复制皮肤", false, HandleDuplicateSkin));
        actGrid.Children.Add(MakeCompactBtn("导出 .kcat", false, HandleExportSkinPack));
        actGrid.Children.Add(MakeCompactBtn("打开目录", false, HandleOpenSkinFolder));
        actGrid.Children.Add(MakeCompactBtn("删除皮肤", true, HandleDeleteSkin));
        foot.Child = actGrid;
        DockPanel.SetDock(foot, Dock.Bottom);
        layout.Children.Add(foot);

        // Middle: Scrollable list of custom skins
        skinListPanel = new StackPanel();
        ScrollViewer scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = skinListPanel
        };
        layout.Children.Add(scroller);

        side.Child = layout;
        return side;
    }

    private UIElement BuildPreviewColumn()
    {
        DockPanel col = new DockPanel
        {
            Margin = new Thickness(12, 12, 8, 12),
            LastChildFill = false
        };

        // 1. Live Preview Stage Card
        StackPanel stageSec = new StackPanel();

        DockPanel stageHead = new DockPanel { Margin = new Thickness(0, 0, 0, 7) };
        TextBlock title = new TextBlock
        {
            Text = "实时交互预览",
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(title, Dock.Left);
        stageHead.Children.Add(title);

        StackPanel tools = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        AddBgPill(tools, "奶油", PreviewBgMode.Cream);
        AddBgPill(tools, "棋盘", PreviewBgMode.Checker);
        AddBgPill(tools, "深色", PreviewBgMode.Dark);

        guideToggleText = new TextBlock { Text = "辅助框", FontSize = 10.5, FontWeight = FontWeights.SemiBold };
        guideToggleBtn = new Border
        {
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(4, 0, 0, 0),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "显示当前选中图层的定位框，开启后可在预览区直接拖拽移动、滚轮缩放",
            Child = guideToggleText
        };
        guideToggleBtn.MouseLeftButtonUp += delegate
        {
            previewStage.ShowGuide = !previewStage.ShowGuide;
            RefreshGuideToggleVisual();
        };
        tools.Children.Add(guideToggleBtn);
        DockPanel.SetDock(tools, Dock.Right);
        stageHead.Children.Add(tools);
        stageSec.Children.Add(stageHead);

        Grid stageBox = new Grid { Height = 252 };
        previewStage = new SkinPreviewStage();
        previewStage.OnNudgeLayer = delegate(double dx, double dy)
        {
            if (currentSkin == null) return;
            SkinLayerConfig layer = currentSkin.GetLayer(selectedSlot);
            layer.OffsetX = CustomSkin.ClampDouble(layer.OffsetX + dx, -120, 120);
            layer.OffsetY = CustomSkin.ClampDouble(layer.OffsetY + dy, -120, 120);
            SyncSelectedLayerSliders();
            ScheduleSaveAndLiveSync();
        };
        previewStage.OnScaleLayerDelta = delegate(double delta)
        {
            if (currentSkin == null) return;
            SkinLayerConfig layer = currentSkin.GetLayer(selectedSlot);
            layer.Scale = CustomSkin.ClampDouble(layer.Scale + delta, 0.25, 2.5);
            SyncSelectedLayerSliders();
            ScheduleSaveAndLiveSync();
        };

        Border stageFrame = new Border
        {
            CornerRadius = new CornerRadius(11),
            BorderBrush = BrushOf(0xE2D8C4),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            Child = previewStage
        };
        stageBox.Children.Add(stageFrame);

        dropOverlayText = new TextBlock
        {
            Text = "松开鼠标导入 PNG 到当前图层",
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = BrushOf(0x6D4106),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        dropOverlay = new Border
        {
            CornerRadius = new CornerRadius(11),
            Background = new SolidColorBrush(Color.FromArgb(215, 255, 245, 212)),
            BorderBrush = ActiveBorder,
            BorderThickness = new Thickness(2),
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            Child = dropOverlayText
        };
        stageBox.Children.Add(dropOverlay);
        stageSec.Children.Add(stageBox);

        stageSec.Children.Add(new TextBlock
        {
            Text = "提示：直接拖入 PNG 文件可快速导入 · 预览区可拖拽定位/滚轮缩放",
            FontSize = 10.5,
            Foreground = TextSecondary,
            Margin = new Thickness(2, 5, 2, 6),
            TextAlignment = TextAlignment.Center
        });

        // Animation test mode pills (4x2)
        UniformGrid animGrid = new UniformGrid { Columns = 4 };
        PreviewAnimMode[] modes = new PreviewAnimMode[]
        {
            PreviewAnimMode.Idle, PreviewAnimMode.Typing, PreviewAnimMode.Blink, PreviewAnimMode.Sweat,
            PreviewAnimMode.Anger, PreviewAnimMode.Sleep, PreviewAnimMode.HuntBug, PreviewAnimMode.Gamepad
        };
        string[] modeNames = new string[] { "常规", "打字", "眨眼", "流汗", "愤怒", "趴睡", "抓虫", "手柄" };
        for (int i = 0; i < modes.Length; ++i)
        {
            PreviewAnimMode m = modes[i];
            PillItem pill = MakeSmallPill(modeNames[i], m, delegate
            {
                previewStage.AnimMode = m;
                UpdatePillGroup(animPills, m);
            });
            animPills.Add(pill);
            animGrid.Children.Add(pill.Root);
        }
        stageSec.Children.Add(animGrid);

        Border stageCard = MakeSectionCard(stageSec, new Thickness(10, 9, 10, 9));
        DockPanel.SetDock(stageCard, Dock.Top);
        col.Children.Add(stageCard);

        // 2. Skin Meta Card (Name + Base Cat)
        StackPanel metaSec = new StackPanel();
        Grid metaGrid = new Grid();
        metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
        metaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        metaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        metaGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(7) });
        metaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        TextBlock nameLbl = new TextBlock { Text = "皮肤名称", FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = TextPrimary, VerticalAlignment = VerticalAlignment.Center };
        nameTextBox = new TextBox
        {
            FontSize = 12,
            Padding = new Thickness(7, 4, 7, 4),
            Background = ItemBg,
            BorderBrush = ItemBorder,
            BorderThickness = new Thickness(1),
            Foreground = TextPrimary,
            MaxLength = 24
        };
        nameTextBox.TextChanged += delegate
        {
            if (syncing || currentSkin == null) return;
            string txt = (nameTextBox.Text ?? "").Trim();
            currentSkin.Name = string.IsNullOrEmpty(txt) ? "未命名皮肤" : txt;
            ScheduleSaveAndLiveSync();
            RebuildSkinList();
        };
        Grid.SetRow(nameLbl, 0); Grid.SetColumn(nameLbl, 0);
        Grid.SetRow(nameTextBox, 0); Grid.SetColumn(nameTextBox, 1);
        metaGrid.Children.Add(nameLbl);
        metaGrid.Children.Add(nameTextBox);

        TextBlock baseLbl = new TextBlock { Text = "底模猫咪", FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = TextPrimary, VerticalAlignment = VerticalAlignment.Center };
        baseCatCombo = new ComboBox
        {
            FontSize = 11.5,
            Padding = new Thickness(6, 3, 6, 3),
            Cursor = Cursors.Hand
        };
        for (int i = 0; i < AllBaseCatNames.Length; ++i) baseCatCombo.Items.Add(AllBaseCatNames[i]);
        baseCatCombo.SelectionChanged += delegate
        {
            if (syncing || currentSkin == null) return;
            int idx = baseCatCombo.SelectedIndex;
            if (idx >= 0 && idx < AllBaseCats.Length)
            {
                currentSkin.BaseCat = AllBaseCats[idx];
                ScheduleSaveAndLiveSync();
                RebuildSkinList();
            }
        };
        Grid.SetRow(baseLbl, 2); Grid.SetColumn(baseLbl, 0);
        Grid.SetRow(baseCatCombo, 2); Grid.SetColumn(baseCatCombo, 1);
        metaGrid.Children.Add(baseLbl);
        metaGrid.Children.Add(baseCatCombo);
        metaSec.Children.Add(metaGrid);

        Border metaCard = MakeSectionCard(metaSec, new Thickness(10, 9, 10, 9));
        DockPanel.SetDock(metaCard, Dock.Top);
        col.Children.Add(metaCard);

        // 3. Apply CTA + Status line anchored at bottom
        StackPanel bottomSec = new StackPanel();
        Grid ctaGrid = new Grid();
        ctaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ctaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        ctaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        applyBtnText = new TextBlock
        {
            Text = "应用到桌面小猫",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = BrushOf(0x472905),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        applyBtn = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 9, 12, 9),
            Background = Gradient(0xFFCF58, 0xF0A018, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xDC8C10),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = applyBtnText
        };
        applyBtn.MouseLeftButtonUp += delegate { HandleApplyCurrentSkin(); };
        Grid.SetColumn(applyBtn, 0);
        ctaGrid.Children.Add(applyBtn);

        Border backBuiltinBtn = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 9, 10, 9),
            Background = BrushOf(0xFFFDF6),
            BorderBrush = BrushOf(0xDFD5BF),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "恢复使用内置矢量猫咪",
            Child = new TextBlock
            {
                Text = "切回内置猫",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x6D5B49)
            }
        };
        backBuiltinBtn.MouseEnter += delegate { backBuiltinBtn.Background = BrushOf(0xFFF3D4); };
        backBuiltinBtn.MouseLeave += delegate { backBuiltinBtn.Background = BrushOf(0xFFFDF6); };
        backBuiltinBtn.MouseLeftButtonUp += delegate
        {
            if (cb.ClearCustomSkin != null) cb.ClearCustomSkin();
            RefreshApplyButtonVisual();
            RebuildSkinList();
            SetStatus("已切换回内置猫咪样式");
        };
        Grid.SetColumn(backBuiltinBtn, 2);
        ctaGrid.Children.Add(backBuiltinBtn);

        bottomSec.Children.Add(ctaGrid);

        statusText = new TextBlock
        {
            Text = "未导入 PNG 的部位会自动沿用所选底模猫咪",
            FontSize = 10.5,
            Foreground = TextSecondary,
            Margin = new Thickness(2, 6, 2, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        bottomSec.Children.Add(statusText);

        DockPanel.SetDock(bottomSec, Dock.Bottom);
        col.Children.Add(bottomSec);

        UpdatePillGroup(bgPills, PreviewBgMode.Cream);
        UpdatePillGroup(animPills, PreviewAnimMode.Idle);
        RefreshGuideToggleVisual();
        return col;
    }

    private UIElement BuildInspectorColumn()
    {
        DockPanel col = new DockPanel
        {
            Margin = new Thickness(4, 12, 14, 12),
            LastChildFill = true
        };

        // Top 2-tab switcher
        UniformGrid tabGrid = new UniformGrid { Columns = 2 };
        string[] titles = { "贴图图层编辑", "眼睛 · 手臂 · 风格" };
        for (int i = 0; i < titles.Length; ++i)
        {
            int idx = i;
            PillItem pill = MakeSegmentPill(titles[i], idx, delegate { SelectInspectorTab(idx); });
            inspectorTabPills.Add(pill);
            tabGrid.Children.Add(pill.Root);
        }
        Border tabHost = new Border
        {
            Background = SidebarBg,
            BorderBrush = BrushOf(0xE5DCC9),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(2),
            Margin = new Thickness(0, 0, 0, 10),
            Child = tabGrid
        };
        DockPanel.SetDock(tabHost, Dock.Top);
        col.Children.Add(tabHost);

        Grid tabBody = new Grid();
        layersTabPanel = BuildLayersTab();
        animStyleTabPanel = BuildAnimStyleTab();
        tabBody.Children.Add(layersTabPanel);
        tabBody.Children.Add(animStyleTabPanel);
        col.Children.Add(tabBody);
        return col;
    }

    private UIElement BuildLayersTab()
    {
        ScrollViewer sv = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        StackPanel stack = new StackPanel();

        // 1. 8 PNG Slots 2x4 grid
        StackPanel slotSec = new StackPanel();
        slotSec.Children.Add(MakeSectionHeader("贴图槽位（支持局部替换）", "绿点 = 已导入 PNG"));
        UniformGrid slotGrid = new UniformGrid { Columns = 2, Margin = new Thickness(-2, 0, -2, 6) };
        for (int i = 0; i < CustomSkin.AllSlots.Length; ++i)
        {
            SkinSlot slot = CustomSkin.AllSlots[i];
            SlotButton sb = MakeSlotButton(slot);
            slotButtons.Add(sb);
            slotGrid.Children.Add(sb.Root);
        }
        slotSec.Children.Add(slotGrid);

        slotHintText = new TextBlock
        {
            Text = CustomSkin.SlotHint(selectedSlot),
            FontSize = 10.5,
            Foreground = TextSecondary,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(2, 0, 2, 7)
        };
        slotSec.Children.Add(slotHintText);

        // Slot action bar
        Grid actRow = new Grid();
        actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
        actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
        actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
        actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
        actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
        actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
        actRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });

        Border importBtn = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 8, 6),
            Background = Gradient(0xFFE092, 0xF5B538, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xDF981B),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = "导入 PNG…",
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                Foreground = BrushOf(0x4C2D05),
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        importBtn.MouseLeftButtonUp += delegate { HandleImportSlotImage(); };

        Border extractSlotBtn = MakeMiniActionBtn("从底模提取", false, HandleExtractSingleSlot);
        Border reloadBtn = MakeMiniActionBtn("重载", false, HandleReloadImages);
        removeSlotBtn = MakeMiniActionBtn("移除", true, HandleRemoveSlotImage);

        Grid.SetColumn(importBtn, 0);
        Grid.SetColumn(extractSlotBtn, 2);
        Grid.SetColumn(reloadBtn, 4);
        Grid.SetColumn(removeSlotBtn, 6);
        actRow.Children.Add(importBtn);
        actRow.Children.Add(extractSlotBtn);
        actRow.Children.Add(reloadBtn);
        actRow.Children.Add(removeSlotBtn);
        slotSec.Children.Add(actRow);

        // Quick Starter Accessories Strip (visible when Accessory slot selected)
        StackPanel accRow = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        accRow.Children.Add(new TextBlock
        {
            Text = "一键生成示例透明 PNG 头饰（即时体验贴图叠加与定位）：",
            FontSize = 10.5,
            Foreground = BrushOf(0x7C5418),
            Margin = new Thickness(1, 0, 0, 4)
        });
        UniformGrid accGrid = new UniformGrid { Columns = 5 };
        string[] keys = { "bow", "crown", "sunglasses", "headphones", "wizard" };
        string[] labels = { "蝴蝶结", "皇冠", "墨镜", "耳机", "巫师帽" };
        for (int i = 0; i < keys.Length; ++i)
        {
            string k = keys[i];
            string lbl = labels[i];
            Border b = MakeMiniActionBtn(lbl, false, delegate
            {
                if (currentSkin == null) return;
                string err = SkinManager.GenerateStarterAccessoryPng(currentSkin, k);
                if (err != null) { SetStatus(err); return; }
                selectedSlot = SkinSlot.Accessory;
                previewStage.SelectedSlot = selectedSlot;
                SyncControlsFromSkin();
                CommitCurrentSkin(true);
                SetStatus("已生成示例透明 PNG：" + lbl + "（可拖拽调整位置/滚轮缩放）");
            });
            b.Margin = new Thickness(1.5, 0, 1.5, 0);
            accGrid.Children.Add(b);
        }
        accRow.Children.Add(accGrid);
        starterAccessoryRow = accRow;
        slotSec.Children.Add(accRow);

        // Quick extract all base-cat parts row (visible when non-accessory slot selected)
        DockPanel extractAllRow = new DockPanel { Margin = new Thickness(0, 7, 0, 0) };
        Border extractAllBtn = MakeMiniActionBtn("一键提取底模全套 6 张 PNG 到皮肤目录（方便 PS/画图二创）", false, HandleExtractAllSlots);
        extractAllRow.Children.Add(extractAllBtn);
        extractFullKitRow = extractAllRow;
        slotSec.Children.Add(extractAllRow);

        stack.Children.Add(MakeSectionCard(slotSec, new Thickness(11, 9, 11, 9)));

        // 2. Selected Layer Transform Inspector
        StackPanel tfSec = new StackPanel();
        tfSec.Children.Add(MakeSectionHeader("当前图层变换微调", "支持在预览区拖拽 / 滚轮缩放"));

        tfSec.Children.Add(MakeSliderRow("水平偏移 X", -120, 120, 0, out offXSlider, out offXVal, delegate(double v)
        {
            if (syncing || currentSkin == null) return;
            currentSkin.GetLayer(selectedSlot).OffsetX = v;
            offXVal.Text = ((int)Math.Round(v)) + " px";
            ScheduleSaveAndLiveSync();
        }));
        tfSec.Children.Add(MakeSliderRow("垂直偏移 Y", -120, 120, 0, out offYSlider, out offYVal, delegate(double v)
        {
            if (syncing || currentSkin == null) return;
            currentSkin.GetLayer(selectedSlot).OffsetY = v;
            offYVal.Text = ((int)Math.Round(v)) + " px";
            ScheduleSaveAndLiveSync();
        }));
        tfSec.Children.Add(MakeSliderRow("缩放比例", 25, 250, 100, out scaleSlider, out scaleVal, delegate(double v)
        {
            if (syncing || currentSkin == null) return;
            currentSkin.GetLayer(selectedSlot).Scale = v / 100.0;
            scaleVal.Text = ((int)Math.Round(v)) + "%";
            ScheduleSaveAndLiveSync();
        }));
        tfSec.Children.Add(MakeSliderRow("旋转角度", -180, 180, 0, out rotSlider, out rotVal, delegate(double v)
        {
            if (syncing || currentSkin == null) return;
            currentSkin.GetLayer(selectedSlot).Rotation = v;
            rotVal.Text = ((int)Math.Round(v)) + "°";
            ScheduleSaveAndLiveSync();
        }));
        tfSec.Children.Add(MakeSliderRow("不透明度", 10, 100, 100, out opacitySlider, out opacityVal, delegate(double v)
        {
            if (syncing || currentSkin == null) return;
            currentSkin.GetLayer(selectedSlot).Opacity = v / 100.0;
            opacityVal.Text = ((int)Math.Round(v)) + "%";
            ScheduleSaveAndLiveSync();
        }));

        Grid tfFooter = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        tfFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tfFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
        tfFooter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        flipXText = new TextBlock { Text = "水平镜像翻转：关", FontSize = 11.5, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center };
        flipXBtn = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 8, 6),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = flipXText
        };
        flipXBtn.MouseLeftButtonUp += delegate
        {
            if (currentSkin == null) return;
            SkinLayerConfig l = currentSkin.GetLayer(selectedSlot);
            l.FlipX = !l.FlipX;
            RefreshFlipBtnVisual(l.FlipX);
            ScheduleSaveAndLiveSync();
        };

        Border resetLayerBtn = MakeMiniActionBtn("重置本层位置与缩放", false, delegate
        {
            if (currentSkin == null) return;
            currentSkin.GetLayer(selectedSlot).ResetTransform(selectedSlot);
            SyncSelectedLayerSliders();
            ScheduleSaveAndLiveSync();
            SetStatus("已重置「" + CustomSkin.SlotDisplayName(selectedSlot) + "」的变换参数");
        });

        Grid.SetColumn(flipXBtn, 0);
        Grid.SetColumn(resetLayerBtn, 2);
        tfFooter.Children.Add(flipXBtn);
        tfFooter.Children.Add(resetLayerBtn);
        tfSec.Children.Add(tfFooter);

        Border tfCard = MakeSectionCard(tfSec, new Thickness(11, 9, 11, 9));
        tfCard.Margin = new Thickness(0);
        stack.Children.Add(tfCard);
        sv.Content = stack;
        return sv;
    }

    private UIElement BuildAnimStyleTab()
    {
        ScrollViewer sv = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        StackPanel stack = new StackPanel();

        // 1. Eye Drive Mode
        StackPanel eyeSec = new StackPanel();
        eyeSec.Children.Add(MakeSectionHeader("头部 PNG 眼睛驱动模式", "仅导入 head.png 时生效"));
        UniformGrid eyeGrid = new UniformGrid { Columns = 2 };
        PillItem ep1 = MakeSegmentPill("纯 PNG 表情切换", CustomEyeMode.PngOnly, delegate
        {
            if (currentSkin == null) return;
            currentSkin.EyeMode = CustomEyeMode.PngOnly;
            UpdatePillGroup(eyeModePills, currentSkin.EyeMode);
            dynamicEyesBox.Visibility = Visibility.Collapsed;
            ScheduleSaveAndLiveSync();
        });
        PillItem ep2 = MakeSegmentPill("叠加动态追踪眼", CustomEyeMode.OverlayDynamicEyes, delegate
        {
            if (currentSkin == null) return;
            currentSkin.EyeMode = CustomEyeMode.OverlayDynamicEyes;
            UpdatePillGroup(eyeModePills, currentSkin.EyeMode);
            dynamicEyesBox.Visibility = Visibility.Visible;
            ScheduleSaveAndLiveSync();
        });
        eyeModePills.Add(ep1);
        eyeModePills.Add(ep2);
        eyeGrid.Children.Add(ep1.Root);
        eyeGrid.Children.Add(ep2.Root);
        eyeSec.Children.Add(WrapSegmentContainer(eyeGrid));

        StackPanel dynBox = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        dynBox.Children.Add(MakeSliderRow("眼部高度", -20, 20, 0, out eyeOffYSlider, out eyeOffYVal, delegate(double v)
        {
            if (syncing || currentSkin == null) return;
            currentSkin.EyeOffsetY = v;
            eyeOffYVal.Text = ((int)Math.Round(v)) + " px";
            ScheduleSaveAndLiveSync();
        }));
        dynBox.Children.Add(MakeSliderRow("双眼睛距", 12, 28, 19, out eyeSpacingSlider, out eyeSpacingVal, delegate(double v)
        {
            if (syncing || currentSkin == null) return;
            currentSkin.EyeSpacing = v;
            eyeSpacingVal.Text = v.ToString("0.0", CultureInfo.InvariantCulture);
            ScheduleSaveAndLiveSync();
        }));
        dynBox.Children.Add(MakeSliderRow("眼睛大小", 60, 145, 100, out eyeScaleSlider, out eyeScaleVal, delegate(double v)
        {
            if (syncing || currentSkin == null) return;
            currentSkin.EyeScale = v / 100.0;
            eyeScaleVal.Text = ((int)Math.Round(v)) + "%";
            ScheduleSaveAndLiveSync();
        }));

        // Iris color swatches + hex input
        DockPanel irisRow = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
        TextBlock irisLbl = new TextBlock { Text = "虹膜颜色", FontSize = 11, Foreground = TextPrimary, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        DockPanel.SetDock(irisLbl, Dock.Left);
        irisRow.Children.Add(irisLbl);

        StackPanel irisControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        string[] irisPresets = { "#D7F550", "#F7CB4E", "#7EC87A", "#8BC4EB", "#F59AC2", "#E65C52" };
        for (int i = 0; i < irisPresets.Length; ++i)
        {
            string hex = irisPresets[i];
            irisControls.Children.Add(MakeColorSwatch(hex, delegate
            {
                if (currentSkin == null) return;
                currentSkin.EyeIrisHex = hex;
                syncing = true;
                eyeIrisHexBox.Text = hex;
                eyeIrisSwatch.Background = new SolidColorBrush(CustomSkin.ParseHexColor(hex, Colors.YellowGreen));
                syncing = false;
                ScheduleSaveAndLiveSync();
            }));
        }
        eyeIrisSwatch = new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(5), BorderBrush = BrushOf(0xC8BCA6), BorderThickness = new Thickness(1), Margin = new Thickness(6, 0, 4, 0) };
        eyeIrisHexBox = new TextBox { Width = 64, FontSize = 11, Padding = new Thickness(4, 2, 4, 2), Background = ItemBg, BorderBrush = ItemBorder, MaxLength = 7 };
        eyeIrisHexBox.TextChanged += delegate
        {
            if (syncing || currentSkin == null) return;
            string raw = (eyeIrisHexBox.Text ?? "").Trim();
            if (raw.Length == 7 && raw[0] == '#')
            {
                currentSkin.EyeIrisHex = CustomSkin.NormalizeHex(raw, currentSkin.EyeIrisHex);
                eyeIrisSwatch.Background = new SolidColorBrush(CustomSkin.ParseHexColor(currentSkin.EyeIrisHex, Colors.YellowGreen));
                ScheduleSaveAndLiveSync();
            }
        };
        eyeIrisHexBox.LostFocus += delegate
        {
            if (currentSkin == null) return;
            syncing = true;
            eyeIrisHexBox.Text = currentSkin.EyeIrisHex;
            syncing = false;
        };
        irisControls.Children.Add(eyeIrisSwatch);
        irisControls.Children.Add(eyeIrisHexBox);
        irisRow.Children.Add(irisControls);
        dynBox.Children.Add(irisRow);
        dynamicEyesBox = dynBox;
        eyeSec.Children.Add(dynBox);
        stack.Children.Add(MakeSectionCard(eyeSec, new Thickness(11, 9, 11, 9)));

        // 2. Arm & Paw Mode
        StackPanel armSec = new StackPanel();
        armSec.Children.Add(MakeSectionHeader("手臂与爪子驱动方式", "适配键盘 / 鼠标 / 手柄联动"));
        UniformGrid armGrid = new UniformGrid { Columns = 3 };
        CustomArmMode[] armModes = { CustomArmMode.FlexArmWithPngPaw, CustomArmMode.FullArmSprite, CustomArmMode.BaseRigArms };
        string[] armLabels = { "弹性臂+PNG爪", "整臂PNG拉伸", "仅底模手臂" };
        for (int i = 0; i < armModes.Length; ++i)
        {
            CustomArmMode am = armModes[i];
            PillItem ap = MakeSegmentPill(armLabels[i], am, delegate
            {
                if (currentSkin == null) return;
                currentSkin.ArmMode = am;
                UpdatePillGroup(armModePills, am);
                ScheduleSaveAndLiveSync();
            });
            armModePills.Add(ap);
            armGrid.Children.Add(ap.Root);
        }
        armSec.Children.Add(WrapSegmentContainer(armGrid));

        mirrorPawToggle = new MiniToggle(delegate(bool on)
        {
            if (currentSkin == null) return;
            currentSkin.MirrorLeftPawToRight = on;
            RefreshSlotButtons();
            ScheduleSaveAndLiveSync();
        });
        armSec.Children.Add(MakeMiniToggleRow("右爪自动镜像左爪", "未单独导入右爪 PNG 时自动水平镜像左爪", mirrorPawToggle, 8));

        customArmColorToggle = new MiniToggle(delegate(bool on)
        {
            if (currentSkin == null) return;
            currentSkin.UseCustomArmColor = on;
            customArmColorRow.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            ScheduleSaveAndLiveSync();
        });
        armSec.Children.Add(MakeMiniToggleRow("自定义弯曲手臂毛色", "在弹性臂模式下让手臂无缝匹配自定义身体贴图", customArmColorToggle, 6));

        StackPanel armColorSec = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        StackPanel armColorLine = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        string[] furPresets = { "#242226", "#F3AC65", "#FFF9F2", "#B78355", "#DCE0E3", "#C7B096" };
        for (int i = 0; i < furPresets.Length; ++i)
        {
            string hex = furPresets[i];
            armColorLine.Children.Add(MakeColorSwatch(hex, delegate
            {
                if (currentSkin == null) return;
                currentSkin.ArmColorHex = hex;
                currentSkin.ArmTipColorHex = hex;
                syncing = true;
                armColorHexBox.Text = hex;
                armColorSwatch.Background = new SolidColorBrush(CustomSkin.ParseHexColor(hex, Colors.Black));
                syncing = false;
                ScheduleSaveAndLiveSync();
            }));
        }
        armColorSwatch = new Border { Width = 18, Height = 18, CornerRadius = new CornerRadius(5), BorderBrush = BrushOf(0xC8BCA6), BorderThickness = new Thickness(1), Margin = new Thickness(5, 0, 4, 0) };
        armColorHexBox = new TextBox { Width = 62, FontSize = 11, Padding = new Thickness(4, 2, 4, 2), Background = ItemBg, BorderBrush = ItemBorder, MaxLength = 7 };
        armColorHexBox.TextChanged += delegate
        {
            if (syncing || currentSkin == null) return;
            string raw = (armColorHexBox.Text ?? "").Trim();
            if (raw.Length == 7 && raw[0] == '#')
            {
                currentSkin.ArmColorHex = CustomSkin.NormalizeHex(raw, currentSkin.ArmColorHex);
                currentSkin.ArmTipColorHex = currentSkin.ArmColorHex;
                armColorSwatch.Background = new SolidColorBrush(CustomSkin.ParseHexColor(currentSkin.ArmColorHex, Colors.Black));
                ScheduleSaveAndLiveSync();
            }
        };
        armColorHexBox.LostFocus += delegate
        {
            if (currentSkin == null) return;
            syncing = true;
            armColorHexBox.Text = currentSkin.ArmColorHex;
            syncing = false;
        };
        armColorLine.Children.Add(armColorSwatch);
        armColorLine.Children.Add(armColorHexBox);

        Border sampleBtn = MakeMiniActionBtn("自动吸色", false, HandleAutoSampleArmColor);
        sampleBtn.Margin = new Thickness(6, 0, 0, 0);
        sampleBtn.Padding = new Thickness(7, 3, 7, 3);
        sampleBtn.ToolTip = "自动从身体/头部 PNG 中吸取主毛色";
        armColorLine.Children.Add(sampleBtn);
        armColorSec.Children.Add(armColorLine);
        customArmColorRow = armColorSec;
        armSec.Children.Add(armColorSec);

        stack.Children.Add(MakeSectionCard(armSec, new Thickness(11, 9, 11, 9)));

        // 3. Render & Effects Toggles
        StackPanel fxSec = new StackPanel();
        fxSec.Children.Add(MakeSectionHeader("渲染质量与动态特效", null));

        pixelArtToggle = new MiniToggle(delegate(bool on)
        {
            if (currentSkin == null) return;
            currentSkin.PixelArtFilter = on;
            ScheduleSaveAndLiveSync();
        });
        fxSec.Children.Add(MakeMiniToggleRow("像素画锐利缩放 (NearestNeighbor)", "像素风 PNG 皮肤开启后边缘清晰锐利不发糊", pixelArtToggle, 2));

        moodFxToggle = new MiniToggle(delegate(bool on)
        {
            if (currentSkin == null) return;
            currentSkin.ShowMoodEffects = on;
            ScheduleSaveAndLiveSync();
        });
        fxSec.Children.Add(MakeMiniToggleRow("保留动态情绪特效", "快速敲键流汗、呼吸灯愤怒符号与闲置 Zzz", moodFxToggle, 7));

        whiskersToggle = new MiniToggle(delegate(bool on)
        {
            if (currentSkin == null) return;
            currentSkin.ShowWhiskersOverlay = on;
            ScheduleSaveAndLiveSync();
        });
        fxSec.Children.Add(MakeMiniToggleRow("在 PNG 头部叠加动态胡须", "为未画胡须的自定义猫头贴图添加矢量胡须", whiskersToggle, 7));

        Border fxCard = MakeSectionCard(fxSec, new Thickness(11, 9, 11, 9));
        fxCard.Margin = new Thickness(0);
        stack.Children.Add(fxCard);

        sv.Content = stack;
        return sv;
    }

    private void SelectInspectorTab(int idx)
    {
        layersTabPanel.Visibility = idx == 0 ? Visibility.Visible : Visibility.Collapsed;
        animStyleTabPanel.Visibility = idx == 1 ? Visibility.Visible : Visibility.Collapsed;
        UpdatePillGroup(inspectorTabPills, idx);
    }

    private void RebuildSkinList()
    {
        if (skinListPanel == null) return;
        skinListPanel.Children.Clear();
        List<CustomSkin> skins = cb.GetSkins != null ? cb.GetSkins() : SkinManager.LoadAll();
        string activeId = cb.GetActiveSkinId != null ? cb.GetActiveSkinId() : "";

        for (int i = 0; i < skins.Count; ++i)
        {
            CustomSkin s = skins[i];
            bool selected = currentSkin != null && string.Equals(s.Id, currentSkin.Id, StringComparison.OrdinalIgnoreCase);
            bool applied = !string.IsNullOrEmpty(activeId) && string.Equals(s.Id, activeId, StringComparison.OrdinalIgnoreCase);

            DockPanel row = new DockPanel { LastChildFill = true };
            Border thumb = new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(8),
                Background = BrushOf(0xFFFDF8),
                BorderBrush = BrushOf(0xE6DCC8),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 8, 0),
                Child = RenderSkinThumb(s, 28, 28)
            };
            DockPanel.SetDock(thumb, Dock.Left);
            row.Children.Add(thumb);

            StackPanel info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            DockPanel titleRow = new DockPanel { LastChildFill = true };
            if (applied)
            {
                Border badge = new Border
                {
                    Background = BrushOf(0xFFE7A3),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(4, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock { Text = "使用中", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = BrushOf(0x70440A) }
                };
                DockPanel.SetDock(badge, Dock.Right);
                titleRow.Children.Add(badge);
            }
            titleRow.Children.Add(new TextBlock
            {
                Text = s.Name,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = selected ? BrushOf(0x4C2D05) : TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            info.Children.Add(titleRow);

            int cnt = s.ActivePngCount;
            string sub = cnt > 0 ? (cnt + " 层 PNG · " + BaseCatLabel(s.BaseCat)) : ("待导入 PNG · " + BaseCatLabel(s.BaseCat));
            info.Children.Add(new TextBlock
            {
                Text = sub,
                FontSize = 10,
                Foreground = TextSecondary,
                Margin = new Thickness(0, 2, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            row.Children.Add(info);

            Border card = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(7, 6, 7, 6),
                Margin = new Thickness(0, 0, 0, 6),
                Background = selected ? ActiveItemBg : BrushOf(0xFAF6EC),
                BorderBrush = selected ? ActiveBorder : BrushOf(0xE4DAC5),
                BorderThickness = new Thickness(selected ? 1.5 : 1),
                Cursor = Cursors.Hand,
                Child = row
            };
            CustomSkin captured = s;
            card.MouseLeftButtonUp += delegate
            {
                if (currentSkin != null) CommitCurrentSkin(false);
                currentSkin = captured;
                RebuildSkinList();
                SyncControlsFromSkin();
            };
            skinListPanel.Children.Add(card);
        }
    }

    private void SyncControlsFromSkin()
    {
        syncing = true;
        try
        {
            previewStage.Skin = currentSkin;
            previewStage.SelectedSlot = selectedSlot;

            if (currentSkin == null)
            {
                nameTextBox.Text = "";
                RefreshApplyButtonVisual();
                return;
            }

            nameTextBox.Text = currentSkin.Name;
            int catIdx = Array.IndexOf(AllBaseCats, currentSkin.BaseCat);
            baseCatCombo.SelectedIndex = catIdx >= 0 ? catIdx : 0;

            RefreshSlotButtons();
            SyncSelectedLayerSliders();

            UpdatePillGroup(eyeModePills, currentSkin.EyeMode);
            dynamicEyesBox.Visibility = currentSkin.EyeMode == CustomEyeMode.OverlayDynamicEyes ? Visibility.Visible : Visibility.Collapsed;
            eyeOffYSlider.Value = currentSkin.EyeOffsetY;
            eyeOffYVal.Text = ((int)Math.Round(currentSkin.EyeOffsetY)) + " px";
            eyeSpacingSlider.Value = currentSkin.EyeSpacing;
            eyeSpacingVal.Text = currentSkin.EyeSpacing.ToString("0.0", CultureInfo.InvariantCulture);
            eyeScaleSlider.Value = currentSkin.EyeScale * 100.0;
            eyeScaleVal.Text = ((int)Math.Round(currentSkin.EyeScale * 100.0)) + "%";
            eyeIrisHexBox.Text = currentSkin.EyeIrisHex;
            eyeIrisSwatch.Background = new SolidColorBrush(CustomSkin.ParseHexColor(currentSkin.EyeIrisHex, Colors.YellowGreen));

            UpdatePillGroup(armModePills, currentSkin.ArmMode);
            mirrorPawToggle.SetValue(currentSkin.MirrorLeftPawToRight);
            customArmColorToggle.SetValue(currentSkin.UseCustomArmColor);
            customArmColorRow.Visibility = currentSkin.UseCustomArmColor ? Visibility.Visible : Visibility.Collapsed;
            armColorHexBox.Text = currentSkin.ArmColorHex;
            armColorSwatch.Background = new SolidColorBrush(CustomSkin.ParseHexColor(currentSkin.ArmColorHex, Colors.Black));

            pixelArtToggle.SetValue(currentSkin.PixelArtFilter);
            moodFxToggle.SetValue(currentSkin.ShowMoodEffects);
            whiskersToggle.SetValue(currentSkin.ShowWhiskersOverlay);

            RefreshApplyButtonVisual();
            previewStage.InvalidateVisual();
        }
        finally
        {
            syncing = false;
        }
    }

    private void RefreshSlotButtons()
    {
        for (int i = 0; i < slotButtons.Count; ++i)
        {
            SlotButton sb = slotButtons[i];
            bool active = sb.Slot == selectedSlot;
            bool loaded = currentSkin != null && currentSkin.HasImage(sb.Slot);
            bool mirrored = !loaded && sb.Slot == SkinSlot.PawRight && currentSkin != null &&
                            currentSkin.MirrorLeftPawToRight && currentSkin.HasImage(SkinSlot.PawLeft);
            sb.Root.Background = active ? ActiveItemBg : ItemBg;
            sb.Root.BorderBrush = active ? ActiveBorder : ItemBorder;
            sb.StatusDot.Fill = loaded ? LoadedDotBrush : mirrored ? MirroredDotBrush : EmptyDotBrush;
            sb.TagText.Text = loaded ? "PNG" : mirrored ? "镜像" : "底模";
            sb.TagText.Foreground = loaded ? BrushOf(0x2F7B45) : mirrored ? BrushOf(0x9C630E) : TextSecondary;
        }
        slotHintText.Text = CustomSkin.SlotHint(selectedSlot);
        bool hasImg = currentSkin != null && currentSkin.HasImage(selectedSlot);
        removeSlotBtn.Opacity = hasImg ? 1.0 : 0.45;
        starterAccessoryRow.Visibility = selectedSlot == SkinSlot.Accessory ? Visibility.Visible : Visibility.Collapsed;
        extractFullKitRow.Visibility = selectedSlot == SkinSlot.Accessory ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SyncSelectedLayerSliders()
    {
        if (currentSkin == null) return;
        bool prev = syncing;
        syncing = true;
        try
        {
            SkinLayerConfig l = currentSkin.GetLayer(selectedSlot);
            offXSlider.Value = l.OffsetX;
            offXVal.Text = ((int)Math.Round(l.OffsetX)) + " px";
            offYSlider.Value = l.OffsetY;
            offYVal.Text = ((int)Math.Round(l.OffsetY)) + " px";
            scaleSlider.Value = l.Scale * 100.0;
            scaleVal.Text = ((int)Math.Round(l.Scale * 100.0)) + "%";
            rotSlider.Value = l.Rotation;
            rotVal.Text = ((int)Math.Round(l.Rotation)) + "°";
            opacitySlider.Value = l.Opacity * 100.0;
            opacityVal.Text = ((int)Math.Round(l.Opacity * 100.0)) + "%";
            RefreshFlipBtnVisual(l.FlipX);
            previewStage.InvalidateVisual();
        }
        finally
        {
            syncing = prev;
        }
    }

    private void RefreshFlipBtnVisual(bool flipped)
    {
        flipXText.Text = flipped ? "水平镜像翻转：开" : "水平镜像翻转：关";
        flipXBtn.Background = flipped ? ActiveItemBg : ItemBg;
        flipXBtn.BorderBrush = flipped ? ActiveBorder : ItemBorder;
        flipXText.Foreground = flipped ? TextAmber : BrushOf(0x5C4D3E);
    }

    private void RefreshGuideToggleVisual()
    {
        bool on = previewStage.ShowGuide;
        guideToggleBtn.Background = on ? BrushOf(0xFFE7A3) : SidebarBg;
        guideToggleBtn.BorderBrush = on ? BrushOf(0xE6A324) : BrushOf(0xE2D8C3);
        guideToggleText.Foreground = on ? BrushOf(0x5A3809) : BrushOf(0x6D5D4C);
    }

    private void RefreshApplyButtonVisual()
    {
        string activeId = cb.GetActiveSkinId != null ? cb.GetActiveSkinId() : "";
        bool applied = currentSkin != null && !string.IsNullOrEmpty(activeId) &&
                       string.Equals(activeId, currentSkin.Id, StringComparison.OrdinalIgnoreCase);
        applyBtnText.Text = applied ? "当前桌宠已应用（实时同步中）" : "应用此 PNG 皮肤到桌宠";
    }

    private void ScheduleSaveAndLiveSync()
    {
        previewStage.InvalidateVisual();
        saveDebounce.Stop();
        saveDebounce.Start();
        if (currentSkin != null && cb.OnSkinMutated != null) cb.OnSkinMutated(currentSkin);
    }

    private void CommitCurrentSkin(bool refreshThumbs)
    {
        if (currentSkin == null) return;
        string keepId = currentSkin.Id;
        SkinManager.Save(currentSkin);
        if (cb.OnSkinMutated != null) cb.OnSkinMutated(currentSkin);
        if (refreshThumbs)
        {
            if (cb.OnSkinListChanged != null) cb.OnSkinListChanged();
            RebindCurrentSkinFromList(keepId);
            RebuildSkinList();
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.Text = message ?? "";
    }

    // --- Handlers ---

    private void HandleCreateNewSkin()
    {
        CatStyle baseCat = cb.GetBaseCatFallback != null ? cb.GetBaseCatFallback() : CatStyle.Black;
        List<CustomSkin> existing = cb.GetSkins != null ? cb.GetSkins() : SkinManager.LoadAll();
        string name = "贴图皮肤 #" + (existing.Count + 1);
        CustomSkin created = SkinManager.CreateNew(name, baseCat);
        currentSkin = created;
        if (cb.OnSkinListChanged != null) cb.OnSkinListChanged();
        RebindCurrentSkinFromList(created.Id);
        RebuildSkinList();
        SyncControlsFromSkin();
        SetStatus("已新建「" + created.Name + "」，可直接导入或拖入 PNG 贴图");
    }

    private void HandleDuplicateSkin()
    {
        if (currentSkin == null) return;
        CommitCurrentSkin(false);
        CustomSkin copy = SkinManager.Duplicate(currentSkin);
        if (copy == null) { SetStatus("复制皮肤失败"); return; }
        currentSkin = copy;
        if (cb.OnSkinListChanged != null) cb.OnSkinListChanged();
        RebindCurrentSkinFromList(copy.Id);
        RebuildSkinList();
        SyncControlsFromSkin();
        SetStatus("已复制为「" + copy.Name + "」");
    }

    private void HandleDeleteSkin()
    {
        if (currentSkin == null) return;
        string name = currentSkin.Name;
        string id = currentSkin.Id;
        MessageBoxResult res = MessageBox.Show(this,
            "确定要删除自定义皮肤「" + name + "」及其目录内的贴图文件吗？此操作无法撤销。",
            "删除自定义皮肤", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (res != MessageBoxResult.Yes) return;

        string activeId = cb.GetActiveSkinId != null ? cb.GetActiveSkinId() : "";
        bool wasActive = string.Equals(activeId, id, StringComparison.OrdinalIgnoreCase);
        SkinManager.Delete(id);
        if (wasActive && cb.ClearCustomSkin != null) cb.ClearCustomSkin();
        if (cb.OnSkinListChanged != null) cb.OnSkinListChanged();

        List<CustomSkin> rest = cb.GetSkins != null ? cb.GetSkins() : SkinManager.LoadAll();
        currentSkin = rest.Count > 0 ? rest[0] : null;
        RebuildSkinList();
        SyncControlsFromSkin();
        SetStatus("已删除皮肤「" + name + "」");
    }

    private void HandleOpenSkinFolder()
    {
        if (currentSkin == null || string.IsNullOrEmpty(currentSkin.DirectoryPath)) return;
        try
        {
            Directory.CreateDirectory(currentSkin.DirectoryPath);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + currentSkin.DirectoryPath + "\"") { UseShellExecute = true });
            SetStatus("已打开皮肤目录，外部修改 PNG 保存后点击「重载」即可即时生效");
        }
        catch (Exception)
        {
            SetStatus("无法打开皮肤目录");
        }
    }

    private void HandleApplyCurrentSkin()
    {
        if (currentSkin == null) return;
        CommitCurrentSkin(false);
        if (cb.ApplySkin != null) cb.ApplySkin(currentSkin);
        RefreshApplyButtonVisual();
        RebuildSkinList();
        SetStatus("已将「" + currentSkin.Name + "」应用到桌面小猫！");
    }

    private void HandleImportSlotImage()
    {
        if (currentSkin == null) { HandleCreateNewSkin(); }
        OpenFileDialog dlg = new OpenFileDialog
        {
            Title = "选择要导入到「" + CustomSkin.SlotDisplayName(selectedSlot) + "」的图片",
            Filter = "PNG 透明贴图 (*.png)|*.png|所有支持的图片 (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };
        if (dlg.ShowDialog(this) != true) return;
        ImportFileIntoSlot(dlg.FileName, selectedSlot);
    }

    private void ImportFileIntoSlot(string filePath, SkinSlot slot)
    {
        if (currentSkin == null) HandleCreateNewSkin();
        string err = SkinManager.ImportImageToSlot(currentSkin, slot, filePath);
        if (err != null)
        {
            SetStatus(err);
            return;
        }
        SyncControlsFromSkin();
        CommitCurrentSkin(true);
        SetStatus("已导入「" + System.IO.Path.GetFileName(filePath) + "」到 " + CustomSkin.SlotDisplayName(slot));
    }

    private void HandleRemoveSlotImage()
    {
        if (currentSkin == null || !currentSkin.HasImage(selectedSlot)) return;
        SkinManager.RemoveSlotImage(currentSkin, selectedSlot);
        SyncControlsFromSkin();
        CommitCurrentSkin(true);
        SetStatus("已移除「" + CustomSkin.SlotDisplayName(selectedSlot) + "」贴图，恢复底模渲染");
    }

    private void HandleReloadImages()
    {
        if (currentSkin == null) return;
        currentSkin.ReloadImages();
        SyncControlsFromSkin();
        CommitCurrentSkin(true);
        SetStatus("已从磁盘重新加载所有 PNG 贴图");
    }

    private void HandleExtractSingleSlot()
    {
        if (currentSkin == null) return;
        if (selectedSlot == SkinSlot.Accessory)
        {
            HandleExportTemplatePack();
            return;
        }
        string err = SkinManager.ExtractBaseCatToPngs(currentSkin, selectedSlot);
        if (err != null) { SetStatus(err); return; }
        SyncControlsFromSkin();
        CommitCurrentSkin(true);
        SetStatus("已从底模「" + BaseCatLabel(currentSkin.BaseCat) + "」提取 " + CustomSkin.SlotDefaultFileName(selectedSlot) + "，可打开目录二创");
    }

    private void HandleExtractAllSlots()
    {
        if (currentSkin == null) return;
        string err = SkinManager.ExtractBaseCatToPngs(currentSkin, null);
        if (err != null) { SetStatus(err); return; }
        SyncControlsFromSkin();
        CommitCurrentSkin(true);
        SetStatus("已提取底模全套 6 张高清 PNG 到皮肤目录！点左侧「打开目录」即可直接编辑");
    }

    private void HandleAutoSampleArmColor()
    {
        if (currentSkin == null) return;
        BitmapSource src = currentSkin.GetImage(SkinSlot.Body) ??
                           currentSkin.GetImage(SkinSlot.Head) ??
                           currentSkin.GetImage(SkinSlot.PawLeft);
        if (src == null)
        {
            SetStatus("请先导入身体、头部或爪子 PNG，再使用自动吸色");
            return;
        }
        Color sampled;
        if (!CustomSkin.TrySampleDominantColor(src, out sampled))
        {
            SetStatus("未能从贴图中提取到有效不透明颜色");
            return;
        }
        string hex = CustomSkin.ToHexColor(sampled);
        currentSkin.UseCustomArmColor = true;
        currentSkin.ArmColorHex = hex;
        currentSkin.ArmTipColorHex = hex;
        SyncControlsFromSkin();
        ScheduleSaveAndLiveSync();
        SetStatus("已从 PNG 贴图自动吸取主毛色：" + hex);
    }

    private void HandleExportSkinPack()
    {
        if (currentSkin == null) return;
        CommitCurrentSkin(false);
        string safeName = string.IsNullOrEmpty(currentSkin.Name) ? "custom-skin" : currentSkin.Name;
        char[] invalid = System.IO.Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalid.Length; ++i) safeName = safeName.Replace(invalid[i], '_');

        SaveFileDialog dlg = new SaveFileDialog
        {
            Title = "导出可分享皮肤包 (.kcat)",
            FileName = safeName + ".kcat",
            Filter = "键盘小黑猫皮肤包 (*.kcat)|*.kcat|ZIP 压缩包 (*.zip)|*.zip"
        };
        if (dlg.ShowDialog(this) != true) return;
        string err = SkinManager.ExportSkinPack(currentSkin, dlg.FileName);
        SetStatus(err ?? ("皮肤包已导出：" + System.IO.Path.GetFileName(dlg.FileName)));
    }

    private void HandleImportSkinPack()
    {
        OpenFileDialog dlg = new OpenFileDialog
        {
            Title = "导入皮肤包 (.kcat / .zip)",
            Filter = "皮肤包 (*.kcat;*.zip)|*.kcat;*.zip|所有文件 (*.*)|*.*",
            CheckFileExists = true
        };
        if (dlg.ShowDialog(this) != true) return;
        ImportPackFromFile(dlg.FileName);
    }

    private void ImportPackFromFile(string zipPath)
    {
        CustomSkin imported;
        string err = SkinManager.ImportSkinPack(zipPath, out imported);
        if (err != null || imported == null)
        {
            SetStatus(err ?? "导入皮肤包失败");
            return;
        }
        currentSkin = imported;
        if (cb.OnSkinListChanged != null) cb.OnSkinListChanged();
        RebindCurrentSkinFromList(imported.Id);
        RebuildSkinList();
        SyncControlsFromSkin();
        SetStatus("成功导入皮肤包「" + imported.Name + "」（" + imported.ActivePngCount + " 层 PNG）");
    }

    private void HandleExportTemplatePack()
    {
        CatStyle baseCat = currentSkin != null ? currentSkin.BaseCat : CatStyle.Black;
        string selectedFolder = null;
        bool wasTopmost = Topmost;
        try
        {
            Topmost = false;
            using (Forms.FolderBrowserDialog dlg = new Forms.FolderBrowserDialog())
            {
                dlg.Description = "选择导出 PNG 绘画模板的文件夹（含 5 张 4x 透明底稿与制作说明）";
                dlg.ShowNewFolderButton = true;
                if (dlg.ShowDialog() == Forms.DialogResult.OK) selectedFolder = dlg.SelectedPath;
            }
        }
        finally
        {
            Topmost = wasTopmost;
        }
        if (string.IsNullOrEmpty(selectedFolder)) return;

        string err = SkinManager.ExportTemplatePack(baseCat, selectedFolder);
        if (err != null) { SetStatus(err); return; }
        SetStatus("已导出 5 张 4x 高清 PNG 模板与制作说明！");
        try { Process.Start(new ProcessStartInfo("explorer.exe", "\"" + selectedFolder + "\"") { UseShellExecute = true }); }
        catch (Exception) { }
    }

    // --- Drag & Drop Support ---

    private void HandleDragOver(object sender, DragEventArgs e)
    {
        string file = FirstSupportedDropFile(e.Data);
        if (file != null)
        {
            e.Effects = DragDropEffects.Copy;
            string ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".kcat" || ext == ".zip")
                dropOverlayText.Text = "松开鼠标导入皮肤包：" + System.IO.Path.GetFileName(file);
            else
                dropOverlayText.Text = "松开鼠标导入到「" + CustomSkin.SlotDisplayName(selectedSlot) + "」";
            dropOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
            dropOverlay.Visibility = Visibility.Collapsed;
        }
        e.Handled = true;
    }

    private void HandleDragLeave(object sender, DragEventArgs e)
    {
        dropOverlay.Visibility = Visibility.Collapsed;
    }

    private void HandleDrop(object sender, DragEventArgs e)
    {
        dropOverlay.Visibility = Visibility.Collapsed;
        string file = FirstSupportedDropFile(e.Data);
        if (file == null) return;
        string ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
        if (ext == ".kcat" || ext == ".zip") ImportPackFromFile(file);
        else ImportFileIntoSlot(file, selectedSlot);
        e.Handled = true;
    }

    private static string FirstSupportedDropFile(IDataObject data)
    {
        if (data == null || !data.GetDataPresent(DataFormats.FileDrop)) return null;
        string[] files = data.GetData(DataFormats.FileDrop) as string[];
        if (files == null || files.Length == 0) return null;
        for (int i = 0; i < files.Length; ++i)
        {
            string f = files[i];
            if (string.IsNullOrEmpty(f) || !File.Exists(f)) continue;
            string ext = System.IO.Path.GetExtension(f).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" ||
                ext == ".kcat" || ext == ".zip")
                return f;
        }
        return null;
    }

    // --- UI Builders ---

    private SlotButton MakeSlotButton(SkinSlot slot)
    {
        Ellipse dot = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = EmptyDotBrush,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBlock nameTxt = new TextBlock
        {
            Text = CustomSkin.SlotDisplayName(slot),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        TextBlock tagTxt = new TextBlock
        {
            Text = "底模",
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSecondary,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0)
        };

        DockPanel row = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(dot, Dock.Left);
        DockPanel.SetDock(tagTxt, Dock.Right);
        row.Children.Add(dot);
        row.Children.Add(tagTxt);
        row.Children.Add(nameTxt);

        Border root = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(2),
            Background = ItemBg,
            BorderBrush = ItemBorder,
            BorderThickness = new Thickness(1.2),
            Cursor = Cursors.Hand,
            Child = row
        };
        root.MouseLeftButtonUp += delegate
        {
            selectedSlot = slot;
            previewStage.SelectedSlot = slot;
            if (slot == SkinSlot.HeadBlink)
            {
                previewStage.AnimMode = PreviewAnimMode.Blink;
                UpdatePillGroup(animPills, PreviewAnimMode.Blink);
            }
            else if (slot == SkinSlot.HeadAngry)
            {
                previewStage.AnimMode = PreviewAnimMode.Anger;
                UpdatePillGroup(animPills, PreviewAnimMode.Anger);
            }
            else if (slot == SkinSlot.Head &&
                     (previewStage.AnimMode == PreviewAnimMode.Blink || previewStage.AnimMode == PreviewAnimMode.Anger))
            {
                previewStage.AnimMode = PreviewAnimMode.Idle;
                UpdatePillGroup(animPills, PreviewAnimMode.Idle);
            }
            RefreshSlotButtons();
            SyncSelectedLayerSliders();
        };
        return new SlotButton { Root = root, StatusDot = dot, TagText = tagTxt, Slot = slot };
    }

    private void AddBgPill(StackPanel parent, string label, PreviewBgMode mode)
    {
        PillItem pill = MakeSmallPill(label, mode, delegate
        {
            previewStage.BgMode = mode;
            UpdatePillGroup(bgPills, mode);
            previewStage.InvalidateVisual();
        });
        pill.Root.Margin = new Thickness(2, 0, 0, 0);
        pill.Root.Padding = new Thickness(6, 2, 6, 2);
        bgPills.Add(pill);
        parent.Children.Add(pill.Root);
    }

    private PillItem MakeSmallPill(string text, object value, Action onClick)
    {
        TextBlock tb = new TextBlock
        {
            Text = text,
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushOf(0x6D5D4C),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Border b = new Border
        {
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(5, 4, 5, 4),
            Margin = new Thickness(2),
            Background = SidebarBg,
            BorderBrush = BrushOf(0xE5DCC9),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = tb
        };
        b.MouseLeftButtonUp += delegate { onClick(); };
        return new PillItem { Root = b, Label = tb, Value = value };
    }

    private PillItem MakeSegmentPill(string text, object value, Action onClick)
    {
        TextBlock tb = new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushOf(0x6F5F4E),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Border b = new Border
        {
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(4, 5, 4, 5),
            Margin = new Thickness(1.5),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Transparent,
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = tb
        };
        b.MouseLeftButtonUp += delegate { onClick(); };
        return new PillItem { Root = b, Label = tb, Value = value };
    }

    private static Border WrapSegmentContainer(UIElement child)
    {
        return new Border
        {
            Background = SidebarBg,
            BorderBrush = BrushOf(0xE5DCC9),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(2),
            Child = child
        };
    }

    private static void UpdatePillGroup(List<PillItem> list, object selected)
    {
        for (int i = 0; i < list.Count; ++i)
        {
            bool on = Equals(list[i].Value, selected);
            list[i].Root.Background = on ? BrushOf(0xFFE7A3) : SidebarBg;
            list[i].Root.BorderBrush = on ? BrushOf(0xE7A227) : BrushOf(0xE5DCC9);
            list[i].Label.Foreground = on ? BrushOf(0x5A3809) : BrushOf(0x6D5D4C);
        }
    }

    private static Border MakeSectionCard(UIElement child, Thickness padding)
    {
        return new Border
        {
            Background = CardBg,
            BorderBrush = CardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = padding,
            Margin = new Thickness(0, 0, 0, 9),
            Child = child
        };
    }

    private static DockPanel MakeSectionHeader(string title, string hint)
    {
        DockPanel row = new DockPanel { Margin = new Thickness(0, 0, 0, 7) };
        TextBlock left = new TextBlock
        {
            Text = title,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(left, Dock.Left);
        row.Children.Add(left);
        if (!string.IsNullOrEmpty(hint))
        {
            TextBlock right = new TextBlock
            {
                Text = hint,
                FontSize = 10.5,
                Foreground = TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(right, Dock.Right);
            row.Children.Add(right);
        }
        return row;
    }

    private static Grid MakeSliderRow(string label, double min, double max, double init,
                                      out Slider slider, out TextBlock valueText, Action<double> onChanged)
    {
        Grid g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(66) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });

        TextBlock lbl = new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        Slider sl = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = init,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = 10,
            IsSnapToTickEnabled = true,
            IsMoveToPointEnabled = true,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 6, 0),
            Cursor = Cursors.Hand
        };
        TextBlock val = new TextBlock
        {
            Text = ((int)init).ToString(CultureInfo.InvariantCulture),
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushOf(0x7B5010),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        sl.ValueChanged += delegate { onChanged(sl.Value); };
        Grid.SetColumn(lbl, 0);
        Grid.SetColumn(sl, 1);
        Grid.SetColumn(val, 2);
        g.Children.Add(lbl);
        g.Children.Add(sl);
        g.Children.Add(val);
        slider = sl;
        valueText = val;
        return g;
    }

    private static DockPanel MakeMiniToggleRow(string title, string desc, MiniToggle toggle, double topMargin)
    {
        DockPanel row = new DockPanel { LastChildFill = true, Margin = new Thickness(0, topMargin, 0, 0) };
        DockPanel.SetDock(toggle.Track, Dock.Right);
        row.Children.Add(toggle.Track);

        StackPanel textCol = new StackPanel { Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
        textCol.Children.Add(new TextBlock { Text = title, FontSize = 11.5, FontWeight = FontWeights.SemiBold, Foreground = TextPrimary });
        textCol.Children.Add(new TextBlock { Text = desc, FontSize = 10, Foreground = TextSecondary, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 1, 0, 0) });
        row.Children.Add(textCol);
        return row;
    }

    private static Border MakeColorSwatch(string hex, Action onPick)
    {
        Border b = new Border
        {
            Width = 17,
            Height = 17,
            CornerRadius = new CornerRadius(5),
            Margin = new Thickness(0, 0, 4, 0),
            Background = new SolidColorBrush(CustomSkin.ParseHexColor(hex, Colors.Gray)),
            BorderBrush = BrushOf(0xC8BCA6),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = hex
        };
        b.MouseLeftButtonUp += delegate { onPick(); };
        return b;
    }

    private static Border MakeCompactBtn(string text, bool danger, Action click)
    {
        Brush normalBg = danger ? BrushOf(0xFDF2EE) : BrushOf(0xFFFDF6);
        Brush hoverBg = danger ? BrushOf(0xF9E3DC) : BrushOf(0xFFF3D4);
        Border b = new Border
        {
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(4, 6, 4, 6),
            Margin = new Thickness(2),
            Background = normalBg,
            BorderBrush = danger ? BrushOf(0xF0CEC3) : BrushOf(0xE2D8C3),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = danger ? BrushOf(0xA84332) : BrushOf(0x5A4837),
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        b.MouseEnter += delegate { b.Background = hoverBg; };
        b.MouseLeave += delegate { b.Background = normalBg; };
        b.MouseLeftButtonUp += delegate { click(); };
        return b;
    }

    private static Border MakeMiniActionBtn(string text, bool danger, Action click)
    {
        Brush normalBg = danger ? BrushOf(0xFDF2EE) : BrushOf(0xFAF5E9);
        Brush hoverBg = danger ? BrushOf(0xF9E3DC) : BrushOf(0xFFF2CF);
        Border b = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 6, 6, 6),
            Background = normalBg,
            BorderBrush = danger ? BrushOf(0xF0CEC3) : BrushOf(0xE2D8C3),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = danger ? BrushOf(0xA84332) : BrushOf(0x524234),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };
        b.MouseEnter += delegate { b.Background = hoverBg; };
        b.MouseLeave += delegate { b.Background = normalBg; };
        b.MouseLeftButtonUp += delegate { click(); };
        return b;
    }

    private static Image RenderCatIcon(CatStyle style, double w, double h)
    {
        DrawingGroup art = new DrawingGroup();
        using (DrawingContext dc = art.Open()) CatArt.DrawPortrait(dc, style);
        art.Freeze();
        DrawingImage img = new DrawingImage(art);
        img.Freeze();
        return new Image { Source = img, Width = w, Height = h, Stretch = Stretch.Uniform };
    }

    private static Image RenderSkinThumb(CustomSkin skin, double w, double h)
    {
        DrawingGroup art = new DrawingGroup();
        using (DrawingContext dc = art.Open()) CustomSkinRenderer.DrawPortrait(dc, skin);
        art.Freeze();
        DrawingImage img = new DrawingImage(art);
        img.Freeze();
        return new Image { Source = img, Width = w, Height = h, Stretch = Stretch.Uniform };
    }

    private static string BaseCatLabel(CatStyle style)
    {
        int idx = Array.IndexOf(AllBaseCats, style);
        return idx >= 0 ? AllBaseCatNames[idx] : "经典黑猫";
    }
}
