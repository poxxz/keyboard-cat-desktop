using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

public struct CatMood
{
    public double Sweat;
    public double Anger;
    public double Sleep;
    public double Crouch;
}

public static partial class CatArt
{
    internal static Brush WashAbsolute(byte[] light, byte[] dark, Point from, Point to)
    {
        LinearGradientBrush brush = new LinearGradientBrush(
            Color.FromRgb(light[0], light[1], light[2]),
            Color.FromRgb(dark[0], dark[1], dark[2]), from, to);
        brush.MappingMode = BrushMappingMode.Absolute;
        brush.Freeze();
        return brush;
    }

    internal static Brush ArmGradient(Color shoulder, Color mid, double midStop, Color wrist, Point from, Point to)
    {
        LinearGradientBrush brush = new LinearGradientBrush();
        brush.MappingMode = BrushMappingMode.Absolute;
        brush.StartPoint = from;
        brush.EndPoint = to;
        brush.GradientStops.Add(new GradientStop(shoulder, 0));
        brush.GradientStops.Add(new GradientStop(mid, midStop));
        brush.GradientStops.Add(new GradientStop(wrist, 1));
        brush.Freeze();
        return brush;
    }

    internal static Brush EdgeFadeMask(Point from, Point to)
    {
        LinearGradientBrush mask = new LinearGradientBrush();
        mask.MappingMode = BrushMappingMode.Absolute;
        mask.StartPoint = from;
        mask.EndPoint = to;
        mask.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0));
        mask.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.22));
        mask.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 255, 255), 0.48));
        mask.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 255, 255), 1.0));
        mask.Freeze();
        return mask;
    }

    private static readonly Geometry SweatDropShape = Shape(
        "M 0,-9.8 C 2.3,-4.6 6.5,0.8 6.5,4.9 C 6.5,8.6 3.6,11.2 0,11.2 " +
        "C -3.6,11.2 -6.5,8.6 -6.5,4.9 C -6.5,0.8 -2.3,-4.6 0,-9.8 Z");
    private static readonly Brush SweatDropFill = Wash(new byte[] { 168, 240, 255 }, new byte[] { 30, 150, 245 }, new Point(.2, 0), new Point(.8, 1));
    private static readonly Pen SweatWhiteHalo = Stroke(Ink(255, 255, 255), 1.7);
    private static readonly Pen SweatBlueEdge = Stroke(Ink(20, 98, 166), .72);
    private static readonly Brush SweatGleam1 = AlphaBrush(235, 255, 255, 255);
    private static readonly Brush SweatGleam2 = AlphaBrush(198, 255, 255, 255);

    private static readonly Geometry[] AngerArcs = new Geometry[]
    {
        Shape("M -2.5,-9.8 Q -2.3,-2.3 -9.8,-2.5"),
        Shape("M 2.5,-9.8 Q 2.3,-2.3 9.8,-2.5"),
        Shape("M -2.5,9.8 Q -2.3,2.3 -9.8,2.5"),
        Shape("M 2.5,9.8 Q 2.3,2.3 9.8,2.5")
    };
    private static readonly Pen AngerWhiteHalo = Stroke(Ink(255, 255, 255), 4.8);
    private static readonly Brush AngerAuraBrush = BuildAngerAura();

    private static readonly Typeface ZzzFace = new Typeface(
        new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
    private static readonly Geometry ZzzSmall = BuildGlyphGeometry("z", 13.0);
    private static readonly Geometry ZzzMid = BuildGlyphGeometry("z", 16.5);
    private static readonly Geometry ZzzLarge = BuildGlyphGeometry("Z", 20.5);
    private static readonly Brush ZzzFill = Ink(255, 253, 244);
    private static readonly Pen ZzzOutline = Stroke(Ink(53, 80, 120), 3.0);

    private static readonly Brush BugGlowBrush = BuildBugGlow();
    private static readonly Brush BugWing1 = AlphaBrush(235, 240, 250, 255);
    private static readonly Brush BugWing2 = AlphaBrush(210, 240, 250, 255);
    private static readonly Pen BugWingEdge = Stroke(Ink(107, 140, 168), .65);
    private static readonly Brush BugAbdomen = Ink(255, 225, 90);
    private static readonly Pen BugAbdomenEdge = Stroke(Ink(90, 70, 36), .65);
    private static readonly Brush BugHead = Ink(74, 62, 54);
    private static readonly Pen BugAntennaPen = Stroke(Ink(74, 62, 54), .7);
    private static readonly Geometry BugAntennae = Shape("M 3.6,-1.2 Q 6.0,-4.0 7.8,-3.1 M 3.9,-0.2 Q 6.4,-1.6 8.0,-0.4");
    private static readonly Pen SwatArcOuter = Stroke(Ink(255, 246, 184), 1.9);
    private static readonly Pen SwatArcInner = Stroke(Ink(255, 255, 255), 1.25);
    private static readonly Geometry SwatOuterGeo = Shape("M -15,-11 Q -2,-19 13,-10");
    private static readonly Geometry SwatInnerGeo = Shape("M -12,-5 Q 0,-11 10,-5");

    private static Brush AlphaBrush(byte a, byte r, byte g, byte b)
    {
        SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }

    private static Brush BuildAngerAura()
    {
        RadialGradientBrush brush = new RadialGradientBrush();
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(215, 255, 65, 50), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(90, 255, 40, 40), .52));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 30, 30), 1));
        brush.Freeze();
        return brush;
    }

    private static Brush BuildBugGlow()
    {
        RadialGradientBrush brush = new RadialGradientBrush();
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(185, 255, 245, 130), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(72, 255, 220, 80), .55));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 220, 80), 1));
        brush.Freeze();
        return brush;
    }

    private static Geometry BuildGlyphGeometry(string text, double size)
    {
        FormattedText ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            ZzzFace, size, Brushes.White, 1.0);
        Geometry geo = ft.BuildGeometry(new Point(-ft.Width / 2, -ft.Height / 2));
        geo.Freeze();
        return geo;
    }

    // Continuous 3-drop staggered dripping animation in head-local space.
    internal static void DrawSweat(DrawingContext dc, double t, double intensity)
    {
        if (intensity <= .01) return;
        dc.PushOpacity(Limit(intensity, 0, 1));
        double[] bx = { 45.5, 56.0, 41.5 };
        double[] by = { -34.0, -25.0, -18.0 };
        double[] sc = { 1.00, 0.72, 0.58 };
        double[] rot = { 10.0, 18.0, 6.0 };
        double[] off = { 0.00, 0.36, 0.68 };
        double[] travel = { 16.0, 14.0, 12.0 };
        for (int i = 0; i < 3; ++i)
        {
            double p = (t * 1.35 + off[i]) % 1.0;
            double spawn = Smooth(p / 0.18);
            double fade = 1.0 - Smooth((p - 0.72) / 0.28);
            double op = spawn * fade;
            if (op <= .01) continue;
            double dy = Math.Pow(p, 1.35) * travel[i];
            double dx = Math.Sin(p * Math.PI) * 2.2;
            double curScale = sc[i] * (0.65 + 0.40 * spawn) * (1.0 - 0.15 * Math.Max(0.0, (p - 0.75) / 0.25));
            dc.PushOpacity(op);
            dc.PushTransform(new TranslateTransform(bx[i] + dx, by[i] + dy));
            dc.PushTransform(new RotateTransform(rot[i]));
            dc.PushTransform(new ScaleTransform(curScale, curScale));
            dc.DrawGeometry(SweatDropFill, SweatWhiteHalo, SweatDropShape);
            dc.DrawGeometry(SweatDropFill, SweatBlueEdge, SweatDropShape);
            dc.DrawEllipse(SweatGleam1, null, new Point(-2.0, 2.2), 1.5, 2.9);
            dc.DrawEllipse(SweatGleam2, null, new Point(-1.4, 6.4), 0.8, 0.9);
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
        dc.Pop();
    }

    // Breathing-light pulsing anger mark (💢) with radial red aura in head-local space.
    internal static void DrawAnger(DrawingContext dc, double t, double intensity)
    {
        if (intensity <= .01) return;
        double breath = 0.5 + 0.5 * Math.Sin(t * 6.8);
        double scale = 0.92 + 0.22 * breath;
        double glowOp = (0.18 + 0.62 * breath) * Limit(intensity, 0, 1);
        double veinOp = (0.58 + 0.42 * breath) * Limit(intensity, 0, 1);
        byte r = (byte)(210 + 45 * breath);
        byte g = (byte)(32 + 40 * breath);
        byte b = (byte)(32 + 16 * breath);
        Pen veinPen = new Pen(new SolidColorBrush(Color.FromRgb(r, g, b)), 2.7);
        veinPen.StartLineCap = PenLineCap.Round;
        veinPen.EndLineCap = PenLineCap.Round;

        dc.PushTransform(new TranslateTransform(46.5, -36.5));
        dc.PushTransform(new RotateTransform(8.0));
        dc.PushOpacity(glowOp);
        dc.DrawEllipse(AngerAuraBrush, null, new Point(0, 0), 17.0 + 5.5 * breath, 17.0 + 5.5 * breath);
        dc.Pop();
        dc.PushOpacity(veinOp);
        dc.PushTransform(new ScaleTransform(scale, scale));
        for (int i = 0; i < AngerArcs.Length; ++i) dc.DrawGeometry(null, AngerWhiteHalo, AngerArcs[i]);
        for (int i = 0; i < AngerArcs.Length; ++i) dc.DrawGeometry(null, veinPen, AngerArcs[i]);
        dc.Pop();
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    // Fur-matched angled upper eyelid during anger, leaving wide room for cursor-tracking pupils.
    internal static void DrawAngryEyelid(DrawingContext dc, double ex, double ey, double ew, double eh,
                                         double side, Brush furFill, Pen lidPen, double intensity, double breath)
    {
        if (intensity <= .01) return;
        double eff = Limit(intensity, 0, 1) * (0.85 + 0.15 * breath);
        double dropIn = 6.2 * eff;
        double dropOut = 0.8 * eff;
        double xOut = ex + side * (ew + 2.5);
        double xIn = ex - side * (ew + 2.5);
        double yTop = ey - eh - 5.0;
        double yOut = ey - eh + dropOut + 1.6;
        double yIn = ey - eh + dropIn + 4.8;
        StreamGeometry wedge = new StreamGeometry();
        using (StreamGeometryContext g = wedge.Open())
        {
            g.BeginFigure(new Point(xOut, yTop), true, true);
            g.LineTo(new Point(xIn, yTop), true, false);
            g.LineTo(new Point(xIn, yIn), true, false);
            g.LineTo(new Point(xOut, yOut), true, false);
        }
        dc.DrawGeometry(furFill, null, wedge);
        dc.DrawLine(lidPen, new Point(xOut, yOut), new Point(xIn, yIn));
    }

    // Rising outlined Zzz letters during idle sleep.
    public static void DrawZzz(DrawingContext dc, double t, double anchorX, double anchorY, double opacity)
    {
        if (opacity <= .01) return;
        dc.PushOpacity(Limit(opacity, 0, 1));
        Geometry[] glyphs = { ZzzSmall, ZzzMid, ZzzLarge };
        double[] offsets = { 0.05, 0.38, 0.70 };
        for (int i = 0; i < 3; ++i)
        {
            double p = (t * 0.38 + offsets[i]) % 1.0;
            double alpha = Smooth(p / 0.14) * (1.0 - Smooth((p - 0.76) / 0.24));
            if (alpha <= .01) continue;
            double x = anchorX + p * 36.0 + Math.Sin(p * Math.PI * 2.2 + offsets[i] * 5.0) * 4.5;
            double y = anchorY - p * 60.0;
            double rot = -8.0 + p * 16.0;
            double sc = 0.78 + 0.32 * p;
            dc.PushOpacity(alpha);
            dc.PushTransform(new TranslateTransform(x, y));
            dc.PushTransform(new RotateTransform(rot));
            dc.PushTransform(new ScaleTransform(sc, sc));
            dc.DrawGeometry(null, ZzzOutline, glyphs[i]);
            dc.DrawGeometry(ZzzFill, null, glyphs[i]);
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
        dc.Pop();
    }

    // Glowing flying bug with flight trail dots and optional paw-swat sparkle arc.
    public static void DrawBug(DrawingContext dc, double x, double y, double t, double headingX, double swatFlash)
    {
        double flap = Math.Sin(t * 36.0);
        double wingRy = 3.2 + Math.Abs(flap) * 4.4;
        double dir = headingX >= 0 ? 1.0 : -1.0;

        for (int k = 1; k <= 3; ++k)
        {
            double tx = x - dir * (k * 9.5);
            double ty = y + Math.Sin(t * 6.0 - k * 0.9) * 3.5 + k * 1.4;
            byte a = (byte)(255 * (0.55 - k * 0.14));
            double r = 2.1 - k * 0.4;
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(a, 255, 248, 185)), null, new Point(tx, ty), r, r);
        }

        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new ScaleTransform(1.22, 1.22));
        dc.DrawEllipse(BugGlowBrush, null, new Point(0, 0), 13.0, 13.0);

        dc.PushTransform(new ScaleTransform(dir, 1.0));
        dc.PushTransform(new TranslateTransform(-1.5, -2.5));
        dc.PushTransform(new RotateTransform(-22.0 + flap * 16.0));
        dc.DrawEllipse(BugWing1, BugWingEdge, new Point(0, -wingRy * 0.6), 3.8, wingRy);
        dc.Pop();
        dc.Pop();

        dc.PushTransform(new TranslateTransform(2.0, -2.0));
        dc.PushTransform(new RotateTransform(18.0 - flap * 14.0));
        dc.DrawEllipse(BugWing2, BugWingEdge, new Point(0, -wingRy * 0.55), 3.2, wingRy * 0.85);
        dc.Pop();
        dc.Pop();

        dc.DrawEllipse(BugAbdomen, BugAbdomenEdge, new Point(-3.2, 0.8), 3.8, 2.6);
        dc.DrawEllipse(BugHead, null, new Point(1.6, 0.0), 3.1, 2.5);
        dc.DrawGeometry(null, BugAntennaPen, BugAntennae);
        dc.Pop();

        if (swatFlash > .05)
        {
            dc.PushOpacity(Limit(swatFlash, 0, 1));
            dc.DrawGeometry(null, SwatArcOuter, SwatOuterGeo);
            dc.DrawGeometry(null, SwatArcInner, SwatInnerGeo);
            dc.Pop();
        }
        dc.Pop();
        dc.Pop();
    }

    private static readonly Pen SwatTrailOuter = Stroke(Ink(255, 249, 210), 2.6);
    private static readonly Pen SwatTrailInner = Stroke(AlphaBrush(210, 255, 255, 255), 1.6);

    // Double motion-arc trail behind a fast-swiping paw during idle bug hunting.
    public static void DrawSwatTrail(DrawingContext dc, double pawX, double pawY, double side, double strength)
    {
        if (strength <= .08) return;
        dc.PushOpacity(Limit(strength, 0, 1));
        StreamGeometry outer = new StreamGeometry();
        using (StreamGeometryContext g = outer.Open())
        {
            g.BeginFigure(new Point(pawX - side * 24.0, pawY + 34.0), false, false);
            g.QuadraticBezierTo(new Point(pawX - side * 28.0, pawY + 4.0), new Point(pawX - side * 8.0, pawY - 14.0), true, false);
        }
        StreamGeometry inner = new StreamGeometry();
        using (StreamGeometryContext g = inner.Open())
        {
            g.BeginFigure(new Point(pawX - side * 14.0, pawY + 38.0), false, false);
            g.QuadraticBezierTo(new Point(pawX - side * 18.0, pawY + 12.0), new Point(pawX - side * 2.0, pawY - 6.0), true, false);
        }
        dc.DrawGeometry(null, SwatTrailOuter, outer);
        dc.DrawGeometry(null, SwatTrailInner, inner);
        dc.Pop();
    }
}
