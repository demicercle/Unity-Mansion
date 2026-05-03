using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RPGCharacterAnims.Actions;
using UnityEngine;

public interface IAttackable
{
	Vector3 GetPosition();
	float GetHeight();
	float GetRadius();
	void TakeDamage(DamageInfo damageInfo, object instigator);
}

public enum CharacterFaction
{
	None = -1,
	Neutral,
	Player,
	Enemy
}

public enum CharacterAction
{
	None,
	Run,
	Attack,
	Interact,
	Talk,
	UseItem,
	Defend,
	Riposte
}

public struct DamageInfo
{
	public float amount;
	public bool isRanged;

	public override string ToString()
	{
		return $"DamageInfo (amount={amount}, isRange={isRanged})";
	}
}

public class CharacterEntity : MonoBehaviour, IAttackable
{
	private const float MinFacingDot = 0.9f;
	private const float MinAttackRange = 0.25f;
	//private const float DodgeCountToChance = 2f / 20f;
	private const float MovePointsToDodge = 0.02f;
	private const float MovePointsToHit = 0.00f;
	
	public static int debugStats { get; set; }
	
	public static List<CharacterEntity> AllCharacters => _allCharacters;

	public static CharacterEntity[] GetPartyCharacters() => _partyCharacters.ToArray();

	public static bool TryGetPartyCharacter(int index, out CharacterEntity partyChar)
	{
		if (index >= 0 && index < _partyCharacters.Count)
		{
			partyChar = _partyCharacters[index];
			return true;
		}

		partyChar = null;
		return false;
	}
	
	public static int CountLivingPartyCharacters() => _partyCharacters.Count(p => !p.IsDead());

	public static int PartyCount => _partyCharacters.Count;
	
	public static event System.Action onPartyChanged;
	public static event System.Action<CharacterEntity, float> onAnyCharacterRollDice;
	public static event System.Action<CharacterEntity, DamageInfo, object> onAnyCharacterDamaged;
	public static event System.Action<CharacterEntity, float> onAnyCharacterHealed;
	public static event System.Action<CharacterEntity> onAddedToParty, onRemovedFromParty;
	public static event System.Action<CharacterEntity> onAnyCharacterMoved;
	public static event System.Action<CharacterEntity> onAnyCharacterDead;
	public static event System.Action<CharacterEntity, CharacterAction> onAnyCharacterUsesAction;
	public static event System.Action<CharacterEntity, IAttackable, AttackResult> onAttackResult;
	
	public event System.Action onSpawned;
	public event System.Action onStatsChanged;
	public event System.Action onFactionChanged;
	public event System.Action<bool> onSelectionChanged;
	public event System.Action<float> onHealed;
	public event System.Action<float> onDamaged;
	public event System.Action<CharacterEntity, float> onDamagedBy;
	public event System.Action<int,int> onPartyIndexChanged;
	public event System.Action onDead;
	public event System.Action onMoved;
	public event System.Action onRotate;
	public event System.Action<CharacterAction, object> onUseAction;
	public event System.Action<CharacterEntity> onTalkedBy;
	public event System.Action turnStarted;
	public event System.Action turnEnded;
	
	[SerializeField] private CharacterData characterData;
	[SerializeField] private Canvas characterCanvas;
	[SerializeField] private float clearActionsDelay = 3.0f;
	[SerializeField] private bool rollDiceToDodge = false;
	[SerializeField] private bool rollDiceOnDefense = false;
	[SerializeField] private bool rollDiceOnRun = false;
	[SerializeField] private bool rollDiceOnRiposte = false;
	[Range(0f, 10f)] [SerializeField] private float visionRange = 2.0f; // TODO remove
	[Min(0f)] [SerializeField] private float defaultAttackHitTime = 1f;
	[SerializeField] private CharacterFaction factionOverride;
	[SerializeField] private WeaponData defaultWeapon;
	public CharacterRoutines Routines => routines;

	public bool IsName(string savedPCName)
	{
		return name == savedPCName;
	}
	
