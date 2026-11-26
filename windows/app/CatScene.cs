using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

// All positions are expressed on a 480 x 320 drawing surface.
internal sealed class CatScene : FrameworkElement
{
    private sealed class Cap
    {
        public string Label;
        public Point Start;
        public double Time, Angle;
    }

    private enum IdleAction { None, Sleep, HuntBug }

    private static readonly Point IdleLeft = new Point(192, 233);
    private static readonly Point IdleRight = new Point(229, 233);
    private readonly List<Cap> caps = new List<Cap>();
    private readonly Dictionary<int, double> strikes = new Dictionary<int, double>();
    private readonly Dictionary<int, double> highlights = new Dictionary<int, double>();
    private readonly List<int> expired = new List<int>();
    private readonly Queue<double> fastInputs = new Queue<double>();
    private readonly Random random = new Random();
    private Point left = IdleLeft, freeLeft = IdleLeft, right = IdleRight, leftKey, rightKey;
    private Point pointer, lastPointer;
    private Vector mouseOffset, localTravel, monitorPosition;
    private double grip;
    private bool pointerKnown, leftDown, rightDown, headHovered, clockInitialized;
    private double time, lastKey = -10, leftAt = -10, rightAt = -10;
    private double lastMove = -10, leftClickAt = -10, rightClickAt = -10, lastPet = -10, nextBlink = 2.7;
    private double lastActivity, idleActionStart, idleActionDuration;
    private double gazeX, gazeY, blink, leftActivity, rightActivity;
    private double fastDuration, fastCoolDelay, sweatBlend, angerBlend;
    private double passwordBlend, sleepBlend, crouchBlend;
    private bool passwordFocused;
    private IdleAction idleAction = IdleAction.None;
    private double bugX = 148, bugY = 82, bugHeading = -1, swatFlash;
    private Point huntLeftTarget = new Point(186, 212), huntRightTarget = new Point(240, 212);
    private int huntSwatSide;
    public bool LabelsEnabled = true;
    public bool PasswordModeEnabled = true;
    public int IdleMinutes = 5;
    public CatStyle Cat = CatStyle.Black;
    public CustomSkin ActiveCustomSkin;
    public KeyboardStyle Keyboard = KeyboardStyle.Classic;
    public MouseStyle Mouse = MouseStyle.Classic;
    public GamepadStyle Gamepad = GamepadStyle.Xbox;
    public GamepadMode GamepadMode = GamepadMode.Auto;
    public const double CanvasWidth = 480, CanvasHeight = 320;

    // Gamepad mode: the pad state drives the drawing (GamepadArt), the paws
    // grip the controller, and the gaze follows the last pressed button.
    private readonly GamepadState pad = new GamepadState();
    private readonly Dictionary<GamepadButton, double> padDown = new Dictionary<GamepadButton, double>();
    private readonly Dictionary<GamepadButton, double> padUp = new Dictionary<GamepadButton, double>();
    private readonly List<GamepadButton> padExpired = new List<GamepadButton>();
    private static readonly Point PadLeftBase = new Point(190.68, 175.49);
    private static readonly Point PadRightBase = new Point(237.32, 175.49);
    private Point lastPadKey = new Point(214, 200);
    private double lastPadActivity = -10, lastModeSwitch = -10;
    private bool showingGamepad;

    private bool InPasswordMode { get { return PasswordModeEnabled && passwordFocused; } }

    public void ClearLabels() { caps.Clear(); InvalidateVisual(); }

    public void SetPasswordFocused(bool focused)
    {
        if (passwordFocused == focused) return;
        passwordFocused = focused;
        if (InPasswordMode)
        {
            caps.Clear();
            strikes.Clear();
            highlights.Clear();
            idleAction = IdleAction.None;
            fastDuration = 0;
            fastInputs.Clear();
        }
    }

    private void WakeFromIdle(double now)
    {
        lastActivity = now;
        if (idleAction != IdleAction.None) idleAction = IdleAction.None;
    }

    private void RecordFastInput(double now)
    {
        if (InPasswordMode) return;
        fastInputs.Enqueue(now);
        while (fastInputs.Count > 64) fastInputs.Dequeue();
    }

