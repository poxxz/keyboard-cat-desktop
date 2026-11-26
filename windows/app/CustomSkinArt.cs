using System;
using System.Windows;
using System.Windows.Media;

// Compiled, frozen brushes/pens for zero-allocation 40 FPS rendering of a user-created CatSkinData.
internal sealed class CompiledSkin
{
    public readonly CatSkinData Data;
    public readonly Brush BodyFur;
    public readonly Brush HeadFur;
    public readonly Brush TailFur;
    public readonly Brush ArmFur;
    public readonly Brush LidFur;
    public readonly Brush AccentBrush;
    public readonly Brush AccentSoftBrush;
    public readonly Brush BellyBrush;
    public readonly Brush PawBrush;
    public readonly Brush InnerEarBrush;
    public readonly Brush NoseBrush;
    public readonly Brush LeftIrisBrush;
    public readonly Brush RightIrisBrush;
    public readonly Brush PupilBrush;
    public readonly Brush GlintBrush;
    public readonly Brush EarTipBrush;
    public readonly Brush SiameseEarWash;
    public readonly Brush SiameseMaskWash;
    public readonly Brush SiameseMaskLowerWash;
    public readonly Brush AccPrimary;
    public readonly Brush AccHighlight;
    public readonly Pen LidPen;
    public readonly Pen EyeEdgePen;
    public readonly Pen WhiskerPen;
    public readonly Pen WhiskerRootPen;
    public readonly Pen LipPen;
    public readonly Pen AccEdgePen;
    public readonly Color FurLightColor;
    public readonly Color FurDarkColor;
    public readonly Color PawColor;

    public CompiledSkin(CatSkinData data)
    {
        Data = data.Clone();
        Color fur = CatSkinData.ParseHex(Data.FurColor, Color.FromRgb(58, 55, 60));
        Color accent = CatSkinData.ParseHex(Data.AccentColor, Color.FromRgb(212, 132, 74));
        Color belly = CatSkinData.ParseHex(Data.BellyColor, Color.FromRgb(255, 251, 244));
        Color paw = CatSkinData.ParseHex(Data.PawColor, Color.FromRgb(255, 251, 244));
        Color innerEar = CatSkinData.ParseHex(Data.InnerEarColor, Color.FromRgb(240, 168, 158));
        Color nose = CatSkinData.ParseHex(Data.NoseColor, Color.FromRgb(230, 142, 146));
        Color whisker = CatSkinData.ParseHex(Data.WhiskerColor, Color.FromRgb(236, 230, 220));
        Color leftEye = CatSkinData.ParseHex(Data.LeftEyeColor, Color.FromRgb(242, 182, 50));
        Color rightEye = Data.Heterochromia
            ? CatSkinData.ParseHex(Data.RightEyeColor, Color.FromRgb(107, 184, 232))
            : leftEye;
        Color acc = CatSkinData.ParseHex(Data.AccessoryColor, Color.FromRgb(228, 102, 82));

        FurLightColor = Adjust(fur, 1.09, 6);
        FurDarkColor = Adjust(fur, 0.80, -5);
        PawColor = paw;

        BodyFur = AbsWash(FurLightColor, FurDarkColor, new Point(175, 102), new Point(255, 239));
        HeadFur = RelWash(Adjust(fur, 1.12, 8), Adjust(fur, 0.82, -4), new Point(.24, .05), new Point(.84, .94));
        TailFur = RelWash(Adjust(fur, 1.04, 3), Adjust(fur, 0.78, -5), new Point(.2, 0), new Point(.85, 1));
        ArmFur = BodyFur;
        LidFur = Solid(Adjust(fur, 0.94, 0));
        AccentBrush = Solid(accent);
        AccentSoftBrush = Solid(Color.FromArgb(196, accent.R, accent.G, accent.B));
        BellyBrush = RelWash(Adjust(belly, 1.03, 3), Adjust(belly, 0.92, -4), new Point(.25, 0), new Point(.75, 1));
        PawBrush = Solid(paw);
        InnerEarBrush = RelWash(Adjust(innerEar, 0.94, -3), Adjust(innerEar, 1.07, 8), new Point(.2, 0), new Point(.8, 1));
        NoseBrush = RelWash(Adjust(nose, 1.08, 6), Adjust(nose, 0.84, -4), new Point(.3, 0), new Point(.7, 1));
        LeftIrisBrush = RelWash(Adjust(leftEye, 1.15, 14), Adjust(leftEye, 0.78, -6), new Point(.35, .05), new Point(.65, .95));
        RightIrisBrush = RelWash(Adjust(rightEye, 1.15, 14), Adjust(rightEye, 0.78, -6), new Point(.35, .05), new Point(.65, .95));
        PupilBrush = Solid(Color.FromRgb(30, 24, 22));
        GlintBrush = Solid(Color.FromRgb(255, 252, 240));
        EarTipBrush = Solid(Adjust(fur, 0.42, -10));

        // Soft gradients for Siamese point pattern using the user's AccentColor.
        SiameseEarWash = BuildSiameseEar(accent);
        SiameseMaskWash = BuildRadialFade(accent, 235, 185);
        SiameseMaskLowerWash = BuildRadialFade(accent, 220, 165);

        AccPrimary = RelWash(Adjust(acc, 1.12, 10), Adjust(acc, 0.82, -6), new Point(.2, 0), new Point(.8, 1));
        AccHighlight = Solid(Color.FromArgb(190, 255, 252, 242));
        AccEdgePen = MakePen(Adjust(acc, 0.56, -12), 1.15);

        bool darkFur = (fur.R * 0.299 + fur.G * 0.587 + fur.B * 0.114) < 88;
        Color lidColor = darkFur ? Adjust(leftEye, 0.95, 0) : Adjust(fur, 0.48, -14);
        LidPen = MakePen(lidColor, 2.1);
        EyeEdgePen = MakePen(darkFur ? Color.FromArgb(140, 20, 20, 22) : Adjust(fur, 0.42, -16), 1.05);
        WhiskerPen = MakePen(whisker, 0.92);
        WhiskerRootPen = MakePen(Adjust(whisker, 0.88, 0), 0.92);
        LipPen = MakePen(Color.FromRgb(126, 86, 70), 1.2);
    }

