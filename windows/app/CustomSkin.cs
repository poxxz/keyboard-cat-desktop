using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

public enum SkinSlot
{
    Head,
    HeadBlink,
    HeadAngry,
    Body,
    Tail,
    PawLeft,
    PawRight,
    Accessory
}

public enum CustomEyeMode
{
    PngOnly,
    OverlayDynamicEyes
}

public enum CustomArmMode
{
    FlexArmWithPngPaw,
    FullArmSprite,
    BaseRigArms
}

public sealed class SkinLayerConfig
{
    public string FileName = "";
    public double OffsetX;
    public double OffsetY;
    public double Scale = 1.0;
    public double Rotation;
    public double Opacity = 1.0;
    public bool FlipX;
    public double PivotX = 0.5;
    public double PivotY = 0.5;

    public bool HasFile { get { return !string.IsNullOrEmpty(FileName); } }

    public static SkinLayerConfig CreateDefault(SkinSlot slot)
    {
        SkinLayerConfig cfg = new SkinLayerConfig();
        ApplyDefaultPivot(cfg, slot);
        return cfg;
    }

    public static void ApplyDefaultPivot(SkinLayerConfig cfg, SkinSlot slot)
    {
        switch (slot)
        {
            case SkinSlot.Tail:
                cfg.PivotX = 0.10;
                cfg.PivotY = 0.72;
                break;
            case SkinSlot.Accessory:
                cfg.PivotX = 0.50;
                cfg.PivotY = 0.75;
                break;
            case SkinSlot.PawLeft:
            case SkinSlot.PawRight:
                cfg.PivotX = 0.50;
                cfg.PivotY = 0.50;
                break;
            default:
                cfg.PivotX = 0.50;
                cfg.PivotY = 0.50;
                break;
        }
    }

    public void ResetTransform(SkinSlot slot)
    {
        OffsetX = 0;
        OffsetY = 0;
        Scale = 1.0;
        Rotation = 0;
        Opacity = 1.0;
        FlipX = false;
        ApplyDefaultPivot(this, slot);
    }

    public SkinLayerConfig Clone()
    {
        return new SkinLayerConfig
        {
            FileName = FileName ?? "",
            OffsetX = OffsetX,
            OffsetY = OffsetY,
            Scale = Scale,
            Rotation = Rotation,
            Opacity = Opacity,
            FlipX = FlipX,
            PivotX = PivotX,
            PivotY = PivotY
        };
    }

    public XElement ToXml(SkinSlot slot)
    {
        return new XElement("layer",
            new XAttribute("slot", slot.ToString()),
            new XAttribute("file", FileName ?? ""),
            new XAttribute("offsetX", OffsetX.ToString("0.###", CultureInfo.InvariantCulture)),
            new XAttribute("offsetY", OffsetY.ToString("0.###", CultureInfo.InvariantCulture)),
            new XAttribute("scale", Scale.ToString("0.###", CultureInfo.InvariantCulture)),
            new XAttribute("rotation", Rotation.ToString("0.###", CultureInfo.InvariantCulture)),
            new XAttribute("opacity", Opacity.ToString("0.###", CultureInfo.InvariantCulture)),
            new XAttribute("flipX", FlipX),
            new XAttribute("pivotX", PivotX.ToString("0.###", CultureInfo.InvariantCulture)),
            new XAttribute("pivotY", PivotY.ToString("0.###", CultureInfo.InvariantCulture)));
    }

