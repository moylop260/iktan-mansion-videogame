---
name: mansion-game-design
description: Use this skill whenever you change, extend, review or test "La Mansión", the Unity 6 first-person survival-horror game by Iktan Lopez (IkLpz). It holds the game design rules (ADRs), the room/key/trap map, the visual-consistency rule (what the text says must be visible in the world), the death-scene and audio conventions, the test and release workflow, and where every piece of code lives. Triggers on: "la mansión", "mansion game", "iktan", "IkLpz", "death scene", "escena de muerte", "checkpoint lamp", "closet/refri/espejo trap", "add a room", "add a trap", "furniture", "mueble", "cerradura", "bump2version", "Cinema.cs", "MansionBuilder".
---

# La Mansión: design guide for AI agents

Read this first, then the file you need. Everything in the repo is meant to be read by another AI: names
are literal, rules are numbered, and each rule says *why* and *how to check it*.

## What the game is
A first-person survival-horror game in a two-floor mansion (Resident Evil mansion look, Outlast-style
psychological audio). The player explores, collects items and keys, and opens things. **Opening the wrong
thing triggers an animated death scene** (closet, fridge, mirror...), then the player retries from the last
checkpoint. It was designed by a 13-year-old, **Iktan Lopez (IkLpz)**, in a handwritten notebook
(`docs/notebook/`). Game text and voices are Spanish; code, comments, docs and commits are English.

## Document map (read in this order)
| Need | File |
|---|---|
| Story, rooms, keys, traps, route, notebook backlog | `docs/GAME_DESIGN.md` |
| Why things are the way they are (rules) | `docs/adr/*.md` (index in `docs/adr/README.md`) |
| How to play, build, version | `README.md` |
| Original source material | `docs/notebook/` (14 HEIC photos) |

## Code map (`Assets/Scripts`, assembly `Mansion.Runtime`)
| File | Responsibility |
|---|---|
| `GameManager.cs` | state machine (`Mode`), checkpoints/save, game over, menus and all IMGUI, post-processing |
| `MansionBuilder.cs` | builds the whole house, defines every interactable, key, door and trap (rooms live here) |
| `MansionBuilder.Furniture.cs` | detailed furniture builders (grouped parts), partial class of `MansionBuilder` |
| `Cinema.cs` | the death-scene director (one coroutine per trap kind) |
| `Creatures.cs` | procedural monsters (`Mon.Figure`, `Spider`, `Doll`, `PaleArm`) |
| `AudioGen.cs` / `AudioManager.cs` | synthesized sounds / playback, ambient drone, random scares |
| `TexGen.cs` (+ `TexGen.Extra.cs`) | procedural textures and inventory icons |
| `PlayerController.cs`, `Inp.cs`, `Interactable.cs`, `Door.cs`, `Flicker.cs` | movement, input wrapper, interaction, doors, light flicker |
| `Assets/Editor/MansionEditor.cs` | URP setup, scene, macOS build menu |
| `Assets/Tests/{EditMode,PlayMode}` | automated tests |

## Non-negotiable rules (details in the ADRs)
1. **Text must match the world** (ADR-0006). If a message says "the lock is green", there must be a visibly
   green lock plate; if a clock "is stopped at 3:00", the hands show 3:00. Every colour or object a message
   names must be modelled, in that colour. Check every new string against the geometry.
2. **Deaths are scenes, never text** (ADR-0005). A trap calls `gm.Die(kind, focusObject)`; `Cinema` plays a
   camera + creature + audio sequence. Do not add explanatory death messages.
3. **Everything is procedural** (ADR-0001): no image, model or audio files. Textures in `TexGen`, sounds in
   `AudioGen`, geometry from primitives/box meshes.
4. **Furniture must look like the real thing**, in great detail (ADR-0007): proportions of real house/hotel/
   mansion objects, handles, hinges, mouldings, feet, cushions, labels. A box is not a clock/tub/bed.
5. **A prop with several parts is a group** (`Group`/`BoxIn`), with the `Interactable` on the group and
   colliders only on solid parts. Death scenes read the group's combined renderer bounds.
6. **One change per commit, pushed to GitHub** (ADR-0010). Versions with `bump2version`.
7. **Tests with every behaviour change** (ADR-0009); keep `EditMode` and `PlayMode` green.
8. Game content is Spanish; every artifact (code, docs, commits, PRs) is English.

## Recipes
- **Add a trap**: build the prop group in `MansionBuilder` (named, with `Interactable.Add` + options), call
  `gm.Die("<kind>", propGroup)`, add a `case` and coroutine in `Cinema.Run`, add the kind to
  `GameManager.TestDeath`, extend `docs/GAME_DESIGN.md` (traps table), add a PlayMode test, preview it with
  `-testdeath <kind>`.
- **Add an item/key**: `Take(...)`/`gm.Give(...)`, description in `GameManager.ItemDesc`, icon in
  `TexGen.BuildIcon`, hint in `GameManager.Hint()`, lock/door colour matching the key (rule 1).
- **Add a room**: walls with `Wall(...)` (two surfaces), `Room(...)` (floor, light, zone name), furniture
  builders, a window or two, a checkpoint lamp if the room is a milestone.
- **Preview**: `Builds/Mansion.app/Contents/MacOS/"La Mansion" -testdeath closet` (the game keeps running
  without focus in this mode).

## Commands
```sh
UNITY=/Applications/Unity/Hub/Editor/<ver>/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -nographics -quit -projectPath . -executeMethod MansionEditor.BuildMac -logFile build.log
./tools/run-tests.sh            # EditMode + PlayMode with code coverage
bump2version patch|minor        # bumps ProjectSettings + README, commits and tags
git push origin main --tags
```
Close all Unity processes before running another Unity command on the same project.

## Pitfalls already hit
- No render pipeline on Unity 6.6 gives a **pink screen**: URP asset + base materials are created by `MansionEditor`.
- Packages must match the editor: Input System 1.11.x and Code Coverage 1.2.x do not compile on 6.6.
- Full-screen overlays are drawn after every screen: clear the fade before showing GAME OVER.
- The game pauses without focus; screenshot tests need `-testdeath` (it forces run-in-background).