    private static Color Adjust(Color c, double mul, int add)
    {
        return Color.FromRgb(
            ClampByte(c.R * mul + add),
            ClampByte(c.G * mul + add),
            ClampByte(c.B * mul + add));
    }

    private static byte ClampByte(double v)
    {
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)Math.Round(v);
    }

    private static Brush Solid(Color c)
    {
        SolidColorBrush b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static Brush RelWash(Color a, Color b, Point from, Point to)
    {
        LinearGradientBrush g = new LinearGradientBrush(a, b, from, to);
        g.Freeze();
        return g;
    }

    private static Brush AbsWash(Color a, Color b, Point from, Point to)
    {
        LinearGradientBrush g = new LinearGradientBrush(a, b, from, to);
        g.MappingMode = BrushMappingMode.Absolute;
        g.Freeze();
        return g;
    }

    private static Pen MakePen(Color c, double width)
    {
        Pen p = new Pen(Solid(c), width)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        p.Freeze();
        return p;
    }

    private static Brush BuildSiameseEar(Color c)
    {
        LinearGradientBrush g = new LinearGradientBrush();
        g.StartPoint = new Point(0, 0);
        g.EndPoint = new Point(0, 1);
        g.GradientStops.Add(new GradientStop(c, 0));
        g.GradientStops.Add(new GradientStop(c, .66));
        g.GradientStops.Add(new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1));
        g.Freeze();
        return g;
    }

    private static Brush BuildRadialFade(Color c, byte centerAlpha, byte midAlpha)
    {
        RadialGradientBrush g = new RadialGradientBrush();
        g.GradientStops.Add(new GradientStop(Color.FromArgb(centerAlpha, c.R, c.G, c.B), 0));
        g.GradientStops.Add(new GradientStop(Color.FromArgb(midAlpha, c.R, c.G, c.B), .55));
        g.GradientStops.Add(new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1));
        g.Freeze();
        return g;
    }
}

// Pure-vector modular cat renderer powering the Skin Workshop.
internal static class CustomSkinArt
{
    private static readonly Geometry EarTipLeft = Parse("M -44,-49 Q -41,-59 -36,-63 Q -32,-63 -28,-57 Q -35,-54 -41,-49 Z");
    private static readonly Geometry EarTipRight = Parse("M 28,-57 Q 33,-64 37,-63 Q 42,-58 44,-48 Q 39,-53 28,-57 Z");

    // Clean 3-stroke tabby forehead + subtle cheek bands (Coffee / Tabby).
    private static readonly Geometry ForeheadClean = Parse(
        "M -2.2,-46.5 Q 0,-47.5 2.2,-46.5 Q 1.5,-39.5 0,-34.5 Q -1.5,-39.5 -2.2,-46.5 Z " +
        "M -11.5,-45.5 Q -9.0,-46.5 -6.8,-44.8 Q -7.2,-38.5 -10.5,-33.8 Q -11.8,-39.5 -11.5,-45.5 Z " +
        "M 11.5,-45.5 Q 9.0,-46.5 6.8,-44.8 Q 7.2,-38.5 10.5,-33.8 Q 11.8,-39.5 11.5,-45.5 Z");
    private static readonly Geometry ForeheadClassicM = Parse(
        "M -17,-30 C -16,-36 -14,-42 -12,-48 L -7,-48 Q -4,-42 0,-39 " +
        "Q 4,-42 7,-48 L 12,-48 Q 16,-40 15,-30 Q 11,-33 8.5,-41 " +
        "Q 4,-36 0,-32 Q -4,-36 -8.5,-41 Q -12,-33 -17,-30 Z");
    private static readonly Geometry CheekClean = Parse(
        "M -48,-9.5 Q -41.5,-10.2 -35.5,-8.0 Q -41.5,-6.6 -48,-6.2 Z " +
        "M -47,0.5 Q -40.8,-0.2 -34.8,2.2 Q -40.8,3.6 -46.5,3.8 Z " +
        "M 48,-9.5 Q 41.5,-10.2 35.5,-8.0 Q 41.5,-6.6 48,-6.2 Z " +
        "M 47,0.5 Q 40.8,-0.2 34.8,2.2 Q 40.8,3.6 46.5,3.8 Z");

    // Coffee open-face white lower mask with narrow inverted-V nose blaze.
    private static readonly Geometry CoffeeWhiteMask = Parse(
        "M 0,-17.0 C 1.6,-10.2 4.0,-2.6 8.2,3.4 C 12.4,9.2 18.2,13.8 25.5,16.0 " +
        "Q 34.4,17.2 43.8,15.2 Q 48.0,13.4 52.5,10.8 L 53.5,39.0 L -53.5,39.0 L -52.5,10.8 " +
        "Q -48.0,13.4 -43.8,15.2 Q -34.4,17.2 -25.5,16.0 C -18.2,13.8 -12.4,9.2 -8.2,3.4 " +
        "C -4.0,-2.6 -1.6,-10.2 0,-17.0 Z");

    // Tuxedo / Bicolor inverted-V blaze (wider symmetrical white lower face).
    private static readonly Geometry TuxedoLowerFace = Parse(
        "M 0,-28.0 C 5.5,-15.0 11.5,-3.0 21.5,5.5 C 31.0,12.5 42.0,14.5 53.5,12.0 " +
        "L 54.0,39.0 L -54.0,39.0 L -53.5,12.0 C -42.0,14.5 -31.0,12.5 -21.5,5.5 " +
        "C -11.5,-3.0 -5.5,-15.0 0,-28.0 Z");

