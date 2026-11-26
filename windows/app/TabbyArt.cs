using System;
using System.Windows;
using System.Windows.Media;

// Photo-matched longhaired tabby. All static markings are assembled and frozen once.
internal static class TabbyArt
{
    private const double BodyWidth = 1.07;
    private const double BodyHeight = 1.025;
    private const double BodyTop = 237 - 119 * BodyHeight;
    private static readonly Geometry Head = Shape("M -41,-24 C -44,-31 -46,-46 -44,-57 Q -43,-64 -39,-64 C -32,-64 -23,-51 -18,-43 " +
                 "C -6,-46 7,-46 19,-43 C 26,-50 33,-61 40,-65 Q 45,-69 45,-61 Q 45,-45 40,-29 " +
                 "C 45,-23 48,-16 49,-10 Q 53,-3 54,1 C 52,2 51,1 50,.5 Q 54,7 56,12 Q 55,14 52,13 " +
                 "Q 50,17 51,19 Q 49,21 44,20 C 39,24 34,25 28,27 Q 23,32 11,32 Q 3,36 -5,35 " +
                 "Q -16,34 -23,30 Q -34,29 -41,25 Q -46,25 -50,24 Q -48,21 -48,20 Q -54,19 -56,18 " +
                 "Q -53,15 -52,13 Q -57,14 -58,12 C -56,8 -54,6 -53,2 Q -56,3 -57,2 Q -53,-7 -49,-13 Q -47,-19 -41,-24 Z");
    private static readonly Geometry Body = Shape("M -29,-5 C -35,8 -41,21 -46,34 Q -49,39 -50,46 L -48,45 Q -54,59 -55,72 " +
                 "C -56,91 -53,105 -48,113 Q -49,120 -42,122 Q -35,123 -30,117 C -11,121 13,122 32,117 " +
                 "Q 37,123 45,120 Q 50,118 49,112 C 55,103 56,92 55,78 Q 54,62 49,48 L 51,50 Q 48,36 44,29 " +
                 "Q 38,12 29,-5 Z");
    private static readonly Geometry Tail = Shape("M -8,-3 C 8,9 27,2 33,-9 C 38,-18 34,-27 40,-38 L 42,-35 Q 47,-53 57,-59 L 56,-54 Q 68,-64 74,-58 Q 81,-52 76,-43 L 79,-44 C 82,-24 67,-4 43,6 C 23,15 6,14 -8,8 Z");
    private static readonly Geometry Eye = Shape("M -12,1.2 C -11,-4.8 -6.9,-9 0,-9 Q 8,-9.1 12,-3 C 12,3.1 8,8.5 1,9 Q -8,9.4 -12,1.2 Z");
    private static readonly Geometry ClosedEye = Shape("M -11,0 Q -1,6 12,-1");
    private static readonly Geometry Whiskers = Shape("M 20,12.2 Q 42,8.5 58,13 M 21,15 Q 43,14.8 61,23.5 M 22,18 Q 41,19 54.5,28");
    private static readonly Geometry EarTuft = Shape("M -32,-37 Q -40,-23 -53,-27 M -36,-34 Q -44,-24.2 -55,-25.8");
    private static readonly Geometry FrontSock = Shape("M -8.5,-1 C -8.9,-4.2 -6.6,-7.3 -3.6,-7.5 Q -.8,-9.9 2,-8 Q 5,-8.8 7.1,-6.6 C 9.6,-4.5 10.2,1.6 8.3,4.6 Q 6,8.3 .6,7.1 Q -4.3,8.7 -7.3,5.4 Q -9.4,3.3 -8.5,-1 Z");
    private static readonly Geometry Ruff = Shape("M -42,8 Q -18,-3 0,2 Q 22,-3 41,7 C 43,15 40,23 38,26 " +
            "Q 39,28 41,28 Q 37,33 28,35 Q 25,41 18,44 Q 20,42 20,40 " +
            "Q 13,45 8,49 Q 5,51 1,52 Q 1,50 0,49 Q -6,54 -11,51 Q -8,51 -8,48 " +
            "Q -18,47 -24,42 Q -22,42 -20,40 Q -29,40 -35,35 Q -32,35 -31,33 " +
            "C -37,33 -42,30 -44,27 Q -40,27 -39,24 Q -44,19 -42,8 Z");
    private static readonly Brush RuffFur = Shade("#F4EDDC","#DBCFB7");
    private static readonly Pen EarFeather = Stroke("#ECDFC6",.85);
    private static readonly Pen WhiskerPen = Stroke("#E9D9BD",.79);
    private static readonly Pen EyeEdge = Stroke("#433631",1);
    private static readonly Pen Lid = Stroke("#433631",2.2);
    private static readonly Brush Iris = Shade("#929258","#A9AD63");
    private static readonly Brush Pupil = Ink("#302C2B");
    private static readonly Brush Glint = Ink("#FFF9E9");
    private static readonly Brush Sleeve = Shade("#9E876A","#B69D7A");
    private static readonly Brush Sock = Shade("#F5EFDD","#ECE3D1");
    private static readonly Pen Toe = Stroke("#BFB6A6",.65);
    private static readonly Brush UpperStripe = Ink("#715D47");
    private static readonly Brush MiddleStripe = Ink("#6B5642");
    private static readonly Brush LowerStripe = Ink("#78654E");
    private static readonly Brush WristStripe = Ink("#88745A");

