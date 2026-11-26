using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Xml.Linq;

public enum SkinBodyBase { ShortHair, ChubbyBritish, FoldEar, FluffyLong }
public enum SkinFacePattern { None, AmericanShorthair, RagdollMask, BlueBicolor, CoffeeMask, TabbyM, SiamesePoint, Tuxedo, Calico, CowSpots, Tortie }
public enum SkinBellyStyle { None, Bib, FullBelly, Ruff, HeartChest, TabbyBelly, TuxedoV, Necktie, StarChest, LowerBelly }
public enum SkinBodyPattern { None, FlankStripes, ClassicSwirl, Spotted, Patches, BackSaddle, CowMottled, Mackerel, TortieMottle, ShoulderCape }
public enum SkinPawStyle { BodyColor, Socks, HighBoots, DarkPoints, TabbyBracelets }
public enum SkinTailStyle { Solid, ThickPlush, TabbyRings, LightTip, DarkTip, BobtailPom }
public enum SkinPupilStyle { SleekOval, RoundCute, BeadMatte }
public enum SkinAccessory { None, CollarBell, BowTie, HeadRibbon, Glasses, HeadOrange, Ahoge, WizardHat, AngelWings, DevilWings, Crown, Halo }

// Pure-vector user-customizable cat skin data (.catskin XML).
internal sealed class CatSkinData
{
    public string Id = "";
    public string Name = "我的小猫";
    public SkinBodyBase BodyBase = SkinBodyBase.ChubbyBritish;
    public string FurColor = "#757F8C";
    public string AccentColor = "#4C5562";
    public string BellyColor = "#FFFDF8";
    public string PawColor = "#FFFDF8";
    public string InnerEarColor = "#D8A4A0";
    public string NoseColor = "#5B6068";
    public string WhiskerColor = "#E6E9EE";
    public string LeftEyeColor = "#EB9D28";
    public bool Heterochromia = false;
    public string RightEyeColor = "#6BB8E8";
    public SkinPupilStyle PupilStyle = SkinPupilStyle.SleekOval;
    public SkinFacePattern FacePattern = SkinFacePattern.BlueBicolor;
    public SkinBellyStyle BellyStyle = SkinBellyStyle.FullBelly;
    public SkinBodyPattern BodyPattern = SkinBodyPattern.None;
    public SkinPawStyle PawStyle = SkinPawStyle.Socks;
    public SkinTailStyle TailStyle = SkinTailStyle.ThickPlush;
    public bool DarkEarTips = false;
    public SkinAccessory Accessory = SkinAccessory.CollarBell;
    public string AccessoryColor = "#E46652";

    public CatSkinData Clone()
    {
        return (CatSkinData)MemberwiseClone();
    }

    public bool FaceUsesAccent()
    {
        return FacePattern == SkinFacePattern.AmericanShorthair ||
               FacePattern == SkinFacePattern.RagdollMask ||
               FacePattern == SkinFacePattern.CoffeeMask ||
               FacePattern == SkinFacePattern.TabbyM ||
               FacePattern == SkinFacePattern.SiamesePoint ||
               FacePattern == SkinFacePattern.Calico ||
               FacePattern == SkinFacePattern.CowSpots ||
               FacePattern == SkinFacePattern.Tortie;
    }

    public bool FaceUsesBellyMask()
    {
        return FacePattern == SkinFacePattern.AmericanShorthair ||
               FacePattern == SkinFacePattern.RagdollMask ||
               FacePattern == SkinFacePattern.BlueBicolor ||
               FacePattern == SkinFacePattern.CoffeeMask ||
               FacePattern == SkinFacePattern.Tuxedo ||
               (FacePattern == SkinFacePattern.TabbyM && BellyStyle != SkinBellyStyle.None);
    }

    public bool UsesAccentColor()
    {
        return FaceUsesAccent() ||
               BodyPattern != SkinBodyPattern.None ||
               BellyStyle == SkinBellyStyle.TabbyBelly ||
               PawStyle == SkinPawStyle.DarkPoints ||
               PawStyle == SkinPawStyle.TabbyBracelets ||
               TailStyle == SkinTailStyle.TabbyRings ||
               TailStyle == SkinTailStyle.DarkTip ||
               TailStyle == SkinTailStyle.BobtailPom ||
               DarkEarTips;
    }