    // Calico dual forehead patches (left dark/fur, right warm accent).
    private static readonly Geometry CalicoLeftPatch = Parse(
        "M -54,-66 L -3,-66 C -1,-46 -7,-28 -19,-18 C -30,-9 -44,-8 -54,-11 Z");
    private static readonly Geometry CalicoRightPatch = Parse(
        "M 4,-66 L 54,-66 L 54,-6 C 41,-5 27,-9 16,-19 C 6,-29 2,-45 4,-66 Z");

    // Cow-cat playful asymmetric forehead + cheek spots.
    private static readonly Geometry CowHeadSpots = Parse(
        "M -52,-65 L -8,-65 C -4,-44 -14,-22 -28,-14 C -38,-9 -47,-10 -53,-14 Z " +
        "M 18,-65 L 52,-65 L 52,-32 C 41,-28 29,-34 22,-44 C 17,-51 16,-58 18,-65 Z " +
        "M 28,2 C 37,-3 49,1 51,12 C 47,23 36,27 26,23 C 19,18 20,7 28,2 Z");

    private static readonly Geometry MuzzleOval = Parse(
        "M -25,10 C -23,3 -16,-1 -8,.7 C -4,1 -1,3 0,4 C 5,1 11,-1 17,1 " +
        "C 25,3 28,10 24,17 C 20,24 11,29 1,30 C -10,31 -20,28 -24,21 Q -28,16 -25,10 Z");
    private static readonly Geometry NoseShape = Parse(
        "M -4.8,5.0 Q -3.6,2.9 0,3.9 Q 3.7,2.8 4.9,4.9 Q 5.1,6.5 2.1,8.8 Q .3,10.3 -.8,9.6 Q -5.2,6.9 -4.8,5.0 Z");
    private static readonly Geometry SmileShape = Parse(
        "M 0,9.1 L 0,12.3 C -1.5,15.7 -5.4,15.9 -7.6,13.0 M 0,12.3 C 1.8,15.3 5.5,15.6 7.8,12.9");

    // Body markings (short-hair coordinates around 214, 170).
    private static readonly Geometry BibSmall = Parse(
        "M 182,102 C 189,112 205,110 214,111 C 225,110 240,111 246,101 " +
        "Q 248,124 236,145 L 235,138 Q 230,153 220,163 L 219,156 " +
        "Q 216,162 210,167 Q 205,158 199,153 L 198,157 Q 183,141 181,124 Z");
    private static readonly Geometry FullBellyShort = Parse(
        "M 182,102 C 189,112 205,110 214,111 C 225,110 240,111 244,102 " +
        "Q 251,110 246,118 Q 241,128 249,138 Q 257,150 250,164 Q 245,178 254,194 " +
        "Q 261,210 258,226 C 260,233 259,238 254,239 L 178,239 C 172,238 167,233 166,226 " +
        "Q 169,210 168,194 Q 175,178 171,164 Q 166,150 175,138 Q 182,128 178,118 Q 174,110 182,102 Z");
    private static readonly Geometry FlanksShort = Parse(
        "M 155,168 C 167,166 174,170 181,176 Q 169,173 157,176 Z " +
        "M 156,180 Q 171,178 179,186 Q 169,183 156,188 Z " +
        "M 272,164 Q 257,164 249,173 Q 261,169 272,173 Z " +
        "M 271,178 Q 258,177 250,186 Q 262,182 272,185 Z");
    private static readonly Geometry PatchesShort = Parse(
        "M 156,136 C 174,132 189,144 186,166 C 182,184 166,192 154,186 Z " +
        "M 270,152 C 252,148 238,162 242,184 C 246,204 260,210 272,202 Z");

    // Body markings (fluffy longhair local coordinates around 0, 58).
    private static readonly Geometry FullBellyFluffy = Parse(
        "M -28,0 Q -36,42 -35,84 Q -33,112 -24,122 L 24,122 Q 34,112 36,84 Q 37,42 28,0 Z");
    private static readonly Geometry FlanksFluffy = Parse(
        "M -56,56 C -42,54 -34,58 -28,64 Q -40,61 -54,64 Z " +
        "M -55,70 Q -40,68 -30,76 Q -42,73 -55,78 Z " +
        "M 56,52 Q 42,52 32,61 Q 45,57 56,61 Z " +
        "M 55,68 Q 42,67 33,76 Q 46,72 56,75 Z");
    private static readonly Geometry PatchesFluffy = Parse(
        "M -56,28 C -36,24 -22,38 -25,62 C -29,82 -44,88 -56,80 Z " +
        "M 56,42 C 36,38 22,52 25,78 C 29,96 44,102 56,94 Z");

    private static readonly Geometry FrontSock = Parse(
        "M -9.2,1 C -10,-4 -9.9,-9 -7.3,-10.7 Q -5,-12 -2,-10.8 Q 1,-9.3 3.2,-10.7 " +
        "Q 6.1,-12.5 8.3,-10.4 Q 10.3,-7 9.2,1 Q 7.2,8 0,9 Q -7.7,7.9 -9.2,1 Z");
    private static readonly Geometry TailBand = Parse(
        "M -3,-13 Q .7,-11 4,-13 Q 1,0 4,13 Q -.2,11 -3,13 Q -4.4,0 -3,-13 Z");
    private static readonly Geometry TailTipGeo = Parse(
        "M -3,-12 L 22,-12 L 22,13 L -4,13 Q .4,4 -1,-1 Q -4,-6 -3,-12 Z");

