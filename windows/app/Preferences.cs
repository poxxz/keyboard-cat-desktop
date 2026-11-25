using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

internal sealed class Preferences
{
    public const int MinPercent = 1, MaxPercent = 200;
    public double X = double.NaN, Y = double.NaN, Scale = 1.0;
    public bool Labels = true, ClickThrough = false, OnTop = true, PasswordMode = true;
    public int IdleMinutes = 5;
    public CatStyle Cat = CatStyle.Black;
    public KeyboardStyle Keyboard = KeyboardStyle.Classic;
    public MouseStyle Mouse = MouseStyle.Classic;
    public GamepadStyle Gamepad = GamepadStyle.Xbox;
    public GamepadMode Mode = GamepadMode.Auto;
    private static string FileName
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                  "KeycatDesktop", "preferences.xml"); }
    }
    public static Preferences Load()
    {
        Preferences p = new Preferences();
        try
        {
            if (File.Exists(FileName))
            {
                XElement root = XElement.Load(FileName);
                p.X = (double?)root.Attribute("x") ?? double.NaN;
                p.Y = (double?)root.Attribute("y") ?? double.NaN;
                double scale;
                if (double.TryParse((string)root.Attribute("scale"), NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out scale) && Finite(scale))
                    p.Scale = Math.Max(MinPercent / 100.0, Math.Min(MaxPercent / 100.0, scale));
                p.Labels = (bool?)root.Attribute("labels") ?? true;
                p.ClickThrough = (bool?)root.Attribute("clickThrough") ?? false;
                p.OnTop = (bool?)root.Attribute("onTop") ?? true;
                p.PasswordMode = (bool?)root.Attribute("passwordMode") ?? true;
                int idle = (int?)root.Attribute("idleMinutes") ?? 5;
                p.IdleMinutes = Math.Max(0, Math.Min(60, idle));
                p.Cat = ReadStyle(root, "catStyle", p.Cat);
                p.Keyboard = ReadStyle(root, "keyboard", p.Keyboard);
                p.Mouse = ReadStyle(root, "mouse", p.Mouse);
                p.Gamepad = ReadStyle(root, "gamepad", p.Gamepad);
                p.Mode = ReadStyle(root, "gamepadMode", p.Mode);
            }
        }
        catch (Exception) { return new Preferences(); }
        return p;
    }
    public static bool Finite(double number) { return !double.IsInfinity(number) && !double.IsNaN(number); }
    private static T ReadStyle<T>(XElement settings, string attribute, T fallback) where T : struct
    {
        T value;
        return Enum.TryParse((string)settings.Attribute(attribute), out value) && Enum.IsDefined(typeof(T), value)
            ? value : fallback;
    }
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FileName));
            var settings = new XElement("cat", new XAttribute("x", X), new XAttribute("y", Y),
                new XAttribute("scale", Scale), new XAttribute("labels", Labels),
                new XAttribute("clickThrough", ClickThrough), new XAttribute("onTop", OnTop),
                new XAttribute("passwordMode", PasswordMode), new XAttribute("idleMinutes", IdleMinutes),
                new XAttribute("catStyle", Cat),
                new XAttribute("keyboard", Keyboard), new XAttribute("mouse", Mouse),
                new XAttribute("gamepad", Gamepad), new XAttribute("gamepadMode", Mode));
            string tmp = FileName + ".tmp";
            settings.Save(tmp);
            if (File.Exists(FileName)) File.Replace(tmp, FileName, null);
            else File.Move(tmp, FileName);
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }
}
