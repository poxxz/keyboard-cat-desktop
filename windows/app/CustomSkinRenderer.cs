using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class CustomSkinRenderer
{
    public static void Draw(DrawingContext dc, CustomSkin skin, double t, double gazeX, double gazeY,
                            double blink, Point leftPaw, Point rightPaw, double leftActivity, double rightActivity,
                            double pawRaised, Action<DrawingContext> between, CatMood mood)
    {
        if (skin == null)
        {
            CatArt.DrawStyled(dc, CatStyle.Black, t, gazeX, gazeY, blink, leftPaw, rightPaw,
                leftActivity, rightActivity, pawRaised, between, mood);
            return;
        }
        CatArt.DrawCustomSkin(dc, skin, t, gazeX, gazeY, blink, leftPaw, rightPaw,
            leftActivity, rightActivity, pawRaised, between, mood);
    }

    public static void DrawPortrait(DrawingContext dc, CustomSkin skin)
    {
        if (skin == null)
        {
            CatArt.DrawPortrait(dc, CatStyle.Black);
            return;
        }
        CatArt.DrawCustomPortrait(dc, skin);
    }

    public static bool ContainsHead(CustomSkin skin, Point pointer, double time,
                                    double gazeX, double gazeY, Point leftPaw)
    {
        if (skin == null) return CatArt.ContainsHead(CatStyle.Black, pointer, time, gazeX, gazeY, leftPaw);
        return CatArt.ContainsCustomHead(skin, pointer, time, gazeX, gazeY, leftPaw);
    }

    public static void DrawSlotGuideOverlay(DrawingContext dc, CustomSkin skin, SkinSlot slot, double t,
                                            double gazeX, double gazeY, Point leftPaw, Point rightPaw, CatMood mood)
    {
        if (skin == null) return;
        CatArt.DrawCustomSlotGuide(dc, skin, slot, t, gazeX, gazeY, leftPaw, rightPaw, mood);
    }
}

public static partial class CatArt
{
    private const double HeadRefW = 112.0;
    private const double HeadRefH = 112.0;
    private const double HeadRefY = -14.0;
    private const double BodyRefW = 120.0;
    private const double BodyRefH = 140.0;
    private const double BodyRefX = 214.0;
    private const double BodyRefY = 171.0;
    private const double TailRefW = 96.0;
    private const double TailRefH = 72.0;
    private const double TailPivotX = 251.0;
    private const double TailPivotY = 206.0;
    private const double PawTipRefSize = 32.0;
    private const double FullArmRefW = 42.0;
    private const double FullArmRefH = 96.0;
    private const double AccRefW = 96.0;
    private const double AccRefH = 96.0;
    private const double AccRefY = -54.0;

    private static readonly Pen GuideBoxPen = BuildGuidePen();
    private static readonly Pen GuideCrossPen = Stroke(AlphaBrushRgb(220, 235, 148, 24), 1.2);
    private static readonly Brush GuideFillBrush = AlphaBrushRgb(24, 242, 160, 24);

    private static Brush AlphaBrushRgb(byte a, byte r, byte g, byte b)
    {
        SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }

    private static Pen BuildGuidePen()
    {
        Pen p = new Pen(AlphaBrushRgb(215, 232, 142, 20), 1.3);
        p.DashStyle = new DashStyle(new double[] { 4, 3 }, 0);
        p.Freeze();
        return p;
    }

