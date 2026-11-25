using System;
using System.Windows;
using System.Windows.Media;

public static partial class CatArt
{
    // Siamese is a seal point: warm fawn body with a cream chest, cream head
    // with a soft dark-brown mask around the eyes and nose (no forehead
    // blaze), dark seal ears and tail, pale blue eyes. The forelegs are
    // light fur ending in dark seal paws. The coat sits on the same opaque
    // rig used by the black cat.
    private static class Siamese
    {
        internal static readonly Brush BodyFur = WashAbsolute(new byte[] { 0xd0, 0xb9, 0x9f }, new byte[] { 0xac, 0x8e, 0x76 }, new Point(175, 102), new Point(255, 239));
        internal static readonly Brush HeadFur = Coat(0xeee3d2, 0xddcdb8);
        internal static readonly Brush TailFur = Coat(0x583f3a, 0x3e2d2a);
        internal static readonly Brush ForelegFur = Wash(new byte[]{218,199,176}, new byte[]{198,173,149}, new Point(.2,0), new Point(.9,1));
        internal static readonly Brush LidFur = Ink(90, 63, 58);
        internal static readonly Brush InnerEar = Coat(0x86605a, 0x76524c);
        internal static readonly Brush Iris = Wash(new byte[]{196,222,240}, new byte[]{122,164,204}, new Point(.2,0), new Point(.83,1));
        internal static readonly Pen Lid = Stroke(Ink(104,142,180), 2.0);
        internal static readonly Pen EyeEdge = Stroke(Ink(98,126,156), .6);
        internal static readonly Pen Whisker = Stroke(Ink(138,118,108), .85);
        internal static readonly Pen WhiskerRoot = Stroke(Ink(190,176,166), .85);
        private static readonly Brush PawFur = Coat(0x5c443d, 0x44322e);
        private static readonly Brush NoseColor = Coat(0x3a2626, 0x2a1b1b);
        private static readonly Pen LegEdge = Stroke(Translucent(190,164,142,135), .48);
        private static readonly Brush EarDark = EarWash();
        private static readonly Brush Chest = ChestWash();
        private static readonly Brush Mask = MaskWash(245, 210);
        private static readonly Brush MaskLower = MaskWash(235, 190);
        private static readonly Geometry Nose = Shape(
            "M -4.2,13.2 Q 0,10.8 4.2,13.2 Q 2.4,18 0,18.6 Q -2.4,18 -4.2,13.2 Z");

        private static Brush Translucent(byte r, byte g, byte b, byte a)
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            brush.Freeze();
            return brush;
        }

        private static Brush Coat(int a, int b)
        {
            return Wash(new byte[]{(byte)(a>>16),(byte)(a>>8),(byte)a},
                        new byte[]{(byte)(b>>16),(byte)(b>>8),(byte)b},
                        new Point(.22,0), new Point(.82,1));
        }

        // Dark seal at the top that dissolves downward, so the ears are dark
        // and the colour fades out before it reaches the forehead.
        private static Brush EarWash()
        {
            Color seal = Color.FromRgb(74,52,47);
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0,0);
            brush.EndPoint = new Point(0,1);
            brush.GradientStops.Add(new GradientStop(seal, 0));
            brush.GradientStops.Add(new GradientStop(seal, .66));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, seal.R, seal.G, seal.B), 1));
            brush.Freeze();
            return brush;
        }

        // Cream chest that fades into the fawn sides.
        private static Brush ChestWash()
        {
            Color cream = Color.FromRgb(240,231,216);
            RadialGradientBrush brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop(cream, 0));
            brush.GradientStops.Add(new GradientStop(cream, .62));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, cream.R, cream.G, cream.B), 1));
            brush.Freeze();
            return brush;
        }

        private static Brush MaskWash(byte centre, byte middle)
        {
            RadialGradientBrush brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(centre, 66,44,41), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(middle, 74,52,47), .55));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 84,60,54), 1));
            brush.Freeze();
            return brush;
        }

        internal static void DrawBodyMarkings(DrawingContext dc, Geometry body)
        {
            dc.PushClip(body);
            dc.DrawEllipse(Chest, null, new Point(214,150), 36, 56);
            dc.Pop();
        }

        // Light foreleg with shoulder-blended coat and edge fading in below the chest, then dark seal paw.
        internal static void DrawArm(DrawingContext dc, Geometry arm, Point shoulder, Point paw, double radius, double activity)
        {
            Brush fill = ArmGradient(Color.FromRgb(216, 197, 174), Color.FromRgb(218, 199, 176), 0.32, Color.FromRgb(198, 173, 149), shoulder, paw);
            dc.DrawGeometry(fill, null, arm);
            dc.PushOpacityMask(EdgeFadeMask(shoulder, paw));
            dc.DrawGeometry(null, LegEdge, arm);
            dc.Pop();
            dc.DrawEllipse(PawFur, null, paw, radius+.35, radius*(.94-.06*activity));
        }

        // Dark ears blended into the cream head.
        internal static void DrawEarFur(DrawingContext dc, Geometry head)
        {
            dc.PushClip(head);
            dc.DrawRectangle(EarDark, null, new Rect(-48,-66,40,30));
            dc.DrawRectangle(EarDark, null, new Rect(14,-66,34,30));
            dc.Pop();
        }

        internal static void DrawFaceMarkings(DrawingContext dc, Geometry head, double gx)
        {
            dc.PushClip(head);
            dc.PushTransform(new TranslateTransform(gx*2.9, 0));
            dc.DrawEllipse(Mask, null, new Point(0,-5), 33, 27);
            dc.DrawEllipse(MaskLower, null, new Point(0,9), 14, 17);
            dc.DrawGeometry(NoseColor, null, Nose);
            dc.Pop();
            dc.Pop();
        }
    }
}