	public void FloatingText_Cant() // TODO: in character animation
	{
		Debug.Log(this + " Cant!");
		if(IsCurrentTurn()) FloatingText(!HasActions() ? "No actions!" : "Can't");
	}
	
	public void FloatingText(string canT) // TODO: in character animation
	{
		GameCanvas.Instance.FloatingText(this, canT);
	}
	
	public void Spawn()
	{
		Debug.Log(this + " Spawn");
		transform.SetParent(null, true);
		gameObject.SetActive(true);
		agent.SetAgentActive(true);
		agent.SetObstacle(true);
		SetCurrentWeapon(defaultWeapon);
		characterCanvas.gameObject.SetActive(true);
		onSpawned?.Invoke();
		Rest();
	}

	public CharacterAgent GetAgent()
	{
		if(!Application.isPlaying && !agent) agent = GetComponent<CharacterAgent>(); // for DrawGizmos stuff
		return agent;
	}
	
	public bool IsControllableByPlayer() => isControllable && !isDead;
	
	public bool IsSelected()
	{
		return CharacterSelection.IsCharacterSelected(this);
	}
	
	public void NotifySelected(bool value)
	{
		//agent.SetObstacle(!value);//todo not in selection
		if (value) StopFollow();
		onSelectionChanged?.Invoke(value);
	}

	public bool TryGetClosestEnemy(out CharacterEntity closestEnemy)
	{
		closestEnemy = null;
		
		var closestDistance = 0f;
		foreach (var enemy in CharacterEntity.AllCharacters)
		{
			if (IsEnemyTo(enemy) && !enemy.IsDead() && enemy.IsCharacterRevealed())
			{
				var distance = Vector3.Distance(GetPosition(), enemy.GetPosition());
				if (!closestEnemy || closestDistance > distance)
				{
					closestDistance = distance;
					closestEnemy = enemy;
				}
			}
		}

		return closestEnemy;
	}

	public void Rest(bool verbose = true)
	{
		if(verbose) Debug.Log(this + " Rest");
		usedActions.Clear();
		usedMovePoints = 0;
		dodgeCount = 0;
		riposteCount = 0;
		onStatsChanged?.Invoke();
	}
	
	public bool HasActions() => usedActions.Count < characterData.GetMaxActions();

	public bool HasUsedAction(CharacterAction action) => usedActions.Contains(action);

	public bool CanUseAction(CharacterAction _) // TODO
	{
		return HasActions();
	}

	private IAttackable lastAttackTarget;
	public IAttackable GetLastAttackTarget() => lastAttackTarget;
	
	private object lastDamageInstigator;
	public bool TryGetLastDamageInstigator(out CharacterEntity instigator)
	{
		return (instigator = lastAttackTarget as CharacterEntity) != null;
	}
	
	public float RollDice()
	{
		var rolled = DicePool.LastResult;
		if(DicePool.debugMode>0) Debug.Log(this + " RollDice takes last result: " + rolled);
		onAnyCharacterRollDice?.Invoke(this, rolled);
		DicePool.RollNewDice();
		return rolled;
	}

	public bool CanRiposte() => !IsDead() && riposteCount > 0;

	public bool TryRiposte(CharacterEntity attacker)
	{
		Debug.Log(this + " TryRiposte: " + attacker);
		
		if (IsAtAttackRange(attacker) && riposteCount > 0)
		{
			riposteCount--; // riposte
			Routines.AssignPerformAttack(attacker, false);
			return true;
		}

		return false;
	}

	public bool CanDodge() => characterData.GetDodgeChance() > 0 && dodgeCount > 0;
	
	public float GetDodgeChance() => Mathf.Clamp01(characterData.GetDodgeChance() + GetRemainingMovePoints() * MovePointsToDodge);

	public float GetHitChance() => Mathf.Clamp01(currentWeapon.GetHitChance() + GetRemainingMovePoints() * MovePointsToHit);

