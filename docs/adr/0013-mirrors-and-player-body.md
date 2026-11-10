# ADR-0013: Real mirrors and the player's body

Status: accepted. Origin: "in the mirror I do not see my reflection".

## Decision
A mirror is a real planar reflection (`MirrorSurface`): a camera placed at the reflected position renders to a
texture with an oblique clip plane just behind the glass, and the glass samples it in screen space (shader
`Mansion/MirrorScreen`, horizontally flipped). It renders only when near and in front of the player. In first
person the player has no visible body, so `PlayerAvatar` builds one from primitives on layers 8 and 9 that the
main camera does not draw: it follows the player, walks (arms and legs swing with distance travelled) and lifts the
flashlight when it is on. Layer 9 holds a copy delayed by one second, used only by the haunted mirror.

## Rules
Keep the mirror frame parts off the glass plane (z-fighting with the clip plane showed as a brown block). Any new
mirror calls `OrnateMirror` (or sets up `MirrorSurface` with the room-side normal).

## How to verify
`-testview 2.2,5.5,0,270` shows the living-room mirror with the player's body inside.
