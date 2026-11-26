using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

// Cream-style modular WPF settings window matching the classic cream keyboard & gamepad palette.
internal sealed partial class SettingsWindow : Window
{
    internal sealed class Callbacks
    {
        public Func<CatStyle> GetCat;
        public Action<CatStyle> SetCat;
        public Func<string> GetCustomSkinId;
        public Action<CatSkinData> ApplyCustomSkin;
        public Func<double> GetScale;
        public Action<double> SetScale;
        public Action CommitScale;
        public Func<KeyboardStyle> GetKeyboard;
        public Action<KeyboardStyle> SetKeyboard;
        public Func<MouseStyle> GetMouse;
        public Action<MouseStyle> SetMouse;
        public Func<GamepadMode> GetGamepadMode;
        public Action<GamepadMode> SetGamepadMode;
        public Func<GamepadStyle> GetGamepad;
        public Action<GamepadStyle> SetGamepad;
        public Func<bool> GetPasswordMode;
        public Action<bool> SetPasswordMode;
        public Func<int> GetIdleMinutes;
        public Action<int> SetIdleMinutes;
        public Func<bool> GetLabels;
        public Action<bool> SetLabels;
        public Func<bool> GetAutoCheckUpdate;
        public Action<bool> SetAutoCheckUpdate;
        public Func<bool> GetOnTop;
        public Action<bool> SetOnTop;
        public Func<bool> GetClickThrough;
        public Action<bool> SetClickThrough;
        public Func<bool> GetPetVisible;
        public Action TogglePetVisible;
        public Action DockToCorner;
        public Action ExitApp;
    }

    private sealed class SelectableCard
    {
        public Border Root;
        public Ellipse Dot;
        public object Value;
    }

    private sealed class SegmentButton
    {
        public Border Root;
        public TextBlock Label;
        public object Value;
    }

    private sealed class CreamToggle
    {
        public Border Track;
        public Border Knob;
        private bool isOn;
        public Action<bool> Changed;

        public CreamToggle(Action<bool> onChanged)
        {
            Changed = onChanged;
            Track = new Border
            {
                Width = 42,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            Knob = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
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
    private static readonly Brush HoneyDot = BrushOf(0xF0A019);
    private static readonly Brush UpdateDotBrush = BrushOf(0xE65A3E);
    private static readonly Brush DividerBrush = BrushOf(0xEFE8D8);

    private readonly Callbacks cb;
    private readonly List<Border> navButtons = new List<Border>();
    private readonly List<TextBlock> navTexts = new List<TextBlock>();
    private readonly List<Shape> navIconShapes = new List<Shape>();
    private readonly List<UIElement> tabPanels = new List<UIElement>();
    private readonly List<SelectableCard> catCards = new List<SelectableCard>();
    private readonly List<SelectableCard> appearanceCustomCards = new List<SelectableCard>();
    private readonly List<SelectableCard> kbCards = new List<SelectableCard>();
    private readonly List<SelectableCard> mouseCards = new List<SelectableCard>();
    private readonly List<SelectableCard> gpCards = new List<SelectableCard>();
    private readonly List<SegmentButton> gpModeSegs = new List<SegmentButton>();
    private readonly List<SegmentButton> idleSegs = new List<SegmentButton>();
    private readonly List<SegmentButton> scalePresets = new List<SegmentButton>();
    private StackPanel appearanceCustomStrip;
    private Slider scaleSlider;
    private TextBlock scaleValueText;
    private CreamToggle passwordToggle, labelsToggle, startupToggle, autoCheckToggle, onTopToggle, clickThroughToggle;
    private TextBlock hideBtnText;
    private Ellipse updateNavDot;
    private TextBlock updateStatusText;
    private Border updateBtn;
    private TextBlock updateBtnText;
    private Border openReleaseBtn;
    private bool updateBusy;
    private UpdateManager.CheckResult availableUpdate;
    private string lastReleaseUrl;
    private bool syncing;
    private int activeTab;
    public bool AllowClose;
    private readonly DispatcherTimer scaleFlushTimer = new DispatcherTimer(DispatcherPriority.Render);
    private bool hasPendingScale;
    private double pendingScale;

    public SettingsWindow(Callbacks callbacks)
    {
        cb = callbacks;
        Title = "键盘小黑猫 · 设置";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Width = 684;
        SizeToContent = SizeToContent.Height;
        FontFamily = UiFont;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);

        scaleFlushTimer.Interval = TimeSpan.FromMilliseconds(22);
        scaleFlushTimer.Tick += delegate { FlushPendingScale(); };

        PreviewKeyDown += delegate(object s, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (colorWheelPopup != null && colorWheelPopup.IsOpen)
                {
                    colorWheelPopup.IsOpen = false;
                }
                else
                {
                    Hide();
                }
                e.Handled = true;
            }
        };
        Closing += delegate(object s, System.ComponentModel.CancelEventArgs e)
        {
            FlushPendingScale();
            if (!AllowClose)
            {
                e.Cancel = true;
                Hide();
            }
        };

        Content = BuildRoot();
        RefreshSavedSkinLists();
        SelectTab(0);
    }

    public void OpenTab(int index)
    {
        if (index < 0 || index >= tabPanels.Count) return;
        SelectTab(index);
    }

    public void NotifyUpdateAvailable(UpdateManager.CheckResult res)
    {
        if (res == null || !string.IsNullOrEmpty(res.Error) || !res.HasUpdate) return;
        availableUpdate = res;
        lastReleaseUrl = res.ReleaseUrl;
        if (updateNavDot != null) updateNavDot.Visibility = Visibility.Visible;
        if (updateStatusText != null)
        {
            updateStatusText.Text = "已从 " + res.SourceName + " 发现新正式版 v" + res.LatestVersion + "，点击右侧自动升级";
        }
        if (openReleaseBtn != null) openReleaseBtn.Visibility = Visibility.Collapsed;
        RefreshUpdateButtonVisual();
    }

    private void ScheduleScale(double scale)
    {
        pendingScale = scale;
        hasPendingScale = true;
        if (!scaleFlushTimer.IsEnabled)
        {
            // Apply the first change right away if timer was idle, then coalesce rapid drag events.
            FlushPendingScale();
            scaleFlushTimer.Start();
        }
    }

