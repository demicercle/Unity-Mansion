# Unity-Mansion

- add namespaces

- Talk Action
- DebugGUI Draw Character stats (movePoints, etc)
	> Right Click editor selects character under mouse
- "Forsaken" Startup scene
- "PartyChanged" event for Character, allowing to remove LateUpdate in Portraits, Agent drawPath
- GameView.Routines
- GameView.Cursor
- change animation of characters during the mythos phase ("look around", "weapons ready", etcs)
- OpportunityAttacks
- "explore" command
- tweaks! selection circle alpha when AI turns
- assign action on release click
- rename "NavUtility" into component "NavMeshRebuilder"

		HELLO ASSO
		
AnimationEventHandler "onAnimEvent<EventType>"
CharacterRoutines as MonoBehaviour
OpenDoor Animation

UpdateWeapons on End movement
add debug key to speed up time

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