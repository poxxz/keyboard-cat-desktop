using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

public enum PetZone { None, Head, Body }

public struct CatMood
{
    public double Sweat;
    public double Anger;
    public double Sleep;
    public double Crouch;
    public double HappyPet;
    public double Impatient;
    public double PetNuzzleX;
    public double PetNuzzleY;
    public double ScruffLift;
    public double DragSwing;
    public double Groom;
    public double GroomLick;
    public double GroomSide;
    public double EarTwitchL;
    public double EarTwitchR;
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

    // --- Petting expressions, heart/star particles, and animated petting/grabbing hand ---
    private static readonly Brush BlushBrush = BuildBlushBrush();
    private static readonly Pen BlushHatch = Stroke(AlphaBrush(130, 236, 112, 130), 1.05);
    private static readonly Geometry HeartShape = Shape(
        "M 0,6.8 C -1.2,5.6 -7.6,1.2 -7.6,-2.7 C -7.6,-5.6 -5.3,-7.4 -2.8,-7.4 " +
        "C -1.2,-7.4 -0.3,-6.5 0,-5.3 C 0.3,-6.5 1.2,-7.4 2.8,-7.4 " +
        "C 5.3,-7.4 7.6,-5.6 7.6,-2.7 C 7.6,1.2 1.2,5.6 0,6.8 Z");
    private static readonly Brush HeartFill = Wash(new byte[] { 255, 136, 166 }, new byte[] { 242, 78, 122 }, new Point(.25, 0), new Point(.75, 1));
    private static readonly Pen ParticleWhiteHalo = Stroke(AlphaBrush(230, 255, 255, 255), 2.1);
    private static readonly Pen HeartEdge = Stroke(Ink(198, 52, 92), .85);
    private static readonly Geometry StarShape = Shape(
        "M 0,-8.0 L 2.2,-2.6 L 8.0,-2.2 L 3.4,1.6 L 4.9,7.4 L 0,4.2 L -4.9,7.4 L -3.4,1.6 L -8.0,-2.2 L -2.2,-2.6 Z");
    private static readonly Brush StarFill = Wash(new byte[] { 255, 238, 128 }, new byte[] { 248, 174, 34 }, new Point(.2, 0), new Point(.8, 1));
    private static readonly Pen StarEdge = Stroke(Ink(194, 122, 16), .85);
    private static readonly Brush ParticleGlint = AlphaBrush(225, 255, 255, 255);

    private static readonly Geometry ImpatientPuff = Shape(
        "M -6,2 C -9,2 -10.5,-1.5 -8.5,-4 C -7,-6 -3.5,-6 -2,-4 " +
        "C -0.5,-7.5 4.5,-7.5 6.5,-4 C 9.5,-4 11,-0.5 9,2.5 C 8,4.5 5,5 3,4 " +
        "C 1.2,6 -2.8,6 -4.5,4 C -5.5,4.2 -6.5,3.2 -6,2 Z");
    private static readonly Brush ImpatientPuffFill = Wash(new byte[] { 255, 246, 236 }, new byte[] { 226, 212, 200 }, new Point(.2, 0), new Point(.8, 1));
    private static readonly Pen ImpatientPuffEdge = Stroke(Ink(122, 92, 78), 1.45);
    private static readonly Geometry ImpatientScribble = Shape("M -5,-2 Q -2,-6 1,-1 Q 4,4 7,-2 M -3,3 Q 1,-1 5,4");
    private static readonly Pen ImpatientScribblePen = Stroke(Ink(196, 76, 62), 1.55);

    private static readonly Brush HandFill = Wash(new byte[] { 255, 239, 219 }, new byte[] { 243, 206, 174 }, new Point(.2, 0), new Point(.85, 1));
    private static readonly Pen HandHalo = Stroke(AlphaBrush(205, 255, 255, 255), 3.4);
    private static readonly Pen HandEdge = Stroke(Ink(108, 76, 54), 1.45);
    private static readonly Pen HandCrease = Stroke(AlphaBrush(155, 146, 104, 76), 1.0);
    private static readonly Pen PetStrokeArc = Stroke(AlphaBrush(185, 255, 202, 88), 1.5);
    private static readonly Pen HoldTrackPen = Stroke(AlphaBrush(195, 255, 251, 242), 3.6);
    private static readonly Pen HoldArcPen = Stroke(Ink(242, 160, 24), 2.8);

