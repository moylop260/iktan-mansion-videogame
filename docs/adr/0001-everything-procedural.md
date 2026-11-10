# ADR-0001: Everything is procedural

Status: accepted

## Context
The game is built and iterated by an AI with a 13-year-old designer. Binary assets (images, models, audio)
cannot be authored or reviewed in text and bloat the repo.

## Decision
Geometry comes from Unity primitives and custom box meshes with world-scale UVs (`MansionBuilder.BoxMesh`).
Textures are generated in `TexGen` (seamless value noise). Sounds are synthesized in `AudioGen`. Creatures are
built from primitives in `Creatures.cs`. The only binary files in the repo are the notebook photos (`docs/notebook`).

## Consequences
Anyone can read and diff the whole game. Looks are stylised, not photoreal. New content = new code.

## How to verify
No `.png/.fbx/.wav/.mp3` under `Assets/`. `AudioGenTests` and `TexGenTests` cover the generators.
