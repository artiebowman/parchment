# Parchment

A 3D selection technique for the Meta Quest 3S, hands only. Turn a hard 3D pointing task into an easy 2D one, then shrink the reach.

*[GIF here: one Phantom run, start to seventh sphere, about 10 seconds. Recorded from the headset after the showcase.]*

---

## The Task

The assignment is a 3D selection task on the Quest 3S, hands only. A one-meter glass cube holds 100 small spheres at fixed positions. Each run lights up seven of them in sequence, one at a time, and the clock runs from the first correct selection to the seventh. Seven runs, and the average time is the score. The cube cannot be moved or scaled, every sphere has to be equally selectable, and one action has to select exactly one sphere.

---

---

## Solution and Thought Process

I started by reading the task for opportunities rather than for the obvious answer. Two things stood out. The clock does not start until the first correct selection, so any time spent before that is free. And the cube is fixed, but nothing in the rules says I cannot make a copy of it.

Then I listed what could work and checked each one against the rules. The nuclear option was the fastest on paper: send one wave out from the center of the cube and let it pass through every sphere. It failed the first rule, one action has to select one sphere, so it was out before I wrote a line. World-in-Miniature and Voodoo Dolls from class pointed the other way. Work on a small copy of the world, held in your hands.

With that settled, I built the safest tool first. A plain hand ray with a pinch met the project requirements and gave me a baseline. Then I built Parchment. During the free setup time, the 100-sphere cube is projected once onto a flat 10×10 sheet of bulbs at hip height. Each bulb stands for one sphere. From then on every selection is a touch on a flat panel, right in front of me.

Parchment worked, so I kept iterating. Even on a flat panel the hand has to travel across 45 cm of board, and that travel was most of the remaining time. Phantom fixes it. A small trackpad floats above the board, and one pad width maps to the whole grid with no gain. A few centimeters of finger movement covers the full board.

*[Screenshot here: the board unrolled with the trackpad above it, target lit, guide line showing. After the showcase.]*

The last idea was the grenade, which came from mixing the nuclear option with the rules Parchment and Phantom already lived inside. It would have been the fastest thing I built. It also would have been one action selecting many, so it stayed on paper. Phantom is what shipped.

Every layer roughly halved the time. Hand ray to Parchment, then Parchment to Phantom. Live runs are consistently under 3 seconds, the best is 2.17, and the seven-run average is around 2.6.

---

## How to Use Parchment

Left palm up opens the menu. The right index finger presses buttons.

1. Press **Parchment**. Wait for the board to finish unrolling.
2. Press **Phantom** for the trackpad. Wait for the unroll first; pressing it mid-roll can hang the app.
3. Double pinch to start run 1. A single pinch continues after each run.
4. **Practice** is on by default. Practice runs are unscored and repeat. Turn it off in Settings to go live.

Settings: Dist and Tilt move the board, Pad resizes the trackpad, Mute, Reset.

**For someone starting cold:**

1. Do one practice run. Nobody is counting.
2. Your finger on the little pad is your finger on the big board.
3. Slide, don't stab. The yellow line points at the target.
4. When it feels right, go live.

**Only one surface takes input at a time.** This is deliberate. Hands are cheap to trigger, and without this rule a finger reaching for the pad would poke the board on the way.

<br>

| State | What works |
|---|---|
| Board rolled up | Hand ray and pinch on the cube |
| Board out | Touch or ray on the board. Cube is off |
| Phantom on | Trackpad only. Board touch, ray, and tether are off |
| Menu open | Nothing on the board reacts |

---

## How It's Built

Unity 6, Meta XR Core and Interaction SDKs, OpenXR. Everything is C# in `Parchment/Assets/Scripts/`, no packages beyond Meta's. Sounds are synthesized in code, so there are no audio files. This was my first Unity project.

Three rules the code lives inside, from the assignment: one action selects one sphere, every sphere is equally selectable, the cube does not move. The practical test for any new feature was: would it behave differently with the target highlight turned off? If yes, it reads the target, and it went in the bin.

**Optimized for time, not accuracy.** The assignment scores time, and a wrong selection costs nothing. So the design leans into that: sliding a finger across the board pokes every bulb on the way, and Phantom does the same on the pad. The log fills with "wrong sphere" lines during a good run, and that is by design. If the rules ever changed to penalize misses, the fallback is already built. Poke can be turned off in the menu, and the board then works on hover and pinch only, one deliberate confirm per sphere. Phantom is poke-only by design, so that fallback lives on the board, not the pad.

