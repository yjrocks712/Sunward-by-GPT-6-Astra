# Build and verify Sunward

I built the current release with Unity **6000.6.0f1**, Universal Render Pipeline, and the Input System. Use the matching Editor version first; package versions are pinned in `Packages`.

## Build on macOS

Open `Assets/Sunward/Scenes/Sunward.unity`, then select **Sunward → Build macOS player**. The build is written to `outputs/Sunward.app`.

The equivalent batch command, run from the project root with Unity closed for this checkout, is:

```sh
"/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity"   -batchmode -quit -projectPath "$PWD"   -executeMethod Sunward.Editor.SunwardBuild.BuildMac   -logFile "$PWD/build.log"
```

Choose the desired Mac architecture in Build Profiles before creating Intel or universal builds. The distributed v0.1.0 binary is ARM64. Windows needs its own installed build support module and a Windows Build Profile with the Sunward scene. I have not verified those additional targets.

## Make the DMG

```sh
tools/package-mac.sh
```

Requires Python 3 and Xcode command-line tools. This creates `outputs/Sunward.dmg` with the app, license notices, and an Applications shortcut. The packaged app is ad-hoc signed; Apple Developer ID signing and notarization require a separate setup.

## Run the included player checks

These flags are dormant during normal play. Run them against a development test profile: they drive the UI and, in the photo check, start and abandon a race. The app exits when each run finishes.

```sh
mkdir -p work/verify-photo
open -n outputs/Sunward.app --args   -screen-fullscreen 0 -screen-width 1600 -screen-height 900   -sunward-photo-verify "$PWD/work/verify-photo"   -logFile "$PWD/work/verify-photo/player.log"
```

Use `-sunward-radio-verify` for radio checks or `-sunward-verify` for the startup, scene, shader, and menu checks. Always give a separate absolute output directory. Reports use `passed`, `checks`, and `errors` where applicable. Check the player log too, including shutdown after the report is written.

The radio audit temporarily changes audio settings and restores its saved volume/station selection. The photo audit tests menus, camera controls, exports, and race pause/resume. Use a separate test user profile or back up your game preferences before changing these audits.


— **Gpt-6 Astra**
