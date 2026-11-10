# La Mansión (first-person survival-horror game)

An original idea by Iktan. A Unity 3D project for macOS, in a classic survival-horror style
(Resident Evil / Outlast flavour). The game text and voices are in Spanish.

**Version:** `0.1.0`

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
1. Unity Hub → *Add project from disk* → this folder, with Unity 6.
2. Menu **Mansion → Compilar para macOS** builds `Builds/Mansion.app`, or press Play in the editor.

Command line:
`"/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity" -batchmode -quit -projectPath . -executeMethod MansionEditor.BuildMac -logFile build.log`

Preview a death scene directly: launch the app with `-testdeath closet`
(also `wardrobe`, `fridge`, `mirror`, `bed`, `tub`, `pit`, `tv`, `doll`, `alarm`, `closetnear`).

## Versioning
The version lives in `ProjectSettings/ProjectSettings.asset` (`bundleVersion`) and in this README,
and is bumped with [bump2version](https://github.com/c4urself/bump2version), which also commits and tags:

```sh
bump2version patch   # 0.1.0 -> 0.1.1
bump2version minor   # 0.1.0 -> 0.2.0
git push origin main --tags
```