    public bool UsesBellyColor()
    {
        return BellyStyle != SkinBellyStyle.None ||
               FaceUsesBellyMask() ||
               TailStyle == SkinTailStyle.LightTip;
    }

    public bool UsesPawColor()
    {
        return PawStyle == SkinPawStyle.Socks ||
               PawStyle == SkinPawStyle.HighBoots ||
               PawStyle == SkinPawStyle.TabbyBracelets;
    }

    public string AccentUsageHint()
    {
        List<string> parts = new List<string>();
        if (FaceUsesAccent()) parts.Add(FacePattern == SkinFacePattern.SiamesePoint || FacePattern == SkinFacePattern.RagdollMask ? "重点面罩" : "脸纹");
        if (DarkEarTips) parts.Add("耳尖");
        if (BodyPattern != SkinBodyPattern.None || BellyStyle == SkinBellyStyle.TabbyBelly) parts.Add("身纹");
        if (PawStyle == SkinPawStyle.DarkPoints) parts.Add("重点爪");
        else if (PawStyle == SkinPawStyle.TabbyBracelets) parts.Add("手环");
        if (TailStyle == SkinTailStyle.TabbyRings || TailStyle == SkinTailStyle.DarkTip || TailStyle == SkinTailStyle.BobtailPom) parts.Add("尾纹");
        return parts.Count > 0 ? string.Join("/", parts.ToArray()) : "";
    }

    public string BellyUsageHint()
    {
        List<string> parts = new List<string>();
        if (FaceUsesBellyMask()) parts.Add("白面罩");
        if (BellyStyle != SkinBellyStyle.None) parts.Add("胸腹");
        if (TailStyle == SkinTailStyle.LightTip) parts.Add("浅尾尖");
        return parts.Count > 0 ? string.Join("/", parts.ToArray()) : "";
    }

    public string PawUsageHint()
    {
        if (PawStyle == SkinPawStyle.Socks) return "白手套";
        if (PawStyle == SkinPawStyle.HighBoots) return "长筒袜";
        if (PawStyle == SkinPawStyle.TabbyBracelets) return "爪尖";
        return "";
    }

    public static Color ParseHex(string hex, Color fallback)
    {
        if (string.IsNullOrEmpty(hex)) return fallback;
        string s = hex.Trim();
        if (s.StartsWith("#")) s = s.Substring(1);
        if (s.Length != 6) return fallback;
        int rgb;
        if (!int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rgb))
            return fallback;
        return Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    public static string NormalizeHex(string hex, string fallback)
    {
        Color c = ParseHex(hex, ParseHex(fallback, Colors.Gray));
        return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
    }