    // Cute vector accessories (drawn cleanly in head-local coordinates).
    private static readonly Geometry CollarBand = Parse("M -33,27 Q 0,38 33,27 L 31,34 Q 0,44 -31,34 Z");
    private static readonly Geometry BowLeft = Parse("M -1,35 C -8,28 -18,28 -17,36 C -16,43 -7,42 -1,37 Z");
    private static readonly Geometry BowRight = Parse("M 1,35 C 8,28 18,28 17,36 C 16,43 7,42 1,37 Z");
    private static readonly Geometry RibbonLeft = Parse("M 0,0 C -7,-6 -16,-5 -14,2 C -12,8 -5,7 0,2 Z");
    private static readonly Geometry RibbonRight = Parse("M 0,0 C 7,-6 16,-5 14,2 C 12,8 5,7 0,2 Z");
    private static readonly Geometry OrangeLeaf = Parse("M 1,-9 C 5,-14 11,-13 11,-8 C 8,-6 4,-6 1,-9 Z");
    private static readonly Geometry AhogeShape = Parse("M -2,-44 C -9,-58 -2,-70 9,-66 C 1,-64 1,-54 4,-44 Z");
    private static readonly Brush BellGold = MakeGrad(Color.FromRgb(255, 226, 108), Color.FromRgb(226, 158, 28));
    private static readonly Pen BellEdge = MakePen(Color.FromRgb(142, 92, 16), 1.0);
    private static readonly Brush LeafGreen = MakeGrad(Color.FromRgb(124, 198, 88), Color.FromRgb(68, 144, 52));
    private static readonly Pen LeafEdge = MakePen(Color.FromRgb(46, 102, 36), 0.9);
    private static readonly Brush GlassLens = MakeSolid(Color.FromArgb(46, 224, 244, 255));
    private static readonly Pen GlassGlint = MakePen(Color.FromArgb(185, 255, 255, 255), 1.15);
    private static readonly Brush NoseGlint = MakeSolid(Color.FromArgb(140, 255, 232, 232));

    private static Geometry Parse(string s) { Geometry g = Geometry.Parse(s); g.Freeze(); return g; }
    private static Brush MakeSolid(Color c) { SolidColorBrush b = new SolidColorBrush(c); b.Freeze(); return b; }
    private static Brush MakeGrad(Color a, Color b) { LinearGradientBrush g = new LinearGradientBrush(a, b, new Point(.2, 0), new Point(.8, 1)); g.Freeze(); return g; }
    private static Pen MakePen(Color c, double w)
    {
        Pen p = new Pen(MakeSolid(c), w) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        p.Freeze();
        return p;
    }

    public static PetZone ContainsCat(CompiledSkin skin, Point pointer, double time, double gazeX, double gazeY, Point leftPaw, double sleep)
    {
        if (skin != null && skin.Data.BodyBase == SkinBodyBase.FluffyLong)
            return TabbyArt.ContainsCat(pointer, time, gazeX, gazeY, leftPaw, sleep);
        return CatArt.ContainsCat(CatStyle.Black, pointer, time, gazeX, gazeY, leftPaw, sleep);
    }

    public static void DrawPortrait(DrawingContext dc, CompiledSkin skin)
    {
        DrawPortraitMood(dc, skin, 0, 0, 0, default(CatMood));
    }

    public static void DrawPortraitMood(DrawingContext dc, CompiledSkin skin, double gx, double gy, double blink, CatMood mood)
    {
        if (skin == null) return;
        double happy = CatArt.Limit(mood.HappyPet, 0, 1);
        double impatient = CatArt.Limit(mood.Impatient, 0, 1);
        double anger = Math.Max(mood.Anger, impatient * 0.92);
        DrawHead(dc, skin, gx, gy, blink, anger, 0.75, mood.Crouch, happy);
        if (happy > .05) CatArt.DrawBlush(dc, happy);
        if (mood.Anger > .01) CatArt.DrawAnger(dc, 0.4, mood.Anger);
        if (impatient > .05) CatArt.DrawImpatientMark(dc, 0.4, impatient);
    }

