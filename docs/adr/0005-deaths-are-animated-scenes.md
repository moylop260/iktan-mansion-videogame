# ADR-0005: Deaths are animated scenes with audio, never text

Status: accepted. Requested by the designer's father: "instead of writing the text, put the animation and
sound effects, like a scream, something eerie, like Outlast psychological horror".

## Decision
Each trap calls `GameManager.Die(kind, focus)`; `Cinema` takes over the camera (`PlayerController.SetView`)
and plays a scripted scene: the camera looks at the object, the creature appears, lights go out, the screen
shakes, post-FX pulse (vignette, chromatic aberration, lens), roars/screams/whispers play, then fade to
black and the GAME OVER screen (only the words GAME OVER plus retry buttons).

Rules for scenes: place everything relative to the camera and the object's combined bounds (the player may
stand at any distance; if too close, the player steps back first); keep the creature lit and visible;
never leave the game in `Mode.Death` (a 20 s watchdog ends it); clear overlays before GAME OVER.

## Kinds
`closet`, `wardrobe`, `fridge`, `mirror`, `bed`, `tub`, `pit`, `tv`, `doll`, `alarm`.

## How to verify
`-testdeath <kind>` previews a scene; PlayMode tests run every kind to GAME OVER and fail on any logged error.
