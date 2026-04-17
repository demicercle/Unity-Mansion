using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CharacterController : MonoBehaviour
{
	static public List<CharacterController> instances = new List<CharacterController>();
	static public IEnumerable<CharacterController> GetPartyCharacters() => instances.Where(c => c.InParty());
	
	/*public enum ActionType TODO
	{
		None,
		InteractAction,
		RunAction
	}*/
	
	public event System.Action statsChanged;
	public event System.Action<bool> selectionChanged;
	public event System.Action<float> onHealed;
	public event System.Action<float> onDamaged;
	public event System.Action<int> onPartyIndexChanged;
	public event System.Action onKilled;
	public event System.Action onMoved;
	public event System.Action onRotate;
	public event System.Action<CharacterController> onAttackAction;
	
	[SerializeField] private CharacterData characterData;
	[SerializeField] private List<AIBrainData> aiBrains =  new List<AIBrainData>();
	public List<AIBrainData> GetBrains() => aiBrains.ToList();
	
	[ReadOnly] [SerializeField] private bool isControllable = false;
//	[ReadOnly] [SerializeField] private bool isSelected = false;
	[ReadOnly] [SerializeField] private int partyIndex = -1;
	[ReadOnly] [SerializeField] private float usedMovePoints;
	[ReadOnly] [SerializeField] private int usedActions;
	[ReadOnly] [SerializeField] private float totalDamages;
	[ReadOnly] [SerializeField] private int savedRotation;
	[ReadOnly] [SerializeField] private Vector3 savedPosition;
	[ReadOnly] [SerializeField] private bool isDead;
	[ReadOnly] [SerializeField] private CharacterAgent agent;

	public bool IsControllable() => isControllable && !isDead;
	
	public CharacterAgent GetAgent()
	{
		if (agent == null) agent = Utils.GetOrAddComponent<CharacterAgent>(gameObject);
		return agent;
	}

	public Vector3 GetEyesPosition() => GetPosition() + Vector3.up * agent.GetHeight() * 0.9f;
    
	public Vector3 GetCenterPosition() => GetPosition() + Vector3.up * agent.GetHeight() * 0.5f;

	//public bool IsSelected() => isSelected;
	public void NotifySelected(bool value)
	{
		if (value)
		{
			agent.SetObstacle(!IsControllable());
		}
		else
		{
			agent.SetObstacle(true);
		}
		
		selectionChanged?.Invoke(value);
	}

	public void NewTurn(int turn)
	{
		usedActions = 0;
		usedMovePoints = 0;
	}

	public bool HasActions() => usedActions < characterData.maxActions;

	public void ConsumeActions(int actions = 1)
	{
		usedActions += actions;
		Debug.Log(this + " usedActions=" + usedActions);
		statsChanged?.Invoke();
	}

	public bool IsEnemyToPlayer() => characterData.faction == CharacterFaction.Player;
	
	public bool InParty() => partyIndex >= 0;
	public int GetPartyIndex() => partyIndex;

	public void SetPartyIndex(int index)
	{
		Debug.Log(this + " SetPartyIndex " + index);
		partyIndex = index;
		isControllable = index >= 0;
		onPartyIndexChanged?.Invoke(index);
	}
	
	public float GetRemainingMovePoints() => Mathf.Max(0, characterData.maxMovePoints - usedMovePoints);
	public float GetMovePointsProgress() => 1f - usedMovePoints / characterData.maxMovePoints;

	public bool CanMove() => GetRemainingMovePoints() > 0f;
	
	public bool CanUseRunAction()
	{
		return  HasActions();
	}
	
	public bool CanInteract()
	{
		return HasActions();
	}

	public void RotateTowards(Vector3 worldPosition, float f = -1f)
	{
		LookDirection(worldPosition - GetPosition(), f);
	}

	public void LookDirection(Vector3 getDirection, float f = -1f)
	{
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
			DebugGUI.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 1.0f);
			transform.rotation = Quaternion.LookRotation(getDirection);
		}
		else
		{
			DebugGUI.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 0.0f);
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

	public void Teleport(Vector3 dest) => Teleport(dest, GetRotation());
	public void Teleport(Vector3 dest, int newRot)
	{
		GetAgent().Teleport(dest, newRot);
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
	
	public bool CanAttack() => !characterData.noAttack && HasActions();

	public bool TryGetAIBrain(out AIBrainData aiBrain)
	{
		aiBrains = aiBrains.OrderBy(b => b.GetPriority(this)).ToList();
		Debug.Log(this + " TryGetAIBrain " + Utils.ArrayStr(aiBrains));
		return (aiBrain = aiBrains.FirstOrDefault()) != null;
	}
	
	public List<CharacterController> GetNearbyLivingCharacters(float range, float drawDuration = 0f, Color drawColor = default)
	{
		var outList = new List<CharacterController>();
		var center = GetPosition();
		DebugGUI.WireDisc(center, Vector3.up, range, drawColor, drawDuration);
		foreach (var coll in Physics.OverlapSphere(center, range, LayerMask.GetMask(GameView.LAYER_CHARACTER)))
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
		return GetDistanceTo(wp.GetPosition()) <= agent.reachDistance;
		//return Vector3.Distance(GetPosition(), wp.GetPosition()) <= agent.reachDistance;
	} 

	public bool IsAtAttackRange(CharacterController target, bool losCheck, float drawDuration = 0.0f)
	{
		var atkRange = GetAttackRange();
		atkRange = Mathf.Max(atkRange, agent.GetRadius() + target.agent.GetRadius() + CharacterAgent.minNavDistance);
		if (Vector3.Distance(GetPosition(), target.GetPosition()) <= atkRange)
		{
			return !losCheck || LOSCheck(target, drawDuration);
		}
		else
		{
			return false;
		}
	}

	public bool LOSCheck(CharacterController other, float drawDuration = 0.0f)
	{
		var start = GetEyesPosition();
		var end = other.GetEyesPosition();
		if(Physics.Linecast(start, end, out var hit,
			   LayerMask.GetMask(GameView.LAYER_WALL, GameView.LAYER_GROUND)))
		{
			Debug.Log(this + " LOSCheck " + other + " collide " + hit.collider);
			DebugGUI.Line(start, end, Color.red, drawDuration);
			return false;
		}

		DebugGUI.Line(start, end, Color.green, drawDuration);
		return true;
	}

	public void RunAction()
	{
		ConsumeActions(1);
		usedMovePoints -= characterData.runActionExtraMovePoints;
	}

	private void Awake()
	{
		instances.Add(this);
		
		if(characterData == null)
			Debug.LogError(this + " has no character data");
		
		if(!aiBrains.OfType<AIBrain_Wait>().Any()) aiBrains.Add(ScriptableObject.CreateInstance<AIBrain_Wait>());//todo store instance static
	}

	private void OnDestroy()
	{
		instances.Remove(this);
	}

	public Vector3[] GetCurrentPath() => GetAgent().GetCurrentPath();

	public void StopAgent()
	{
		GetAgent().StopMovement();
	}

	public Vector3 GetVelocity()
	{
		return GetAgent().velocity;
	}

	public bool IsMoving() => GetAgent().isMoving;

	public float GetLastAttackDamage()
	{
		return characterData.lastAttackDamage;
	}

	public void AttackAction(CharacterController target = null)
	{
		ConsumeActions(1);
		onAttackAction?.Invoke(target);
	}

	private void OnDrawGizmosSelected()
	{
		aiBrains.ForEach(b =>
		{
			b?.DrawGizmos(this);
		});
	}
}