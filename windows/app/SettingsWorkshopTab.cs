using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

// Pure-vector modular "皮肤工坊" (Skin Workshop) tab inside SettingsWindow.
internal sealed partial class SettingsWindow
{
    private sealed class WorkshopPill
    {
        public Border Root;
        public TextBlock Label;
        public object Value;
    }

    private sealed class ColorPickerRow
    {
        public UIElement Container;
        public List<Border> Swatches = new List<Border>();
        public List<string> SwatchHexes = new List<string>();
        public Border PreviewChip;
        public TextBox HexBox;
        public Func<string> Getter;
        public Action<string> Setter;
    }

    private sealed class WorkshopSkinChip
    {
        public Border Root;
        public Ellipse ActiveDot;
        public string SkinId;
    }

    private List<CatSkinData> savedSkins = new List<CatSkinData>();
    private CatSkinData editingSkin;
    private int workshopPose; // 0=normal, 1=happy, 2=impatient, 3=blink
    private bool workshopSyncing;

    private StackPanel workshopSkinStrip;
    private readonly List<WorkshopSkinChip> workshopSkinChips = new List<WorkshopSkinChip>();
    private Border workshopPreviewHost;
    private readonly List<SegmentButton> poseButtons = new List<SegmentButton>();
    private TextBox skinNameBox;
    private TextBlock workshopStatusText;

    private readonly List<SegmentButton> bodyBaseSegs = new List<SegmentButton>();
    private readonly List<WorkshopPill> facePills = new List<WorkshopPill>();
    private readonly List<WorkshopPill> bellyPills = new List<WorkshopPill>();
    private readonly List<WorkshopPill> bodyPatternPills = new List<WorkshopPill>();
    private readonly List<WorkshopPill> pawPills = new List<WorkshopPill>();
    private readonly List<WorkshopPill> tailPills = new List<WorkshopPill>();
    private readonly List<SegmentButton> pupilSegs = new List<SegmentButton>();
    private readonly List<WorkshopPill> accPills = new List<WorkshopPill>();
    private CreamToggle darkEarTipsToggle;
    private CreamToggle heteroToggle;

    private readonly List<ColorPickerRow> colorPickers = new List<ColorPickerRow>();
    private ColorPickerRow rightEyePicker;
    private ColorPickerRow accColorPicker;

