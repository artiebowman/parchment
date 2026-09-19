# Parchment — Phase 0 setup

Mirrors Meta's "Unity Hello World for Meta VR devices" tutorial, plus Git and the Mac-specific tools. Do it on the Mac mini; the laptop already has the tutorial done, so it only needs the tools and the clone.

## A. Mac mini — match the laptop

1. Unity Hub → Installs → confirm **6000.6.0f1** is installed with **Android Build Support** (OpenJDK, Android SDK & NDK Tools). Add the module if missing.
2. Unity Asset Store (browser, logged in): **Meta XR Core SDK** and **Meta XR Interaction SDK** → Add to My Assets. (Already done on the laptop account; if it's the same Unity account, skip.)

## B. Repo (do this on the laptop, where the tutorial project lives)

1. Install Git if needed. In the tutorial project folder:
   - `git init`
   - add a Unity `.gitignore` (the standard github/gitignore Unity template: ignores `Library/`, `Temp/`, `Logs/`, `Obj/`, `Build/`, `UserSettings/`, `*.apk`).
   - Set Unity to text serialization and visible meta files (Edit → Project Settings → Editor → Asset Serialization: Force Text; Version Control: Visible Meta Files). These are usually already defaults.
   - `git add -A && git commit -m "tutorial project"`
2. Create a private GitHub repo `parchment`, push.
3. Open in VS Code. In Unity: Edit → Preferences → External Tools → External Script Editor: Visual Studio Code. Install the "Visual Studio Code Editor" package from Package Manager if Unity asks.

## C. Mac mini — clone and open

1. `git clone` the repo. Open in Unity Hub with **6000.6.0f1**. First open rebuilds `Library/` — several minutes on the mini, longer on the laptop.
2. Package Manager → My Assets → confirm Meta XR Core SDK and Interaction SDK are installed (they come in via `Packages/manifest.json`; if not, install from My Assets).
3. Meta XR Tools → Project Setup Tool → Fix All / Apply All (standalone and Meta tabs).
4. Project Settings → XR Plug-in Management → Project Validation → Fix All / Apply All.
5. Project Settings → XR Plug-in Management → OpenXR → Android tab → Meta XR feature group: Meta XR Feature, Foveation, Subsampled Layout enabled.
6. File → Build Profiles → Meta Quest is the active platform.
7. Commit anything the setup tool changed.

## D. Mac mini — simulator (Apple Silicon only, so mini only)

1. Download **Meta XR Simulator** for macOS (ARM) from the Meta downloads page. Install.
2. Meta XR Tools → Meta XR Simulator → Activate. Press Play. You should see the tutorial scene in the simulator window and be able to move the simulated hands.

This is your fast loop: no build, no cable.

## E. Both machines — deploy to the real headset

1. Install **Meta Quest Developer Hub (MQDH)** on both machines.
2. Headset: Developer Mode on (via Meta Horizon phone app → headset → Headset Settings → Developer Mode). If the toggle isn't there because of the admin account, this is the Monday question for Khan / the tech department.
3. USB-C cable to the machine. In the headset, allow USB debugging, "Always allow from this computer."
4. MQDH → Device Manager → headset shows as connected.
5. Unity: File → Build Profiles → Build. Save the APK. Drag it onto the device in MQDH (or Build And Run from Unity).
6. In the headset: App Library → Unknown Sources → run it.

**Phase 0 exit:** the tutorial cube runs on the headset from an APK built on the mini, and from an APK built on the laptop.

## Notes

- Never let the laptop's first `Library/` rebuild happen the morning of the showcase. Do the laptop clone-and-build early and again after any big package change.
- `Packages/manifest.json` is the source of truth for which packages a project has. If the two machines disagree, diff that file.
- Sequence file on device: `Application.persistentDataPath` on Quest is `/sdcard/Android/data/<package>/files/`. That's where the showcase file goes via `adb push` or MQDH's file manager. Bundle a fallback copy in `StreamingAssets`.
