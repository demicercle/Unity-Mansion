using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class CharacterController : MonoBehaviour
{
	const float MinFacingDot = 0.9f;
	
	public static List<CharacterController> AllCharacters => _allCharacters;
	public static IEnumerable<CharacterController> GetPartyCharacters() => _allCharacters.Where(c => c.InParty());
	
	public static event System.Action partyChanged;
	
	public event System.Action statsChanged;
	public event System.Action<bool> selectionChanged;
	public event System.Action<float> onHealed;
	public event System.Action<float> onDamaged;
	public event System.Action<int> onPartyIndexChanged;
	public event System.Action onKilled;
	public event System.Action onMoved;
	public event System.Action onRotate;
	public event System.Action<CharacterAction, object> onUseAction;
	public event System.Action turnStarted;
	public event System.Action turnEnded;
	
	[SerializeField] private CharacterData characterData;
	[SerializeField] private List<AIBrainBase> aiBrains =  new List<AIBrainBase>();
	[Range(0f, 10f)] [SerializeField] private float visionRange = 2.0f;
	[Min(0f)] [SerializeField] private float defaultAttackHitTime = 1f;
	
	public void FloatingText_Cant() // TODO: in character animation
	{
		if(IsCurrentTurn()) FloatingText(!HasActions() ? "No actions!" : "Can't");
	}
	
	public void FloatingText(string canT) // TODO: in character animation
	{
		GameCanvas.Instance.FloatingText(this, canT);
	}
	
	public void Spawn()
	{
		transform.SetParent(null, true);
		gameObject.SetActive(true);
		agent.SetAgentActive(true);
		agent.SetObstacle(true);
	}
	
	public CharacterRoutines Routines => routines;
	public CharacterAgent GetAgent() => agent;
	public bool IsControllableByPlayer() => isControllable && !isDead;
	
	public bool IsSelected()
	{
		return CharacterSelection.IsCharacterSelected(this);
	}
	
	public void NotifySelected(bool value)
	{
		//agent.SetObstacle(!value);//todo not in selection
		selectionChanged?.Invoke(value);
	}

	public void EndTurn()
	{
		StopAgent();
		turnEnded?.Invoke();
	}
	
	public void NewTurn(int turn)
	{
		Rest();
		GetAgent().ClearDestination();
		if (turn >= 0) currentTurn = turn;
		turnStarted?.Invoke();
	}

	public void Rest()
	{
		usedActions.Clear();
		usedMovePoints = 0;
	}
	
	public bool HasActions() => usedActions.Count < characterData.maxActions;

	public bool HasUsedAction(CharacterAction action) => usedActions.Contains(action);

	public bool CanUseAction(CharacterAction _) // TODO
	{
		return HasActions();
	}
	
	public bool TryUseAction(CharacterAction action, bool floatingText)
	{
		if (CanUseAction(action))
		{
			UseAction(action);
			return true;
		}

		Debug.Log(this + " TryUseAction " + action + " failed");
		if(floatingText) FloatingText_Cant();
		return false;
	}
	
	
	public void AttackAction(CharacterController target = null, bool stopAgent = true)
	{
		UseAction(CharacterAction.AttackAction, target);
	}
	
	public void UseAction(CharacterAction actionType, object param = null)
	{
		usedActions.Add(actionType);
		Debug.Log(this + " usedAction=" + usedActions.Last());
		if (actionType == CharacterAction.RunAction)
		{
			usedMovePoints -= characterData.runActionExtraMovePoints;
			Routines.AssignRunAction();
		}
		onUseAction?.Invoke(actionType, param);
		statsChanged?.Invoke();
	}
	
	public void UseAllActions()
	{
		ConsumeMovePoints(GetRemainingMovePoints());
		UseAction(CharacterAction.None);
	}
	
	public CharacterFaction GetCurrentFaction() => currentFaction;
	
	public bool IsPlayer() => GetCurrentFaction() == CharacterFaction.Player;
	
	public bool IsEnemyToPlayer(CharacterController other = null) => characterData.faction == CharacterFaction.Enemy;

	public bool IsEnemyTo(CharacterFaction faction)
	{
		if (currentFaction == CharacterFaction.Enemy)
			return faction != CharacterFaction.Enemy;
		else
			return faction == CharacterFaction.Enemy;
	}
	
	public bool InParty() => partyIndex >= 0;
	
	public int GetPartyIndex() => partyIndex;

	public void SetPartyIndex(int index)
	{
		Debug.Log(this + " SetPartyIndex " + index);
		partyIndex = index;
		if (index >= 0)
		{
			isControllable = true;
			SetCurrentFaction(CharacterFaction.Player);
		}
		else
		{
			isControllable = false;
			SetCurrentFaction(previousFaction);
		}
		onPartyIndexChanged?.Invoke(index);
		partyChanged?.Invoke();
	}

	private void SetCurrentFaction(CharacterFaction faction)
	{
		if (currentFaction == previousFaction) return;
		Debug.Log(this  + " SetCurrentFaction " + faction + " from " + previousFaction);
		previousFaction = currentFaction;
		currentFaction = faction;
	}

	public bool IsCurrentTurn() => GameTurnManager.Instance.IsPlayerTurn() && IsPlayer();
	
	public float GetMaxMovePoints() => characterData.maxMovePoints;
	
	public float GetRemainingMovePoints() => Mathf.Max(0, characterData.maxMovePoints - usedMovePoints);
	
	public float GetMovePointsProgress() => 1f - usedMovePoints / characterData.maxMovePoints;
	
	public bool HasMovePoints(float d = 0f) => usedMovePoints + d < GetMaxMovePoints();
	
	public bool CanInteract()
	{
		return HasActions();
	}

	public void LookWorldPosition(Vector3 worldPosition, float f = -1f)
	{
		DebugDrawer.Line(GetEyesPosition(), worldPosition, Color.cyan, f);
		LookDirection(worldPosition - GetPosition(), f);
	} // TODO not in use

	public void LookDirection(Vector3 getDirection, float f = -1f)
	{
//		Debug.Log(this + " LookDirection " + f + " " + getDirection);
		lastRotatedTime = Time.time;		

		getDirection.y = 0;
		getDirection.Normalize();

		if (getDirection.sqrMagnitude < 0.01f)
		{
			Debug.LogWarning(this + " invalid direction");
			return;
		}

		var radius = agent.GetRadius();
		if (f < 0f)
		{
			DebugDrawer.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 1.0f);
			transform.rotation = Quaternion.LookRotation(getDirection);
		}
		else
		{
			DebugDrawer.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 0.0f);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(getDirection), f * agent.rotationSpeed); //TODO property "rotationSpeed" separated from agent
		}
	}
    
	public bool IsFacingDirection(Vector3 dir)
	{
		dir.y = 0;
		dir.Normalize();
		var dot = Vector3.Dot(dir, transform.forward);
		//Debug.Log(dot);
		// DebugGUI.FloatingText(transform.position, dot.ToString("N2"));
		return dot >= MinFacingDot;
	}
	
	public Vector3 GetPosition() => transform.position;
	public Vector3 GetEyesPosition() => GetPosition() + Vector3.up * agent.GetHeight() * 0.9f;
	public Vector3 GetCenterPosition(bool local = false) => (!local ? GetPosition() : Vector3.zero) + Vector3.up * agent.GetHeight() * 0.5f;

	public bool IsMoving() => GetAgent().IsMoving();
	
	public void StopAgent()
	{
		agent.StopMovement();
	}

	public Vector3 GetVelocity()
	{
		return GetAgent().velocity;
	}

	public void Teleport(Vector3 dest) => Teleport(dest, GetRotation(), true);
	public void Teleport(Vector3 dest, bool stopMovement) => Teleport(dest, GetRotation(), stopMovement);
	public void Teleport(Vector3 dest, int newRot, bool stopMovement)
	{
		GetAgent().Teleport(dest, newRot, stopMovement);
	}

	public int GetRotation() => (int)transform.eulerAngles.y;
	public void ConsumeMovePoints(float amount)
	{
		usedMovePoints += amount;
		statsChanged?.Invoke();
	}
	
	public float GetTotalDamages() => totalDamages;
	public int GetMaxHealth() => characterData.maxHealth;
	public int GetCurrentHealthInt() => Mathf.CeilToInt(GetCurrentHealth());
	public float GetCurrentHealth() => GetMaxHealth() - GetTotalDamages();
	public float GetDamageProgress() => Mathf.Clamp01(GetCurrentHealth() / (float)GetMaxHealth());
	public bool IsDead() => isDead;

	public Texture2D GetPortraitTexture() => characterData.portraitTexture;
	public string GetDisplayName() => characterData.displayName;
	
	public void Heal(float amount = 0f)
	{
		if (amount == 0f)
			totalDamages = 0;
		else
			totalDamages = Mathf.Max(0, totalDamages - amount);
		
		onHealed?.Invoke(amount);
		statsChanged?.Invoke();
	}
	public void TakeDamage(float amount)
	{
		Debug.Log(this + " TakeDamage " + amount);
		totalDamages += amount;
		
		onDamaged?.Invoke(amount);
		
		if (GetCurrentHealth() <= 0f)
		{
			TryKill();
			return;
		}
		
		statsChanged?.Invoke();
	}
	public void TryKill()
	{
		if (!IsDead())
		{
			isDead = true;
			agent.SetAgentActive(false);
			onKilled?.Invoke();
			statsChanged?.Invoke();
		}
	}
	

	public float GetAttackRange() => characterData.attackRange;
	public bool CanAttack(bool checkActions = true) => !characterData.noAttack && (!checkActions || HasActions());
	
	public bool TryGetAIBrain(out AIBrainBase aiBrain)
	{
		aiBrains = aiBrains.OrderBy(b => -b.GetPriority(this)).ToList();
		Debug.Log(this + " TryGetAIBrain " + Utils.ArrayStr(aiBrains));
		return (aiBrain = aiBrains.FirstOrDefault()) != null;
	}

	public float GetDistanceTo(Vector3 world) => Vector3.Distance(GetPosition(), world);

	public float GetDistanceTo(CharacterController other) => GetDistanceTo(other.GetPosition());
	
	public bool WaypointReached(Waypoint wp)
	{
		return agent.ReachedDestination(wp.GetPosition());
		//return Vector3.Distance(GetPosition(), wp.GetPosition()) <= agent.reachDistance;
	} 

	public bool IsAtAttackRange(CharacterController target, bool losCheck)
	{
		var atkRange = GetAttackRange();
		atkRange = Mathf.Max(atkRange, agent.GetRadius() + target.agent.GetRadius() + target.agent.GetMinReachDistance());
		if (Vector3.Distance(GetPosition(), target.GetPosition()) <= atkRange)
		{
			return !losCheck || LOSCheck(target);
		}
		else
		{
			return false;
		}
	}
	public float GetLastAttackDamage()
	{
		return characterData.lastAttackDamage;
	}

	public float GetDefaultAttackHitTime() => defaultAttackHitTime;
	
	public bool IsCharacterVisible()
	{
		if (RoomTrigger.activeRooms.Any(t => t.HasCharacter(this)))
			return false;
		
		return true;
	}
	
	public bool CanSeeAnyEnemyAround() => _allCharacters.FirstOrDefault((other) =>
	{
		if (!IsEnemyTo(other.GetCurrentFaction())) return false;
		if (!other.IsCharacterVisible()) return false;
		return Vector3.Distance(GetPosition(), other.GetPosition()) <= visionRange;
	});

	public CharacterController GetClosestCharacter(List<CharacterController> getNearbyLivingCharacters)
	{
		return getNearbyLivingCharacters.OrderBy(other => GetDistanceTo(other)).FirstOrDefault();
	}
	
	public List<CharacterController> GetNearbyLivingCharacters(float range, float drawDuration = 0f, Color drawColor = default)
	{
		var outList = new List<CharacterController>();
		var center = GetPosition();
		DebugDrawer.WireDisc(center, Vector3.up, range, drawColor, drawDuration);
		foreach (var coll in Physics.OverlapSphere(center, range, LayerMask.GetMask(Consts.LAYER_CHARACTER)))
		{
			if (coll.TryGetComponent(out CharacterController otherCharacter) && otherCharacter != this && !otherCharacter.IsDead())
			{
				outList.Add(otherCharacter);
			}
		}

		return outList;
	}
	
	public bool TryGetAnimation(out CharacterAnimation outAnimation)
	{
		outAnimation = characterAnimation;
		return outAnimation != null;
	}
	
	public Coroutine WaitAnimationEvent(CharacterAnimation.EventType eventType) => StartCoroutine(DoWaitAnimationEvent(eventType));
	
	private IEnumerator DoWaitAnimationEvent(CharacterAnimation.EventType eventType)
	{
		if (characterAnimation == null || !characterAnimation.HasAnimationEvent(CharacterAnimation.EventType.Hit))
			yield return new WaitForSeconds(GetDefaultAttackHitTime());
		else
		{
			yield return characterAnimation.WaitEvent(eventType);
		}
	}
	
	#region PRIVATE
	private static List<CharacterController> _allCharacters = new List<CharacterController>();
	private static ScriptableObject defaultAIBrain;

	private int partyIndex = -1;
	
	private int currentTurn;
	private bool isControllable;
	private float usedMovePoints;
	private List<CharacterAction> usedActions = new List<CharacterAction>();
	private float totalDamages;
	private float lastRotatedTime; // TODO not in use
	private bool isDead;
	private CharacterFaction currentFaction;
	private CharacterFaction previousFaction;
	private CharacterAgent agent;
	private CharacterRoutines routines;
	private CharacterAnimation characterAnimation;
	
	private void Awake()
	{
		if(characterData == null)
			Debug.LogError(this + " has no character data");
		
		_allCharacters.Add(this);
		
		agent = GetComponent<CharacterAgent>();
		if(!agent) agent = gameObject.AddComponent<CharacterAgent>();
		
		routines = GetComponent<CharacterRoutines>();
		if(!routines) routines = gameObject.AddComponent<CharacterRoutines>();
		
		characterAnimation = GetComponent<CharacterAnimation>();
		if(!characterAnimation) characterAnimation = gameObject.AddComponent<CharacterAnimation>();
		
		currentFaction = characterData.faction;

		if (!aiBrains.OfType<AIBrain_Wait>().Any())
		{
			if (!defaultAIBrain)
				defaultAIBrain = ScriptableObject.CreateInstance<AIBrain_Wait>();
			
			aiBrains.Add(defaultAIBrain as AIBrainBase);
		}
	}

	private void OnDestroy()
	{
		_allCharacters.Remove(this);
	}
	
	private bool LOSCheck(CharacterController other)
	{
		var start = GetEyesPosition();
		var end = other.GetEyesPosition();
		if(Physics.Linecast(start, end, out var hit,
			   LayerMask.GetMask(Consts.LAYER_WALL, Consts.LAYER_GROUND)))
		{
			Debug.Log(this + " LOSCheck " + other + " collide " + hit.collider);
			//DebugGUI.Line(start, end, Color.red, drawDuration);
			return false;
		}

		//DebugGUI.Line(start, end, Color.green, drawDuration);
		return true;
	}
	#endregion

	#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		aiBrains.ForEach(b =>
		{
			b?.DrawGizmos(this);
		});

		Gizmos.color = Color.magenta;
		Utils.DrawGizmosCircle(transform.position, Vector3.up, visionRange);
	}
	#endif
}