	public enum AttackResult
	{
		Undefined,
		Hit,
		Miss,
		Dodge,
	}
	
	public AttackResult TryHit(IAttackable target, float diceResult)
	{
		if (target is CharacterEntity)
		{
			return TryHit(target as CharacterEntity, diceResult);
		}

		if (target is Destructible)
		{
			return AttackResult.Hit; // destructible always hit
		}
		
		Debug.LogWarning(this + " TryAttack " + target + " AttackResult.Undefined!");
		return AttackResult.Undefined;
	}

	private float calculatedHitChance;
	private float targetRange;

	public float CalculateHitChance(Vector3 targetPosition)
	{
		if (IsUsingRangeWeapon())
		{
			targetRange = Vector3.Distance(GetCenterPosition(), targetPosition);
			return GetHitChance() * currentWeapon.EvaluateRange(targetRange);
		}
		else
		{
			return GetHitChance();
		}
	}
	
	public float CalculateHitChance(CharacterEntity target)
	{
		calculatedHitChance = CalculateHitChance(target.GetCenterPosition());
		if (!IsUsingRangeWeapon())
		{
			if (target.CanDodge())
			{
				calculatedHitChance *= (1f - target.GetDodgeChance());
			}
		}
		return Mathf.Clamp01(calculatedHitChance);
	}
	
	public AttackResult TryHit(CharacterEntity target, float atkRoll)
	{
		var hitChance = CalculateHitChance(target.GetCenterPosition());
		if(DicePool.debugMode>0) Debug.Log(this + " TryHit " + target + " roll=" + atkRoll + " hitChance=" + hitChance);
		if (1f - atkRoll > hitChance) // miss
		{
			onAttackResult?.Invoke(this, target, AttackResult.Miss);
			return AttackResult.Miss;
		}

		if (!IsUsingRangeWeapon())
		{
			if (target.CanDodge())
			{
				target.dodgeCount = Mathf.Max(0, target.dodgeCount - 1);
				if (rollDiceToDodge ? 1f - target.RollDice() > target.GetDodgeChance() : target.GetDodgeChance() > atkRoll)
				{
					onAttackResult?.Invoke(this, target, AttackResult.Dodge);
					return AttackResult.Dodge;
				}
			}
		}
			
		onAttackResult?.Invoke(this, target, AttackResult.Hit);
		return AttackResult.Hit;
	}
	
	public void AttackAction(IAttackable target, bool stopAgent = true)
	{
		lastAttackTarget = target;
		UseAction(CharacterAction.Attack, target);
	}
	
	public void UseAction(CharacterAction actionType, object param = null)
	{
		usedActions.Add(actionType);
		Debug.Log(this + " usedAction=" + usedActions.Last() + " param=" + param);
		if (actionType == CharacterAction.Run)
		{
			if (rollDiceOnRun) RollDice();
			usedMovePoints -= characterData.GetRunActionExtraMovePoints();
		}
		else if (actionType == CharacterAction.Talk)
		{
			(param as CharacterEntity)?.onTalkedBy?.Invoke(this);
		}
		else if (actionType == CharacterAction.Defend)
		{
			if (rollDiceOnDefense) RollDice();
			dodgeCount++;
		}
		else if (actionType == CharacterAction.Riposte)
		{
			if (rollDiceOnRiposte) RollDice();
			riposteCount++;
		}
		onUseAction?.Invoke(actionType, param);
		onAnyCharacterUsesAction?.Invoke(this, actionType);
		onStatsChanged?.Invoke();
		
		if (!GameTurnManager.Instance.IsTurnBasedMode())
		{
			CancelInvoke("ClearActions");
			Invoke("ClearActions", clearActionsDelay);
		}
	}

	public void ClearActions()
	{
		if (GameTurnManager.Instance.IsTurnBasedMode())
			return;

		usedActions.Clear();
	}
	
	public void UseAllActions(bool consumeMovePoints = true)
	{
		if(consumeMovePoints && HasMovePoints()) 
			ConsumeMovePoints(GetRemainingMovePoints(), false);
		
		usedActions.Add(CharacterAction.None); // TODO
	}
	