    private UIElement BuildWorkshopTab()
    {
        editingSkin = new CatSkinData
        {
            Id = CatSkinData.NewId(),
            Name = "燕尾小猫"
        };

        DockPanel root = new DockPanel { LastChildFill = true, Height = 496 };

        // 1. Top bar: saved skins strip + quick actions (New / Import / Export / Delete)
        DockPanel topRow = new DockPanel { LastChildFill = true };
        StackPanel actBtns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        actBtns.Children.Add(MakeMiniToolButton("新建", false, CreateNewSkinDraft));
        actBtns.Children.Add(MakeMiniToolButton("导入", false, ImportSkinFromDisk));
        actBtns.Children.Add(MakeMiniToolButton("导出", false, ExportCurrentSkin));
        actBtns.Children.Add(MakeMiniToolButton("删除", true, DeleteEditingSkin));
        DockPanel.SetDock(actBtns, Dock.Right);
        topRow.Children.Add(actBtns);

        workshopSkinStrip = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        ScrollViewer stripScroll = new ScrollViewer
        {
            Height = 32,
            Margin = new Thickness(0, 0, 8, 0),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = workshopSkinStrip
        };
        topRow.Children.Add(stripScroll);

        Border topCard = MakeSectionCard(topRow, new Thickness(8, 6, 8, 6));
        topCard.Margin = new Thickness(0, 0, 0, 8);
        DockPanel.SetDock(topCard, Dock.Top);
        root.Children.Add(topCard);

        // 2. Main split: Left live preview + name + templates + Save CTA | Right scrollable editor
        Grid main = new Grid();
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(178) });
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        UIElement leftCol = BuildWorkshopLeftColumn();
        Grid.SetColumn(leftCol, 0);
        main.Children.Add(leftCol);

        UIElement rightCol = BuildWorkshopRightColumn();
        Grid.SetColumn(rightCol, 2);
        main.Children.Add(rightCol);

        root.Children.Add(main);
        return root;
    }

    private UIElement BuildWorkshopLeftColumn()
    {
        DockPanel left = new DockPanel { LastChildFill = false };

        // Live preview card (top)
        StackPanel stageStack = new StackPanel();
        workshopPreviewHost = new Border
        {
            Height = 158,
            CornerRadius = new CornerRadius(10),
            Background = Gradient(0xFFFDF8, 0xF4EBD7, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xE8DFCD),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };
        stageStack.Children.Add(workshopPreviewHost);

        Border poseBar = MakeSegmentBar(
            poseButtons,
            new[] { "正常", "开心", "不耐烦", "闭眼" },
            new object[] { 0, 1, 2, 3 },
            delegate(object v)
            {
                workshopPose = (int)v;
                UpdateSegmentGroup(poseButtons, workshopPose);
                RefreshWorkshopPreview();
            });
        poseBar.Margin = new Thickness(0, 7, 0, 0);
        for (int i = 0; i < poseButtons.Count; ++i)
        {
            poseButtons[i].Label.FontSize = 10.5;
            poseButtons[i].Root.Padding = new Thickness(2, 4, 2, 4);
        }
        stageStack.Children.Add(poseBar);

        Border stageCard = MakeSectionCard(stageStack, new Thickness(8, 8, 8, 8));
        stageCard.Margin = new Thickness(0, 0, 0, 8);
        DockPanel.SetDock(stageCard, Dock.Top);
        left.Children.Add(stageCard);

        // Name & built-in remix templates (middle)
        StackPanel meta = new StackPanel();
        meta.Children.Add(new TextBlock
        {
            Text = "皮肤名称",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = TextPrimary,
            Margin = new Thickness(0, 0, 0, 4)
        });
        skinNameBox = new TextBox
        {
            MaxLength = 16,
            Height = 26,
            FontSize = 11.5,
            Padding = new Thickness(6, 3, 6, 3),
            Foreground = TextPrimary,
            Background = BrushOf(0xFFFDF9),
            BorderBrush = BrushOf(0xDFD5C0),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        skinNameBox.TextChanged += delegate
        {
            if (workshopSyncing || editingSkin == null) return;
            editingSkin.Name = skinNameBox.Text;
        };
        meta.Children.Add(skinNameBox);

        meta.Children.Add(new TextBlock
        {
            Text = "从内置小猫复制配色",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSecondary,
            Margin = new Thickness(0, 8, 0, 4)
        });
        WrapPanel tplWrap = new WrapPanel();
        var templates = new[]
        {
            new { Style = CatStyle.Black, Label = "黑猫" },
            new { Style = CatStyle.Wednesday, Label = "星期三" },
            new { Style = CatStyle.Ginger, Label = "橘猫" },
            new { Style = CatStyle.Coffee, Label = "咖啡" },
            new { Style = CatStyle.SilverShaded, Label = "银渐层" },
            new { Style = CatStyle.Siamese, Label = "暹罗" },
            new { Style = CatStyle.JapaneseBobtail, Label = "三花" },
            new { Style = CatStyle.PhotoTabby, Label = "虎斑" }
        };
        for (int i = 0; i < templates.Length; ++i)
        {
            CatStyle st = templates[i].Style;
            string lbl = templates[i].Label;
            Border chip = new Border
            {
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(6, 2.5, 6, 2.5),
                Margin = new Thickness(0, 0, 4, 4),
                Background = ItemBg,
                BorderBrush = ItemBorder,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Child = new TextBlock
                {
                    Text = lbl,
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushOf(0x5A4A3A)
                }
            };
            chip.MouseEnter += delegate { chip.Background = BrushOf(0xFFF2CC); };
            chip.MouseLeave += delegate { chip.Background = ItemBg; };
            chip.MouseLeftButtonUp += delegate
            {
                string keepId = editingSkin != null ? editingSkin.Id : CatSkinData.NewId();
                editingSkin = CatSkinData.FromBuiltIn(st, lbl + "二创");
                editingSkin.Id = keepId;
                SyncWorkshopControlsFromModel();
                SetWorkshopStatus("已载入「" + lbl + "」底色模板");
            };
            tplWrap.Children.Add(chip);
        }
        meta.Children.Add(tplWrap);

        Border metaCard = MakeSectionCard(meta, new Thickness(9, 8, 9, 6));
        metaCard.Margin = new Thickness(0, 0, 0, 8);
        DockPanel.SetDock(metaCard, Dock.Top);
        left.Children.Add(metaCard);

        // Save & Apply CTA anchored at bottom
        StackPanel ctaWrap = new StackPanel();
        workshopStatusText = new TextBlock
        {
            Text = "实时预览 · 点击下方应用",
            FontSize = 10.5,
            Foreground = BrushOf(0x8A5B14),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 0, 5)
        };
        ctaWrap.Children.Add(workshopStatusText);

        Border applyBtn = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8),
            Background = Gradient(0xFFCC58, 0xF09E16, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xDC8C10),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = "保存并应用到桌面",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = BrushOf(0x472905),
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        applyBtn.MouseEnter += delegate { applyBtn.Background = Gradient(0xFFD674, 0xF5A824, 0, 0, 0, 1); };
        applyBtn.MouseLeave += delegate { applyBtn.Background = Gradient(0xFFCC58, 0xF09E16, 0, 0, 0, 1); };
        applyBtn.MouseLeftButtonUp += delegate { SaveAndApplyEditingSkin(); };
        ctaWrap.Children.Add(applyBtn);

        DockPanel.SetDock(ctaWrap, Dock.Bottom);
        left.Children.Add(ctaWrap);

        return left;
    }

    private UIElement BuildWorkshopRightColumn()
    {
        StackPanel editor = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };

        // Section 1: Body base & markings
        StackPanel shapeSec = new StackPanel();
        shapeSec.Children.Add(MakeSectionHeader("体型与花纹", "纯矢量高清组合"));

        shapeSec.Children.Add(MakeSubLabel("基础体型"));
        shapeSec.Children.Add(MakeSegmentBar(
            bodyBaseSegs,
            new[] { "短毛圆润", "长毛蓬松" },
            new object[] { SkinBodyBase.ShortHair, SkinBodyBase.FluffyLong },
            delegate(object v)
            {
                editingSkin.BodyBase = (SkinBodyBase)v;
                UpdateSegmentGroup(bodyBaseSegs, editingSkin.BodyBase);
                RefreshWorkshopPreview();
            }));

        shapeSec.Children.Add(MakeSubLabel("脸部花纹"));
        shapeSec.Children.Add(MakePillWrap(
            facePills,
            new[] { "无", "咖啡面具", "虎斑M", "暹罗重点", "燕尾服", "三花斑", "奶牛斑" },
            new object[]
            {
                SkinFacePattern.None, SkinFacePattern.CoffeeMask, SkinFacePattern.TabbyM,
                SkinFacePattern.SiamesePoint, SkinFacePattern.Tuxedo, SkinFacePattern.Calico, SkinFacePattern.CowSpots
            },
            delegate(object v)
            {
                editingSkin.FacePattern = (SkinFacePattern)v;
                UpdatePillGroup(facePills, editingSkin.FacePattern);
                RefreshWorkshopPreview();
            }));

        shapeSec.Children.Add(MakeSubLabel("胸腹毛色"));
        shapeSec.Children.Add(MakePillWrap(
            bellyPills,
            new[] { "无", "小围兜", "大白肚", "长毛围脖" },
            new object[] { SkinBellyStyle.None, SkinBellyStyle.Bib, SkinBellyStyle.FullBelly, SkinBellyStyle.Ruff },
            delegate(object v)
            {
                editingSkin.BellyStyle = (SkinBellyStyle)v;
                UpdatePillGroup(bellyPills, editingSkin.BellyStyle);
                RefreshWorkshopPreview();
            }));

        shapeSec.Children.Add(MakeSubLabel("身体纹路"));
        shapeSec.Children.Add(MakePillWrap(
            bodyPatternPills,
            new[] { "纯色", "侧肋条纹", "花斑块" },
            new object[] { SkinBodyPattern.None, SkinBodyPattern.FlankStripes, SkinBodyPattern.Patches },
            delegate(object v)
            {
                editingSkin.BodyPattern = (SkinBodyPattern)v;
                UpdatePillGroup(bodyPatternPills, editingSkin.BodyPattern);
                RefreshWorkshopPreview();
            }));

        shapeSec.Children.Add(MakeSubLabel("前爪与尾巴"));
        shapeSec.Children.Add(MakePillWrap(
            pawPills,
            new[] { "爪同体色", "白手套", "深色重点爪" },
            new object[] { SkinPawStyle.BodyColor, SkinPawStyle.Socks, SkinPawStyle.DarkPoints },
            delegate(object v)
            {
                editingSkin.PawStyle = (SkinPawStyle)v;
                UpdatePillGroup(pawPills, editingSkin.PawStyle);
                RefreshWorkshopPreview();
            }));
        shapeSec.Children.Add(MakePillWrap(
            tailPills,
            new[] { "纯色尾", "虎斑环纹尾", "浅色尾尖" },
            new object[] { SkinTailStyle.Solid, SkinTailStyle.TabbyRings, SkinTailStyle.LightTip },
            delegate(object v)
            {
                editingSkin.TailStyle = (SkinTailStyle)v;
                UpdatePillGroup(tailPills, editingSkin.TailStyle);
                RefreshWorkshopPreview();
            }));

        darkEarTipsToggle = new CreamToggle(delegate(bool on)
        {
            if (workshopSyncing) return;
            editingSkin.DarkEarTips = on;
            RefreshWorkshopPreview();
        });
        DockPanel earTipRow = MakeCompactToggleRow("深色耳尖", darkEarTipsToggle);
        earTipRow.Margin = new Thickness(0, 6, 0, 0);
        shapeSec.Children.Add(earTipRow);
        editor.Children.Add(MakeSectionCard(shapeSec, new Thickness(11, 9, 11, 9)));

        // Section 2: Coat colors
        StackPanel coatSec = new StackPanel();
        coatSec.Children.Add(MakeSectionHeader("毛色配色", "点击色板或输入 #RRGGBB"));
        coatSec.Children.Add(AddColorPicker(
            "主毛色",
            new[] { "#242226", "#3E3A40", "#F0A860", "#BD8952", "#D5DAE0", "#FAF4E8", "#E6D5BE", "#B898A4" },
            delegate { return editingSkin.FurColor; },
            delegate(string h) { editingSkin.FurColor = h; }));
        coatSec.Children.Add(AddColorPicker(
            "斑纹 / 重点色",
            new[] { "#4C3530", "#634328", "#C26F38", "#7A6048", "#8E95A0", "#2C2A30", "#E59876", "#8A5E44" },
            delegate { return editingSkin.AccentColor; },
            delegate(string h) { editingSkin.AccentColor = h; }));
        coatSec.Children.Add(AddColorPicker(
            "胸腹 / 面罩色",
            new[] { "#FFFDF8", "#FFF5E4", "#F2E4CE", "#FFE6C4", "#F4F6F9", "#F8D8B0", "#DFDFDF", "#2D2B30" },
            delegate { return editingSkin.BellyColor; },
            delegate(string h) { editingSkin.BellyColor = h; }));
        coatSec.Children.Add(AddColorPicker(
            "爪套色",
            new[] { "#FFFDF8", "#FFE8C8", "#F5F7FA", "#4E3732", "#B2B6BE", "#E8B884", "#29272C", "#F4C8C6" },
            delegate { return editingSkin.PawColor; },
            delegate(string h) { editingSkin.PawColor = h; }));
        coatSec.Children.Add(AddColorPicker(
            "内耳色",
            new[] { "#F0A89E", "#F5BDB6", "#ECC8A4", "#E29596", "#85605A", "#4A434A", "#D9A89C", "#F6D0C6" },
            delegate { return editingSkin.InnerEarColor; },
            delegate(string h) { editingSkin.InnerEarColor = h; }));
        coatSec.Children.Add(AddColorPicker(
            "鼻头色",
            new[] { "#E68E92", "#EC989B", "#DF8480", "#C97A7E", "#443C42", "#3B2727", "#D47278", "#9E6A68" },
            delegate { return editingSkin.NoseColor; },
            delegate(string h) { editingSkin.NoseColor = h; }));
        coatSec.Children.Add(AddColorPicker(
            "胡须色",
            new[] { "#FCF8F2", "#ECE6DC", "#D4CFC8", "#9CA2A8", "#926B4D", "#6E655E", "#555054", "#383438" },
            delegate { return editingSkin.WhiskerColor; },
            delegate(string h) { editingSkin.WhiskerColor = h; }));
        editor.Children.Add(MakeSectionCard(coatSec, new Thickness(11, 9, 11, 9)));

        // Section 3: Eyes & Heterochromia
        StackPanel eyeSec = new StackPanel();
        eyeSec.Children.Add(MakeSectionHeader("眼睛定制", "支持异瞳与瞳孔形态"));
        eyeSec.Children.Add(MakeSubLabel("瞳孔样式"));
        eyeSec.Children.Add(MakeSegmentBar(
            pupilSegs,
            new[] { "修长椭圆", "圆润呆萌" },
            new object[] { SkinPupilStyle.SleekOval, SkinPupilStyle.RoundCute },
            delegate(object v)
            {
                editingSkin.PupilStyle = (SkinPupilStyle)v;
                UpdateSegmentGroup(pupilSegs, editingSkin.PupilStyle);
                RefreshWorkshopPreview();
            }));

        UIElement leftEyeEl = AddColorPicker(
            "左眼虹膜",
            new[] { "#F2B632", "#6CC07B", "#C9EC4A", "#76BDE8", "#B8A85C", "#DE853E", "#9B86D6", "#E28496" },
            delegate { return editingSkin.LeftEyeColor; },
            delegate(string h)
            {
                editingSkin.LeftEyeColor = h;
                if (!editingSkin.Heterochromia) editingSkin.RightEyeColor = h;
            });
        eyeSec.Children.Add(leftEyeEl);

        heteroToggle = new CreamToggle(delegate(bool on)
        {
            if (workshopSyncing) return;
            editingSkin.Heterochromia = on;
            if (!on) editingSkin.RightEyeColor = editingSkin.LeftEyeColor;
            UpdateDynamicPickerVisibility();
            RefreshWorkshopPreview();
        });
        DockPanel heteroRow = MakeCompactToggleRow("开启异瞳（左右不同眼色）", heteroToggle);
        heteroRow.Margin = new Thickness(0, 6, 0, 2);
        eyeSec.Children.Add(heteroRow);

        UIElement rightEyeEl = AddColorPicker(
            "右眼虹膜",
            new[] { "#76BDE8", "#F2B632", "#6CC07B", "#C9EC4A", "#DE853E", "#9B86D6", "#E28496", "#58C8C4" },
            delegate { return editingSkin.RightEyeColor; },
            delegate(string h) { editingSkin.RightEyeColor = h; },
            out rightEyePicker);
        eyeSec.Children.Add(rightEyeEl);
        editor.Children.Add(MakeSectionCard(eyeSec, new Thickness(11, 9, 11, 9)));

        // Section 4: Cute Vector Accessories
        StackPanel accSec = new StackPanel();
        accSec.Children.Add(MakeSectionHeader("可爱配饰", "随头部动作自然跟随"));
        accSec.Children.Add(MakePillWrap(
            accPills,
            new[] { "无配饰", "铃铛项圈", "蝴蝶结", "头顶发带", "小圆眼镜", "头顶橘子", "呆毛" },
            new object[]
            {
                SkinAccessory.None, SkinAccessory.CollarBell, SkinAccessory.BowTie,
                SkinAccessory.HeadRibbon, SkinAccessory.Glasses, SkinAccessory.HeadOrange, SkinAccessory.Ahoge
            },
            delegate(object v)
            {
                editingSkin.Accessory = (SkinAccessory)v;
                UpdatePillGroup(accPills, editingSkin.Accessory);
                UpdateDynamicPickerVisibility();
                RefreshWorkshopPreview();
            }));

        UIElement accColorEl = AddColorPicker(
            "配饰颜色",
            new[] { "#E46652", "#EC8298", "#5B9AD6", "#5CC4A0", "#F59824", "#9A80D6", "#6D543D", "#E8C466" },
            delegate { return editingSkin.AccessoryColor; },
            delegate(string h) { editingSkin.AccessoryColor = h; },
            out accColorPicker);
        accSec.Children.Add(accColorEl);

        Border accCard = MakeSectionCard(accSec, new Thickness(11, 9, 11, 9));
        accCard.Margin = new Thickness(0);
        editor.Children.Add(accCard);

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = editor
        };
    }

    private static TextBlock MakeSubLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSecondary,
            Margin = new Thickness(0, 6, 0, 4)
        };
    }

    private static DockPanel MakeCompactToggleRow(string label, CreamToggle toggle)
    {
        DockPanel row = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(toggle.Track, Dock.Right);
        row.Children.Add(toggle.Track);
        row.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        });
        return row;
    }

    private Border MakeMiniToolButton(string text, bool danger, Action click)
    {
        Brush normalBg = danger ? BrushOf(0xFDF2EE) : BrushOf(0xFAF5E9);
        Brush hoverBg = danger ? BrushOf(0xF9E3DC) : BrushOf(0xFFF2CC);
        Border btn = new Border
        {
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(7, 4, 7, 4),
            Margin = new Thickness(4, 0, 0, 0),
            Background = normalBg,
            BorderBrush = danger ? BrushOf(0xF0CEC3) : BrushOf(0xE2D8C3),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = danger ? BrushOf(0xA84332) : BrushOf(0x524232)
            }
        };
        btn.MouseEnter += delegate { btn.Background = hoverBg; };
        btn.MouseLeave += delegate { btn.Background = normalBg; };
        btn.MouseLeftButtonUp += delegate { click(); };
        return btn;
    }

    private WrapPanel MakePillWrap(List<WorkshopPill> group, string[] labels, object[] values, Action<object> onPick)
    {
        WrapPanel wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
        for (int i = 0; i < labels.Length; ++i)
        {
            object val = values[i];
            TextBlock tb = new TextBlock
            {
                Text = labels[i],
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x6D5D4C)
            };
            Border b = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 3.5, 8, 3.5),
                Margin = new Thickness(0, 0, 5, 5),
                Background = ItemBg,
                BorderBrush = ItemBorder,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Child = tb
            };
            b.MouseLeftButtonUp += delegate { onPick(val); };
            group.Add(new WorkshopPill { Root = b, Label = tb, Value = val });
            wrap.Children.Add(b);
        }
        return wrap;
    }

    private static void UpdatePillGroup(List<WorkshopPill> group, object selected)
    {
        for (int i = 0; i < group.Count; ++i)
        {
            bool on = Equals(group[i].Value, selected);
            group[i].Root.Background = on ? ActiveItemBg : ItemBg;
            group[i].Root.BorderBrush = on ? ActiveBorder : ItemBorder;
            group[i].Label.Foreground = on ? TextAmber : BrushOf(0x6D5D4C);
        }
    }

    private UIElement AddColorPicker(string label, string[] swatches, Func<string> getter, Action<string> setter)
    {
        ColorPickerRow dummy;
        return AddColorPicker(label, swatches, getter, setter, out dummy);
    }

    private UIElement AddColorPicker(string label, string[] swatches, Func<string> getter, Action<string> setter, out ColorPickerRow picker)
    {
        picker = new ColorPickerRow { Getter = getter, Setter = setter };
        StackPanel wrap = new StackPanel { Margin = new Thickness(0, 5, 0, 2) };

        DockPanel topLine = new DockPanel { LastChildFill = false, Margin = new Thickness(0, 0, 0, 4) };
        TextBlock lbl = new TextBlock
        {
            Text = label,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSecondary,
            VerticalAlignment = VerticalAlignment.Center
        };
        DockPanel.SetDock(lbl, Dock.Left);
        topLine.Children.Add(lbl);

        StackPanel hexPart = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        Border chip = new Border
        {
            Width = 14,
            Height = 14,
            CornerRadius = new CornerRadius(4),
            BorderBrush = BrushOf(0xC8BDA8),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 5, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        TextBox hexBox = new TextBox
        {
            Width = 66,
            Height = 20,
            MaxLength = 7,
            FontSize = 10.5,
            FontFamily = new FontFamily("Consolas, Segoe UI"),
            Padding = new Thickness(4, 1, 4, 1),
            Foreground = TextPrimary,
            Background = BrushOf(0xFFFDF9),
            BorderBrush = BrushOf(0xDFD5C0),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        hexPart.Children.Add(chip);
        hexPart.Children.Add(hexBox);
        DockPanel.SetDock(hexPart, Dock.Right);
        topLine.Children.Add(hexPart);
        wrap.Children.Add(topLine);

        StackPanel swatchRow = new StackPanel { Orientation = Orientation.Horizontal };
        ColorPickerRow localPicker = picker;
        for (int i = 0; i < swatches.Length; ++i)
        {
            string hex = swatches[i];
            Color c = CatSkinData.ParseHex(hex, Colors.Gray);
            SolidColorBrush fill = new SolidColorBrush(c);
            fill.Freeze();
            Border sw = new Border
            {
                Width = 22,
                Height = 20,
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 5, 0),
                Background = fill,
                BorderBrush = BrushOf(0xCFC4AE),
                BorderThickness = new Thickness(1.2),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };
            sw.MouseLeftButtonUp += delegate
            {
                localPicker.Setter(hex);
                UpdateColorPickerVisual(localPicker, hex);
                RefreshWorkshopPreview();
            };
            picker.Swatches.Add(sw);
            picker.SwatchHexes.Add(hex);
            swatchRow.Children.Add(sw);
        }
        wrap.Children.Add(swatchRow);

        hexBox.TextChanged += delegate
        {
            if (workshopSyncing) return;
            string raw = (hexBox.Text ?? "").Trim();
            string s = raw.StartsWith("#") ? raw.Substring(1) : raw;
            if (s.Length != 6) return;
            string norm = CatSkinData.NormalizeHex(raw, localPicker.Getter());
            localPicker.Setter(norm);
            UpdateColorPickerChipAndSwatches(localPicker, norm);
            RefreshWorkshopPreview();
        };
        hexBox.LostFocus += delegate
        {
            if (workshopSyncing) return;
            string norm = CatSkinData.NormalizeHex(hexBox.Text, localPicker.Getter());
            localPicker.Setter(norm);
            UpdateColorPickerVisual(localPicker, norm);
            RefreshWorkshopPreview();
        };

        picker.Container = wrap;
        picker.PreviewChip = chip;
        picker.HexBox = hexBox;
        colorPickers.Add(picker);
        return wrap;
    }

    private void UpdateColorPickerVisual(ColorPickerRow picker, string hex)
    {
        bool prev = workshopSyncing;
        workshopSyncing = true;
        try
        {
            picker.HexBox.Text = hex;
            UpdateColorPickerChipAndSwatches(picker, hex);
        }
        finally
        {
            workshopSyncing = prev;
        }
    }

    private static void UpdateColorPickerChipAndSwatches(ColorPickerRow picker, string hex)
    {
        Color c = CatSkinData.ParseHex(hex, Colors.Gray);
        SolidColorBrush chipBrush = new SolidColorBrush(c);
        chipBrush.Freeze();
        picker.PreviewChip.Background = chipBrush;
        for (int i = 0; i < picker.Swatches.Count; ++i)
        {
            bool match = string.Equals(picker.SwatchHexes[i], hex, StringComparison.OrdinalIgnoreCase);
            picker.Swatches[i].BorderBrush = match ? ActiveBorder : BrushOf(0xCFC4AE);
            picker.Swatches[i].BorderThickness = new Thickness(match ? 2.2 : 1.2);
        }
    }

    private void UpdateDynamicPickerVisibility()
    {
        if (rightEyePicker != null && rightEyePicker.Container != null)
        {
            rightEyePicker.Container.Visibility = editingSkin.Heterochromia ? Visibility.Visible : Visibility.Collapsed;
        }
        if (accColorPicker != null && accColorPicker.Container != null)
        {
            bool needsColor = editingSkin.Accessory != SkinAccessory.None && editingSkin.Accessory != SkinAccessory.Ahoge;
            accColorPicker.Container.Visibility = needsColor ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void SyncWorkshopControlsFromModel()
    {
        if (editingSkin == null) return;
        workshopSyncing = true;
        try
        {
            if (skinNameBox != null) skinNameBox.Text = editingSkin.Name ?? "";
            UpdateSegmentGroup(poseButtons, workshopPose);
            UpdateSegmentGroup(bodyBaseSegs, editingSkin.BodyBase);
            UpdatePillGroup(facePills, editingSkin.FacePattern);
            UpdatePillGroup(bellyPills, editingSkin.BellyStyle);
            UpdatePillGroup(bodyPatternPills, editingSkin.BodyPattern);
            UpdatePillGroup(pawPills, editingSkin.PawStyle);
            UpdatePillGroup(tailPills, editingSkin.TailStyle);
            UpdateSegmentGroup(pupilSegs, editingSkin.PupilStyle);
            UpdatePillGroup(accPills, editingSkin.Accessory);
            if (darkEarTipsToggle != null) darkEarTipsToggle.SetValue(editingSkin.DarkEarTips);
            if (heteroToggle != null) heteroToggle.SetValue(editingSkin.Heterochromia);
            for (int i = 0; i < colorPickers.Count; ++i)
            {
                UpdateColorPickerVisual(colorPickers[i], colorPickers[i].Getter());
            }
            UpdateDynamicPickerVisibility();
            RefreshWorkshopActiveMarkers();
        }
        finally
        {
            workshopSyncing = false;
        }
        RefreshWorkshopPreview();
    }

    private void RefreshWorkshopPreview()
    {
        if (workshopPreviewHost == null || editingSkin == null) return;
        CompiledSkin compiled = new CompiledSkin(editingSkin);
        CatMood mood = default(CatMood);
        double blink = 0;
        double gx = 0.08, gy = 0.04;
        if (workshopPose == 1)
        {
            mood.HappyPet = 1.0;
            mood.PetNuzzleX = 0.35;
            mood.PetNuzzleY = -0.15;
        }
        else if (workshopPose == 2)
        {
            mood.Impatient = 1.0;
            gx = -0.22;
            gy = 0.14;
        }
        else if (workshopPose == 3)
        {
            blink = 1.0;
        }

        DrawingGroup art = new DrawingGroup();
        using (DrawingContext dc = art.Open())
        {
            // Keep consistent full-cat bounds across ShortHair (right tail) and FluffyLong (left tail).
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(102, 14, 238, 232));
            CustomSkinArt.Draw(
                dc, compiled, 0.85, gx, gy, blink,
                new Point(176, 224), new Point(252, 224), 0, 0, -1, null, mood);
            if (workshopPose == 1)
            {
                CatArt.DrawPetParticle(dc, true, 156, 46, 1.0, -12, 0.92);
                CatArt.DrawPetParticle(dc, false, 272, 54, 0.95, 14, 0.90);
                CatArt.DrawPetParticle(dc, true, 258, 30, 0.72, 8, 0.82);
            }
        }
        art.Freeze();
        DrawingImage img = new DrawingImage(art);
        img.Freeze();
        workshopPreviewHost.Child = new Image
        {
            Source = img,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = false
        };
    }

    private void RefreshSavedSkinLists()
    {
        savedSkins = CustomSkinStore.LoadAll();
        string activeCustomId = cb.GetCustomSkinId != null ? (cb.GetCustomSkinId() ?? "") : "";

        // Pick an initial skin to edit if editingSkin has not been loaded from disk yet.
        if (savedSkins.Count > 0)
        {
            CatSkinData match = null;
            string wanted = !string.IsNullOrEmpty(activeCustomId) ? activeCustomId : (editingSkin != null ? editingSkin.Id : "");
            for (int i = 0; i < savedSkins.Count; ++i)
            {
                if (string.Equals(savedSkins[i].Id, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    match = savedSkins[i];
                    break;
                }
            }
            if (match != null) editingSkin = match.Clone();
            else if (editingSkin == null) editingSkin = savedSkins[0].Clone();
        }

        // 1. Rebuild Appearance tab quick custom skin strip
        if (appearanceCustomStrip != null)
        {
            appearanceCustomStrip.Children.Clear();
            appearanceCustomCards.Clear();
            if (savedSkins.Count == 0)
            {
                Border emptyHint = new Border
                {
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 6, 10, 6),
                    Background = BrushOf(0xF7F1E3),
                    BorderBrush = BrushOf(0xE8DEC9),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Child = new TextBlock
                    {
                        Text = "还没有自定义皮肤，点击「打开皮肤工坊」亲手搭配一只专属小猫",
                        FontSize = 11,
                        Foreground = TextSecondary
                    }
                };
                emptyHint.MouseLeftButtonUp += delegate { SelectTab(1); };
                appearanceCustomStrip.Children.Add(emptyHint);
            }
            else
            {
                for (int i = 0; i < savedSkins.Count; ++i)
                {
                    CatSkinData skin = savedSkins[i];
                    StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    row.Children.Add(RenderCustomThumb(skin, 26, 24));
                    row.Children.Add(new TextBlock
                    {
                        Text = skin.Name,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = BrushOf(0x4E3F30),
                        Margin = new Thickness(5, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    Ellipse dot = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = HoneyDot,
                        VerticalAlignment = VerticalAlignment.Center,
                        Visibility = Visibility.Collapsed
                    };
                    row.Children.Add(dot);

                    Border card = new Border
                    {
                        Background = ItemBg,
                        BorderBrush = ItemBorder,
                        BorderThickness = new Thickness(1.5),
                        CornerRadius = new CornerRadius(9),
                        Padding = new Thickness(6, 4, 8, 4),
                        Margin = new Thickness(0, 0, 6, 0),
                        Cursor = Cursors.Hand,
                        Child = row
                    };
                    CatSkinData picked = skin;
                    card.MouseLeftButtonUp += delegate
                    {
                        if (cb.ApplyCustomSkin != null) cb.ApplyCustomSkin(picked);
                        UpdateCardGroup(catCards, null);
                        UpdateCardGroup(appearanceCustomCards, picked.Id);
                        editingSkin = picked.Clone();
                        SyncWorkshopControlsFromModel();
                    };
                    appearanceCustomCards.Add(new SelectableCard { Root = card, Dot = dot, Value = skin.Id });
                    appearanceCustomStrip.Children.Add(card);
                }
                UpdateCardGroup(appearanceCustomCards, string.IsNullOrEmpty(activeCustomId) ? null : (object)activeCustomId);
            }
        }

        // 2. Rebuild Workshop top saved skins strip
        if (workshopSkinStrip != null)
        {
            workshopSkinStrip.Children.Clear();
            workshopSkinChips.Clear();
            if (savedSkins.Count == 0)
            {
                workshopSkinStrip.Children.Add(new TextBlock
                {
                    Text = "当前为新皮肤草稿（保存后将出现在此处）",
                    FontSize = 11,
                    Foreground = TextSecondary,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0)
                });
            }
            else
            {
                for (int i = 0; i < savedSkins.Count; ++i)
                {
                    CatSkinData skin = savedSkins[i];
                    StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                    row.Children.Add(RenderCustomThumb(skin, 20, 18));
                    row.Children.Add(new TextBlock
                    {
                        Text = skin.Name,
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = BrushOf(0x4E3F30),
                        Margin = new Thickness(4, 0, 4, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    Ellipse dot = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = HoneyDot,
                        VerticalAlignment = VerticalAlignment.Center,
                        Visibility = Visibility.Collapsed
                    };
                    row.Children.Add(dot);

                    Border chip = new Border
                    {
                        Background = ItemBg,
                        BorderBrush = ItemBorder,
                        BorderThickness = new Thickness(1.2),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(6, 3, 7, 3),
                        Margin = new Thickness(0, 0, 5, 0),
                        Cursor = Cursors.Hand,
                        Child = row
                    };
                    CatSkinData picked = skin;
                    chip.MouseLeftButtonUp += delegate
                    {
                        editingSkin = picked.Clone();
                        SyncWorkshopControlsFromModel();
                        SetWorkshopStatus("正在编辑「" + picked.Name + "」");
                    };
                    workshopSkinChips.Add(new WorkshopSkinChip { Root = chip, ActiveDot = dot, SkinId = skin.Id });
                    workshopSkinStrip.Children.Add(chip);
                }
            }
        }

        SyncWorkshopControlsFromModel();
    }

    private void RefreshWorkshopActiveMarkers()
    {
        string activeCustomId = cb.GetCustomSkinId != null ? (cb.GetCustomSkinId() ?? "") : "";
        string editId = editingSkin != null ? (editingSkin.Id ?? "") : "";
        for (int i = 0; i < workshopSkinChips.Count; ++i)
        {
            WorkshopSkinChip chip = workshopSkinChips[i];
            bool isEditing = string.Equals(chip.SkinId, editId, StringComparison.OrdinalIgnoreCase);
            bool isActive = !string.IsNullOrEmpty(activeCustomId) && string.Equals(chip.SkinId, activeCustomId, StringComparison.OrdinalIgnoreCase);
            chip.Root.Background = isEditing ? ActiveItemBg : ItemBg;
            chip.Root.BorderBrush = isEditing ? ActiveBorder : ItemBorder;
            chip.ActiveDot.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void CreateNewSkinDraft()
    {
        editingSkin = new CatSkinData
        {
            Id = CatSkinData.NewId(),
            Name = "我的小猫 " + (savedSkins.Count + 1)
        };
        SyncWorkshopControlsFromModel();
        SetWorkshopStatus("已新建皮肤草稿，完成后点击应用");
    }

    private void SaveAndApplyEditingSkin()
    {
        if (editingSkin == null) return;
        string cleanName = (editingSkin.Name ?? "").Trim();
        if (string.IsNullOrEmpty(cleanName)) cleanName = "我的小猫";
        if (cleanName.Length > 16) cleanName = cleanName.Substring(0, 16);
        editingSkin.Name = cleanName;
        if (string.IsNullOrEmpty(editingSkin.Id)) editingSkin.Id = CatSkinData.NewId();

        if (!CustomSkinStore.Save(editingSkin))
        {
            SetWorkshopStatus("保存失败，请检查写入权限");
            return;
        }
        CatSkinData applied = editingSkin.Clone();
        if (cb.ApplyCustomSkin != null) cb.ApplyCustomSkin(applied);
        RefreshSavedSkinLists();
        SyncFromState();
        SetWorkshopStatus("已保存并应用「" + applied.Name + "」");
    }

    private void ExportCurrentSkin()
    {
        if (editingSkin == null) return;
        string cleanName = (editingSkin.Name ?? "custom-cat").Trim();
        if (string.IsNullOrEmpty(cleanName)) cleanName = "custom-cat";
        char[] bad = System.IO.Path.GetInvalidFileNameChars();
        for (int i = 0; i < bad.Length; ++i) cleanName = cleanName.Replace(bad[i], '_');

        SaveFileDialog dlg = new SaveFileDialog
        {
            Title = "导出自定义小猫皮肤",
            Filter = "小猫皮肤文件 (*.catskin)|*.catskin",
            DefaultExt = ".catskin",
            FileName = cleanName + ".catskin"
        };
        bool? ok = dlg.ShowDialog(this);
        if (ok == true)
        {
            if (CustomSkinStore.ExportToFile(editingSkin, dlg.FileName))
                SetWorkshopStatus("已导出 .catskin 分享文件");
            else
                SetWorkshopStatus("导出失败，请重试");
        }
    }

    private void ImportSkinFromDisk()
    {
        OpenFileDialog dlg = new OpenFileDialog
        {
            Title = "导入自定义小猫皮肤",
            Filter = "小猫皮肤文件 (*.catskin)|*.catskin"
        };
        bool? ok = dlg.ShowDialog(this);
        if (ok == true)
        {
            CatSkinData imported = CustomSkinStore.ImportFromFile(dlg.FileName);
            if (imported == null)
            {
                SetWorkshopStatus("文件格式无效，导入失败");
                return;
            }
            editingSkin = imported.Clone();
            if (cb.ApplyCustomSkin != null) cb.ApplyCustomSkin(imported);
            RefreshSavedSkinLists();
            SyncFromState();
            SetWorkshopStatus("已导入并应用「" + imported.Name + "」");
        }
    }

    private void DeleteEditingSkin()
    {
        if (editingSkin == null) return;
        string targetId = editingSkin.Id;
        bool existed = false;
        for (int i = 0; i < savedSkins.Count; ++i)
        {
            if (string.Equals(savedSkins[i].Id, targetId, StringComparison.OrdinalIgnoreCase))
            {
                existed = true;
                break;
            }
        }
        if (!existed)
        {
            CreateNewSkinDraft();
            SetWorkshopStatus("已重置当前草稿");
            return;
        }

        string activeCustomId = cb.GetCustomSkinId != null ? (cb.GetCustomSkinId() ?? "") : "";
        CustomSkinStore.Delete(targetId);
        if (string.Equals(activeCustomId, targetId, StringComparison.OrdinalIgnoreCase))
        {
            if (cb.ApplyCustomSkin != null) cb.ApplyCustomSkin(null);
        }
        editingSkin = null;
        RefreshSavedSkinLists();
        if (editingSkin == null) CreateNewSkinDraft();
        SyncFromState();
        SetWorkshopStatus("已删除自定义皮肤");
    }

    private void SetWorkshopStatus(string msg)
    {
        if (workshopStatusText != null) workshopStatusText.Text = msg;
    }
}