    public void Strike(int vk, double now)
    {
        WakeFromIdle(now);
        if (InPasswordMode)
        {
            if (GamepadMode == GamepadMode.Auto && showingGamepad)
            {
                showingGamepad = false;
                lastModeSwitch = now;
            }
            return;
        }
        if (GamepadMode == GamepadMode.Gamepad) return;
        if (showingGamepad)
        {
            // Auto mode: a key press after 5 s of pad idleness switches back,
            // once the 1.5 s dwell after the last switch has passed.
            if (GamepadMode == GamepadMode.Auto && now - lastPadActivity > 5
                && now - lastModeSwitch > 1.5)
            {
                showingGamepad = false;
                lastModeSwitch = now;
            }
            else return;
        }
        RecordFastInput(now);
        Point target = DeskArt.KeyPoint(vk);
        lastKey = now;
        strikes[vk] = now;
        if (!UsingMouse(now) && (target.X < 218 || (vk == 32 && rightAt > leftAt)))
        {
            leftAt = now;
            leftKey = target;
        }
        else
        {
            rightAt = now;
            rightKey = target;
        }
        if (!LabelsEnabled) return;
        string label = KeyLabels.Get(vk);
        if (label == null) return;
        if (caps.Count >= 9) caps.RemoveAt(0);
        caps.Add(new Cap { Label = label, Time = now,
            Start = new Point(target.X + random.Next(-5, 6), target.Y - 14),
            Angle = random.Next(-7, 8) });
    }

    public void SetGamepadMode(GamepadMode mode)
    {
        GamepadMode = mode;
        if (mode == GamepadMode.Gamepad) showingGamepad = true;
        else if (mode == GamepadMode.Keyboard) showingGamepad = false;
        InvalidateVisual();
    }

    public void GamepadPress(GamepadButton button, double now)
    {
        WakeFromIdle(now);
        lastPadActivity = now;
        if (GamepadMode == GamepadMode.Keyboard) return;
        RecordFastInput(now);
        padDown[button] = now;
        padUp.Remove(button);
        lastPadKey = GamepadArt.ButtonPoint(Gamepad, button);
        if (LabelsEnabled && !InPasswordMode)
        {
            string label = GamepadArt.Label(Gamepad, button);
            if (label != null)
            {
                if (caps.Count >= 9) caps.RemoveAt(0);
                caps.Add(new Cap { Label = label, Time = now,
                    Start = new Point(lastPadKey.X + random.Next(-5, 6), lastPadKey.Y - 14),
                    Angle = random.Next(-7, 8) });
            }
        }
        if (GamepadMode == GamepadMode.Auto && !showingGamepad && now - lastModeSwitch > 1.5)
        {
            showingGamepad = true;
            lastModeSwitch = now;
        }
    }

    public void GamepadRelease(GamepadButton button, double now)
    {
        WakeFromIdle(now);
        lastPadActivity = now;
        if (padDown.ContainsKey(button)) padUp[button] = now;
    }

    public void GamepadStick(bool left, Vector tilt, double now)
    {
        if (tilt.LengthSquared > 0.04) WakeFromIdle(now);
        lastPadActivity = now;
        if (GamepadMode == GamepadMode.Keyboard) return;
        if (tilt.LengthSquared > 1) tilt.Normalize();
        if (left) pad.LeftStick = tilt; else pad.RightStick = tilt;
        if (GamepadMode == GamepadMode.Auto && !showingGamepad && now - lastModeSwitch > 1.5)
        {
            showingGamepad = true;
            lastModeSwitch = now;
        }
    }

    public void GamepadTrigger(bool left, double value, double now)
    {
        if (GamepadMode == GamepadMode.Keyboard) return;
        value = Math.Max(0, Math.Min(1, value));
        if (left) pad.LeftTrigger = value; else pad.RightTrigger = value;
        // The trigger is analog: below the 30/255 threshold it only moves
        // the visual, it does not count as pad activity (spec section 8).
        if (value * 255 < 30) return;
        WakeFromIdle(now);
        lastPadActivity = now;
        if (value > 0 && GamepadMode == GamepadMode.Auto && !showingGamepad
            && now - lastModeSwitch > 1.5)
        {
            showingGamepad = true;
            lastModeSwitch = now;
        }
    }

    private double PadIntensity(GamepadButton button, double now)
    {
        double down;
        if (!padDown.TryGetValue(button, out down)) return 0;
        double up;
        if (padUp.TryGetValue(button, out up))
        {
            double age = now - up;
            if (age >= 0.33) return 0;
            return 1 - Smooth(age / 0.33);
        }
        return Math.Min(1, (now - down) / 0.05);
    }

