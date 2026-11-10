# La Mansión (first-person survival-horror game)

A game by **Iktan Lopez (IkLpz)**, based on his handwritten notebook (see `docs/notebook`).
A Unity 3D project for macOS, in a classic survival-horror style
(Resident Evil / Outlast flavour). The game text and voices are in Spanish.

**Version:** `0.2.0`

## Quick start (how to launch the game)
The built app lives in `Builds/` (git-ignored, so it only exists on the machine that built it).

```sh
cd ~/videojuego_iktan/mansion
open Builds/Mansion.app
```

- If macOS refuses to open it (the app is unsigned): right-click `Mansion.app` → *Open* → *Open*,
  or run `xattr -dr com.apple.quarantine Builds/Mansion.app` once.
- If `Builds/Mansion.app` does not exist (fresh clone or after code changes), build it first with the
  command in *Open and build* below, then run `open Builds/Mansion.app`.
- Alternative: open the project in Unity 6 and press **Play** in the editor.
- Quit the game with **Esc** → pause menu, or **Cmd+Q**.

## How to play
- **WASD** move · **Mouse** look · **Shift** run
- **E** interact · **F** flashlight (needs flashlight + batteries) · **H** hint · **Tab** inventory · **Esc** pause
- **Blue lamps** are checkpoints: walking near one saves your progress (they turn green).
- Open the wrong thing and you get an animated death scene (with sound), then you retry from the
  last checkpoint (R) or start over (N).

## Winning route (spoilers)
Kitchen drawer (flashlight) → bedroom nightstand (batteries) → bathroom sink (green key) → library
(the blue book gives the blue key, never the red one) → stair gate → bedroom dresser (sheet music) →
piano room (flashlight on) → yellow key → office → safe (the year the house was bought, read in the
living-room diary) → golden key → front door.

## Open and build
Both ways produce `Builds/Mansion.app`. Close any other Unity process first, otherwise the project is locked and the build fails.

**From the terminal**
```sh
cd ~/videojuego_iktan/mansion
/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -executeMethod MansionEditor.BuildMac -logFile build.log
grep "Compilación" build.log     # "Compilación OK" means it worked
open Builds/Mansion.app
```
It takes a few minutes. Adjust the Unity version in the path to the one installed (`ls /Applications/Unity/Hub/Editor`).

**From Unity**
1. Unity Hub → *Add project from disk* → this folder, with Unity 6.
2. Menu **Mansion → Compilar para macOS**, or just press **Play** in the editor to play without building.

Preview a death scene directly: launch the app with `-testdeath closet`
(also `wardrobe`, `fridge`, `mirror`, `bed`, `tub`, `pit`, `tv`, `doll`, `alarm`, `closetnear`).

## Tests and coverage
Unity Test Framework: 50 EditMode tests (audio synthesis, textures, data) and 55 PlayMode tests (house structure,
winning route, every death scene, checkpoints, stairs, containers, mirror). Line coverage of the game code is 86.6 %.

```sh
./tools/run-tests.sh      # runs both suites with Code Coverage; report in CodeCoverage/Report/index.html
```
Close any other Unity process first.

## Versioning
The version lives in `ProjectSettings/ProjectSettings.asset` (`bundleVersion`) and in this README,
and is bumped with [bump2version](https://github.com/c4urself/bump2version), which also commits and tags:

```sh
bump2version patch   # 0.1.0 -> 0.1.1
bump2version minor   # 0.1.0 -> 0.2.0
git push origin main --tags
```
