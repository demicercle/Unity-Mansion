# Unity-Mansion

- add namespaces

- Talk Action
- DebugGUI Draw Character stats (movePoints, etc)
	> Right Click editor selects character under mouse
- "PartyChanged" event for Character, allowing to remove LateUpdate in Portraits, Agent drawPath
- change animation of characters during the mythos phase ("look around", "weapons ready", etcs)
- OpportunityAttacks
- tweaks! selection circle alpha when AI turns
- assign action on release click
- rename "NavUtility" into component "NavMeshRebuilder"
- GameView is GameInput
- if no actions, change the cursor

- custom asset post processor
- update color of nav target depending on the action (red for enemies)

- assignation of target cursor mouse over issue
- ai detection is important for triggering opportunity attack, because weapons ready
- door nav rebuild
- zombi should rotate on scream
- zombi scream should be linked to ai detection and not weapon ready?
- "round" logic (characters rest on start round)

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