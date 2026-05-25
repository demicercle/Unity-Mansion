# Unity-Mansion

REFACTO
- add namespaces
- clean "CharacterController"
- training mannequin (invicible life)
- NPC should try to flee if no ammo, no health (even triggering opportunity attacks)
- save camera rotation
- do "CursorRaycast" only in FixedUpdate
- if you miss while triggering turn based, should add 1 miss to the gauge
- retake "initative" rules
- tweak (again) range weapon atk modifier
- wood break SFX

ANIMS / VFX
- add anims and VFX/SFX for actions "Run", "Defend"
- check a "drop on ground" and "pickup loot bag" Animation
- display setItemToUse with other items than grenades

SAVE
- 

UI
- popup "are you sure you want to reroll dices?"
- popup "you have no karma points!"
- retake gamelog : should warn about dice launch

ENV ART
- add a "roof" (ceiling plane) to cull sun light, and point light in the buildings

NODES
- 

GAMEPLAY
- is dying state (-1 HP / turn or 6 seconds

ITEMS
- create new items:
	> a "key" to use on a "locked" door
	> "food" item (self heal HP)
	
	
URLs
	https://opengameart.org/content/medieval-props-textured
	https://kaylousberg.itch.io/halloween-bits
	https://kenney.nl/assets/retro-urban-kit
	https://pigeon-gang.itch.io/3d-fences
	
	
<b>First Aid Kit</b>
<color=gray>Usable on any character</color>
Give +5HP to the target. Can be used on dying people.