    private static bool IsLeftPadButton(GamepadStyle style, GamepadButton button)
    {
        return GamepadArt.ButtonPoint(style, button).X < 214;
    }

    public void Pointer(Point p, bool buttonDown, double now)
    {
        Pointer(p, buttonDown, false, now, new Vector(Math.Tanh((p.X - 214) / 600), Math.Tanh((p.Y - 150) / 400)));
    }

    public void Pointer(Point p, bool buttonDown, double now, Vector positionOnMonitor)
    {
        Pointer(p, buttonDown, false, now, positionOnMonitor);
    }

    public void Pointer(Point p, bool physicalLeft, bool physicalRight, double now, Vector positionOnMonitor)
    {
        Vector delta = p - lastPointer;
        if (pointerKnown && delta.LengthSquared > .000001)
        {
            localTravel.X = Clamp(localTravel.X + delta.X * .13, -5.5, 5.5);
            localTravel.Y = Clamp(localTravel.Y + delta.Y * .13, -6, 6);
            MarkMouseActivity(now);
        }
        monitorPosition = positionOnMonitor;
        bool onHead = ActiveCustomSkin != null
            ? CustomSkinRenderer.ContainsHead(ActiveCustomSkin, p, time, gazeX, gazeY, left)
            : CatArt.ContainsHead(Cat, p, time, gazeX, gazeY, left);
        if (onHead && !headHovered)
        {
            lastPet = now;
            WakeFromIdle(now);
        }
        headHovered = onHead;
        pointerKnown = true;
        pointer = lastPointer = p;
        MouseButton(1, physicalLeft, now);
        MouseButton(2, physicalRight, now);
    }

    public void MouseButton(int virtualKey, bool down, double now)
    {
        if (down) WakeFromIdle(now);
        if (GamepadMode == GamepadMode.Gamepad) return;
        if (showingGamepad && down)
        {
            if (GamepadMode == GamepadMode.Auto && now - lastPadActivity > 5
                && now - lastModeSwitch > 1.5)
            {
                showingGamepad = false;
                lastModeSwitch = now;
            }
            else return;
        }
        if (virtualKey == 1)
        {
            if (leftDown == down) return;
            MarkMouseActivity(now);
            leftDown = down;
            if (down) leftClickAt = now;
        }
        else if (virtualKey == 2)
        {
            if (rightDown == down) return;
            MarkMouseActivity(now);
            rightDown = down;
            if (down) rightClickAt = now;
        }
        if (down && headHovered) lastPet = now;
    }

    private void MarkMouseActivity(double now)
    {
        WakeFromIdle(now);
        // A key-down can arrive just before the next cursor sample.
        if (!UsingMouse(now) && leftAt > rightAt && now - leftAt < .24)
        {
            rightKey = leftKey; rightAt = leftAt; leftAt = -10;
        }
        lastMove = now;
    }

    private bool UsingMouse(double now) { return leftDown || rightDown || now - lastMove < .83; }
    private double ClickGlow(bool down, double onset)
    {
        // A tap that starts and ends between animation frames still gets a visible press.
        return down ? 1 : 1 - Smooth((time - onset - .10) / .14);
    }
    private static double Clamp(double value, double low, double high) { return Math.Max(low, Math.Min(high, value)); }

    private void UpdateFastMood(double now, double dt)
    {
        while (fastInputs.Count > 0 && now - fastInputs.Peek() > 1.5) fastInputs.Dequeue();
        if (InPasswordMode || idleAction != IdleAction.None)
        {
            fastDuration = Math.Max(0, fastDuration - dt * 4.0);
        }
        else if (fastInputs.Count >= 7)
        {
            fastDuration = Math.Min(7.0, fastDuration + dt);
            fastCoolDelay = 0;
        }
        else
        {
            fastCoolDelay += dt;
            if (fastCoolDelay > 0.75) fastDuration = Math.Max(0, fastDuration - dt * 1.8);
        }
        double targetSweat = (fastDuration >= 1.6 && fastDuration < 4.5) ? 1.0 : 0.0;
        double targetAnger = fastDuration >= 4.5 ? 1.0 : 0.0;
        double rate = 1 - Math.Exp(-dt * 7.5);
        sweatBlend += (targetSweat - sweatBlend) * rate;
        angerBlend += (targetAnger - angerBlend) * rate;
    }

