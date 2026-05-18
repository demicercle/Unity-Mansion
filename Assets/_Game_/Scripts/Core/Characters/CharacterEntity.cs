using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RPGCharacterAnims.Actions;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.UIElements;

public interface IAttackable
{
	Vector3 GetPosition();
	Vector3 GetCenterPosition();
	float GetHeight();
	float GetRadius();
	void TakeDamage(DamageInfo damageInfo, object instigator);
	void AimedBy(CharacterEntity other);
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
	Defend,
	Riposte,
	Reload,
	UseItem,
	GiveItem,
	DropItem,
	PickupItem,
	LaunchItem,
	DrinkItem,
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
	private const float MinAttackRange = 0.05f;
	//private const float DodgeCountToChance = 2f / 20f;
	private const float MovePointsToDodge = 0.02f;
	private const float MovePointsToHit = 0.00f;

	public Vector3 GetReachPosition() => GetPosition();
	
	public static int debugStats { get; set; }
	public static int debugAim { get; set; }
	public static int debugRoutines => CharacterRoutines.debugMode;
	
	public static List<CharacterEntity> AllCharacters => _allCharacters;

	public static CharacterEntity[] GetPartyCharacters() => _partyCharacters.ToArray();

	public static bool PartyGathered(float distance)
	{
		foreach (var pc0 in _partyCharacters)
		{
			if (pc0.IsDead()) continue;
			if (!IsAtDistance(pc0, distance))
				return false;
		}

		return true;
	}

	public static bool IsAtDistance(CharacterEntity partyChar, float distance)
	{
		foreach (var otherPartyChar in _partyCharacters)
		{
			if (otherPartyChar.IsDead() || otherPartyChar == partyChar) continue;
			Debug.Log("IsAtDistance " + partyChar  + " of " + otherPartyChar + " " + otherPartyChar.GetDistanceTo(partyChar.GetPosition()) + " / " + distance);
			if (otherPartyChar.GetDistanceTo(partyChar.GetPosition()) <= distance)
				return true;
		}

		return false;
	}

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
	public event System.Action onRevive;
	public event System.Action onMoved;
	//public event System.Action onRotate;
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
	[SerializeField] private bool canOpenDoors = false;
	[Range(0f, 10f)] [SerializeField] private float visionRange = 2.0f; // TODO remove
	[Min(0f)] [SerializeField] private float defaultAttackHitTime = 1f;
	[SerializeField] private CharacterFaction factionOverride;
	[SerializeField] private WeaponData defaultWeapon;
	[SerializeField] private bool noRevive = false;
	public CharacterRoutines Routines => routines;

	public bool CanOpenDoors()
	{
		return canOpenDoors;
	}

	public bool IsName(string savedPCName)
	{
		return name == savedPCName;
	}
	
	public void FloatingText_Cant() // TODO: in character animation
	{
		Debug.Log(this + " Cant!");
		if (IsCurrentTurn()) FloatingText(LanguageSettings.Main.Cant);
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
		if (TryGetComponent(out Inventory inventory) && inventory.TryGetItem(Inventory.InventorySlot_Weapon, out var weaponItem) && weaponItem.IsWeapon(out var weaponData))
		{
			SetCurrentWeapon(weaponData);
		}
		else
		{
			SetCurrentWeapon(defaultWeapon);
		}
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
		if (!value) SetAiming(false);
		onSelectionChanged?.Invoke(value);
	}

