using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;
using Imaging = System.Drawing.Imaging;
using Forms = System.Windows.Forms;

internal sealed class StyleMenu : IDisposable
{
    private readonly Dictionary<Enum, Drawing.Bitmap> images = new Dictionary<Enum, Drawing.Bitmap>();

    public Forms.ToolStripMenuItem Cats(CatStyle selected, Action<CatStyle> choose)
    {
        var parent = Group("猫咪样式", 29);
        Add(parent, "经典黑猫", CatStyle.Black, selected, choose);
        Add(parent, "奶油橘猫", CatStyle.Ginger, selected, choose);
        Add(parent, "日本短尾三花", CatStyle.JapaneseBobtail, selected, choose);
        Add(parent, "长毛虎斑", CatStyle.PhotoTabby, selected, choose);
        Add(parent, "星期三", CatStyle.Wednesday, selected, choose);
        Add(parent, "咖啡", CatStyle.Coffee, selected, choose);
        Add(parent, "英短银渐层", CatStyle.SilverShaded, selected, choose);
        Add(parent, "暹罗猫", CatStyle.Siamese, selected, choose);
        return parent;
    }

    public Forms.ToolStripMenuItem Keyboards(KeyboardStyle selected, Action<KeyboardStyle> choose)
    {
        var parent = Group("键盘样式", 94);
        Add(parent, "经典奶油", KeyboardStyle.Classic, selected, choose);
        Add(parent, "薄荷打字机", KeyboardStyle.Round, selected, choose);
        Add(parent, "冰蓝电竞", KeyboardStyle.Neon, selected, choose);
        Add(parent, "胡桃木", KeyboardStyle.Wood, selected, choose);
        return parent;
    }

    public Forms.ToolStripMenuItem Mice(MouseStyle selected, Action<MouseStyle> choose)
    {
        var parent = Group("鼠标样式", 29);
        Add(parent, "奶油滚轮", MouseStyle.Classic, selected, choose);
        Add(parent, "雾紫鹅卵石", MouseStyle.Pebble, selected, choose);
        Add(parent, "冰蓝电竞", MouseStyle.Gaming, selected, choose);
        Add(parent, "酒红轨迹球", MouseStyle.Trackball, selected, choose);
        return parent;
    }

    public Forms.ToolStripMenuItem Gamepads(GamepadStyle selected, Action<GamepadStyle> choose)
    {
        var parent = Group("手柄款式", 94);
        Add(parent, "Xbox 手柄", GamepadStyle.Xbox, selected, choose);
        Add(parent, "PS5 手柄", GamepadStyle.PS5, selected, choose);
        return parent;
    }

    private static Forms.ToolStripMenuItem Group(string title, int imageWidth)
    {
        var group = new Forms.ToolStripMenuItem(title);
        var list = (Forms.ToolStripDropDownMenu)group.DropDown;
        list.ShowCheckMargin = true;
        list.ShowImageMargin = true;
        list.ImageScalingSize = new Drawing.Size(imageWidth, 26);
        return group;
    }

    private void Add<T>(Forms.ToolStripMenuItem parent, string title, T value, T selected, Action<T> choose)
        where T : struct
    {
        Enum key = (Enum)(object)value;
        Drawing.Bitmap preview;
        if (!images.TryGetValue(key, out preview))
        {
            preview = Thumbnail(key);
            images.Add(key, preview);
        }
        var item = new Forms.ToolStripMenuItem(title, preview);
        item.Checked = value.Equals(selected);
        item.ImageScaling = Forms.ToolStripItemImageScaling.None;
        item.Click += delegate { choose(value); };
        parent.DropDownItems.Add(item);
    }

    private static Drawing.Bitmap Thumbnail(Enum style)
    {
        bool keyboard = style is KeyboardStyle;
        bool gamepad = style is GamepadStyle;
        int width = keyboard || gamepad ? 94 : 29;
        const int height = 26;
        var art = new DrawingGroup();
        using (DrawingContext dc = art.Open())
        {
            if (keyboard) DeskArt.DrawKeyboard(dc, (KeyboardStyle)(object)style, null);
            else if (style is CatStyle) CatArt.DrawPortrait(dc, (CatStyle)(object)style);
            else if (gamepad) GamepadArt.Draw(dc, (GamepadStyle)(object)style, new GamepadState());
            else MouseArt.Draw(dc, (MouseStyle)(object)style, 0);
        }
        Rect bounds = art.Bounds;
        double scale = Math.Min((width - 2) / bounds.Width, (height - 2) / bounds.Height);
        var drawing = new DrawingVisual();
        using (DrawingContext dc = drawing.RenderOpen())
        {
            dc.PushTransform(new MatrixTransform(scale, 0, 0, scale,
                (width - scale * bounds.Width) / 2 - scale * bounds.X,
                (height - scale * bounds.Height) / 2 - scale * bounds.Y));
            dc.DrawDrawing(art);
            dc.Pop();
        }
        var render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        render.Render(drawing);
        var image = new Drawing.Bitmap(width, height, Imaging.PixelFormat.Format32bppPArgb);
        var area = new Drawing.Rectangle(0, 0, width, height);
        var pixels = image.LockBits(area, Imaging.ImageLockMode.WriteOnly, image.PixelFormat);
        try { render.CopyPixels(Int32Rect.Empty, pixels.Scan0, height * pixels.Stride, pixels.Stride); }
        finally { image.UnlockBits(pixels); }
        return image;
    }

    public void Dispose()
    {
        foreach (Drawing.Bitmap image in images.Values) image.Dispose();
        images.Clear();
    }
}