    private void UpdateIdleState(double now, double dt)
    {
        if (IdleMinutes <= 0 || InPasswordMode)
        {
            idleAction = IdleAction.None;
        }
        else if (now - lastActivity >= IdleMinutes * 60.0)
        {
            if (idleAction == IdleAction.None || now - idleActionStart >= idleActionDuration)
            {
                if (showingGamepad && GamepadMode == GamepadMode.Auto)
                {
                    showingGamepad = false;
                    lastModeSwitch = now;
                }
                if (showingGamepad)
                {
                    idleAction = IdleAction.Sleep;
                    idleActionDuration = 60.0;
                }
                else
                {
                    IdleAction prev = idleAction;
                    if (prev == IdleAction.Sleep) idleAction = IdleAction.HuntBug;
                    else if (prev == IdleAction.HuntBug) idleAction = IdleAction.Sleep;
                    else idleAction = random.Next(2) == 0 ? IdleAction.Sleep : IdleAction.HuntBug;
                    idleActionDuration = idleAction == IdleAction.Sleep
                        ? 45.0 + random.NextDouble() * 35.0
                        : 30.0 + random.NextDouble() * 20.0;
                }
                idleActionStart = now;
            }
        }
        else
        {
            idleAction = IdleAction.None;
        }

        double targetSleep = idleAction == IdleAction.Sleep ? 1.0 : 0.0;
        double sleepRate = targetSleep > sleepBlend ? 1 - Math.Exp(-dt * 2.4) : 1 - Math.Exp(-dt * 9.0);
        sleepBlend += (targetSleep - sleepBlend) * sleepRate;
        if (sleepBlend < .002 && targetSleep == 0) sleepBlend = 0;

        double targetCrouch = 0.0;
        swatFlash = 0.0;
        huntSwatSide = 0;
        if (idleAction == IdleAction.HuntBug)
        {
            double phase = (now - idleActionStart) % 3.6;
            if (phase < 0.70)
            {
                double p = phase / 0.70;
                bugX = 148.0 - 26.0 * Smooth(p) + Math.Sin(now * 7.0) * 5.0;
                bugY = 82.0 + Math.Sin(now * 6.0) * 10.0;
                bugHeading = -1.0;
                huntLeftTarget = Mix(new Point(186, 212), new Point(144, 152), Smooth(p));
                huntRightTarget = new Point(240, 216);
                targetCrouch = 0.85;
            }
            else if (phase < 1.50)
            {
                double p = (phase - 0.70) / 0.80;
                huntSwatSide = -1;
                if (p < 0.32)
                {
                    double k = Smooth(p / 0.32);
                    huntLeftTarget = Mix(new Point(144, 152), new Point(122, 74), k);
                    bugX = 122.0; bugY = 74.0; bugHeading = -1.0;
                    swatFlash = k;
                }
                else if (p < 0.65)
                {
                    double k = Smooth((p - 0.32) / 0.33);
                    huntLeftTarget = Mix(new Point(122, 74), new Point(178, 112), k);
                    bugX = 122.0 + (304.0 - 122.0) * k;
                    bugY = 74.0 + (62.0 - 74.0) * k - Math.Sin(k * Math.PI) * 24.0;
                    bugHeading = 1.0;
                    swatFlash = 1.0 - 0.3 * k;
                }
                else
                {
                    double k = Smooth((p - 0.65) / 0.35);
                    huntLeftTarget = Mix(new Point(178, 112), new Point(186, 212), k);
                    bugX = 304.0 + Math.Sin(k * 4.0) * 6.0;
                    bugY = 70.0 + k * 14.0;
                    bugHeading = 1.0;
                    swatFlash = (1.0 - k) * 0.7;
                }
                huntRightTarget = new Point(240, 216);
                targetCrouch = 0.30;
            }
            else if (phase < 2.20)
            {
                double p = (phase - 1.50) / 0.70;
                bugX = 300.0 + 18.0 * Smooth(p) + Math.Sin(now * 7.0) * 5.0;
                bugY = 82.0 + Math.Sin(now * 6.0) * 10.0;
                bugHeading = 1.0;
                huntLeftTarget = new Point(186, 216);
                huntRightTarget = Mix(new Point(240, 212), new Point(284, 150), Smooth(p));
                targetCrouch = 0.85;
            }
            else if (phase < 3.00)
            {
                double p = (phase - 2.20) / 0.80;
                huntSwatSide = 1;
                if (p < 0.32)
                {
                    double k = Smooth(p / 0.32);
                    huntRightTarget = Mix(new Point(284, 150), new Point(310, 74), k);
                    bugX = 310.0; bugY = 74.0; bugHeading = 1.0;
                    swatFlash = k;
                }
                else if (p < 0.65)
                {
                    double k = Smooth((p - 0.32) / 0.33);
                    huntRightTarget = Mix(new Point(310, 74), new Point(252, 112), k);
                    bugX = 310.0 + (148.0 - 310.0) * k;
                    bugY = 74.0 + (62.0 - 74.0) * k - Math.Sin(k * Math.PI) * 24.0;
                    bugHeading = -1.0;
                    swatFlash = 1.0 - 0.3 * k;
                }
                else
                {
                    double k = Smooth((p - 0.65) / 0.35);
                    huntRightTarget = Mix(new Point(252, 112), new Point(240, 212), k);
                    bugX = 148.0;
                    bugY = 70.0 + k * 12.0;
                    bugHeading = -1.0;
                    swatFlash = (1.0 - k) * 0.7;
                }
                huntLeftTarget = new Point(186, 216);
                targetCrouch = 0.30;
            }
            else
            {
                double p = (phase - 3.00) / 0.60;
                bugX = 148.0;
                bugY = 82.0 + Math.Sin(p * Math.PI) * 8.0;
                bugHeading = -1.0;
                huntLeftTarget = new Point(186, 212);
                huntRightTarget = new Point(240, 212);
                targetCrouch = 0.80;
            }
        }
        double crouchRate = 1 - Math.Exp(-dt * 6.0);
        crouchBlend += (targetCrouch - crouchBlend) * crouchRate;
        if (crouchBlend < .002 && targetCrouch == 0) crouchBlend = 0;
    }