    public static void Draw(DrawingContext dc, CompiledSkin skin, double t, double gazeX, double gazeY,
                            double blink, Point leftPaw, Point rightPaw, double leftActivity, double rightActivity,
                            double pawRaised = -1, Action<DrawingContext> between = null, CatMood mood = default(CatMood))
    {
        if (skin == null) return;
        bool fluffy = skin.Data.BodyBase == SkinBodyBase.FluffyLong;
        double gx = CatArt.Limit(gazeX, -1, 1);
        double gy = CatArt.Limit(gazeY, -1, 1);
        double sleep = CatArt.Limit(mood.Sleep, 0, 1);
        double crouch = CatArt.Limit(mood.Crouch, 0, 1);
        double happy = CatArt.Limit(mood.HappyPet, 0, 1) * (1 - sleep);
        double impatient = CatArt.Limit(mood.Impatient, 0, 1) * (1 - sleep);
        double highLeft = CatArt.Smooth((195 - leftPaw.Y) / 45);
        double lean = CatArt.Smooth((157 - leftPaw.X) / 50) * (1 - sleep) * (1 - highLeft);
        double breath = Math.Sin(t * (sleep > 0.5 ? 1.15 : 1.7)) * (sleep > 0.5 ? 1.7 : 0.72);
        double angerBreath = 0.5 + 0.5 * Math.Sin(t * 6.8);

        double syScale = 1.0 - 0.45 * sleep - 0.10 * crouch + 0.025 * happy * Math.Sin(t * 5.0);
        double sxScale = 1.0 + 0.04 * sleep + 0.04 * crouch + 0.02 * happy;

        // 1. Tail
        DrawTail(dc, skin, fluffy, t, mood, happy, impatient, sleep, crouch);

        // 2. Torso
        if (fluffy)
        {
            double bodyYShift = (1.0 - syScale) * (235.0 - TabbyArt.BodyTop);
            dc.PushTransform(new MatrixTransform(1, 0, .285 * lean, 1, -233 * .285 * lean, 0));
            dc.PushTransform(new TranslateTransform(214, TabbyArt.BodyTop + bodyYShift));
            dc.PushTransform(new ScaleTransform(TabbyArt.BodyWidth * sxScale, TabbyArt.BodyHeight * syScale));
            dc.DrawGeometry(skin.HeadFur, null, TabbyArt.Body);
            DrawBodyMarkingsFluffy(dc, skin);
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
        else
        {
            dc.PushTransform(new MatrixTransform(1, 0, lean * 0.282, 1, -233 * lean * 0.282, 0));
            dc.PushTransform(new TranslateTransform(214, 235));
            dc.PushTransform(new ScaleTransform(sxScale, syScale));
            dc.PushTransform(new TranslateTransform(-214, -235));
            dc.DrawGeometry(skin.BodyFur, null, CatArt.Body);
            DrawBodyMarkingsShort(dc, skin);
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();

            if (sleep > 0.1)
            {
                dc.PushOpacity(CatArt.Smooth((sleep - 0.1) / 0.5));
                dc.DrawEllipse(skin.BodyFur, null, new Point(214, 198), 53.0, 36.0);
                dc.Pop();
            }
        }

        // 3. Optional mid-layer (gamepad)
        if (between != null) between(dc);

        // 4. Arms
        DrawArm(dc, skin, false, rightPaw, lean, rightActivity, pawRaised, sxScale, syScale);
        DrawArm(dc, skin, true, leftPaw, lean, leftActivity, pawRaised, sxScale, syScale);

        // Fluffy chest ruff sits over upper arms when selected or on fluffy base.
        if (fluffy && (skin.Data.BellyStyle == SkinBellyStyle.Ruff || skin.Data.BellyStyle == SkinBellyStyle.FullBelly))
        {
            double bodyYShift = (1.0 - syScale) * (235.0 - TabbyArt.BodyTop);
            dc.PushTransform(new MatrixTransform(1, 0, .285 * lean, 1, -233 * .285 * lean, 0));
            dc.PushTransform(new TranslateTransform(214, TabbyArt.BodyTop + bodyYShift));
            dc.PushTransform(new ScaleTransform(TabbyArt.BodyWidth * sxScale * (1.0 - 0.22 * sleep), TabbyArt.BodyHeight * syScale));
            dc.DrawGeometry(skin.BellyBrush, null, TabbyArt.Ruff);
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }

        // 5. Head
        double nuzzleX = mood.PetNuzzleX * 4.5 * happy;
        double nuzzleY = mood.PetNuzzleY * 2.5 * happy - 1.5 * happy + 2.2 * impatient;
        double shakeX = impatient > 0.02 ? Math.Sin(t * 26.0) * 1.9 * impatient : 0;
        double hx = 214 - (fluffy ? 33.5 : 36.8) * lean + gx * 3.2 * (1 - sleep) - 2.0 * sleep + nuzzleX + shakeX;
        double hyNorm = (fluffy ? TabbyArt.BodyTop - 2 : 86) + breath + gy * 2.0 + lean * 1.4 + 12.0 * crouch + nuzzleY;
        double hy = hyNorm + ((fluffy ? 186 : 180) + breath - hyNorm) * sleep;
        double hrot = (gx * 2.8 - lean * 2.4 + mood.PetNuzzleX * 6.5 * happy + Math.Sin(t * 5.2) * 2.6 * happy + Math.Sin(t * 22.0) * 2.8 * impatient) * (1 - sleep) - 8.5 * sleep;
        double browAnger = Math.Max(mood.Anger, impatient * 0.92);

        dc.PushTransform(new TranslateTransform(hx, hy));
        dc.PushTransform(new RotateTransform(hrot));
        if (fluffy) dc.PushTransform(new ScaleTransform(1.04, 1.0));
        DrawHead(dc, skin, gx, gy, blink, browAnger, angerBreath, crouch, happy);
        if (happy > .05) CatArt.DrawBlush(dc, happy);
        if (mood.Sweat > .01) CatArt.DrawSweat(dc, t, mood.Sweat);
        if (mood.Anger > .01) CatArt.DrawAnger(dc, t, mood.Anger);
        if (impatient > .05) CatArt.DrawImpatientMark(dc, t, impatient);
        if (fluffy) dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    private static void DrawTail(DrawingContext dc, CompiledSkin skin, bool fluffy, double t, CatMood mood,
                                 double happy, double impatient, double sleep, double crouch)
    {
        if (fluffy)
        {
            double tailSpeed = 1.8 + mood.Anger * 2.8 + impatient * 3.4 + crouch * 2.2 - sleep * 1.1 + happy * 0.45;
            double tailAmp = 2.1 + mood.Anger * 2.0 + impatient * 2.6 + crouch * 2.5 + happy * 0.9;
            dc.PushTransform(new TranslateTransform(187, 184 + 14 * sleep));
            dc.PushTransform(new RotateTransform(186 + Math.Sin(t * tailSpeed) * tailAmp));
            dc.PushTransform(new ScaleTransform(1.1, 1.22));
            dc.DrawGeometry(skin.TailFur, null, TabbyArt.Tail);
            if (skin.Data.TailStyle == SkinTailStyle.TabbyRings)
            {
                dc.PushClip(TabbyArt.Tail);
                dc.PushOpacity(0.55);
                dc.DrawEllipse(skin.AccentBrush, null, new Point(26, -8), 14, 18);
                dc.DrawEllipse(skin.AccentBrush, null, new Point(54, -36), 16, 18);
                dc.Pop();
                dc.Pop();
            }
            else if (skin.Data.TailStyle == SkinTailStyle.LightTip)
            {
                dc.PushClip(TabbyArt.Tail);
                dc.DrawEllipse(skin.BellyBrush, null, new Point(66, -48), 18, 20);
                dc.Pop();
            }
            dc.Pop();
            dc.Pop();
            dc.Pop();
            return;
        }

        double freq = 1.9 + mood.Anger * 3.0 + impatient * 3.4 + crouch * 2.4 - sleep * 1.2 + happy * 0.45;
        double amp = 1.0 + mood.Anger * 0.65 + impatient * 0.72 + crouch * 0.75 - sleep * 0.65 + happy * 0.25;
        double wave = Math.Sin(t * freq) * amp;
        double tip = Math.Sin(t * freq - 0.68) * amp;
        double droop = sleep * 24.0;
        StreamGeometry tail = CatArt.BuildTailGeometry(wave, tip, droop);
        Brush tBrush = skin.Data.FacePattern == SkinFacePattern.SiamesePoint ? skin.AccentBrush : skin.TailFur;
        dc.DrawGeometry(tBrush, null, tail);

        if (skin.Data.TailStyle == SkinTailStyle.TabbyRings)
        {
            dc.PushClip(tail);
            TailMark(dc, TailBand, skin.AccentSoftBrush, 275, 208 + wave * .7 + droop * .15, -7);
            TailMark(dc, TailBand, skin.AccentSoftBrush, 289.5, 203 + wave * 1.7 + droop * .45, -43 + wave * 3);
            TailMark(dc, TailBand, skin.AccentSoftBrush, 304, 183 + wave * 2.5 + droop * .75, -48 + tip * 4);
            TailMark(dc, TailBand, skin.AccentSoftBrush, 320, 174 + tip * 5 + droop * .95, -13 + tip * 8);
            dc.Pop();
        }
        else if (skin.Data.TailStyle == SkinTailStyle.LightTip)
        {
            dc.PushClip(tail);
            TailMark(dc, TailTipGeo, skin.BellyBrush, 320, 174 + tip * 5 + droop * .95, -13 + tip * 8);
            dc.Pop();
        }
    }

    private static void TailMark(DrawingContext dc, Geometry shape, Brush color, double x, double y, double angle)
    {
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new RotateTransform(angle));
        dc.DrawGeometry(color, null, shape);
        dc.Pop();
        dc.Pop();
    }

    private static void DrawBodyMarkingsShort(DrawingContext dc, CompiledSkin skin)
    {
        dc.PushClip(CatArt.Body);
        if (skin.Data.BodyPattern == SkinBodyPattern.FlankStripes)
        {
            dc.PushOpacity(.70);
            dc.DrawGeometry(skin.AccentBrush, null, FlanksShort);
            dc.Pop();
        }
        else if (skin.Data.BodyPattern == SkinBodyPattern.Patches)
        {
            dc.DrawGeometry(skin.AccentBrush, null, PatchesShort);
        }

        if (skin.Data.BellyStyle == SkinBellyStyle.Bib)
            dc.DrawGeometry(skin.BellyBrush, null, BibSmall);
        else if (skin.Data.BellyStyle == SkinBellyStyle.FullBelly || skin.Data.BellyStyle == SkinBellyStyle.Ruff)
            dc.DrawGeometry(skin.BellyBrush, null, FullBellyShort);
        dc.Pop();
    }

    private static void DrawBodyMarkingsFluffy(DrawingContext dc, CompiledSkin skin)
    {
        dc.PushClip(TabbyArt.Body);
        if (skin.Data.BodyPattern == SkinBodyPattern.FlankStripes)
        {
            dc.PushOpacity(.66);
            dc.DrawGeometry(skin.AccentBrush, null, FlanksFluffy);
            dc.Pop();
        }
        else if (skin.Data.BodyPattern == SkinBodyPattern.Patches)
        {
            dc.DrawGeometry(skin.AccentBrush, null, PatchesFluffy);
        }
        if (skin.Data.BellyStyle == SkinBellyStyle.FullBelly || skin.Data.BellyStyle == SkinBellyStyle.Bib)
            dc.DrawGeometry(skin.BellyBrush, null, FullBellyFluffy);
        dc.Pop();
    }

    private static void DrawArm(DrawingContext dc, CompiledSkin skin, bool left, Point paw, double lean, double activity,
                                double pawRaised, double sxScale, double syScale)
    {
        double side = left ? -1 : 1;
        double raised = pawRaised >= 0 ? pawRaised : CatArt.Smooth((221 - paw.Y) / 32);
        Point a = CatArt.ShoulderPoint(left, lean, sxScale, syScale);
        Point b, c;
        CatArt.ComputeArmControls(a, paw, side, raised, out b, out c);
        double pawRadius = 8.4 + CatArt.Limit(activity, 0, 1) * 1.0;
        StreamGeometry limb = CatArt.BuildArmGeometry(a, b, c, paw, pawRadius);

        Brush armFill;
        if (skin.Data.PawStyle == SkinPawStyle.DarkPoints)
            armFill = CatArt.ArmGradient(skin.FurLightColor, skin.FurDarkColor, 0.42, skin.PawColor, a, paw);
        else
            armFill = CatArt.ArmGradient(skin.FurLightColor, skin.FurLightColor, 0.30, skin.FurDarkColor, a, paw);

        dc.DrawGeometry(armFill, null, limb);

        if (skin.Data.PawStyle == SkinPawStyle.Socks)
        {
            Vector dir = paw - c;
            if (dir.LengthSquared < .001) dir = new Vector(0, 1);
            dir.Normalize();
            dc.PushClip(limb);
            dc.PushTransform(new MatrixTransform(dir.Y, -dir.X, dir.X, dir.Y, paw.X, paw.Y));
            dc.DrawGeometry(skin.PawBrush, null, FrontSock);
            dc.Pop();
            dc.Pop();
            dc.DrawEllipse(skin.PawBrush, null, paw, pawRadius + .35, pawRadius * (.94 - .06 * activity));
        }
        else if (skin.Data.PawStyle == SkinPawStyle.DarkPoints)
        {
            dc.DrawEllipse(skin.PawBrush, null, paw, pawRadius + .35, pawRadius * (.94 - .06 * activity));
        }
        else
        {
            dc.DrawEllipse(armFill, null, paw, pawRadius + .35, pawRadius * (.94 - .06 * activity));
        }
    }

    private static void DrawHead(DrawingContext dc, CompiledSkin skin, double gx, double gy, double blink,
                                 double anger, double angerBreath, double crouch, double happyPet)
    {
        Geometry headGeo = skin.Data.BodyBase == SkinBodyBase.FluffyLong ? TabbyArt.Head : CatArt.Head;
        dc.DrawGeometry(skin.HeadFur, null, headGeo);

        // Pre-ear layer: ear tips & Siamese ear dark wash.
        dc.PushClip(headGeo);
        if (skin.Data.FacePattern == SkinFacePattern.SiamesePoint)
        {
            dc.DrawRectangle(skin.SiameseEarWash, null, new Rect(-50, -68, 42, 32));
            dc.DrawRectangle(skin.SiameseEarWash, null, new Rect(12, -68, 38, 32));
        }
        if (skin.Data.DarkEarTips)
        {
            dc.PushOpacity(.85);
            dc.DrawGeometry(skin.EarTipBrush, null, EarTipLeft);
            dc.DrawGeometry(skin.EarTipBrush, null, EarTipRight);
            dc.Pop();
        }
        dc.Pop();

        // Inner ears
        dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarLeft);
        dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarRight);