	public CharacterFaction GetCurrentFaction() => currentFaction;
	
	public bool IsPlayer() => GetCurrentFaction() == CharacterFaction.Player;
	
	public bool IsEnemyToPlayer(CharacterEntity other = null) => currentFaction == CharacterFaction.Enemy;

	public bool ShouldTalk(CharacterEntity other) => other.GetCurrentFaction() == CharacterFaction.Neutral &&
	                                                 GetCurrentFaction() == CharacterFaction.Player &&
	                                                 !other.IsDead() && !IsDead();

	public float GetTalkDistance(CharacterEntity other) =>
		other.GetRadius() + GetRadius() + agent.GetMinReachDistance() + other.agent.GetMinReachDistance();

	public bool ShouldAttack(IAttackable other) =>
		other is CharacterEntity ? ShouldAttack(other as CharacterEntity) : IsAtAttackRange(other);
	
	public bool ShouldAttack(CharacterEntity other) => !other.IsDead() && IsEnemyTo(other);
	public bool IsEnemyTo(CharacterEntity other) => IsEnemyTo(other.GetCurrentFaction());
	public bool IsEnemyTo(CharacterFaction faction)
	{
		if (currentFaction == CharacterFaction.Enemy)
			return faction != CharacterFaction.Enemy;
		else
			return faction == CharacterFaction.Enemy;
	}

	public bool IsLeaderOf(CharacterEntity other) => GameInput.Instance.IsLeaderMode() &&
														InParty() && other.InParty() && IsSelected();
	
	public bool InParty() => partyIndex >= 0;
	
	public int GetPartyIndex() => partyIndex;

	public void SetPartyIndex(int index)
	{
		Debug.Log(this + " SetPartyIndex " + index);
		partyIndex = index;
		if (index >= 0)
		{
			isControllable = true;
		}
		else
		{
			isControllable = false;
		}
		_partyCharacters = _allCharacters.Where(c => c.InParty()).OrderBy(c => c.partyIndex).ToList();
		if (index >= 0)
		{
			SetCurrentFaction(CharacterFaction.Player);
		}
		else
		{
			SetCurrentFaction(previousFaction);
		}
		onPartyIndexChanged?.Invoke(index, _partyCharacters.Count);
		onPartyChanged?.Invoke();
		if(index >= 0)
			onAddedToParty?.Invoke(_partyCharacters[index]);
		else
			onRemovedFromParty?.Invoke(_partyCharacters[index]);
	}

	private void SetCurrentFaction(CharacterFaction faction)
	{
		if (currentFaction == previousFaction) return;
		Debug.Log(this  + " SetCurrentFaction " + faction + " from " + previousFaction);
		previousFaction = currentFaction;
		currentFaction = faction;
		onFactionChanged?.Invoke();
	}

	public bool IsCurrentTurn() => GameTurnManager.Instance.IsPlayerTurn() == IsPlayer();
	
	public float GetMaxMovePoints() => characterData.GetMaxMovePoints();
	
	public float GetRemainingMovePoints() => Mathf.Max(0, characterData.GetMaxMovePoints() - usedMovePoints);
	
	public float GetMovePointsProgress() => 1f - usedMovePoints / characterData.GetMaxMovePoints();
	
	public bool HasMovePoints(float d = 0f) => !GameTurnManager.Instance.IsTurnBasedMode() || usedMovePoints + d < GetMaxMovePoints();
	
	public bool CanInteract()
	{
		return HasActions();
	}

	public void LookWorldPosition(Vector3 worldPosition, float f = -1f)
	{
		//DebugDrawer.Line(GetEyesPosition(), worldPosition, Color.cyan, f);
		LookDirection(worldPosition - GetPosition(), f);
	} // TODO not in use