    public void Advance(double now)
    {
        if (!clockInitialized)
        {
            clockInitialized = true;
            time = now;
            lastActivity = now;
        }
        double dt = Math.Max(0.001, Math.Min(0.1, now - time));
        time = now;

        double targetPassword = InPasswordMode ? 1.0 : 0.0;
        passwordBlend += (targetPassword - passwordBlend) * (1 - Math.Exp(-dt * 9.0));
        if (passwordBlend < .002 && targetPassword == 0) passwordBlend = 0;

        UpdateIdleState(now, dt);
        UpdateFastMood(now, dt);

        if (now > nextBlink + 0.30) nextBlink = now + 2.6 + random.NextDouble() * 3.7;
        blink = Math.Max(Pulse(now - nextBlink, .23), Pulse(now - lastPet, .59));
        blink = Math.Max(blink, passwordBlend);
        if (sleepBlend > .15) blink = Math.Max(blink, Smooth((sleepBlend - .15) / .45));

        if (showingGamepad) AdvanceGamepad(now, dt);
        else AdvanceKeyboardMouse(now, dt);

        for (int i = caps.Count - 1; i >= 0; i--)
        {
            // The newest key drifts over to the tail and lingers like the reference.
            double lifetime = i == caps.Count - 1 ? 5.8 : .89;
            if (now - caps[i].Time > lifetime) caps.RemoveAt(i);
        }
        highlights.Clear();
        expired.Clear();
        foreach (var pair in strikes)
        {
            double age = now - pair.Value;
            if (age > .43) expired.Add(pair.Key);
            else highlights[pair.Key] = 1 - Smooth(age / .43);
        }
        foreach (int key in expired) strikes.Remove(key);
        InvalidateVisual();
    }

