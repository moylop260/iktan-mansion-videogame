# ADR-0012: Every room looks different

Status: accepted. Origin: "decorate each room in a different colour so I know I am in a different one; it can be
confusing, it looks like I am in the same room".

## Decision
Each room has its own wallpaper or tile hue, floor material and light colour, and its name appears as a banner
when entering. Current palette: foyer wine red + marble; living room green + wood + firelight; kitchen yellow tiles
+ black and white checker + bright white light; bedroom 1 blue + blue carpet + cool light; bathroom aqua tiles +
cyan light; library oak panels + red carpet + amber light; second-floor hallway violet; bedroom 3 pink + pink carpet;
piano room black and crimson (dark, flashlight needed); bedroom 4 terracotta; office slate green.
New rooms must pick a hue and a floor not yet used.

## How to verify
Look at two adjacent rooms from a doorway; `-testview` opens the game at any spot.