    private static readonly DrawingGroup HeadCoat = Coat(Head,"#AB9170","#968066",FaceMarkings);
    private static readonly DrawingGroup BodyCoat = Coat(Body,"#AB9170","#82705D",ChestMarkings);
    private static readonly DrawingGroup TailCoat = Coat(Tail,"#81796D","#6E6053",TailMarkings);
    private static readonly DrawingGroup Muzzle = MakeMuzzle();

    private static Geometry Shape(string path)
    {
        Geometry geometry = Geometry.Parse(path);
        geometry.Freeze();
        return geometry;
    }

    private static Color ParseColor(string hex) { return (Color)ColorConverter.ConvertFromString(hex); }

    private static Brush Ink(string hex)
    {
        var brush = new SolidColorBrush(ParseColor(hex));
        brush.Freeze();
        return brush;
    }

    private static Brush Shade(string from, string to)
    {
        var brush = new LinearGradientBrush(ParseColor(from),ParseColor(to),new Point(.30,.1),new Point(.85,1));
        brush.Freeze();
        return brush;
    }

    private static Pen Stroke(string color, double width)
    {
        var pen = new Pen(Ink(color),width) {
            StartLineCap=PenLineCap.Round, EndLineCap=PenLineCap.Round, LineJoin=PenLineJoin.Round
        };
        pen.Freeze();
        return pen;
    }

    private static void Wash(DrawingContext dc, string light, string dark, string path)
    {
        var brush = Shade(light,dark);
        dc.DrawGeometry(brush,null,Shape(path));
    }

    private static void Fill(DrawingContext dc, string hex, string path) { dc.DrawGeometry(Ink(hex),null,Shape(path)); }
    private static void Line(DrawingContext dc, string hex, double width, string path) { dc.DrawGeometry(null,Stroke(hex,width),Shape(path)); }
    private static void Oval(DrawingContext dc, string hex, double x, double y, double rx, double ry)
    {
        dc.DrawEllipse(Ink(hex),null,new Point(x,y),rx,ry);
    }

    private static DrawingGroup Coat(Geometry outline, string light, string dark, Action<DrawingContext> markings)
    {
        var coat = new DrawingGroup();
        using (DrawingContext dc = coat.Open())
        {
            dc.DrawGeometry(Shade(light,dark),null,outline);
            dc.PushClip(outline);
            markings(dc);
            dc.Pop();
        }
        coat.Freeze();
        return coat;
    }

    private static DrawingGroup MakeMuzzle()
    {
        var drawing = new DrawingGroup();
        using (DrawingContext dc = drawing.Open())
        {
            dc.DrawGeometry(Ink("#AA6568"),Stroke("#594139",.85),Shape("M -4.8,8.1 Q -3.5,6.4 0,7.3 Q 3.5,6.4 4.8,8.1 Q 5.4,9.7 1,12.3 Q 0,13.1 -1,12.4 Q -5.4,9.7 -4.8,8.1 Z"));
            Line(dc,"#5C4C44",1.05,"M 0,12.2 L 0,14.7 Q -3.0,18.2 -6.4,15.6 M 0,14.7 Q 3.1,17.8 6.4,15.4");
        }
        drawing.Freeze();
        return drawing;
    }

    private static void Bloom(DrawingContext dc,string center,string mid,string edge,
                              Point origin,double rx,double ry,string path)
    {
        var b=new RadialGradientBrush {
            Center=origin, GradientOrigin=origin, RadiusX=rx, RadiusY=ry
        };
        b.GradientStops.Add(new GradientStop(ParseColor(center),0));
        b.GradientStops.Add(new GradientStop(ParseColor(mid),.65));
        b.GradientStops.Add(new GradientStop(ParseColor(edge),1));
        dc.DrawGeometry(b,null,Shape(path));
    }