    private void AdvanceKeyboardMouse(double now, double dt)
    {
        double armEase = 1 - Math.Exp(-dt * 46);
        double eyeEase = 1 - Math.Exp(-dt * 14);
        if (InPasswordMode)
        {
            // Completely freeze both paws and release the mouse while focused in a password box.
            grip += Clamp(-grip, -dt * 12, dt * 12);
            localTravel *= Math.Exp(-dt * 10);
            mouseOffset += (-mouseOffset) * (1 - Math.Exp(-dt * 14));
            double pwdArmEase = 1 - Math.Exp(-dt * 14);
            freeLeft = Mix(freeLeft, new Point(188, 226), pwdArmEase);
            left = Mix(freeLeft, MouseArt.PawTarget + mouseOffset, grip);
            right = Mix(right, new Point(238, 226), pwdArmEase);
            leftActivity = rightActivity = 0;
            gazeX += (0 - gazeX) * eyeEase;
            gazeY += (0.15 - gazeY) * eyeEase;
            return;
        }
        if (idleAction == IdleAction.Sleep)
        {
            grip += Clamp(-grip, -dt * 10, dt * 12);
            mouseOffset += (-mouseOffset) * (1 - Math.Exp(-dt * 14));
            double sleepArmEase = 1 - Math.Exp(-dt * 8);
            left = freeLeft = Mix(left, new Point(186, 230), sleepArmEase);
            right = Mix(right, new Point(240, 230), sleepArmEase);
            leftActivity = rightActivity = 0;
            gazeX += (0 - gazeX) * eyeEase;
            gazeY += (0 - gazeY) * eyeEase;
            return;
        }
        if (idleAction == IdleAction.HuntBug)
        {
            grip += Clamp(-grip, -dt * 10, dt * 12);
            mouseOffset += (-mouseOffset) * (1 - Math.Exp(-dt * 14));
            double huntArmEase = 1 - Math.Exp(-dt * 28);
            left = freeLeft = Mix(left, huntLeftTarget, huntArmEase);
            right = Mix(right, huntRightTarget, huntArmEase);
            leftActivity = huntSwatSide < 0 ? Math.Max(0.35, swatFlash) : 0.25;
            rightActivity = huntSwatSide > 0 ? Math.Max(0.35, swatFlash) : 0.25;
            double btx = Clamp((bugX - 214.0) / 95.0, -1, 1);
            double bty = Clamp((bugY - 105.0) / 75.0, -1, 1);
            gazeX += (btx - gazeX) * eyeEase;
            gazeY += (bty - gazeY) * eyeEase;
            return;
        }

        double typing = 1 - Smooth((now - lastKey - .95) / .48);
        double hold = leftDown || rightDown ? 1 : 1 - Smooth((now - lastMove - .55) / .28);
        grip += Clamp(hold - grip, -dt * 10, dt * 12);
        if (hold == 0) localTravel *= Math.Exp(-dt * 8);
        // Slow sweeps follow the position on the monitor; small reversals also
        // move the miniature mouse without requiring a sweep across the screen.
        Vector travel = new Vector(monitorPosition.X * 5 + localTravel.X, monitorPosition.Y * 5 + localTravel.Y);
        mouseOffset += (travel * grip - mouseOffset) * (1 - Math.Exp(-dt * 32));
        Point leftRest = Mix(IdleLeft, new Point(188, 189), typing);
        Point rightRest = Mix(IdleRight, new Point(233, 193), typing);
        double la = Beat(now - leftAt);
        double ra = Beat(now - rightAt);
        leftRest = Mix(leftRest, leftKey, la);
        rightRest = Mix(rightRest, rightKey, ra);
        Point mousePaw = MouseArt.PawTarget + mouseOffset;
        double leftClick = ClickGlow(leftDown, leftClickAt), rightClick = ClickGlow(rightDown, rightClickAt);
        mousePaw.X += (leftClick - rightClick) * 1.6;
        mousePaw.Y += 2.6 * Math.Max(leftClick, rightClick);
        freeLeft = Mix(freeLeft, leftRest, armEase);
        left = Mix(freeLeft, mousePaw, grip);
        right = Mix(right, rightRest, armEase);
        leftActivity = Math.Max(typing, grip);
        rightActivity = typing;

        // Keep eyes tracking the cursor smoothly even while rapidly typing (sweat / anger expressions).
        double tx = pointerKnown ? Math.Tanh((pointer.X - 214) / 155) : 0;
        double ty = pointerKnown ? Math.Tanh((pointer.Y - 85) / 155) : 0;
        if (!pointerKnown && typing > 0)
        {
            double currentKeyX = leftAt >= rightAt ? leftKey.X : rightKey.X;
            tx = Math.Tanh((currentKeyX - 214) / 130) * typing;
            ty = typing;
        }
        gazeX += (tx - gazeX) * eyeEase;
        gazeY += (ty - gazeY) * eyeEase;
    }

