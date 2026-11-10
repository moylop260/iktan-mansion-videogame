# ADR-0006: What the text says must be visible in the world

Status: accepted. Origin: the designer's father: "it says the lock is GREEN and you did not make it green".

## Decision
Every colour, material or state named by a message, hint or description must be modelled in the 3D scene:
- Lock colour = key colour: library door green, stair gate blue padlock, office door yellow, front door golden.
  `MakeDoor(..., lockColor)` adds a glowing coloured lock plate (both sides) or a hanging padlock (gate).
- The grandfather clock "stopped at 3:00" shows 3:00 on its dial (`TexGen.ClockFace`).
- A "bathtub" is a hollow tub with a basin; "the shower curtain" is a sliding curtain on a rail.
- Item icons use the item colour.

- A "gate" (reja) is an iron grate of bars you can see through, not a solid slab; its lock is a padlock in the key colour.
- "Table with a diary": an open diary lies on the table, with the year of the safe code (1973) written in red.
- "Unmade bed": rumpled covers thrown back, crooked dented pillows, a corner hanging off; each bed also matches its
  own text (warm, cold with the blanket on the floor, old with a body-shaped hollow and cobwebs).
- Containers show what the text says is inside when opened (`Openable`): dresser = old clothes + sheet music,
  nightstand = batteries, kitchen drawer = flashlight, pantry = expired rusty cans and cobwebs. Taking the item
  removes it from the container (`HideWhenOwned`).
- "The fridge hums loudly and drips something dark": a 3D hum loop and a dark puddle with a falling drop.
- "Your reflection looks a second late": the mirror shows the player's body, delayed one second.
- "Stairs": the stairwell opening must be open (no floor covering over it) and every step solid.

## How to apply
When writing or changing a string, search the builder for the object it names and check geometry, colour and
state. When adding geometry for an item, make sure the descriptions in `GameManager.ItemDesc`,
`Hint()` and the interactables agree with it.