    // One-click common cat breed & built-in presets for the Skin Workshop.
    public static CatSkinData FromPreset(string presetKey)
    {
        CatSkinData d = new CatSkinData { Id = NewId() };
        switch ((presetKey ?? "").ToLowerInvariant())
        {
            case "blue":
                d.Name = "英短蓝猫";
                d.BodyBase = SkinBodyBase.ChubbyBritish;
                d.FurColor = "#757F8C"; d.AccentColor = "#565F6C"; d.BellyColor = "#8993A0";
                d.PawColor = "#757F8C"; d.InnerEarColor = "#B89A9C"; d.NoseColor = "#4C525C";
                d.WhiskerColor = "#C8D0D8"; d.LeftEyeColor = "#EB9D28"; d.RightEyeColor = "#EB9D28";
                d.PupilStyle = SkinPupilStyle.RoundCute;
                d.FacePattern = SkinFacePattern.None; d.BellyStyle = SkinBellyStyle.None;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.BodyColor;
                d.TailStyle = SkinTailStyle.ThickPlush; d.Accessory = SkinAccessory.CollarBell;
                d.AccessoryColor = "#E46652";
                return d;

            case "bluewhite":
                d.Name = "英短蓝白";
                d.BodyBase = SkinBodyBase.ChubbyBritish;
                d.FurColor = "#757F8C"; d.AccentColor = "#565F6C"; d.BellyColor = "#FFFDF8";
                d.PawColor = "#FFFDF8"; d.InnerEarColor = "#E4ACA8"; d.NoseColor = "#E4969A";
                d.WhiskerColor = "#F4F6F9"; d.LeftEyeColor = "#F0A82C"; d.RightEyeColor = "#F0A82C";
                d.PupilStyle = SkinPupilStyle.RoundCute;
                d.FacePattern = SkinFacePattern.BlueBicolor; d.BellyStyle = SkinBellyStyle.FullBelly;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.HighBoots;
                d.TailStyle = SkinTailStyle.ThickPlush; d.Accessory = SkinAccessory.BowTie;
                d.AccessoryColor = "#5B8EC9";
                return d;

            case "amshort":
                d.Name = "美短虎斑";
                d.BodyBase = SkinBodyBase.ShortHair;
                d.FurColor = "#CFD4DA"; d.AccentColor = "#3B4048"; d.BellyColor = "#F3F5F7";
                d.PawColor = "#EAEDF0"; d.InnerEarColor = "#E0A8A4"; d.NoseColor = "#D48482";
                d.WhiskerColor = "#F5F7FA"; d.LeftEyeColor = "#74C47C"; d.RightEyeColor = "#74C47C";
                d.PupilStyle = SkinPupilStyle.SleekOval;
                d.FacePattern = SkinFacePattern.AmericanShorthair; d.BellyStyle = SkinBellyStyle.TabbyBelly;
                d.BodyPattern = SkinBodyPattern.ClassicSwirl; d.PawStyle = SkinPawStyle.TabbyBracelets;
                d.TailStyle = SkinTailStyle.TabbyRings; d.DarkEarTips = true;
                d.Accessory = SkinAccessory.None;
                return d;

            case "ragdoll":
                d.Name = "海双布偶";
                d.BodyBase = SkinBodyBase.FluffyLong;
                d.FurColor = "#F6F0E8"; d.AccentColor = "#6F5B50"; d.BellyColor = "#FFFDF9";
                d.PawColor = "#FFFDF9"; d.InnerEarColor = "#B8948C"; d.NoseColor = "#E4969C";
                d.WhiskerColor = "#FFFFFF"; d.LeftEyeColor = "#60B2EB"; d.RightEyeColor = "#60B2EB";
                d.PupilStyle = SkinPupilStyle.SleekOval;
                d.FacePattern = SkinFacePattern.RagdollMask; d.BellyStyle = SkinBellyStyle.Ruff;
                d.BodyPattern = SkinBodyPattern.BackSaddle; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.DarkTip; d.Accessory = SkinAccessory.HeadRibbon;
                d.AccessoryColor = "#EC8298";
                return d;

            case "fold":
                d.Name = "软萌折耳";
                d.BodyBase = SkinBodyBase.FoldEar;
                d.FurColor = "#E8DFD2"; d.AccentColor = "#A89886"; d.BellyColor = "#FFFBF4";
                d.PawColor = "#FFFBF4"; d.InnerEarColor = "#EAB4AE"; d.NoseColor = "#E28E92";
                d.WhiskerColor = "#C6B9AA"; d.LeftEyeColor = "#E8A236"; d.RightEyeColor = "#E8A236";
                d.PupilStyle = SkinPupilStyle.RoundCute;
                d.FacePattern = SkinFacePattern.None; d.BellyStyle = SkinBellyStyle.HeartChest;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.ThickPlush; d.Accessory = SkinAccessory.CollarBell;
                d.AccessoryColor = "#5CC4A0";
                return d;

            case "cow":
                d.Name = "奶牛猫";
                d.BodyBase = SkinBodyBase.ShortHair;
                d.FurColor = "#FAF8F4"; d.AccentColor = "#26252A"; d.BellyColor = "#FAF8F4";
                d.PawColor = "#FAF8F4"; d.InnerEarColor = "#F2B2B0"; d.NoseColor = "#EA9498";
                d.WhiskerColor = "#B8B2AA"; d.LeftEyeColor = "#9AD64C"; d.RightEyeColor = "#9AD64C";
                d.PupilStyle = SkinPupilStyle.BeadMatte;
                d.FacePattern = SkinFacePattern.CowSpots; d.BellyStyle = SkinBellyStyle.None;
                d.BodyPattern = SkinBodyPattern.CowMottled; d.PawStyle = SkinPawStyle.BodyColor;
                d.TailStyle = SkinTailStyle.DarkTip; d.Accessory = SkinAccessory.CollarBell;
                d.AccessoryColor = "#E46652";
                return d;

            case "tuxedo":
                d.Name = "黑警长奶牛";
                d.BodyBase = SkinBodyBase.ShortHair;
                d.FurColor = "#26252A"; d.AccentColor = "#1B1A1E"; d.BellyColor = "#FAF8F4";
                d.PawColor = "#FAF8F4"; d.InnerEarColor = "#E6A6A4"; d.NoseColor = "#E49498";
                d.WhiskerColor = "#F4F2EE"; d.LeftEyeColor = "#F0B832"; d.RightEyeColor = "#F0B832";
                d.PupilStyle = SkinPupilStyle.RoundCute;
                d.FacePattern = SkinFacePattern.Tuxedo; d.BellyStyle = SkinBellyStyle.TuxedoV;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.LightTip; d.Accessory = SkinAccessory.BowTie;
                d.AccessoryColor = "#E46652";
                return d;

            case "wednesday": return FromBuiltIn(CatStyle.Wednesday, "星期三二创");
            case "ginger": return FromBuiltIn(CatStyle.Ginger, "奶油橘二创");
            case "coffee": return FromBuiltIn(CatStyle.Coffee, "咖啡二创");
            case "silver": return FromBuiltIn(CatStyle.SilverShaded, "银渐层二创");
            case "siamese": return FromBuiltIn(CatStyle.Siamese, "暹罗二创");
            case "calico": return FromBuiltIn(CatStyle.JapaneseBobtail, "三花二创");
            case "tabby": return FromBuiltIn(CatStyle.PhotoTabby, "长毛虎斑二创");
            default: return FromBuiltIn(CatStyle.Wednesday, "我的小猫");
        }
    }

