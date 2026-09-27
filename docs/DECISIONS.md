# Parchment — decisions

Every design decision, with status. Build / Later / Drop.

## Positioning (the 90-second version)

Hand rays are the weakest pointer for 2 cm targets a meter away. Instead of improving the ray, Parchment removes the 3D problem: during untimed setup, the 100-sphere cube is projected once onto a 2D parchment of bulbs, one bulb per sphere. Every selection is then a hover and a pinch on a flat panel, which is what hands are good at. Related work: Expand (Cashion et al. 2012) and SQUAD (Kopper et al. 2011) refine per selection, on demand. Parchment refines once, in setup, so every selection is a single 2D press. Lineage from lecture: World-in-Miniature / Voodoo Dolls, reducing degrees of freedom, hysteresis, Uncertain Pointer feedforward.

## Rules we are designing inside

| Rule | What it forbids | What it allows |
|---|---|---|
| Explicit confirmation | Hover/gaze/dwell select; one action selecting many | One pinch → one sphere |
| Equally selectable | Disabling non-targets; bigger hit area on the target; any logic that reads target identity | Reading the layout, the hand, the field of bulbs |
| Cube fixed | Moving/scaling the cube | Copies, proxies, the user moving |

Self-test for any new idea: would it behave differently with the highlight turned off? If yes, it reads the target. Drop it.

## Decision table

| Area | Decision | Status |
|---|---|---|
| Technique | Parchment: 3D→2D proxy built once in untimed setup | Build |
| Name | "Parchment"; the transform is the "Parchment Transform" | Build |
| Setup | Rolled at start, grab to unroll, hold toward cube, free-hand pinch to scan | Build |
| Setup | Scan maps by layout, never by sequence | Build |
| Setup | Park world-locked, regrab to move; tilt ~30° | Build |
| Setup | Re-loadout between sequences | Build |
| Setup | Unroll / sink / ink animation | Later |
| Layout | ID-sorted 10×10; depth slices only if find is slow | Build |
| Layout | Bulb ~3 cm, gap ~1 cm, panel ~40 cm; test 3 vs 4 | Build |
| Layout | Faint alternating row tint | Build |
| Layout | Grid within ~25° of view | Build |
| Bulb states | Dim gray / red target / green target+hovered | Build |
| Bulb states | Wrong-pinch flash | Drop |
| UI | Current target number on parchment | Build |
| UI | Seven progress dots | Build |
| UI | Timer hidden during run, shown at end | Build |
| UI | Confetti on seventh | Later |
| Pointing | Hover mode: hand XY projected, Z ignored, gain 1.0 default | Build |
| Pointing | Ray mode for comparison | Build if needed |
| Pointing | Control-display gain knob | Build |
| Pointing | Light low-pass filter | Build |
| Pointing | Snap-to-nearest-bulb with hysteresis | Build |
| Pointing | Lock that fires on target match | Drop (reads target) |
| Pointing | Two hands, alternate freely | Build |
| Reticles | Go stones, black L / white R, contrasting rims, smaller than bulb | Build |
| Reticles | Speed-scaled reticle | Later |
| Confirm | Own pinch detector, hysteresis + cooldown, per hand | Build |
| Confirm | Click on pinch, tone on correct | Build |
| Confirm | Zero-delay next-target transition | Build |
| Hands | Fingertip dots only | Build |
| Fallback | Rolled = cube mode: hand ray + pinch on spheres | Build |
| Input | Input layer: pointer pose + confirm event; hands first, controller later | Build |
| Logging | Per sequence: total, splits, errors, mode, sequence-set id | Build |
| Arcade | Start / Leaderboard / Settings on rolled parchment | Build (Phase 5) |
| Arcade | Three-dial initials, defaults to last used | Build (Phase 5) |
| Arcade | Leaderboard keyed by sequence set | Build (Phase 5) |
| Parked | Drones (Ninja Hands), rifle ammo types, bombs, region voodoo-doll copy | Drop for HW1; possible project material |

## Ideas rejected and why (so we don't re-argue them)

- Sweep/wipe selection, exploding charges, plunger chains: one action, many selections.
- Heat-seeking drones, lock-on-match, pierce-until-red: logic reads the target.
- Bigger collider on the lit bulb: target easier to select than non-targets.
- Body-locked or head-locked parchment: drift and sickness; world-locked is simpler and we don't move during a run.

## Phantom (extension of Parchment)
- Absolute mapping: phantom = sheet center + gain × (fingertip − sheet center), in-plane axes only.
  Depth axis stays 1:1 so poke works through the phantom finger. Clamped to sheet bounds.
- Gain default 2.0. Settings: 1.0–10.0, coarse ±0.5 and fine ±0.1 nudge buttons (slider post-showcase).
- Ghost hand = real hand mesh baked per frame, translated so index tip sits at the gained point.
  Reticle stays under the phantom fingertip. Fallback if the hand renderer fights: flat hand outline.
- Real hand dimmed with a ghostly violet glow while Phantom is active (via HandTint).
- Both hands, symmetric.
- IsOn = wanted. Effective only while Parchment is unrolled (ParchmentMode.Ready); button stays green
  when rolled up, phantom hidden, comes back on unroll.
- Per-trial log: search (target appears → hover start) and move (hover start → confirm).
- Grenade: dropped. README future work only.
