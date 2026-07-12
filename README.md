
------------------------------
## Secure Cooking Pots (No Pot Spillage Fix)
Have you ever accidentally ruined a/many batch of glue just because you picked up the cooking pot from the firepit before emptying it?
I sure did. And I didn't like it.

## What it does
Instead of spilling your precious resources into the ground, this mod safely locks the cooking container slot inside the firepit GUI if it contains any liquid.

Attempting to remove a fluid-filled pot:

- triggers a red background flash on the item slot
- plays a "nope" sound based on player voice
- inflicts a small amount of burning damage and plays a hiss sound if too hot
- ~~displays a helpful chat notification~~ removed

If your pot contains solid food (like carrots, meat) or ores for smelting, you can still remove the container freely.

## Compatibility
* Compatible with modded firepits and custom cooking containers.
* Works seamlessly with modded liquids (as long as they utilize the native WaterTightContainableProps system).

## Installation

* Side: Universal (Required on Server, Optional but highly recommended on Client for the visual and sound effects).

## Feedback
Make noise on the discord channel under mods > Furio's Mods if you think this should be native behaviour.

------------------------------
