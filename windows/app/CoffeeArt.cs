using System;
using System.Windows;
using System.Windows.Media;

public static partial class CatArt
{
    // Coffee is a Jianzhou tabby-and-white with a signature open-face tabby mask:
    // clean caramel-brown mask around the forehead, eyes and round cheeks,
    // pure white lower face with a narrow inverted-V white nose blaze merging into
    // the white chest/legs, minimal soft forehead & cheek stripes (Version A),
    // warm apricot inner ears with dark ear tips, bright golden-amber eyes with
    // sleek Wednesday-style oval pupils, pink nose, and crisp white whiskers.
    private static class Coffee
    {
        internal static readonly Brush BodyFur = Coat(0xa87b50, 0x7c5836);
        internal static readonly Brush HeadFur = Wash(new byte[] { 0xcf, 0x9b, 0x61 }, new byte[] { 0x94, 0x68, 0x3d }, new Point(.25, .05), new Point(.85, .92));
        internal static readonly Brush TailFur = Coat(0xa07449, 0x765232);
        internal static readonly Brush ForelegFur = WashAbsolute(new byte[] { 0xff, 0xfc, 0xf8 }, new byte[] { 0xf2, 0xeb, 0xe0 }, new Point(175, 102), new Point(255, 239));
        internal static readonly Brush LidFur = Ink(172, 124, 76);
        internal static readonly Brush InnerEar = Wash(new byte[] { 0xe3, 0xb9, 0x92 }, new byte[] { 0xf7, 0xe4, 0xcd }, new Point(.2, 0), new Point(.8, 1));
        internal static readonly Brush Iris = Wash(new byte[] { 0xf7, 0xc3, 0x38 }, new byte[] { 0xc4, 0x75, 0x0e }, new Point(.42, .05), new Point(.58, .98));
        internal static readonly Pen Lid = Stroke(Ink(78, 52, 34), 2.1);
        internal static readonly Pen EyeEdge = Stroke(Ink(58, 39, 24), 1.25);
        internal static readonly Pen Whisker = Stroke(Ink(253, 250, 246), 1.0);
        internal static readonly Pen WhiskerRoot = Stroke(Ink(225, 217, 206), .95);
        private static readonly Brush CrownShade = Wash(new byte[] { 0x5a, 0x3e, 0x27 }, new byte[] { 0x94, 0x68, 0x3d }, new Point(.5, 0), new Point(.5, 1));
        private static readonly Brush CheekDepth = Ink(112, 74, 42);
        private static readonly Brush EarTipColor = Ink(54, 38, 26);
        private static readonly Pen EarTuftPen = Stroke(Ink(249, 240, 226), .9);
        private static readonly Brush EyeGlow = Ink(238, 202, 145);
        private static readonly Brush IrisHighlight = Ink(255, 224, 110);
        private static readonly Brush Marking = Ink(98, 66, 39);
        private static readonly Brush SoftMarking = Ink(106, 72, 42);
        private static readonly Brush TailMarking = Ink(88, 60, 36);
        private static readonly Brush FlankMarking = Ink(94, 64, 38);
        private static readonly Brush MaskFeather = Ink(225, 195, 156);
        private static readonly Brush WhiteFace = Wash(new byte[] { 0xff, 0xfe, 0xfb }, new byte[] { 0xf5, 0xef, 0xe7 }, new Point(.5, 0), new Point(.5, 1));
        private static readonly Brush JawShadow = Ink(230, 221, 208);
        private static readonly Brush Cream = Ink(255, 252, 248);
        private static readonly Brush BibFur = ForelegFur;
        private static readonly Brush NoseColor = Wash(new byte[] { 0xf0, 0xa2, 0xa4 }, new byte[] { 0xcf, 0x78, 0x7d }, new Point(.3, 0), new Point(.7, 1));
        private static readonly Brush NoseHighlight = Ink(255, 228, 228);
        private static readonly Brush DarkEye = Ink(30, 23, 18);
        private static readonly Brush Catchlight = Ink(255, 252, 238);
        private static readonly Pen Lip = Stroke(Ink(142, 98, 84), 1.2);

