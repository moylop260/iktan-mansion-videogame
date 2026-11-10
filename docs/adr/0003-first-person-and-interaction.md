# ADR-0003: First-person view and graphic interaction

Status: accepted

## Decision
First-person POV, WASD + mouse, Shift to run, **E** to interact, **F** flashlight, **H** hint, **Tab**
inventory, **Esc** pause. Interaction is Resident-Evil-like: a bottom dialog with gold frame and serif font,
options chosen with W/S/arrows + Enter, mouse or digits; item pickup cards with icons; room-name banners;
an inventory grid with descriptions. `Interactable.options` is computed at use time so choices depend on the
inventory. Single-option interactables run immediately.

## How to verify
PlayMode tests walk the whole route through the interactables.
