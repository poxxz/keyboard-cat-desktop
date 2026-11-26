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

    private readonly List<WorkshopPill> bodyBasePills = new List<WorkshopPill>();
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
        editingSkin = CatSkinData.FromPreset("bluewhite");

        DockPanel root = new DockPanel { LastChildFill = true, VerticalAlignment = VerticalAlignment.Stretch };

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

        // 2. Main split: Left live preview + name + templates + Reset/Save CTA | Right scrollable editor
        Grid main = new Grid { Height = 492, VerticalAlignment = VerticalAlignment.Top };
        main.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(194) });
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

    private Border MakeWeightedPoseBar()
    {
        string[] titles = { "正常", "开心", "不耐烦", "闭眼" };
        double[] weights = { 1.0, 1.0, 1.42, 1.0 };
        Grid grid = new Grid();
        for (int i = 0; i < titles.Length; ++i)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(weights[i], GridUnitType.Star) });
            int val = i;
            TextBlock tb = new TextBlock
            {
                Text = titles[i],
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x6F5F4E),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Border b = new Border
            {
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(3, 4.5, 3, 4.5),
                Margin = new Thickness(1.5),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Child = tb
            };
            b.MouseLeftButtonUp += delegate
            {
                workshopPose = val;
                UpdateSegmentGroup(poseButtons, workshopPose);
                RefreshWorkshopPreview();
            };
            Grid.SetColumn(b, i);
            poseButtons.Add(new SegmentButton { Root = b, Label = tb, Value = val });
            grid.Children.Add(b);
        }
        return new Border
        {
            Background = SidebarBg,
            BorderBrush = BrushOf(0xE5DCC9),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(2),
            Margin = new Thickness(0, 7, 0, 0),
            Child = grid
        };
    }

    private UIElement BuildWorkshopLeftColumn()
    {
        DockPanel left = new DockPanel { LastChildFill = false };

        // Live preview card (top)
        StackPanel stageStack = new StackPanel();
        workshopPreviewHost = new Border
        {
            Height = 160,
            CornerRadius = new CornerRadius(10),
            Background = Gradient(0xFFFDF8, 0xF4EBD7, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xE8DFCD),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };
        stageStack.Children.Add(workshopPreviewHost);
        stageStack.Children.Add(MakeWeightedPoseBar());

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
            Text = "常见猫咪一键预设",
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextSecondary,
            Margin = new Thickness(0, 8, 0, 4)
        });
        WrapPanel tplWrap = new WrapPanel();
        var templates = new[]
        {
            new { Key = "wednesday", Label = "星期三" },
            new { Key = "blue", Label = "蓝猫" },
            new { Key = "bluewhite", Label = "蓝白" },
            new { Key = "amshort", Label = "美短" },
            new { Key = "ragdoll", Label = "布偶" },
            new { Key = "fold", Label = "折耳" },
            new { Key = "ginger", Label = "橘猫" },
            new { Key = "coffee", Label = "咖啡" },
            new { Key = "silver", Label = "银渐层" },
            new { Key = "siamese", Label = "暹罗" },
            new { Key = "calico", Label = "三花" },
            new { Key = "tabby", Label = "虎斑" }
        };
        for (int i = 0; i < templates.Length; ++i)
        {
            string key = templates[i].Key;
            string lbl = templates[i].Label;
            Border chip = new Border
            {
                CornerRadius = new CornerRadius(6.5),
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
                editingSkin = CatSkinData.FromPreset(key);
                editingSkin.Id = keepId;
                SyncWorkshopControlsFromModel();
                SetWorkshopStatus("已载入「" + lbl + "」品种预设");
            };
            tplWrap.Children.Add(chip);
        }
        meta.Children.Add(tplWrap);

        Border metaCard = MakeSectionCard(meta, new Thickness(9, 8, 9, 6));
        metaCard.Margin = new Thickness(0, 0, 0, 8);
        DockPanel.SetDock(metaCard, Dock.Top);
        left.Children.Add(metaCard);

        // Reset + Save & Apply CTA row anchored cleanly at bottom with full bottom border visible
        StackPanel ctaWrap = new StackPanel { Margin = new Thickness(0, 0, 0, 2) };
        workshopStatusText = new TextBlock
        {
            Text = "实时预览 · 点击下方应用",
            FontSize = 10.5,
            Foreground = BrushOf(0x8A5B14),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 0, 6)
        };
        ctaWrap.Children.Add(workshopStatusText);

        Grid ctaGrid = new Grid();
        ctaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
        ctaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        ctaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Border resetBtn = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(6, 8, 6, 8),
            Background = BrushOf(0xFFFDF6),
            BorderBrush = BrushOf(0xDDD3BF),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "恢复到上次保存或默认配色",
            Child = new TextBlock
            {
                Text = "重置",
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushOf(0x5C4D3E),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        resetBtn.MouseEnter += delegate { resetBtn.Background = BrushOf(0xFFF3D8); };
        resetBtn.MouseLeave += delegate { resetBtn.Background = BrushOf(0xFFFDF6); };
        resetBtn.MouseLeftButtonUp += delegate { ResetEditingSkin(); };
        Grid.SetColumn(resetBtn, 0);
        ctaGrid.Children.Add(resetBtn);

        Border applyBtn = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 8, 8, 8),
            Background = Gradient(0xFFCC58, 0xF09E16, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xDC8C10),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = "保存并应用",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = BrushOf(0x472905),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        applyBtn.MouseEnter += delegate { applyBtn.Background = Gradient(0xFFD674, 0xF5A824, 0, 0, 0, 1); };
        applyBtn.MouseLeave += delegate { applyBtn.Background = Gradient(0xFFCC58, 0xF09E16, 0, 0, 0, 1); };
        applyBtn.MouseLeftButtonUp += delegate { SaveAndApplyEditingSkin(); };
        Grid.SetColumn(applyBtn, 2);
        ctaGrid.Children.Add(applyBtn);

        ctaWrap.Children.Add(ctaGrid);
        DockPanel.SetDock(ctaWrap, Dock.Bottom);
        left.Children.Add(ctaWrap);

        return left;
    }

    private UIElement BuildWorkshopRightColumn()
    {
        StackPanel editor = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };

        // Section 1: Body base & markings
        StackPanel shapeSec = new StackPanel();
        shapeSec.Children.Add(MakeSectionHeader("底模与花纹", "英短 / 美短 / 布偶 / 折耳混搭"));

        shapeSec.Children.Add(MakeSubLabel("基础底模"));
        shapeSec.Children.Add(MakePillWrap(
            bodyBasePills,
            new[] { "短毛圆润", "英短圆脸", "折耳软萌", "长毛蓬松" },
            new object[] { SkinBodyBase.ShortHair, SkinBodyBase.ChubbyBritish, SkinBodyBase.FoldEar, SkinBodyBase.FluffyLong },
            delegate(object v)
            {
                editingSkin.BodyBase = (SkinBodyBase)v;
                UpdatePillGroup(bodyBasePills, editingSkin.BodyBase);
                RefreshWorkshopPreview();
            }));

        shapeSec.Children.Add(MakeSubLabel("脸部花纹"));
        shapeSec.Children.Add(MakePillWrap(
            facePills,
            new[] { "无", "美短虎斑", "布偶开脸", "蓝白正八", "咖啡面具", "经典虎斑M", "暹罗重点", "燕尾服", "三花斑", "奶牛斑", "玳瑁斑" },
            new object[]
            {
                SkinFacePattern.None, SkinFacePattern.AmericanShorthair, SkinFacePattern.RagdollMask,
                SkinFacePattern.BlueBicolor, SkinFacePattern.CoffeeMask, SkinFacePattern.TabbyM,
                SkinFacePattern.SiamesePoint, SkinFacePattern.Tuxedo, SkinFacePattern.Calico,
                SkinFacePattern.CowSpots, SkinFacePattern.Tortie
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
            new[] { "无", "小围兜", "大白肚", "长毛围脖", "胸口爱心", "虎斑肚点" },
            new object[]
            {
                SkinBellyStyle.None, SkinBellyStyle.Bib, SkinBellyStyle.FullBelly,
                SkinBellyStyle.Ruff, SkinBellyStyle.HeartChest, SkinBellyStyle.TabbyBelly
            },
            delegate(object v)
            {
                editingSkin.BellyStyle = (SkinBellyStyle)v;
                UpdatePillGroup(bellyPills, editingSkin.BellyStyle);
                RefreshWorkshopPreview();
            }));

        shapeSec.Children.Add(MakeSubLabel("身体纹路"));
        shapeSec.Children.Add(MakePillWrap(
            bodyPatternPills,
            new[] { "纯色", "侧肋条纹", "美短旋涡", "豹纹斑点", "花斑块", "披风背纹" },
            new object[]
            {
                SkinBodyPattern.None, SkinBodyPattern.FlankStripes, SkinBodyPattern.ClassicSwirl,
                SkinBodyPattern.Spotted, SkinBodyPattern.Patches, SkinBodyPattern.BackSaddle
            },
            delegate(object v)
            {
                editingSkin.BodyPattern = (SkinBodyPattern)v;
                UpdatePillGroup(bodyPatternPills, editingSkin.BodyPattern);
                RefreshWorkshopPreview();
            }));

        shapeSec.Children.Add(MakeSubLabel("前爪样式"));
        shapeSec.Children.Add(MakePillWrap(
            pawPills,
            new[] { "爪同体色", "白手套", "长筒白袜", "深色重点爪", "虎斑手环" },
            new object[]
            {
                SkinPawStyle.BodyColor, SkinPawStyle.Socks, SkinPawStyle.HighBoots,
                SkinPawStyle.DarkPoints, SkinPawStyle.TabbyBracelets
            },
            delegate(object v)
            {
                editingSkin.PawStyle = (SkinPawStyle)v;
                UpdatePillGroup(pawPills, editingSkin.PawStyle);
                RefreshWorkshopPreview();
            }));

        shapeSec.Children.Add(MakeSubLabel("尾巴样式"));
        shapeSec.Children.Add(MakePillWrap(
            tailPills,
            new[] { "标准纯色", "英短粗尾", "虎斑环纹", "浅色尾尖", "深色尾尖", "短毛球尾" },
            new object[]
            {
                SkinTailStyle.Solid, SkinTailStyle.ThickPlush, SkinTailStyle.TabbyRings,
                SkinTailStyle.LightTip, SkinTailStyle.DarkTip, SkinTailStyle.BobtailPom
            },
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
            new[] { "#757F8C", "#CFD4DA", "#262428", "#F0A860", "#BD8952", "#F6F0E8", "#E6D5BE", "#E8DFD2" },
            delegate { return editingSkin.FurColor; },
            delegate(string h) { editingSkin.FurColor = h; }));
        coatSec.Children.Add(AddColorPicker(
            "斑纹 / 重点色",
            new[] { "#3B4048", "#565F6C", "#6F5B50", "#4C3530", "#634328", "#C26F38", "#8E95A0", "#E59876" },
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
            Width = 16,
            Height = 16,
            CornerRadius = new CornerRadius(4),
            BorderBrush = BrushOf(0xC8BDA8),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 5, 0),
            Cursor = Cursors.Hand,
            ToolTip = "点击打开色盘自由选色",
            VerticalAlignment = VerticalAlignment.Center
        };
        Border paletteBtn = MakePaletteTriggerButton();
        TextBox hexBox = new TextBox
        {
            Width = 64,
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
        ColorPickerRow localPicker = picker;
        chip.MouseLeftButtonUp += delegate { OpenColorWheelPopup(localPicker, paletteBtn); };
        paletteBtn.MouseLeftButtonUp += delegate { OpenColorWheelPopup(localPicker, paletteBtn); };

        hexPart.Children.Add(chip);
        hexPart.Children.Add(paletteBtn);
        hexPart.Children.Add(hexBox);
        DockPanel.SetDock(hexPart, Dock.Right);
        topLine.Children.Add(hexPart);
        wrap.Children.Add(topLine);

        StackPanel swatchRow = new StackPanel { Orientation = Orientation.Horizontal };
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

    // --- Interactive Cream HSV Color Palette Popup (SV Square + Rainbow Hue Bar) ---
    private Popup colorWheelPopup;
    private ColorPickerRow activeWheelPicker;
    private double wheelH, wheelS = 0.6, wheelV = 0.8;
    private SolidColorBrush wheelHueFill;
    private Ellipse wheelSvThumb;
    private Border wheelHueThumb;
    private Border wheelPreviewBox;
    private TextBlock wheelHexLabel;
    private bool wheelDraggingSv, wheelDraggingHue;
    private const double SvWidth = 196.0, SvHeight = 132.0, HueWidth = 196.0;

    private Border MakePaletteTriggerButton()
    {
        LinearGradientBrush rainbow = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };
        rainbow.GradientStops.Add(new GradientStop(Color.FromRgb(255, 102, 102), 0.0));
        rainbow.GradientStops.Add(new GradientStop(Color.FromRgb(255, 206, 74), 0.35));
        rainbow.GradientStops.Add(new GradientStop(Color.FromRgb(86, 204, 146), 0.68));
        rainbow.GradientStops.Add(new GradientStop(Color.FromRgb(94, 158, 245), 1.0));
        rainbow.Freeze();

        StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        sp.Children.Add(new Ellipse
        {
            Width = 9,
            Height = 9,
            Fill = rainbow,
            Stroke = BrushOf(0x9E8E78),
            StrokeThickness = 0.7,
            Margin = new Thickness(0, 0, 3.5, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        sp.Children.Add(new TextBlock
        {
            Text = "色盘",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushOf(0x5A4834),
            VerticalAlignment = VerticalAlignment.Center
        });
        Border btn = new Border
        {
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(5, 1.5, 5, 1.5),
            Margin = new Thickness(0, 0, 5, 0),
            Background = BrushOf(0xFAF4E4),
            BorderBrush = BrushOf(0xDFD4BC),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "打开完整色盘实时选色",
            VerticalAlignment = VerticalAlignment.Center,
            Child = sp
        };
        btn.MouseEnter += delegate { btn.Background = BrushOf(0xFFF0C6); };
        btn.MouseLeave += delegate { btn.Background = BrushOf(0xFAF4E4); };
        return btn;
    }

    private void EnsureColorWheelPopup()
    {
        if (colorWheelPopup != null) return;

        wheelHueFill = new SolidColorBrush(Colors.Red);

        // 1. 2D Saturation / Value square
        Grid svGrid = new Grid { Width = SvWidth, Height = SvHeight, ClipToBounds = true, Cursor = Cursors.Cross };
        svGrid.Children.Add(new Border { Background = wheelHueFill });
        LinearGradientBrush whiteMask = new LinearGradientBrush(
            Color.FromArgb(255, 255, 255, 255), Color.FromArgb(0, 255, 255, 255),
            new Point(0, 0.5), new Point(1, 0.5));
        whiteMask.Freeze();
        svGrid.Children.Add(new Border { Background = whiteMask });
        LinearGradientBrush blackMask = new LinearGradientBrush(
            Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 0, 0),
            new Point(0.5, 0), new Point(0.5, 1));
        blackMask.Freeze();
        svGrid.Children.Add(new Border { Background = blackMask });

        Canvas svOverlay = new Canvas { Width = SvWidth, Height = SvHeight, IsHitTestVisible = false };
        wheelSvThumb = new Ellipse
        {
            Width = 12,
            Height = 12,
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Fill = Brushes.Transparent
        };
        svOverlay.Children.Add(wheelSvThumb);
        svGrid.Children.Add(svOverlay);

        svGrid.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
        {
            wheelDraggingSv = true;
            svGrid.CaptureMouse();
            UpdateSvFromPoint(e.GetPosition(svGrid));
            e.Handled = true;
        };
        svGrid.MouseMove += delegate(object s, MouseEventArgs e)
        {
            if (wheelDraggingSv)
            {
                UpdateSvFromPoint(e.GetPosition(svGrid));
                e.Handled = true;
            }
        };
        svGrid.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
        {
            wheelDraggingSv = false;
            if (svGrid.IsMouseCaptured) svGrid.ReleaseMouseCapture();
            e.Handled = true;
        };
        svGrid.LostMouseCapture += delegate { wheelDraggingSv = false; };

        Border svFrame = new Border
        {
            CornerRadius = new CornerRadius(7),
            BorderBrush = BrushOf(0xD8CCB6),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            Child = svGrid
        };

        // 2. Rainbow Hue bar
        LinearGradientBrush spectrum = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
        spectrum.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 0.00));
        spectrum.GradientStops.Add(new GradientStop(Color.FromRgb(255, 255, 0), 0.17));
        spectrum.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 0), 0.33));
        spectrum.GradientStops.Add(new GradientStop(Color.FromRgb(0, 255, 255), 0.50));
        spectrum.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 255), 0.67));
        spectrum.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 255), 0.83));
        spectrum.GradientStops.Add(new GradientStop(Color.FromRgb(255, 0, 0), 1.00));
        spectrum.Freeze();

        Grid hueGrid = new Grid { Width = HueWidth, Height = 16, Background = spectrum, Cursor = Cursors.Hand };
        Canvas hueOverlay = new Canvas { Width = HueWidth, Height = 16, IsHitTestVisible = false };
        wheelHueThumb = new Border
        {
            Width = 7,
            Height = 16,
            CornerRadius = new CornerRadius(3),
            Background = Brushes.White,
            BorderBrush = BrushOf(0x5A4834),
            BorderThickness = new Thickness(1)
        };
        hueOverlay.Children.Add(wheelHueThumb);
        hueGrid.Children.Add(hueOverlay);

        hueGrid.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
        {
            wheelDraggingHue = true;
            hueGrid.CaptureMouse();
            UpdateHueFromPoint(e.GetPosition(hueGrid));
            e.Handled = true;
        };
        hueGrid.MouseMove += delegate(object s, MouseEventArgs e)
        {
            if (wheelDraggingHue)
            {
                UpdateHueFromPoint(e.GetPosition(hueGrid));
                e.Handled = true;
            }
        };
        hueGrid.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
        {
            wheelDraggingHue = false;
            if (hueGrid.IsMouseCaptured) hueGrid.ReleaseMouseCapture();
            e.Handled = true;
        };
        hueGrid.LostMouseCapture += delegate { wheelDraggingHue = false; };

        Border hueFrame = new Border
        {
            CornerRadius = new CornerRadius(6),
            BorderBrush = BrushOf(0xD8CCB6),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 8, 0, 8),
            ClipToBounds = true,
            Child = hueGrid
        };

        // 3. Footer: preview swatch + hex + Done button
        DockPanel footer = new DockPanel { LastChildFill = false };
        StackPanel leftInfo = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        wheelPreviewBox = new Border
        {
            Width = 24,
            Height = 20,
            CornerRadius = new CornerRadius(5),
            BorderBrush = BrushOf(0xC6B9A2),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 6, 0)
        };
        wheelHexLabel = new TextBlock
        {
            Text = "#FFFFFF",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Consolas, Segoe UI"),
            Foreground = TextPrimary,
            VerticalAlignment = VerticalAlignment.Center
        };
        leftInfo.Children.Add(wheelPreviewBox);
        leftInfo.Children.Add(wheelHexLabel);
        DockPanel.SetDock(leftInfo, Dock.Left);
        footer.Children.Add(leftInfo);

        Border okBtn = new Border
        {
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10, 4, 10, 4),
            Background = Gradient(0xFFCC58, 0xF09E16, 0, 0, 0, 1),
            BorderBrush = BrushOf(0xDC8C10),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new TextBlock
            {
                Text = "确定",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = BrushOf(0x472905)
            }
        };
        okBtn.MouseEnter += delegate { okBtn.Background = Gradient(0xFFD674, 0xF5A824, 0, 0, 0, 1); };
        okBtn.MouseLeave += delegate { okBtn.Background = Gradient(0xFFCC58, 0xF09E16, 0, 0, 0, 1); };
        okBtn.MouseLeftButtonUp += delegate { colorWheelPopup.IsOpen = false; };
        DockPanel.SetDock(okBtn, Dock.Right);
        footer.Children.Add(okBtn);

        StackPanel body = new StackPanel();
        body.Children.Add(svFrame);
        body.Children.Add(hueFrame);
        body.Children.Add(footer);

        Border card = new Border
        {
            Background = CardBg,
            BorderBrush = BrushOf(0xD8CCB6),
            BorderThickness = new Thickness(1.2),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10),
            Child = body
        };

        colorWheelPopup = new Popup
        {
            AllowsTransparency = true,
            StaysOpen = false,
            Placement = PlacementMode.Bottom,
            VerticalOffset = 4,
            Child = card
        };
        colorWheelPopup.Closed += delegate
        {
            wheelDraggingSv = false;
            wheelDraggingHue = false;
        };
    }

    private void OpenColorWheelPopup(ColorPickerRow picker, UIElement anchor)
    {
        EnsureColorWheelPopup();
        activeWheelPicker = picker;
        Color current = CatSkinData.ParseHex(picker.Getter(), Colors.Gray);
        RgbToHsv(current, out wheelH, out wheelS, out wheelV);
        RefreshColorWheelVisuals(false);
        colorWheelPopup.PlacementTarget = anchor;
        colorWheelPopup.IsOpen = true;
    }

    private void UpdateSvFromPoint(Point pt)
    {
        wheelS = Math.Max(0.0, Math.Min(1.0, pt.X / SvWidth));
        wheelV = Math.Max(0.0, Math.Min(1.0, 1.0 - pt.Y / SvHeight));
        RefreshColorWheelVisuals(true);
    }

    private void UpdateHueFromPoint(Point pt)
    {
        wheelH = Math.Max(0.0, Math.Min(359.5, (pt.X / HueWidth) * 360.0));
        RefreshColorWheelVisuals(true);
    }

    private void RefreshColorWheelVisuals(bool applyToModel)
    {
        wheelHueFill.Color = HsvToRgb(wheelH, 1.0, 1.0);
        Canvas.SetLeft(wheelSvThumb, wheelS * SvWidth - 6.0);
        Canvas.SetTop(wheelSvThumb, (1.0 - wheelV) * SvHeight - 6.0);
        wheelSvThumb.Stroke = wheelV > 0.72 && wheelS < 0.35 ? BrushOf(0x423529) : Brushes.White;
        Canvas.SetLeft(wheelHueThumb, Math.Max(0, Math.Min(HueWidth - 7.0, (wheelH / 360.0) * HueWidth - 3.5)));

        Color picked = HsvToRgb(wheelH, wheelS, wheelV);
        string hex = string.Format(System.Globalization.CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", picked.R, picked.G, picked.B);
        SolidColorBrush previewBrush = new SolidColorBrush(picked);
        previewBrush.Freeze();
        wheelPreviewBox.Background = previewBrush;
        wheelHexLabel.Text = hex;

        if (applyToModel && activeWheelPicker != null)
        {
            activeWheelPicker.Setter(hex);
            UpdateColorPickerVisual(activeWheelPicker, hex);
            RefreshWorkshopPreview();
        }
    }

    private static void RgbToHsv(Color c, out double h, out double s, out double v)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        v = max;
        s = max <= 0.0001 ? 0.0 : delta / max;
        if (delta <= 0.0001) h = 0.0;
        else if (max == r) h = 60.0 * (((g - b) / delta) % 6.0);
        else if (max == g) h = 60.0 * (((b - r) / delta) + 2.0);
        else h = 60.0 * (((r - g) / delta) + 4.0);
        if (h < 0.0) h += 360.0;
    }

    private static Color HsvToRgb(double h, double s, double v)
    {
        h = ((h % 360.0) + 360.0) % 360.0;
        double c = v * s;
        double x = c * (1.0 - Math.Abs((h / 60.0) % 2.0 - 1.0));
        double m = v - c;
        double r = 0, g = 0, b = 0;
        if (h < 60) { r = c; g = x; }
        else if (h < 120) { r = x; g = c; }
        else if (h < 180) { g = c; b = x; }
        else if (h < 240) { g = x; b = c; }
        else if (h < 300) { r = x; b = c; }
        else { r = c; b = x; }
        return Color.FromRgb(
            (byte)Math.Max(0, Math.Min(255, (int)Math.Round((r + m) * 255.0))),
            (byte)Math.Max(0, Math.Min(255, (int)Math.Round((g + m) * 255.0))),
            (byte)Math.Max(0, Math.Min(255, (int)Math.Round((b + m) * 255.0))));
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
            UpdatePillGroup(bodyBasePills, editingSkin.BodyBase);
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
        editingSkin = CatSkinData.FromPreset("bluewhite");
        editingSkin.Id = CatSkinData.NewId();
        editingSkin.Name = "我的小猫 " + (savedSkins.Count + 1);
        SyncWorkshopControlsFromModel();
        SetWorkshopStatus("已新建皮肤草稿，完成后点击应用");
    }

    private void ResetEditingSkin()
    {
        if (editingSkin == null)
        {
            CreateNewSkinDraft();
            return;
        }
        string keepId = editingSkin.Id;
        CatSkinData saved = null;
        for (int i = 0; i < savedSkins.Count; ++i)
        {
            if (string.Equals(savedSkins[i].Id, keepId, StringComparison.OrdinalIgnoreCase))
            {
                saved = savedSkins[i];
                break;
            }
        }
        if (saved != null)
        {
            editingSkin = saved.Clone();
            SyncWorkshopControlsFromModel();
            SetWorkshopStatus("已恢复「" + saved.Name + "」上次保存状态");
        }
        else
        {
            editingSkin = CatSkinData.FromPreset("bluewhite");
            editingSkin.Id = keepId;
            SyncWorkshopControlsFromModel();
            SetWorkshopStatus("已重置为默认配色");
        }
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