	public bool TryGetClosestEnemy(out CharacterEntity closestEnemy)
	{
		closestEnemy = null;
		
		var closestDistance = 0f;
		foreach (var enemy in AllCharacters)
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

	public void Rest(bool verbose = false)
	{
		if(verbose) Debug.Log(this + " Rest");
		usedActions.Clear();
		usedMovePoints = 0;
		dodgeCount = 0;
		riposteCount = 0;
		extraActions = 0;
		onStatsChanged?.Invoke();
	}
	
	public bool HasActions() => !GameTurnManager.Instance.IsTurnBasedMode() || usedActions.Count < characterData.GetMaxActions() + extraActions;

	public bool HasUsedAction(CharacterAction action) => usedActions.Contains(action);

	public bool CanUseAction(CharacterAction _) // TODO
	{
		return HasActions();
	}

	public bool CanUseItem(ItemData item) => CanUseAction(item.GetCharacterAction());

	private IAttackable lastAttackTarget;
	public IAttackable GetLastAttackTarget() => lastAttackTarget;
	
	private object lastDamageInstigator;
	public bool TryGetLastDamageInstigator(out CharacterEntity instigator)
	{
		return (instigator = lastAttackTarget as CharacterEntity) != null;
	}
	
	public int RollDice()
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
		if (IsAtAttackRange(attacker, true) && riposteCount > 0)
		{
			Debug.Log(this + " TryRiposte: " + attacker + " " + riposteCount);
			riposteCount--; // riposte
			Routines.AssignPerformAttack(attacker, false);
			return true;
		}

		return false;
	}

	public bool CanDodge() => characterData.GetDodgeChance() > 0 && dodgeCount > 0;
	
	public int GetDodgeChance() => Mathf.Clamp(characterData.GetDodgeChance() + Mathf.RoundToInt(GetRemainingMovePoints() * MovePointsToDodge), 1, 20);

	public int GetHitChance() => Mathf.Clamp(currentWeapon.GetHitChance() + Mathf.RoundToInt(GetRemainingMovePoints() * MovePointsToHit), 1, 20);

	public int GetHitChanceModifiedByRange(float range, float radius)
	{
		return (int)(GetHitChance() *
		             Mathf.Clamp01(currentWeapon.EvaluateRange(range, GetRadius() + radius)));
	}

	public int GetHitChanceHeightModifier(float height)
	{
		return (int)(currentWeapon.EvaluateHeight(Mathf.Abs(height)) * 20f);
	}
	
	public enum AttackResult
	{
		Undefined,
		Hit,
		Miss,
		Dodge,
	}
	
	public AttackResult TryHit(IAttackable target, int diceResult)
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

	private int calculatedHitChance;
	private float targetRange, targetHeight;

	public int CalculateHitChance(Vector3 targetPosition, float targetRadius)
	{
		if (IsUsingRangeWeapon())
		{
			targetRange = Vector3.Distance(GetReachPosition(), targetPosition);
			targetHeight = GetPosition().y - targetPosition.y;
			return GetHitChanceModifiedByRange(targetRange, targetRadius) + GetHitChanceHeightModifier(targetHeight);
		}
		else
		{
			return GetHitChance();
		}
	}
	
	public int CalculateHitChance(CharacterEntity target)
	{
		calculatedHitChance = CalculateHitChance(target.GetReachPosition(), target.GetRadius());
		
		if (!IsUsingRangeWeapon())
		{
			if (target.CanDodge())
			{
				calculatedHitChance = Mathf.RoundToInt(calculatedHitChance * (1f - target.GetDodgeChance() / 20f));
			}
		}
		
		return Mathf.Clamp(calculatedHitChance, 1, 20);
	}
	
	public AttackResult TryHit(CharacterEntity target, int d20)
	{
		var hitChance = CalculateHitChance(target.GetReachPosition(), target.GetRadius());
		
		if(DicePool.debugMode>0) Debug.Log(this + " TryHit " + target + " d20=" + d20 + " hitChance=" + hitChance);
		
		if (d20 <= 1 || 20-d20 > hitChance)
		{
			onAttackResult?.Invoke(this, target, AttackResult.Miss);
			return AttackResult.Miss;
		}

		if (d20 < 20)
		{
			if (!IsUsingRangeWeapon())
			{
				if (target.CanDodge())
				{
					target.dodgeCount = Mathf.Max(0, target.dodgeCount - 1);
					if (rollDiceToDodge ? target.RollDice() < target.GetDodgeChance() : target.GetDodgeChance() > d20)
					{
						onAttackResult?.Invoke(this, target, AttackResult.Dodge);
						return AttackResult.Dodge;
					}
				}
			}
		}
			
		onAttackResult?.Invoke(this, target, AttackResult.Hit);
		return AttackResult.Hit;
	}
	