    private static void FaceMarkings(DrawingContext d)
    {
        // The photographed crown is smoky rather than orange. A soft light falls on the left outer ruff.
        Bloom(d,"#CFB58C","#AABFA37D","#00A68B6E",new Point(.38,.5),.67,.78,
            "M -60,-27 C -44,-35 -27,-27 -24,-11 Q -14,12 -26,28 Q -40,38 -60,25 Z");
        Bloom(d,"#927B61","#9C917B62","#008C7861",new Point(.55,.26),.68,.8,
            "M -42,-48 Q -9,-60 28,-52 Q 48,-39 41,-21 Q 25,-13 18,-6 Q 1,-11 -13,-10 Q -34,-18 -42,-48 Z");

        // Angled dark ear rims, warm inner shells and low white furnishing on the broader left ear.
        Wash(d,"#625748","#8B775D","M -48,-68 L -36,-70 Q -27,-59 -17,-43 Q -31,-43 -42,-27 L -51,-27 Z");
        Wash(d,"#705F4C","#94806A","M 18,-43 Q 31,-60 44,-71 L 49,-60 Q 42,-46 41,-29 Q 33,-39 18,-43 Z");
        Wash(d,"#A58A81","#857269","M -41,-55 Q -41,-60 -37,-58 Q -30,-53 -22,-44 Q -32,-43 -39,-33 Q -42,-41 -41,-55 Z");
        Wash(d,"#A8907D","#887665","M 25,-44 Q 33,-55 41,-60 Q 41,-48 38,-36 Q 32,-42 25,-44 Z");
        Fill(d,"#D4C8AE","M -43,-31 Q -34,-32 -29,-45 Q -28,-37 -35,-30 Q -30,-33 -26,-37 Q -27,-32 -29,-29 Q -37,-25 -46,-27 Q -43,-28 -40,-30 Z");
        Line(d,"#F0E7CE",.85,"M -47,-27 Q -37,-25 -30,-38 M -45,-28 Q -35,-27 -33,-34");
        Fill(d,"#CFBFA2","M 26,-42 Q 31,-40 38,-45 Q 36,-40 32,-39 Q 36,-39 39,-41 Q 38,-37 37,-35 Q 32,-40 26,-41 Z");

        // Paired dark scallops enclose the small golden islands on the brow and meet the nasal bridge.
        Wash(d,"#695A48","#83705A","M -28,-43 Q -20,-45 -16,-43 C -14,-36 -12,-28 -8,-24 " +
            "Q -12,-33 -8,-43 L -4,-46 Q -4,-37 -.5,-31 Q 1,-27 2,-23 Q 3,-30 4,-35 Q 6,-42 9,-46 " +
            "L 15,-45 Q 10,-37 10,-32 Q 10,-27 11,-23 Q 13,-27 14,-33 Q 15,-40 19,-43 L 24,-43 " +
            "Q 18,-36 18,-29 Q 18,-24 15,-20 Q 11,-17 7,-16 Q 3,-17 1,-23 Q -3,-21 -4,-17 " +
            "Q -9,-18 -13,-24 Q -19,-33 -28,-43 Z");
        Fill(d,"#78664F","M -37,-32 Q -29,-35 -22,-31 Q -28,-31 -34,-27 Z M 26,-32 Q 33,-36 39,-32 L 40,-28 Q 34,-31 26,-30 Z");
        // Pale rims and little dark outer eye corners are characteristic of this cat's alert expression.
        Wash(d,"#D5BB94","#BBA481","M -37,-13 Q -32,-21 -22,-22 Q -15,-22 -11,-18 Q -22,-18 -27,-14 Q -32,-12 -37,-10 Z " +
            "M 9,-17 Q 15,-23 23,-22 Q 31,-21 36,-17 Q 25,-19 17,-15 Z");
        Fill(d,"#6D5946","M -40,-9 Q -33,-14 -27,-16 Q -28,-12 -30,-10 Q -36,-10 -40,-7 Z " +
            "M 29,-15 Q 35,-18 42,-16 Q 36,-15 30,-12 Z");
        Wash(d,"#D9C4A1","#C0AC8D","M -30,-3 Q -21,5 -11,-1 Q -15,4 -23,4 Q -28,3 -30,-3 Z " +
            "M 10,-1 Q 20,4 30,-5 Q 27,3 18,3 Q 12,3 10,-1 Z");
        // Two broken, downswept bands per cheek, softened into the longer jaw fur.
        Wash(d,"#8C775C","#A88F70","M -55,-3 Q -44,-8 -32,-7 Q -42,-5 -50,1 L -55,4 Z " +
            "M -51,8 Q -44,3 -30,2 Q -34,5 -42,8 Q -46,10 -52,13 Z " +
            "M 33,-8 Q 44,-8 54,-4 L 55,0 Q 46,-5 34,-5 Z M 31,0 Q 43,0 54,7 L 53,10 Q 47,7 40,5 Q 34,4 31,0 Z");
        Bloom(d,"#77624F","#8C7C6654","#009D8265",new Point(.50,.70),.56,.73,
            "M -10,-20 Q -2,-23 6,-20 Q 12,-13 11,-4 Q 10,5 4,10 L -5,10 Q -13,2 -12,-8 Z");
        Wash(d,"#D4BA91","#C3AA86","M -24,10 Q -21,4 -15,3 Q -8,2 -2,7 Q .2,9 2,7 Q 10,2 17,4 Q 24,5 26,12 " +
            "Q 24,20 13,22 Q 7,25 -2,24 Q -11,25 -18,20 Q -25,18 -24,10 Z");
        var chin=new LinearGradientBrush { StartPoint=new Point(0,0),EndPoint=new Point(.05,1) };
        chin.GradientStops.Add(new GradientStop(ParseColor("#00D7C4A4"),0));
        chin.GradientStops.Add(new GradientStop(ParseColor("#DFD3B9"),.57));
        chin.GradientStops.Add(new GradientStop(ParseColor("#EFE6D1"),1));
        d.DrawGeometry(chin,null,Shape("M -22,11 C -14,10 -8,13 -.5,13 Q 13,11 22,11.5 " +
            "C 24,18.5 19,25 16,27.5 Q 9,33 2,33 Q -6,34 -8,32.6 Q -7,32 -7,31 " +
            "Q -15,30.7 -18.2,26.7 Q -20.2,25 -21,21.5 Q -19,22 -19,21 Q -23,18 -22,11 Z"));
        Oval(d,"#A38C6C",-12,10.6,.83,.63);
        Oval(d,"#A38C6C",-16,12.1,.72,.58);
        Oval(d,"#A38C6C",14,11.4,.77,.6);
    }