	public void LookDirection(Vector3 getDirection, float f = -1f)
	{
//		Debug.Log(this + " LookDirection " + f + " " + getDirection);
		lastRotatedTime = Time.time;		

		getDirection.y = 0;
		getDirection.Normalize();

		if (getDirection.sqrMagnitude < Consts.MinMagnitude)
		{
			Debug.LogWarning(this + " invalid direction");
			return;
		}

		var radius = agent.GetRadius();
		if (f < 0f)
		{
			//DebugDrawer.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 1.0f);
			transform.rotation = Quaternion.LookRotation(getDirection);
		}
		else
		{
			//DebugDrawer.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 0.0f);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(getDirection), f * agent.rotationSpeed); //TODO property "rotationSpeed" separated from agent
		}

		agent.SetRotationY(transform.rotation.eulerAngles.y);
	}

	public bool IsFacingPosition(Vector3 pos) => IsFacingDirection(pos - GetPosition());
    
	public bool IsFacingDirection(Vector3 dir)
	{
		dir.y = 0;
		dir.Normalize();
		return dir.sqrMagnitude > Consts.MinMagnitude && Vector3.Dot(dir, transform.forward) >= MinFacingDot;
	}
	
	public Vector3 GetDirection() => transform.forward;

	public Vector3 GetDirectionTo(Vector3 world, bool flat = true)
	{
		return Vector3.Scale(new Vector3(1,flat ? 0 : 1,1), world - GetPosition()).normalized;
	}
	
	public Vector3 GetPosition() => transform.position;

	private const float VectorDecimals = 100f;
	public Vector3 GetSavedPosition() => new Vector3(Mathf.Round(transform.position.x * VectorDecimals) / VectorDecimals,
		Mathf.Round(transform.position.y * VectorDecimals) / VectorDecimals, Mathf.Round(transform.position.z * VectorDecimals) / VectorDecimals);
	
	public Vector3 GetEyesPosition() => GetPosition() + Vector3.up * GetAgent().GetHeight() * 0.9f; // TODO "GetAgent()"
	
	public Vector3 GetCenterPosition(bool local = false) => (!local ? GetPosition() : Vector3.zero) + Vector3.up * GetAgent().GetHeight() * 0.5f;  // TODO "GetAgent()"
	
	public float GetRadius() => GetAgent().GetRadius();
	public float GetHeight() => GetAgent().GetHeight();
	public bool IsMoving() => GetAgent().IsMoving();
	
	public void StopAgent()
	{
		StopFollow();
		agent.StopMovement();
	}

	public void StopFollow()
	{
		follows.enabled = false;
	}

	public bool TryGetFollowing(out CharacterEntity leader)
	{
		leader = null;
		
		if (follows.enabled)
		{
			leader = follows.GetLeader();
		}

		return leader != null;
	}

	public Vector3 GetVelocity()
	{
		return GetAgent().velocity;
	}

	public void Teleport(Vector3 dest) => Teleport(dest, GetRotation(), true);
	
	public void Teleport(Vector3 dest, bool stopMovement = true) => Teleport(dest, GetRotation(), stopMovement);
	
	public void Teleport(Vector3 dest, int newRot, bool stopMovement = true)
	{
		GetAgent().Teleport(dest, newRot, stopMovement);
		onMoved?.Invoke();
		onAnyCharacterMoved?.Invoke(this);
	}

	public int GetRotation() => (int)transform.eulerAngles.y;
	
	public void ConsumeMovePoints(float amount, bool notifyMoves)
	{
		if (amount != 0f)
		{
			usedMovePoints += amount;
			onStatsChanged?.Invoke();
		}
		
		if (notifyMoves)
		{
			onMoved?.Invoke();
			onAnyCharacterMoved?.Invoke(this);
		}
	}
	
	public float GetTotalDamages() => totalDamages;
	
	public int GetMaxHealth() => characterData.GetMaxHealth();
	public int GetCurrentHealth() => GetMaxHealth() - Mathf.CeilToInt(GetTotalDamages());
	public float GetDamageProgress() => Mathf.Clamp01(GetCurrentHealth() / (float)GetMaxHealth());
	public bool IsDead() => isDead;

