# Parchment — HW1 plan

Technique name: **Parchment**. What it does: the **Parchment Transform**.
Assignment: HCC 552 Homework 1, 3D sphere selection. In-class showcase: Sept 30.

Phases have exit tests, not dates. Move on when the test passes. Run several in a sitting when it's going well.
Phases 0–2 are the floor. Phase 3 is the assignment as designed. Phases 4–6 are how good it gets.

## The two invariants (never break these)

1. One pinch selects exactly one sphere.
2. The selection logic never reads which sphere is the target. It must behave identically with the highlight turned off.

Also: uniform hit areas on all bulbs; the real cube never moves after placement.

---

## Phase 0 — Environment

- [ ] Mac mini matches the MacBook: Unity 6000.6.0f1, Android Build Support (OpenJDK, SDK/NDK), same packages.
- [ ] Tutorial project in Git with a Unity `.gitignore`. Mini and laptop both clone and open cleanly.
- [ ] Meta XR Simulator installed on the mini (Apple Silicon only) and Play works in it.
- [ ] Meta Quest Developer Hub installed on both machines, headset shows up as a device.
- [ ] Developer Mode confirmed on the loaner headset.

**Exit:** an APK built on the mini runs on the headset, AND an APK built on the laptop runs on the headset.

## Phase 1 — Skeleton (spec without a technique)

- [ ] Cube placed 1.0 m ahead, 1.5 m up, yaw only, world-locked after init.
- [ ] 100 spheres loaded from `sphere_coordinates.txt` as cube children. localPosition = xyz, localScale = diameter.
- [ ] Sequence file loaded from device path, bundled fallback copy. Tolerant of blank lines and whitespace.
- [ ] Trial manager: highlight only current target; correct advances; wrong is ignored; next sequence; done.
- [ ] Timer: starts on first correct, stops on seventh. Per-target splits. Error count.
- [ ] On-screen readout and log file on device.
- [ ] Editor mouse rig: click spheres in the editor to drive the whole state machine without a build.

**Exit:** in the editor, click through seven targets with the mouse and see a time print and a log line.

## Phase 2 — Fallback (spec passed on hardware)

- [ ] Input layer: two questions only — where is the pointer, did it just confirm. Hands implementation first.
- [ ] Own pinch detector: hysteresis (different on/off thresholds) and a short cooldown. Fires once per pinch.
- [ ] Hand ray on the cube, pinch selects the first sphere hit. Light low-pass filter on the ray.
- [ ] Rolled-parchment state = this mode.

**Exit:** full sequence completed on the headset with hands, time printed. **Must be done before the showcase no matter what.**

## Phase 3 — Parchment core

- [ ] Parchment object: grab, unroll, park (world-locked, regrab to move), tilt ~30°.
- [ ] Scan: builds the bulb→sphere table by layout, never by sequence.
- [ ] 100 bulbs on the slab, ID-sorted 10×10, faint alternating row tint. Bulb ~3 cm, gap ~1 cm.
- [ ] Bulb states: dim gray idle, red = current target, green = target and hovered. Nothing on wrong pinch.
- [ ] Hover mode: hand XY projected onto the parchment plane, Z ignored, gain 1.0.
- [ ] Snap-to-nearest-bulb with hysteresis (stays on a bulb until you've crossed a margin toward a neighbor).
- [ ] Two hands, two reticles (go stones, black L / white R, thin contrasting rims, smaller than a bulb).
- [ ] Pinch on hand X selects the bulb hand X is on.
- [ ] Click sound on every pinch, distinct tone on correct. Zero-delay transition to next target.
- [ ] Unrolled = parchment mode, rolled = cube mode.

**Exit:** full sequence on the headset via the parchment, faster than Phase 2's best time.

## Phase 4 — Speed pass

- [ ] Gain knob (start 1.0, test 1.5). Bulb size test (3 vs 4 cm). Snap margin and pinch thresholds tuned.
- [ ] Fingertip-only hand rendering. Current target number readout. Seven progress dots. Timer hidden until end.
- [ ] Ray mode as a comparison, only if hover feels wrong.
- [ ] One-line onboarding on the parchment: "Hover a finger over the red bulb, then pinch."

**Exit:** best time roughly halved from Phase 3's first run; a novice completes a sequence with the one-line instruction and no help.

## Phase 5 — Arcade

- [ ] Start screen on the rolled parchment: Start / Leaderboard / Settings.
- [ ] Settings: mode, gain, bulb size, hands/controller, fallback toggle.
- [ ] Initials: three A–Z dials, hover + pinch to advance, OK stone to lock. Defaults to last used.
- [ ] Leaderboard keyed by sequence set (file name or hash), time to the hundredth.
- [ ] Controller implementation of the input layer (only if Khan allows controllers).
- [ ] Unroll / sink / ink animation. Confetti on seventh.

**Exit:** someone else picks up the headset cold, runs it, puts their initials on the board.

## Phase 6 — Showcase hardening

- [ ] Laptop pulls latest, builds, deploys, full run passes.
- [ ] Headset screen recording of a full run as backup.
- [ ] 90-second explanation rehearsed (see DECISIONS.md, "Positioning").
- [ ] No new code after this passes.

**Exit:** laptop-built APK on the headset passes a full run the night before.

---

## Questions for Khan (Monday)

Ask these. None of them reveal the technique.

1. How does the showcase sequence file get onto the headset? USB stick, adb push, which path, which filename (`test_sequence.txt` vs `test_sequences.txt`)?
2. Grading rubric: novelty vs correctness vs speed? Is there a cross-student time comparison?
3. "Print" the time where: on-screen, log file, both?
4. Still hands-only for the showcase, or are controllers okay?
5. Interaction SDK primitives (hand pose, pinch detection) are fine as building blocks, right?
6. Is stepping toward the cube allowed?
7. Individual or pairs?
8. Anything expected at showcase beyond the demo (short explanation, backup video)?