    public static CatSkinData FromBuiltIn(CatStyle style, string customName)
    {
        CatSkinData d = new CatSkinData
        {
            Id = NewId(),
            Name = string.IsNullOrEmpty(customName) ? "自定义小猫" : customName
        };
        if (style == CatStyle.Black) style = CatStyle.Wednesday;
        switch (style)
        {
            case CatStyle.Wednesday:
                d.BodyBase = SkinBodyBase.ShortHair;
                d.FurColor = "#262428"; d.AccentColor = "#1B191D"; d.BellyColor = "#262428";
                d.PawColor = "#262428"; d.InnerEarColor = "#3B353B"; d.NoseColor = "#443C42";
                d.WhiskerColor = "#555054"; d.LeftEyeColor = "#F2BF3C"; d.RightEyeColor = "#F2BF3C";
                d.PupilStyle = SkinPupilStyle.BeadMatte;
                d.FacePattern = SkinFacePattern.None; d.BellyStyle = SkinBellyStyle.None;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.BodyColor;
                d.TailStyle = SkinTailStyle.Solid; d.Accessory = SkinAccessory.CollarBell;
                d.AccessoryColor = "#E65A4F";
                break;
            case CatStyle.Ginger:
                d.BodyBase = SkinBodyBase.ShortHair;
                d.FurColor = "#F0A860"; d.AccentColor = "#C26F38"; d.BellyColor = "#FFE7C3";
                d.PawColor = "#FFE8C8"; d.InnerEarColor = "#EE9A8F"; d.NoseColor = "#DF8480";
                d.WhiskerColor = "#926B4D"; d.LeftEyeColor = "#C8AF68"; d.RightEyeColor = "#C8AF68";
                d.FacePattern = SkinFacePattern.TabbyM; d.BellyStyle = SkinBellyStyle.Bib;
                d.BodyPattern = SkinBodyPattern.FlankStripes; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.TabbyRings; d.Accessory = SkinAccessory.HeadOrange;
                d.AccessoryColor = "#F59824";
                break;
            case CatStyle.Coffee:
                d.BodyBase = SkinBodyBase.ShortHair;
                d.FurColor = "#BD8952"; d.AccentColor = "#634328"; d.BellyColor = "#FFFDF9";
                d.PawColor = "#FFFDF9"; d.InnerEarColor = "#ECC8A4"; d.NoseColor = "#EC989B";
                d.WhiskerColor = "#FCF8F2"; d.LeftEyeColor = "#F2B528"; d.RightEyeColor = "#F2B528";
                d.FacePattern = SkinFacePattern.CoffeeMask; d.BellyStyle = SkinBellyStyle.FullBelly;
                d.BodyPattern = SkinBodyPattern.FlankStripes; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.TabbyRings; d.DarkEarTips = true;
                d.Accessory = SkinAccessory.None;
                break;
            case CatStyle.SilverShaded:
                d.BodyBase = SkinBodyBase.ChubbyBritish;
                d.FurColor = "#E4E7EA"; d.AccentColor = "#9AA0A8"; d.BellyColor = "#F7F8FA";
                d.PawColor = "#F7F8FA"; d.InnerEarColor = "#E6B4AE"; d.NoseColor = "#DF928A";
                d.WhiskerColor = "#9CA2A8"; d.LeftEyeColor = "#6CC07B"; d.RightEyeColor = "#6CC07B";
                d.FacePattern = SkinFacePattern.None; d.BellyStyle = SkinBellyStyle.FullBelly;
                d.BodyPattern = SkinBodyPattern.FlankStripes; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.DarkTip; d.Accessory = SkinAccessory.BowTie;
                d.AccessoryColor = "#5B8EC9";
                break;
            case CatStyle.Siamese:
                d.BodyBase = SkinBodyBase.ShortHair;
                d.FurColor = "#E7D9C6"; d.AccentColor = "#4C3530"; d.BellyColor = "#F3EADF";
                d.PawColor = "#523B35"; d.InnerEarColor = "#85605A"; d.NoseColor = "#3B2727";
                d.WhiskerColor = "#9E8A7E"; d.LeftEyeColor = "#8DC0E6"; d.RightEyeColor = "#8DC0E6";
                d.FacePattern = SkinFacePattern.SiamesePoint; d.BellyStyle = SkinBellyStyle.Bib;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.DarkPoints;
                d.TailStyle = SkinTailStyle.DarkTip; d.Accessory = SkinAccessory.Glasses;
                d.AccessoryColor = "#6D543D";
                break;
            case CatStyle.JapaneseBobtail:
                d.BodyBase = SkinBodyBase.ShortHair;
                d.FurColor = "#FAF6EE"; d.AccentColor = "#DF924C"; d.BellyColor = "#FFFDF8";
                d.PawColor = "#FFFDF8"; d.InnerEarColor = "#F0A9A0"; d.NoseColor = "#E58D90";
                d.WhiskerColor = "#9A8B7C"; d.LeftEyeColor = "#D49B46"; d.RightEyeColor = "#6DB4DF";
                d.Heterochromia = true;
                d.FacePattern = SkinFacePattern.Calico; d.BellyStyle = SkinBellyStyle.FullBelly;
                d.BodyPattern = SkinBodyPattern.Patches; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.BobtailPom; d.Accessory = SkinAccessory.HeadRibbon;
                d.AccessoryColor = "#E66678";
                break;
            case CatStyle.PhotoTabby:
                d.BodyBase = SkinBodyBase.FluffyLong;
                d.FurColor = "#A88D6D"; d.AccentColor = "#695440"; d.BellyColor = "#F5EEDC";
                d.PawColor = "#F5EEDC"; d.InnerEarColor = "#E4B8A8"; d.NoseColor = "#C97A7E";
                d.WhiskerColor = "#F3E8D4"; d.LeftEyeColor = "#A6AD5E"; d.RightEyeColor = "#A6AD5E";
                d.FacePattern = SkinFacePattern.TabbyM; d.BellyStyle = SkinBellyStyle.Ruff;
                d.BodyPattern = SkinBodyPattern.FlankStripes; d.PawStyle = SkinPawStyle.TabbyBracelets;
                d.TailStyle = SkinTailStyle.TabbyRings; d.Accessory = SkinAccessory.None;
                break;
        }
        return d;
    }