    private static void ChestMarkings(DrawingContext d)
    {
        // The white neck collar and the separate, narrow belly plume sit on a striped brown breast.
        Wash(d,"#988066","#7F6F5F","M -58,2 L -34,-6 Q -19,24 -20,48 Q -20,68 -33,87 Q -39,98 -39,119 L -59,126 Z");
        Wash(d,"#937B63","#7B6E60","M 32,-6 L 58,0 L 62,125 L 40,121 Q 40,103 32,87 Q 23,67 26,44 Q 25,18 32,-6 Z");
        Wash(d,"#766250","#8D7459","M -50,34 Q -36,44 -21,46 Q -31,48 -47,43 Z M 19,44 Q 38,38 49,29 L 52,35 Q 37,47 18,50 Z " +
            "M -54,54 Q -39,57 -26,63 Q -39,61 -54,60 Z M 24,59 Q 39,54 54,47 L 56,53 Q 38,63 25,66 Z");
        Wash(d,"#A58C6B","#8A7864","M -19,53 Q -10,56 -2,54 Q 8,54 20,48 Q 20,70 15,88 Q 12,104 5,114 " +
            "Q -4,109 -11,94 Q -17,77 -19,53 Z");
        Wash(d,"#BFA888","#E5D6BB","M -12,69 Q -9,72 -5,75 Q -.5,72 2,68 Q 6,62 12,59 Q 10,67 11,72 Q 13,71 15,68 " +
            "Q 17,79 13,88 Q 11,95 8,100 Q 9,96 7,96 Q 6,107 1,114 Q -.5,109 -3,107 " +
            "Q -4,109 -5,110 Q -5,104 -8,99 Q -13,90 -11,82 Q -13,79 -12,69 Z");
        Wash(d,"#F4EFDD","#DBD0B5","M -42,-7 L 42,-7 Q 44,12 43,24 Q 45,29 43,33 Q 38,37 30,38 " +
            "Q 23,41 18,47 Q 12,51 5,52 Q 2,56 -3,57 Q -7,57 -10,53 Q -20,53 -25,48 Q -34,47 -37,42 " +
            "Q -41,40 -44,40 Q -44,35 -42,32 Q -46,24 -45,15 Z");
        Fill(d,"#E7DFC9","M -32,34 Q -27,39 -19,39 Q -22,42 -28,41 Q -29,43 -25,45 Q -32,42 -32,34 Z " +
            "M 21,30 Q 31,28 36,24 Q 34,30 27,33 Q 30,33 33,32 Q 28,37 20,38 Q 23,35 21,30 Z");
        Wash(d,"#DBCEB5","#EAE1CC","M -52,115 Q -47,111 -43,111 Q -41,109 -38,110 Q -31,110 -28,115 L -27,126 L -53,126 Z");
        Line(d,"#B7A388",.78,"M -43.5,117 Q -44.4,118.5 -44,121");
    }