        private static readonly Geometry EarTipLeft = Shape("M -44,-49 Q -41,-59 -36,-63 Q -32,-63 -28,-57 Q -35,-54 -41,-49 Z");
        private static readonly Geometry EarTipRight = Shape("M 28,-57 Q 33,-64 37,-63 Q 42,-58 44,-48 Q 39,-53 28,-57 Z");
        private static readonly Geometry EarTufts = Shape(
            "M -36,-39 Q -32,-44 -27,-42 M -38,-35 Q -33,-39 -29,-38 " +
            "M 36,-39 Q 32,-44 27,-42 M 38,-35 Q 33,-39 29,-38");

        // Version A: clean, restrained 3-stroke forehead tabby accent.
        private static readonly Geometry Forehead = Shape(
            "M -2.2,-46.5 Q 0,-47.5 2.2,-46.5 Q 1.5,-39.5 0,-34.5 Q -1.5,-39.5 -2.2,-46.5 Z " +
            "M -11.5,-45.5 Q -9.0,-46.5 -6.8,-44.8 Q -7.2,-38.5 -10.5,-33.8 Q -11.8,-39.5 -11.5,-45.5 Z " +
            "M 11.5,-45.5 Q 9.0,-46.5 6.8,-44.8 Q 7.2,-38.5 10.5,-33.8 Q 11.8,-39.5 11.5,-45.5 Z");

        // Version A: subtle 2 short cheek strokes per side.
        private static readonly Geometry Cheeks = Shape(
            "M -47.5,-9.5 Q -41.5,-10.2 -35.5,-8.0 Q -41.5,-6.6 -47.8,-6.2 Z " +
            "M -46.8,0.5 Q -40.8,-0.2 -34.8,2.2 Q -40.8,3.6 -46.5,3.8 Z " +
            "M 47.5,-9.5 Q 41.5,-10.2 35.5,-8.0 Q 41.5,-6.6 47.8,-6.2 Z " +
            "M 46.8,0.5 Q 40.8,-0.2 34.8,2.2 Q 40.8,3.6 46.5,3.8 Z");

        // Soft feathered rim & pure-white lower face with narrow inverted-V nose blaze,
        // carving out Coffee's iconic round open-face tabby mask above.
        private static readonly Geometry WhiteFaceRim = Shape(
            "M 0,-18.8 " +
            "C 2.0,-11.5 4.8,-3.5 9.2,2.6 " +
            "C 13.5,8.2 19.5,12.4 26.8,14.5 " +
            "Q 33.8,16.2 40.5,14.6 Q 46.2,13.2 51.5,9.5 " +
            "L 52.5,38.0 L -52.5,38.0 L -51.5,9.5 " +
            "Q -46.2,13.2 -40.5,14.6 Q -33.8,16.2 -26.8,14.5 " +
            "C -19.5,12.4 -13.5,8.2 -9.2,2.6 " +
            "C -4.8,-3.5 -2.0,-11.5 0,-18.8 Z");

        private static readonly Geometry WhiteLowerFace = Shape(
            "M 0,-17.0 " +
            "C 1.6,-10.2 4.0,-2.6 8.2,3.4 " +
            "C 12.4,9.2 18.2,13.8 25.5,16.0 " +
            "Q 29.8,17.3 34.4,17.2 Q 39.2,17.0 43.8,15.2 Q 48.0,13.4 51.5,10.8 " +
            "L 52.5,38.0 L -52.5,38.0 L -51.5,10.8 " +
            "Q -48.0,13.4 -43.8,15.2 Q -39.2,17.0 -34.4,17.2 Q -29.8,17.3 -25.5,16.0 " +
            "C -18.2,13.8 -12.4,9.2 -8.2,3.4 " +
            "C -4.0,-2.6 -1.6,-10.2 0,-17.0 Z");

