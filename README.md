# Unity-Mansion

- draw line no depth test
- add namespaces
- character use action with serializable "action parameter" info
- add keyboard shortcut + turn UI popup
- GameController
- more delay between turn + popup
- attack zombi (enemy to player)
- interact with npc
- CantEvent
- DebugGUI Draw Character stats (movePoints, etc)
	> Right Click editor selects character under mouse
- CharacterView > CharacterSelection
- CharacterAnimation
- RPGAnims (AnimationSettings)
- "Forsaken" Startup scene
- CursorSprite "Wait" during loading
- CursorSprite "Hand" on Character or Prop
- "PartyChanged" event for Character, allowing to remove LateUpdate in Portraits, Agent drawPath
- GameView.Routines
- GameView.Cursor
- camera render target not working anymore
- change animation of characters during the mythos phase ("look around", "weapons ready", etcs)
- OpportunityAttacks
- DefaultAnims

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