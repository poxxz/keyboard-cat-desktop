using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Xml.Linq;

public enum SkinBodyBase { ShortHair, FluffyLong }
public enum SkinFacePattern { None, CoffeeMask, TabbyM, SiamesePoint, Tuxedo, Calico, CowSpots }
public enum SkinBellyStyle { None, Bib, FullBelly, Ruff }
public enum SkinBodyPattern { None, FlankStripes, Patches }
public enum SkinPawStyle { BodyColor, Socks, DarkPoints }
public enum SkinTailStyle { Solid, TabbyRings, LightTip }
public enum SkinPupilStyle { SleekOval, RoundCute }
public enum SkinAccessory { None, CollarBell, BowTie, HeadRibbon, Glasses, HeadOrange, Ahoge }

// Pure-vector user-customizable cat skin data (.catskin XML).
internal sealed class CatSkinData
{
    public string Id = "";
    public string Name = "我的小猫";
    public SkinBodyBase BodyBase = SkinBodyBase.ShortHair;
    public string FurColor = "#3A373C";
    public string AccentColor = "#D4844A";
    public string BellyColor = "#FFFBF4";
    public string PawColor = "#FFFBF4";
    public string InnerEarColor = "#F0A89E";
    public string NoseColor = "#E68E92";
    public string WhiskerColor = "#ECE6DC";
    public string LeftEyeColor = "#F2B632";
    public bool Heterochromia = false;
    public string RightEyeColor = "#6BB8E8";
    public SkinPupilStyle PupilStyle = SkinPupilStyle.SleekOval;
    public SkinFacePattern FacePattern = SkinFacePattern.Tuxedo;
    public SkinBellyStyle BellyStyle = SkinBellyStyle.FullBelly;
    public SkinBodyPattern BodyPattern = SkinBodyPattern.None;
    public SkinPawStyle PawStyle = SkinPawStyle.Socks;
    public SkinTailStyle TailStyle = SkinTailStyle.Solid;
    public bool DarkEarTips = false;
    public SkinAccessory Accessory = SkinAccessory.CollarBell;
    public string AccessoryColor = "#E46652";