    public static string NewId()
    {
        return "skin_" + Guid.NewGuid().ToString("N").Substring(0, 10);
    }

    public XElement ToXml()
    {
        return new XElement("catskin",
            new XAttribute("version", 1),
            new XAttribute("id", string.IsNullOrEmpty(Id) ? NewId() : Id),
            new XAttribute("name", string.IsNullOrEmpty(Name) ? "自定义小猫" : Name.Trim()),
            new XAttribute("bodyBase", BodyBase),
            new XAttribute("fur", NormalizeHex(FurColor, "#757F8C")),
            new XAttribute("accent", NormalizeHex(AccentColor, "#4C5562")),
            new XAttribute("belly", NormalizeHex(BellyColor, "#FFFDF8")),
            new XAttribute("paw", NormalizeHex(PawColor, "#FFFDF8")),
            new XAttribute("innerEar", NormalizeHex(InnerEarColor, "#D8A4A0")),
            new XAttribute("nose", NormalizeHex(NoseColor, "#5B6068")),
            new XAttribute("whisker", NormalizeHex(WhiskerColor, "#E6E9EE")),
            new XAttribute("leftEye", NormalizeHex(LeftEyeColor, "#EB9D28")),
            new XAttribute("heterochromia", Heterochromia),
            new XAttribute("rightEye", NormalizeHex(RightEyeColor, "#6BB8E8")),
            new XAttribute("pupil", PupilStyle),
            new XAttribute("face", FacePattern),
            new XAttribute("bellyStyle", BellyStyle),
            new XAttribute("bodyPattern", BodyPattern),
            new XAttribute("pawStyle", PawStyle),
            new XAttribute("tailStyle", TailStyle),
            new XAttribute("darkEarTips", DarkEarTips),
            new XAttribute("accessory", Accessory),
            new XAttribute("accessoryColor", NormalizeHex(AccessoryColor, "#E46652"))
        );
    }

