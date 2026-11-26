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

    private sealed class PetParticle
    {
        public bool IsHeart;
        public double X, Y, Vx, Vy, Born, Life, Scale, Rot, Phase;
    }

    private enum IdleAction { None, Sleep, HuntBug }

    private static readonly Point IdleLeft = new Point(192, 233);
    private static readonly Point IdleRight = new Point(229, 233);
    private readonly List<Cap> caps = new List<Cap>();
    private readonly List<PetParticle> petParticles = new List<PetParticle>();
    private readonly Dictionary<int, double> strikes = new Dictionary<int, double>();
    private readonly Dictionary<int, double> highlights = new Dictionary<int, double>();
    private readonly List<int> expired = new List<int>();
    private readonly Queue<double> fastInputs = new Queue<double>();
    private readonly Random random = new Random();
    private Point left = IdleLeft, freeLeft = IdleLeft, right = IdleRight, leftKey, rightKey;
    private Point pointer, lastPointer;
    private Vector mouseOffset, localTravel, monitorPosition;
    private double grip;
    private bool pointerKnown, leftDown, rightDown, clockInitialized;
    private PetZone petZone = PetZone.None;
    private double time, lastKey = -10, leftAt = -10, rightAt = -10;
    private double lastMove = -10, leftClickAt = -10, rightClickAt = -10, lastPet = -10, nextBlink = 2.7;
    private double lastActivity, idleActionStart, idleActionDuration;
    private double gazeX, gazeY, blink, leftActivity, rightActivity;
    private double fastDuration, fastCoolDelay, sweatBlend, angerBlend;
    private double passwordBlend, sleepBlend, crouchBlend;
    private double petSpeed, petVelX, petHandPhase, petRubHold, fastRubDuration, impatientHold;
    private double happyPetBlend, impatientBlend, petNuzzleX, petNuzzleY, nextPetParticleAt;
    private bool allowPetHand = true, dragActive;
    private double dragHoldProgress, dragLiftBlend, dragSwingDeg, dragSwingVel, dragTargetSwing;
    private bool passwordFocused;
    private IdleAction idleAction = IdleAction.None;
    private double bugX = 148, bugY = 82, bugHeading = -1, swatFlash;
    private Point huntLeftTarget = new Point(186, 212), huntRightTarget = new Point(240, 212);
    private int huntSwatSide;
    public bool LabelsEnabled = true;
    public bool PasswordModeEnabled = true;
    public int IdleMinutes = 5;
    public CatStyle Cat = CatStyle.Wednesday;
    public CompiledSkin CustomSkin;
    public KeyboardStyle Keyboard = KeyboardStyle.Classic;
    public MouseStyle Mouse = MouseStyle.Classic;
    public GamepadStyle Gamepad = GamepadStyle.Xbox;
    public GamepadMode GamepadMode = GamepadMode.Auto;
    public const double CanvasWidth = 480, CanvasHeight = 320;

    public PetZone CurrentPetZone { get { return petZone; } }
    public bool IsPetHandVisible
    {
        get
        {
            return (dragActive || dragLiftBlend > 0.03) ||
                (allowPetHand && pointerKnown &&
                 (dragHoldProgress > 0.03 || (petZone != PetZone.None && !InPasswordMode)));
        }
    }

    public void SetDragVisual(bool allowVisualHand, double holdProgress, bool isDragging)
    {
        allowPetHand = allowVisualHand;
        dragHoldProgress = Clamp(holdProgress, 0, 1);
        dragActive = isDragging;
        if (!allowVisualHand && !isDragging)
        {
            petZone = PetZone.None;
            petSpeed = 0;
            fastRubDuration = 0;
        }
    }

    // Feeds horizontal window drag velocity into the scruff-held pendulum sway.
    public void NotifyDragMove(double deltaDipX)
    {
        if (!dragActive) return;
        dragTargetSwing = Clamp(dragTargetSwing - deltaDipX * 0.42, -13.0, 13.0);
    }

    // Triggered by a quick left-click pat on the cat (shorter than long-press drag).
    public void QuickPat(double now)
    {
        if (InPasswordMode || !allowPetHand) return;
        WakeFromIdle(now);
        lastPet = now;
        petRubHold = Math.Max(petRubHold, 0.85);
        impatientHold = 0;
        fastRubDuration = 0;
        Point origin = pointerKnown ? pointer : new Point(214, 92);
        SpawnPetParticle(origin, now, true);
        SpawnPetParticle(new Point(origin.X + random.Next(-18, 19), origin.Y - 8), now, false);
    }

    private void SpawnPetParticle(Point origin, double now, bool forceHeart)
    {
        if (petParticles.Count >= 16) petParticles.RemoveAt(0);
        petParticles.Add(new PetParticle
        {
            IsHeart = forceHeart,
            X = Clamp(origin.X + random.Next(-22, 23), 130, 305),
            Y = Clamp(origin.Y - 10 + random.Next(-14, 10), 28, 210),
            Vx = (random.NextDouble() - 0.5) * 20.0,
            Vy = -(36.0 + random.NextDouble() * 24.0),
            Born = now,
            Life = 0.95 + random.NextDouble() * 0.45,
            Scale = 0.72 + random.NextDouble() * 0.36,
            Rot = (random.NextDouble() - 0.5) * 26.0,
            Phase = random.NextDouble() * Math.PI * 2.0
        });
    }

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
        double sampleDt = Math.Max(0.012, Math.Min(0.1, now - time));
        if (pointerKnown && delta.LengthSquared > .000001)
        {
            localTravel.X = Clamp(localTravel.X + delta.X * .13, -5.5, 5.5);
            localTravel.Y = Clamp(localTravel.Y + delta.Y * .13, -6, 6);
            MarkMouseActivity(now);
        }
        monitorPosition = positionOnMonitor;

        PetZone zone = allowPetHand
            ? (CustomSkin != null
                ? CustomSkinArt.ContainsCat(CustomSkin, p, time, gazeX, gazeY, left, sleepBlend)
                : CatArt.ContainsCat(Cat, p, time, gazeX, gazeY, left, sleepBlend))
            : PetZone.None;

        if (zone != PetZone.None && !InPasswordMode && !dragActive)
        {
            if (petZone == PetZone.None)
            {
                lastPet = now;
                WakeFromIdle(now);
            }
            if (pointerKnown)
            {
                double instSpeed = delta.Length / sampleDt;
                petSpeed = petSpeed * 0.58 + instSpeed * 0.42;
                petVelX = petVelX * 0.55 + (delta.X / sampleDt) * 0.45;
                if (delta.Length > 1.1)
                {
                    petRubHold = 0.48;
                    WakeFromIdle(now);
                }
            }
        }
        else
        {
            petSpeed *= 0.65;
            petVelX *= 0.65;
        }
        petZone = zone;
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
    }

    private void UpdatePetMood(double now, double dt)
    {
        bool activeHover = allowPetHand && petZone != PetZone.None && !InPasswordMode && !dragActive;
        if (!activeHover)
        {
            petSpeed *= Math.Exp(-dt * 9.0);
            petVelX *= Math.Exp(-dt * 9.0);
        }
        petRubHold = Math.Max(0, petRubHold - dt);
        impatientHold = Math.Max(0, impatientHold - dt);

        if (activeHover && petSpeed > 490.0)
        {
            fastRubDuration = Math.Min(3.5, fastRubDuration + dt);
            if (fastRubDuration >= 0.80) impatientHold = 1.65;
        }
        else
        {
            fastRubDuration = Math.Max(0, fastRubDuration - dt * 1.45);
        }

        bool isImpatient = impatientHold > 0.01 && !InPasswordMode;
        bool isRubbing = activeHover && petRubHold > 0.01 && !isImpatient;
        bool isHappy = !InPasswordMode && !isImpatient && (isRubbing || (petRubHold > 0.05 && happyPetBlend > 0.25));

        double targetImpatient = isImpatient ? 1.0 : 0.0;
        double targetHappy = isHappy ? 1.0 : 0.0;
        double moodRate = 1 - Math.Exp(-dt * 7.5);
        impatientBlend += (targetImpatient - impatientBlend) * moodRate;
        happyPetBlend += (targetHappy - happyPetBlend) * moodRate;
        if (impatientBlend < .004 && targetImpatient == 0) impatientBlend = 0;
        if (happyPetBlend < .004 && targetHappy == 0) happyPetBlend = 0;

        double targetNx = isHappy && pointerKnown ? Clamp((pointer.X - 214.0) / 55.0, -1, 1) : 0;
        double targetNy = isHappy && pointerKnown ? Clamp((pointer.Y - 105.0) / 65.0, -1, 1) : 0;
        double nuzzleEase = 1 - Math.Exp(-dt * 10.0);
        petNuzzleX += (targetNx - petNuzzleX) * nuzzleEase;
        petNuzzleY += (targetNy - petNuzzleY) * nuzzleEase;

        double handRate = isImpatient ? 18.0 : (isRubbing ? 11.0 : 3.2);
        petHandPhase += dt * handRate;

        // Spawn floating hearts & stars during happy petting.
        if (isHappy && happyPetBlend > 0.28 && now >= nextPetParticleAt)
        {
            Point spawnOrigin = pointerKnown && petZone != PetZone.None
                ? pointer
                : new Point(214 + random.Next(-26, 27), 86);
            SpawnPetParticle(spawnOrigin, now, random.Next(2) == 0);
            nextPetParticleAt = now + 0.24 + random.NextDouble() * 0.18;
        }

        // Advance floating particles.
        for (int i = petParticles.Count - 1; i >= 0; --i)
        {
            PetParticle pt = petParticles[i];
            if (now - pt.Born >= pt.Life)
            {
                petParticles.RemoveAt(i);
                continue;
            }
            pt.X += (pt.Vx + Math.Sin((now - pt.Born) * 5.5 + pt.Phase) * 14.0) * dt;
            pt.Y += pt.Vy * dt;
            pt.Vy *= Math.Exp(-dt * 0.85);
        }
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

        if (dragActive) WakeFromIdle(now);
        double targetLift = dragActive ? 1.0 : 0.0;
        double liftRate = dragActive ? 1 - Math.Exp(-dt * 14.0) : 1 - Math.Exp(-dt * 11.0);
        dragLiftBlend += (targetLift - dragLiftBlend) * liftRate;
        if (dragLiftBlend < 0.002 && !dragActive) dragLiftBlend = 0;

        dragTargetSwing *= Math.Exp(-dt * 7.5);
        double idleDangle = dragActive ? Math.Sin(now * 3.4) * 2.2 : 0.0;
        double swingGoal = (dragTargetSwing + idleDangle) * dragLiftBlend;
        double swingAcc = (swingGoal - dragSwingDeg) * 92.0 - dragSwingVel * 11.0;
        dragSwingVel += swingAcc * dt;
        dragSwingDeg = Clamp(dragSwingDeg + dragSwingVel * dt, -15.0, 15.0);
        if (dragLiftBlend == 0 && Math.Abs(dragSwingDeg) < 0.05 && Math.Abs(dragSwingVel) < 0.05)
        {
            dragSwingDeg = 0;
            dragSwingVel = 0;
        }

        UpdateIdleState(now, dt);
        UpdateFastMood(now, dt);
        UpdatePetMood(now, dt);

        if (now > nextBlink + 0.30) nextBlink = now + 2.6 + random.NextDouble() * 3.7;
        blink = Math.Max(Pulse(now - nextBlink, .23), Pulse(now - lastPet, .36));
        blink = Math.Max(blink, passwordBlend);
        if (sleepBlend > .15) blink = Math.Max(blink, Smooth((sleepBlend - .15) / .45));

        if (showingGamepad) AdvanceGamepad(now, dt);
        else AdvanceKeyboardMouse(now, dt);

        if (dragLiftBlend > 0.01)
        {
            double hug = Smooth(dragLiftBlend);
            if (showingGamepad)
            {
                left = Mix(left, new Point(185, 166), hug);
                right = Mix(right, new Point(243, 166), hug);
            }
            else
            {
                // Left paw clutches the tucked-in mouse & left edge of keyboard; right paw wraps the right side of keyboard.
                left = Mix(left, new Point(168, 194), hug);
                right = Mix(right, new Point(256, 202), hug);
            }
            leftActivity = Math.Max(leftActivity, 0.30 * hug);
            rightActivity = Math.Max(rightActivity, 0.30 * hug);
            double liftGazeX = Clamp(-dragSwingDeg / 16.0, -0.35, 0.35);
            gazeX += (liftGazeX - gazeX) * Math.Min(1, hug * 0.65);
            gazeY += (-0.42 - gazeY) * Math.Min(1, hug * 0.65);
        }

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

        bool beingPetted = allowPetHand && (petZone != PetZone.None || dragActive || dragHoldProgress > 0.03);
        double typing = 1 - Smooth((now - lastKey - .95) / .48);
        double hold = beingPetted ? 0 : (leftDown || rightDown ? 1 : 1 - Smooth((now - lastMove - .55) / .28));
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

    private void DrawActiveCat(DrawingContext dc, double pawRaised, Action<DrawingContext> between, CatMood mood)
    {
        if (CustomSkin != null)
            CustomSkinArt.Draw(dc, CustomSkin, time, gazeX, gazeY, blink, left, right, leftActivity, rightActivity, pawRaised, between, mood);
        else
            CatArt.DrawStyled(dc, Cat, time, gazeX, gazeY, blink, left, right, leftActivity, rightActivity, pawRaised, between, mood);
    }

    protected override void OnRender(DrawingContext dc)
    {
        dc.PushTransform(new ScaleTransform(ActualWidth / CanvasWidth, ActualHeight / CanvasHeight));
        double lift = Smooth(dragLiftBlend);
        CatMood mood = new CatMood
        {
            Sweat = sweatBlend,
            Anger = angerBlend,
            Sleep = sleepBlend,
            Crouch = crouchBlend,
            HappyPet = happyPetBlend,
            Impatient = impatientBlend,
            PetNuzzleX = petNuzzleX,
            PetNuzzleY = petNuzzleY,
            ScruffLift = lift,
            DragSwing = dragSwingDeg
        };

        bool lifted = lift > 0.005;
        if (lifted)
        {
            dc.PushTransform(new TranslateTransform(0, -6.0 * lift));
            dc.PushTransform(new RotateTransform(dragSwingDeg, 214, 28));
        }

        if (showingGamepad)
        {
            double padDrop = 26.0 * sleepBlend * (1 - lift) - 22.0 * lift;
            DrawActiveCat(dc, lifted ? 0.45 : 0.25,
                delegate(DrawingContext g)
                {
                    if (Math.Abs(padDrop) > .01 || lifted)
                    {
                        g.PushTransform(new TranslateTransform(0, padDrop));
                        if (lifted) g.PushTransform(new RotateTransform(-5.0 * lift, 214, 175));
                    }
                    GamepadArt.Draw(g, Gamepad, pad);
                    if (Math.Abs(padDrop) > .01 || lifted)
                    {
                        if (lifted) g.Pop();
                        g.Pop();
                    }
                }, mood);
        }
        else
        {
            double lGlow = InPasswordMode ? 0 : ClickGlow(leftDown, leftClickAt);
            double rGlow = InPasswordMode ? 0 : ClickGlow(rightDown, rightClickAt);
            if (!lifted)
            {
                DeskArt.DrawDesk(dc, InPasswordMode ? null : highlights, lGlow, rGlow,
                    Keyboard, Mouse, mouseOffset);
                DrawActiveCat(dc, -1, null, mood);
                if (passwordBlend > .005) DeskArt.DrawPasswordCover(dc, passwordBlend);
            }
            else
            {
                // While lifted by the scruff, draw keyboard & mouse in the `between` layer
                // (in front of the torso, behind the forelegs) so both paws visibly hug them!
                DrawActiveCat(dc, 0.55 * lift,
                    delegate(DrawingContext g)
                    {
                        // Hugged keyboard lifted up snugly against the cat's tummy
                        g.PushTransform(new TranslateTransform(-8.0 * lift, -62.0 * lift));
                        g.PushTransform(new RotateTransform(-7.0 * lift, 228, 262));
                        g.PushTransform(new ScaleTransform(1.0 - 0.24 * lift, 1.0 - 0.14 * lift, 228, 262));
                        DeskArt.DrawKeyboard(g, Keyboard, InPasswordMode ? null : highlights);
                        g.Pop();
                        g.Pop();
                        g.Pop();

                        // Hugged mouse pulled inward under the cat's left arm next to the keyboard
                        g.PushTransform(new TranslateTransform(50.0 * lift + mouseOffset.X * (1 - lift), -84.0 * lift + mouseOffset.Y * (1 - lift)));
                        g.PushTransform(new RotateTransform(16.0 * lift, MouseArt.Center.X, MouseArt.Center.Y));
                        g.PushTransform(new ScaleTransform(1.0 - 0.08 * lift, 1.0 - 0.08 * lift, MouseArt.Center.X, MouseArt.Center.Y));
                        MouseArt.Draw(g, Mouse, lGlow, rGlow);
                        g.Pop();
                        g.Pop();
                        g.Pop();
                    }, mood);
            }
        }

        if (lifted)
        {
            CatArt.DrawDangleLines(dc, lift, dragSwingDeg, time);
            dc.Pop();
            dc.Pop();
        }

        if (sleepBlend > .35 && !lifted)
        {
            CatArt.DrawZzz(dc, time, 258, 144, Smooth((sleepBlend - .35) / .5));
        }
        if (idleAction == IdleAction.HuntBug && !showingGamepad && !lifted)
        {
            if (huntSwatSide < 0) CatArt.DrawSwatTrail(dc, left.X, left.Y, -1, swatFlash);
            else if (huntSwatSide > 0) CatArt.DrawSwatTrail(dc, right.X, right.Y, 1, swatFlash);
            CatArt.DrawBug(dc, bugX, bugY, time, bugHeading, swatFlash);
        }
        for (int i = 0; i < petParticles.Count; ++i)
        {
            PetParticle pt = petParticles[i];
            double p = Clamp((time - pt.Born) / pt.Life, 0, 1);
            double fadeIn = Smooth(p / 0.16);
            double fadeOut = 1.0 - Smooth((p - 0.65) / 0.35);
            double sc = pt.Scale * (0.65 + 0.45 * fadeIn);
            CatArt.DrawPetParticle(dc, pt.IsHeart, pt.X, pt.Y, sc, pt.Rot + Math.Sin(p * Math.PI * 2.0) * 8.0, fadeIn * fadeOut);
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
        if (IsPetHandVisible)
        {
            bool rubbing = petRubHold > 0.01 && petZone != PetZone.None && !dragActive && !lifted;
            Point scruffAnchor = new Point(214, 24 - 6.0 * lift);
            Point handPos = lifted
                ? Mix(pointerKnown ? pointer : scruffAnchor, scruffAnchor, lift)
                : pointer;
            CatArt.DrawPettingHand(dc, handPos, petVelX, petHandPhase, dragHoldProgress, dragActive || lift > 0.35, rubbing);
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