    public static SkinLayerConfig FromXml(XElement el, SkinSlot slot)
    {
        SkinLayerConfig cfg = CreateDefault(slot);
        if (el == null) return cfg;
        string file = (string)el.Attribute("file");
        if (!string.IsNullOrEmpty(file))
        {
            // Keep only safe file name inside the skin directory.
            cfg.FileName = Path.GetFileName(file.Trim());
        }
        cfg.OffsetX = CustomSkin.ClampDouble(ReadDouble(el, "offsetX", 0), -180, 180);
        cfg.OffsetY = CustomSkin.ClampDouble(ReadDouble(el, "offsetY", 0), -180, 180);
        cfg.Scale = CustomSkin.ClampDouble(ReadDouble(el, "scale", 1.0), 0.15, 3.5);
        cfg.Rotation = CustomSkin.ClampDouble(ReadDouble(el, "rotation", 0), -180, 180);
        cfg.Opacity = CustomSkin.ClampDouble(ReadDouble(el, "opacity", 1.0), 0.05, 1.0);
        cfg.FlipX = (bool?)el.Attribute("flipX") ?? false;
        cfg.PivotX = CustomSkin.ClampDouble(ReadDouble(el, "pivotX", cfg.PivotX), 0.0, 1.0);
        cfg.PivotY = CustomSkin.ClampDouble(ReadDouble(el, "pivotY", cfg.PivotY), 0.0, 1.0);
        return cfg;
    }

    private static double ReadDouble(XElement el, string name, double fallback)
    {
        string raw = (string)el.Attribute(name);
        double value;
        if (raw != null && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !double.IsNaN(value) && !double.IsInfinity(value))
            return value;
        return fallback;
    }
}

public sealed class CustomSkin
{
    public const int FormatVersion = 1;

    public static readonly SkinSlot[] AllSlots = new SkinSlot[]
    {
        SkinSlot.Head,
        SkinSlot.HeadBlink,
        SkinSlot.HeadAngry,
        SkinSlot.Body,
        SkinSlot.Tail,
        SkinSlot.PawLeft,
        SkinSlot.PawRight,
        SkinSlot.Accessory
    };

    public string Id = "";
    public string Name = "新建贴图皮肤";
    public string Author = "";
    public string DirectoryPath = "";
    public CatStyle BaseCat = CatStyle.Black;
    public CustomEyeMode EyeMode = CustomEyeMode.PngOnly;
    public CustomArmMode ArmMode = CustomArmMode.FlexArmWithPngPaw;
    public bool MirrorLeftPawToRight = true;
    public bool ShowMoodEffects = true;
    public bool ShowWhiskersOverlay = false;
    public bool PixelArtFilter = false;

    public bool UseCustomArmColor = false;
    public string ArmColorHex = "#242226";
    public string ArmTipColorHex = "#242226";

    public string EyeIrisHex = "#D7F550";
    public string EyePupilHex = "#161810";
    public double EyeOffsetY = 0;
    public double EyeSpacing = 19.1;
    public double EyeScale = 1.0;

    public readonly Dictionary<SkinSlot, SkinLayerConfig> Layers = new Dictionary<SkinSlot, SkinLayerConfig>();
    private readonly Dictionary<SkinSlot, BitmapSource> imageCache = new Dictionary<SkinSlot, BitmapSource>();

    public CustomSkin()
    {
        for (int i = 0; i < AllSlots.Length; ++i)
        {
            SkinSlot slot = AllSlots[i];
            Layers[slot] = SkinLayerConfig.CreateDefault(slot);
        }
    }

    public SkinLayerConfig GetLayer(SkinSlot slot)
    {
        SkinLayerConfig cfg;
        if (!Layers.TryGetValue(slot, out cfg))
        {
            cfg = SkinLayerConfig.CreateDefault(slot);
            Layers[slot] = cfg;
        }
        return cfg;
    }

    public BitmapSource GetImage(SkinSlot slot)
    {
        BitmapSource bmp;
        return imageCache.TryGetValue(slot, out bmp) ? bmp : null;
    }

    public bool HasImage(SkinSlot slot)
    {
        return GetImage(slot) != null;
    }