    private static void TailMarkings(DrawingContext d)
    {
        // Broad smoky rings, strongest at the hanging tip. Coordinates are local to the root of the plume.
        Wash(d,"#8D8270","#716D62","M -12,-18 L 2,-17 Q 10,-4 18,-2 Q 25,3 35,4 L 38,18 L -16,22 Z");
        Wash(d,"#766F63","#625E57","M 19,-28 L 30,-29 Q 27,-11 36,-3 Q 41,1 47,3 L 40,11 Q 25,7 19,-3 Q 16,-11 19,-28 Z");
        Wash(d,"#70685E","#585651","M 38,-51 L 50,-52 Q 45,-32 55,-23 Q 61,-18 67,-16 L 62,-6 Q 48,-11 41,-20 Q 34,-32 38,-51 Z");
        Wash(d,"#514E48","#454744","M 52,-69 L 89,-68 L 92,-24 L 76,-19 Q 64,-23 57,-34 Q 52,-42 53,-51 Q 55,-59 52,-69 Z");
    }
    private static void DrawEye(DrawingContext dc, int side, double gazeX, double gazeY, double blink, double crouch = 0)
    {
        dc.PushTransform(new TranslateTransform(side*18.65,-8.45));
        dc.PushTransform(new ScaleTransform(side*10.9/12,9.1/9));
        if (blink>.92)
        {
            dc.DrawGeometry(null,Lid,ClosedEye);
        }
        else
        {
            dc.PushTransform(new ScaleTransform(1,1-blink));
            dc.DrawGeometry(Iris,EyeEdge,Eye);
            dc.PushClip(Eye);
            double boost = 1.0 + 0.16 * crouch;
            double x = -.3 + side * gazeX * 6.4, y = .2 + gazeY * 4.6;
            double rx = (3.7 + Math.Abs(gazeY) * 0.65) * boost;
            double ry = 6.2 * boost;
            dc.DrawEllipse(Pupil, null, new Point(x, y), rx, ry);
            dc.DrawEllipse(Glint, null, new Point(x - side * 1.35, y - 2.3), 1.35, 1.4);
            dc.Pop();
            dc.Pop();
        }
        dc.Pop();
        dc.Pop();
    }

    private static readonly Brush LidFur = Ink("#A2896B");

    private static void DrawHead(DrawingContext dc, double gazeX, double gazeY, double blink,
                                 double anger = 0, double angerBreath = 0.5, double crouch = 0)
    {
        dc.DrawDrawing(HeadCoat);
        dc.DrawGeometry(null,EarFeather,EarTuft);
        gazeX = Math.Min(1,.37+.79*gazeX);
        gazeY -= .035;
        DrawEye(dc,-1,gazeX,gazeY,blink,crouch);
        DrawEye(dc,1,gazeX,gazeY,blink,crouch);
        if (anger > .01 && blink <= .92)
        {
            CatArt.DrawAngryEyelid(dc, -18.65, -8.45, 11.2, 9.2, -1, LidFur, Lid, anger, angerBreath);
            CatArt.DrawAngryEyelid(dc, 18.65, -8.45, 11.2, 9.2, 1, LidFur, Lid, anger, angerBreath);
        }
        dc.PushTransform(new TranslateTransform(gazeX*1.55,gazeY*.5));
        dc.DrawDrawing(Muzzle);
        dc.Pop();
        dc.PushTransform(new ScaleTransform(-1,1));
        dc.DrawGeometry(null,WhiskerPen,Whiskers);
        dc.Pop();
        dc.DrawGeometry(null,WhiskerPen,Whiskers);
    }

    public static void DrawPortrait(DrawingContext dc)
    {
        dc.PushTransform(new RotateTransform(-2.25));
        dc.PushTransform(new ScaleTransform(1.055,1));
        DrawHead(dc,0,0,0);
        dc.Pop();
        dc.Pop();
    }

    // Rings are placed on the changing foreleg curve, keeping their widths and positions
    // relative to the wrist even when a paw crosses the keyboard or curls towards the chest.
    private struct Foreleg
    {
        private readonly Point shoulder, near, far, paw;
        private readonly int side;

