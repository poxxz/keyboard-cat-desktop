using System;
using System.Windows;
using System.Windows.Media;

public static partial class CatArt
{
    // SilverShaded is a classic British Shorthair silver shaded: dense
    // silver-grey coat with darker tipping, round chubby face, pale
    // silver-white chest and belly, big round green eyes, brick-pink nose.
    // Its tabby markings use the same M / cheek / flank shapes as Coffee,
    // drawn softer so they read as shading, not stripes. The coat sits on
    // the same opaque rig used by the black cat.
    private static class SilverShaded
    {
        internal static readonly Brush BodyFur = Coat(0xe4e7e9, 0xccd1d4);
        internal static readonly Brush HeadFur = Coat(0xeceef0, 0xd8dce0);
        internal static readonly Brush TailFur = Coat(0xdde0e3, 0xc4c9ce);
        internal static readonly Brush ForelegFur = Coat(0xf4f5f6, 0xe8eaec);
        internal static readonly Brush LidFur = Ink(228, 231, 234);
        internal static readonly Brush InnerEar = Coat(0xe8b8b2, 0xdaa49e);
        internal static readonly Brush Iris = Wash(new byte[]{126,200,122}, new byte[]{62,158,88}, new Point(.45,0), new Point(.55,1));
        internal static readonly Pen Lid = Stroke(Ink(58,108,68), 2.0);
        internal static readonly Pen EyeEdge = Stroke(Ink(72,92,76), .85);
        internal static readonly Pen Whisker = Stroke(Ink(140,145,150), .82);
        internal static readonly Pen WhiskerRoot = Stroke(Ink(165,170,175), .85);
        private static readonly Brush Marking = Ink(150,155,160);
        private static readonly Brush SoftMarking = Ink(172,177,182);
        private static readonly Brush Cream = Ink(250,251,252);
        private static readonly Brush BibFur = WashAbsolute(new byte[] { 0xf5, 0xf6, 0xf8 }, new byte[] { 0xe6, 0xea, 0xed }, new Point(175, 102), new Point(255, 239));

        internal static Brush ArmBrush(Point shoulder, Point paw)
        {
            return ArmGradient(Color.FromRgb(236, 239, 241), Color.FromRgb(242, 244, 246), 0.28, Color.FromRgb(246, 247, 248), shoulder, paw);
        }
        private static readonly Brush NoseColor = Coat(0xe29a92, 0xcc7d74);
        private static readonly Brush DarkEye = Ink(38,42,40);
        private static readonly Brush Catchlight = Ink(255,255,255);
        private static readonly Pen Lip = Stroke(Ink(140,145,150), 1.25);
        // No forehead M and no cheek stripes: a silver shaded face is
        // clean, almost white, with only the faintest grey tipping on the
        // back and tail. Muzzle, nose and smile share Coffee's shapes.
        private static readonly Geometry Muzzle = Shape(
            "M -26,10 C -24,3 -17,-1 -9,.6 C -5,.9 -2,3 0,4 C 5,1 11,-1 18,1 " +
            "C 26,3 29,10 25,17 C 21,24 12,29 1,30 C -10,31 -21,28 -25,21 Q -29,16 -26,10 Z");
        private static readonly Geometry Nose = Shape(
            "M -4.8,5 Q -3.7,2.9 0,3.9 Q 3.8,2.8 5.0,4.9 Q 5.2,6.4 2.2,8.8 Q .3,10.4 -.8,9.7 Q -5.3,6.9 -4.8,5 Z");
        private static readonly Geometry Smile = Shape(
            "M 0,9 L 0,12.3 C -1.5,15.8 -5.5,16 -7.8,13 M 0,12.3 C 1.9,15.4 5.6,15.7 8,12.9");
        // Pale chest and belly with smooth edges: the silver coat lightens
        // gradually, so there is no hard cut. Clipped to the body.
        private static readonly Geometry Bib = Shape(
            "M 182,102 C 190,108 200,106 214,107 C 228,106 238,108 244,102 " +
            "C 250,112 252,130 250,150 C 248,175 246,205 252,226 " +
            "C 254,234 252,239 246,239 L 180,239 C 174,239 172,234 174,226 " +
            "C 180,205 178,175 176,150 C 174,130 176,112 182,102 Z");
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
            return Wash(new byte[]{(byte)(a>>16),(byte)(a>>8),(byte)a},
                        new byte[]{(byte)(b>>16),(byte)(b>>8),(byte)b},
                        new Point(.22,0), new Point(.82,1));
        }

        internal static void DrawTailBands(DrawingContext dc, Geometry tail, double wave, double tip)
        {
            dc.PushClip(tail);
            TailMark(dc, TailBand, Marking, 275, 208+wave*.7, -7);
            TailMark(dc, TailBand, Marking, 289.5, 203+wave*1.7, -43+wave*3);
            TailMark(dc, TailBand, Marking, 304, 183+wave*2.5, -48+tip*4);
            TailMark(dc, TailBand, Marking, 320, 174+tip*5, -13+tip*8);
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
            dc.DrawGeometry(SoftMarking, null, Flanks);
            dc.DrawGeometry(BibFur, null, Bib);
            dc.Pop();
        }

        internal static void DrawSock(DrawingContext dc, Geometry arm, Point paw, Vector direction, double radius, double activity)
        {
            if (direction.LengthSquared < .001) direction = new Vector(0,1);
            direction.Normalize();
            dc.PushClip(arm);
            dc.PushTransform(new MatrixTransform(direction.Y, -direction.X, direction.X, direction.Y, paw.X, paw.Y));
            dc.DrawGeometry(Cream, null, FrontSock);
            dc.Pop();
            dc.Pop();
            dc.DrawEllipse(Cream, null, paw, radius+.35, radius*(.94-.06*activity));
        }

        internal static void DrawPupil(DrawingContext dc, double x, double y, double gx, double gy, double crouch)
        {
            double boost = 1.0 + 0.16 * crouch;
            Point pupil = new Point(x + gx * 7.4, y + gy * 6.2);
            double rx = (4.4 + Math.Abs(gy) * 0.75) * boost;
            double ry = 8.2 * boost;
            dc.DrawEllipse(DarkEye, null, pupil, rx, ry);
            dc.DrawEllipse(Catchlight, null, new Point(pupil.X - 1.5, pupil.Y - 2.9), 1.55, 1.65);
        }

        internal static void DrawFaceMarkings(DrawingContext dc, Geometry head, double gx)
        {
            dc.PushClip(head);
            dc.PushTransform(new TranslateTransform(gx*2.9, 0));
            dc.DrawGeometry(Cream, null, Muzzle);
            dc.DrawGeometry(null, Lip, Smile);
            dc.DrawGeometry(NoseColor, null, Nose);
            dc.Pop();
            dc.Pop();
        }
    }
}