	public Texture2D GetPortraitTexture() => characterData.GetPortrait();
	public string GetDisplayName() => characterData.GetDisplayName();
	
	public void Heal(float amount = 0f)
	{
		if (amount == 0f)
			totalDamages = 0;
		else
			totalDamages = Mathf.Max(0, totalDamages - amount);
		
		onHealed?.Invoke(amount);
		onAnyCharacterHealed?.Invoke(this, amount);
		onStatsChanged?.Invoke();
	}

	public void SetDamages(float newDamage)
	{
		totalDamages = newDamage;
		
		if (GetCurrentHealth() <= 0f)
		{
			if(TryKill())
				return;
		}
		
		onStatsChanged?.Invoke();
	}
	
	public void TakeDamage(DamageInfo damageInfo, object objectFrom)
	{
		Debug.Log(this + " TakeDamage " + damageInfo);
		totalDamages += damageInfo.amount;
		lastDamageInstigator = objectFrom;
		
		StopAgent();

		onDamaged?.Invoke(damageInfo.amount);
		onAnyCharacterDamaged?.Invoke(this, damageInfo, objectFrom);
		
		if (GetCurrentHealth() <= 0f)
		{
			if(TryKill())
				return;
		}

		onStatsChanged?.Invoke();
		
		if (objectFrom is CharacterEntity)
		{
			onDamagedBy?.Invoke(objectFrom as CharacterEntity, damageInfo.amount);
		}

	}
	public bool TryKill()
	{
		if (!IsDead())
		{
			Debug.Log(this + " Killed!");
			isDead = true;
			agent.SetAgentActive(false);
			if (IsSelected()) CharacterSelection.SelectCharacter(null);
			Routines.StopAllRoutines();
			if(detection) detection.ClearAll();
			onDead?.Invoke();
			onAnyCharacterDead?.Invoke(this);
			onStatsChanged?.Invoke();
			return true;
		}

		return false;
	}

	public bool CanAttack(bool checkActions = true, bool checkMoves = true) =>
		!isDead && (!checkActions || HasActions()) && (!checkMoves || HasMovePoints());
	
	public bool CanAttackOther(CharacterEntity other)
	{
		return CanAttack() && !other.IsDead() && IsEnemyTo(other);
	}

	public WeaponData GetCurrentWeapon() => currentWeapon;

	public float GetDistanceTo(Vector3 world) => NavUtils.NavDistance(GetPosition(), world);
	
	public bool WaypointReached(Waypoint wp)
	{
		return agent.ReachedDestination(wp.GetPosition());
		//return Vector3.Distance(GetPosition(), wp.GetPosition()) <= agent.reachDistance;
	}

	public float GetAttackRange()
	{
		return Mathf.Max( currentWeapon ? currentWeapon.GetAttackRange() : defaultWeapon ? defaultWeapon.GetAttackRange() : 0f, agent.GetRadius() + MinAttackRange);
	}
	
	public float GetAttackReachDistance(IAttackable target)
	{
		return Mathf.Max(MinAttackRange, currentWeapon.GetAttackRange(), agent.GetRadius() + target.GetRadius() + agent.GetMinReachDistance());
	}

	public bool IsAtAttackRange(IAttackable target)
	{
		var atkRange = GetAttackReachDistance(target) + MinAttackRange;
		if (IsUsingRangeWeapon())
		{
			var dist = GetDistanceTo(target.GetPosition());
			return dist <= atkRange;
		}
		else
		{
			var hDist = NavUtils.NavHDistance(GetPosition(), target.GetPosition());
			var vDist = NavUtils.NavVDistance(GetPosition(), target.GetPosition());
			return vDist <= GetHeight() && hDist <= atkRange;
		}
	}
	
	public bool LOSCheck(IAttackable other)
	{
		var start = GetEyesPosition();
		return LOSCheck(start, other.GetPosition())
		       || LOSCheck(start, other.GetPosition() + Vector3.up * other.GetHeight());
	}