        // Face pattern markings clipped inside head
        dc.PushClip(headGeo);
        switch (skin.Data.FacePattern)
        {
            case SkinFacePattern.CoffeeMask:
                dc.PushOpacity(.68);
                dc.DrawGeometry(skin.AccentBrush, null, ForeheadClean);
                dc.Pop();
                dc.PushOpacity(.50);
                dc.DrawGeometry(skin.AccentBrush, null, CheekClean);
                dc.Pop();
                dc.PushTransform(new TranslateTransform(gx * 2.4, 0));
                dc.DrawGeometry(skin.BellyBrush, null, CoffeeWhiteMask);
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.TabbyM:
                dc.PushOpacity(.78);
                dc.DrawGeometry(skin.AccentBrush, null, ForeheadClassicM);
                dc.Pop();
                dc.PushOpacity(.58);
                dc.DrawGeometry(skin.AccentBrush, null, CheekClean);
                dc.Pop();
                dc.PushTransform(new TranslateTransform(gx * 2.6, 0));
                if (skin.Data.BellyStyle != SkinBellyStyle.None)
                    dc.DrawGeometry(skin.BellyBrush, null, MuzzleOval);
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.SiamesePoint:
                dc.PushTransform(new TranslateTransform(gx * 2.8, 0));
                dc.DrawEllipse(skin.SiameseMaskWash, null, new Point(0, -5), 33, 27);
                dc.DrawEllipse(skin.SiameseMaskLowerWash, null, new Point(0, 9), 14, 17);
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.Tuxedo:
                dc.PushTransform(new TranslateTransform(gx * 2.5, 0));
                dc.DrawGeometry(skin.BellyBrush, null, TuxedoLowerFace);
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.Calico:
                dc.DrawGeometry(skin.BellyBrush, null, headGeo);
                dc.DrawGeometry(skin.EarTipBrush, null, CalicoLeftPatch);
                dc.DrawGeometry(skin.AccentBrush, null, CalicoRightPatch);
                dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarLeft);
                dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarRight);
                dc.PushTransform(new TranslateTransform(gx * 2.5, 0));
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.CowSpots:
                dc.DrawGeometry(skin.BellyBrush, null, headGeo);
                dc.DrawGeometry(skin.AccentBrush, null, CowHeadSpots);
                dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarLeft);
                dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarRight);
                dc.PushTransform(new TranslateTransform(gx * 2.5, 0));
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            default:
                if (skin.Data.BellyStyle == SkinBellyStyle.FullBelly)
                {
                    dc.PushTransform(new TranslateTransform(gx * 2.6, 0));
                    dc.DrawGeometry(skin.BellyBrush, null, MuzzleOval);
                    DrawNoseAndMouth(dc, skin);
                    dc.Pop();
                }
                else
                {
                    dc.PushTransform(new TranslateTransform(gx * 2.6, 0));
                    DrawNoseAndMouth(dc, skin);
                    dc.Pop();
                }
                break;
        }
        dc.Pop(); // end head clip

        // Eyes (supports heterochromia + SleekOval / RoundCute pupils)
        DrawEye(dc, skin, -1, gx, gy, CatArt.Limit(blink, 0, 1), anger, angerBreath, crouch, happyPet);
        DrawEye(dc, skin, 1, gx, gy, CatArt.Limit(blink, 0, 1), anger, angerBreath, crouch, happyPet);

        // Whiskers
        DrawWhiskers(dc, skin, -1, gx);
        DrawWhiskers(dc, skin, 1, gx);

        // Accessory
        if (skin.Data.Accessory != SkinAccessory.None)
            DrawAccessory(dc, skin);
    }

    private static void DrawNoseAndMouth(DrawingContext dc, CompiledSkin skin)
    {
        dc.DrawGeometry(null, skin.LipPen, SmileShape);
        dc.DrawGeometry(skin.NoseBrush, null, NoseShape);
        dc.DrawEllipse(NoseGlint, null, new Point(-1.3, 4.6), 1.4, 0.75);
    }

    private static void DrawEye(DrawingContext dc, CompiledSkin skin, double side, double gx, double gy, double blink,
                                double anger, double angerBreath, double crouch, double happyPet)
    {
        double angle = side * 3 + gx * 4;
        double x = side * 19.1 + gx * 2.6;
        double y = -15.8 + gy * 1.7 - side * gx * 1.1;
        double width = 13.0 * (1 - Math.Max(0, side * gx) * 0.095);
        double height = 12.5 * (1 - blink);
        dc.PushTransform(new RotateTransform(angle, x, y));

        if (happyPet > 0.32 && blink < 0.85)
        {
            CatArt.DrawHappyEye(dc, x, y, width, skin.LidPen);
        }
        else if (height < 1.6)
        {
            StreamGeometry slit = new StreamGeometry();
            using (StreamGeometryContext g = slit.Open())
            {
                g.BeginFigure(new Point(x - width * 0.86, y), false, false);
                g.QuadraticBezierTo(new Point(x, y + 3.0), new Point(x + width * 0.86, y - 0.2), true, true);
            }
            dc.DrawGeometry(null, skin.LidPen, slit);
        }
        else
        {
            Brush irisBrush = side < 0 ? skin.LeftIrisBrush : skin.RightIrisBrush;
            EllipseGeometry iris = new EllipseGeometry(new Point(x, y), width, height);
            dc.DrawGeometry(irisBrush, skin.EyeEdgePen, iris);
            dc.PushClip(iris);
            double boost = 1.0 + 0.16 * crouch;
            if (skin.Data.PupilStyle == SkinPupilStyle.RoundCute)
            {
                Point p = new Point(x + gx * 5.2, y + gy * 4.2);
                dc.DrawEllipse(skin.PupilBrush, null, p, 7.6 * boost, 9.6 * boost);
                dc.DrawEllipse(skin.GlintBrush, null, new Point(p.X - 2.1, p.Y - 3.3), 2.35, 2.4);
                dc.DrawEllipse(skin.GlintBrush, null, new Point(p.X + 3.0, p.Y + 4.2), 0.85, 0.85);
            }
            else
            {
                Point p = new Point(x + gx * 7.4, y + gy * 6.2);
                double rx = (4.4 + Math.Abs(gy) * 0.75) * boost;
                double ry = 8.2 * boost;
                dc.DrawEllipse(skin.PupilBrush, null, p, rx, ry);
                dc.DrawEllipse(skin.GlintBrush, null, new Point(p.X - 1.5, p.Y - 2.9), 1.55, 1.65);
            }
            dc.Pop();
            if (anger > .01)
                CatArt.DrawAngryEyelid(dc, x, y, width, height, side, skin.LidFur, skin.LidPen, anger, angerBreath);
        }
        dc.Pop();
    }

    private static void DrawWhiskers(DrawingContext dc, CompiledSkin skin, double side, double gx)
    {
        double cheek = side * 26 + gx * 3;
        for (int k = 0; k < 3; ++k)
        {
            Point start = new Point(cheek + side * (0.8 - k * 0.4), 6 + k * 5.0);
            Point end = new Point(cheek + side * (29 - k * 2.4), start.Y + (k - 0.8) * 5.6);
            StreamGeometry whisker = new StreamGeometry();
            using (StreamGeometryContext g = whisker.Open())
            {
                g.BeginFigure(start, false, false);
                g.QuadraticBezierTo(new Point((start.X + end.X) / 2, start.Y - 1.1 + k * 0.65), end, true, true);
            }
            dc.DrawGeometry(null, skin.WhiskerPen, whisker);
            dc.DrawLine(skin.WhiskerRootPen, start, new Point(start.X + side * (6.2 - k * 0.9), start.Y - 0.45 + k * 0.65));
        }
    }

    private static void DrawAccessory(DrawingContext dc, CompiledSkin skin)
    {
        switch (skin.Data.Accessory)
        {
            case SkinAccessory.CollarBell:
                dc.DrawGeometry(skin.AccPrimary, skin.AccEdgePen, CollarBand);
                dc.DrawEllipse(BellGold, BellEdge, new Point(0, 39.5), 5.6, 5.6);
                dc.DrawLine(BellEdge, new Point(-3.5, 40.5), new Point(3.5, 40.5));
                dc.DrawEllipse(skin.AccHighlight, null, new Point(-1.8, 37.6), 1.5, 1.1);
                break;

            case SkinAccessory.BowTie:
                dc.DrawGeometry(skin.AccPrimary, skin.AccEdgePen, CollarBand);
                dc.DrawGeometry(skin.AccPrimary, skin.AccEdgePen, BowLeft);
                dc.DrawGeometry(skin.AccPrimary, skin.AccEdgePen, BowRight);
                dc.DrawEllipse(skin.AccPrimary, skin.AccEdgePen, new Point(0, 36.2), 3.6, 3.4);
                break;

            case SkinAccessory.HeadRibbon:
                dc.PushTransform(new TranslateTransform(26.0, -42.0));
                dc.PushTransform(new RotateTransform(22.0));
                dc.DrawGeometry(skin.AccPrimary, skin.AccEdgePen, RibbonLeft);
                dc.DrawGeometry(skin.AccPrimary, skin.AccEdgePen, RibbonRight);
                dc.DrawEllipse(skin.AccPrimary, skin.AccEdgePen, new Point(0, 0.5), 3.4, 3.2);
                dc.Pop();
                dc.Pop();
                break;

            case SkinAccessory.Glasses:
                dc.DrawEllipse(GlassLens, skin.AccEdgePen, new Point(-19.5, -15.5), 14.5, 13.8);
                dc.DrawEllipse(GlassLens, skin.AccEdgePen, new Point(19.5, -15.5), 14.5, 13.8);
                dc.DrawLine(skin.AccEdgePen, new Point(-5.0, -16.0), new Point(5.0, -16.0));
                dc.DrawLine(skin.AccEdgePen, new Point(-34.0, -16.5), new Point(-45.5, -19.5));
                dc.DrawLine(skin.AccEdgePen, new Point(34.0, -16.5), new Point(45.5, -19.5));
                dc.DrawLine(GlassGlint, new Point(-26.0, -21.5), new Point(-22.0, -24.0));
                dc.DrawLine(GlassGlint, new Point(13.0, -21.5), new Point(17.0, -24.0));
                break;

            case SkinAccessory.HeadOrange:
                dc.PushTransform(new TranslateTransform(0, -54.0));
                dc.DrawEllipse(skin.AccPrimary, skin.AccEdgePen, new Point(0, 0), 12.2, 10.6);
                dc.DrawEllipse(skin.AccHighlight, null, new Point(-4.0, -3.6), 3.0, 1.8);
                dc.DrawGeometry(LeafGreen, LeafEdge, OrangeLeaf);
                dc.Pop();
                break;

            case SkinAccessory.Ahoge:
                dc.DrawGeometry(skin.HeadFur, skin.EyeEdgePen, AhogeShape);
                break;
        }
    }
}
