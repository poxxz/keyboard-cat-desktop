using System;
using System.Windows;
using System.Windows.Media;

public static partial class CatArt
{
    // Ginger coat markings sit on top of the same opaque rig used by the black cat.
    private static class Ginger
    {
        internal static readonly Brush BodyFur = WashAbsolute(new byte[] { 0xf1, 0xaa, 0x63 }, new byte[] { 0xda, 0x88, 0x4a }, new Point(175, 102), new Point(255, 239));
        internal static readonly Brush HeadFur = Coat(0xffc179,0xe89a55);
        internal static readonly Brush TailFur = Coat(0xeaa15c,0xd58b4f);
        internal static readonly Brush LegFur = BodyFur;
        internal static readonly Brush LidFur = Ink(246, 176, 105);
        internal static readonly Brush InnerEar = Coat(0xe88c84,0xf7b398);
        internal static readonly Brush Iris = Wash(new byte[]{135,143,101},new byte[]{204,179,113},new Point(.45,0),new Point(.55,1));
        internal static readonly Pen Lid = Stroke(Ink(92,62,40),2.0);
        internal static readonly Pen EyeEdge = Stroke(Ink(115,81,50),.85);
        internal static readonly Pen Whisker = Stroke(Ink(125,88,64),.82);
        internal static readonly Pen WhiskerRoot = Stroke(Ink(147,104,75),.85);
        private static readonly Brush Marking = Ink(193,112,57);
        private static readonly Brush SoftMarking = Ink(208,128,68);
        private static readonly Brush Cream = Ink(255,230,192);
        private static readonly Brush BibFur = Coat(0xffe7c3,0xf5ce9d);
        private static readonly Brush NoseColor = Coat(0xdd8380,0xbf6e6b);
        private static readonly Brush DarkEye = Ink(60,47,37);
        private static readonly Brush Catchlight = Ink(255,250,229);
        private static readonly Brush Freckle = Ink(187,141,103);
        private static readonly Pen Lip = Stroke(Ink(149,95,67),1.25);
        private static readonly Geometry Chin = Shape(
            "M -26,10 C -24,3 -17,-1 -9,.6 C -5,.9 -2,3 0,4 C 5,1 11,-1 18,1 " +
            "C 26,3 29,10 25,17 C 21,24 12,29 1,30 C -10,31 -21,28 -25,21 Q -29,16 -26,10 Z");
        private static readonly Geometry Forehead = Shape(
            "M -18,-29 C -17,-35 -15,-42 -13,-48 L -7,-48 Q -4,-42 0,-39 " +
            "Q 4,-42 7,-48 L 13,-48 Q 17,-40 16,-30 Q 12,-33 9,-41 " +
            "Q 4,-36 .2,-32 Q -5,-35 -10,-41 Q -13,-33 -18,-29 Z");
        private static readonly Geometry LeftCheek = Shape(
            "M -52,-8 C -46,-9 -40,-8 -35,-4 Q -42,-3 -51,-3 Z " +
            "M -51,1 Q -44,-1 -38,2 Q -43,4 -50,7 Z");
        private static readonly Geometry RightCheek = Shape(
            "M 51,-8 Q 42,-8 35,-4 Q 43,-3 51,-3 Z M 51,1 Q 44,0 38,3 Q 44,5 50,7 Z");
        private static readonly Geometry Nose = Shape(
            "M -4.8,5 Q -3.7,2.9 0,3.9 Q 3.8,2.8 5.0,4.9 Q 5.2,6.4 2.2,8.8 Q .3,10.4 -.8,9.7 Q -5.3,6.9 -4.8,5 Z");
        private static readonly Geometry Smile = Shape(
            "M 0,9 L 0,12.3 C -1.5,15.8 -5.5,16 -7.8,13 M 0,12.3 C 1.9,15.4 5.6,15.7 8,12.9");
        private static readonly Geometry Bib = Shape(
            "M 182,102 C 189,112 205,110 214,111 C 225,110 240,111 246,101 " +
            "Q 248,124 236,145 L 235,138 Q 230,153 220,163 L 219,156 " +
            "Q 216,162 210,167 Q 205,158 199,153 L 198,157 Q 183,141 181,124 Z");
        private static readonly Geometry Flanks = Shape(
            "M 155,168 C 167,166 174,170 181,176 Q 169,173 157,176 Z " +
            "M 156,180 Q 171,178 179,186 Q 169,183 156,188 Z " +
            "M 272,164 Q 257,164 249,173 Q 261,169 272,173 Z " +
            "M 271,178 Q 258,177 250,186 Q 262,182 272,185 Z");
        private static readonly Geometry FrontSock = Shape(
            "M -9.2,1 C -10,-4 -9.9,-9 -7.3,-10.7 Q -5,-12 -2,-10.8 Q 1,-9.3 3.2,-10.7 " +
            "Q 6.1,-12.5 8.3,-10.4 Q 10.3,-7 9.2,1 Q 7.2,8 0,9 Q -7.7,7.9 -9.2,1 Z");
        private static readonly Geometry LegStripe = Shape(
            "M -15,-27 C -7,-28 -3,-25 .5,-23.5 Q -6,-24 -15,-21 Z");
        private static readonly Geometry TailBand = Shape(
            "M -3,-13 Q .7,-11 4,-13 Q 1,0 4,13 Q -.2,11 -3,13 Q -4.4,0 -3,-13 Z");
        private static readonly Geometry TailTip = Shape(
            "M -3,-12 L 22,-12 L 22,13 L -4,13 Q .4,4 -1,-1 Q -4,-6 -3,-12 Z");