	public bool LOSCheck(Vector3 start, Vector3 end)
	{
		if(Physics.Linecast(start, end, out var hit, Layers.MaskObstruction))
		{
			if(CharacterRoutines.debugMode > 0) DebugDrawer.Line(start, hit.point, Color.red, 1.0f);
			if(CharacterRoutines.debugMode > 0) Debug.Log(this + " LOSCheck  collide " + hit.collider);
			return false;
		}

		if(CharacterRoutines.debugMode > 0) DebugDrawer.Line(start, end, Color.green, 1.0f);
		return true;
	}
	
	public float GetLastAttackDamage(float atkResult)
	{
		return currentWeapon.RollDamage(atkResult);
	}

	public bool IsUsingRangeWeapon() => currentWeapon.IsRanged();

	public bool WeaponsReady() => characterAnimation && characterAnimation.WeaponsSwitched();

	public bool IsCharacterRevealed() => InParty() || RoomTrigger.IsCharacterVisible(this);

	public float GetMaxVisionRange() => InParty() ? visionRange : detection.GetMaxSeeingDistance();
	
	public bool CanSeeAnyEnemyAround() => !InParty() ? detection.GetDetectedCharacters().Any(IsEnemyTo)
		: _allCharacters.FirstOrDefault((other) =>
	{
		if (!IsEnemyTo(other.GetCurrentFaction())) return false;
		if (!other.IsCharacterRevealed()) return false;
		return Vector3.Distance(GetPosition(), other.GetPosition()) <= visionRange;
	});

	public bool InCombat() => GameTurnManager.Instance.IsTurnBasedMode() 
	                          && !IsDead() 
	                          && IsCharacterRevealed() 
	                          && (InParty() || HasDetectedAnyPartyCharacter());
	
	public bool HasDetectedAnyPartyCharacter() => _partyCharacters.Any(HasDetectedOther);
	
	public bool HasDetectedOther(CharacterEntity other) => detection && detection.IsCharacterDetected(other);
	
	public bool TryGetAnimation(out CharacterAnimation outAnimation)
	{
		outAnimation = characterAnimation;
		return outAnimation != null;
	}
	
	public Coroutine WaitAnimationEvent(CharacterAnimation.EventType eventType) => StartCoroutine(DoWaitAnimationEvent(eventType));
	
	private IEnumerator DoWaitAnimationEvent(CharacterAnimation.EventType eventType)
	{
		if (characterAnimation == null || !characterAnimation.HasAnimationEvent(CharacterAnimation.EventType.Hit))
			yield return new WaitForSeconds(defaultAttackHitTime);
		else
		{
			yield return characterAnimation.WaitEvent(eventType);
		}
	}
	public void SetItemToUse(ItemData itemToUse, bool verbose = false)
	{
		if (itemInHand != itemToUse)
		{
			if(verbose) Debug.Log(this + " SetItemToUse " + itemToUse);
			itemInHand = itemToUse;
			characterAnimation?.WeaponChanged();
		}
	}

	public bool HasItemToUse(out ItemData itemToUse)
	{
		return (itemToUse = itemInHand) != null;
	}
	
	#region PRIVATE
	private static List<CharacterEntity> _allCharacters = new List<CharacterEntity>();
	private static List<CharacterEntity> _partyCharacters = new List<CharacterEntity>();

	private int partyIndex = -1;
	private int dodgeCount = 0;
	private int riposteCount = 0;
	private int currentTurn; // TODO not in use
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
	private CharacterFollows follows;
	private WeaponData currentWeapon;
	private CharacterDetection detection;
	private ItemData itemInHand;
//	private CharacterLookAt characterLookAt;
	