        private static readonly Geometry Nose = Shape(
            "M -4.9,5.0 Q -3.7,2.9 0,3.9 Q 3.8,2.8 5.0,4.9 Q 5.2,6.5 2.2,8.9 Q .3,10.5 -.8,9.8 Q -5.3,6.9 -4.9,5.0 Z");
        private static readonly Geometry Smile = Shape(
            "M 0,9.2 L 0,12.4 C -1.5,15.8 -5.4,16.0 -7.6,13.1 M 0,12.4 C 1.8,15.4 5.5,15.7 7.8,13.0");

        // White chest and belly with soft fur-scalloped edges.
        private static readonly Geometry Bib = Shape(
            "M 182,102 C 189,112 205,110 214,111 C 225,110 240,111 244,102 " +
            "Q 250.8,108 245.6,114 Q 238.5,120.8 247.4,127.5 " +
            "Q 257.1,133.2 248.9,139 Q 241.5,146 250.1,153 " +
            "Q 259.6,159.2 251.2,165.5 Q 243.7,172 252.6,178.5 " +
            "Q 262.5,184.5 254.5,190.5 Q 247.6,197.2 256.5,204 " +
            "Q 264.1,209.8 257.8,215.5 Q 252.8,220.8 259,226 " +
            "C 260,233 259,238 254,239 L 178,239 C 172,238 167,233 166,226 " +
            "Q 172,220.8 166.8,215.5 Q 160.2,209.8 167.7,204 " +
            "Q 176.2,197.2 168.8,190.5 Q 160.2,184.5 169.7,178.5 " +
            "Q 178.4,172 171.4,165.5 Q 163.2,159.2 173.1,153 " +
            "Q 182.1,146 175.2,139 Q 167.2,133.2 177.3,127.5 " +
            "Q 186.5,120.8 179.8,114 Q 174.9,108 182,102 Z");
        private static readonly Geometry Flanks = Shape(
            "M 155,168 C 167,166 174,170 181,176 Q 169,173 157,176 Z " +
            "M 156,180 Q 171,178 179,186 Q 169,183 156,188 Z " +
            "M 272,164 Q 257,164 249,173 Q 261,169 272,173 Z " +
            "M 271,178 Q 258,177 250,186 Q 262,182 272,185 Z");
        private static readonly Geometry FrontSock = Shape(
            "M -9.2,1 C -10,-4 -9.9,-9 -7.3,-10.7 Q -5,-12 -2,-10.8 Q 1,-9.3 3.2,-10.7 " +
            "Q 6.1,-12.5 8.3,-10.4 Q 10.3,-7 9.2,1 Q 7.2,8 0,9 Q -7.7,7.9 -9.2,1 Z");
        private static readonly Geometry TailBand = Shape(
            "M -3,-13 Q .7,-11 4,-13 Q 1,0 4,13 Q -.2,11 -3,13 Q -4.4,0 -3,-13 Z");

        private static Brush Coat(int a, int b)
        {
            return Wash(new byte[] { (byte)(a >> 16), (byte)(a >> 8), (byte)a },
                        new byte[] { (byte)(b >> 16), (byte)(b >> 8), (byte)b },
                        new Point(.22, 0), new Point(.82, 1));
        }

        internal static void DrawTailBands(DrawingContext dc, Geometry tail, double wave, double tip)
        {
            dc.PushClip(tail);
            TailMark(dc, TailBand, TailMarking, 275, 208 + wave * .7, -7);
            TailMark(dc, TailBand, TailMarking, 289.5, 203 + wave * 1.7, -43 + wave * 3);
            TailMark(dc, TailBand, TailMarking, 304, 183 + wave * 2.5, -48 + tip * 4);
            TailMark(dc, TailBand, TailMarking, 320, 174 + tip * 5, -13 + tip * 8);
            dc.Pop();
        }

        private static void TailMark(DrawingContext dc, Geometry shape, Brush color, double x, double y, double angle)
        {
            dc.PushTransform(new TranslateTransform(x, y));
            dc.PushTransform(new RotateTransform(angle));
            dc.DrawGeometry(color, null, shape);
            dc.Pop();
            dc.Pop();
        }

        internal static void DrawBodyMarkings(DrawingContext dc, Geometry body)
        {
            dc.PushClip(body);
            dc.PushOpacity(.68);
            dc.DrawGeometry(FlankMarking, null, Flanks);
            dc.Pop();
            dc.DrawGeometry(BibFur, null, Bib);
            dc.Pop();
        }