    public static CatSkinData FromXml(XElement root)
    {
        if (root == null || !string.Equals(root.Name.LocalName, "catskin", StringComparison.OrdinalIgnoreCase))
            return null;
        CatSkinData d = new CatSkinData();
        string id = ((string)root.Attribute("id") ?? "").Trim();
        d.Id = string.IsNullOrEmpty(id) ? NewId() : SanitizeId(id);
        string name = ((string)root.Attribute("name") ?? "").Trim();
        if (name.Length > 18) name = name.Substring(0, 18);
        d.Name = string.IsNullOrEmpty(name) ? "自定义小猫" : name;
        d.BodyBase = ReadEnum(root, "bodyBase", d.BodyBase);
        d.FurColor = NormalizeHex((string)root.Attribute("fur"), d.FurColor);
        d.AccentColor = NormalizeHex((string)root.Attribute("accent"), d.AccentColor);
        d.BellyColor = NormalizeHex((string)root.Attribute("belly"), d.BellyColor);
        d.PawColor = NormalizeHex((string)root.Attribute("paw"), d.PawColor);
        d.InnerEarColor = NormalizeHex((string)root.Attribute("innerEar"), d.InnerEarColor);
        d.NoseColor = NormalizeHex((string)root.Attribute("nose"), d.NoseColor);
        d.WhiskerColor = NormalizeHex((string)root.Attribute("whisker"), d.WhiskerColor);
        d.LeftEyeColor = NormalizeHex((string)root.Attribute("leftEye"), d.LeftEyeColor);
        d.Heterochromia = (bool?)root.Attribute("heterochromia") ?? false;
        d.RightEyeColor = NormalizeHex((string)root.Attribute("rightEye"), d.LeftEyeColor);
        d.PupilStyle = ReadEnum(root, "pupil", d.PupilStyle);
        d.FacePattern = ReadEnum(root, "face", d.FacePattern);
        d.BellyStyle = ReadEnum(root, "bellyStyle", d.BellyStyle);
        d.BodyPattern = ReadEnum(root, "bodyPattern", d.BodyPattern);
        d.PawStyle = ReadEnum(root, "pawStyle", d.PawStyle);
        d.TailStyle = ReadEnum(root, "tailStyle", d.TailStyle);
        d.DarkEarTips = (bool?)root.Attribute("darkEarTips") ?? false;
        d.Accessory = ReadEnum(root, "accessory", d.Accessory);
        d.AccessoryColor = NormalizeHex((string)root.Attribute("accessoryColor"), d.AccessoryColor);
        return d;
    }