    private void FlushPendingScale()
    {
        if (!hasPendingScale)
        {
            scaleFlushTimer.Stop();
            return;
        }
        hasPendingScale = false;
        if (cb.SetScale != null) cb.SetScale(pendingScale);
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
        // Keep DropShadowEffect on an empty sibling Border behind the content so WPF never
        // rasterizes the window's text and vectors into a 96-DPI bitmap on 2K/4K displays.
        Grid root = new Grid();

        Border shadowLayer = new Border
        {
            Margin = new Thickness(14),
            Background = CreamBg,
            CornerRadius = new CornerRadius(16),
            IsHitTestVisible = false,
            Effect = new DropShadowEffect
            {
                BlurRadius = 20,
                ShadowDepth = 5,
                Direction = 270,
                Opacity = 0.22,
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
            CornerRadius = new CornerRadius(16)
        };

        DockPanel shell = new DockPanel { LastChildFill = true };
        shell.Children.Add(BuildTitleBar());

        Grid body = new Grid { MinHeight = 496 };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border sidebar = BuildSidebar();
        Grid.SetColumn(sidebar, 0);
        body.Children.Add(sidebar);

        Grid contentHost = new Grid { Margin = new Thickness(14, 12, 14, 12) };
        tabPanels.Add(BuildAppearanceTab());
        tabPanels.Add(BuildWorkshopTab());
        tabPanels.Add(BuildInteractionTab());
        tabPanels.Add(BuildSystemTab());
        for (int i = 0; i < tabPanels.Count; ++i) contentHost.Children.Add(tabPanels[i]);
        Grid.SetColumn(contentHost, 1);
        body.Children.Add(contentHost);

        shell.Children.Add(body);
        frame.Child = shell;
        root.Children.Add(frame);
        return root;
    }

    private UIElement BuildTitleBar()
    {
        Border bar = new Border
        {
            Height = 44,
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
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(7),
            Background = BrushOf(0xFFF3CE),
            BorderBrush = BrushOf(0xECD59E),
            BorderThickness = new Thickness(1),
            Child = RenderVectorThumb(CatStyle.Wednesday, 18, 18)
        };
        left.Children.Add(iconBox);
        left.Children.Add(new TextBlock
        {
            Text = "键盘小黑猫",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary,
            Margin = new Thickness(8, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        left.Children.Add(new Border
        {
            Background = BrushOf(0xF5E8C8),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "设置",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x8A5B14)
            }
        });
        DockPanel.SetDock(left, Dock.Left);
        row.Children.Add(left);

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
            ToolTip = "关闭设置窗口",
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
        // Intercept MouseLeftButtonDown so bar's DragMove never swallows clicks on the close button.
        closeBtn.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
        {
            e.Handled = true;
        };
        closeBtn.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Hide();
        };
        DockPanel.SetDock(closeBtn, Dock.Right);
        row.Children.Add(closeBtn);

        bar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
        {
            if (closeBtn.IsMouseOver) return;
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch (InvalidOperationException) { }
            }
        };

        bar.Child = row;
        return bar;
    }

    private Border BuildSidebar()
    {
        Border side = new Border
        {
            Background = SidebarBg,
            BorderBrush = BrushOf(0xE6DEC9),
            BorderThickness = new Thickness(0, 0, 1, 0),
            CornerRadius = new CornerRadius(0, 0, 0, 15),
            Padding = new Thickness(9, 12, 9, 12)
        };
        DockPanel layout = new DockPanel { LastChildFill = false };

        StackPanel navStack = new StackPanel();
        string[] labels = { "外观装扮", "皮肤工坊", "互动模式", "系统窗口" };
        string[] iconPaths =
        {
            // Cat head silhouette
            "M 3,6.5 L 3,3 L 6,5.2 Q 8,4.6 10,5.2 L 13,3 L 13,6.5 C 13.8,7.5 14,8.9 13.6,10.2 C 12.9,12.4 10.9,13.5 8,13.5 C 5.1,13.5 3.1,12.4 2.4,10.2 C 2,8.9 2.2,7.5 3,6.5 Z",
            // Artist brush / craft spark icon
            "M 12.6,3.2 L 6.6,9.2 C 5.8,9.1 4.9,9.5 4.5,10.4 C 4.2,11.1 3.7,11.8 2.8,12.1 C 4.3,13.2 6.6,12.8 7.4,11.4 C 7.7,10.9 7.8,10.3 7.7,9.8 L 13.4,4.0 C 13.7,3.7 13.7,3.2 13.4,2.9 C 13.1,2.6 12.9,2.9 12.6,3.2 Z M 3.5,4.2 L 4.7,4.2 M 4.1,3.6 L 4.1,4.8",
            // Gamepad outline
            "M 5.5,4.5 L 10.5,4.5 A 3.5,3.5 0 0 1 10.5,11.5 L 5.5,11.5 A 3.5,3.5 0 0 1 5.5,4.5 Z M 5.5,6.8 L 5.5,9.2 M 4.3,8 L 6.7,8",
            // System sliders
            "M 3,4.5 L 13,4.5 M 3,8 L 13,8 M 3,11.5 L 13,11.5"
        };
        for (int i = 0; i < labels.Length; ++i)
        {
            int tabIdx = i;
            Path icon = new Path
            {
                Data = Geometry.Parse(iconPaths[i]),
                Width = 15,
                Height = 15,
                Stretch = Stretch.Uniform,
                Stroke = BrushOf(0x6A5B4B),
                StrokeThickness = 1.4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock tb = new TextBlock
            {
                Text = labels[i],
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x6A5B4B),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel row = new DockPanel { LastChildFill = false };
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(icon);
            sp.Children.Add(tb);
            DockPanel.SetDock(sp, Dock.Left);
            row.Children.Add(sp);

            if (i == 3)
            {
                updateNavDot = new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = UpdateDotBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed
                };
                DockPanel.SetDock(updateNavDot, Dock.Right);
                row.Children.Add(updateNavDot);
            }

            Border btn = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 9, 10, 9),
                Margin = new Thickness(0, 0, 0, 5),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Child = row
            };
            btn.MouseLeftButtonUp += delegate { SelectTab(tabIdx); };
            navButtons.Add(btn);
            navTexts.Add(tb);
            navIconShapes.Add(icon);
            navStack.Children.Add(btn);
        }
        DockPanel.SetDock(navStack, Dock.Top);
        layout.Children.Add(navStack);

