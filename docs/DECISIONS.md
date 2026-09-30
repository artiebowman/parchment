# Parchment: Decisions

What was decided, why, and whether it shipped.

---

# Summary

## Task

The assignment is a 3D selection task on the Quest 3S, hands only. A one-meter glass cube holds 100 small spheres at fixed positions. Each run lights up seven of them in sequence, one at a time, and the clock runs from the first correct selection to the seventh. Seven runs, and the average time is the score. The cube cannot be moved or scaled, every sphere has to be equally selectable, and one action has to select exactly one sphere.

## Solution and Thought Process

I started by reading the task for opportunities rather than for the obvious answer. Two things stood out. The clock does not start until the first correct selection, so any time spent before that is free. And the cube is fixed, but nothing in the rules says I cannot make a copy of it.

Then I listed what could work and checked each one against the rules. The nuclear option was the fastest on paper: send one wave out from the center of the cube and let it pass through every sphere. It failed the first rule, one action has to select one sphere, so it was out before I wrote a line. World-in-Miniature and Voodoo Dolls from class pointed the other way. Work on a small copy of the world, held in your hands.

With that settled, I built the safest tool first. A plain hand ray with a pinch met the project requirements and gave me a baseline. Then I built Parchment. During the free setup time, the 100-sphere cube is projected once onto a flat 10×10 sheet of bulbs at hip height. Each bulb stands for one sphere. From then on every selection is a touch on a flat panel, right in front of me.

Parchment worked, so I kept iterating. Even on a flat panel the hand has to travel across 45 cm of board, and that travel was most of the remaining time. Phantom fixes it. A small trackpad floats above the board, and one pad width maps to the whole grid with no gain. A few centimeters of finger movement covers the full board.

The last idea was the grenade, which came from mixing the nuclear option with the rules Parchment and Phantom already lived inside. It would have been the fastest thing I built. It also would have been one action selecting many, so it stayed on paper. Phantom is what shipped.

*# Lineage from class: World-in-Miniature and Voodoo Dolls for the proxy, reducing degrees of freedom for the 3D to 2D move, hysteresis for every threshold in the project, and Uncertain Pointer feedforward for the tether. Related work: Expand (Cashion 2012) and SQUAD (Kopper 2011) refine per selection, on demand. Parchment refines once, during setup.*

---

# Rules I Designed Inside

Before building anything I broke the assignment down into three rules the project has to follow. Every idea below was checked against them, and a few good ones were dropped because they broke one. The table lists each rule, what it rules out, and what it still leaves open.

| Rule | Forbids | Allows |
|---|---|---|
| Explicit confirmation | Hover, gaze, or dwell select. One action selecting many | One pinch or poke = one sphere |
| Equally selectable | Disabling non-targets. Bigger hit area on the target. Any logic that reads target identity | Reading the layout, the hand, the field of bulbs |
| Cube fixed | Moving or scaling the cube | Copies, proxies, the user moving |

Self-test for any idea: would it behave differently with the highlight turned off? If yes, it reads the target. Drop it.

One consequence of the first rule: a wrong selection has to be free. If misses cost time or reset the run, then drag-through and Phantom's slide to the target would be punished for doing exactly what they are meant to do. So a wrong sphere is logged and ignored, and the adb log fills with "wrong sphere" lines during a good run. That is normal.

---

# What Shipped

Every decision that made it into the build, plus the ones that were changed, put off, or dropped along the way. Status means what it says: Build shipped as planned, Changed shipped in a different form than first decided, Later is parked for after the showcase, Drop is gone.