    internal static string SanitizeId(string id)
    {
        char[] chars = id.ToCharArray();
        List<char> clean = new List<char>(chars.Length);
        for (int i = 0; i < chars.Length; ++i)
        {
            char c = chars[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-')
                clean.Add(c);
        }
        return clean.Count == 0 ? NewId() : new string(clean.ToArray());
    }

    private static T ReadEnum<T>(XElement el, string attr, T fallback) where T : struct
    {
        T parsed;
        return Enum.TryParse((string)el.Attribute(attr), true, out parsed) && Enum.IsDefined(typeof(T), parsed)
            ? parsed : fallback;
    }
}

internal static class CustomSkinStore
{
    public static string SkinsFolder
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "KeycatDesktop", "skins");
        }
    }

    public static List<CatSkinData> LoadAll()
    {
        List<CatSkinData> list = new List<CatSkinData>();
        try
        {
            if (!Directory.Exists(SkinsFolder)) return list;
            string[] files = Directory.GetFiles(SkinsFolder, "*.catskin");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; ++i)
            {
                try
                {
                    XElement root = XElement.Load(files[i]);
                    CatSkinData skin = CatSkinData.FromXml(root);
                    if (skin != null) list.Add(skin);
                }
                catch (Exception) { }
            }
        }
        catch (Exception) { }
        return list;
    }

    public static CatSkinData FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        string path = PathForId(id);
        try
        {
            if (File.Exists(path))
                return CatSkinData.FromXml(XElement.Load(path));
        }
        catch (Exception) { }
        List<CatSkinData> all = LoadAll();
        for (int i = 0; i < all.Count; ++i)
            if (string.Equals(all[i].Id, id, StringComparison.OrdinalIgnoreCase)) return all[i];
        return null;
    }

    public static bool Save(CatSkinData skin)
    {
        if (skin == null) return false;
        if (string.IsNullOrEmpty(skin.Id)) skin.Id = CatSkinData.NewId();
        skin.Id = CatSkinData.SanitizeId(skin.Id);
        try
        {
            Directory.CreateDirectory(SkinsFolder);
            string dest = PathForId(skin.Id);
            string tmp = dest + ".tmp";
            skin.ToXml().Save(tmp);
            if (File.Exists(dest)) File.Replace(tmp, dest, null);
            else File.Move(tmp, dest);
            return true;
        }
        catch (Exception) { return false; }
    }

    public static bool Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        try
        {
            string dest = PathForId(id);
            if (File.Exists(dest)) File.Delete(dest);
            return true;
        }
        catch (Exception) { return false; }
    }

    public static bool ExportToFile(CatSkinData skin, string filePath)
    {
        if (skin == null || string.IsNullOrEmpty(filePath)) return false;
        try
        {
            skin.ToXml().Save(filePath);
            return true;
        }
        catch (Exception) { return false; }
    }

    public static CatSkinData ImportFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;
        try
        {
            XElement root = XElement.Load(filePath);
            CatSkinData skin = CatSkinData.FromXml(root);
            if (skin == null) return null;
            // Assign a fresh ID if one already exists locally so importing never overwrites an existing skin unexpectedly.
            if (File.Exists(PathForId(skin.Id))) skin.Id = CatSkinData.NewId();
            if (!Save(skin)) return null;
            return skin;
        }
        catch (Exception) { return null; }
    }

    private static string PathForId(string id)
    {
        return Path.Combine(SkinsFolder, CatSkinData.SanitizeId(id) + ".catskin");
    }
}