	private void Awake()
	{
		if(characterData == null)
			Debug.LogError(this + " has no character data");
		
		_allCharacters.Add(this);
		
		agent = GetComponent<CharacterAgent>();
		if(!agent) agent = gameObject.AddComponent<CharacterAgent>();
		
		routines = GetComponent<CharacterRoutines>();
		if(!routines) routines = gameObject.AddComponent<CharacterRoutines>();
		
		follows = GetComponent<CharacterFollows>();
		if(!follows) follows = gameObject.AddComponent<CharacterFollows>();
		
		detection = GetComponent<CharacterDetection>();
		if (!detection) Debug.LogWarning(this + " has no detection component");
		
		characterAnimation = GetComponent<CharacterAnimation>();
		//if(!characterAnimation) characterAnimation = gameObject.AddComponent<CharacterAnimation>(); //TODO is it possible to have characters with no anims?
		
		currentFaction = characterData.GetFaction();
		if (factionOverride != CharacterFaction.None)
		{
			Debug.Log(this + " factionOverride=" + factionOverride);
			currentFaction = factionOverride;
		}

		if (!defaultWeapon)
		{
			Debug.LogWarning(this + " has no weapon, CreateInstance default");
			defaultWeapon = WeaponData.CreateInstance<WeaponData>();
		}

		characterCanvas.gameObject.SetActive(false);
		gameObject.SetActive(false); // wait for "Spawn"
		
		GameTurnManager.Instance.onTurnStarted += OnTurnStarted;
		GameTurnManager.Instance.onTurnEnded += OnTurnEnded;
	}

	private void OnDestroy()
	{
		_allCharacters.Remove(this);
		
		GameTurnManager.Instance.onTurnStarted -= OnTurnStarted;
		GameTurnManager.Instance.onTurnEnded -= OnTurnEnded;
	}

	private void OnTurnStarted()
	{
		if (IsCurrentTurn())
		{
			if(GameTurnManager.debugMode > 0) Debug.Log(this + " OnTurnStarted");
			turnStarted?.Invoke();
		}
	}

	private void OnTurnEnded()
	{
		if (IsCurrentTurn())
		{
			if(GameTurnManager.debugMode > 0) Debug.Log(this + " OnTurnEnded");
			turnEnded?.Invoke();
		}
	}

	private void SetCurrentWeapon(WeaponData newWeapon)
	{
		currentWeapon = newWeapon;

		if (characterAnimation)
		{
			characterAnimation.WeaponChanged();
		}
	}
	
	#endregion

	#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Utils.DrawGizmosCircle(transform.position, Vector3.up, visionRange);
	}
	#endif

	private void OnGUI()
	{
		if (debugStats > 0)
		{
			Utils.DrawWorldGUI(GetEyesPosition(), name  + (IsDead() ? "\nDEAD" : ""
			                                      + "\n" + GetCurrentHealth().ToString("N2") + "/" + GetMaxHealth().ToString("N2")
			                                      + "\n" + "Actions: " + usedActions.Count + "/" + characterData.GetMaxActions()
			                                      + "\n" + "MovePoints: " + usedMovePoints.ToString("N2") + "/" + GetMaxMovePoints().ToString("N2")
			                                      + "\n" + "HitChance: " + GetHitChance().ToString("N2")
			                                      + "\n" + "Dodge: " + GetDodgeChance().ToString("N2") + " (x" + dodgeCount + ")"
				), "label");
		}
	}


	public float GetStealth()
	{
		return characterData.GetStealthValue();
	}

	public bool RaycastForDestructible(out Destructible o)
	{
		if (Physics.SphereCast(new Ray(GetCenterPosition(), transform.forward), GetRadius() / 2f, out var hit, Mathf.Max(GetRadius(), GetAttackRange()), Layers.MaskDestructible))
		{
			if (hit.collider.TryGetComponent(out Destructible destructible))
			{
				o = destructible;
				return true;
			}
		}

		o = null;
		return false;
	}

	public bool ActionUsesDiceRolls(CharacterAction actionType)
	{
		return actionType == CharacterAction.Defend ? rollDiceOnDefense :
			actionType == CharacterAction.Riposte ? rollDiceOnRiposte :
			actionType == CharacterAction.Run ? rollDiceOnRun :
			false;
	}
}