        Border footWrap = new Border
        {
            BorderBrush = BrushOf(0xE2D9C6),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 9, 0, 0)
        };
        StackPanel foot = new StackPanel();
        foot.Children.Add(MakeSideButton("回到右下角", false, delegate { if (cb.DockToCorner != null) cb.DockToCorner(); }));
        foot.Children.Add(MakeSideButton("退出小猫", true, delegate { if (cb.ExitApp != null) cb.ExitApp(); }));
        footWrap.Child = foot;
        DockPanel.SetDock(footWrap, Dock.Bottom);
        layout.Children.Add(footWrap);

        side.Child = layout;
        return side;
    }

    private Border MakeSideButton(string text, bool quiet, Action click)
    {
        Border b = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 4),
            Background = quiet ? Brushes.Transparent : BrushOf(0xFFFDF6),
            BorderBrush = quiet ? Brushes.Transparent : BrushOf(0xE2D9C6),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = quiet ? BrushOf(0x8B6958) : BrushOf(0x5C4D3E),
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        b.MouseEnter += delegate { b.Background = quiet ? BrushOf(0xEBE2D1) : BrushOf(0xFFF5DB); };
        b.MouseLeave += delegate { b.Background = quiet ? Brushes.Transparent : BrushOf(0xFFFDF6); };
        b.MouseLeftButtonUp += delegate { click(); };
        return b;
    }

    private void SelectTab(int index)
    {
        if (colorWheelPopup != null && colorWheelPopup.IsOpen) colorWheelPopup.IsOpen = false;
        activeTab = index;
        for (int i = 0; i < tabPanels.Count; ++i)
        {
            bool active = i == index;
            tabPanels[i].Visibility = active ? Visibility.Visible : Visibility.Hidden;
            navButtons[i].Background = active ? (Brush)Gradient(0xFFE49E, 0xFFD066, 0, 0, 0, 1) : Brushes.Transparent;
            navButtons[i].BorderBrush = active ? BrushOf(0xE8AE36) : Brushes.Transparent;
            Brush fg = active ? BrushOf(0x4A300B) : BrushOf(0x6A5B4B);
            navTexts[i].Foreground = fg;
            navIconShapes[i].Stroke = fg;
        }
    }

    private static Border MakeSectionCard(UIElement child, Thickness? padding = null)
    {
        return new Border
        {
            Background = CardBg,
            BorderBrush = CardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = padding.HasValue ? padding.Value : new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = child
        };
    }

    private static DockPanel MakeSectionHeader(string title, string hint)
    {
        DockPanel row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        TextBlock left = new TextBlock
        {
            Text = title,
            FontSize = 12.5,
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
                FontSize = 11,
                Foreground = TextSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(right, Dock.Right);
            row.Children.Add(right);
        }
        return row;
    }

    private SelectableCard MakeVisualCard(Enum style, string label, double thumbW, double thumbH, Action onClick, bool showDot)
    {
        Grid content = new Grid();
        StackPanel sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        Border thumbHost = new Border
        {
            Height = thumbH,
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = RenderVectorThumb(style, thumbW, thumbH)
        };
        sp.Children.Add(thumbHost);
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushOf(0x4E3F30),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0)
        });
        content.Children.Add(sp);

        Ellipse dot = null;
        if (showDot)
        {
            dot = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = HoneyDot,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed
            };
            content.Children.Add(dot);
        }

        Border root = new Border
        {
            Background = ItemBg,
            BorderBrush = ItemBorder,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(4, 6, 4, 5),
            Margin = new Thickness(3),
            Cursor = Cursors.Hand,
            Child = content
        };
        root.MouseLeftButtonUp += delegate { onClick(); };
        return new SelectableCard { Root = root, Dot = dot, Value = style };
    }

    private UIElement BuildAppearanceTab()
    {
        StackPanel stack = new StackPanel();

        // 1. Built-in cats 4x2 grid + compact custom skins quick strip
        StackPanel catSec = new StackPanel();
        catSec.Children.Add(MakeSectionHeader("猫咪样式", "点击即可切换"));
        UniformGrid catGrid = new UniformGrid { Columns = 4 };
        var catDefs = new[]
        {
            new { Style = CatStyle.Wednesday, Name = "星期三" },
            new { Style = CatStyle.Ginger, Name = "奶油橘猫" },
            new { Style = CatStyle.JapaneseBobtail, Name = "短尾三花" },
            new { Style = CatStyle.PhotoTabby, Name = "长毛虎斑" },
            new { Style = CatStyle.Coffee, Name = "咖啡" },
            new { Style = CatStyle.SilverShaded, Name = "银渐层" },
            new { Style = CatStyle.Siamese, Name = "暹罗猫" }
        };
        for (int i = 0; i < catDefs.Length; ++i)
        {
            CatStyle st = catDefs[i].Style;
            SelectableCard card = MakeVisualCard(st, catDefs[i].Name, 40, 34, delegate
            {
                if (cb.SetCat != null) cb.SetCat(st);
                UpdateCardGroup(catCards, st);
                UpdateCardGroup(appearanceCustomCards, null);
                RefreshWorkshopActiveMarkers();
            }, true);
            catCards.Add(card);
            catGrid.Children.Add(card.Root);
        }

        // 8th slot: quick entry card into the pure-vector Skin Workshop
        StackPanel workshopEntrySp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Border plusCircle = new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(15),
            Margin = new Thickness(0, 2, 0, 2),
            Background = BrushOf(0xFFF3CE),
            BorderBrush = BrushOf(0xE8BC5A),
            BorderThickness = new Thickness(1),
            Child = new Path
            {
                Data = Geometry.Parse("M 7,3 L 7,11 M 3,7 L 11,7"),
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                Stroke = BrushOf(0x8A570E),
                StrokeThickness = 1.8,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        workshopEntrySp.Children.Add(plusCircle);
        workshopEntrySp.Children.Add(new TextBlock
        {
            Text = "更多自定义",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushOf(0x7B4E0E),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Border workshopEntryCard = new Border
        {
            Background = BrushOf(0xFDF7E7),
            BorderBrush = BrushOf(0xE8D2A2),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(4, 6, 4, 5),
            Margin = new Thickness(3),
            Cursor = Cursors.Hand,
            Child = workshopEntrySp
        };
        workshopEntryCard.MouseEnter += delegate { workshopEntryCard.Background = BrushOf(0xFFF1CA); };
        workshopEntryCard.MouseLeave += delegate { workshopEntryCard.Background = BrushOf(0xFDF7E7); };
        workshopEntryCard.MouseLeftButtonUp += delegate { SelectTab(1); };
        catGrid.Children.Add(workshopEntryCard);

        catSec.Children.Add(catGrid);

        // Compact custom skins quick row (fixed height so adding skins never changes window height)
        catSec.Children.Add(new Border { Height = 1, Background = DividerBrush, Margin = new Thickness(0, 7, 0, 7) });
        DockPanel customHead = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 0, 0, 5) };
        TextBlock customTitle = new TextBlock
        {
            Text = "我的自定义皮肤",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(customTitle, Dock.Left);
        customHead.Children.Add(customTitle);

        Border openWorkshopLink = new Border
        {
            Background = BrushOf(0xFFF3CE),
            BorderBrush = BrushOf(0xE8C068),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(9, 2.5, 9, 2.5),
            Cursor = Cursors.Hand,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "打开皮肤工坊",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x7B4E0E)
            }
        };
        openWorkshopLink.MouseEnter += delegate { openWorkshopLink.Background = BrushOf(0xFFE9AE); };
        openWorkshopLink.MouseLeave += delegate { openWorkshopLink.Background = BrushOf(0xFFF3CE); };
        openWorkshopLink.MouseLeftButtonUp += delegate { SelectTab(1); };
        DockPanel.SetDock(openWorkshopLink, Dock.Right);
        customHead.Children.Add(openWorkshopLink);
        catSec.Children.Add(customHead);

        appearanceCustomStrip = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        ScrollViewer customScroll = new ScrollViewer
        {
            Height = 40,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = appearanceCustomStrip
        };
        catSec.Children.Add(customScroll);
        stack.Children.Add(MakeSectionCard(catSec));

        // 2. Scale slider + quick presets
        StackPanel scaleSec = new StackPanel();
        DockPanel scaleHead = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        TextBlock scaleTitle = new TextBlock
        {
            Text = "小猫大小",
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(scaleTitle, Dock.Left);
        scaleHead.Children.Add(scaleTitle);
        scaleValueText = new TextBlock
        {
            Text = "100%",
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Foreground = BrushOf(0x8A570E)
        };
        Border pill = new Border
        {
            Background = BrushOf(0xFFF0C2),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(8, 2, 8, 2),
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = scaleValueText
        };
        DockPanel.SetDock(pill, Dock.Right);
        scaleHead.Children.Add(pill);
        scaleSec.Children.Add(scaleHead);

        scaleSlider = new Slider
        {
            Minimum = Preferences.MinPercent,
            Maximum = Preferences.MaxPercent,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = 10,
            IsSnapToTickEnabled = true,
            IsMoveToPointEnabled = true,
            Margin = new Thickness(2, 2, 2, 6),
            Cursor = Cursors.Hand
        };
        scaleSlider.ValueChanged += delegate
        {
            if (syncing) return;
            int pct = (int)Math.Round(scaleSlider.Value);
            scaleValueText.Text = pct + "%";
            UpdatePresetGroup(scalePresets, pct);
            ScheduleScale(pct / 100.0);
        };
        scaleSlider.PreviewMouseLeftButtonUp += delegate
        {
            FlushPendingScale();
            if (cb.CommitScale != null) cb.CommitScale();
        };
        scaleSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(delegate
        {
            FlushPendingScale();
            if (cb.CommitScale != null) cb.CommitScale();
        }));
        scaleSec.Children.Add(scaleSlider);

        StackPanel presetRow = new StackPanel { Orientation = Orientation.Horizontal };
        int[] presets = { 75, 100, 125, 150 };
        for (int i = 0; i < presets.Length; ++i)
        {
            int pct = presets[i];
            TextBlock tb = new TextBlock
            {
                Text = pct + "%",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x6D5D4C)
            };
            Border b = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 3, 10, 3),
                Margin = new Thickness(0, 0, 6, 0),
                Background = SidebarBg,
                BorderBrush = BrushOf(0xE5DCC9),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Child = tb
            };
            b.MouseLeftButtonUp += delegate
            {
                scaleSlider.Value = pct;
                FlushPendingScale();
                if (cb.CommitScale != null) cb.CommitScale();
            };
            scalePresets.Add(new SegmentButton { Root = b, Label = tb, Value = pct });
            presetRow.Children.Add(b);
        }
        scaleSec.Children.Add(presetRow);
        stack.Children.Add(MakeSectionCard(scaleSec));

        // 3. Keyboards & Mice side-by-side
        Grid bottomRow = new Grid();
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        bottomRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        StackPanel kbSec = new StackPanel();
        kbSec.Children.Add(MakeSectionHeader("键盘样式", null));
        UniformGrid kbGrid = new UniformGrid { Columns = 2 };
        var kbDefs = new[]
        {
            new { Style = KeyboardStyle.Classic, Name = "经典奶油" },
            new { Style = KeyboardStyle.Round, Name = "薄荷打字机" },
            new { Style = KeyboardStyle.Neon, Name = "冰蓝电竞" },
            new { Style = KeyboardStyle.Wood, Name = "胡桃木" }
        };
        for (int i = 0; i < kbDefs.Length; ++i)
        {
            KeyboardStyle st = kbDefs[i].Style;
            SelectableCard card = MakeVisualCard(st, kbDefs[i].Name, 74, 24, delegate
            {
                if (cb.SetKeyboard != null) cb.SetKeyboard(st);
                UpdateCardGroup(kbCards, st);
            }, false);
            kbCards.Add(card);
            kbGrid.Children.Add(card.Root);
        }
        kbSec.Children.Add(kbGrid);
        Border kbCard = MakeSectionCard(kbSec, new Thickness(10, 9, 10, 9));
        kbCard.Margin = new Thickness(0);
        Grid.SetColumn(kbCard, 0);
        bottomRow.Children.Add(kbCard);

        StackPanel mouseSec = new StackPanel();
        mouseSec.Children.Add(MakeSectionHeader("鼠标样式", null));
        UniformGrid mouseGrid = new UniformGrid { Columns = 2 };
        var mouseDefs = new[]
        {
            new { Style = MouseStyle.Classic, Name = "奶油滚轮" },
            new { Style = MouseStyle.Pebble, Name = "雾紫鹅卵石" },
            new { Style = MouseStyle.Gaming, Name = "冰蓝电竞" },
            new { Style = MouseStyle.Trackball, Name = "酒红轨迹球" }
        };
        for (int i = 0; i < mouseDefs.Length; ++i)
        {
            MouseStyle st = mouseDefs[i].Style;
            SelectableCard card = MakeVisualCard(st, mouseDefs[i].Name, 26, 26, delegate
            {
                if (cb.SetMouse != null) cb.SetMouse(st);
                UpdateCardGroup(mouseCards, st);
            }, false);
            mouseCards.Add(card);
            mouseGrid.Children.Add(card.Root);
        }
        mouseSec.Children.Add(mouseGrid);
        Border mouseCard = MakeSectionCard(mouseSec, new Thickness(10, 9, 10, 9));
        mouseCard.Margin = new Thickness(0);
        Grid.SetColumn(mouseCard, 2);
        bottomRow.Children.Add(mouseCard);

        stack.Children.Add(bottomRow);
        return stack;
    }

    private Border MakeSegmentBar(List<SegmentButton> list, string[] titles, object[] values, Action<object> onPick)
    {
        UniformGrid grid = new UniformGrid { Columns = titles.Length };
        for (int i = 0; i < titles.Length; ++i)
        {
            object val = values[i];
            TextBlock tb = new TextBlock
            {
                Text = titles[i],
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x6F5F4E),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Border b = new Border
            {
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(4, 6, 4, 6),
                Margin = new Thickness(2),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Child = tb
            };
            b.MouseLeftButtonUp += delegate { onPick(val); };
            list.Add(new SegmentButton { Root = b, Label = tb, Value = val });
            grid.Children.Add(b);
        }
        return new Border
        {
            Background = SidebarBg,
            BorderBrush = BrushOf(0xE5DCC9),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(2),
            Child = grid
        };
    }

    private DockPanel MakeToggleRow(string title, string desc, CreamToggle toggle)
    {
        DockPanel row = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(toggle.Track, Dock.Right);
        row.Children.Add(toggle.Track);

        StackPanel textCol = new StackPanel { Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        textCol.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary
        });
        if (!string.IsNullOrEmpty(desc))
        {
            textCol.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 11,
                Foreground = TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        row.Children.Add(textCol);
        return row;
    }

    private UIElement BuildInteractionTab()
    {
        DockPanel page = new DockPanel { LastChildFill = false };
        StackPanel stack = new StackPanel();

        // Gamepad mode + style
        StackPanel gpSec = new StackPanel();
        gpSec.Children.Add(MakeSectionHeader("手柄模式", "支持 Xbox / PS5 DualSense"));
        gpSec.Children.Add(MakeSegmentBar(
            gpModeSegs,
            new[] { "自动切换", "仅键盘鼠标", "始终手柄" },
            new object[] { GamepadMode.Auto, GamepadMode.Keyboard, GamepadMode.Gamepad },
            delegate(object v)
            {
                GamepadMode mode = (GamepadMode)v;
                if (cb.SetGamepadMode != null) cb.SetGamepadMode(mode);
                UpdateSegmentGroup(gpModeSegs, mode);
            }));

        UniformGrid gpGrid = new UniformGrid { Columns = 2, Margin = new Thickness(-3, 8, -3, 0) };
        var gpDefs = new[]
        {
            new { Style = GamepadStyle.Xbox, Name = "Xbox 手柄" },
            new { Style = GamepadStyle.PS5, Name = "PS5 手柄" }
        };
        for (int i = 0; i < gpDefs.Length; ++i)
        {
            GamepadStyle st = gpDefs[i].Style;
            SelectableCard card = MakeVisualCard(st, gpDefs[i].Name, 74, 38, delegate
            {
                if (cb.SetGamepad != null) cb.SetGamepad(st);
                UpdateCardGroup(gpCards, st);
            }, true);
            gpCards.Add(card);
            gpGrid.Children.Add(card.Root);
        }
        gpSec.Children.Add(gpGrid);
        stack.Children.Add(MakeSectionCard(gpSec));

        // Password mode
        passwordToggle = new CreamToggle(delegate(bool on) { if (cb.SetPasswordMode != null) cb.SetPasswordMode(on); });
        stack.Children.Add(MakeSectionCard(MakeToggleRow(
            "密码防偷窥模式",
            "检测到网页或桌面软件密码输入框时，小猫闭眼停爪并罩上键盘防窥罩",
            passwordToggle)));

        // Idle time
        StackPanel idleSec = new StackPanel();
        idleSec.Children.Add(MakeSectionHeader("闲置互动时间", "无操作超时后随机趴睡或抓虫子"));
        idleSec.Children.Add(MakeSegmentBar(
            idleSegs,
            new[] { "1 分钟", "3 分钟", "5 分钟", "10 分钟", "关闭" },
            new object[] { 1, 3, 5, 10, 0 },
            delegate(object v)
            {
                int mins = (int)v;
                if (cb.SetIdleMinutes != null) cb.SetIdleMinutes(mins);
                UpdateSegmentGroup(idleSegs, mins);
            }));
        stack.Children.Add(MakeSectionCard(idleSec));

        // Floating keycaps
        labelsToggle = new CreamToggle(delegate(bool on) { if (cb.SetLabels != null) cb.SetLabels(on); });
        Border capCard = MakeSectionCard(MakeToggleRow(
            "显示浮动键帽",
            "按键时在桌面上飘出键帽，连续快速输入会依次触发流汗与愤怒表情",
            labelsToggle));
        capCard.Margin = new Thickness(0);
        stack.Children.Add(capCard);

        DockPanel.SetDock(stack, Dock.Top);
        page.Children.Add(stack);

        // Petting & long-press drag gesture tip card anchored cleanly at bottom
        StackPanel petTip = new StackPanel();
        petTip.Children.Add(new TextBlock
        {
            Text = "桌面抚摸与拖动提示",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = BrushOf(0x5A4324)
        });
        petTip.Children.Add(new TextBlock
        {
            Text = "鼠标移到小猫头或身体上将切换为抚摸手势：轻揉或轻点会开心眯眼冒爱心与星星，揉得太快会不耐烦；按住不动 0.2 秒即可侧身提起抱着键鼠拖动位置。",
            FontSize = 11,
            Foreground = BrushOf(0x7C6B59),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        Border petCard = MakeSectionCard(petTip, new Thickness(12, 9, 12, 9));
        petCard.Background = BrushOf(0xF6F0E3);
        petCard.Margin = new Thickness(0, 10, 0, 0);
        DockPanel.SetDock(petCard, Dock.Bottom);
        page.Children.Add(petCard);

        return page;
    }

    private UIElement BuildSystemTab()
    {
        DockPanel page = new DockPanel { LastChildFill = false };
        StackPanel stack = new StackPanel();

        StackPanel sysToggles = new StackPanel();
        startupToggle = new CreamToggle(delegate(bool on)
        {
            if (!StartupManager.SetEnabled(on))
            {
                startupToggle.SetValue(StartupManager.IsEnabled());
            }
        });
        sysToggles.Children.Add(MakeToggleRow("开机自动启动", "登录 Windows 时自动运行键盘小黑猫", startupToggle));
        sysToggles.Children.Add(new Border { Height = 1, Background = DividerBrush, Margin = new Thickness(0, 9, 0, 9) });

        autoCheckToggle = new CreamToggle(delegate(bool on) { if (cb.SetAutoCheckUpdate != null) cb.SetAutoCheckUpdate(on); });
        sysToggles.Children.Add(MakeToggleRow("启动时自动检测更新", "启动后在后台静默检查新版本（优先 Gitee 镜像，自动回退 GitHub）", autoCheckToggle));
        sysToggles.Children.Add(new Border { Height = 1, Background = DividerBrush, Margin = new Thickness(0, 9, 0, 9) });

        onTopToggle = new CreamToggle(delegate(bool on) { if (cb.SetOnTop != null) cb.SetOnTop(on); });
        sysToggles.Children.Add(MakeToggleRow("保持窗口置顶", "小猫始终停留在其他应用窗口最前方", onTopToggle));
        sysToggles.Children.Add(new Border { Height = 1, Background = DividerBrush, Margin = new Thickness(0, 9, 0, 9) });

        clickThroughToggle = new CreamToggle(delegate(bool on) { if (cb.SetClickThrough != null) cb.SetClickThrough(on); });
        sysToggles.Children.Add(MakeToggleRow("鼠标穿透", "点击直接穿透小猫，开启后仍可通过右下角托盘图标打开设置", clickThroughToggle));
        stack.Children.Add(MakeSectionCard(sysToggles));

        // Quick window actions
        StackPanel actSec = new StackPanel();
        actSec.Children.Add(MakeSectionHeader("窗口操作", "长按小猫 0.2 秒也可直接提起拖动位置"));
        Grid btnGrid = new Grid { Margin = new Thickness(-3, 0, -3, 0) };
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        TextBlock dummy1, dummy2;
        Border bDock = MakeActionButton("回到屏幕右下角", false, delegate { if (cb.DockToCorner != null) cb.DockToCorner(); }, out dummy1);
        Border bHide = MakeActionButton("暂时隐藏小猫", false, delegate
        {
            if (cb.TogglePetVisible != null) cb.TogglePetVisible();
            SyncFromState();
        }, out hideBtnText);
        Border bExit = MakeActionButton("退出小猫", true, delegate { if (cb.ExitApp != null) cb.ExitApp(); }, out dummy2);
        Grid.SetColumn(bDock, 0);
        Grid.SetColumn(bHide, 1);
        Grid.SetColumn(bExit, 2);
        btnGrid.Children.Add(bDock);
        btnGrid.Children.Add(bHide);
        btnGrid.Children.Add(bExit);
        actSec.Children.Add(btnGrid);
        Border actCard = MakeSectionCard(actSec);
        actCard.Margin = new Thickness(0);
        stack.Children.Add(actCard);

        DockPanel.SetDock(stack, Dock.Top);
        page.Children.Add(stack);

        // Version & Gitee-first official release updater card anchored at bottom
        DockPanel updateRow = new DockPanel { LastChildFill = true };

        StackPanel updateActions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        openReleaseBtn = new Border
        {
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(8, 1, 0, 1),
            Background = BrushOf(0xFFFDF6),
            BorderBrush = BrushOf(0xDDD3BF),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
            Child = new TextBlock
            {
                Text = "打开下载页",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x5C4D3E)
            }
        };
        openReleaseBtn.MouseEnter += delegate { openReleaseBtn.Background = BrushOf(0xFFF3D8); };
        openReleaseBtn.MouseLeave += delegate { openReleaseBtn.Background = BrushOf(0xFFFDF6); };
        openReleaseBtn.MouseLeftButtonUp += delegate { UpdateManager.OpenReleasePage(lastReleaseUrl); };
        updateActions.Children.Add(openReleaseBtn);

        updateBtnText = new TextBlock
        {
            Text = "检测更新",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushOf(0x5A3A0E),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        updateBtn = new Border
        {
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14, 7, 14, 7),
            Margin = new Thickness(8, 1, 0, 1),
            Background = BrushOf(0xFFF3CE),
            BorderBrush = BrushOf(0xE6B95A),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Child = updateBtnText
        };
        updateBtn.MouseEnter += delegate
        {
            if (updateBusy) return;
            updateBtn.Background = (availableUpdate != null && availableUpdate.HasUpdate)
                ? (Brush)Gradient(0xFFD572, 0xF3A623, 0, 0, 0, 1)
                : BrushOf(0xFFE9AE);
        };
        updateBtn.MouseLeave += delegate { RefreshUpdateButtonVisual(); };
        updateBtn.MouseLeftButtonUp += delegate { HandleUpdateButtonClick(); };
        updateActions.Children.Add(updateBtn);

        DockPanel.SetDock(updateActions, Dock.Right);
        updateRow.Children.Add(updateActions);

        StackPanel about = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        StackPanel verLine = new StackPanel { Orientation = Orientation.Horizontal };
        verLine.Children.Add(new TextBlock
        {
            Text = "键盘小黑猫 v" + UpdateManager.CurrentVersionText,
            FontSize = 12.5,
            FontWeight = FontWeights.Bold,
            Foreground = BrushOf(0x4B3B2B),
            VerticalAlignment = VerticalAlignment.Center
        });
        verLine.Children.Add(new Border
        {
            Background = BrushOf(0xEDE4D1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(7, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "Gitee 优先 · GitHub 备用",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x7A664F)
            }
        });
        about.Children.Add(verLine);

        updateStatusText = new TextBlock
        {
            Text = "优先从 Gitee 镜像获取最新正式版，安装后自动清理临时文件",
            FontSize = 11,
            Foreground = BrushOf(0x857564),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        };
        about.Children.Add(updateStatusText);
        updateRow.Children.Add(about);

        Border aboutCard = MakeSectionCard(updateRow);
        aboutCard.Background = BrushOf(0xF6F0E3);
        aboutCard.Margin = new Thickness(0, 10, 0, 0);
        DockPanel.SetDock(aboutCard, Dock.Bottom);
        page.Children.Add(aboutCard);

        return page;
    }

    private void RefreshUpdateButtonVisual()
    {
        if (updateBtn == null || updateBtnText == null) return;
        if (updateBusy)
        {
            updateBtn.Background = BrushOf(0xEAE2D2);
            updateBtn.BorderBrush = BrushOf(0xD8CEBC);
            updateBtnText.Foreground = BrushOf(0x8C7D6B);
            updateBtn.Cursor = Cursors.Arrow;
            return;
        }
        updateBtn.Cursor = Cursors.Hand;
        if (availableUpdate != null && availableUpdate.HasUpdate)
        {
            updateBtn.Background = Gradient(0xFFCC58, 0xF09E16, 0, 0, 0, 1);
            updateBtn.BorderBrush = BrushOf(0xDC8C10);
            updateBtnText.Foreground = BrushOf(0x472905);
            updateBtnText.Text = "立即更新 v" + availableUpdate.LatestVersion;
        }
        else
        {
            updateBtn.Background = BrushOf(0xFFF3CE);
            updateBtn.BorderBrush = BrushOf(0xE6B95A);
            updateBtnText.Foreground = BrushOf(0x5A3A0E);
            updateBtnText.Text = "检测更新";
        }
    }

    private void HandleUpdateButtonClick()
    {
        if (updateBusy) return;
        if (availableUpdate != null && availableUpdate.HasUpdate)
        {
            StartDownloadAndInstall(availableUpdate);
            return;
        }

        updateBusy = true;
        updateBtnText.Text = "检测中…";
        updateStatusText.Text = "正在连接 Gitee / GitHub 查询最新正式版…";
        if (openReleaseBtn != null) openReleaseBtn.Visibility = Visibility.Collapsed;
        RefreshUpdateButtonVisual();

        ThreadPool.QueueUserWorkItem(delegate
        {
            UpdateManager.CheckResult res = UpdateManager.CheckLatest();
            Dispatcher.BeginInvoke(new Action(delegate
            {
                updateBusy = false;
                lastReleaseUrl = res.ReleaseUrl;
                if (!string.IsNullOrEmpty(res.Error))
                {
                    availableUpdate = null;
                    if (updateNavDot != null) updateNavDot.Visibility = Visibility.Collapsed;
                    updateStatusText.Text = res.Error;
                    if (openReleaseBtn != null) openReleaseBtn.Visibility = Visibility.Visible;
                }
                else if (res.HasUpdate)
                {
                    availableUpdate = res;
                    if (updateNavDot != null) updateNavDot.Visibility = Visibility.Visible;
                    updateStatusText.Text = "已从 " + res.SourceName + " 发现新正式版 v" + res.LatestVersion + "，点击右侧自动升级";
                    if (openReleaseBtn != null) openReleaseBtn.Visibility = Visibility.Collapsed;
                }
                else
                {
                    availableUpdate = null;
                    if (updateNavDot != null) updateNavDot.Visibility = Visibility.Collapsed;
                    updateStatusText.Text = "当前已是最新正式版（" + res.SourceName + " 最新：v" + res.LatestVersion + "）";
                    if (openReleaseBtn != null) openReleaseBtn.Visibility = Visibility.Collapsed;
                }
                RefreshUpdateButtonVisual();
            }));
        });
    }

    private void StartDownloadAndInstall(UpdateManager.CheckResult info)
    {
        updateBusy = true;
        updateBtnText.Text = "下载中…";
        updateStatusText.Text = "正在从 " + info.SourceName + " 下载 v" + info.LatestVersion + "（0%）…";
        if (openReleaseBtn != null) openReleaseBtn.Visibility = Visibility.Collapsed;
        RefreshUpdateButtonVisual();

        ThreadPool.QueueUserWorkItem(delegate
        {
            Action launchUpdater;
            string err = UpdateManager.DownloadAndPrepareInstall(info, delegate(int pct)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (updateBusy) updateStatusText.Text = "正在下载 v" + info.LatestVersion + "（" + pct + "%）…";
                }));
            }, out launchUpdater);

            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (!string.IsNullOrEmpty(err) || launchUpdater == null)
                {
                    updateBusy = false;
                    lastReleaseUrl = info.ReleaseUrl;
                    updateStatusText.Text = string.IsNullOrEmpty(err) ? "更新准备失败，请重试" : err;
                    if (openReleaseBtn != null) openReleaseBtn.Visibility = Visibility.Visible;
                    RefreshUpdateButtonVisual();
                    return;
                }
                updateStatusText.Text = "下载完成，正在自动安装并重启小猫…";
                launchUpdater();
                if (cb.ExitApp != null) cb.ExitApp();
            }));
        });
    }

    private Border MakeActionButton(string text, bool danger, Action click, out TextBlock textBlock)
    {
        textBlock = new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = danger ? BrushOf(0xA84332) : BrushOf(0x4B3C2E),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Brush normalBg = danger ? BrushOf(0xFDF2EE) : BrushOf(0xFAF5E9);
        Brush hoverBg = danger ? BrushOf(0xF9E3DC) : BrushOf(0xFFF3D4);
        Border btn = new Border
        {
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(6, 8, 6, 8),
            Margin = new Thickness(3, 1, 3, 2),
            Background = normalBg,
            BorderBrush = danger ? BrushOf(0xF0CEC3) : BrushOf(0xE2D8C3),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = textBlock
        };
        btn.MouseEnter += delegate { btn.Background = hoverBg; };
        btn.MouseLeave += delegate { btn.Background = normalBg; };
        btn.MouseLeftButtonUp += delegate { click(); };
        return btn;
    }

    private static Image RenderVectorThumb(Enum style, double width, double height)
    {
        DrawingGroup art = new DrawingGroup();
        using (DrawingContext dc = art.Open())
        {
            if (style is KeyboardStyle) DeskArt.DrawKeyboard(dc, (KeyboardStyle)(object)style, null);
            else if (style is CatStyle) CatArt.DrawPortrait(dc, (CatStyle)(object)style);
            else if (style is GamepadStyle) GamepadArt.Draw(dc, (GamepadStyle)(object)style, new GamepadState());
            else MouseArt.Draw(dc, (MouseStyle)(object)style, 0);
        }
        art.Freeze();
        DrawingImage img = new DrawingImage(art);
        img.Freeze();
        return new Image
        {
            Source = img,
            Width = width,
            Height = height,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = false
        };
    }

    private static Image RenderCustomThumb(CatSkinData skin, double width, double height)
    {
        CompiledSkin compiled = new CompiledSkin(skin);
        DrawingGroup art = new DrawingGroup();
        using (DrawingContext dc = art.Open())
        {
            // Keep consistent portrait bounds across short/fluffy bases and accessories.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(-56, -70, 112, 116));
            CustomSkinArt.DrawPortrait(dc, compiled);
        }
        art.Freeze();
        DrawingImage img = new DrawingImage(art);
        img.Freeze();
        return new Image
        {
            Source = img,
            Width = width,
            Height = height,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = false
        };
    }

    private static void UpdateCardGroup(List<SelectableCard> group, object selected)
    {
        for (int i = 0; i < group.Count; ++i)
        {
            bool on = selected != null && Equals(group[i].Value, selected);
            group[i].Root.Background = on ? ActiveItemBg : ItemBg;
            group[i].Root.BorderBrush = on ? ActiveBorder : ItemBorder;
            if (group[i].Dot != null) group[i].Dot.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void UpdateSegmentGroup(List<SegmentButton> group, object selected)
    {
        for (int i = 0; i < group.Count; ++i)
        {
            bool on = Equals(group[i].Value, selected);
            group[i].Root.Background = on ? CardBg : Brushes.Transparent;
            group[i].Root.BorderBrush = on ? BrushOf(0xE8AE36) : Brushes.Transparent;
            group[i].Label.Foreground = on ? TextAmber : BrushOf(0x6F5F4E);
        }
    }

    private static void UpdatePresetGroup(List<SegmentButton> group, int pct)
    {
        for (int i = 0; i < group.Count; ++i)
        {
            bool on = Equals(group[i].Value, pct);
            group[i].Root.Background = on ? BrushOf(0xFFE7A3) : SidebarBg;
            group[i].Root.BorderBrush = on ? BrushOf(0xE7A227) : BrushOf(0xE5DCC9);
            group[i].Label.Foreground = on ? BrushOf(0x5A3809) : BrushOf(0x6D5D4C);
        }
    }

    public void SyncFromState()
    {
        syncing = true;
        try
        {
            string activeCustomId = cb.GetCustomSkinId != null ? (cb.GetCustomSkinId() ?? "") : "";
            if (!string.IsNullOrEmpty(activeCustomId))
            {
                UpdateCardGroup(catCards, null);
                UpdateCardGroup(appearanceCustomCards, activeCustomId);
            }
            else
            {
                if (cb.GetCat != null) UpdateCardGroup(catCards, cb.GetCat());
                UpdateCardGroup(appearanceCustomCards, null);
            }
            RefreshWorkshopActiveMarkers();
            if (cb.GetKeyboard != null) UpdateCardGroup(kbCards, cb.GetKeyboard());
            if (cb.GetMouse != null) UpdateCardGroup(mouseCards, cb.GetMouse());
            if (cb.GetGamepad != null) UpdateCardGroup(gpCards, cb.GetGamepad());
            if (cb.GetGamepadMode != null) UpdateSegmentGroup(gpModeSegs, cb.GetGamepadMode());
            if (cb.GetIdleMinutes != null) UpdateSegmentGroup(idleSegs, cb.GetIdleMinutes());
            if (cb.GetScale != null)
            {
                int pct = Math.Max(Preferences.MinPercent, Math.Min(Preferences.MaxPercent, (int)Math.Round(cb.GetScale() * 100.0)));
                scaleSlider.Value = pct;
                scaleValueText.Text = pct + "%";
                UpdatePresetGroup(scalePresets, pct);
            }
            if (cb.GetPasswordMode != null) passwordToggle.SetValue(cb.GetPasswordMode());
            if (cb.GetLabels != null) labelsToggle.SetValue(cb.GetLabels());
            if (cb.GetAutoCheckUpdate != null) autoCheckToggle.SetValue(cb.GetAutoCheckUpdate());
            if (cb.GetOnTop != null) onTopToggle.SetValue(cb.GetOnTop());
            if (cb.GetClickThrough != null) clickThroughToggle.SetValue(cb.GetClickThrough());
            startupToggle.SetValue(StartupManager.IsEnabled());
            if (cb.GetPetVisible != null && hideBtnText != null)
                hideBtnText.Text = cb.GetPetVisible() ? "暂时隐藏小猫" : "显示小猫";
        }
        finally
        {
            syncing = false;
        }
    }
}