        public Foreleg(Point shoulder, Point paw, int side, double pawRaised)
        {
            this.shoulder = shoulder;
            this.paw = paw;
            this.side = side;
            double bend = pawRaised >= 0 ? pawRaised : Math.Max(0,Math.Min(1,(228-paw.Y)/44));
            double high = Math.Max(0, Math.Min(1, (185.0 - paw.Y) / 55.0));
            high = high * high * (3 - 2 * high);
            Vector span = paw-shoulder;
            Point nearLow = new Point(shoulder.X+span.X*.18+side*(3+7*bend),shoulder.Y+10+bend*9);
            Point farLow = new Point(paw.X-span.X*.20+side*(2+12*bend),paw.Y-16+31*bend);
            Point nearHigh = new Point(shoulder.X+span.X*.32+side*6,shoulder.Y+span.Y*.28+12);
            Point farHigh = new Point(shoulder.X+span.X*.72+side*4,shoulder.Y+span.Y*.72+8);
            near = new Point(nearLow.X + (nearHigh.X - nearLow.X) * high, nearLow.Y + (nearHigh.Y - nearLow.Y) * high);
            far = new Point(farLow.X + (farHigh.X - farLow.X) * high, farLow.Y + (farHigh.Y - farLow.Y) * high);
        }

        private Point Center(double u)
        {
            double v = 1-u;
            return new Point(v*v*v*shoulder.X+3*v*v*u*near.X+3*v*u*u*far.X+u*u*u*paw.X,
                             v*v*v*shoulder.Y+3*v*v*u*near.Y+3*v*u*u*far.Y+u*u*u*paw.Y);
        }

        private Vector Normal(double u,double minLengthSquared)
        {
            double v = 1-u;
            Vector tangent=(near-shoulder)*(3*v*v)+(far-near)*(6*v*u)+(paw-far)*(3*u*u);
            if (tangent.LengthSquared<minLengthSquared) tangent=new Vector(0,1);
            tangent.Normalize();
            return new Vector(-tangent.Y,tangent.X);
        }

        public StreamGeometry Outline()
        {
            const int steps=24;
            Point[] opposite=new Point[steps+1];
            var arm=new StreamGeometry { FillRule=FillRule.Nonzero };
            using (StreamGeometryContext path=arm.Open())
            {
                for (int i=0;i<=steps;i++)
                {
                    double u=i/(double)steps;
                    double baseR=12*BodyWidth*(1-u)+8.1*u;
                    double k=Math.Min(1,u/.14);
                    double dome=.22+.78*Math.Sqrt(Math.Max(0,1-(1-k)*(1-k)));
                    double radius=baseR*dome;
                    Point center=Center(u);
                    Vector normal=Normal(u,.01)*radius;
                    if (i==0) path.BeginFigure(center+normal,true,true);
                    else path.LineTo(center+normal,true,true);
                    opposite[i]=center-normal;
                }
                for (int i=steps;i>=0;i--) path.LineTo(opposite[i],true,true);
            }
            return arm;
        }

        private Point StripePoint(double u,double across)
        {
            return Center(u) + Normal(u,.001)*(across*side*(12*BodyWidth*(1-u)+8.1*u));
        }

        public void DrawStripe(DrawingContext dc,double t,double breadth,Brush color)
        {
            var stripe=new StreamGeometry();
            using (StreamGeometryContext path=stripe.Open())
            {
                path.BeginFigure(StripePoint(t-.035,-1.35),true,true);
                path.BezierTo(StripePoint(t-.040,-.73),StripePoint(t+.029,-.06),StripePoint(t+.010,.88),true,false);
                path.BezierTo(StripePoint(t+.040,.56),StripePoint(t+.064,.03),StripePoint(t+breadth*.57,-.38),true,false);
                path.QuadraticBezierTo(StripePoint(t+.018,-.83),StripePoint(t+.020,-1.30),true,false);
            }
            dc.DrawGeometry(color,null,stripe);
        }
    }

    private static void DrawArm(DrawingContext dc,Point shoulder,Point paw,int side,double activity,double pawRaised)
    {
        var leg=new Foreleg(shoulder,paw,side,pawRaised);
        Geometry arm=leg.Outline();
        Brush blendedSleeve = CatArt.ArmGradient(
            Color.FromRgb(156, 134, 108), Color.FromRgb(162, 139, 111), 0.35, Color.FromRgb(176, 152, 120), shoulder, paw);
        dc.DrawGeometry(blendedSleeve,null,arm);
        dc.PushClip(arm);
        leg.DrawStripe(dc,.26,.100,UpperStripe);
        leg.DrawStripe(dc,side>0?.44:.47,.117,MiddleStripe);
        leg.DrawStripe(dc,side>0?.64:.66,.092,LowerStripe);
        leg.DrawStripe(dc,.79,.063,WristStripe);
        dc.Pop();
        dc.PushTransform(new TranslateTransform(paw.X,paw.Y));
        dc.DrawGeometry(Sock,null,FrontSock);
        dc.Pop();
        if (activity<.25)
            dc.DrawLine(Toe,new Point(paw.X-1.8,paw.Y+3.5),new Point(paw.X-1.9,paw.Y+5.9));
    }