        private static Brush Coat(int a,int b)
        {
            return Wash(new byte[]{(byte)(a>>16),(byte)(a>>8),(byte)a},
                        new byte[]{(byte)(b>>16),(byte)(b>>8),(byte)b},
                        new Point(.22,0),new Point(.82,1));
        }

        internal static void DrawTailBands(DrawingContext dc, Geometry tail, double wave, double tip)
        {
            dc.PushClip(tail);
            TailMark(dc,TailBand,Marking,275,208+wave*.7,-7);
            TailMark(dc,TailBand,Marking,289.5,203+wave*1.7,-43+wave*3);
            TailMark(dc,TailBand,Marking,304,183+wave*2.5,-48+tip*4);
            TailMark(dc,TailTip,Cream,320,174+tip*5,-13+tip*8);
            dc.Pop();
        }

        private static void TailMark(DrawingContext dc,Geometry shape,Brush color,double x,double y,double angle)
        {
            dc.PushTransform(new TranslateTransform(x,y));
            dc.PushTransform(new RotateTransform(angle));
            dc.DrawGeometry(color,null,shape);
            dc.Pop();
            dc.Pop();
        }

        internal static void DrawBodyMarkings(DrawingContext dc,Geometry body)
        {
            dc.PushClip(body);
            dc.DrawGeometry(SoftMarking,null,Flanks);
            dc.DrawGeometry(BibFur,null,Bib);
            dc.Pop();
        }

        internal static void DrawSock(DrawingContext dc,Geometry arm,Point paw,Vector direction,double radius,double activity)
        {
            if (direction.LengthSquared < .001) direction = new Vector(0,1);
            direction.Normalize();
            // Wrist stripes turn with the final segment of the foreleg as the paw reaches.
            dc.PushClip(arm);
            dc.PushTransform(new MatrixTransform(direction.Y,-direction.X,direction.X,direction.Y,paw.X,paw.Y));
            dc.DrawGeometry(SoftMarking,null,LegStripe);
            dc.DrawGeometry(Cream,null,FrontSock);
            dc.Pop();
            dc.Pop();
            dc.DrawEllipse(Cream,null,paw,radius+.35,radius*(.94-.06*activity));
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

        internal static void DrawFaceMarkings(DrawingContext dc,Geometry head,double gx)
        {
            dc.PushClip(head);
            dc.DrawGeometry(Marking,null,Forehead);
            dc.DrawGeometry(SoftMarking,null,LeftCheek);
            dc.DrawGeometry(SoftMarking,null,RightCheek);
            dc.PushTransform(new TranslateTransform(gx*2.9,0));
            dc.DrawGeometry(Cream,null,Chin);
            dc.DrawGeometry(null,Lip,Smile);
            dc.DrawGeometry(NoseColor,null,Nose);
            for (int side=-1;side<=1;side+=2)
            {
                dc.DrawEllipse(Freckle,null,new Point(side*12,9.8),1.05,.83);
                dc.DrawEllipse(Freckle,null,new Point(side*16.6,10.4),.86,.72);
                dc.DrawEllipse(Freckle,null,new Point(side*13.8,13.3),.75,.62);
            }
            dc.Pop();
            dc.Pop();
        }
    }
}

