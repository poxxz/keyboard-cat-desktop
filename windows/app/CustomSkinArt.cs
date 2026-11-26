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
    public readonly Pen FoldCreasePen;
    public readonly Color FurLightColor;
    public readonly Color FurDarkColor;
    public readonly Color PawColor;

    public CompiledSkin(CatSkinData data)
    {
        Data = data.Clone();
        Color fur = CatSkinData.ParseHex(Data.FurColor, Color.FromRgb(117, 127, 140));
        Color accent = CatSkinData.ParseHex(Data.AccentColor, Color.FromRgb(76, 85, 98));
        Color belly = CatSkinData.ParseHex(Data.BellyColor, Color.FromRgb(255, 253, 248));
        Color paw = CatSkinData.ParseHex(Data.PawColor, Color.FromRgb(255, 253, 248));
        Color innerEar = CatSkinData.ParseHex(Data.InnerEarColor, Color.FromRgb(216, 164, 160));
        Color nose = CatSkinData.ParseHex(Data.NoseColor, Color.FromRgb(91, 96, 104));
        Color whisker = CatSkinData.ParseHex(Data.WhiskerColor, Color.FromRgb(230, 233, 238));
        Color leftEye = CatSkinData.ParseHex(Data.LeftEyeColor, Color.FromRgb(235, 157, 40));
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

        SiameseEarWash = BuildSiameseEar(accent);
        SiameseMaskWash = BuildRadialFade(accent, 235, 185);
        SiameseMaskLowerWash = BuildRadialFade(accent, 220, 165);

        AccPrimary = RelWash(Adjust(acc, 1.12, 10), Adjust(acc, 0.82, -6), new Point(.2, 0), new Point(.8, 1));
        AccHighlight = Solid(Color.FromArgb(190, 255, 252, 242));
        AccEdgePen = MakePen(Adjust(acc, 0.56, -12), 1.15);
        FoldCreasePen = MakePen(Adjust(fur, 0.62, -14), 1.45);

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
    // Chubby British Shorthair round cheeks + softer rounded ears.
    private static readonly Geometry BritishHead = Parse(
        "M -47,-21 C -48,-38 -43,-55 -35,-60 C -29,-61 -21,-51 -16,-45 " +
        "Q 0,-49 16,-45 C 21,-51 29,-61 35,-60 C 43,-55 48,-38 47,-21 " +
        "C 57,-7 58,12 49,24 C 39,37 22,42 0,42 C -22,42 -39,37 -49,24 C -58,12 -57,-7 -47,-21 Z");

    // Scottish Fold round head + folded-down ear flaps.
    private static readonly Geometry FoldHead = Parse(
        "M -47,-20 C -47,-39 -31,-48 0,-48 C 31,-48 47,-39 47,-20 " +
        "C 56,-6 56,12 48,24 C 38,37 22,41 0,41 C -22,41 -38,37 -48,24 C -56,12 -56,-6 -47,-20 Z");
    private static readonly Geometry FoldEarFlapL = Parse(
        "M -46,-25 C -47,-40 -36,-50 -20,-47 C -29,-40 -36,-32 -44,-23 Z");
    private static readonly Geometry FoldEarFlapR = Parse(
        "M 46,-25 C 47,-40 36,-50 20,-47 C 29,-40 36,-32 44,-23 Z");
    private static readonly Geometry FoldEarCrease = Parse(
        "M -43,-24 Q -33,-36 -21,-45 M 43,-24 Q 33,-36 21,-45");

    private static readonly Geometry EarTipLeft = Parse("M -44,-49 Q -41,-59 -36,-63 Q -32,-63 -28,-57 Q -35,-54 -41,-49 Z");
    private static readonly Geometry EarTipRight = Parse("M 28,-57 Q 33,-64 37,-63 Q 42,-58 44,-48 Q 39,-53 28,-57 Z");

    // Forehead & cheek patterns
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
    // American Shorthair bold 3-stripe mascara cheeks + eye-corner liner.
    private static readonly Geometry AmShortCheeks = Parse(
        "M -51,-15 Q -40,-15 -30,-11.5 Q -39,-9.5 -51,-11 Z " +
        "M -52,-4.5 Q -41,-4.5 -31,-1.5 Q -40,0.5 -52,-0.5 Z " +
        "M -49,6 Q -39,5.5 -31,8.5 Q -40,10 -49,9.5 Z " +
        "M 51,-15 Q 40,-15 30,-11.5 Q 39,-9.5 51,-11 Z " +
        "M 52,-4.5 Q 41,-4.5 31,-1.5 Q 40,0.5 52,-0.5 Z " +
        "M 49,6 Q 39,5.5 31,8.5 Q 40,10 49,9.5 Z");

    // Coffee open-face white lower mask with narrow inverted-V nose blaze.
    private static readonly Geometry CoffeeWhiteMask = Parse(
        "M 0,-17.0 C 1.6,-10.2 4.0,-2.6 8.2,3.4 C 12.4,9.2 18.2,13.8 25.5,16.0 " +
        "Q 34.4,17.2 43.8,15.2 Q 48.0,13.4 52.5,10.8 L 53.5,39.0 L -53.5,39.0 L -52.5,10.8 " +
        "Q -48.0,13.4 -43.8,15.2 Q -34.4,17.2 -25.5,16.0 C -18.2,13.8 -12.4,9.2 -8.2,3.4 " +
        "C -4.0,-2.6 -1.6,-10.2 0,-17.0 Z");

    // British Blue-White & Ragdoll symmetrical inverted-V ("正八") blaze.
    private static readonly Geometry BlueBicolorBlaze = Parse(
        "M 0,-36.0 C 5.0,-22.0 11.0,-8.0 21.5,1.5 C 32.0,10.0 44.0,13.5 56.0,12.5 " +
        "L 56.0,44.0 L -56.0,44.0 L -56.0,12.5 C -44.0,13.5 -32.0,10.0 -21.5,1.5 " +
        "C -11.0,-8.0 -5.0,-22.0 0,-36.0 Z");

    // Ragdoll dark upper-mask around ears & eyes (underneath the white inverted-V).
    private static readonly Geometry RagdollUpperMask = Parse(
        "M -56,-68 L 56,-68 L 56,12 C 42,14 28,11 18,4 C 10,-2 4,-16 0,-28 " +
        "C -4,-16 -10,-2 -18,4 C -28,11 -42,14 -56,12 Z");

    // Tuxedo symmetrical lower face.
    private static readonly Geometry TuxedoLowerFace = Parse(
        "M 0,-26.0 C 5.5,-14.0 11.5,-2.5 21.5,5.5 C 31.0,12.5 42.0,14.5 55.0,12.0 " +
        "L 55.0,44.0 L -55.0,44.0 L -55.0,12.0 C -42.0,14.5 -31.0,12.5 -21.5,5.5 " +
        "C -11.5,-2.5 -5.5,-14.0 0,-26.0 Z");

    private static readonly Geometry CalicoLeftPatch = Parse(
        "M -56,-66 L -3,-66 C -1,-46 -7,-28 -19,-18 C -30,-9 -44,-8 -56,-11 Z");
    private static readonly Geometry CalicoRightPatch = Parse(
        "M 4,-66 L 56,-66 L 56,-6 C 41,-5 27,-9 16,-19 C 6,-29 2,-45 4,-66 Z");

    private static readonly Geometry CowHeadSpots = Parse(
        "M -54,-65 L -8,-65 C -4,-44 -14,-22 -28,-14 C -38,-9 -47,-10 -55,-14 Z " +
        "M 18,-65 L 54,-65 L 54,-32 C 41,-28 29,-34 22,-44 C 17,-51 16,-58 18,-65 Z " +
        "M 28,2 C 37,-3 49,1 52,12 C 47,23 36,27 26,23 C 19,18 20,7 28,2 Z");

    // Tortoiseshell warm mottled splashes.
    private static readonly Geometry TortiePatches = Parse(
        "M -48,-52 C -34,-54 -22,-42 -26,-28 C -31,-16 -46,-14 -54,-24 Z " +
        "M 12,-42 C 26,-48 44,-38 42,-22 C 39,-10 22,-8 12,-18 C 6,-26 6,-36 12,-42 Z " +
        "M -36,4 C -24,-1 -12,6 -16,18 C -21,28 -36,27 -44,18 Z " +
        "M 22,8 C 34,4 46,12 42,24 C 36,32 22,30 16,21 Z");

    private static readonly Geometry MuzzleOval = Parse(
        "M -25,10 C -23,3 -16,-1 -8,.7 C -4,1 -1,3 0,4 C 5,1 11,-1 17,1 " +
        "C 25,3 28,10 24,17 C 20,24 11,29 1,30 C -10,31 -20,28 -24,21 Q -28,16 -25,10 Z");
    private static readonly Geometry NoseShape = Parse(
        "M -4.8,5.0 Q -3.6,2.9 0,3.9 Q 3.7,2.8 4.9,4.9 Q 5.1,6.5 2.1,8.8 Q .3,10.3 -.8,9.6 Q -5.2,6.9 -4.8,5.0 Z");
    private static readonly Geometry SmileShape = Parse(
        "M 0,9.1 L 0,12.3 C -1.5,15.7 -5.4,15.9 -7.6,13.0 M 0,12.3 C 1.8,15.3 5.5,15.6 7.8,12.9");

    // Body markings (short-hair / British / Fold coordinates around 214, 170).
    private static readonly Geometry BibSmall = Parse(
        "M 182,102 C 189,112 205,110 214,111 C 225,110 240,111 246,101 " +
        "Q 248,124 236,145 L 235,138 Q 230,153 220,163 L 219,156 " +
        "Q 216,162 210,167 Q 205,158 199,153 L 198,157 Q 183,141 181,124 Z");
    private static readonly Geometry FullBellyShort = Parse(
        "M 182,102 C 189,112 205,110 214,111 C 225,110 240,111 244,102 " +
        "Q 251,110 246,118 Q 241,128 249,138 Q 257,150 250,164 Q 245,178 254,194 " +
        "Q 261,210 258,226 C 260,233 259,238 254,239 L 178,239 C 172,238 167,233 166,226 " +
        "Q 169,210 168,194 Q 175,178 171,164 Q 166,150 175,138 Q 182,128 178,118 Q 174,110 182,102 Z");
    private static readonly Geometry HeartChestShort = Parse(
        "M 214,174 C 210,170 190,156 190,141 C 190,130 198,124 205,124 " +
        "C 210,124 213,128 214,133 C 215,128 218,124 223,124 " +
        "C 230,124 238,130 238,141 C 238,156 218,170 214,174 Z");
    private static readonly Geometry TabbyBellySpotsShort = Parse(
        "M 196,152 Q 214,158 232,152 Q 214,163 196,152 Z " +
        "M 192,172 Q 214,178 236,172 Q 214,183 192,172 Z " +
        "M 195,193 Q 214,198 233,193 Q 214,203 195,193 Z");

    private static readonly Geometry FlanksShort = Parse(
        "M 155,168 C 167,166 174,170 181,176 Q 169,173 157,176 Z " +
        "M 156,180 Q 171,178 179,186 Q 169,183 156,188 Z " +
        "M 272,164 Q 257,164 249,173 Q 261,169 272,173 Z " +
        "M 271,178 Q 258,177 250,186 Q 262,182 272,185 Z");
    // Classic American Shorthair bullseye / oyster swirl flank markings.
    private static readonly Geometry SwirlShort = Parse(
        "M 152,148 C 170,144 185,158 183,178 C 181,196 164,206 152,198 " +
        "L 152,188 C 162,193 172,186 173,176 C 174,164 164,156 152,158 Z " +
        "M 154,168 C 161,167 166,172 165,178 C 164,183 158,185 154,182 Z " +
        "M 276,148 C 258,144 243,158 245,178 C 247,196 264,206 276,198 " +
        "L 276,188 C 266,193 256,186 255,176 C 254,164 264,156 276,158 Z " +
        "M 274,168 C 267,167 262,172 263,178 C 264,183 270,185 274,182 Z");
    private static readonly Geometry SpottedShort = Parse(
        "M 164,146 A 7,6 0 1 1 163.9,146 Z M 176,168 A 6.5,5.5 0 1 1 175.9,168 Z " +
        "M 163,188 A 7.5,6 0 1 1 162.9,188 Z M 176,208 A 6,5 0 1 1 175.9,208 Z " +
        "M 264,146 A 7,6 0 1 1 263.9,146 Z M 252,168 A 6.5,5.5 0 1 1 251.9,168 Z " +
        "M 265,188 A 7.5,6 0 1 1 264.9,188 Z M 252,208 A 6,5 0 1 1 251.9,208 Z");
    private static readonly Geometry PatchesShort = Parse(
        "M 156,136 C 174,132 189,144 186,166 C 182,184 166,192 154,186 Z " +
        "M 270,152 C 252,148 238,162 242,184 C 246,204 260,210 272,202 Z");
    private static readonly Geometry BackSaddleShort = Parse(
        "M 152,102 L 194,102 C 186,136 184,174 190,212 L 150,212 Z " +
        "M 234,102 L 276,102 L 278,212 L 238,212 C 244,174 242,136 234,102 Z");

    // Body markings (fluffy longhair local coordinates around 0, 58).
    private static readonly Geometry FullBellyFluffy = Parse(
        "M -28,0 Q -36,42 -35,84 Q -33,112 -24,122 L 24,122 Q 34,112 36,84 Q 37,42 28,0 Z");
    private static readonly Geometry HeartChestFluffy = Parse(
        "M 0,68 C -4,64 -20,52 -20,39 C -20,30 -13,25 -7,25 C -3,25 -1,28 0,32 " +
        "C 1,28 3,25 7,25 C 13,25 20,30 20,39 C 20,52 4,64 0,68 Z");
    private static readonly Geometry TabbyBellySpotsFluffy = Parse(
        "M -18,52 Q 0,58 18,52 Q 0,63 -18,52 Z M -20,74 Q 0,80 20,74 Q 0,85 -20,74 Z");
    private static readonly Geometry FlanksFluffy = Parse(
        "M -56,56 C -42,54 -34,58 -28,64 Q -40,61 -54,64 Z " +
        "M -55,70 Q -40,68 -30,76 Q -42,73 -55,78 Z " +
        "M 56,52 Q 42,52 32,61 Q 45,57 56,61 Z " +
        "M 55,68 Q 42,67 33,76 Q 46,72 56,75 Z");
    private static readonly Geometry SwirlFluffy = Parse(
        "M -58,42 C -40,38 -26,52 -28,72 C -30,90 -44,98 -58,92 L -58,82 C -46,86 -38,80 -37,70 C -36,60 -46,52 -58,54 Z " +
        "M 58,42 C 40,38 26,52 28,72 C 30,90 44,98 58,92 L 58,82 C 46,86 38,80 37,70 C 36,60 46,52 58,54 Z");
    private static readonly Geometry SpottedFluffy = Parse(
        "M -46,42 A 6.5,5.5 0 1 1 -46.1,42 Z M -36,66 A 6,5 0 1 1 -36.1,66 Z M -48,88 A 7,5.5 0 1 1 -48.1,88 Z " +
        "M 46,42 A 6.5,5.5 0 1 1 45.9,42 Z M 36,66 A 6,5 0 1 1 35.9,66 Z M 48,88 A 7,5.5 0 1 1 47.9,88 Z");
    private static readonly Geometry PatchesFluffy = Parse(
        "M -56,28 C -36,24 -22,38 -25,62 C -29,82 -44,88 -56,80 Z " +
        "M 56,42 C 36,38 22,52 25,78 C 29,96 44,102 56,94 Z");
    private static readonly Geometry BackSaddleFluffy = Parse(
        "M -58,0 L -22,0 C -28,34 -30,68 -24,104 L -58,104 Z " +
        "M 22,0 L 58,0 L 58,104 L 24,104 C 30,68 28,34 22,0 Z");

    private static readonly Geometry FrontSock = Parse(
        "M -9.2,1 C -10,-4 -9.9,-9 -7.3,-10.7 Q -5,-12 -2,-10.8 Q 1,-9.3 3.2,-10.7 " +
        "Q 6.1,-12.5 8.3,-10.4 Q 10.3,-7 9.2,1 Q 7.2,8 0,9 Q -7.7,7.9 -9.2,1 Z");
    private static readonly Geometry HighBootShape = Parse(
        "M -11.5,2 L -12.0,-32 Q -6,-35 0,-32 Q 6,-35 12.0,-32 L 11.5,2 Q 8,10 0,10 Q -8,10 -11.5,2 Z");
    private static readonly Geometry WristBracelets = Parse(
        "M -13,-14 Q 0,-11 13,-14 L 13,-9 Q 0,-6 -13,-9 Z " +
        "M -13,-27 Q 0,-24 13,-27 L 13,-22 Q 0,-19 -13,-22 Z " +
        "M -13,-40 Q 0,-37 13,-40 L 13,-35 Q 0,-32 -13,-35 Z");
    private static readonly Geometry TailBand = Parse(
        "M -3,-13 Q .7,-11 4,-13 Q 1,0 4,13 Q -.2,11 -3,13 Q -4.4,0 -3,-13 Z");
    private static readonly Geometry TailTipGeo = Parse(
        "M -3,-13 L 24,-13 L 24,14 L -4,14 Q .4,4 -1,-1 Q -4,-6 -3,-13 Z");
    private static readonly Geometry BobtailPomShape = Parse(
        "M -4,8 C -10,0 -8,-14 4,-18 C 16,-22 28,-12 28,0 C 28,12 18,20 6,18 C 0,17 -2,12 -4,8 Z");

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

    public static PetZone ContainsCat(CompiledSkin skin, Point pointer, double time, double gazeX, double gazeY, Point leftPaw, double sleep, double margin = 0)
    {
        if (skin != null && skin.Data.BodyBase == SkinBodyBase.FluffyLong)
            return TabbyArt.ContainsCat(pointer, time, gazeX, gazeY, leftPaw, sleep, margin);
        return CatArt.ContainsCat(CatStyle.Wednesday, pointer, time, gazeX, gazeY, leftPaw, sleep, margin);
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
        bool chubby = skin.Data.BodyBase == SkinBodyBase.ChubbyBritish || skin.Data.BodyBase == SkinBodyBase.FoldEar;
        double gx = CatArt.Limit(gazeX, -1, 1);
        double gy = CatArt.Limit(gazeY, -1, 1);
        double sleep = CatArt.Limit(mood.Sleep, 0, 1);
        double lift = CatArt.Limit(mood.ScruffLift, 0, 1);
        double crouch = CatArt.Limit(mood.Crouch, 0, 1) * (1 - lift);
        double happy = CatArt.Limit(mood.HappyPet, 0, 1) * (1 - sleep) * (1 - lift);
        double impatient = CatArt.Limit(mood.Impatient, 0, 1) * (1 - sleep) * (1 - lift);
        double highLeft = CatArt.Smooth((195 - leftPaw.Y) / 45);
        double lean = CatArt.Smooth((157 - leftPaw.X) / 50) * (1 - sleep) * (1 - highLeft) * (1 - lift);
        double breath = Math.Sin(t * (sleep > 0.5 ? 1.15 : 1.7)) * (sleep > 0.5 ? 1.7 : 0.72);
        double angerBreath = 0.5 + 0.5 * Math.Sin(t * 6.8);

        double baseWidthFactor = chubby ? 1.07 : 1.0;
        double syScale = 1.0 - 0.45 * sleep - 0.10 * crouch + 0.025 * happy * Math.Sin(t * 5.0) + 0.075 * lift;
        double sxScale = (1.0 + 0.04 * sleep + 0.04 * crouch + 0.02 * happy - 0.065 * lift) * baseWidthFactor;

        // 1. Tail
        DrawTail(dc, skin, fluffy, t, mood, happy, impatient, sleep, crouch, lift);

        // 2. Torso
        double torsoShear = lean * 0.282 - lift * 0.13;
        double torsoOffset = -233 * lean * 0.282 + 108 * lift * 0.13;
        if (fluffy)
        {
            double bodyYShift = (1.0 - syScale) * (235.0 - TabbyArt.BodyTop);
            dc.PushTransform(new MatrixTransform(1, 0, torsoShear, 1, torsoOffset, 0));
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
            dc.PushTransform(new MatrixTransform(1, 0, torsoShear, 1, torsoOffset, 0));
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
                dc.DrawEllipse(skin.BodyFur, null, new Point(214, 198), 53.0 * baseWidthFactor, 36.0);
                dc.Pop();
            }
        }
        if (lift > .02) CatArt.DrawScruffNape(dc, fluffy ? skin.HeadFur : skin.BodyFur, lift);

        // 3. Optional mid-layer (gamepad or hugged keyboard & mouse during drag)
        if (between != null) between(dc);

        // 4. Arms
        DrawArm(dc, skin, false, rightPaw, lean, rightActivity, pawRaised, sxScale, syScale);
        DrawArm(dc, skin, true, leftPaw, lean, leftActivity, pawRaised, sxScale, syScale);

        // Fluffy chest ruff sits over upper arms when selected on fluffy base.
        if (fluffy && (skin.Data.BellyStyle == SkinBellyStyle.Ruff || skin.Data.BellyStyle == SkinBellyStyle.FullBelly))
        {
            double bodyYShift = (1.0 - syScale) * (235.0 - TabbyArt.BodyTop);
            dc.PushTransform(new MatrixTransform(1, 0, torsoShear, 1, torsoOffset, 0));
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
        double hx = 214 - (fluffy ? 33.5 : 36.8) * lean + gx * 3.2 * (1 - sleep) - 2.0 * sleep + nuzzleX + shakeX - 9.5 * lift;
        double hyNorm = (fluffy ? TabbyArt.BodyTop - 2 : 86) + breath + gy * 2.0 + lean * 1.4 + 12.0 * crouch + nuzzleY + 5.0 * lift;
        double hy = hyNorm + ((fluffy ? 186 : 180) + breath - hyNorm) * sleep;
        double hrot = (gx * 2.8 - lean * 2.4 + mood.PetNuzzleX * 6.5 * happy + Math.Sin(t * 5.2) * 2.6 * happy + Math.Sin(t * 22.0) * 2.8 * impatient) * (1 - sleep) * (1 - lift)
            - 8.5 * sleep - 10.5 * lift - mood.DragSwing * 0.15 * lift;
        double browAnger = Math.Max(mood.Anger, impatient * 0.92) * (1 - lift);
        double eyeBoost = Math.Max(crouch, lift * 0.55);
        double headScaleX = fluffy ? 1.04 : (chubby ? 1.04 : 1.0);

        dc.PushTransform(new TranslateTransform(hx, hy));
        dc.PushTransform(new RotateTransform(hrot));
        dc.PushTransform(new ScaleTransform(headScaleX, 1.0));
        DrawHead(dc, skin, gx, gy, blink, browAnger, angerBreath, eyeBoost, happy);
        double blushAmt = Math.Max(happy, lift * 0.38);
        if (blushAmt > .05) CatArt.DrawBlush(dc, blushAmt);
        if (mood.Sweat > .01) CatArt.DrawSweat(dc, t, mood.Sweat);
        if (mood.Anger > .01 && lift < .5) CatArt.DrawAnger(dc, t, mood.Anger);
        if (impatient > .05) CatArt.DrawImpatientMark(dc, t, impatient);
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    private static void DrawTail(DrawingContext dc, CompiledSkin skin, bool fluffy, double t, CatMood mood,
                                 double happy, double impatient, double sleep, double crouch, double lift)
    {
        // Cute short pom-pom bobtail works on every body base.
        if (skin.Data.TailStyle == SkinTailStyle.BobtailPom)
        {
            double pomSpeed = 2.0 + mood.Anger * 2.8 + impatient * 3.4 + happy * 0.6;
            double pomAmp = 3.2 + mood.Anger * 2.6 + impatient * 3.0 + happy * 1.1;
            dc.PushTransform(new TranslateTransform(254, 206 + 8 * sleep + 6 * lift));
            dc.PushTransform(new RotateTransform(Math.Sin(t * pomSpeed) * pomAmp));
            dc.DrawGeometry(skin.TailFur, null, BobtailPomShape);
            dc.PushClip(BobtailPomShape);
            dc.PushOpacity(0.52);
            dc.DrawEllipse(skin.AccentBrush, null, new Point(16, -6), 11, 10);
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
            return;
        }

        if (fluffy)
        {
            double tailSpeed = 1.8 + mood.Anger * 2.8 + impatient * 3.4 + crouch * 2.2 - sleep * 1.1 + happy * 0.45;
            double tailAmp = 2.1 + mood.Anger * 2.0 + impatient * 2.6 + crouch * 2.5 + happy * 0.9;
            double thick = skin.Data.TailStyle == SkinTailStyle.ThickPlush ? 1.24 : 1.1;
            dc.PushTransform(new TranslateTransform(187, 184 + 14 * sleep + 8 * lift));
            dc.PushTransform(new RotateTransform(186 - 10 * lift + Math.Sin(t * tailSpeed) * tailAmp));
            dc.PushTransform(new ScaleTransform(thick, 1.22));
            dc.DrawGeometry(skin.TailFur, null, TabbyArt.Tail);
            if (skin.Data.TailStyle == SkinTailStyle.TabbyRings)
            {
                dc.PushClip(TabbyArt.Tail);
                dc.PushOpacity(0.58);
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
            else if (skin.Data.TailStyle == SkinTailStyle.DarkTip)
            {
                dc.PushClip(TabbyArt.Tail);
                dc.DrawEllipse(skin.AccentBrush, null, new Point(66, -48), 20, 22);
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
        double droop = sleep * 24.0 + lift * 16.0;
        StreamGeometry tail = skin.Data.TailStyle == SkinTailStyle.ThickPlush
            ? BuildPlushTailGeometry(wave, tip, droop)
            : CatArt.BuildTailGeometry(wave, tip, droop);
        bool darkWholeTail = skin.Data.FacePattern == SkinFacePattern.SiamesePoint && skin.Data.TailStyle == SkinTailStyle.Solid;
        Brush tBrush = darkWholeTail ? skin.AccentBrush : skin.TailFur;
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
            TailMark(dc, TailTipGeo, skin.BellyBrush, 318, 174 + tip * 5 + droop * .95, -13 + tip * 8);
            dc.Pop();
        }
        else if (skin.Data.TailStyle == SkinTailStyle.DarkTip)
        {
            dc.PushClip(tail);
            TailMark(dc, TailTipGeo, skin.AccentBrush, 316, 175 + tip * 5 + droop * .95, -13 + tip * 8);
            dc.Pop();
        }
    }

    private static StreamGeometry BuildPlushTailGeometry(double wave, double tip, double droop)
    {
        StreamGeometry g = new StreamGeometry();
        using (StreamGeometryContext p = g.Open())
        {
            p.BeginFigure(new Point(260, 213), true, true);
            p.BezierTo(
                new Point(284, 211 + wave * 1.3 + droop * .25),
                new Point(298, 184 + wave * 2.0 + droop * .55),
                new Point(312, 176 + tip * 3.4 + droop * .80), true, false);
            p.BezierTo(
                new Point(326, 169 + tip * 4.8 + droop * .95),
                new Point(335, 178 + tip * 5.6 + droop),
                new Point(331, 190 + tip * 5.2 + droop), true, false);
            p.BezierTo(
                new Point(326, 200 + tip * 4.2 + droop * .92),
                new Point(316, 198 + tip * 3.0 + droop * .80),
                new Point(306, 208 + wave * 2.0 + droop * .58), true, false);
            p.BezierTo(
                new Point(293, 222 + wave * 1.5 + droop * .32),
                new Point(279, 234 + wave * .8 + droop * .14),
                new Point(262, 234), true, false);
        }
        return g;
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
        switch (skin.Data.BodyPattern)
        {
            case SkinBodyPattern.FlankStripes:
                dc.PushOpacity(.72);
                dc.DrawGeometry(skin.AccentBrush, null, FlanksShort);
                dc.Pop();
                break;
            case SkinBodyPattern.ClassicSwirl:
                dc.PushOpacity(.78);
                dc.DrawGeometry(skin.AccentBrush, null, SwirlShort);
                dc.Pop();
                break;
            case SkinBodyPattern.Spotted:
                dc.PushOpacity(.76);
                dc.DrawGeometry(skin.AccentBrush, null, SpottedShort);
                dc.Pop();
                break;
            case SkinBodyPattern.Patches:
                dc.DrawGeometry(skin.AccentBrush, null, PatchesShort);
                break;
            case SkinBodyPattern.BackSaddle:
                dc.PushOpacity(.82);
                dc.DrawGeometry(skin.AccentBrush, null, BackSaddleShort);
                dc.Pop();
                break;
        }

        switch (skin.Data.BellyStyle)
        {
            case SkinBellyStyle.Bib:
                dc.DrawGeometry(skin.BellyBrush, null, BibSmall);
                break;
            case SkinBellyStyle.FullBelly:
            case SkinBellyStyle.Ruff:
                dc.DrawGeometry(skin.BellyBrush, null, FullBellyShort);
                break;
            case SkinBellyStyle.HeartChest:
                dc.DrawGeometry(skin.BellyBrush, null, HeartChestShort);
                break;
            case SkinBellyStyle.TabbyBelly:
                dc.DrawGeometry(skin.BellyBrush, null, FullBellyShort);
                dc.PushOpacity(.46);
                dc.DrawGeometry(skin.AccentBrush, null, TabbyBellySpotsShort);
                dc.Pop();
                break;
        }
        dc.Pop();
    }

    private static void DrawBodyMarkingsFluffy(DrawingContext dc, CompiledSkin skin)
    {
        dc.PushClip(TabbyArt.Body);
        switch (skin.Data.BodyPattern)
        {
            case SkinBodyPattern.FlankStripes:
                dc.PushOpacity(.68);
                dc.DrawGeometry(skin.AccentBrush, null, FlanksFluffy);
                dc.Pop();
                break;
            case SkinBodyPattern.ClassicSwirl:
                dc.PushOpacity(.74);
                dc.DrawGeometry(skin.AccentBrush, null, SwirlFluffy);
                dc.Pop();
                break;
            case SkinBodyPattern.Spotted:
                dc.PushOpacity(.74);
                dc.DrawGeometry(skin.AccentBrush, null, SpottedFluffy);
                dc.Pop();
                break;
            case SkinBodyPattern.Patches:
                dc.DrawGeometry(skin.AccentBrush, null, PatchesFluffy);
                break;
            case SkinBodyPattern.BackSaddle:
                dc.PushOpacity(.78);
                dc.DrawGeometry(skin.AccentBrush, null, BackSaddleFluffy);
                dc.Pop();
                break;
        }

        switch (skin.Data.BellyStyle)
        {
            case SkinBellyStyle.FullBelly:
            case SkinBellyStyle.Bib:
                dc.DrawGeometry(skin.BellyBrush, null, FullBellyFluffy);
                break;
            case SkinBellyStyle.HeartChest:
                dc.DrawGeometry(skin.BellyBrush, null, HeartChestFluffy);
                break;
            case SkinBellyStyle.TabbyBelly:
                dc.DrawGeometry(skin.BellyBrush, null, FullBellyFluffy);
                dc.PushOpacity(.44);
                dc.DrawGeometry(skin.AccentBrush, null, TabbyBellySpotsFluffy);
                dc.Pop();
                break;
        }
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

        Vector dir = paw - c;
        if (dir.LengthSquared < .001) dir = new Vector(0, 1);
        dir.Normalize();

        if (skin.Data.PawStyle == SkinPawStyle.Socks)
        {
            dc.PushClip(limb);
            dc.PushTransform(new MatrixTransform(dir.Y, -dir.X, dir.X, dir.Y, paw.X, paw.Y));
            dc.DrawGeometry(skin.PawBrush, null, FrontSock);
            dc.Pop();
            dc.Pop();
            dc.DrawEllipse(skin.PawBrush, null, paw, pawRadius + .35, pawRadius * (.94 - .06 * activity));
        }
        else if (skin.Data.PawStyle == SkinPawStyle.HighBoots)
        {
            dc.PushClip(limb);
            dc.PushTransform(new MatrixTransform(dir.Y, -dir.X, dir.X, dir.Y, paw.X, paw.Y));
            dc.DrawGeometry(skin.PawBrush, null, HighBootShape);
            dc.Pop();
            dc.Pop();
            dc.DrawEllipse(skin.PawBrush, null, paw, pawRadius + .35, pawRadius * (.94 - .06 * activity));
        }
        else if (skin.Data.PawStyle == SkinPawStyle.TabbyBracelets)
        {
            dc.PushClip(limb);
            dc.PushTransform(new MatrixTransform(dir.Y, -dir.X, dir.X, dir.Y, paw.X, paw.Y));
            dc.DrawGeometry(skin.AccentSoftBrush, null, WristBracelets);
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

    private static Geometry SelectHeadGeometry(SkinBodyBase bodyBase)
    {
        switch (bodyBase)
        {
            case SkinBodyBase.FluffyLong: return TabbyArt.Head;
            case SkinBodyBase.ChubbyBritish: return BritishHead;
            case SkinBodyBase.FoldEar: return FoldHead;
            default: return CatArt.Head;
        }
    }

    private static void DrawHead(DrawingContext dc, CompiledSkin skin, double gx, double gy, double blink,
                                 double anger, double angerBreath, double crouch, double happyPet)
    {
        bool isFold = skin.Data.BodyBase == SkinBodyBase.FoldEar;
        Geometry headGeo = SelectHeadGeometry(skin.Data.BodyBase);
        dc.DrawGeometry(skin.HeadFur, null, headGeo);

        // Pre-ear layer: ear tips & Siamese/Ragdoll ear dark wash.
        dc.PushClip(headGeo);
        if (skin.Data.FacePattern == SkinFacePattern.SiamesePoint || skin.Data.FacePattern == SkinFacePattern.RagdollMask)
        {
            dc.DrawRectangle(skin.SiameseEarWash, null, new Rect(-52, -68, 44, 34));
            dc.DrawRectangle(skin.SiameseEarWash, null, new Rect(10, -68, 42, 34));
        }
        if (skin.Data.DarkEarTips && !isFold)
        {
            dc.PushOpacity(.85);
            dc.DrawGeometry(skin.EarTipBrush, null, EarTipLeft);
            dc.DrawGeometry(skin.EarTipBrush, null, EarTipRight);
            dc.Pop();
        }
        dc.Pop();

        // Inner ears (upright breeds)
        if (!isFold)
        {
            dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarLeft);
            dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarRight);
        }

        // Face pattern markings clipped inside head
        dc.PushClip(headGeo);
        switch (skin.Data.FacePattern)
        {
            case SkinFacePattern.AmericanShorthair:
                dc.PushOpacity(.86);
                dc.DrawGeometry(skin.AccentBrush, null, ForeheadClassicM);
                dc.Pop();
                dc.PushOpacity(.76);
                dc.DrawGeometry(skin.AccentBrush, null, AmShortCheeks);
                dc.Pop();
                dc.PushTransform(new TranslateTransform(gx * 2.5, 0));
                dc.DrawGeometry(skin.BellyBrush, null, MuzzleOval);
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.RagdollMask:
                dc.PushOpacity(.82);
                dc.DrawGeometry(skin.AccentBrush, null, RagdollUpperMask);
                dc.Pop();
                dc.PushTransform(new TranslateTransform(gx * 2.5, 0));
                dc.DrawGeometry(skin.BellyBrush, null, BlueBicolorBlaze);
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.BlueBicolor:
                dc.PushTransform(new TranslateTransform(gx * 2.5, 0));
                dc.DrawGeometry(skin.BellyBrush, null, BlueBicolorBlaze);
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

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
                if (!isFold)
                {
                    dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarLeft);
                    dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarRight);
                }
                dc.PushTransform(new TranslateTransform(gx * 2.5, 0));
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.CowSpots:
                dc.DrawGeometry(skin.BellyBrush, null, headGeo);
                dc.DrawGeometry(skin.AccentBrush, null, CowHeadSpots);
                if (!isFold)
                {
                    dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarLeft);
                    dc.DrawGeometry(skin.InnerEarBrush, null, CatArt.EarRight);
                }
                dc.PushTransform(new TranslateTransform(gx * 2.5, 0));
                DrawNoseAndMouth(dc, skin);
                dc.Pop();
                break;

            case SkinFacePattern.Tortie:
                dc.PushOpacity(.82);
                dc.DrawGeometry(skin.AccentBrush, null, TortiePatches);
                dc.Pop();
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

        // Scottish Fold folded ear flaps sit over the upper temples.
        if (isFold)
        {
            Brush flapBrush = skin.Data.DarkEarTips ? skin.EarTipBrush : skin.HeadFur;
            dc.DrawGeometry(flapBrush, null, FoldEarFlapL);
            dc.DrawGeometry(flapBrush, null, FoldEarFlapR);
            dc.DrawGeometry(null, skin.FoldCreasePen, FoldEarCrease);
        }

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
            {
                dc.PushClip(new EllipseGeometry(new Point(x, y), width + 1.3, height + 1.3));
                CatArt.DrawAngryEyelid(dc, x, y, width, height, side, skin.LidFur, skin.LidPen, anger, angerBreath);
                dc.Pop();
            }
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