| Area | Decision | Status |
|---|---|---|
| Technique | 3D to 2D proxy, built once in untimed setup | Build |
| Layout | 10×10 by sorted id. Bulb 3 cm, spacing 4.5 cm, panel 45 cm | Build |
| Board | Unrolls from the palm menu. Follows body yaw at a fixed distance and height, tilted back. Dist and Tilt nudges in Settings | Changed from grab-to-unroll and world-locked |
| Board | Unroll and roll-up animation with staggered bulb arrival. First unroll runs a sync sweep: bulbs read in red to green with a tick per column and a halo on the sphere, chime and flare at the end | Build |
| Bulbs | White idle, green target, yellow hover, pale green hovered target. Under Phantom: gray with a violet target | Changed |
| Touch | Near mode: fingertip projected straight down, height ignored. Far mode: hand ray to the sheet, hover only | Build |
| Touch | Poke to select: entering a bulb's ball presses it, leaving releases it. Hysteresis in and out. Drag-through allowed. No entry from below | Build |
| Touch | Snap to nearest bulb, two hands, alternate freely | Build |
| Phantom | Trackpad above the board. Fingertip maps 1:1 onto the bulb grid, always on the bulb plane. Pad width is the sensitivity | Build |
| Phantom | Ghost hand at the phantom point. Board dims under a shade. Target and hover mirrored onto the pad. Guide line finger to target | Build |
| Mode ladder | Cube only with the board away. Board touch only with Phantom off. Pad only with Phantom on. Nothing while the menu is up | Build |
| Confirm | Own pinch detector: on at 0.8, off at 0.5, cooldown, per hand | Build |
| Confirm | Double pinch starts run 1. Single pinch continues after | Build |
| Feedback | Tether from the nearer reticle to the target, hidden under Phantom. Click on hover, blip on menu, ding on correct. All sounds synthesized in code | Build |
| Hands | Outline tint: blue palm-up, violet under Phantom. Reticle shrinks and goes violet under Phantom | Changed from fingertip dots and go stones |
| Menu | Left palm up opens it, right hand presses. Pinches suppressed while up. Settings page: Dist, Tilt, Pad, Practice, Mute, Reset | Build |
| Trial | Practice on by default, unscored, same run repeats. Live counts and advances. Clock starts on the first correct selection. Wrong sphere logged and ignored | Build |
| UI | Scoreboard above the cube. RunLog beside it: practice lines with their average, or seven live slots with the live average | Changed from progress dots |
| Fallback | Board rolled up = cube mode, hand ray and pinch on the spheres | Build |
| Logging | Run time and average on screen. Splits and misses only in the adb log | Partial |
| Arcade | Leaderboard, initials, start screen | Later |
| Gain knob | Control-display gain on Phantom | Drop, pad size does the same job |
| Parked | Drones, ammo types, bombs, region copy | Drop for HW1 |

## Why, for the rows that need it

**No gain.** The pad is a 1:1 grid, 10×10 to the board. Scale the pad down and it gets more sensitive. Scale it up and it gets more accurate. I user-tested the width and settled on about the most sensitive I am comfortable with. Pad width is a Settings nudge. There is no gain slider.

**Mode ladder.** Hands are cheap to trigger. Without the ladder, a fingertip reaching for the pad would poke the board on the way, and a pinch meant for the menu would fire on the cube. So only one surface is live at a time, and which one depends on what is switched on.

**Practice and live.** Practice started as a tutorial idea. The point is that people get comfortable with the instruments before a live run. Practice is safe, with no score. Live is the real run, the one the class times.

**Double pinch.** A single pinch is quick and intuitive, but it fights with everything else that uses a pinch: the board, Phantom, even the Meta system menu. A double pinch is intentional, so a stray pinch during setup never starts the clock. After run 1, a single pinch continues.

---

# Rejected, So I Don't Re-argue Them

Ideas that came up more than once and lost every time. Each one is here with the rule it broke, so the next time it sounds good I can check the list instead of rebuilding the argument.

- Sweep selection, exploding charges, plunger chains, and the grenade: one action, many selections. The grenade was the last of these and the closest to shipping. A button above Phantom that drops a hundred randomized hands sweeping the board from the inside out. Seven presses, maybe fewer, and faster than my best time. It broke the first rule, and I was out of time anyway. I have other classes.
- Heat-seeking drones, lock on match, pierce until red: the logic reads the target.
- Bigger collider on the lit bulb: the target is easier to hit than the others.
- Head-locked board: drift and sickness. Body-yaw follow with a glide is the compromise.
- Gain knob: one more concept for no benefit. Scale the pad instead.

---

# What the Numbers Say

Every layer roughly halved the time. Laser hands to Parchment, then Parchment to Phantom. Parchment removes the 3D problem. Phantom removes the reach. Parchment is intuitive on first contact. Phantom takes practice, my wife had a hard time with it, but once learned it is quicker. Live runs are consistently under 3 seconds, the best is 2.17, and the seven-run average is around 2.6.

---

# Known Issue

Pressing Parchment and Phantom back to back, while the unroll is still animating, occasionally hangs the app. Four or five times in dozens of sessions. Workaround: press Parchment, wait for the roll to finish, then press Phantom. Logged for after the showcase.