    public CatSkinData Clone()
    {
        return (CatSkinData)MemberwiseClone();
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

    public static CatSkinData FromBuiltIn(CatStyle style, string customName)
    {
        CatSkinData d = new CatSkinData
        {
            Id = NewId(),
            Name = string.IsNullOrEmpty(customName) ? "自定义小猫" : customName
        };
        switch (style)
        {
            case CatStyle.Black:
                d.FurColor = "#242226"; d.AccentColor = "#1B191D"; d.BellyColor = "#242226";
                d.PawColor = "#242226"; d.InnerEarColor = "#3B353B"; d.NoseColor = "#443C42";
                d.WhiskerColor = "#4B484A"; d.LeftEyeColor = "#C9EC4A"; d.RightEyeColor = "#C9EC4A";
                d.FacePattern = SkinFacePattern.None; d.BellyStyle = SkinBellyStyle.None;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.BodyColor;
                d.TailStyle = SkinTailStyle.Solid; d.Accessory = SkinAccessory.None;
                break;
            case CatStyle.Wednesday:
                d.FurColor = "#262428"; d.AccentColor = "#1B191D"; d.BellyColor = "#262428";
                d.PawColor = "#262428"; d.InnerEarColor = "#3B353B"; d.NoseColor = "#443C42";
                d.WhiskerColor = "#555054"; d.LeftEyeColor = "#F2BF3C"; d.RightEyeColor = "#F2BF3C";
                d.FacePattern = SkinFacePattern.None; d.BellyStyle = SkinBellyStyle.None;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.BodyColor;
                d.TailStyle = SkinTailStyle.Solid; d.Accessory = SkinAccessory.CollarBell;
                d.AccessoryColor = "#E65A4F";
                break;
            case CatStyle.Ginger:
                d.FurColor = "#F0A860"; d.AccentColor = "#C26F38"; d.BellyColor = "#FFE7C3";
                d.PawColor = "#FFE8C8"; d.InnerEarColor = "#EE9A8F"; d.NoseColor = "#DF8480";
                d.WhiskerColor = "#926B4D"; d.LeftEyeColor = "#C8AF68"; d.RightEyeColor = "#C8AF68";
                d.FacePattern = SkinFacePattern.TabbyM; d.BellyStyle = SkinBellyStyle.Bib;
                d.BodyPattern = SkinBodyPattern.FlankStripes; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.TabbyRings; d.Accessory = SkinAccessory.HeadOrange;
                d.AccessoryColor = "#F59824";
                break;
            case CatStyle.Coffee:
                d.FurColor = "#BD8952"; d.AccentColor = "#634328"; d.BellyColor = "#FFFDF9";
                d.PawColor = "#FFFDF9"; d.InnerEarColor = "#ECC8A4"; d.NoseColor = "#EC989B";
                d.WhiskerColor = "#FCF8F2"; d.LeftEyeColor = "#F2B528"; d.RightEyeColor = "#F2B528";
                d.FacePattern = SkinFacePattern.CoffeeMask; d.BellyStyle = SkinBellyStyle.FullBelly;
                d.BodyPattern = SkinBodyPattern.FlankStripes; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.TabbyRings; d.DarkEarTips = true;
                d.Accessory = SkinAccessory.None;
                break;
            case CatStyle.SilverShaded:
                d.FurColor = "#E4E7EA"; d.AccentColor = "#9AA0A8"; d.BellyColor = "#F7F8FA";
                d.PawColor = "#F7F8FA"; d.InnerEarColor = "#E6B4AE"; d.NoseColor = "#DF928A";
                d.WhiskerColor = "#9CA2A8"; d.LeftEyeColor = "#6CC07B"; d.RightEyeColor = "#6CC07B";
                d.FacePattern = SkinFacePattern.None; d.BellyStyle = SkinBellyStyle.FullBelly;
                d.BodyPattern = SkinBodyPattern.FlankStripes; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.TabbyRings; d.Accessory = SkinAccessory.BowTie;
                d.AccessoryColor = "#5B8EC9";
                break;
            case CatStyle.Siamese:
                d.FurColor = "#E7D9C6"; d.AccentColor = "#4C3530"; d.BellyColor = "#F3EADF";
                d.PawColor = "#523B35"; d.InnerEarColor = "#85605A"; d.NoseColor = "#3B2727";
                d.WhiskerColor = "#9E8A7E"; d.LeftEyeColor = "#8DC0E6"; d.RightEyeColor = "#8DC0E6";
                d.FacePattern = SkinFacePattern.SiamesePoint; d.BellyStyle = SkinBellyStyle.Bib;
                d.BodyPattern = SkinBodyPattern.None; d.PawStyle = SkinPawStyle.DarkPoints;
                d.TailStyle = SkinTailStyle.Solid; d.Accessory = SkinAccessory.Glasses;
                d.AccessoryColor = "#6D543D";
                break;
            case CatStyle.JapaneseBobtail:
                d.FurColor = "#FAF6EE"; d.AccentColor = "#DF924C"; d.BellyColor = "#FFFDF8";
                d.PawColor = "#FFFDF8"; d.InnerEarColor = "#F0A9A0"; d.NoseColor = "#E58D90";
                d.WhiskerColor = "#9A8B7C"; d.LeftEyeColor = "#D49B46"; d.RightEyeColor = "#6DB4DF";
                d.Heterochromia = true;
                d.FacePattern = SkinFacePattern.Calico; d.BellyStyle = SkinBellyStyle.FullBelly;
                d.BodyPattern = SkinBodyPattern.Patches; d.PawStyle = SkinPawStyle.Socks;
                d.TailStyle = SkinTailStyle.LightTip; d.Accessory = SkinAccessory.HeadRibbon;
                d.AccessoryColor = "#E66678";
                break;
            case CatStyle.PhotoTabby:
                d.BodyBase = SkinBodyBase.FluffyLong;
                d.FurColor = "#A88D6D"; d.AccentColor = "#695440"; d.BellyColor = "#F5EEDC";
                d.PawColor = "#F5EEDC"; d.InnerEarColor = "#E4B8A8"; d.NoseColor = "#C97A7E";
                d.WhiskerColor = "#F3E8D4"; d.LeftEyeColor = "#A6AD5E"; d.RightEyeColor = "#A6AD5E";
                d.FacePattern = SkinFacePattern.TabbyM; d.BellyStyle = SkinBellyStyle.Ruff;
                d.BodyPattern = SkinBodyPattern.FlankStripes; d.PawStyle = SkinPawStyle.Socks;
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
            new XAttribute("fur", NormalizeHex(FurColor, "#3A373C")),
            new XAttribute("accent", NormalizeHex(AccentColor, "#D4844A")),
            new XAttribute("belly", NormalizeHex(BellyColor, "#FFFBF4")),
            new XAttribute("paw", NormalizeHex(PawColor, "#FFFBF4")),
            new XAttribute("innerEar", NormalizeHex(InnerEarColor, "#F0A89E")),
            new XAttribute("nose", NormalizeHex(NoseColor, "#E68E92")),
            new XAttribute("whisker", NormalizeHex(WhiskerColor, "#ECE6DC")),
            new XAttribute("leftEye", NormalizeHex(LeftEyeColor, "#F2B632")),
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
