# Unity-Mansion


- disable save&quit in battle
- "prepare" action
- display cursor text "attack will miss" or "attack will hit" 
- min time between assigns
- randomize hp on game seed 
- zombi 7 stuck in patrol after load
- zombi can attack from below

- "miss" gauge
- save dodge dice for later


REFACTO
- add namespaces
- clean "CharacterController"
- retake "door breaks trigger" with enemy tag (zombi will destroy, others will open...)
- dialog line reference bug in editor

ANIMS / VFX
- add anims and VFX/SFX for actions "Run", "Defend"
- add a "drop on ground" and "pickup loot bag" Animation
- damage VFX (blood)

SAVE
- brain state
- door / destructible state
- reroll dice points

UI
- popup "are you sure you want to reroll dices?"
- popup "you have no karma points!"
- display zombies hp to the player?
- retake gamelog : should warn about dice launch

ENV ART
- add a "roof" (ceiling plane) to cull sun light, and point light in the buildings

NODES
- retakes "scene reference" with a list of references in GameScript

GAMEPLAY
- tweak "range to hit chance" and take height into account
- grenade should alert damaged enemy
- "prepare attack" Action
- check can fire pistol through walls
- check clicks on "IsWaiting" while real time
- assign attack : should update destination if target moves

ITEMS
- equip, drop and equip weapons
- pistol ammo
- create new items:
	> a "key" to use on a "locked" door
	> "Soda" (give 1 extra action next turn)
	> "Chicken" (heal HP)
	> replace "Bandage" by "First aid kit" (to revive characters)
	> "Grigri" (roll the dice for free, but cost 1 action, unlimited uses)
	
URLs
	https://opengameart.org/content/medieval-props-textured
	https://kaylousberg.itch.io/halloween-bits
	https://kenney.nl/assets/retro-urban-kit
	https://pigeon-gang.itch.io/3d-fences