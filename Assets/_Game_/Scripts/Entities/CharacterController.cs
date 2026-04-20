using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CharacterController : MonoBehaviour
{
	
	static public List<CharacterController> instances = new List<CharacterController>();
	static public IEnumerable<CharacterController> GetPartyCharacters() => instances.Where(c => c.InParty());
	
	
	public event System.Action statsChanged;
	public event System.Action<bool> selectionChanged;
	public event System.Action<float> onHealed;
	public event System.Action<float> onDamaged;
	public event System.Action<int> onPartyIndexChanged;
	public event System.Action onKilled;
	public event System.Action onMoved;
	public event System.Action onRotate;
	public event System.Action<CharacterAction, object> onUseAction;
	
	[SerializeField] private CharacterData characterData;
	[SerializeField] private List<AIBrainData> aiBrains =  new List<AIBrainData>();
	[SerializeField] private float visionRange = 2.0f;
	public float GetVisionRange() => visionRange;
	public List<AIBrainData> GetBrains() => aiBrains.ToList();
	
	[ReadOnly] [SerializeField] private bool isControllable = false;
//	[ReadOnly] [SerializeField] private bool isSelected = false;
	[ReadOnly] [SerializeField] private int partyIndex = -1;
	[ReadOnly] [SerializeField] private float usedMovePoints;
	//[ReadOnly] [SerializeField] private int usedActions;
	[ReadOnly] [SerializeField] private List<CharacterAction> usedActions = new List<CharacterAction>();
	[ReadOnly] [SerializeField] private float totalDamages;
	[ReadOnly] [SerializeField] private int savedRotation;
	[ReadOnly] [SerializeField] private Vector3 savedPosition;
	[ReadOnly] [SerializeField] private bool isDead;
	[ReadOnly] [SerializeField] private CharacterAgent agent;
	[ReadOnly] [SerializeField] private int currentTurn;
	[ReadOnly] [SerializeField] private CharacterFaction currentFaction;
	[ReadOnly] [SerializeField] private CharacterFaction previousFaction;

	[ReadOnly] [SerializeField] private float lastRotatedTime;
	//[SerializeField] private ColorSettings colorSettings;
	
	public int GetCurrentTurn() => currentTurn;
	
	public void Spawn()
	{
		transform.SetParent(null, true);
		gameObject.SetActive(true);
		GetAgent().SetObstacle(true);
	}
	
	public bool IsControllableByPlayer() => isControllable && !isDead; // TODO: "IsPlayerTurn() && IsParty()"
	
	public CharacterAgent GetAgent()
	{
		if (agent == null) agent = Utils.GetOrAddComponent<CharacterAgent>(gameObject);
		return agent;
	}

	public Vector3 GetEyesPosition() => GetPosition() + Vector3.up * agent.GetHeight() * 0.9f;
    
	public Vector3 GetCenterPosition(bool local = false) => (!local ? GetPosition() : Vector3.zero) + Vector3.up * agent.GetHeight() * 0.5f;

	//public bool IsSelected() => isSelected;
	public void NotifySelected(bool value)
	{
		//agent.SetObstacle(!value);//todo not in selection
		selectionChanged?.Invoke(value);
	}

	public void EndTurn()
	{
		StopAgent();
	}

	public void Restore() => NewTurn(-1);
	public void NewTurn(int turn)
	{
		usedActions.Clear();
		usedMovePoints = 0;
		GetAgent().ClearDestination();
		if (turn >= 0) currentTurn = turn;
	}


	public int GetRemaingActions() // TODO GD
	{
		return characterData.maxActions - usedActions.Count;
	}
	
	public bool HasActions() => usedActions.Count < characterData.maxActions;
	
	public void UseAction(CharacterAction actionType, object param = null)
	{
		usedActions.Add(actionType);
		Debug.Log(this + " usedAction=" + usedActions.Last());
		if (actionType == CharacterAction.RunAction)
		{
			usedMovePoints -= characterData.runActionExtraMovePoints;
			//StopAgent();
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
	}

	private void SetCurrentFaction(CharacterFaction faction)
	{
		if (currentFaction == previousFaction) return;
		Debug.Log(this  + " SetCurrentFaction " + faction + " from " + previousFaction);
		previousFaction = currentFaction;
		currentFaction = faction;
	}

	public bool IsCurrentTurn() => GameController.GetInstance().IsPlayerTurn() && IsPlayer();
	
	public float GetMaxMovePoints() => characterData.maxMovePoints;
	public float GetRemainingMovePoints() => Mathf.Max(0, characterData.maxMovePoints - usedMovePoints);
	public float GetMovePointsProgress() => 1f - usedMovePoints / characterData.maxMovePoints;
	public bool HasMovePoints(float d = 0f) => usedMovePoints + d < GetMaxMovePoints();
	
	public bool CanUseRunAction()
	{
		return  HasActions();
	}
	
	public bool CanInteract()
	{
		return HasActions();
	}

	public void LookWorldPosition(Vector3 worldPosition, float f = -1f)
	{
		DebugDrawer.Line(GetEyesPosition(), worldPosition, Color.cyan, f);
		LookDirection(worldPosition - GetPosition(), f);
	}

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
    
	const float MIN_FACING_DOT = 0.9f;
	public bool IsFacingDirection(Vector3 dir)
	{
		dir.y = 0;
		dir.Normalize();
		var dot = Vector3.Dot(dir, transform.forward);
		//Debug.Log(dot);
		// DebugGUI.FloatingText(transform.position, dot.ToString("N2"));
		return dot >= MIN_FACING_DOT;
	}
	
	public Vector3 GetPosition() => transform.position;

	public void Teleport(Vector3 dest) => Teleport(dest, GetRotation(), true);
	public void Teleport(Vector3 dest, bool stopMovement) => Teleport(dest, GetRotation(), stopMovement);
	public void Teleport(Vector3 dest, int newRot, bool stopMovement)
	{
		GetAgent().Teleport(dest, newRot, stopMovement);
	}
	public void SavePosition(Vector3 newPos)
	{
		savedPosition = newPos;
		onMoved?.Invoke();
	}

	public int GetRotation() => (int)transform.eulerAngles.y;
	public void SaveRotation(int newRot)
	{
		savedRotation = newRot;
		onRotate?.Invoke();
	}
	
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
			agent.StopMovement();
			onKilled?.Invoke();
			statsChanged?.Invoke();
		}
	}
	

	public float GetAttackRange() => characterData.attackRange;
	
	public bool CanAttack(bool checkActions = true) => !characterData.noAttack && (!checkActions || HasActions());

	public bool TryGetAIBrain(out AIBrainData aiBrain)
	{
		aiBrains = aiBrains.OrderBy(b => -b.GetPriority(this)).ToList();
		Debug.Log(this + " TryGetAIBrain " + Utils.ArrayStr(aiBrains));
		return (aiBrain = aiBrains.FirstOrDefault()) != null;
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

	public CharacterController GetClosestCharacter(List<CharacterController> getNearbyLivingCharacters)
	{
		return getNearbyLivingCharacters.OrderBy(other => GetDistanceTo(other)).FirstOrDefault();
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

	bool LOSCheck(CharacterController other)
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

	public void RunAction()
	{
		UseAction(CharacterAction.RunAction);
	}

	public bool HasUsedAction(CharacterAction action) => usedActions.Contains(action);

	private void Awake()
	{
		instances.Add(this);
		
		if(characterData == null)
			Debug.LogError(this + " has no character data");

		currentFaction = characterData.faction;
		
		if(!aiBrains.OfType<AIBrain_Wait>().Any()) aiBrains.Add(ScriptableObject.CreateInstance<AIBrain_Wait>());//todo store instance static
	}

	private void OnDestroy()
	{
		instances.Remove(this);
	}

	public void StopAgent()
	{
		agent.StopMovement();
	}

	public Vector3 GetVelocity()
	{
		return GetAgent().velocity;
	}

	public bool IsMoving() => GetAgent().IsMoving();

	public bool IsRotating() => Time.time - lastRotatedTime <= Consts.MinTimeDuration; // \o/ every character starts the game by rotating for a very short time

	public float GetLastAttackDamage()
	{
		return characterData.lastAttackDamage;
	}

	public float GetLastAttackHitTime() => characterData.lastAttackHitTime;
	
	public void AttackAction(CharacterController target = null, bool stopAgent = true)
	{
		UseAction(CharacterAction.AttackAction);
	}

	public void FloatingText_Cant() // TODO: in character animation
	{
		if(IsCurrentTurn()) FloatingText(!HasActions() ? "No actions!" : "Can't");
	}
	
	public void FloatingText(string canT) // TODO: in character animation
	{
		GameCanvas.Instance.FloatingText(this, canT);
	}

	private void OnDrawGizmosSelected()
	{
		aiBrains.ForEach(b =>
		{
			b?.DrawGizmos(this);
		});
	}


	/*public Color GetCurrentColor()
	{
		return isDead ? GetDeadColor() : InParty() ? GetFactionColor() : GetDefaultColor();
	}*/

	
	public bool IsSelected()
	{
		return CharacterSelection.IsCharacterSelected(this);
	}
}