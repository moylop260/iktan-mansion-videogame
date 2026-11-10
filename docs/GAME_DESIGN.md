# La Mansión: game design

Source: the notebook of Iktan Lopez (IkLpz), `docs/notebook/`. Interpreted and extended with him.

## Premise
You buy a cheap old mansion. The previous owner, Javier, disappeared. The house is full of traps; the way out
is the golden key in the safe of the second-floor office. Notebook structure: "La Mansión, 46 pages,
5 endings, 14 game overs": every wrong choice is a different unexpected ending and you start again until you
choose the right path.

## Rooms (24 x 16 m, two floors, x to the right, z forward)
| Floor | Rooms |
|---|---|
| 1 | Living room (Sala) · Foyer (Vestíbulo, grand stair, front door) · Kitchen · Bedroom 1 (Cuarto 1) · Bathroom (Baño) · Library (Biblioteca) |
| 2 | Bedroom 3 · Hallway (stair landing) · Bedroom 4 · Piano room (dark) · Office |

## Items, keys and locks (colour of the lock = colour of the key, rule ADR-0006)
| Item | Where | Used for |
|---|---|---|
| Linterna | kitchen counter drawer | light (needs Pilas) |
| Pilas | bedroom 1 nightstand | powers the flashlight (F) |
| Llave verde | bathroom pedestal sink | **green lock** on the library door |
| Llave azul | library, blue book | **blue padlock** on the stair gate |
| Partituras | bedroom 3 dresser | play the piano |
| Llave amarilla | piano (flashlight on, with the sheet music) | **yellow lock** on the office door |
| Llave dorada | office safe, code 1973 | **golden lock** on the front door (win) |

Safe code: 1973 = the year Javier bought the house, written in the living-room diary
("Compré esta casa en 1973"). Wrong codes: 1937, 1793.

## Winning route
Kitchen drawer → bedroom 1 nightstand → bathroom sink → library door (green) → blue book → stair gate (blue) →
bedroom 3 dresser → piano room (flashlight on) → piano → office door (yellow) → safe (1973) → front door.

## Traps (every one ends in an animated death scene, `Cinema.cs`)
| Kind | Trigger | Scene |
|---|---|---|
| `mirror` | living-room mirror | glass cracks, pale arm pulls you into the reflection |
| `fridge` | kitchen fridge | cold light, giant spider leaps at your face (notebook: "the fridge") |
| `bed` | bedroom 1 bed | nightmare, red fade, heartbeat, pale face above you |
| `closet` | bedroom 1 closet | doors open, eyes in the dark, long arms, beast roar, dragged in |
| `tub` | bathroom shower curtain | the curtain slides open, a soaked figure rises and lunges |
| `pit` | library red book | floor opens, you fall screaming |
| `tv` | bedroom 3 TV | static, lights out, silhouette crawls out |
| `doll` | piano-room doll | head spins, giggle, jumps at you |
| `wardrobe` | bedroom 4 wardrobe | tall red-eyed figure steps out and lifts you |
| `alarm` | wrong safe code | siren, red strobe, blackout, running footsteps, eyes in the dark |

## Systems
- **Checkpoints**: blue lamps (turn green when active) save items, opened doors, position and death count.
- **Game over**: retry from last checkpoint (R / button) or restart (N). Only the words GAME OVER are shown.
- **Interaction**: aim and press E; choices appear in a bottom dialog (W/S/arrows + Enter, mouse, or 1-3).
- **Inventory** (Tab) with icons and descriptions; **hint** (H); **room name banners**; **item cards**.
- **Audio**: low drone, random whispers/creaks/drips/thuds/giggles every 16-38 s, footsteps, heartbeat in
  scares, roars and screams in deaths. All synthesized (`AudioGen`).

## Backlog from the notebook (not built yet)
Third floor (rooms 5, 6, 7, cinema, disc room, attic with the music box and cassette), basement (ritual:
salt, candles, lighter, blood, formula in the fridge), the three "nights" with window-closing rushes, golf
course, forest, cemetery with Javier's tomb, the labyrinth dream, the 5 endings and 14 game overs.

## Authorship
Original idea, story, rooms, traps and endings: **Iktan Lopez (IkLpz)**.
