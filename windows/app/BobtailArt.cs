using System;
using System.Windows;
using System.Windows.Media;

// Japanese bobtail artwork and pose geometry from the selected portrait.
internal static class BobtailArt
{
    private const double BodyWidth = .92;
    private const double BodyTop = 118;
    private static readonly Geometry Head = Shape("M -42,-21 C -45,-30 -47,-49 -46,-61 Q -45,-71 -40,-68 C -33,-65 -22,-50 -16,-44 Q -1,-47 13,-43 C 22,-50 32,-62 40,-65 Q 48,-70 48,-60 Q 49,-37 44,-21 C 49,-6 43,8 33,16 Q 21,25 10,31 Q 1,35 -11,31 C -19,27 -26,23 -33,19 C -46,11 -49,-4 -42,-21 Z");
    private static readonly Geometry Body = Shape("M -30,-4 C -34,18 -44,39 -49,64 C -56,88 -54,103 -49,110 C -49,120 -38,124 -30,116 C -14,119 13,119 32,115 C 42,123 53,117 50,106 C 56,95 56,81 52,63 C 47,40 36,13 30,-4 Z");
    private static readonly Geometry Tail = Shape("M -4,3 C -9,-7 -3,-16 5,-17 Q 7,-25 13,-22 Q 19,-27 20,-22 C 33,-25 37,-12 32,-8 Q 36,-2 30,0 Q 28,7 20,7 Q 8,12 -4,3 Z");
    private static readonly Geometry Eye = Shape("M -12,1.2 C -11,-4.8 -6.9,-9 0,-9 Q 8,-9.1 12,-3 C 12,3.1 8,8.5 1,9 Q -8,9.4 -12,1.2 Z");
    private static readonly Geometry ClosedEye = Shape("M -11,0 Q -1,6 12,-1");
    private static readonly Geometry Whiskers = Shape("M 23,12 Q 39,8 53,9 M 24,16 Q 37,15 50,18");
    private static readonly Pen WhiskerPen = Stroke("#A59A8E", .73);
    private static readonly Pen EyeEdge = Stroke("#635650", 1);
    private static readonly Pen Lid = Stroke("#635650", 2.2);
    private static readonly Brush Iris = Shade("#BD914E","#D8A34D");
    private static readonly Brush Pupil = Ink("#302C2B");
    private static readonly Brush Glint = Ink("#FFF9E9");
    private static readonly Brush Sleeve = Shade("#F6EDDC","#F6ECDA");
    private static readonly Brush Paw = Ink("#F4E8D6");
    private static readonly Pen Toe = Stroke("#EADDCB",.65);
    // The face, torso and tail have fixed markings. Freeze those layers once for animation.
    private static readonly DrawingGroup HeadCoat = Coat(Head,"#FAF1E1","#EFE4D3",CalicoFace);
    private static readonly DrawingGroup BodyCoat = Coat(Body,"#FAF1E1","#E5D8C7",CalicoChest);
    private static readonly DrawingGroup TailCoat = Coat(Tail,"#B7663E","#DBCCBA",CalicoTail);
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
        var brush = new LinearGradientBrush(ParseColor(light),ParseColor(dark),new Point(.24,0),new Point(.78,1));
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
            Fill(dc,"#DA938C","M -4.8,8.1 Q -3.5,6.4 0,7.3 Q 3.5,6.4 4.8,8.1 Q 5.4,9.7 1,12.3 Q 0,13.1 -1,12.4 Q -5.4,9.7 -4.8,8.1 Z");
            Line(dc,"#938278",1.05,"M 0,12.2 L 0,14.7 Q -3.0,18.2 -6.4,15.6 M 0,14.7 Q 3.1,17.8 6.4,15.4");
        }
        drawing.Freeze();
        return drawing;
    }

    private static void CalicoFace(DrawingContext dc)
    {
        // A jagged white blaze divides the apricot and charcoal masks and reaches the nose.
        Wash(dc,"#E8A65F","#CA804A","M -54,-78 L -13,-74 L -4,-46 Q -6,-35 -11,-30 L -10,-34 Q -14,-24 -8,-18 Q -5,-13 -8,-6 Q -12,0 -19,-1 Q -24,-4 -34,0 L -46,5 L -54,-22 Z");
        Wash(dc,"#484249","#34343A","M 2,-48 Q 9,-47 19,-50 L 51,-77 L 57,-25 L 53,15 Q 40,22 30,16 L 31,13 Q 22,14 19,6 Q 16,3 11,4 Q 8,-1 8,-9 Q 8,-21 13,-28 L 10,-26 Q 12,-35 7,-39 L 5,-31 Q 3,-40 2,-48 Z");
        Wash(dc,"#E5A696","#F1C3A9","M -42,-61 Q -40,-63 -23,-45 Q -31,-46 -39,-36 Q -42,-47 -42,-61 Z");
        Wash(dc,"#9B7775","#C48F8B","M 23,-44 Q 35,-58 43,-61 Q 45,-47 40,-37 Q 32,-42 23,-44 Z");
        Fill(dc,"#B77444","M -30,-33 Q -23,-37 -16,-39 Q -21,-35 -22,-31 Z M -32,-28 Q -26,-32 -21,-31 L -27,-27 Z");
        Fill(dc,"#595158","M 29,-31 Q 37,-35 43,-35 L 40,-31 Q 32,-30 29,-27 Z");
        Wash(dc,"#F3E8D8","#F9EDDD","M -20,8 Q -12,4 -5,6 L 0,8 L 5,6 Q 14,3 22,11 Q 19,19 10,21 Q 4,27 -1,26 Q -14,24 -20,17 Z");
        Oval(dc,"#BC9686",-11,13,.75,.6); Oval(dc,"#BC9686",-14,15,.7,.55);
        Oval(dc,"#BDA097",12,14,.75,.55);
    }
    private static void CalicoChest(DrawingContext dc)
    {
        Wash(dc,"#45434A","#38363A","M 23,-10 Q 44,-8 53,18 L 58,66 Q 36,64 29,51 Q 22,37 26,25 Q 31,13 23,-10 Z");
        Wash(dc,"#E2A15F","#CB824F","M 29,32 Q 41,29 53,40 L 58,72 Q 53,78 44,75 Q 32,68 29,63 Q 32,54 27,50 Q 24,39 29,32 Z");
        Wash(dc,"#D99B5E","#C7804C","M -55,72 Q -43,66 -37,71 Q -28,77 -31,83 Q -26,94 -35,99 Q -41,104 -52,99 Z");
        Fill(dc,"#444046","M -54,95 Q -43,90 -36,96 Q -26,104 -34,115 L -56,117 Z");
        Wash(dc,"#FFFAEE","#EEE0CF","M -21,-4 Q -3,4 16,-4 Q 19,11 13,24 Q 10,25 7,31 Q -4,42 -3,44 Q -8,34 -15,31 Q -23,20 -21,-4 Z");
    }

    private static void CalicoTail(DrawingContext dc)
    {
        Wash(dc,"#4E4446","#37353B","M -11,-13 Q -6,-17 1,-15 Q 8,-13 12,-7 L 10,-8 Q 13,-3 22,1 L 19,2 L 24,7 L 10,12 L -14,10 Z");
        Line(dc,"#E3A168",1.2,"M 14,-18 Q 18,-18 20,-15 M 22,-17 Q 26,-16 28,-13");
    }

    private static void DrawEye(DrawingContext dc, int side, double gazeX, double gazeY, double blink, double crouch = 0, double happyPet = 0)
    {
        dc.PushTransform(new TranslateTransform(side*19.8,-8));
        dc.PushTransform(new ScaleTransform(side*10.2/12,8.8/9));
        if (happyPet > 0.32 && blink < 0.85)
        {
            CatArt.DrawHappyEye(dc, 0, 0, 11.2, Lid);
        }
        else if (blink>.92)
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

    private static readonly Brush LeftLidFur = Ink("#DC9656");
    private static readonly Brush RightLidFur = Ink("#3F3B42");

    private static void DrawHead(DrawingContext dc, double gazeX, double gazeY, double blink,
                                 double anger = 0, double angerBreath = 0.5, double crouch = 0, double happyPet = 0)
    {
        dc.DrawDrawing(HeadCoat);
        DrawEye(dc,-1,gazeX,gazeY,blink,crouch,happyPet);
        DrawEye(dc,1,gazeX,gazeY,blink,crouch,happyPet);
        if (anger > .01 && blink <= .92 && happyPet <= 0.32)
        {
            CatArt.DrawAngryEyelid(dc, -19.8, -8.0, 10.6, 8.8, -1, LeftLidFur, Lid, anger, angerBreath);
            CatArt.DrawAngryEyelid(dc, 19.8, -8.0, 10.6, 8.8, 1, RightLidFur, Lid, anger, angerBreath);
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
        dc.PushTransform(new RotateTransform(3.2));
        dc.PushTransform(new ScaleTransform(.95,.99));
        DrawHead(dc,0,0,0);
        dc.Pop();
        dc.Pop();
    }

    private static void DrawArm(DrawingContext dc, Point shoulder, Point paw, int side, double activity, double pawRaised)
    {
        double bend = pawRaised >= 0 ? pawRaised : Math.Max(0,Math.Min(1,(228-paw.Y)/44));
        double high = Math.Max(0, Math.Min(1, (185.0 - paw.Y) / 55.0));
        high = high * high * (3 - 2 * high);
        Vector span = paw-shoulder;
        Point nearLow = new Point(shoulder.X+span.X*.18+side*(3+7*bend),shoulder.Y+10+bend*9);
        Point farLow = new Point(paw.X-span.X*.20+side*(2+12*bend),paw.Y-16+31*bend);
        Point nearHigh = new Point(shoulder.X+span.X*.32+side*6,shoulder.Y+span.Y*.28+12);
        Point farHigh = new Point(shoulder.X+span.X*.72+side*4,shoulder.Y+span.Y*.72+8);
        Point near = new Point(nearLow.X + (nearHigh.X - nearLow.X) * high, nearLow.Y + (nearHigh.Y - nearLow.Y) * high);
        Point far = new Point(farLow.X + (farHigh.X - farLow.X) * high, farLow.Y + (farHigh.Y - farLow.Y) * high);
        const int steps = 24;
        var opposite = new Point[steps+1];
        var arm = new StreamGeometry { FillRule=FillRule.Nonzero };
        using (StreamGeometryContext path=arm.Open())
        {
            for (int i=0; i<=steps; i++)
            {
                double u=i/(double)steps,v=1-u;
                Point center=new Point(v*v*v*shoulder.X+3*v*v*u*near.X+3*v*u*u*far.X+u*u*u*paw.X,
                                       v*v*v*shoulder.Y+3*v*v*u*near.Y+3*v*u*u*far.Y+u*u*u*paw.Y);
                Vector tangent=(near-shoulder)*(3*v*v)+(far-near)*(6*v*u)+(paw-far)*(3*u*u);
                if (tangent.LengthSquared<.01) tangent=new Vector(0,1);
                tangent.Normalize();
                double baseR=12*BodyWidth*(1-u)+8.1*u;
                double k=Math.Min(1,u/.14);
                double dome=.22+.78*Math.Sqrt(Math.Max(0,1-(1-k)*(1-k)));
                double radius=baseR*dome;
                Vector normal=new Vector(-tangent.Y,tangent.X)*radius;
                if (i==0) path.BeginFigure(center+normal,true,true);
                else path.LineTo(center+normal,true,true);
                opposite[i]=center-normal;
            }
            for (int i=steps; i>=0; i--) path.LineTo(opposite[i],true,true);
        }
        Brush blendedSleeve = CatArt.ArmGradient(
            Color.FromRgb(242, 231, 214), Color.FromRgb(246, 237, 220), 0.35, Color.FromRgb(246, 236, 218), shoulder, paw);
        dc.DrawGeometry(blendedSleeve,null,arm);
        dc.DrawEllipse(Paw,null,paw,8.65,7.4);
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
        pose.Scale(.95,.99);
        pose.Rotate(3.2+2.8*gazeX-2.4*lean);
        pose.Translate(214-32.8*lean+2.5*gazeX,BodyTop-2+2*gazeY+Math.Sin(t*1.7)*.6);
        return pose;
    }

    public static bool ContainsHead(Point pointer, double time, double gazeX, double gazeY, Point leftPaw)
    {
        return ContainsCat(pointer, time, gazeX, gazeY, leftPaw, 0) == PetZone.Head;
    }

    public static PetZone ContainsCat(Point pointer, double time, double gazeX, double gazeY, Point leftPaw, double sleep)
    {
        double s = Math.Max(0, Math.Min(1, sleep));
        double lean = Lean(leftPaw) * (1 - s);
        if (s < 0.3)
        {
            Matrix pose = HeadPose(time, gazeX, gazeY, lean);
            pose.Invert();
            if (Head.FillContains(pose.Transform(pointer))) return PetZone.Head;
        }
        else
        {
            double hx = 214 - 32.8 * lean - 2.0 * s;
            double hy = BodyTop - 2 + (186 - (BodyTop - 2)) * s;
            if (Math.Pow((pointer.X - hx) / 56.0, 2) + Math.Pow((pointer.Y - hy) / 48.0, 2) < 1.0)
                return PetZone.Head;
        }
        double bx = 214 - 14.0 * lean;
        double by = 176 + 24.0 * s;
        if (Math.Pow((pointer.X - bx) / 58.0, 2) + Math.Pow((pointer.Y - by) / (64.0 - 20.0 * s), 2) < 1.0)
            return PetZone.Body;
        return PetZone.None;
    }

    public static void Draw(DrawingContext dc, double time, double gazeX, double gazeY, double blink,
                            Point leftPaw, Point rightPaw, double leftActivity, double rightActivity,
                            Action<DrawingContext> between = null, double pawRaised = -1, CatMood mood = default(CatMood))
    {
        double sleep = Math.Max(0, Math.Min(1, mood.Sleep));
        double lift = Math.Max(0, Math.Min(1, mood.ScruffLift));
        double crouch = Math.Max(0, Math.Min(1, mood.Crouch)) * (1 - lift);
        double happy = Math.Max(0, Math.Min(1, mood.HappyPet)) * (1 - sleep) * (1 - lift);
        double impatient = Math.Max(0, Math.Min(1, mood.Impatient)) * (1 - sleep) * (1 - lift);
        double lean=Lean(leftPaw) * (1 - sleep) * (1 - lift);
        double syScale = 1.0 - 0.45 * sleep - 0.10 * crouch + 0.025 * happy * Math.Sin(time * 5.0) + 0.07 * lift;
        double sxScale = 1.0 + 0.04 * sleep + 0.04 * crouch + 0.02 * happy - 0.06 * lift;
        double bodyYShift = (1.0 - syScale) * (235.0 - BodyTop);
        double angerBreath = 0.5 + 0.5 * Math.Sin(time * 6.8);

        double tailSpeed = 1.8 + mood.Anger * 3.0 + impatient * 3.4 + crouch * 2.5 - sleep * 1.0 + happy * 0.45;
        double tailAmp = 3.0 + mood.Anger * 3.0 + impatient * 3.2 + crouch * 3.5 + happy * 1.0;
        dc.PushTransform(new TranslateTransform(249,207 + 8 * sleep + 6 * lift));
        dc.PushTransform(new RotateTransform(Math.Sin(time*tailSpeed)*tailAmp));
        dc.DrawDrawing(TailCoat);
        dc.Pop();
        dc.Pop();

        dc.PushTransform(new MatrixTransform(1,0,.285*lean,1,-233*.285*lean,0));
        dc.PushTransform(new TranslateTransform(214,BodyTop + bodyYShift));
        dc.PushTransform(new ScaleTransform(BodyWidth * sxScale, syScale));
        dc.DrawDrawing(BodyCoat);
        dc.Pop();
        dc.Pop();
        dc.Pop();
        // Same layering as the base cat: a caller-supplied layer (the
        // gamepad or hugged keyboard & mouse) goes between the torso and the forelegs.
        if (between != null) between(dc);
        double rSy = BodyTop + bodyYShift + 17.0 * syScale;
        double lSy = BodyTop + bodyYShift + (17.0 - 5.0 * lean) * syScale;
        double rShear = (rSy - 233.0) * 0.285 * lean;
        double lShear = (lSy - 233.0) * 0.285 * lean;
        DrawArm(dc,new Point(214+22*BodyWidth*sxScale+rShear, rSy),rightPaw,1,rightActivity,pawRaised);
        DrawArm(dc,new Point(214-(22*BodyWidth*sxScale+2.5*lean)+lShear, lSy),leftPaw,-1,leftActivity,pawRaised);

        double breathFreq = sleep > 0.5 ? 1.15 : 1.7;
        double breathAmp = sleep > 0.5 ? 1.6 : 0.6;
        double nuzzleX = mood.PetNuzzleX * 4.5 * happy;
        double nuzzleY = mood.PetNuzzleY * 2.5 * happy - 1.5 * happy + 2.2 * impatient;
        double shakeX = impatient > 0.02 ? Math.Sin(time * 26.0) * 1.9 * impatient : 0;
        double hx = 214 - 32.8*lean + 2.5*gazeX*(1 - sleep) - 2.0*sleep + nuzzleX + shakeX;
        double hyNorm = BodyTop - 2 + 2*gazeY + Math.Sin(time*breathFreq)*breathAmp + 12.0*crouch + nuzzleY + 4.0*lift;
        double hy = hyNorm + (186 + Math.Sin(time*1.15)*1.8 - hyNorm)*sleep;
        double rot = (3.2 + 2.8*gazeX - 2.4*lean + mood.PetNuzzleX * 6.5 * happy + Math.Sin(time * 5.2) * 2.6 * happy + Math.Sin(time * 22.0) * 2.8 * impatient)*(1 - sleep)*(1 - lift) - 8.5*sleep - mood.DragSwing * 0.18 * lift;
        double browAnger = Math.Max(mood.Anger, impatient * 0.92) * (1 - lift);
        double eyeBoost = Math.Max(crouch, lift * 0.72);
        dc.PushTransform(new TranslateTransform(hx, hy));
        dc.PushTransform(new RotateTransform(rot));
        dc.PushTransform(new ScaleTransform(.95 * (1.0 + 0.02 * lift), .99 * (1.0 - 0.03 * lift)));
        if (lift > .02) CatArt.DrawScruffPinch(dc, Paw, lift);
        DrawHead(dc,gazeX,gazeY,blink,browAnger,angerBreath,eyeBoost,happy);
        double blushAmt = Math.Max(happy, lift * 0.42);
        if (blushAmt > .05) CatArt.DrawBlush(dc, blushAmt);
        if (mood.Sweat > .01) CatArt.DrawSweat(dc, time, mood.Sweat);
        if (mood.Anger > .01 && lift < .5) CatArt.DrawAnger(dc, time, mood.Anger);
        if (impatient > .05) CatArt.DrawImpatientMark(dc, time, impatient);
        dc.Pop();
        dc.Pop();
        dc.Pop();
    }
}