    private void AdvanceGamepad(double now, double dt)
    {
        double eyeEase = 1 - Math.Exp(-dt * 14);
        if (idleAction == IdleAction.Sleep)
        {
            double sleepArmEase = 1 - Math.Exp(-dt * 8);
            left = Mix(left, new Point(192, 218), sleepArmEase);
            right = Mix(right, new Point(236, 218), sleepArmEase);
            leftActivity = rightActivity = 0;
            gazeX += (0 - gazeX) * eyeEase;
            gazeY += (0 - gazeY) * eyeEase;
            return;
        }
        // Button intensities: 0.05 s up, hold while down, 0.33 s decay.
        padExpired.Clear();
        foreach (var pair in padDown)
        {
            double intensity = PadIntensity(pair.Key, now);
            pad.Set(pair.Key, intensity);
            if (intensity <= 0 && padUp.ContainsKey(pair.Key)) padExpired.Add(pair.Key);
        }
        foreach (GamepadButton button in padExpired)
        {
            padDown.Remove(button);
            padUp.Remove(button);
        }
        // Paws: each paw works the buttons on its side of the screen, and
        // follows its stick's cap displacement.
        Point newLeft = PadPaw(true, now);
        Point newRight = PadPaw(false, now);
        double armEase = 1 - Math.Exp(-dt * 46);
        left = Mix(left, newLeft, armEase);
        right = Mix(right, newRight, armEase);
        double leftStick = Math.Min(1, pad.RightStick.Length);
        double rightStick = Math.Min(1, pad.LeftStick.Length);
        leftActivity = Math.Max(PadPawActivity(true, now), 0.75 * leftStick);
        rightActivity = Math.Max(PadPawActivity(false, now), 0.75 * rightStick);
        // Gaze: follow cursor when cursor moved recently, otherwise look down at the active gamepad button.
        bool cursorRecent = pointerKnown && now - lastMove < 3.0;
        double tx = cursorRecent ? Math.Tanh((pointer.X - 214) / 155) : Math.Tanh((lastPadKey.X - 214) / 130);
        double ty = cursorRecent ? Math.Tanh((pointer.Y - 85) / 155) : 0.9;
        gazeX += (tx - gazeX) * eyeEase;
        gazeY += (ty - gazeY) * eyeEase;
    }

    private Point PadPaw(bool leftScreen, double now)
    {
        Point basePaw = leftScreen ? PadLeftBase : PadRightBase;
        Point paw = basePaw;
        foreach (var pair in padDown)
        {
            if (IsLeftPadButton(Gamepad, pair.Key) != leftScreen) continue;
            double beat = Beat(now - pair.Value);
            if (beat <= 0) continue;
            Point target = GamepadArt.ButtonPoint(Gamepad, pair.Key);
            paw += (target - basePaw) * 0.24 * beat;
            paw.Y += 1.4 * beat;
        }
        // The paw follows its stick: the screen-left paw works the right
        // stick, the screen-right paw the left stick.
        Vector tilt = leftScreen ? pad.RightStick : pad.LeftStick;
        if (tilt.LengthSquared > 0)
        {
            Vector offset = GamepadArt.StickCapOffset(Gamepad, !leftScreen, tilt);
            paw += offset * 1.2;
        }
        return paw;
    }

    private double PadPawActivity(bool leftScreen, double now)
    {
        double activity = 0;
        foreach (var pair in padDown)
        {
            if (IsLeftPadButton(Gamepad, pair.Key) != leftScreen) continue;
            activity = Math.Max(activity, Beat(now - pair.Value));
        }
        return activity;
    }

    private static double Beat(double age)
    {
        if (age < 0 || age > .24) return 0;
        return age < .065 ? 1 : 1 - Smooth((age - .065) / .175);
    }
    private static double Pulse(double age, double duration)
    {
        if (age < 0 || age > duration) return 0;
        return Math.Min(1, Math.Sin(Math.PI * age / duration) * 1.4);
    }
    private static double Smooth(double a)
    {
        a = Math.Max(0, Math.Min(1, a));
        return a * a * (3 - 2 * a);
    }
    private static Point Mix(Point a, Point b, double k) { return a + (b - a) * k; }

