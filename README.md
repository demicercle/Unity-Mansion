# Unity-Mansion

- add namespaces

- Talk Action
- DebugGUI Draw Character stats (movePoints, etc)
	> Right Click editor selects character under mouse
- "PartyChanged" event for Character, allowing to remove LateUpdate in Portraits, Agent drawPath
- tweaks! selection circle alpha when AI turns
- if you end turn during a PC movement, it stops
- custom asset post processor
- update color of nav target depending on the action (red for enemies)
- zombi timedout should stop the current turn
- door nav rebuild
- zombi should rotate on scream
- zombi scream should be linked to ai detection and not weapon ready?
- "round" logic (characters rest on start round)
- rename "Handlers" in "Listeners"
- cmd "kill_all"
- game turn manager "PartyRest" on NewTurn, and then Rest each otherCharacter one by one after
- clean "CharacterController"
- heal / damage VFX

dev-level
- enemy take damage: should switch chase target to closest one

NullReferenceException: Object reference not set to an instance of an object
GameInput.OnDisable () (at Assets/_Game_/Scripts/Core/GameInput.cs:162)


PC_Player_2 (CharacterController) !HasMovePoints
UnityEngine.Debug:Log (object)
CharacterRoutines/<WaitCharacterMoved>d__21:MoveNext () (at Assets/_Game_/Scripts/Core/Characters/CharacterRoutines.cs:290)
UnityEngine.SetupCoroutine:InvokeMoveNext (System.Collections.IEnumerator,intptr)


AnimationEventHandler "onAnimEvent<EventType>" DoWaitEvent
OpenDoor Animation
UpdateWeapons on End movement
add debug key to speed up time

		[o/	needed to go to "-10" in Execution Order Settings
		
		Si je suis venu chez toi, FadeScreen.cs, c'est qu'il y avait quelque chose à rajouter

- RpgAnims 
	> DrawWeapons
	> SheathWeapons
	> Attack
	> GetHit

- CharacterComponent
	> allInstances

- CharacterStates
	> IdleState
	> AssignState (for Player)
	> ThinkState (for AI)
	> MoveToState
	> PerformsActionState
	> DeadState

- CharacterActions
	> None
	> RunAction
	> AttackAction
	> InteractAction
	...
	> TalkAction
	> UseItemAction

- PlayerInput
	> HasFocus
	> ClickNavigation
	> ClickProp
	> ClickCharacter
	> MouseMove
	> CameraMove
	> CameraOrbit
	...

- PlayerParty
	> AddCharacter
	> RemoveCharacter
	> AssignMove
	> AssignInteract
	> AssignRun
	... ("assign action")

- GameRoutines
	> QueueRoutine
	> routinesStarted
	> routinesCompleted
	> WaitMoveCharacter
	> WaitRotateCharacter
	...

- GameMaster
	> NewTurn
	> EndTurn
	> IsPlayerTurn
	> IsCurrentTurn(faction)
	> onTurnStarted
	> onTurnEnded
	> onGameOver
	> onVictory
	...
	
	
	### GD ###
	
	- "Can't" action
	
	
	
	
	
	https://opengameart.org/content/medieval-props-textured
	https://kaylousberg.itch.io/halloween-bits
	https://kenney.nl/assets/retro-urban-kit
	https://pigeon-gang.itch.io/3d-fences