The scripts, in the order a selection flows through them:

<br>

| Script | Job |
|---|---|
| HandPointer | Turns Meta hand tracking into a pointer ray, a fingertip, a palm, and a one-frame confirm. One pinch, one confirm, enforced here only |
| CubeRaySelector | Cube mode: ray plus pinch picks a sphere directly. Also where a pinch continues to the next run |
| ParchmentMode | Board on and off, and the unroll animation. Owns IsOn and Ready. First rung of the mode ladder |
| ParchmentScanner | Once at start: projects the 100 spheres onto the bulb grid by sorted id. Per frame: which bulb is the target, feedback only |
| Bulb | One bulb's id and colour. State, override, and the Phantom dark palette |
| ParchmentHover | Per hand, per frame: Phantom, near, or far path onto the sheet. Hover snap, poke with hysteresis, drag-through, no entry from below |
| PhantomPad | The trackpad. Fingertip to a spot on the pad, no gain. Draws the dot, mirrored bulbs, guide line |
| PhantomMode | Phantom switch. IsOn is the button, Active waits for the board to land. Dark palette, shade, violet hand, ghost hand |
| ParchmentSelector | Board touch to selection: pinch on hovered, poke on entered. Phantom forces poke. One Send per selection |
| TrialManager | The referee: sequence, target index, clock, practice or live, run count |
| Scoreboard, RunLog | The text above the cube and the run list beside it |
| PalmMenu, MenuButton, MenuPages, SettingsActions | The palm menu: open on palm-up, fingertip-in-bubble buttons, page swap, one method per Settings button |
| ParchmentFollow, ParchmentTether, HandTint, SyncSweep, AudioFeedback | Feedback layer: board follow, target tether, hand tint, first-unroll sync sweep, synthesized sounds |
| SequenceLoader, SphereLoader | Read the two text files in Resources and build the sequences and the spheres |
| EditorMouseRig | Editor only: mouse stands in for a hand |

One pattern shows up everywhere: hysteresis. The pinch turns on at 0.8 and off at 0.5. A poke enters at one radius and leaves at a larger one. The menu opens after a quarter second palm-up and closes after 0.4 seconds down. Two thresholds instead of one, so noise near the line never retriggers anything.

---

## Where It Stands

Shipped for the HW1 showcase on September 30, 2026, after about two weeks of evenings. First Unity project.

**Known issue.** Pressing Parchment and Phantom back to back, while the unroll is still animating, occasionally locks the app up completely. Four or five times in dozens of sessions. Recovery is the Meta menu, close the app, relaunch. Avoiding it is easy: wait for the roll to finish before pressing Phantom. Not common enough to chase down before the showcase, so it's logged for after.

**Parked.** A leaderboard and start screen on the rolled-up board. Per-target split times in the on-screen log; they are in the adb log today. Icons for the menu buttons, which are tiles with text for now.

**Dropped.** The grenade, a button that would drop a hundred randomized hands sweeping the board from the inside out. Faster than anything I built, and one action selecting many, so it broke the first rule. A gain knob for Phantom, replaced by resizing the pad.

**Next.** Fix the hang. Record the GIF. Then user-test Phantom's pad width with people who are not me, since my wife found it hard on first contact and I tuned it to my own hand.

---

## AI Disclosure

This is my first Unity project and my first C#. I used Claude throughout as a tutor and a coding partner. The design, the rules, every decision in `docs/DECISIONS.md`, the user testing, and the tuning are mine. The C# was written with Claude alongside, one piece at a time: I described what I wanted, Claude explained the Unity and C# needed to do it and drafted the code, and I reviewed, tested on the headset, and directed each change. I did not hand the assignment to an AI and take the output. Claude Code was used for documentation only. I can walk through any script in the project and say what it does and why it is there.

---

## More

- `docs/DECISIONS.md`: what was decided, why, and whether it shipped. The full reasoning behind everything above.
- `docs/PLAN.md`: the phased plan with exit tests.
- `docs/SETUP.md`: environment setup for both machines.
- `Data/`: the supplied `sphere_coordinates.txt` and `test_sequences.txt`. Live copies are in `Parchment/Assets/Resources/`. To run a new sequence set, replace the contents of `test_sequences.txt`, keep the filename, rebuild.

Built by Artie Bowman for HCC 552, AR/VR Design and Research, Grand Valley State University, Fall 2026.