	public void UseAction(CharacterAction actionType, bool useAction = true, object param = null)
	{
		if (useAction)
		{
			usedActions.Add(actionType);
		}
		
		if(debugStats > 0) Debug.Log(this + " UseAction " + actionType + " param=" + param + " usedActions=" + usedActions.LastOrDefault());
		
		if (actionType == CharacterAction.Run)
		{
			if (rollDiceOnRun) RollDice();
			usedMovePoints -= characterData.GetRunActionExtraMovePoints();
			FloatingText(LanguageSettings.Main.Run);
		}
		else if (actionType == CharacterAction.Attack)
		{
			lastAttackTarget = param as CharacterEntity;
			if (currentWeapon.IsReloadable())
			{
				currentWeapon.ConsumeAmmo();
			}
		}
		else if (actionType == CharacterAction.Talk)
		{
			(param as CharacterEntity)?.onTalkedBy?.Invoke(this);
		}
		else if (actionType == CharacterAction.Defend)
		{
			if (rollDiceOnDefense) RollDice();
			dodgeCount++;
			FloatingText(LanguageSettings.Main.Defend);
		}
		else if (actionType == CharacterAction.Riposte)
		{
			if (rollDiceOnRiposte) RollDice();
			riposteCount++;
			FloatingText(LanguageSettings.Main.Ready);
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
	
	public CharacterEntity[] GetLivingEnemyCharacters() => AllCharacters.Where(c => !c.IsDead() && IsEnemyTo(c)).ToArray();
	
	public float GetClosestDistanceToAnyPartyCharacter(out CharacterEntity closestChar)
	{
		closestChar = null;
		
		var closestDistance = -1f;
		foreach (var partyChar in _partyCharacters)
		{
			var dist = GetDistanceTo(partyChar.GetPosition());
			if (closestChar == null || closestDistance > dist)
			{
				closestChar = partyChar;
				closestDistance = dist;
			}
		}
		
		return closestDistance;
	}
	
	

	public Vector3 GetAvoidPosition(Vector3 dummyPosition, float avoidDistance)
	{
		var dir = dummyPosition - GetPosition();
		dir.y = 0;
		dir.Normalize();
		
		var dest = GetPosition() - dir * avoidDistance;
		if (!NavUtils.TryGetNearestNavPosition(dest, GetRadius() * 2, out dest) || !RoomTrigger.IsPositionVisible(dest))
		{
			dest = GetPosition() + dir * avoidDistance;
			if (!NavUtils.TryGetNearestNavPosition(dest, GetRadius() * 2, out dest))
			{
				return dummyPosition;
			}
		}

		return dest;
	}
	
	public bool IsEnemyToPlayer(CharacterEntity other = null) => currentFaction == CharacterFaction.Enemy;

	public bool ShouldTalk(CharacterEntity other) => other.GetCurrentFaction() == CharacterFaction.Neutral &&
	                                                 GetCurrentFaction() == CharacterFaction.Player &&
	                                                 !other.IsDead() && !IsDead();

	public float GetTalkDistance(CharacterEntity other) =>
		other.GetRadius() + GetRadius() + agent.GetMinReachDistance() + other.agent.GetMinReachDistance();
	
	public bool ShouldAttack(CharacterEntity other) => !other.IsDead() && IsEnemyTo(other);
	public bool IsEnemyTo(CharacterEntity other) => IsEnemyTo(other.GetCurrentFaction());
	public bool IsEnemyTo(CharacterFaction faction)
	{
		if (currentFaction == CharacterFaction.Enemy)
			return faction != CharacterFaction.Enemy;
		else
			return faction == CharacterFaction.Enemy;
	}

	
	public bool IsAllyTo(CharacterEntity other) => IsAllyTo(other.GetCurrentFaction());
	public bool IsAllyTo(CharacterFaction faction)
	{
		if (currentFaction == CharacterFaction.Enemy)
			return faction == CharacterFaction.Enemy;
		else
			return faction != CharacterFaction.Enemy;
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

	public void SetCurrentFaction(CharacterFaction faction)
	{
		if (currentFaction == faction) return;
		if(debugStats > 0) Debug.Log(this  + " SetCurrentFaction " + currentFaction + " to " + faction);
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

	private Quaternion tempRotation;
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

		tempRotation = transform.rotation;
		
		if (f < 0f)
		{
			//DebugDrawer.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 1.0f);
			tempRotation = Quaternion.LookRotation(getDirection);
		}
		else
		{
			//DebugDrawer.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 0.0f);
			tempRotation = Quaternion.RotateTowards(tempRotation, Quaternion.LookRotation(getDirection), f * agent.GetRotationSpeed()); //TODO property "rotationSpeed" separated from agent
		}

		agent.SetRotationY(tempRotation.eulerAngles.y);
		transform.rotation = tempRotation;
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
	
	public Vector3 GetEyesPosition(bool local = false) => (!local ? GetPosition() : Vector3.zero) + Vector3.up * GetAgent().GetHeight() * 0.9f; // TODO "GetAgent()"

	public Vector3 GetShootFromPosition() => GetEyesPosition() + currentWeapon.GetShootOffset();
	
	public Vector3 GetCenterPosition() => GetCenterPosition(false);
	public Vector3 GetCenterPosition(bool local) => (!local ? GetPosition() : Vector3.zero) + Vector3.up * GetAgent().GetHeight() * 0.5f;  // TODO "GetAgent()"
	
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
		if(follows) follows.enabled = false;
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
	
	public int GetMaxHealth() => characterData.GetMaxHealth() + extraHP;
	public int GetCurrentHealth() => GetMaxHealth() - Mathf.CeilToInt(GetTotalDamages());
	public float GetDamageProgress() => Mathf.Clamp01(GetCurrentHealth() / (float)GetMaxHealth());
	public bool IsDead() => isDead;


	public string GetHealthDescription()
	{
		var health01 = (float)GetCurrentHealth() / (float)GetMaxHealth();
		if (health01 <= 1f / 5f) return LanguageSettings.GetDialogLines(DialogFiles.main).GetLine(26, "HP 20%");
		if (health01 <= 2f / 5f) return LanguageSettings.GetDialogLines(DialogFiles.main).GetLine(25, "HP 40%");
		if (health01 <= 3f / 5f) return LanguageSettings.GetDialogLines(DialogFiles.main).GetLine(24, "HP 60%");
		if (health01 < 1f) return LanguageSettings.GetDialogLines(DialogFiles.main).GetLine(23, "HP 80%");
		return LanguageSettings.GetDialogLines(DialogFiles.main).GetLine(22, "HP 100%");
	}

	public void AddMaxHealth(int i)
	{
		extraHP += i;
	}
	
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

		if (isDead && !noRevive && GetCurrentHealth() > 0)
		{
			Debug.Log(this + " Revive!");
			isDead = false;
			agent.SetAgentActive(true);
			gameObject.layer = Layers.LayerCharacter;
			onRevive?.Invoke();
		}
		
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

	public void AimedBy(CharacterEntity otherCharacter)
	{
		foreach (var outline in GetComponentsInChildren<OutlineRenderer>())
		{
			outline.CursorOver();
		}
	}
	
	public void TakeDamage(DamageInfo damageInfo, object objectFrom)
	{
		Debug.Log(this + " TakeDamage " + damageInfo + " from " + objectFrom);
		totalDamages += damageInfo.amount;
		lastDamageInstigator = objectFrom;
		
		StopAgent();

		onDamaged?.Invoke(damageInfo.amount);
		onAnyCharacterDamaged?.Invoke(this, damageInfo, objectFrom);
		
		if (GetCurrentHealth() <= 0f)
		{
			if (TryKill())
			{
				if (objectFrom is CharacterEntity)
				{
					if (Squad.TryFindSquads(this, out var squads))
					{
						foreach (var squad in squads)
						{
							squad.AlertAllies(this, objectFrom as CharacterEntity);
						}
					}
				}
				
				return;
			}
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
			gameObject.layer = Layers.LayerDead;
			
			if (TryGetComponent(out Inventory inv)) 
				inv.DropAllOnGround();
			
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

	private const float MaxInteractHeight = 2.0f;
	private const float MaxInteractNavDistance = 1.5f;
	public bool TryGetClosestNavigablePosition(Vector3 world, out Vector3 navPos)
	{
		navPos = world;

		if (NavUtils.TryGetNearestNavPosition(navPos, MaxInteractNavDistance * 2, out var navPosY))
		{
			navPos.y = navPosY.y;
		}
		
		//DebugDrawer.Line(navPos, world, Color.yellow, 1.0f);

		if (Utils.VDistance(navPos, world) > MaxInteractHeight)
		{
			//Debug.Log(this + " TryGetClosestNavigablePosition invalid height");
			return false;
		}

		var hDist = Utils.HDistance(navPos, world);
//		Debug.Log(this + " TryGetClosestNavigablePosition hDist=" + hDist);
		return hDist <= MaxInteractNavDistance;
	}

	public float GetDistanceTo(Vector3 world) => NavUtils.NavDistance(GetPosition(), world);
	
	public bool WaypointReached(Waypoint wp)
	{
		return agent.ReachedDestination(wp.GetPosition(), wp.radius);
		//return Vector3.Distance(GetPosition(), wp.GetPosition()) <= agent.reachDistance;
	}

	public float GetAttackRange()
	{
		if (IsUsingRangeWeapon())
		{
			return currentWeapon.GetAttackRange();
		}
		else
		{
			return agent.GetRadius() + Mathf.Max(MinAttackRange, currentWeapon.GetAttackRange());
		}
	}
	
	public float GetAttackReachDistance(IAttackable target)
	{
		return Mathf.Max(MinAttackRange, currentWeapon.GetAttackRange(), agent.GetRadius() + target.GetRadius() + agent.GetMinReachDistance());
	}

	public bool IsAtAttackRange(IAttackable target, bool losCheck)
	{
		var atkRange = GetAttackReachDistance(target) + MinAttackRange;
		if (IsUsingRangeWeapon())
		{
			var dist = GetDistanceTo(target.GetPosition());
			if (dist > atkRange)
				return false;
		}
		else
		{
			var hDist = NavUtils.NavHDistance(GetPosition(), target.GetPosition());
			var vDist = NavUtils.NavVDistance(GetPosition(), target.GetPosition());
			if (vDist > GetHeight() || hDist > atkRange)
				return false;
		}

		return !losCheck || LOSCheck(target);
	}
	
	public bool LOSCheck(IAttackable other)
	{
		if (other == null) return false;
		var start = GetEyesPosition();
		return LOSCheck(start, other.GetPosition())
		       || LOSCheck(start, other.GetCenterPosition())
		       || LOSCheck(start, other.GetPosition() + Vector3.up * other.GetHeight(), other is Component ? (other as Component).GetComponent<Collider>() : null);
	}

	public bool LOSCheck(Vector3 pos) => LOSCheck(GetCenterPosition(), pos);

	public bool LOSCheck(Vector3 start, Vector3 end, Collider other = null)
	{
		if(debugRoutines > 0) DebugDrawer.Line(start, end, Color.green, 1.0f);
		
		/*if(Physics.Linecast(start, end, out var hit, Layers.MaskObstruction) && hit.collider != other)
		{
			if(debugRoutines > 0) DebugDrawer.Line(start, hit.point, Color.red, 1.0f);
			if(debugRoutines > 0) Debug.Log(this + " LOSCheck  collide " + hit.collider);
			return false;
		}*/

		foreach (var hit in Physics.RaycastAll(new Ray(start, (end - start).normalized), Vector3.Distance(start, end),
			         Layers.MaskObstruction).OrderBy(hit => hit.distance))
		{
			if (hit.collider != other)
			{
				if(debugRoutines > 0) DebugDrawer.Line(start, hit.point, Color.red, 1.0f);
				//if(debugRoutines > 0) Debug.Log(this + " LOSCheck  collide " + hit.collider);
				return false;
			}
		}
		
		return true;
	}
	
	public float GetLastAttackDamage(int atkResult)
	{
		return currentWeapon.RollDamage(Mathf.Clamp(atkResult, 1, 20) / 20f);
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
	public void SetItemToUse(ItemData itemToUse)
	{
		if (itemInHand != itemToUse)
		{
			if(CharacterAnimation.debugMode > 0) Debug.Log(this + " SetItemToUse " + itemToUse);

			if (itemInHand && itemInHand.EquipOnHand())
			{
				characterAnimation?.SwitchWeapons(false);
			}
			
			itemInHand = itemToUse;
			
			if (itemInHand)
			{
				if (itemInHand.EquipOnHand())
				{
					SetAiming(false);
					characterAnimation?.SwitchWeapon(itemInHand.GetWeaponInHand());
				}
			}
			else
			{
				if(InBattle()) 
					characterAnimation?.SwitchWeapons(true);
			}
		}
	}

	public bool HasItemToUse(out ItemData itemToUse)
	{
		return (itemToUse = itemInHand) != null;
	}


	public void SetCurrentWeapon(WeaponData newWeapon)
	{
		if (currentWeapon != newWeapon)
		{
			currentWeapon = newWeapon;
			characterAnimation?.WeaponChanged();
		}
	}

	
	#region PRIVATE
	private static List<CharacterEntity> _allCharacters = new List<CharacterEntity>();
	private static List<CharacterEntity> _partyCharacters = new List<CharacterEntity>();

	private int extraHP = 0;
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
	private Inventory selfInventory;
//	private CharacterLookAt characterLookAt;
	
	private void Awake()
	{
		if(characterData == null)
			Debug.LogError(this + " has no character data");
		
		_allCharacters.Add(this);
		
		selfInventory = GetComponent<Inventory>();
		if(!selfInventory) selfInventory = gameObject.AddComponent<Inventory>();
		
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

		currentWeapon = defaultWeapon;
		
		characterCanvas.gameObject.SetActive(false);
	}

	private void Start()
	{
		gameObject.SetActive(false); // wait for "Spawn"
	}

	private void OnDestroy()
	{
		_allCharacters.Remove(this);
	}

	private void OnEnable()
	{
		GameTurnManager.Instance.onTurnStarted += OnTurnStarted;
		GameTurnManager.Instance.onTurnEnded += OnTurnEnded;

		Inventory.onAnyInventoryChanged += OnAnyInventoryChanged;
	}

	private void OnDisable()
	{
		GameTurnManager.Instance.onTurnStarted -= OnTurnStarted;
		GameTurnManager.Instance.onTurnEnded -= OnTurnEnded;
		
		Inventory.onAnyInventoryChanged -= OnAnyInventoryChanged;
	}

	private void OnAnyInventoryChanged(Inventory inventory)
	{
		if (selfInventory == inventory)
		{
			if (inventory.TryGetItem(Inventory.InventorySlot_Weapon, out var weaponItem) && weaponItem.IsWeapon(out var weaponData))
			{
				SetCurrentWeapon(weaponData);
			}
			else
			{
				SetCurrentWeapon(defaultWeapon);
			}
		}
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
			var debugString = name;
			
			if (IsDead())
				debugString += "\nDEAD";
			
			debugString += "\n" + GetCurrentHealth().ToString("N2") + "/" + GetMaxHealth().ToString("N2");
				
			if (debugStats > 1)
			{
				debugString += "\n" + "Actions: " + usedActions.Count + "/" + characterData.GetMaxActions();
				debugString += "\n" + "MovePoints: " + usedMovePoints.ToString("N2") + "/" + GetMaxMovePoints().ToString("N2");
				debugString += "\n" + "HitChance: " + GetHitChance().ToString("N2");
				debugString += "\n" + "Dodge: " + GetDodgeChance().ToString("N2") + " (x" + dodgeCount + ")";
			}

			Utils.DrawWorldGUI(GetEyesPosition(), debugString, "label");
		}
	}


	public float GetStealth()
	{
		return characterData.GetStealthValue();
	}

	[SerializeField] private float raycastForwardOffset = 0.5f;
	[SerializeField] private float raycastRadiusScale = 0.2f;
	public bool RaycastForDestructible(out Destructible o)
	{
		var pos = GetPosition() + GetDirection() * GetRadius() * raycastForwardOffset;
		var radius = GetRadius() * raycastRadiusScale;
		if(debugRoutines > 0) DebugDrawer.WireDisc(pos, radius, Color.red, 0.5f);
		foreach(var coll in Physics.OverlapSphere(pos, radius, Layers.MaskDestructible))
		{
			if (coll.TryGetComponent(out Destructible destructible))
			{
				if (destructible.IsDoor())
				{
				/*	if (!IsOnLastAgentPath(destructible.gameObject))
					{
						if(debugRoutines > 0) Debug.Log(this + " raycast for destructible " + destructible + " but it is not on path");
						continue;
					}*/
					
					o = destructible;
					return true;
				}
			}
		}

		o = null;
		return false;
	}

	/*private bool IsOnLastAgentPath(GameObject go)
	{
		if(debugRoutines > 0) DebugDrawer.Line(GetPosition(), go.transform.position, Color.lightPink, 1.0f);
		var offset = agent.GetHeight() * 0.5f * Vector3.up;
		var lastPath = agent.GetCurrentPath();
		for (int i = 1; i < lastPath.Length; i++)
		{
			var p0 = lastPath[i - 1] + offset;
			var p1 = lastPath[i] + offset;
			if(debugRoutines > 0) DebugDrawer.Line(p0, p1, Color.magenta, 1.0f);
			if (Physics.Linecast(p0, p1, out var hit, go.layer))
			{
				if (hit.collider.gameObject == go)
				{
					return true;
				}
			}
		}

		return false;
	}*/

	public bool ActionUsesDiceRolls(CharacterAction actionType)
	{
		return actionType == CharacterAction.Defend ? rollDiceOnDefense :
			actionType == CharacterAction.Riposte ? rollDiceOnRiposte :
			actionType == CharacterAction.Run ? rollDiceOnRun :
			false;
	}

	public bool InBattle() => GameTurnManager.Instance.IsTurnBasedMode() 
	                         && !IsDead() 
	                         && IsCharacterRevealed() 
	                         && (InParty() || HasDetectedAnyPartyCharacter());

	private bool isAiming;
	private Vector3 aimingPosition;
	private RaycastHit lastShootHitResult;

	public event System.Action<bool, Vector3> onAiming;

	public bool IsAiming() => isAiming;

	public bool TryGetAimingPosition(out Vector3 world)
	{
		world = aimingPosition;
		return isAiming;
	}

	public bool WillHit(GameObject target)
	{
		return lastShootHits.Any(hit => hit.collider?.gameObject == target);
	}

	public bool GetShootLines(out Vector3[] hitPoints)
	{
		hitPoints = new Vector3[lastShootHits.Length];
		for (int i = 0; i < hitPoints.Length; i++)
		{
			hitPoints[i] = lastShootHits[i].point;
		}
		return isAiming && hitPoints.Length > 0;
	}

	private void FixedUpdate()
	{
		if (isAiming)
		{
			var from = GetShootFromPosition();
			
			var dir = (aimingPosition - from).normalized;
			if (currentWeapon.ShootsFlat() && Mathf.Abs(from.y - aimingPosition.y) <= GetHeight())
			{
				dir.y = 0;
				dir.Normalize();
			}
			
			//if(debugAim > 1) Debug.Log(this + " isAiming FixedUpdate dir=" + dir);
			
			var range = GetAttackRange();
			
			if (currentWeapon.GetShootInfos(out var shoots))
			{
				lastShootHits = new RaycastHit[shoots.Length];
				for(int i = 0; i < shoots.Length; i++)
				{
					var rotatedDir = Quaternion.Euler(0, shoots[i].angle, 0) * dir;
					
					lastShootHits[i].point = from + rotatedDir * range;
					
					if (Physics.Raycast(from, rotatedDir, out var hit, range, Layers.MaskAiming))
						lastShootHits[i] = hit;
				}
			}
			else
			{
				lastShootHits = new RaycastHit[1];
				lastShootHits[0].point = from + dir * range;
				if (Physics.Raycast(from, dir, out var hit, range, Layers.MaskAiming))
					lastShootHits[0] = hit;
			}
			
			if (debugAim > 0)
			{
				//DebugDrawer.Line(from, from + dir * range, Color.yellow, 0.2f);
			
				for (int i = 0; i < lastShootHits.Length; i++)
				{
					DebugDrawer.Line(from, lastShootHits[i].point, lastShootHits[i].collider ? Color.red : Color.green, Routines.AnyActive() ? 1.0f : 0.0f);
					if (debugAim > 1) Debug.Log(this + " range=" + range + " hits " + i + " " + lastShootHits[i].collider);
				}
			}
		}
	}

	private RaycastHit[] lastShootHits = Array.Empty<RaycastHit>();
	public bool GetLastShootHits( out RaycastHit[] hits)
	{
		hits = lastShootHits;
		return hits.Length > 0;
	}

	public void SetAimingPosition(Vector3 world)
	{
		SetAiming(true);
		aimingPosition = world;
		if(debugAim > 0) DebugDrawer.Sphere(aimingPosition, 0.1f, Color.darkRed, Routines.AnyActive() ? 1.0f : 0.0f);
		if(debugAim > 0) DebugDrawer.Line(aimingPosition, GetShootFromPosition(), Utils.ColorAlpha(Color.darkRed, 0.5f), Routines.AnyActive() ? 1.0f : 0.0f);
		onAiming?.Invoke(isAiming, world);
	}
	
	public void SetAiming(bool value)
	{
		if (isAiming && !value)
			onAiming?.Invoke(false, aimingPosition);
		
		isAiming = value;
		if(debugAim > 1) Debug.Log(this + " ToggleAim " + value);
	}

	public bool IsActing() => Routines.AnyActive();

	public void Stop()
	{
		StopAgent();
		Routines.StopAllRoutines();
	}

	public bool HasWeaponToReload()
	{
		return currentWeapon.IsReloadable();
	}

	private int extraActions;

	public void GiveExtraActions(int amount)
	{
		extraActions += amount;
		onStatsChanged?.Invoke();
	}

	public int GetRemainingActions()
	{
		return characterData.GetMaxActions() + extraActions - usedActions.Count;
	}

	public bool CanSwitchWeapons()
	{
		return !IsDead() && !IsActing() && !(characterAnimation && characterAnimation.IsSwitchingWeapons);
	}

	public bool WeaponNeedsReload()
	{
		return currentWeapon.IsReloadable() && currentWeapon.IsEmptyAmmo();
	}

	public bool TryFindAmmo(out ItemData ammoItem)
	{
		foreach (var item in selfInventory.GetItems())
		{
			if (currentWeapon.CanBeReloadedWithAmmo(item))
			{
				ammoItem = item;
				return true;
			}
		}

		ammoItem = null;
		return false;
	}

	public void SwitchDefaultWeapon()
	{
		SetCurrentWeapon(defaultWeapon);
	}

	public Vector3 GetAimingPosition(IAttackable target)
	{
		var result = target.GetPosition();
		if (target is CharacterEntity)
			result = (target as CharacterEntity).GetEyesPosition();
		if (target is Destructible)
			result = (target as Destructible).GetCenterPosition();

		if (currentWeapon.ShootsFlat())
		{
			var shootFrom = GetShootFromPosition();
			var height = GetHeight();
			if (Mathf.Abs(result.y - shootFrom.y) <= height)
				result.y = shootFrom.y;
		}

		return result;
	}
}