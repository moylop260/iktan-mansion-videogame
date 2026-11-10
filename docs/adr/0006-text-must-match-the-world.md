# ADR-0006: What the text says must be visible in the world

Status: accepted. Origin: the designer's father: "it says the lock is GREEN and you did not make it green".

## Decision
Every colour, material or state named by a message, hint or description must be modelled in the 3D scene:
- Lock colour = key colour: library door green, stair gate blue padlock, office door yellow, front door golden.
  `MakeDoor(..., lockColor)` adds a glowing coloured lock plate (both sides) or a hanging padlock (gate).
- The grandfather clock "stopped at 3:00" shows 3:00 on its dial (`TexGen.ClockFace`).
- A "bathtub" is a hollow tub with a basin; "the shower curtain" is a sliding curtain on a rail.
- Item icons use the item colour.

## How to apply
When writing or changing a string, search the builder for the object it names and check geometry, colour and
state. When adding geometry for an item, make sure the descriptions in `GameManager.ItemDesc`,
`Hint()` and the interactables agree with it.