        internal static void DrawSock(DrawingContext dc, Geometry arm, Point paw, Vector direction, double radius, double activity)
        {
            if (direction.LengthSquared < .001) direction = new Vector(0, 1);
            direction.Normalize();
            dc.PushClip(arm);
            dc.PushTransform(new MatrixTransform(direction.Y, -direction.X, direction.X, direction.Y, paw.X, paw.Y));
            dc.DrawGeometry(Cream, null, FrontSock);
            dc.Pop();
            dc.Pop();
            dc.DrawEllipse(Cream, null, paw, radius + .35, radius * (.94 - .06 * activity));
        }

        // Sleek smaller vertical-oval pupil inspired by Wednesday, with lively gaze travel and delicate catchlight.
        internal static void DrawPupil(DrawingContext dc, double x, double y, double gx, double gy, double crouch)
        {
            dc.PushOpacity(.24);
            dc.DrawEllipse(IrisHighlight, null, new Point(x, y + 2.5), 9.2, 8.0);
            dc.Pop();
            double boost = 1.0 + 0.16 * crouch;
            Point pupil = new Point(x + gx * 7.4, y + gy * 6.2);
            double rx = (4.4 + Math.Abs(gy) * 0.75) * boost;
            double ry = 8.2 * boost;
            dc.DrawEllipse(DarkEye, null, pupil, rx, ry);
            dc.DrawEllipse(Catchlight, null, new Point(pupil.X - 1.5, pupil.Y - 2.9), 1.55, 1.65);
        }

        // Drawn before EarLeft/EarRight so crown shading and dark ear tips sit cleanly behind the inner ears.
        internal static void DrawEarFur(DrawingContext dc, Geometry head)
        {
            dc.PushClip(head);
            dc.PushOpacity(.22);
            dc.DrawEllipse(CrownShade, null, new Point(0, -45), 32, 13);
            dc.Pop();
            dc.PushOpacity(.16);
            dc.DrawEllipse(CheekDepth, null, new Point(-45, -4), 12, 18);
            dc.DrawEllipse(CheekDepth, null, new Point(45, -4), 12, 18);
            dc.Pop();
            dc.PushOpacity(.88);
            dc.DrawGeometry(EarTipColor, null, EarTipLeft);
            dc.DrawGeometry(EarTipColor, null, EarTipRight);
            dc.Pop();
            dc.Pop();
        }

        internal static void DrawFaceMarkings(DrawingContext dc, Geometry head, double gx)
        {
            dc.PushClip(head);
            dc.PushOpacity(.75);
            dc.DrawGeometry(null, EarTuftPen, EarTufts);
            dc.Pop();
            dc.PushOpacity(.34);
            dc.DrawEllipse(EyeGlow, null, new Point(-18.5, -15.5), 16.0, 14.2);
            dc.DrawEllipse(EyeGlow, null, new Point(18.5, -15.5), 16.0, 14.2);
            dc.Pop();
            dc.PushOpacity(.68);
            dc.DrawGeometry(Marking, null, Forehead);
            dc.Pop();
            dc.PushOpacity(.52);
            dc.DrawGeometry(SoftMarking, null, Cheeks);
            dc.Pop();

            dc.PushTransform(new TranslateTransform(gx * 2.4, 0));
            dc.PushOpacity(.45);
            dc.DrawGeometry(MaskFeather, null, WhiteFaceRim);
            dc.Pop();
            dc.DrawGeometry(WhiteFace, null, WhiteLowerFace);
            dc.PushOpacity(.42);
            dc.DrawEllipse(JawShadow, null, new Point(0, 32), 30, 6.5);
            dc.Pop();
            dc.DrawGeometry(null, Lip, Smile);
            dc.DrawGeometry(NoseColor, null, Nose);
            dc.PushOpacity(.55);
            dc.DrawEllipse(NoseHighlight, null, new Point(-1.4, 4.6), 1.5, .8);
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
    }
}