    protected override void OnRender(DrawingContext dc)
    {
        dc.PushTransform(new ScaleTransform(ActualWidth / CanvasWidth, ActualHeight / CanvasHeight));
        CatMood mood = new CatMood
        {
            Sweat = sweatBlend,
            Anger = angerBlend,
            Sleep = sleepBlend,
            Crouch = crouchBlend
        };
        if (showingGamepad)
        {
            double padDrop = 26.0 * sleepBlend;
            Action<DrawingContext> drawPad = delegate(DrawingContext g)
            {
                if (padDrop > .01) g.PushTransform(new TranslateTransform(0, padDrop));
                GamepadArt.Draw(g, Gamepad, pad);
                if (padDrop > .01) g.Pop();
            };
            if (ActiveCustomSkin != null)
                CustomSkinRenderer.Draw(dc, ActiveCustomSkin, time, gazeX, gazeY, blink, left, right, leftActivity, rightActivity, 0.25, drawPad, mood);
            else
                CatArt.DrawStyled(dc, Cat, time, gazeX, gazeY, blink, left, right, leftActivity, rightActivity, 0.25, drawPad, mood);
        }
        else
        {
            double lGlow = InPasswordMode ? 0 : ClickGlow(leftDown, leftClickAt);
            double rGlow = InPasswordMode ? 0 : ClickGlow(rightDown, rightClickAt);
            DeskArt.DrawDesk(dc, InPasswordMode ? null : highlights, lGlow, rGlow,
                Keyboard, Mouse, mouseOffset);
            if (ActiveCustomSkin != null)
                CustomSkinRenderer.Draw(dc, ActiveCustomSkin, time, gazeX, gazeY, blink, left, right, leftActivity, rightActivity, -1, null, mood);
            else
                CatArt.DrawStyled(dc, Cat, time, gazeX, gazeY, blink, left, right, leftActivity, rightActivity, -1, null, mood);
            if (passwordBlend > .005) DeskArt.DrawPasswordCover(dc, passwordBlend);
        }
        if (sleepBlend > .35 && (ActiveCustomSkin == null || ActiveCustomSkin.ShowMoodEffects))
        {
            CatArt.DrawZzz(dc, time, 258, 144, Smooth((sleepBlend - .35) / .5));
        }
        if (idleAction == IdleAction.HuntBug && !showingGamepad)
        {
            if (huntSwatSide < 0) CatArt.DrawSwatTrail(dc, left.X, left.Y, -1, swatFlash);
            else if (huntSwatSide > 0) CatArt.DrawSwatTrail(dc, right.X, right.Y, 1, swatFlash);
            CatArt.DrawBug(dc, bugX, bugY, time, bugHeading, swatFlash);
        }
        for (int i = 0; i < caps.Count; i++)
        {
            Cap cap = caps[i];
            double age = Math.Max(0, time - cap.Time);
            double progress = age / .89;
            double travel = 1 - Math.Pow(1 - Math.Min(1, Math.Max(0, progress)), 2);
            Point center = new Point(cap.Start.X + cap.Angle * travel, cap.Start.Y - 53 * travel);
            bool newest = i == caps.Count - 1;
            double settle = newest ? Smooth((age - .60) / .42) : 0;
            center = Mix(center, new Point(298, 214), settle);
            double opacity = newest ? 1 - Smooth((age - 5.4) / .4) : 1 - Smooth((progress - .55) / .45);
            DeskArt.DrawCap(dc, cap.Label, center, opacity, cap.Angle * travel * (1 - settle));
        }
        dc.Pop();
    }
}

internal static class KeyLabels
{
    public static string Get(int vk)
    {
        if ((vk >= 48 && vk <= 57) || (vk >= 65 && vk <= 90)) return ((char)vk).ToString();
        if (vk >= 96 && vk <= 105) return (vk - 96).ToString();
        if (vk >= 112 && vk <= 135) return "F" + (vk - 111);
        switch (vk)
        {
            case 8: return "⌫"; case 9: return "Tab"; case 13: return "↵";
            case 16: case 160: case 161: return "⇧";
            case 17: case 162: case 163: return "Ctrl";
            case 18: case 164: case 165: return "Alt";
            case 19: return "Pause"; case 20: return "Caps"; case 27: return "Esc";
            case 32: return "␣"; case 33: return "PgUp"; case 34: return "PgDn";
            case 35: return "End"; case 36: return "Home"; case 37: return "←";
            case 38: return "↑"; case 39: return "→"; case 40: return "↓";
            case 44: return "Prt"; case 45: return "Ins"; case 46: return "Del";
            case 91: case 92: return "Win"; case 93: return "Menu";
            case 106: return "×"; case 107: return "+"; case 109: return "−";
            case 110: case 190: return "."; case 111: case 191: return "/";
            case 144: return "Num"; case 145: return "Scr";
            case 186: return ";"; case 187: return "="; case 188: return ",";
            case 189: return "−"; case 192: return "`"; case 219: return "[";
            case 220: case 226: return "\\"; case 221: return "]"; case 222: return "'";
            default: return null;
        }
    }
}