    public int ActivePngCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < AllSlots.Length; ++i)
            {
                if (GetImage(AllSlots[i]) != null) count++;
            }
            return count;
        }
    }

    public bool HasAnyPng { get { return ActivePngCount > 0; } }

    public void ReloadImages()
    {
        imageCache.Clear();
        if (string.IsNullOrEmpty(DirectoryPath) || !Directory.Exists(DirectoryPath)) return;
        for (int i = 0; i < AllSlots.Length; ++i)
        {
            SkinSlot slot = AllSlots[i];
            SkinLayerConfig layer = GetLayer(slot);
            if (!layer.HasFile) continue;
            string fullPath = Path.Combine(DirectoryPath, layer.FileName);
            BitmapSource bmp = LoadUnlockedBitmap(fullPath);
            if (bmp != null) imageCache[slot] = bmp;
        }
    }

    // Load PNG fully into memory and freeze so the file on disk is never locked for external editing or deletion.
    public static BitmapSource LoadUnlockedBitmap(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            if (bytes == null || bytes.Length < 8) return null;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze();
                return img;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    public XElement ToXml()
    {
        XElement root = new XElement("skin",
            new XAttribute("formatVersion", FormatVersion),
            new XAttribute("id", Id ?? ""),
            new XAttribute("name", Name ?? "未命名皮肤"),
            new XAttribute("author", Author ?? ""),
            new XAttribute("baseCat", BaseCat.ToString()),
            new XAttribute("eyeMode", EyeMode.ToString()),
            new XAttribute("armMode", ArmMode.ToString()),
            new XAttribute("mirrorLeftPaw", MirrorLeftPawToRight),
            new XAttribute("showMoodEffects", ShowMoodEffects),
            new XAttribute("showWhiskers", ShowWhiskersOverlay),
            new XAttribute("pixelArt", PixelArtFilter),
            new XAttribute("useCustomArmColor", UseCustomArmColor),
            new XAttribute("armColor", NormalizeHex(ArmColorHex, "#242226")),
            new XAttribute("armTipColor", NormalizeHex(ArmTipColorHex, "#242226")),
            new XAttribute("eyeIris", NormalizeHex(EyeIrisHex, "#D7F550")),
            new XAttribute("eyePupil", NormalizeHex(EyePupilHex, "#161810")),
            new XAttribute("eyeOffsetY", EyeOffsetY.ToString("0.###", CultureInfo.InvariantCulture)),
            new XAttribute("eyeSpacing", EyeSpacing.ToString("0.###", CultureInfo.InvariantCulture)),
            new XAttribute("eyeScale", EyeScale.ToString("0.###", CultureInfo.InvariantCulture)));

        XElement layersEl = new XElement("layers");
        for (int i = 0; i < AllSlots.Length; ++i)
        {
            SkinSlot slot = AllSlots[i];
            layersEl.Add(GetLayer(slot).ToXml(slot));
        }
        root.Add(layersEl);
        return root;
    }

    public static CustomSkin FromXml(XElement root, string folderPath, string fallbackId)
    {
        CustomSkin skin = new CustomSkin();
        skin.DirectoryPath = folderPath ?? "";
        skin.Id = fallbackId ?? "";
        if (root == null) return skin;

        string id = (string)root.Attribute("id");
        if (!string.IsNullOrEmpty(id)) skin.Id = SanitizeId(id, fallbackId);
        string name = (string)root.Attribute("name");
        if (!string.IsNullOrEmpty(name)) skin.Name = name.Trim();
        string author = (string)root.Attribute("author");
        if (author != null) skin.Author = author.Trim();

        skin.BaseCat = ReadEnum(root, "baseCat", CatStyle.Black);
        skin.EyeMode = ReadEnum(root, "eyeMode", CustomEyeMode.PngOnly);
        skin.ArmMode = ReadEnum(root, "armMode", CustomArmMode.FlexArmWithPngPaw);
        skin.MirrorLeftPawToRight = (bool?)root.Attribute("mirrorLeftPaw") ?? true;
        skin.ShowMoodEffects = (bool?)root.Attribute("showMoodEffects") ?? true;
        skin.ShowWhiskersOverlay = (bool?)root.Attribute("showWhiskers") ?? false;
        skin.PixelArtFilter = (bool?)root.Attribute("pixelArt") ?? false;

        skin.UseCustomArmColor = (bool?)root.Attribute("useCustomArmColor") ?? false;
        skin.ArmColorHex = NormalizeHex((string)root.Attribute("armColor"), "#242226");
        skin.ArmTipColorHex = NormalizeHex((string)root.Attribute("armTipColor"), skin.ArmColorHex);

        skin.EyeIrisHex = NormalizeHex((string)root.Attribute("eyeIris"), "#D7F550");
        skin.EyePupilHex = NormalizeHex((string)root.Attribute("eyePupil"), "#161810");
        skin.EyeOffsetY = ClampDouble(ReadDouble(root, "eyeOffsetY", 0), -25, 25);
        skin.EyeSpacing = ClampDouble(ReadDouble(root, "eyeSpacing", 19.1), 10, 32);
        skin.EyeScale = ClampDouble(ReadDouble(root, "eyeScale", 1.0), 0.5, 1.6);

        XElement layersEl = root.Element("layers");
        if (layersEl != null)
        {
            foreach (XElement el in layersEl.Elements("layer"))
            {
                SkinSlot slot;
                if (Enum.TryParse((string)el.Attribute("slot"), out slot) && Enum.IsDefined(typeof(SkinSlot), slot))
                {
                    skin.Layers[slot] = SkinLayerConfig.FromXml(el, slot);
                }
            }
        }
        skin.ReloadImages();
        return skin;
    }

    public static string SlotDefaultFileName(SkinSlot slot)
    {
        switch (slot)
        {
            case SkinSlot.Head: return "head.png";
            case SkinSlot.HeadBlink: return "head_blink.png";
            case SkinSlot.HeadAngry: return "head_angry.png";
            case SkinSlot.Body: return "body.png";
            case SkinSlot.Tail: return "tail.png";
            case SkinSlot.PawLeft: return "paw_left.png";
            case SkinSlot.PawRight: return "paw_right.png";
            case SkinSlot.Accessory: return "accessory.png";
            default: return "layer.png";
        }
    }

    public static string SlotDisplayName(SkinSlot slot)
    {
        switch (slot)
        {
            case SkinSlot.Head: return "头部 (常态)";
            case SkinSlot.HeadBlink: return "头部 (闭眼/睡)";
            case SkinSlot.HeadAngry: return "头部 (生气)";
            case SkinSlot.Body: return "身体躯干";
            case SkinSlot.Tail: return "尾巴";
            case SkinSlot.PawLeft: return "左爪 / 左臂";
            case SkinSlot.PawRight: return "右爪 / 右臂";
            case SkinSlot.Accessory: return "头饰 / 挂件";
            default: return slot.ToString();
        }
    }

    public static string SlotHint(SkinSlot slot)
    {
        switch (slot)
        {
            case SkinSlot.Head: return "建议透明 PNG（如 512×512），自动跟随呼吸与视线转动";
            case SkinSlot.HeadBlink: return "眨眼、趴睡与密码防偷窥时自动切换此图（可选）";
            case SkinSlot.HeadAngry: return "连续快速敲键触发愤怒时自动切换此图（可选）";
            case SkinSlot.Body: return "自动跟随前倾拉伸、蹲伏与趴睡压扁形变";
            case SkinSlot.Tail: return "以左下方尾根为支点动态摆动，情绪越急摇得越快";
            case SkinSlot.PawLeft: return "敲键盘、握鼠标与握持手柄时联动；右爪未导入时可自动镜像";
            case SkinSlot.PawRight: return "可选，留空且开启“镜像左爪”时自动复用左爪贴图";
            case SkinSlot.Accessory: return "帽子、蝴蝶结、墨镜、耳机等，叠在头部最前层同步运动";
            default: return "";
        }
    }

    public static string SanitizeId(string raw, string fallback)
    {
        if (string.IsNullOrEmpty(raw)) return string.IsNullOrEmpty(fallback) ? "skin" : fallback;
        char[] chars = raw.ToCharArray();
        List<char> clean = new List<char>(chars.Length);
        for (int i = 0; i < chars.Length; ++i)
        {
            char c = chars[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') || c == '-' || c == '_')
            {
                clean.Add(c);
            }
        }
        string s = new string(clean.ToArray());
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(fallback) ? "skin" : fallback;
        return s.Length > 48 ? s.Substring(0, 48) : s;
    }

    public static double ClampDouble(double value, double min, double max)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return min;
        return Math.Max(min, Math.Min(max, value));
    }

    public static Color ParseHexColor(string hex, Color fallback)
    {
        if (string.IsNullOrEmpty(hex)) return fallback;
        string s = hex.Trim();
        if (!s.StartsWith("#", StringComparison.Ordinal)) s = "#" + s;
        if (s.Length == 7)
        {
            byte r, g, b;
            if (byte.TryParse(s.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
                byte.TryParse(s.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
                byte.TryParse(s.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
            {
                return Color.FromRgb(r, g, b);
            }
        }
        return fallback;
    }

    public static string ToHexColor(Color c)
    {
        return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
    }

    public static string NormalizeHex(string hex, string fallback)
    {
        Color fb = ParseHexColor(fallback, Color.FromRgb(36, 34, 38));
        return ToHexColor(ParseHexColor(hex, fb));
    }

    // Samples average opaque interior color from an imported PNG so dynamic curved arms can auto-match the coat.
    public static bool TrySampleDominantColor(BitmapSource source, out Color color)
    {
        color = Color.FromRgb(36, 34, 38);
        if (source == null || source.PixelWidth <= 0 || source.PixelHeight <= 0) return false;
        try
        {
            FormatConvertedBitmap bgra = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            int w = bgra.PixelWidth;
            int h = bgra.PixelHeight;
            int stride = w * 4;
            byte[] pixels = new byte[h * stride];
            bgra.CopyPixels(pixels, stride, 0);

            // Sample central 60% first so borders/outlines don't skew the fur color, fallback to full image.
            int x0 = (int)(w * 0.20), x1 = Math.Max(x0 + 1, (int)(w * 0.80));
            int y0 = (int)(h * 0.20), y1 = Math.Max(y0 + 1, (int)(h * 0.80));
            if (SampleRegion(pixels, stride, x0, y0, x1, y1, out color)) return true;
            return SampleRegion(pixels, stride, 0, 0, w, h, out color);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool SampleRegion(byte[] pixels, int stride, int x0, int y0, int x1, int y1, out Color color)
    {
        color = Color.FromRgb(36, 34, 38);
        long sumR = 0, sumG = 0, sumB = 0, count = 0;
        int stepX = Math.Max(1, (x1 - x0) / 48);
        int stepY = Math.Max(1, (y1 - y0) / 48);
        for (int y = y0; y < y1; y += stepY)
        {
            int row = y * stride;
            for (int x = x0; x < x1; x += stepX)
            {
                int idx = row + x * 4;
                byte a = pixels[idx + 3];
                if (a < 180) continue;
                sumB += pixels[idx];
                sumG += pixels[idx + 1];
                sumR += pixels[idx + 2];
                count++;
            }
        }
        if (count < 4) return false;
        color = Color.FromRgb((byte)(sumR / count), (byte)(sumG / count), (byte)(sumB / count));
        return true;
    }

    private static T ReadEnum<T>(XElement el, string name, T fallback) where T : struct
    {
        T value;
        return Enum.TryParse((string)el.Attribute(name), out value) && Enum.IsDefined(typeof(T), value)
            ? value : fallback;
    }

    private static double ReadDouble(XElement el, string name, double fallback)
    {
        string raw = (string)el.Attribute(name);
        double value;
        if (raw != null && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !double.IsNaN(value) && !double.IsInfinity(value))
            return value;
        return fallback;
    }
}