    private static double Lean(Point leftPaw)
    {
        double s=Math.Max(0,Math.Min(1,(157-leftPaw.X)/50));
        double h=Math.Max(0,Math.Min(1,(195-leftPaw.Y)/45));
        double high=h*h*(3-2*h);
        return s*s*(3-2*s)*(1-high);
    }

    private static Matrix HeadPose(double t, double gazeX, double gazeY, double lean)
    {
        Matrix pose=Matrix.Identity;
        pose.Scale(1.055,1);
        pose.Rotate(-2.25+2.8*gazeX-2.4*lean);
        pose.Translate(214-33.5*lean+2.5*gazeX,BodyTop-2+2*gazeY+Math.Sin(t*1.7)*.6);
        return pose;
    }

    public static bool ContainsHead(Point pointer, double time, double gazeX, double gazeY, Point leftPaw)
    {
        Matrix pose=HeadPose(time,gazeX,gazeY,Lean(leftPaw));
        pose.Invert();
        return Head.FillContains(pose.Transform(pointer));
    }

    private static void PushBodyPose(DrawingContext dc,double lean,double sxScale,double syScale,double bodyYShift)
    {
        dc.PushTransform(new MatrixTransform(1,0,.285*lean,1,-233*.285*lean,0));
        dc.PushTransform(new TranslateTransform(214,BodyTop + bodyYShift));
        dc.PushTransform(new ScaleTransform(BodyWidth * sxScale, BodyHeight * syScale));
    }

