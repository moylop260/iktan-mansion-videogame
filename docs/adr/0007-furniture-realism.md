# ADR-0007: Furniture looks like real house, hotel and mansion objects

Status: accepted. Origin: "it looks like a box, it is not even a clock"; "the tub is not concave with a
bottom for the water"; "sweep every element and be very detailed".

## Decision
Every fixture is modelled like its real counterpart with real proportions (seat 0.45 m, table 0.75 m,
mattress 0.55 m, counter 0.9 m) and the small details that identify it: handles, knobs, hinges, mouldings,
feet, cushions with piping, pillows, drawer fronts, labels, dials. Pieces are groups of parts
(`Group` + `BoxIn`) built in `MansionBuilder.Furniture.cs` from a local frame (x right, y up, z facing).
Solid parts collide; decoration does not. Keep each piece inside its footprint (+/- 5 cm; handles up to 6 cm)
and the front face of wardrobes, closets and the fridge flat (death scenes overlay the opening there).
Budget: about 150 objects per piece.

## How to verify
Look at it in the running app at 2 m. PlayMode tests check the group structure (colliders, names).