    internal static void DrawCustomSkin(DrawingContext dc, CustomSkin skin, double t, double gazeX, double gazeY,
                                        double blink, Point leftPaw, Point rightPaw, double leftActivity, double rightActivity,
                                        double pawRaised, Action<DrawingContext> between, CatMood mood)
    {
        CatStyle baseCat = skin.BaseCat;
        bool isTabby = baseCat == CatStyle.PhotoTabby;
        bool isBobtail = baseCat == CatStyle.JapaneseBobtail;
        double gx = Limit(gazeX, -1, 1);
        double gy = Limit(gazeY, -1, 1);
        double sleep = Limit(mood.Sleep, 0, 1);
        double crouch = Limit(mood.Crouch, 0, 1);
        double highLeft = Smooth((195.0 - leftPaw.Y) / 45.0);
        double lean = Smooth((157.0 - leftPaw.X) / 50.0) * (1.0 - sleep) * (1.0 - highLeft);
        double syScale = 1.0 - 0.45 * sleep - 0.10 * crouch;
        double sxScale = 1.0 + 0.04 * sleep + 0.04 * crouch;
        double angerBreath = 0.5 + 0.5 * Math.Sin(t * 6.8);

        // 1. Tail layer
        BitmapSource tailImg = skin.GetImage(SkinSlot.Tail);
        if (tailImg != null)
        {
            DrawCustomTailBitmap(dc, skin, tailImg, t, mood);
        }
        else
        {
            DrawBaseTail(dc, baseCat, t, mood);
        }

        // 2. Body layer
        BitmapSource bodyImg = skin.GetImage(SkinSlot.Body);
        if (bodyImg != null)
        {
            dc.PushTransform(new MatrixTransform(1, 0, lean * 0.282, 1, -233.0 * lean * 0.282, 0));
            dc.PushTransform(new TranslateTransform(214, 235));
            dc.PushTransform(new ScaleTransform(sxScale, syScale));
            dc.PushTransform(new TranslateTransform(-214, -235));
            DrawLayerBitmap(dc, bodyImg, skin.GetLayer(SkinSlot.Body), BodyRefX, BodyRefY, BodyRefW, BodyRefH, skin.PixelArtFilter);
            dc.Pop();
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
        else
        {
            DrawBaseBody(dc, baseCat, lean, sxScale, syScale, sleep);
        }

        // 3. Between layer (e.g. Gamepad sits in front of body and behind paws)
        if (between != null) between(dc);

        // 4. Forelegs / Paws layer
        DrawCustomArm(dc, skin, false, rightPaw, lean, rightActivity, pawRaised, sxScale, syScale);
        DrawCustomArm(dc, skin, true, leftPaw, lean, leftActivity, pawRaised, sxScale, syScale);

        // Tabby front ruff sits between arms and head when using Tabby base body without custom body PNG
        if (isTabby && bodyImg == null)
        {
            TabbyArt.DrawRuffPart(dc, lean, sxScale, syScale, sleep);
        }

        // 5. Head + Eyes + Whiskers + Accessory + Mood Effects
        double hx, hy, hrot;
        ComputeCustomHeadPose(baseCat, t, gx, gy, lean, sleep, crouch, out hx, out hy, out hrot);

        dc.PushTransform(new TranslateTransform(hx, hy));
        dc.PushTransform(new RotateTransform(hrot));

        DrawCustomHeadInner(dc, skin, gx, gy, Limit(blink, 0, 1), mood.Anger, angerBreath, crouch, isTabby, isBobtail);

        if (skin.ShowMoodEffects)
        {
            if (mood.Sweat > .01) DrawSweat(dc, t, mood.Sweat);
            if (mood.Anger > .01) DrawAnger(dc, t, mood.Anger);
        }

        dc.Pop();
        dc.Pop();
    }

    internal static void DrawCustomPortrait(DrawingContext dc, CustomSkin skin)
    {
        bool isTabby = skin.BaseCat == CatStyle.PhotoTabby;
        bool isBobtail = skin.BaseCat == CatStyle.JapaneseBobtail;
        if (!skin.HasImage(SkinSlot.Head))
        {
            if (isTabby)
            {
                dc.PushTransform(new RotateTransform(-2.25));
                TabbyArt.DrawHeadPart(dc, 0, 0, 0, 0, 0.5, 0);
                DrawAccessoryIfPresent(dc, skin);
                dc.Pop();
                return;
            }
            if (isBobtail)
            {
                dc.PushTransform(new RotateTransform(3.2));
                BobtailArt.DrawHeadPart(dc, 0, 0, 0, 0, 0.5, 0);
                DrawAccessoryIfPresent(dc, skin);
                dc.Pop();
                return;
            }
        }
        DrawCustomHeadInner(dc, skin, 0, 0, 0, 0, 0.5, 0, isTabby, isBobtail);
    }

    internal static bool ContainsCustomHead(CustomSkin skin, Point pointer, double time,
                                            double gazeX, double gazeY, Point leftPaw)
    {
        if (!skin.HasImage(SkinSlot.Head))
        {
            return ContainsHead(skin.BaseCat, pointer, time, gazeX, gazeY, leftPaw);
        }
        double gx = Limit(gazeX, -1, 1);
        double lean = Smooth((157.0 - leftPaw.X) / 50.0);
        SkinLayerConfig headLayer = skin.GetLayer(SkinSlot.Head);
        double hx = 214.0 - 36.8 * lean + gx * 3.2 + headLayer.OffsetX;
        double hy = 86.0 + HeadRefY + headLayer.OffsetY;
        double rx = 58.0 * Math.Max(0.5, headLayer.Scale);
        double ry = 54.0 * Math.Max(0.5, headLayer.Scale);
        return Math.Pow((pointer.X - hx) / rx, 2) + Math.Pow((pointer.Y - hy) / ry, 2) < 1.0;
    }

    internal static void DrawCustomSlotGuide(DrawingContext dc, CustomSkin skin, SkinSlot slot, double t,
                                             double gazeX, double gazeY, Point leftPaw, Point rightPaw, CatMood mood)
    {
        double gx = Limit(gazeX, -1, 1);
        double gy = Limit(gazeY, -1, 1);
        double sleep = Limit(mood.Sleep, 0, 1);
        double crouch = Limit(mood.Crouch, 0, 1);
        double highLeft = Smooth((195.0 - leftPaw.Y) / 45.0);
        double lean = Smooth((157.0 - leftPaw.X) / 50.0) * (1.0 - sleep) * (1.0 - highLeft);
        double syScale = 1.0 - 0.45 * sleep - 0.10 * crouch;
        double sxScale = 1.0 + 0.04 * sleep + 0.04 * crouch;
        SkinLayerConfig layer = skin.GetLayer(slot);
        BitmapSource bmp = skin.GetImage(slot);

        switch (slot)
        {
            case SkinSlot.Head:
            case SkinSlot.HeadBlink:
            case SkinSlot.HeadAngry:
            {
                double hx, hy, hrot;
                ComputeCustomHeadPose(skin.BaseCat, t, gx, gy, lean, sleep, crouch, out hx, out hy, out hrot);
                dc.PushTransform(new TranslateTransform(hx, hy));
                dc.PushTransform(new RotateTransform(hrot));
                DrawLayerGuideBox(dc, bmp, layer, 0, HeadRefY, HeadRefW, HeadRefH);
                dc.Pop();
                dc.Pop();
                break;
            }
            case SkinSlot.Accessory:
            {
                double hx, hy, hrot;
                ComputeCustomHeadPose(skin.BaseCat, t, gx, gy, lean, sleep, crouch, out hx, out hy, out hrot);
                dc.PushTransform(new TranslateTransform(hx, hy));
                dc.PushTransform(new RotateTransform(hrot));
                DrawLayerGuideBox(dc, bmp, layer, 0, AccRefY, AccRefW, AccRefH);
                dc.Pop();
                dc.Pop();
                break;
            }
            case SkinSlot.Body:
            {
                dc.PushTransform(new MatrixTransform(1, 0, lean * 0.282, 1, -233.0 * lean * 0.282, 0));
                dc.PushTransform(new TranslateTransform(214, 235));
                dc.PushTransform(new ScaleTransform(sxScale, syScale));
                dc.PushTransform(new TranslateTransform(-214, -235));
                DrawLayerGuideBox(dc, bmp, layer, BodyRefX, BodyRefY, BodyRefW, BodyRefH);
                dc.Pop();
                dc.Pop();
                dc.Pop();
                dc.Pop();
                break;
            }
            case SkinSlot.Tail:
            {
                double freq = 1.9 + mood.Anger * 3.0 + crouch * 2.4 - sleep * 1.2;
                double amp = 6.5 + mood.Anger * 5.5 + crouch * 6.0 - sleep * 4.5;
                double rot = Math.Sin(t * freq) * amp + sleep * 22.0;
                dc.PushTransform(new TranslateTransform(TailPivotX, TailPivotY + sleep * 10.0));
                dc.PushTransform(new RotateTransform(rot));
                DrawLayerGuideBox(dc, bmp, layer, 0, 0, TailRefW, TailRefH);
                dc.Pop();
                dc.Pop();
                break;
            }
            case SkinSlot.PawLeft:
            case SkinSlot.PawRight:
            {
                bool isLeft = slot == SkinSlot.PawLeft;
                Point paw = isLeft ? leftPaw : rightPaw;
                if (skin.ArmMode == CustomArmMode.FullArmSprite)
                {
                    Point shoulder = GetCustomShoulder(skin.BaseCat, isLeft, lean, sxScale, syScale);
                    Vector diff = paw - shoulder;
                    double angle = Math.Atan2(diff.Y, diff.X) * 180.0 / Math.PI - 90.0;
                    double stretchY = Math.Max(0.35, diff.Length / 92.0);
                    SkinLayerConfig guideLayer = layer.Clone();
                    guideLayer.PivotY = 0.12;
                    dc.PushTransform(new TranslateTransform(shoulder.X, shoulder.Y));
                    dc.PushTransform(new RotateTransform(angle));
                    DrawLayerGuideBox(dc, bmp, guideLayer, 0, 0, FullArmRefW, FullArmRefH * stretchY);
                    dc.Pop();
                    dc.Pop();
                }
                else
                {
                    DrawLayerGuideBox(dc, bmp, layer, paw.X, paw.Y, PawTipRefSize, PawTipRefSize);
                }
                break;
            }
        }
    }

    private static void ComputeCustomHeadPose(CatStyle baseCat, double t, double gx, double gy, double lean,
                                              double sleep, double crouch, out double hx, out double hy, out double hrot)
    {
        if (baseCat == CatStyle.PhotoTabby)
        {
            TabbyArt.ComputeHeadPose(t, gx, gy, lean, sleep, crouch, out hx, out hy, out hrot);
            return;
        }
        if (baseCat == CatStyle.JapaneseBobtail)
        {
            BobtailArt.ComputeHeadPose(t, gx, gy, lean, sleep, crouch, out hx, out hy, out hrot);
            return;
        }
        double breath = Math.Sin(t * (sleep > 0.5 ? 1.15 : 1.7)) * (sleep > 0.5 ? 1.7 : 0.72);
        hx = 214.0 - 36.8 * lean + gx * 3.2 * (1.0 - sleep) - 2.0 * sleep;
        double hyNorm = 86.0 + breath + gy * 2.0 + lean * 1.4 + 12.0 * crouch;
        hy = hyNorm + (180.0 + breath - hyNorm) * sleep;
        hrot = (gx * 2.8 - lean * 2.4) * (1.0 - sleep) - 8.5 * sleep;
    }

    private static Point GetCustomShoulder(CatStyle baseCat, bool left, double lean, double sxScale, double syScale)
    {
        if (baseCat == CatStyle.PhotoTabby) return TabbyArt.GetShoulderPoint(left, lean, sxScale, syScale);
        if (baseCat == CatStyle.JapaneseBobtail) return BobtailArt.GetShoulderPoint(left, lean, sxScale, syScale);
        return ShoulderPoint(left, lean, sxScale, syScale);
    }

    private static void DrawBaseTail(DrawingContext dc, CatStyle style, double t, CatMood mood)
    {
        if (style == CatStyle.PhotoTabby) { TabbyArt.DrawTailPart(dc, t, mood); return; }
        if (style == CatStyle.JapaneseBobtail) { BobtailArt.DrawTailPart(dc, t, mood); return; }
        bool ginger = style == CatStyle.Ginger;
        bool styled = style == CatStyle.Coffee || style == CatStyle.SilverShaded || style == CatStyle.Siamese || style == CatStyle.Wednesday;
        if (styled) TailStyled(dc, t, style, mood);
        else TailMood(dc, t, ginger, mood);
    }

    private static void DrawBaseBody(DrawingContext dc, CatStyle style, double lean, double sxScale, double syScale, double sleep)
    {
        if (style == CatStyle.PhotoTabby) { TabbyArt.DrawBodyPart(dc, lean, sxScale, syScale); return; }
        if (style == CatStyle.JapaneseBobtail) { BobtailArt.DrawBodyPart(dc, lean, sxScale, syScale); return; }

        bool ginger = style == CatStyle.Ginger;
        bool coffee = style == CatStyle.Coffee;
        bool silver = style == CatStyle.SilverShaded;
        bool siamese = style == CatStyle.Siamese;
        Brush bodyBrush = siamese ? Siamese.BodyFur : silver ? SilverShaded.BodyFur : coffee ? Coffee.BodyFur : ginger ? Ginger.BodyFur : Fur;

        dc.PushTransform(new MatrixTransform(1, 0, lean * 0.282, 1, -233.0 * lean * 0.282, 0));
        dc.PushTransform(new TranslateTransform(214, 235));
        dc.PushTransform(new ScaleTransform(sxScale, syScale));
        dc.PushTransform(new TranslateTransform(-214, -235));
        dc.DrawGeometry(bodyBrush, null, Body);
        if (ginger) Ginger.DrawBodyMarkings(dc, Body);
        else if (coffee) Coffee.DrawBodyMarkings(dc, Body);
        else if (silver) SilverShaded.DrawBodyMarkings(dc, Body);
        else if (siamese) Siamese.DrawBodyMarkings(dc, Body);
        dc.Pop();
        dc.Pop();
        dc.Pop();
        dc.Pop();

        if (sleep > 0.1)
        {
            dc.PushOpacity(Smooth((sleep - 0.1) / 0.5));
            dc.DrawEllipse(bodyBrush, null, new Point(214, 198), 53.0, 36.0);
            dc.Pop();
        }
    }

    private static void DrawCustomTailBitmap(DrawingContext dc, CustomSkin skin, BitmapSource tailImg, double t, CatMood mood)
    {
        double sleep = Limit(mood.Sleep, 0, 1);
        double crouch = Limit(mood.Crouch, 0, 1);
        double freq = 1.9 + mood.Anger * 3.0 + crouch * 2.4 - sleep * 1.2;
        double amp = 6.5 + mood.Anger * 5.5 + crouch * 6.0 - sleep * 4.5;
        double angle = Math.Sin(t * freq) * amp + sleep * 22.0;

        dc.PushTransform(new TranslateTransform(TailPivotX, TailPivotY + sleep * 10.0));
        dc.PushTransform(new RotateTransform(angle));
        DrawLayerBitmap(dc, tailImg, skin.GetLayer(SkinSlot.Tail), 0, 0, TailRefW, TailRefH, skin.PixelArtFilter);
        dc.Pop();
        dc.Pop();
    }

    private static void DrawCustomArm(DrawingContext dc, CustomSkin skin, bool left, Point paw, double lean,
                                      double activity, double pawRaised, double sxScale, double syScale)
    {
        SkinSlot primarySlot = left ? SkinSlot.PawLeft : SkinSlot.PawRight;
        BitmapSource pawImg = skin.GetImage(primarySlot);
        SkinLayerConfig pawLayer = skin.GetLayer(primarySlot);
        bool mirroredFromLeft = false;

        if (!left && pawImg == null && skin.MirrorLeftPawToRight)
        {
            pawImg = skin.GetImage(SkinSlot.PawLeft);
            if (pawImg != null)
            {
                pawLayer = skin.GetLayer(SkinSlot.PawLeft);
                mirroredFromLeft = true;
            }
        }

        // Mode 1: Full arm sprite stretches & rotates from shoulder to paw target
        if (skin.ArmMode == CustomArmMode.FullArmSprite && pawImg != null)
        {
            Point shoulder = GetCustomShoulder(skin.BaseCat, left, lean, sxScale, syScale);
            Vector diff = paw - shoulder;
            double dist = Math.Max(22.0, diff.Length);
            double angle = Math.Atan2(diff.Y, diff.X) * 180.0 / Math.PI - 90.0;
            double stretchY = Math.Max(0.35, dist / 92.0);
            SkinLayerConfig eff = pawLayer.Clone();
            if (mirroredFromLeft)
            {
                eff.FlipX = !eff.FlipX;
                eff.OffsetX = -eff.OffsetX;
                eff.Rotation = -eff.Rotation;
            }
            // Anchor near shoulder top (pivotY = 0.12) for full-arm stretch
            eff.PivotY = 0.12;
            dc.PushTransform(new TranslateTransform(shoulder.X, shoulder.Y));
            dc.PushTransform(new RotateTransform(angle));
            DrawLayerBitmap(dc, pawImg, eff, 0, 0, FullArmRefW, FullArmRefH * stretchY, skin.PixelArtFilter);
            dc.Pop();
            dc.Pop();
            return;
        }

        // Mode 2 & 3: Curved Bezier arm (custom color or BaseCat) + optional PNG paw tip overlay
        bool hasPngPawTip = skin.ArmMode == CustomArmMode.FlexArmWithPngPaw && pawImg != null;
        double side = left ? -1 : 1;
        double raised = pawRaised >= 0 ? pawRaised : Smooth((221.0 - paw.Y) / 32.0);
        Point a = GetCustomShoulder(skin.BaseCat, left, lean, sxScale, syScale);
        Point b, c;
        ComputeArmControls(a, paw, side, raised, out b, out c);
        double pawRadius = 8.4 + Limit(activity, 0, 1) * 1.0;

        if (skin.UseCustomArmColor)
        {
            Color baseCol = CustomSkin.ParseHexColor(skin.ArmColorHex, Color.FromRgb(36, 34, 38));
            Color tipCol = CustomSkin.ParseHexColor(skin.ArmTipColorHex, baseCol);
            Color midCol = Color.FromRgb(
                (byte)((baseCol.R + tipCol.R) / 2),
                (byte)((baseCol.G + tipCol.G) / 2),
                (byte)((baseCol.B + tipCol.B) / 2));
            StreamGeometry limb = BuildArmGeometry(a, b, c, paw, pawRadius);
            Brush armBrush = ArmGradient(baseCol, midCol, 0.45, tipCol, a, paw);
            dc.DrawGeometry(armBrush, null, limb);
            if (!hasPngPawTip)
            {
                SolidColorBrush pawBrush = new SolidColorBrush(tipCol);
                pawBrush.Freeze();
                dc.DrawEllipse(pawBrush, null, paw, pawRadius + 0.35, pawRadius * (0.94 - 0.06 * activity));
            }
        }
        else
        {
            DrawBaseArm(dc, skin.BaseCat, left, paw, lean, activity, pawRaised, sxScale, syScale);
        }

        if (hasPngPawTip)
        {
            Vector dir = paw - c;
            double wristAngle = 0;
            if (dir.LengthSquared > 0.01)
            {
                wristAngle = Math.Atan2(dir.Y, dir.X) * 180.0 / Math.PI - 90.0;
                // Normalize to [-180, 180] and keep gentle wrist tilt so paw art never flips awkwardly
                while (wristAngle > 180) wristAngle -= 360;
                while (wristAngle < -180) wristAngle += 360;
                wristAngle = Limit(wristAngle * 0.45, -36.0, 36.0);
            }
            double squashY = 1.0 - 0.08 * Limit(activity, 0, 1);
            double squashX = 1.0 + 0.05 * Limit(activity, 0, 1);
            SkinLayerConfig eff = pawLayer.Clone();
            if (mirroredFromLeft)
            {
                eff.FlipX = !eff.FlipX;
                eff.OffsetX = -eff.OffsetX;
                eff.Rotation = -eff.Rotation;
            }
            dc.PushTransform(new TranslateTransform(paw.X, paw.Y));
            dc.PushTransform(new RotateTransform(wristAngle));
            dc.PushTransform(new ScaleTransform(squashX, squashY));
            DrawLayerBitmap(dc, pawImg, eff, 0, 0, PawTipRefSize, PawTipRefSize, skin.PixelArtFilter);
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }
    }

    private static void DrawBaseArm(DrawingContext dc, CatStyle style, bool left, Point paw, double lean,
                                    double activity, double pawRaised, double sxScale, double syScale)
    {
        if (style == CatStyle.PhotoTabby)
        {
            TabbyArt.DrawArmPart(dc, left, paw, lean, activity, pawRaised, sxScale, syScale);
            return;
        }
        if (style == CatStyle.JapaneseBobtail)
        {
            BobtailArt.DrawArmPart(dc, left, paw, lean, activity, pawRaised, sxScale, syScale);
            return;
        }
        bool ginger = style == CatStyle.Ginger;
        bool styled = style == CatStyle.Coffee || style == CatStyle.SilverShaded || style == CatStyle.Siamese || style == CatStyle.Wednesday;
        if (styled) ArmStyled(dc, left, paw, lean, activity, style, pawRaised, sxScale, syScale);
        else Arm(dc, left, paw, lean, activity, ginger, pawRaised, sxScale, syScale);
    }

    private static void DrawCustomHeadInner(DrawingContext dc, CustomSkin skin, double gx, double gy, double blink,
                                            double anger, double angerBreath, double crouch, bool isTabby, bool isBobtail)
    {
        BitmapSource headNormal = skin.GetImage(SkinSlot.Head);
        BitmapSource headBlink = skin.GetImage(SkinSlot.HeadBlink);
        BitmapSource headAngry = skin.GetImage(SkinSlot.HeadAngry);

        if (headNormal != null || headBlink != null || headAngry != null)
        {
            SkinSlot chosenSlot = SkinSlot.Head;
            BitmapSource chosenImg = headNormal;
            if (blink >= 0.55 && headBlink != null)
            {
                chosenSlot = SkinSlot.HeadBlink;
                chosenImg = headBlink;
            }
            else if (anger >= 0.35 && headAngry != null)
            {
                chosenSlot = SkinSlot.HeadAngry;
                chosenImg = headAngry;
            }
            if (chosenImg == null)
            {
                if (headNormal != null) { chosenSlot = SkinSlot.Head; chosenImg = headNormal; }
                else if (headBlink != null) { chosenSlot = SkinSlot.HeadBlink; chosenImg = headBlink; }
                else { chosenSlot = SkinSlot.HeadAngry; chosenImg = headAngry; }
            }

            // Subtle breathing-blink micro squash if single PNG head has no blink sprite and no dynamic eye overlay
            bool microBlinkSquash = blink > 0.55 && headBlink == null && skin.EyeMode == CustomEyeMode.PngOnly;
            if (microBlinkSquash)
            {
                double sq = 1.0 - 0.03 * Smooth((blink - 0.55) / 0.45);
                dc.PushTransform(new TranslateTransform(0, HeadRefY));
                dc.PushTransform(new ScaleTransform(1.0 / Math.Sqrt(sq), sq));
                dc.PushTransform(new TranslateTransform(0, -HeadRefY));
            }

            DrawLayerBitmap(dc, chosenImg, skin.GetLayer(chosenSlot), 0, HeadRefY, HeadRefW, HeadRefH, skin.PixelArtFilter);

            if (microBlinkSquash)
            {
                dc.Pop();
                dc.Pop();
                dc.Pop();
            }

            if (skin.EyeMode == CustomEyeMode.OverlayDynamicEyes)
            {
                DrawCustomDynamicEye(dc, skin, -1, gx, gy, blink, anger, angerBreath, crouch);
                DrawCustomDynamicEye(dc, skin, 1, gx, gy, blink, anger, angerBreath, crouch);
            }

            if (skin.ShowWhiskersOverlay)
            {
                bool styled = skin.BaseCat == CatStyle.Coffee || skin.BaseCat == CatStyle.SilverShaded || skin.BaseCat == CatStyle.Siamese;
                if (styled)
                {
                    WhiskersStyled(dc, -1, gx, skin.BaseCat);
                    WhiskersStyled(dc, 1, gx, skin.BaseCat);
                }
                else
                {
                    Whiskers(dc, -1, gx, skin.BaseCat == CatStyle.Ginger);
                    Whiskers(dc, 1, gx, skin.BaseCat == CatStyle.Ginger);
                }
            }
        }
        else
        {
            // No custom head PNG: render BaseCat's full animated head
            if (isTabby) TabbyArt.DrawHeadPart(dc, gx, gy, blink, anger, angerBreath, crouch);
            else if (isBobtail) BobtailArt.DrawHeadPart(dc, gx, gy, blink, anger, angerBreath, crouch);
            else
            {
                bool ginger = skin.BaseCat == CatStyle.Ginger;
                bool styled = skin.BaseCat == CatStyle.Coffee || skin.BaseCat == CatStyle.SilverShaded ||
                              skin.BaseCat == CatStyle.Siamese || skin.BaseCat == CatStyle.Wednesday;
                if (styled) DrawHeadStyled(dc, skin.BaseCat, gx, gy, blink, anger, angerBreath, crouch);
                else DrawHeadMood(dc, ginger, gx, gy, blink, anger, angerBreath, crouch);
            }
        }

        // Accessory sits on top of head (hat, bow, glasses, headphones, etc.)
        DrawAccessoryIfPresent(dc, skin);
    }

    private static void DrawAccessoryIfPresent(DrawingContext dc, CustomSkin skin)
    {
        BitmapSource accImg = skin.GetImage(SkinSlot.Accessory);
        if (accImg == null) return;
        DrawLayerBitmap(dc, accImg, skin.GetLayer(SkinSlot.Accessory), 0, AccRefY, AccRefW, AccRefH, skin.PixelArtFilter);
    }

    private static void DrawCustomDynamicEye(DrawingContext dc, CustomSkin skin, double side, double gx, double gy,
                                             double blink, double anger, double angerBreath, double crouch)
    {
        Color irisLight = CustomSkin.ParseHexColor(skin.EyeIrisHex, Color.FromRgb(215, 245, 80));
        Color irisDark = Color.FromRgb(
            (byte)(irisLight.R * 0.80),
            (byte)(irisLight.G * 0.80),
            (byte)(irisLight.B * 0.80));
        Color pupilCol = CustomSkin.ParseHexColor(skin.EyePupilHex, Color.FromRgb(22, 24, 16));
        Color lidCol = Color.FromRgb(
            (byte)(irisDark.R * 0.72),
            (byte)(irisDark.G * 0.72),
            (byte)(irisDark.B * 0.72));

        LinearGradientBrush irisBrush = new LinearGradientBrush(irisLight, irisDark, new Point(0.2, 0), new Point(0.83, 1));
        irisBrush.Freeze();
        SolidColorBrush pupilBrush = new SolidColorBrush(pupilCol);
        pupilBrush.Freeze();
        Pen lidPen = Stroke(new SolidColorBrush(lidCol), 2.1);
        Pen eyeEdge = Stroke(AlphaBrushRgb(140, 40, 32, 28), 0.95);
        Brush angryCover = skin.UseCustomArmColor
            ? (Brush)new SolidColorBrush(CustomSkin.ParseHexColor(skin.ArmColorHex, Color.FromRgb(36, 34, 38)))
            : BlackLidFur;
        if (angryCover.CanFreeze) angryCover.Freeze();

        double sc = skin.EyeScale;
        double angle = side * 3.0 + gx * 4.0;
        double x = side * skin.EyeSpacing + gx * 2.6;
        double y = -15.8 + skin.EyeOffsetY + gy * 1.7 - side * gx * 1.1;
        double width = 12.6 * sc * (1.0 - Math.Max(0, side * gx) * 0.095);
        double height = 12.1 * sc * (1.0 - blink);

        dc.PushTransform(new RotateTransform(angle, x, y));
        if (height < 1.6)
        {
            StreamGeometry slit = new StreamGeometry();
            using (StreamGeometryContext g = slit.Open())
            {
                g.BeginFigure(new Point(x - width * 0.86, y), false, false);
                g.QuadraticBezierTo(new Point(x, y + 3.0 * sc), new Point(x + width * 0.86, y - 0.2), true, true);
            }
            dc.DrawGeometry(null, lidPen, slit);
        }
        else
        {
            EllipseGeometry iris = new EllipseGeometry(new Point(x, y), width, height);
            dc.DrawGeometry(irisBrush, eyeEdge, iris);
            dc.PushClip(iris);
            double boost = 1.0 + 0.16 * crouch;
            Point pupilPos = new Point(x + gx * 7.2 * sc, y + gy * 6.0 * sc);
            double prx = (4.3 + Math.Abs(gy) * 0.75) * sc * boost;
            double pry = 8.0 * sc * boost;
            dc.DrawEllipse(pupilBrush, null, pupilPos, prx, pry);
            dc.DrawEllipse(AlphaBrushRgb(245, 255, 254, 246), null,
                new Point(pupilPos.X - 1.5 * sc, pupilPos.Y - 2.7 * sc), 1.6 * sc, 1.7 * sc);
            dc.Pop();
            if (anger > .01)
                DrawAngryEyelid(dc, x, y, width, height, side, angryCover, lidPen, anger, angerBreath);
        }
        dc.Pop();
    }

    private static void DrawLayerBitmap(DrawingContext dc, BitmapSource bmp, SkinLayerConfig layer,
                                        double refCenterX, double refCenterY, double refW, double refH, bool pixelArt)
    {
        if (bmp == null || layer == null) return;
        double imgW = Math.Max(1.0, bmp.PixelWidth);
        double imgH = Math.Max(1.0, bmp.PixelHeight);
        double fit = Math.Min(refW / imgW, refH / imgH);
        double w = imgW * fit * layer.Scale;
        double h = imgH * fit * layer.Scale;
        double px = w * layer.PivotX;
        double py = h * layer.PivotY;

        bool pushOp = layer.Opacity < 0.999;
        if (pushOp) dc.PushOpacity(Limit(layer.Opacity, 0.05, 1.0));
        dc.PushTransform(new TranslateTransform(refCenterX + layer.OffsetX, refCenterY + layer.OffsetY));
        if (Math.Abs(layer.Rotation) > 0.01) dc.PushTransform(new RotateTransform(layer.Rotation));
        if (layer.FlipX) dc.PushTransform(new ScaleTransform(-1, 1));

        Rect rect = new Rect(-px, -py, w, h);
        DrawingGroup dg = new DrawingGroup();
        RenderOptions.SetBitmapScalingMode(dg, pixelArt ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
        using (DrawingContext idc = dg.Open())
        {
            idc.DrawImage(bmp, rect);
        }
        dg.Freeze();
        dc.DrawDrawing(dg);

        if (layer.FlipX) dc.Pop();
        if (Math.Abs(layer.Rotation) > 0.01) dc.Pop();
        dc.Pop();
        if (pushOp) dc.Pop();
    }

    private static void DrawLayerGuideBox(DrawingContext dc, BitmapSource bmp, SkinLayerConfig layer,
                                          double refCenterX, double refCenterY, double refW, double refH)
    {
        double w, h;
        if (bmp != null && bmp.PixelWidth > 0 && bmp.PixelHeight > 0)
        {
            double fit = Math.Min(refW / bmp.PixelWidth, refH / bmp.PixelHeight);
            w = bmp.PixelWidth * fit * layer.Scale;
            h = bmp.PixelHeight * fit * layer.Scale;
        }
        else
        {
            w = refW * layer.Scale;
            h = refH * layer.Scale;
        }
        double px = w * layer.PivotX;
        double py = h * layer.PivotY;

        dc.PushTransform(new TranslateTransform(refCenterX + layer.OffsetX, refCenterY + layer.OffsetY));
        if (Math.Abs(layer.Rotation) > 0.01) dc.PushTransform(new RotateTransform(layer.Rotation));
        Rect rect = new Rect(-px, -py, w, h);
        dc.DrawRoundedRectangle(GuideFillBrush, GuideBoxPen, rect, 4, 4);
        // Pivot crosshair
        dc.DrawLine(GuideCrossPen, new Point(-6, 0), new Point(6, 0));
        dc.DrawLine(GuideCrossPen, new Point(0, -6), new Point(0, 6));
        if (Math.Abs(layer.Rotation) > 0.01) dc.Pop();
        dc.Pop();
    }

    internal static void DrawBaseSlotForPng(DrawingContext dc, CatStyle style, SkinSlot slot)
    {
        bool isTabby = style == CatStyle.PhotoTabby;
        bool isBobtail = style == CatStyle.JapaneseBobtail;
        bool ginger = style == CatStyle.Ginger;
        bool styled = style == CatStyle.Coffee || style == CatStyle.SilverShaded ||
                      style == CatStyle.Siamese || style == CatStyle.Wednesday;

        switch (slot)
        {
            case SkinSlot.Head:
            case SkinSlot.HeadBlink:
            case SkinSlot.HeadAngry:
            {
                double blink = slot == SkinSlot.HeadBlink ? 1.0 : 0.0;
                double anger = slot == SkinSlot.HeadAngry ? 1.0 : 0.0;
                // Center head-local (0, -14) into a 112x112 export box at (56, 56)
                dc.PushTransform(new TranslateTransform(HeadRefW * 0.5, HeadRefH * 0.5 - HeadRefY));
                if (isTabby) TabbyArt.DrawHeadPart(dc, 0, 0, blink, anger, 0.7, 0);
                else if (isBobtail) BobtailArt.DrawHeadPart(dc, 0, 0, blink, anger, 0.7, 0);
                else if (styled) DrawHeadStyled(dc, style, 0, 0, blink, anger, 0.7, 0);
                else DrawHeadMood(dc, ginger, 0, 0, blink, anger, 0.7, 0);
                dc.Pop();
                break;
            }
            case SkinSlot.Body:
            {
                // Center body (214, 171) into a 120x140 export box
                dc.PushTransform(new TranslateTransform(BodyRefW * 0.5 - BodyRefX, BodyRefH * 0.5 - BodyRefY));
                DrawBaseBody(dc, style, 0, 1, 1, 0);
                if (isTabby) TabbyArt.DrawRuffPart(dc, 0, 1, 1, 0);
                dc.Pop();
                break;
            }
            case SkinSlot.Tail:
            {
                // Place tail pivot (251, 206) at (10%, 72%) of a 96x72 box
                dc.PushTransform(new TranslateTransform(TailRefW * 0.10 - TailPivotX, TailRefH * 0.72 - TailPivotY));
                DrawBaseTail(dc, style, 0, default(CatMood));
                dc.Pop();
                break;
            }
            case SkinSlot.PawLeft:
            case SkinSlot.PawRight:
            {
                // Draw a cute standalone paw mitten centered inside 32x32 box
                double cx = PawTipRefSize * 0.5;
                double cy = PawTipRefSize * 0.5;
                Color fillCol = GetBasePawColor(style);
                Color padCol = Color.FromRgb(236, 168, 174);
                SolidColorBrush fur = new SolidColorBrush(fillCol);
                fur.Freeze();
                SolidColorBrush pad = new SolidColorBrush(padCol);
                pad.Freeze();
                Pen outline = Stroke(AlphaBrushRgb(110, 45, 36, 30), 1.1);
                dc.DrawEllipse(fur, outline, new Point(cx, cy + 0.5), 11.2, 10.0);
                // Soft subtle toe bean hints
                dc.PushOpacity(0.55);
                dc.DrawEllipse(pad, null, new Point(cx - 4.8, cy - 2.6), 2.1, 2.5);
                dc.DrawEllipse(pad, null, new Point(cx, cy - 4.4), 2.1, 2.6);
                dc.DrawEllipse(pad, null, new Point(cx + 4.8, cy - 2.6), 2.1, 2.5);
                dc.DrawEllipse(pad, null, new Point(cx, cy + 2.2), 4.0, 3.0);
                dc.Pop();
                break;
            }
            case SkinSlot.Accessory:
            {
                // Subtle head ear contour guide for accessory alignment in 96x96 box
                dc.PushTransform(new TranslateTransform(AccRefW * 0.5, AccRefH * 0.75 - AccRefY));
                dc.DrawGeometry(null, GuideBoxPen, Head);
                dc.Pop();
                break;
            }
        }
    }

    internal static Size GetSlotExportLogicalSize(SkinSlot slot)
    {
        switch (slot)
        {
            case SkinSlot.Head:
            case SkinSlot.HeadBlink:
            case SkinSlot.HeadAngry:
                return new Size(HeadRefW, HeadRefH);
            case SkinSlot.Body:
                return new Size(BodyRefW, BodyRefH);
            case SkinSlot.Tail:
                return new Size(TailRefW, TailRefH);
            case SkinSlot.PawLeft:
            case SkinSlot.PawRight:
                return new Size(PawTipRefSize, PawTipRefSize);
            case SkinSlot.Accessory:
                return new Size(AccRefW, AccRefH);
            default:
                return new Size(112, 112);
        }
    }

    internal static Color GetBasePawColor(CatStyle style)
    {
        switch (style)
        {
            case CatStyle.Black:
            case CatStyle.Wednesday: return Color.FromRgb(30, 28, 32);
            case CatStyle.Ginger: return Color.FromRgb(255, 230, 192);
            case CatStyle.JapaneseBobtail: return Color.FromRgb(244, 232, 214);
            case CatStyle.PhotoTabby: return Color.FromRgb(245, 239, 221);
            case CatStyle.Coffee: return Color.FromRgb(255, 252, 248);
            case CatStyle.SilverShaded: return Color.FromRgb(246, 247, 248);
            case CatStyle.Siamese: return Color.FromRgb(82, 60, 54);
            default: return Color.FromRgb(30, 28, 32);
        }
    }
}
