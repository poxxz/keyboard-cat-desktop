using System;
using System.Windows;
using System.Windows.Media;

// The resting pose, keyboard and mouse share the same 480 x 320 drawing space.
public static partial class CatArt
{
    private static SolidColorBrush Ink(byte r, byte g, byte b)
    {
        SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static Brush Wash(byte[] light, byte[] dark, Point from, Point to)
    {
        LinearGradientBrush brush = new LinearGradientBrush(
            Color.FromRgb(light[0], light[1], light[2]),
            Color.FromRgb(dark[0], dark[1], dark[2]), from, to);
        brush.Freeze();
        return brush;
    }

    private static Pen Stroke(Brush brush, double width)
    {
        Pen pen = new Pen(brush, width);
        pen.StartLineCap = PenLineCap.Round;
        pen.EndLineCap = PenLineCap.Round;
        pen.LineJoin = PenLineJoin.Round;
        pen.Freeze();
        return pen;
    }

    private static Geometry Shape(string data)
    {
        Geometry g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }

    private static readonly Brush Fur = WashAbsolute(new byte[] { 31,29,32 }, new byte[] { 22,21,24 }, new Point(160,102), new Point(268,222));
    private static readonly Brush Face = Wash(new byte[] { 32,30,32 }, new byte[] { 24,23,25 }, new Point(0,0), new Point(0.9,1));
    private static readonly Brush Foreleg = Fur;
    private static readonly Brush BlackLidFur = Ink(29,27,30);
    private static readonly Brush Iris = Wash(new byte[] { 215,245,80 }, new byte[] { 182,224,62 }, new Point(0.2,0), new Point(0.83,1));
    private static readonly Brush Pupil = Ink(22,24,16);
    private static readonly Brush Ear = Ink(36,32,36);
    private static readonly Brush WhiskerRoot = Ink(172,169,162);
    private static readonly Pen Whisker = Stroke(Ink(39,37,38), 0.9);
    private static readonly Pen RootPen = Stroke(WhiskerRoot, 0.95);
    private static readonly Pen Lid = Stroke(Ink(189,221,70), 2.2);
    private static readonly Brush WednesdayIris = Wash(new byte[] { 247,203,78 }, new byte[] { 219,168,44 }, new Point(0.2,0), new Point(0.83,1));
    private static readonly Pen WednesdayLid = Stroke(Ink(219,168,44), 2.2);
    private static readonly Pen PawLight = Stroke(Ink(92,89,91), 0.95);

    private static readonly Geometry Body = Shape(
        "M 175,102 C 175,121 173,140 168,156 C 163,173 157,195 160,210 " +
        "C 161,219 163,225 167,228 C 165,234 169,239 175,239 " +
        "C 181,239 185,235 186,231 C 199,234 223,235 240,230 " +
        "C 241,235 245,238 251,238 C 258,238 262,232 260,227 " +
        "C 267,218 269,201 267,185 C 264,159 253,130 251,103 Z");

    private static readonly Geometry Head = Shape(
        "M -45,-21 C -45,-32 -43,-52 -39,-59 C -37,-63 -34,-63 -31,-61 " +
        "C -26,-58 -19,-48 -15,-44 C -5,-47 6,-47 16,-44 " +
        "C 22,-51 30,-62 34,-63 C 39,-64 41,-57 43,-47 C 45,-39 45,-27 45,-20 " +
        "C 50,-12 50,-1 47,9 C 44,19 36,27 25,30 C 11,35 -13,35 -28,29 " +
        "C -39,27 -47,17 -49,5 C -51,-4 -49,-14 -45,-21 Z");
    private static readonly Geometry EarLeft = Shape("M -40,-35 Q -40,-51 -36,-56 Q -32,-54 -23,-43 Q -32,-42 -40,-35 Z");
    private static readonly Geometry EarRight = Shape("M 24,-44 Q 33,-56 35,-57 Q 39,-52 40,-36 Q 35,-41 24,-44 Z");

    private static double Limit(double value, double low, double high)
    {
        return Math.Max(low, Math.Min(high, value));
    }

    private static double Smooth(double x)
    {
        x = Limit(x, 0, 1);
        return x * x * (3 - 2 * x);
    }

    private static void Tail(DrawingContext dc, double t, bool ginger)
    {
        TailMood(dc, t, ginger, default(CatMood));
    }

    private static void TailMood(DrawingContext dc, double t, bool ginger, CatMood mood)
    {
        double freq = 1.9 + mood.Anger * 3.0 + mood.Crouch * 2.4 - mood.Sleep * 1.2;
        double amp = 1.0 + mood.Anger * 0.65 + mood.Crouch * 0.75 - mood.Sleep * 0.65;
        double wave = Math.Sin(t * freq) * amp;
        double tip = Math.Sin(t * freq - 0.68) * amp;
        double droop = mood.Sleep * 24.0;
        StreamGeometry tail = BuildTailGeometry(wave, tip, droop);
        dc.DrawGeometry(ginger ? Ginger.TailFur : Fur, null, tail);
        if (ginger) Ginger.DrawTailBands(dc, tail, wave, tip);
    }

    private static StreamGeometry BuildTailGeometry(double wave, double tip, double droop)
    {
        StreamGeometry tail = new StreamGeometry();
        using (StreamGeometryContext g = tail.Open())
        {
            g.BeginFigure(new Point(251,199), true, true);
            g.BezierTo(new Point(263,200),new Point(273,204),new Point(281,201 + wave * 1.3 + droop * 0.3),true,true);
            g.BezierTo(new Point(290,198),new Point(292,186 + wave * 2 + droop * 0.7),new Point(300,177 + wave * 2.5 + droop * 0.95),true,true);
            g.BezierTo(new Point(307,169 + tip * 4 + droop),new Point(317,165 + tip * 5 + droop),new Point(325,166 + tip * 6 + droop),true,true);
            g.BezierTo(new Point(335,167 + tip * 6 + droop),new Point(334,181 + tip * 6 + droop),new Point(324,181 + tip * 6 + droop),true,true);
            g.BezierTo(new Point(311,180 + tip * 4 + droop * 0.9),new Point(305,191 + droop * 0.6),new Point(298,202 + wave * 2 + droop * 0.4),true,true);
            g.BezierTo(new Point(289,217 + wave + droop * 0.2),new Point(268,220),new Point(251,214),true,true);
        }
        return tail;
    }

    private static Point Cubic(Point a, Point b, Point c, Point d, double t)
    {
        double z = 1-t;
        return new Point(z*z*z*a.X+3*z*z*t*b.X+3*z*t*t*c.X+t*t*t*d.X,
                         z*z*z*a.Y+3*z*z*t*b.Y+3*z*t*t*c.Y+t*t*t*d.Y);
    }

    private static StreamGeometry BuildArmGeometry(Point a, Point b, Point c, Point paw, double pawRadius)
    {
        StreamGeometry limb = new StreamGeometry { FillRule = FillRule.Nonzero };
        const int count = 24;
        Point[] secondSide = new Point[count+1];
        using (StreamGeometryContext g = limb.Open())
        {
            for (int i = 0; i <= count; ++i)
            {
                double u = i/(double)count;
                Point mid = Cubic(a,b,c,paw,u);
                Vector tangent = Cubic(a,b,c,paw,Math.Min(1,u+0.01)) - Cubic(a,b,c,paw,Math.Max(0,u-0.01));
                if (tangent.Length < 0.001) tangent = new Vector(0,1);
                tangent.Normalize();
                double baseW = 12.8 + (pawRadius - 12.8)*Smooth(u);
                double k = Limit(u / 0.14, 0, 1);
                double dome = 0.22 + 0.78 * Math.Sqrt(Math.Max(0, 1 - (1 - k) * (1 - k)));
                double width = baseW * dome;
                Vector offset = new Vector(-tangent.Y*width,tangent.X*width);
                Point first = mid+offset;
                secondSide[i] = mid-offset;
                if (i==0) g.BeginFigure(first,true,true);
                else g.LineTo(first,true,true);
            }
            for (int i=count; i>=0; --i) g.LineTo(secondSide[i],true,true);
        }
        return limb;
    }

    private static Point ShoulderPoint(bool left, double lean, double sxScale, double syScale)
    {
        double side = left ? -1 : 1;
        double rawSy = 136.0 - (left ? 10.0 * lean : 0.0);
        double sy = 235.0 + (rawSy - 235.0) * syScale;
        double sxShear = (sy - 233.0) * 0.282 * lean;
        return new Point(214.0 + side * (23.5 * sxScale + (left ? 3.2 * lean : 0.0)) + sxShear, sy);
    }

    private static void ComputeArmControls(Point a, Point paw, double side, double raised, out Point b, out Point c)
    {
        double dx = paw.X - a.X, dy = paw.Y - a.Y;
        double high = Smooth((185.0 - paw.Y) / 55.0);
        Point bLow = new Point(a.X + dx * 0.21 + side * (5.5 + 7.5 * raised), a.Y + dy * 0.35);
        Point cLow = new Point(a.X + dx * 0.78 + side * (4.0 + 20.0 * raised), paw.Y - 15.0 + raised * 41.0);
        Point bHigh = new Point(a.X + dx * 0.32 + side * 6.0, a.Y + dy * 0.28 + 12.0);
        Point cHigh = new Point(a.X + dx * 0.72 + side * 4.0, a.Y + dy * 0.72 + 8.0);
        b = new Point(bLow.X + (bHigh.X - bLow.X) * high, bLow.Y + (bHigh.Y - bLow.Y) * high);
        c = new Point(cLow.X + (cHigh.X - cLow.X) * high, cLow.Y + (cHigh.Y - cLow.Y) * high);
    }

    private static void Arm(DrawingContext dc, bool left, Point paw, double lean, double activity, bool ginger,
                            double pawRaised = -1, double sxScale = 1, double syScale = 1)
    {
        double side = left ? -1 : 1;
        double raised = pawRaised >= 0 ? pawRaised : Smooth((221 - paw.Y)/32);
        Point a = ShoulderPoint(left, lean, sxScale, syScale);
        Point b, c;
        ComputeArmControls(a, paw, side, raised, out b, out c);
        double pawRadius = 8.4 + Limit(activity,0,1)*1.0;
        StreamGeometry limb = BuildArmGeometry(a, b, c, paw, pawRadius);
        dc.DrawGeometry(ginger ? Ginger.LegFur : Foreleg, null, limb);
        if (ginger)
        {
            Ginger.DrawSock(dc, limb, paw, paw-c, pawRadius, activity);
            return;
        }
        // End the continuous foreleg with a soft mitten shape even at oblique key reaches.
        dc.DrawEllipse(Foreleg,null,paw,pawRadius+0.35,pawRadius*(0.94-0.06*activity));
        // A fixed raised value (gamepad pose) omits the arc under the paw.
        if (pawRaised < 0 && raised > 0.1)
        {
            dc.PushOpacity(raised * 0.85);
            StreamGeometry rim = new StreamGeometry();
            using (StreamGeometryContext g = rim.Open())
            {
                g.BeginFigure(new Point(paw.X+side*7.5,paw.Y+0.7),false,false);
                g.BezierTo(new Point(paw.X+side*7.1,paw.Y+6.2),new Point(paw.X+side*2.7,paw.Y+8.2),new Point(paw.X-side*2.3,paw.Y+7.7),true,true);
            }
            dc.DrawGeometry(null,PawLight,rim);
            dc.Pop();
        }
    }

    private static void Eye(DrawingContext dc, double side, double gx, double gy, double blink, bool ginger,
                            double anger = 0, double angerBreath = 0.5, double crouch = 0)
    {
        double angle = side*3 + gx*4;
        double x = side*19.1 + gx*2.6;
        double y = -15.8 + gy*1.7 - side*gx*1.1;
        double width = 13.0 * (1 - Math.Max(0, side*gx)*0.095);
        double height = 12.5 * (1 - blink);
        dc.PushTransform(new RotateTransform(angle,x,y));
        if (height < 1.6)
        {
            StreamGeometry slit = new StreamGeometry();
            using (StreamGeometryContext g = slit.Open())
            {
                g.BeginFigure(new Point(x-width*0.86,y),false,false);
                g.QuadraticBezierTo(new Point(x,y+3.0),new Point(x+width*0.86,y-0.2),true,true);
            }
            dc.DrawGeometry(null,ginger ? Ginger.Lid : Lid,slit);
        }
        else
        {
            EllipseGeometry iris = new EllipseGeometry(new Point(x,y),width,height);
            dc.DrawGeometry(ginger ? Ginger.Iris : Iris,ginger ? Ginger.EyeEdge : null,iris);
            dc.PushClip(iris);
            double boost = 1.0 + 0.16 * crouch;
            if (ginger) Ginger.DrawPupil(dc, x, y, gx, gy, crouch);
            else dc.DrawEllipse(Pupil,null,new Point(x+gx*7.8,y+gy*6.7),(4.3+Math.Abs(gy)*0.8)*boost,8.1*boost);
            dc.Pop();
            if (anger > .01)
                DrawAngryEyelid(dc, x, y, width, height, side, ginger ? Ginger.LidFur : BlackLidFur, ginger ? Ginger.Lid : Lid, anger, angerBreath);
        }
        dc.Pop();
    }

    private static void Whiskers(DrawingContext dc, double side, double gx, bool ginger)
    {
        double cheek = side*26 + gx*3;
        for (int k=0; k<3; ++k)
        {
            Point start = new Point(cheek+side*(0.8 - k*0.4),6 + k*5.0);
            Point end = new Point(cheek+side*(29-k*2.4), start.Y + (k-0.8)*5.6);
            StreamGeometry whisker = new StreamGeometry();
            using (StreamGeometryContext g = whisker.Open())
            {
                g.BeginFigure(start,false,false);
                g.QuadraticBezierTo(new Point((start.X+end.X)/2,start.Y-1.1+k*0.65),end,true,true);
            }
            dc.DrawGeometry(null,ginger ? Ginger.Whisker : Whisker,whisker);
            dc.DrawLine(ginger ? Ginger.WhiskerRoot : RootPen,start,new Point(start.X+side*(6.2-k*0.9),start.Y-0.45+k*0.65));
        }
    }

    public static void Draw(DrawingContext dc, double t, double gazeX, double gazeY,
                            double blink, Point leftPaw, Point rightPaw, double leftActivity, double rightActivity)
    {
        DrawStyled(dc, CatStyle.Black, t, gazeX, gazeY, blink, leftPaw, rightPaw, leftActivity, rightActivity);
    }

    public static void DrawStyled(DrawingContext dc, CatStyle style, double t, double gazeX, double gazeY,
                                  double blink, Point leftPaw, Point rightPaw, double leftActivity, double rightActivity,
                                  double pawRaised = -1, Action<DrawingContext> between = null, CatMood mood = default(CatMood))
    {
        if (style == CatStyle.PhotoTabby)
        {
            TabbyArt.Draw(dc,t,gazeX,gazeY,blink,leftPaw,rightPaw,leftActivity,rightActivity,between,pawRaised,mood);
            return;
        }
        if (style == CatStyle.JapaneseBobtail)
        {
            BobtailArt.Draw(dc,t,gazeX,gazeY,blink,leftPaw,rightPaw,leftActivity,rightActivity,between,pawRaised,mood);
            return;
        }
        bool ginger = style == CatStyle.Ginger;
        bool coffee = style == CatStyle.Coffee;
        bool silver = style == CatStyle.SilverShaded;
        bool siamese = style == CatStyle.Siamese;
        bool styled = coffee || silver || siamese || style == CatStyle.Wednesday;
        double gx=Limit(gazeX,-1,1), gy=Limit(gazeY,-1,1);
        double sleep = Limit(mood.Sleep, 0, 1);
        double crouch = Limit(mood.Crouch, 0, 1);
        double highLeft = Smooth((195 - leftPaw.Y) / 45);
        double lean=Smooth((157-leftPaw.X)/50) * (1 - sleep) * (1 - highLeft);
        double breath=Math.Sin(t*(sleep > 0.5 ? 1.15 : 1.7))*(sleep > 0.5 ? 1.7 : 0.72);
        double angerBreath = 0.5 + 0.5 * Math.Sin(t * 6.8);
        if (styled) TailStyled(dc,t,style,mood); else TailMood(dc,t,ginger,mood);

        double syScale = 1.0 - 0.45 * sleep - 0.10 * crouch;
        double sxScale = 1.0 + 0.04 * sleep + 0.04 * crouch;
        Brush bodyBrush = siamese ? Siamese.BodyFur : silver ? SilverShaded.BodyFur : coffee ? Coffee.BodyFur : ginger ? Ginger.BodyFur : Fur;

        // Keep the hips planted while the chest follows the outstretched mouse paw, and squash cleanly for sleep/crouch.
        dc.PushTransform(new MatrixTransform(1,0,lean*0.282,1, -233*lean*0.282,0));
        dc.PushTransform(new TranslateTransform(214, 235));
        dc.PushTransform(new ScaleTransform(sxScale, syScale));
        dc.PushTransform(new TranslateTransform(-214, -235));
        // Forelegs overlap one solid torso at every reach, including two-key chords.
        dc.DrawGeometry(bodyBrush, null, Body);
        if (ginger) Ginger.DrawBodyMarkings(dc, Body);
        else if (coffee) Coffee.DrawBodyMarkings(dc, Body);
        else if (silver) SilverShaded.DrawBodyMarkings(dc, Body);
        else if (siamese) Siamese.DrawBodyMarkings(dc, Body);
        dc.Pop();
        dc.Pop();
        dc.Pop();
        dc.Pop();

        // Rounded loaf back dome during sleep removes any flat top-edge discontinuity.
        if (sleep > 0.1)
        {
            dc.PushOpacity(Smooth((sleep - 0.1) / 0.5));
            dc.DrawEllipse(bodyBrush, null, new Point(214, 198), 53.0, 36.0);
            dc.Pop();
        }

        // A caller-supplied layer goes here, between the torso and the
        // forelegs: the gamepad sits in front of the chest and under the
        // paws that hold it. Drawing it before DrawStyled would leave it
        // hidden behind the opaque body.
        if (between != null) between(dc);
        if (styled)
        {
            ArmStyled(dc,false,rightPaw,lean,rightActivity,style,pawRaised,sxScale,syScale);
            ArmStyled(dc,true,leftPaw,lean,leftActivity,style,pawRaised,sxScale,syScale);
        }
        else
        {
            Arm(dc,false,rightPaw,lean,rightActivity,ginger,pawRaised,sxScale,syScale);
            Arm(dc,true,leftPaw,lean,leftActivity,ginger,pawRaised,sxScale,syScale);
        }

        double hx = 214 - 36.8*lean + gx*3.2*(1 - sleep) - 2.0*sleep;
        double hyNorm = 86 + breath + gy*2.0 + lean*1.4 + 12.0*crouch;
        double hy = hyNorm + (180 + breath - hyNorm) * sleep;
        double hrot = (gx*2.8 - lean*2.4)*(1 - sleep) - 8.5*sleep;
        dc.PushTransform(new TranslateTransform(hx, hy));
        dc.PushTransform(new RotateTransform(hrot));
        if (styled) DrawHeadStyled(dc, style, gx, gy, blink, mood.Anger, angerBreath, crouch);
        else DrawHeadMood(dc, ginger, gx, gy, blink, mood.Anger, angerBreath, crouch);
        if (mood.Sweat > .01) DrawSweat(dc, t, mood.Sweat);
        if (mood.Anger > .01) DrawAnger(dc, t, mood.Anger);
        dc.Pop();
        dc.Pop();
    }

    // Head-space bounds including whiskers are approximately (-57,-64) to (57,35).
    public static void DrawPortrait(DrawingContext dc, CatStyle style)
    {
        if (style == CatStyle.PhotoTabby) TabbyArt.DrawPortrait(dc);
        else if (style == CatStyle.JapaneseBobtail) BobtailArt.DrawPortrait(dc);
        else if (style == CatStyle.Coffee || style == CatStyle.Wednesday || style == CatStyle.SilverShaded || style == CatStyle.Siamese) DrawHeadStyled(dc, style, 0, 0, 0);
        else DrawHead(dc, style == CatStyle.Ginger, 0, 0, 0);
    }

    public static bool ContainsHead(CatStyle style, Point pointer, double time,
                                    double gazeX, double gazeY, Point leftPaw)
    {
        switch (style)
        {
            case CatStyle.PhotoTabby:
                return TabbyArt.ContainsHead(pointer,time,gazeX,gazeY,leftPaw);
            case CatStyle.JapaneseBobtail:
                return BobtailArt.ContainsHead(pointer,time,gazeX,gazeY,leftPaw);
            default:
                double lean = Smooth((157 - leftPaw.X) / 50);
                double hx = 214 - 36.8 * lean + Limit(gazeX, -1, 1) * 3.2;
                return Math.Pow((pointer.X - hx) / 63, 2) + Math.Pow((pointer.Y - 85) / 53, 2) < 1;
        }
    }

    private static void DrawHead(DrawingContext dc, bool ginger, double gx, double gy, double blink)
    {
        DrawHeadMood(dc, ginger, gx, gy, blink, 0, 0.5, 0);
    }

    private static void DrawHeadMood(DrawingContext dc, bool ginger, double gx, double gy, double blink,
                                     double anger, double angerBreath, double crouch)
    {
        dc.DrawGeometry(ginger ? Ginger.HeadFur : Face, null, Head);
        dc.DrawGeometry(ginger ? Ginger.InnerEar : Ear, null, EarLeft);
        dc.DrawGeometry(ginger ? Ginger.InnerEar : Ear, null, EarRight);
        if (ginger) Ginger.DrawFaceMarkings(dc, Head, gx);
        Eye(dc,-1,gx,gy,Limit(blink,0,1),ginger,anger,angerBreath,crouch);
        Eye(dc,1,gx,gy,Limit(blink,0,1),ginger,anger,angerBreath,crouch);
        Whiskers(dc,-1,gx,ginger);
        Whiskers(dc,1,gx,ginger);
    }

    // Style-aware variants for Wednesday, Coffee, SilverShaded and Siamese.
    private static void TailStyled(DrawingContext dc, double t, CatStyle style, CatMood mood)
    {
        double freq = 1.9 + mood.Anger * 3.0 + mood.Crouch * 2.4 - mood.Sleep * 1.2;
        double amp = 1.0 + mood.Anger * 0.65 + mood.Crouch * 0.75 - mood.Sleep * 0.65;
        double wave = Math.Sin(t * freq) * amp;
        double tip = Math.Sin(t * freq - 0.68) * amp;
        double droop = mood.Sleep * 24.0;
        StreamGeometry tail = BuildTailGeometry(wave, tip, droop);
        if (style == CatStyle.Coffee)
        {
            dc.DrawGeometry(Coffee.TailFur, null, tail);
            Coffee.DrawTailBands(dc, tail, wave, tip);
        }
        else if (style == CatStyle.SilverShaded)
        {
            dc.DrawGeometry(SilverShaded.TailFur, null, tail);
            SilverShaded.DrawTailBands(dc, tail, wave, tip);
        }
        else if (style == CatStyle.Siamese) dc.DrawGeometry(Siamese.TailFur, null, tail);
        else dc.DrawGeometry(Fur, null, tail);
    }

    private static void ArmStyled(DrawingContext dc, bool left, Point paw, double lean, double activity, CatStyle style,
                                  double pawRaised = -1, double sxScale = 1, double syScale = 1)
    {
        double side = left ? -1 : 1;
        double raised = pawRaised >= 0 ? pawRaised : Smooth((221 - paw.Y)/32);
        Point a = ShoulderPoint(left, lean, sxScale, syScale);
        Point b, c;
        ComputeArmControls(a, paw, side, raised, out b, out c);
        double pawRadius = 8.4 + Limit(activity,0,1)*1.0;
        StreamGeometry limb = BuildArmGeometry(a, b, c, paw, pawRadius);
        if (style == CatStyle.Coffee)
        {
            dc.DrawGeometry(Coffee.ForelegFur, null, limb);
            Coffee.DrawSock(dc, limb, paw, paw-c, pawRadius, activity);
            return;
        }
        if (style == CatStyle.SilverShaded)
        {
            dc.DrawGeometry(SilverShaded.ArmBrush(a, paw), null, limb);
            SilverShaded.DrawSock(dc, limb, paw, paw-c, pawRadius, activity);
            return;
        }
        if (style == CatStyle.Siamese)
        {
            Siamese.DrawArm(dc, limb, a, paw, pawRadius, activity);
            return;
        }
        dc.DrawGeometry(Foreleg, null, limb);
        // End the continuous foreleg with a soft mitten shape even at oblique key reaches.
        dc.DrawEllipse(Foreleg,null,paw,pawRadius+0.35,pawRadius*(0.94-0.06*activity));
        // A fixed raised value (gamepad pose) omits the arc under the paw.
        if (pawRaised < 0 && raised > 0.1)
        {
            dc.PushOpacity(raised * 0.85);
            StreamGeometry rim = new StreamGeometry();
            using (StreamGeometryContext g = rim.Open())
            {
                g.BeginFigure(new Point(paw.X+side*7.5,paw.Y+0.7),false,false);
                g.BezierTo(new Point(paw.X+side*7.1,paw.Y+6.2),new Point(paw.X+side*2.7,paw.Y+8.2),new Point(paw.X-side*2.3,paw.Y+7.7),true,true);
            }
            dc.DrawGeometry(null,PawLight,rim);
            dc.Pop();
        }
    }

    private static void EyeStyled(DrawingContext dc, double side, double gx, double gy, double blink, CatStyle style,
                                  double anger = 0, double angerBreath = 0.5, double crouch = 0)
    {
        bool coffee = style == CatStyle.Coffee;
        bool wednesday = style == CatStyle.Wednesday;
        bool silver = style == CatStyle.SilverShaded;
        bool siamese = style == CatStyle.Siamese;
        Pen lid = coffee ? Coffee.Lid : wednesday ? WednesdayLid : silver ? SilverShaded.Lid : siamese ? Siamese.Lid : Lid;
        Brush irisBrush = coffee ? Coffee.Iris : wednesday ? WednesdayIris : silver ? SilverShaded.Iris : siamese ? Siamese.Iris : Iris;
        Brush lidFur = coffee ? Coffee.LidFur : silver ? SilverShaded.LidFur : siamese ? Siamese.LidFur : BlackLidFur;
        Pen edge = coffee ? Coffee.EyeEdge : silver ? SilverShaded.EyeEdge : siamese ? Siamese.EyeEdge : null;
        double angle = side*3 + gx*4;
        double x = side*19.1 + gx*2.6;
        double y = -15.8 + gy*1.7 - side*gx*1.1;
        double width = 13.0 * (1 - Math.Max(0, side*gx)*0.095);
        double height = 12.5 * (1 - blink);
        dc.PushTransform(new RotateTransform(angle,x,y));
        if (height < 1.6)
        {
            StreamGeometry slit = new StreamGeometry();
            using (StreamGeometryContext g = slit.Open())
            {
                g.BeginFigure(new Point(x-width*0.86,y),false,false);
                g.QuadraticBezierTo(new Point(x,y+3.0),new Point(x+width*0.86,y-0.2),true,true);
            }
            dc.DrawGeometry(null,lid,slit);
        }
        else
        {
            EllipseGeometry iris = new EllipseGeometry(new Point(x,y),width,height);
            dc.DrawGeometry(irisBrush,edge,iris);
            dc.PushClip(iris);
            double boost = 1.0 + 0.16 * crouch;
            if (coffee) Coffee.DrawPupil(dc, x, y, gx, gy, crouch);
            else if (silver) SilverShaded.DrawPupil(dc, x, y, gx, gy, crouch);
            else dc.DrawEllipse(Pupil,null,new Point(x+gx*7.8,y+gy*6.7),(4.3+Math.Abs(gy)*0.8)*boost,8.1*boost);
            dc.Pop();
            if (anger > .01)
                DrawAngryEyelid(dc, x, y, width, height, side, lidFur, lid, anger, angerBreath);
        }
        dc.Pop();
    }

    private static void WhiskersStyled(DrawingContext dc, double side, double gx, CatStyle style)
    {
        bool coffee = style == CatStyle.Coffee;
        bool silver = style == CatStyle.SilverShaded;
        bool siamese = style == CatStyle.Siamese;
        Pen whiskerPen = coffee ? Coffee.Whisker : silver ? SilverShaded.Whisker : siamese ? Siamese.Whisker : Whisker;
        Pen rootPen = coffee ? Coffee.WhiskerRoot : silver ? SilverShaded.WhiskerRoot : siamese ? Siamese.WhiskerRoot : RootPen;
        double cheek = side*26 + gx*3;
        for (int k=0; k<3; ++k)
        {
            Point start = new Point(cheek+side*(0.8 - k*0.4),6 + k*5.0);
            Point end = new Point(cheek+side*(29-k*2.4), start.Y + (k-0.8)*5.6);
            StreamGeometry whisker = new StreamGeometry();
            using (StreamGeometryContext g = whisker.Open())
            {
                g.BeginFigure(start,false,false);
                g.QuadraticBezierTo(new Point((start.X+end.X)/2,start.Y-1.1+k*0.65),end,true,true);
            }
            dc.DrawGeometry(null,whiskerPen,whisker);
            dc.DrawLine(rootPen,start,new Point(start.X+side*(6.2-k*0.9),start.Y-0.45+k*0.65));
        }
    }

    private static void DrawHeadStyled(DrawingContext dc, CatStyle style, double gx, double gy, double blink,
                                       double anger = 0, double angerBreath = 0.5, double crouch = 0)
    {
        bool coffee = style == CatStyle.Coffee;
        bool silver = style == CatStyle.SilverShaded;
        bool siamese = style == CatStyle.Siamese;
        dc.DrawGeometry(siamese ? Siamese.HeadFur : coffee ? Coffee.HeadFur : silver ? SilverShaded.HeadFur : Face, null, Head);
        if (siamese) Siamese.DrawEarFur(dc, Head);
        else if (coffee) Coffee.DrawEarFur(dc, Head);
        dc.DrawGeometry(siamese ? Siamese.InnerEar : coffee ? Coffee.InnerEar : silver ? SilverShaded.InnerEar : Ear, null, EarLeft);
        dc.DrawGeometry(siamese ? Siamese.InnerEar : coffee ? Coffee.InnerEar : silver ? SilverShaded.InnerEar : Ear, null, EarRight);
        if (coffee) Coffee.DrawFaceMarkings(dc, Head, gx);
        else if (silver) SilverShaded.DrawFaceMarkings(dc, Head, gx);
        else if (siamese) Siamese.DrawFaceMarkings(dc, Head, gx);
        EyeStyled(dc,-1,gx,gy,Limit(blink,0,1),style,anger,angerBreath,crouch);
        EyeStyled(dc,1,gx,gy,Limit(blink,0,1),style,anger,angerBreath,crouch);
        WhiskersStyled(dc,-1,gx,style);
        WhiskersStyled(dc,1,gx,style);
    }
}
