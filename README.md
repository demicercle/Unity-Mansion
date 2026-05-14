# Unity-Mansion

REFACTO
- add namespaces
- clean "CharacterController"
- retake "raycast for destructible" (zombi will destroy, others will open...)
- dialog line reference bug in editor
- door nav obstacle
- training mannequin (invicible life)
- NPC should try to flee if no ammo, no health (even triggering opportunity attacks)
- save camera rotation
- do "CursorRaycast" only in FixedUpdate
- if you miss while triggering turn based, should add 1 miss to the gauge
- when zombi about to attack, should display if "dice will hit"
- assign attack : should update destination if target moves: "assign follow transform"
- check "min time between assigns"
- wood break SFX
x opportunity attacks through walls + should trigger only once (retake logic/conditions)
x try to reach dest taking wall
- cutscene: if zombi 7 does not detect players, then should patrol or move

ANIMS / VFX
- add anims and VFX/SFX for actions "Run", "Defend"
- add a "drop on ground" and "pickup loot bag" Animation
- display setItemToUse with others than grenades

SAVE
- travel to next level?

UI
- popup "are you sure you want to reroll dices?"
- popup "you have no karma points!"
- retake gamelog : should warn about dice launch

ENV ART
- add a "roof" (ceiling plane) to cull sun light, and point light in the buildings

NODES
- retakes "scene reference" with a list of references in GameScript

GAMEPLAY
- is dying state (-1 HP / turn or 6 seconds

ITEMS
- equip, drop and equip weapons
- create new items:
	> a "key" to use on a "locked" door
	> "Chicken" (heal HP)
	> "Bag of chips" starts with 1d20 chips inside
	
	
URLs
	https://opengameart.org/content/medieval-props-textured
	https://kaylousberg.itch.io/halloween-bits
	https://kenney.nl/assets/retro-urban-kit
	https://pigeon-gang.itch.io/3d-fences
	
	
<b>First Aid Kit</b>
<color=gray>Usable on any character</color>
Give +5HP to the target. Can be used on dying people.