    public static void Draw(DrawingContext dc,double time,double gazeX,double gazeY,double blink,
                            Point leftPaw,Point rightPaw,double leftActivity,double rightActivity,
                            Action<DrawingContext> between = null, double pawRaised = -1, CatMood mood = default(CatMood))
    {
        double sleep = Math.Max(0, Math.Min(1, mood.Sleep));
        double crouch = Math.Max(0, Math.Min(1, mood.Crouch));
        double lean=Lean(leftPaw) * (1 - sleep);
        double syScale = 1.0 - 0.45 * sleep - 0.10 * crouch;
        double sxScale = 1.0 + 0.04 * sleep + 0.04 * crouch;
        double bodyYShift = (1.0 - syScale) * (235.0 - BodyTop);
        double angerBreath = 0.5 + 0.5 * Math.Sin(time * 6.8);

        double tailSpeed = 1.8 + mood.Anger * 2.8 + crouch * 2.2 - sleep * 1.1;
        double tailAmp = 2.1 + mood.Anger * 2.0 + crouch * 2.5;
        dc.PushTransform(new TranslateTransform(187,184 + 14 * sleep));
        dc.PushTransform(new RotateTransform(186+Math.Sin(time*tailSpeed)*tailAmp));
        dc.PushTransform(new ScaleTransform(1.1,1.22));
        dc.DrawDrawing(TailCoat);
        dc.Pop();
        dc.Pop();
        dc.Pop();

        PushBodyPose(dc,lean,sxScale,syScale,bodyYShift);
        dc.DrawDrawing(BodyCoat);
        dc.Pop();
        dc.Pop();
        dc.Pop();
        // Same layering as the base cat: a caller-supplied layer (the
        // gamepad) goes between the torso and the forelegs, or the opaque
        // body hides it.
        if (between != null) between(dc);
        double rSy = BodyTop + bodyYShift + 17.0 * syScale;
        double lSy = BodyTop + bodyYShift + (17.0 - 5.0 * lean) * syScale;
        double rShear = (rSy - 233.0) * 0.285 * lean;
        double lShear = (lSy - 233.0) * 0.285 * lean;
        DrawArm(dc,new Point(214+22*BodyWidth*sxScale+rShear, rSy),rightPaw,1,rightActivity,pawRaised);
        DrawArm(dc,new Point(214-(22*BodyWidth*sxScale+2.5*lean)+lShear, lSy),leftPaw,-1,leftActivity,pawRaised);

        // This front ruff overlaps the upper arm stripes, and tucks slightly inward during sleep.
        PushBodyPose(dc,lean,sxScale * (1.0 - 0.22 * sleep),syScale,bodyYShift);
        dc.DrawGeometry(RuffFur,null,Ruff);
        dc.Pop();
        dc.Pop();
        dc.Pop();

        double breathFreq = sleep > 0.5 ? 1.15 : 1.7;
        double breathAmp = sleep > 0.5 ? 1.6 : 0.6;
        double hx = 214 - 33.5*lean + 2.5*gazeX*(1 - sleep) - 2.0*sleep;
        double hyNorm = BodyTop - 2 + 2*gazeY + Math.Sin(time*breathFreq)*breathAmp + 12.0*crouch;
        double hy = hyNorm + (186 + Math.Sin(time*1.15)*1.8 - hyNorm)*sleep;
        double rot = (-2.25 + 2.8*gazeX - 2.4*lean)*(1 - sleep) - 8.5*sleep;
        dc.PushTransform(new TranslateTransform(hx, hy));
        dc.PushTransform(new RotateTransform(rot));
        dc.PushTransform(new ScaleTransform(1.055,1));
        DrawHead(dc,gazeX,gazeY,blink,mood.Anger,angerBreath,mood.Crouch);
        if (mood.Sweat > .01) CatArt.DrawSweat(dc, time, mood.Sweat);
        if (mood.Anger > .01) CatArt.DrawAnger(dc, time, mood.Anger);
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    internal static void DrawTailPart(DrawingContext dc, double time, CatMood mood)
    {
        double sleep = Math.Max(0, Math.Min(1, mood.Sleep));
        double crouch = Math.Max(0, Math.Min(1, mood.Crouch));
        double tailSpeed = 1.8 + mood.Anger * 2.8 + crouch * 2.2 - sleep * 1.1;
        double tailAmp = 2.1 + mood.Anger * 2.0 + crouch * 2.5;
        dc.PushTransform(new TranslateTransform(187, 184 + 14 * sleep));
        dc.PushTransform(new RotateTransform(186 + Math.Sin(time * tailSpeed) * tailAmp));
        dc.PushTransform(new ScaleTransform(1.1, 1.22));
        dc.DrawDrawing(TailCoat);
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    internal static void DrawBodyPart(DrawingContext dc, double lean, double sxScale, double syScale)
    {
        double bodyYShift = (1.0 - syScale) * (235.0 - BodyTop);
        PushBodyPose(dc, lean, sxScale, syScale, bodyYShift);
        dc.DrawDrawing(BodyCoat);
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    internal static void DrawRuffPart(DrawingContext dc, double lean, double sxScale, double syScale, double sleep)
    {
        double bodyYShift = (1.0 - syScale) * (235.0 - BodyTop);
        PushBodyPose(dc, lean, sxScale * (1.0 - 0.22 * sleep), syScale, bodyYShift);
        dc.DrawGeometry(RuffFur, null, Ruff);
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    internal static Point GetShoulderPoint(bool left, double lean, double sxScale, double syScale)
    {
        double bodyYShift = (1.0 - syScale) * (235.0 - BodyTop);
        double sy = BodyTop + bodyYShift + (left ? (17.0 - 5.0 * lean) : 17.0) * syScale;
        double shear = (sy - 233.0) * 0.285 * lean;
        double x = left ? 214.0 - (22.0 * BodyWidth * sxScale + 2.5 * lean) + shear
                        : 214.0 + 22.0 * BodyWidth * sxScale + shear;
        return new Point(x, sy);
    }

    internal static void DrawArmPart(DrawingContext dc, bool left, Point paw, double lean, double activity,
                                     double pawRaised, double sxScale, double syScale)
    {
        Point shoulder = GetShoulderPoint(left, lean, sxScale, syScale);
        DrawArm(dc, shoulder, paw, left ? -1 : 1, activity, pawRaised);
    }

    internal static void DrawHeadPart(DrawingContext dc, double gazeX, double gazeY, double blink,
                                      double anger, double angerBreath, double crouch)
    {
        dc.PushTransform(new ScaleTransform(1.055, 1));
        DrawHead(dc, gazeX, gazeY, blink, anger, angerBreath, crouch);
        dc.Pop();
    }

    internal static void ComputeHeadPose(double time, double gazeX, double gazeY, double lean, double sleep, double crouch,
                                         out double hx, out double hy, out double rot)
    {
        double breathFreq = sleep > 0.5 ? 1.15 : 1.7;
        double breathAmp = sleep > 0.5 ? 1.6 : 0.6;
        hx = 214.0 - 33.5 * lean + 2.5 * gazeX * (1 - sleep) - 2.0 * sleep;
        double hyNorm = BodyTop - 2 + 2 * gazeY + Math.Sin(time * breathFreq) * breathAmp + 12.0 * crouch;
        hy = hyNorm + (186 + Math.Sin(time * 1.15) * 1.8 - hyNorm) * sleep;
        rot = (-2.25 + 2.8 * gazeX - 2.4 * lean) * (1 - sleep) - 8.5 * sleep;
    }
}