    // Open warm petting hand pointing gently down-left over the cat's fur.
    private static readonly Geometry OpenHandShape = Shape(
        "M -2.2,-3.4 " +
        "L -1.8,-10.2 C -1.8,-13.0 2.0,-13.0 2.2,-10.2 L 2.6,-3.2 " +
        "L 4.4,-8.8 C 5.2,-11.4 8.6,-10.4 7.8,-7.6 L 6.4,-1.8 " +
        "L 8.8,-5.4 C 10.0,-7.4 13.0,-6.0 11.8,-3.4 L 9.8,4.0 " +
        "C 8.8,9.2 4.8,12.2 -0.2,12.2 C -5.0,12.2 -8.4,9.2 -9.4,4.6 " +
        "L -12.6,-0.2 C -14.0,-2.4 -12.2,-5.0 -9.8,-3.6 L -6.4,-1.2 " +
        "L -7.8,-8.2 C -8.4,-11.0 -4.8,-12.0 -3.8,-9.2 Z");
    private static readonly Geometry OpenHandLines = Shape("M -4.2,2.4 Q 0.4,0.4 5.4,2.8 M -1.6,5.6 Q 1.6,4.8 4.4,6.0");

    // Compact curled grabbing hand while dragging the cat.
    private static readonly Geometry GrabHandShape = Shape(
        "M -7,-5.5 C -7,-8.6 -3.8,-9.5 -2.2,-7.6 C -1.2,-9.8 2.4,-9.8 3.2,-7.4 " +
        "C 4.6,-9.2 7.8,-8.4 7.8,-5.4 L 7.6,2.2 C 7.6,7.4 4.2,10.6 -0.8,10.6 " +
        "C -5.6,10.6 -9.0,7.2 -9.0,2.4 L -9.6,-1.6 C -9.8,-4.2 -8.2,-5.6 -7,-5.5 Z");
    private static readonly Geometry GrabHandKnuckles = Shape("M -3.4,-4.2 L -3.2,0.5 M 0.6,-4.4 L 0.6,0.6 M 4.2,-3.8 L 4.0,0.6");

    private static Brush BuildBlushBrush()
    {
        RadialGradientBrush b = new RadialGradientBrush();
        b.GradientStops.Add(new GradientStop(Color.FromArgb(168, 255, 134, 152), 0));
        b.GradientStops.Add(new GradientStop(Color.FromArgb(88, 255, 146, 162), .58));
        b.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 150, 166), 1));
        b.Freeze();
        return b;
    }

    // Happy upward-arching crescent eye (^ ^) during gentle petting.
    internal static void DrawHappyEye(DrawingContext dc, double x, double y, double width, Pen lidPen)
    {
        StreamGeometry arc = new StreamGeometry();
        using (StreamGeometryContext g = arc.Open())
        {
            g.BeginFigure(new Point(x - width * 0.82, y + 2.4), false, false);
            g.QuadraticBezierTo(new Point(x, y - 6.2), new Point(x + width * 0.82, y + 2.4), true, true);
        }
        dc.DrawGeometry(null, lidPen, arc);
    }

    // Soft rosy cheek blush in head-local coordinates.
    internal static void DrawBlush(DrawingContext dc, double intensity)
    {
        if (intensity <= .02) return;
        dc.PushOpacity(Limit(intensity, 0, 1));
        dc.DrawEllipse(BlushBrush, null, new Point(-28.5, -0.5), 11.5, 6.4);
        dc.DrawEllipse(BlushBrush, null, new Point(28.5, -0.5), 11.5, 6.4);
        dc.DrawLine(BlushHatch, new Point(-32.0, 0.8), new Point(-29.5, -2.0));
        dc.DrawLine(BlushHatch, new Point(-27.2, 1.0), new Point(-24.8, -1.8));
        dc.DrawLine(BlushHatch, new Point(24.8, 1.0), new Point(27.2, -1.8));
        dc.DrawLine(BlushHatch, new Point(29.5, 0.8), new Point(32.0, -2.0));
        dc.Pop();
    }

    // Comic impatient puff & agitated squiggle when petted too fast.
    internal static void DrawImpatientMark(DrawingContext dc, double t, double intensity)
    {
        if (intensity <= .02) return;
        double jitterX = Math.Sin(t * 28.0) * 1.4;
        double jitterY = Math.Cos(t * 22.0) * 1.1;
        double pulse = 1.12 + 0.14 * Math.Sin(t * 10.0);
        dc.PushOpacity(Limit(intensity, 0, 1));
        dc.PushTransform(new TranslateTransform(-48.0 + jitterX, -39.0 + jitterY));
        dc.PushTransform(new ScaleTransform(pulse, pulse));
        dc.DrawGeometry(ImpatientPuffFill, ImpatientPuffEdge, ImpatientPuff);
        dc.DrawGeometry(null, ImpatientScribblePen, ImpatientScribble);
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    // Floating heart or twinkling star spawned while gently petting.
    public static void DrawPetParticle(DrawingContext dc, bool isHeart, double x, double y, double scale, double rotDeg, double opacity)
    {
        if (opacity <= .01 || scale <= .05) return;
        dc.PushOpacity(Limit(opacity, 0, 1));
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new RotateTransform(rotDeg));
        dc.PushTransform(new ScaleTransform(scale, scale));
        if (isHeart)
        {
            dc.DrawGeometry(null, ParticleWhiteHalo, HeartShape);
            dc.DrawGeometry(HeartFill, HeartEdge, HeartShape);
            dc.DrawEllipse(ParticleGlint, null, new Point(-2.6, -3.4), 1.4, 0.9);
        }
        else
        {
            dc.DrawGeometry(null, ParticleWhiteHalo, StarShape);
            dc.DrawGeometry(StarFill, StarEdge, StarShape);
            dc.DrawEllipse(ParticleGlint, null, new Point(-1.2, -1.8), 1.2, 1.2);
        }
        dc.Pop();
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    // Animated vector petting hand cursor + 0.35s long-press amber progress ring + grab hand.
    public static void DrawPettingHand(DrawingContext dc, Point pos, double strokeVelX, double phase,
                                       double holdProgress, bool isDragging, bool isRubbing)
    {
        dc.PushTransform(new TranslateTransform(pos.X, pos.Y));

        // Long-press drag charge ring around the cursor before dragging kicks in.
        if (holdProgress > .03 && !isDragging)
        {
            double p = Limit(holdProgress, 0, 1);
            const double r = 17.5;
            dc.DrawEllipse(null, HoldTrackPen, new Point(0, 0), r, r);
            double sweep = p * Math.PI * 2.0;
            if (sweep >= Math.PI * 1.99)
            {
                dc.DrawEllipse(null, HoldArcPen, new Point(0, 0), r, r);
            }
            else
            {
                Point start = new Point(0, -r);
                Point end = new Point(Math.Sin(sweep) * r, -Math.Cos(sweep) * r);
                StreamGeometry arc = new StreamGeometry();
                using (StreamGeometryContext g = arc.Open())
                {
                    g.BeginFigure(start, false, false);
                    g.ArcTo(end, new Size(r, r), 0, sweep > Math.PI, SweepDirection.Clockwise, true, true);
                }
                dc.DrawGeometry(null, HoldArcPen, arc);
            }
        }

        if (isDragging)
        {
            // Natural upper-right grip pinching the back of the cat's neck.
            dc.PushTransform(new RotateTransform(24.0));
            dc.PushTransform(new ScaleTransform(1.04, 1.04));
            dc.DrawGeometry(null, HandHalo, GrabHandShape);
            dc.DrawGeometry(HandFill, HandEdge, GrabHandShape);
            dc.DrawGeometry(null, HandCrease, GrabHandKnuckles);
            dc.Pop();
            dc.Pop();
        }
        else
        {
            double tilt = -14.0 + Limit(strokeVelX / 360.0, -1, 1) * 16.0 + Math.Sin(phase) * (isRubbing ? 6.5 : 2.2);
            double bob = Math.Sin(phase * 1.4) * (isRubbing ? 2.2 : 0.8);
            double pressScale = holdProgress > .03 ? 1.0 - 0.08 * Limit(holdProgress, 0, 1) : 1.0;

            if (isRubbing)
            {
                StreamGeometry w1 = new StreamGeometry();
                using (StreamGeometryContext g = w1.Open())
                {
                    g.BeginFigure(new Point(-15, -4 + bob), false, false);
                    g.QuadraticBezierTo(new Point(-18, 2 + bob), new Point(-14, 8 + bob), true, false);
                }
                StreamGeometry w2 = new StreamGeometry();
                using (StreamGeometryContext g = w2.Open())
                {
                    g.BeginFigure(new Point(15, -3 + bob), false, false);
                    g.QuadraticBezierTo(new Point(18, 3 + bob), new Point(14, 9 + bob), true, false);
                }
                dc.DrawGeometry(null, PetStrokeArc, w1);
                dc.DrawGeometry(null, PetStrokeArc, w2);
            }

            dc.PushTransform(new TranslateTransform(0, bob));
            dc.PushTransform(new RotateTransform(tilt));
            dc.PushTransform(new ScaleTransform(pressScale, pressScale));
            dc.DrawGeometry(null, HandHalo, OpenHandShape);
            dc.DrawGeometry(HandFill, HandEdge, OpenHandShape);
            dc.DrawGeometry(null, HandCrease, OpenHandLines);
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
        dc.Pop();
    }

    // Soft organic nape-fur gather bridging back-of-head and upper shoulder when turned 3/4 sideways.
    private static readonly Geometry ScruffNapeShape = Shape(
        "M 224,96 C 234,94 246,96 250,103 C 253,109 249,120 242,128 L 222,126 Z");
    private static readonly Pen DangleArcPen = Stroke(AlphaBrush(155, 232, 162, 54), 1.5);

    internal static void DrawScruffNape(DrawingContext dc, Brush furBrush, double lift)
    {
        if (lift <= .03) return;
        dc.PushOpacity(Limit(lift, 0, 1));
        dc.DrawGeometry(furBrush, null, ScruffNapeShape);
        dc.Pop();
    }

    // Subtle comic motion lines on the sides while the scruff-held cat dangles and swings.
    public static void DrawDangleLines(DrawingContext dc, double lift, double swingDeg, double t)
    {
        if (lift <= .10) return;
        double op = Limit(lift, 0, 1) * (0.35 + 0.65 * Limit(Math.Abs(swingDeg) / 7.0, 0, 1));
        double bob = Math.Sin(t * 7.0) * 1.8;
        dc.PushOpacity(op);
        StreamGeometry l1 = new StreamGeometry();
        using (StreamGeometryContext g = l1.Open())
        {
            g.BeginFigure(new Point(126, 128 + bob), false, false);
            g.QuadraticBezierTo(new Point(119, 144 + bob), new Point(125, 160 + bob), true, false);
        }
        StreamGeometry r1 = new StreamGeometry();
        using (StreamGeometryContext g = r1.Open())
        {
            g.BeginFigure(new Point(286, 128 - bob), false, false);
            g.QuadraticBezierTo(new Point(293, 144 - bob), new Point(287, 160 - bob), true, false);
        }
        dc.DrawGeometry(null, DangleArcPen, l1);
        dc.DrawGeometry(null, DangleArcPen, r1);
        dc.Pop();
    }

    // --- Idle grooming tongue & snappy ear-twitch animations ---
    private static readonly Brush TongueFill = Wash(new byte[] { 248, 152, 166 }, new byte[] { 226, 108, 128 }, new Point(.5, 0), new Point(.5, 1));
    private static readonly Pen TongueEdge = Stroke(Ink(168, 74, 88), 1.0);
    private static readonly Pen TongueGroove = Stroke(AlphaBrush(130, 178, 76, 92), 0.85);
    private static readonly Brush TongueGlint = AlphaBrush(160, 255, 232, 236);
    private static readonly Pen EarTwitchPen = Stroke(AlphaBrush(195, 236, 166, 52), 1.55);

    private static readonly Geometry LeftEarClip = Shape("M -62,-74 L -10,-74 L -10,-32 Q -28,-22 -48,-16 L -62,-16 Z");
    private static readonly Geometry RightEarClip = Shape("M 10,-74 L 62,-74 L 62,-16 L 48,-16 Q 28,-22 10,-32 Z");
    private static readonly Geometry SkullMinusLeftClip = Shape(
        "F0 M -96,-96 L 96,-96 L 96,96 L -96,96 Z M -62,-74 L -14,-74 L -16,-44 Q -31,-41 -44,-26 L -62,-26 Z");
    private static readonly Geometry SkullMinusRightClip = Shape(
        "F0 M -96,-96 L 96,-96 L 96,96 L -96,96 Z M 14,-74 L 62,-74 L 62,-26 L 44,-26 Q 31,-41 16,-44 Z");
    private static readonly Geometry SkullMinusBothClip = Shape(
        "F0 M -96,-96 L 96,-96 L 96,96 L -96,96 Z " +
        "M -62,-74 L -14,-74 L -16,-44 Q -31,-41 -44,-26 L -62,-26 Z " +
        "M 14,-74 L 62,-74 L 62,-26 L 44,-26 Q 31,-41 16,-44 Z");

    // Draws head layers with zero-allocation EvenOdd ear hinge clipping so upright ears physically flick seamlessly.
    internal static void DrawHeadWithEarTwitch(DrawingContext dc, double twitchL, double twitchR, Action drawHead)
    {
        bool tl = twitchL > 0.015;
        bool tr = twitchR > 0.015;
        if (!tl && !tr)
        {
            drawHead();
            return;
        }
        if (tl)
        {
            double k = Limit(twitchL, 0, 1);
            dc.PushTransform(new TranslateTransform(2.0 * k, 3.2 * k));
            dc.PushTransform(new RotateTransform(-19.0 * k, -34.0, -34.0));
            dc.PushTransform(new ScaleTransform(1.0 + 0.04 * k, 1.0 - 0.14 * k, -34.0, -34.0));
            dc.PushClip(LeftEarClip);
            drawHead();
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
        if (tr)
        {
            double k = Limit(twitchR, 0, 1);
            dc.PushTransform(new TranslateTransform(-2.0 * k, 3.2 * k));
            dc.PushTransform(new RotateTransform(19.0 * k, 34.0, -34.0));
            dc.PushTransform(new ScaleTransform(1.0 + 0.04 * k, 1.0 - 0.14 * k, 34.0, -34.0));
            dc.PushClip(RightEarClip);
            drawHead();
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
        Geometry bodyClip = (tl && tr) ? SkullMinusBothClip : (tl ? SkullMinusLeftClip : SkullMinusRightClip);
        dc.PushClip(bodyClip);
        drawHead();
        dc.Pop();
    }

    // Crisp cartoon flick lines radiating outside a twitching ear tip.
    internal static void DrawEarTwitchMark(DrawingContext dc, double side, double intensity)
    {
        double k = Limit(intensity, 0, 1);
        if (k <= 0.08) return;
        double s = side < 0 ? -1.0 : 1.0;
        dc.PushOpacity(k * 0.92);
        StreamGeometry g1 = new StreamGeometry();
        using (StreamGeometryContext g = g1.Open())
        {
            g.BeginFigure(new Point(s * (46.0 + 2.0 * k), -60.0), false, false);
            g.QuadraticBezierTo(new Point(s * (53.0 + 3.0 * k), -53.0), new Point(s * (50.0 + 2.5 * k), -44.0), true, false);
        }
        StreamGeometry g2 = new StreamGeometry();
        using (StreamGeometryContext g = g2.Open())
        {
            g.BeginFigure(new Point(s * (51.0 + 3.0 * k), -58.0), false, false);
            g.QuadraticBezierTo(new Point(s * (57.5 + 3.5 * k), -52.0), new Point(s * (55.0 + 3.0 * k), -45.5), true, false);
        }
        dc.DrawGeometry(null, EarTwitchPen, g1);
        dc.DrawGeometry(null, EarTwitchPen, g2);
        dc.Pop();
    }

    // Adorable pink cat tongue peeking out in head-local space while licking a raised paw.
    internal static void DrawGroomTongue(DrawingContext dc, double side, double lick, double groom)
    {
        double k = Limit(lick, 0, 1) * Limit(groom, 0, 1);
        if (k <= 0.04) return;
        double s = side < 0 ? -1.0 : 1.0;
        double ext = 0.35 + 0.65 * k;
        double len = 10.5 * ext;
        double cx = s * (2.2 + 2.0 * k);
        double topY = 12.2;
        dc.PushOpacity(Limit(k * 1.4, 0, 1));
        dc.PushTransform(new RotateTransform(-s * 12.0 * k, cx, topY));
        StreamGeometry tongue = new StreamGeometry();
        using (StreamGeometryContext g = tongue.Open())
        {
            g.BeginFigure(new Point(cx - 4.0, topY), true, true);
            g.BezierTo(
                new Point(cx - 4.4, topY + len * 0.72),
                new Point(cx - 2.6, topY + len),
                new Point(cx, topY + len + 0.8), true, true);
            g.BezierTo(
                new Point(cx + 2.6, topY + len),
                new Point(cx + 4.4, topY + len * 0.72),
                new Point(cx + 4.0, topY), true, true);
        }
        dc.DrawGeometry(TongueFill, TongueEdge, tongue);
        dc.DrawLine(TongueGroove, new Point(cx, topY + 1.4), new Point(cx, topY + len * 0.68));
        dc.DrawEllipse(TongueGlint, null, new Point(cx - 1.4, topY + len * 0.45), 1.1, 1.7 * ext);
        dc.Pop();
        dc.Pop();
    }
}
