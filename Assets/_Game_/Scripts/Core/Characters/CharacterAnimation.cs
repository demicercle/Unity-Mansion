using System;
using System.Collections;
using System.Collections.Generic;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.Events;
using RPGCharacterAnims.Extensions;
using Unity.VisualScripting;

public class CharacterAnimation : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	public enum EventType
	{
		Undefined,
		Hit,
		Shoot,
		WeaponSwitch,
	}
	
	//public const string EventHit = "Hit";
	
	static public List<CharacterAnimation> activeInstances  = new List<CharacterAnimation>();
	
	[SerializeField] private Animator animator;
	[SerializeField] private RPGAnimationHandler rpgAnimationHandler;
	[SerializeField] [Min(0)] private float switchWeaponExtraTime;
	[SerializeField] [Min(0)] private float waitToRelaxTime = 6.0f;
	
	[System.Serializable]
	public struct Parameters
	{
		[Header("Movement")]
		public string moving;
		public string velocityX;
		public string velocityZ;
		[Header("Actions")]
		public string runTrigger;
		public string attackTrigger;
		[Header("Reactions")]
		public string deathTrigger;
		public string getHitTrigger;
		public string switchOffTrigger;
		public string switchOnTrigger;
		public string instantSwitchTrigger;
		//[Header("RPG Anims")]
		//public string weapon;
		//public string triggerNumber;
		//public string trigger;
	}
	
	[SerializeField] private Parameters parameters;
	[SerializeField] private List<EventType> eventTypes = new List<EventType>();

#if UNITY_EDITOR
	/*[ContextMenu("SetParameters/RPGCharacter")]
	void SetParameters_RPGCharacter()
	{
		UnityEditor.Undo.RecordObject(this, "SetParameters/RPGCharacter");
		parameters = new Parameters()
		{
			moving = "Moving",
			velocityX = "Velocity X",
			velocityZ = "Velocity Z",
			weapon = "Weapon",
			triggerNumber = "TriggerNumber",
			trigger = "Trigger"
		};
		Utils.SetDirty(this);
	}*/
	
	[ContextMenu("ClearParameters")]
	void ClearParameters()
	{
		UnityEditor.Undo.RecordObject(this, "ClearParameters");
		parameters = new Parameters();
		Utils.SetDirty(this);
	}
#endif
	
	[Space]
	[SerializeField] private float velocityScale = 1.0f;
	[SerializeField] private float minMovePointsToRun = 0.1f;
	[SerializeField] private float roomsReveleadSeconds = 1.0f;
	[SerializeField] private bool shouldSwitchWeaponWhenDamaged = true;

	public bool IsSwitchingWeapons => isSwitchingWeapons;

	public bool HasAnimationEvent(EventType eventType) => eventTypes.Contains(eventType);

	public Coroutine WaitEvent(EventType eventType, float timeout = 6.0f) => StartCoroutine(DoWaitEvent(eventType, timeout));
	
	private IEnumerator DoWaitEvent(EventType eventType, float timeout)
	{
		var startTime = Time.time;
		if (debugMode > 0) Debug.Log(this + " DoWaitEvent " + eventType);
		waitForEvent = eventType;
		do
		{
			if (timeout > 0f && Time.time - startTime > timeout)
			{
				Debug.LogWarning(this + " timeout! "  + eventType);
				yield break;
			}
			
			yield return null;
		} while (waitForEvent != EventType.Undefined);
	}

	public void InstantSwitchWeapons()
	{
		if (WeaponsSwitched())
		{
			return;
		}
		
		if(debugMode >0 )Debug.Log(this + " InstantSwitchWeapons");
		if (rpgAnimationHandler) rpgAnimationHandler.InstantWeaponSwitch();
		TrySetTrigger(parameters.instantSwitchTrigger);
		weaponSwitched = true;
		UpdateWeaponRenderers();
	}

	public bool TrySwitchWeapons(bool weaponsReady) // TODO instant switch weapons
	{
		if (_selfCharacter.IsDead())
			return false;
		
		if (weaponSwitched == weaponsReady)
		{
			if(debugMode >0 )Debug.Log(this + " weapons already switched");
			return false; 
		}
		
		StopAllCoroutines();
		
		if (rpgAnimationHandler == null) // RPG Character Animation override
		{
			if (weaponsReady && !hasSwitchOnTrigger) return false; // no animator parameter for true
			if (!weaponsReady && !hasSwitchOffTrigger) return false; // no animator parameter for false
		}
		
		StartCoroutine(DoWaitSwitchWeapons(weaponsReady)); // do not returns coroutine, use a bool "IsSwitchingWeapons"
		return true;
	}

	public bool WeaponsSwitched() => weaponSwitched;

	public void WeaponChanged()
	{
		UpdateWeaponRenderers();
	}

	/*public void SetWeapon(Weapon rpgWeapon, bool isLeft)
	{
		
	}*/
	
	#region PRIVATE
	private CharacterAnimationEvent _characterAnimationEvent;
	private CharacterController _selfCharacter;
	private EventType waitForEvent;
	private bool weaponSwitched;
	private bool wasDamaged, wasAttacking, wasMoving;
	private bool hasSwitchOnTrigger, hasSwitchOffTrigger, isSwitchingWeapons;
	private WeaponRenderer[] weaponRenderers;
	
	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterController>();
		if (!_selfCharacter)
		{
			Debug.LogWarning(this + " has no CharacterController");
			enabled = false;
			return;
		}
		
		if (!animator)
		{
			Debug.LogWarning(this + " disabled because no animator assigned");
			enabled = false;
			return;
		}
		
		_characterAnimationEvent = animator.GetComponent<CharacterAnimationEvent>();
		if(!_characterAnimationEvent) _characterAnimationEvent = animator.gameObject.AddComponent<CharacterAnimationEvent>();
		_characterAnimationEvent.onCharacterAnimationEvent += OnCharacterAnimationEvent;
		
		if(!rpgAnimationHandler) rpgAnimationHandler = GetComponent<RPGAnimationHandler>();
		if(!rpgAnimationHandler) rpgAnimationHandler = GetComponentInChildren<RPGAnimationHandler>();

		weaponRenderers = GetComponentsInChildren<WeaponRenderer>();

		_selfCharacter.onMoved += OnCharacterMoved;
		_selfCharacter.onHealed += OnHealed;
		_selfCharacter.onDamaged += OnDamaged;
		_selfCharacter.onDead += OnDead;
		_selfCharacter.onUseAction += OnUseAction;
		_selfCharacter.turnStarted += OnTurnStarted;
		_selfCharacter.turnEnded += OnTurnEnded;
		_selfCharacter.onDamagedBy += OnDamagedBy;

		RoomTrigger.onChanged += OnRoomsChanged;
	}
	
	private void OnEnable()
	{
		activeInstances.Add(this);
	}

	private void Start()
	{
		hasSwitchOnTrigger = !string.IsNullOrWhiteSpace(parameters.switchOnTrigger);
		hasSwitchOffTrigger = !string.IsNullOrWhiteSpace(parameters.switchOffTrigger);
		
		TrySetFloat(parameters.velocityX, 0.0f);      
		TrySetFloat(parameters.velocityZ, 0.0f);        
		TrySetBool(parameters.moving, false);

		if (rpgAnimationHandler)
		{
			rpgAnimationHandler.RelaxOnStart();
		}
		
		if(!TrySwitchWeapons(weaponSwitched))
			UpdateWeaponRenderers(); // instant update
		
		if (waitForEvent != EventType.Undefined) 
			Debug.LogError(this + " has waitForEvent on start " + waitForEvent);
	}

	private void OnDisable()
	{
		activeInstances.Remove(this);
	}

	private void OnDestroy()
	{
		RoomTrigger.onChanged -= OnRoomsChanged;
		
		_selfCharacter.onMoved -= OnCharacterMoved;
		_selfCharacter.onHealed -= OnHealed;
		//controller.onPartyIndexChanged -= (newIndex) =>
		_selfCharacter.onDamaged -= OnDamaged;
		_selfCharacter.onDead -= OnDead;
		//controller.onMoved -= () =>
		//controller.onRotate -= () =>
		_selfCharacter.onUseAction -= OnUseAction;
		_selfCharacter.turnStarted -= OnTurnStarted;
		_selfCharacter.turnEnded -= OnTurnEnded;
		_selfCharacter.onDamagedBy -= OnDamagedBy;
		
		_characterAnimationEvent.onCharacterAnimationEvent -= OnCharacterAnimationEvent;
	}

	private void OnCharacterAnimationEvent(EventType eventType)
	{
		if (waitForEvent != EventType.Undefined)
		{
			if (waitForEvent == eventType)
			{
				waitForEvent = EventType.Undefined;
			}
		}
	}
	
	private void OnTurnStarted()
	{
		StartCoroutine(CheckForEnemies());
	}

	private void OnTurnEnded()
	{
		StartCoroutine(CheckForEnemies());
	}

	private void OnRoomsChanged()
	{
		StartCoroutine(CheckForEnemies());
	}

	private void OnHealed(float amount)
	{
		_selfCharacter.FloatingText("Heal!");
	}

	private void OnDamaged(float amount)
	{
		wasDamaged = true;
		_selfCharacter.FloatingText(Mathf.CeilToInt(amount).ToString());
		TrySetTrigger(parameters.getHitTrigger);
		if (rpgAnimationHandler) rpgAnimationHandler.GetHit();
	}

	private void OnDead()
	{
		//TrySetTrigger(parameters.trigger);
		//TrySetInteger(parameters.triggerNumber, (int)AnimatorTrigger.DeathTrigger);
		TrySetTrigger(parameters.deathTrigger);
		if (rpgAnimationHandler) rpgAnimationHandler.Death();
		gameObject.layer = Layers.LayerIgnore;
	}

	private void OnUseAction(CharacterAction actionType, object args = null)
	{
		if(debugMode > 0) Debug.Log(this + " OnUseAction " + actionType + " " + args);
		
		if (actionType == CharacterAction.Run)
		{
			//TrySetTrigger(parameters.trigger);
			//TrySetInteger(parameters.triggerNumber, (int)AnimatorTrigger.ReloadTrigger);
			TrySetTrigger(parameters.runTrigger);
			_selfCharacter.FloatingText("Run!");
		}
		
		else if (actionType == CharacterAction.Attack)
		{
			wasAttacking = true;
			
			if (!TrySetTrigger(parameters.attackTrigger))
			{
				if (rpgAnimationHandler)
				{
					if (!weaponSwitched)
						InstantSwitchWeapons();
					
					rpgAnimationHandler.Attack_Right(); // TODO
				}
			}
			
			_selfCharacter.FloatingText("Take this!");
		}
	}

	private void OnDamagedBy(CharacterController other, float amount)
	{
		if(shouldSwitchWeaponWhenDamaged)
			InstantSwitchWeapons();
	}

	private void UpdateWeaponRenderers()
	{
		if (!rpgAnimationHandler) return;

		rpgAnimationHandler.rightWeapon = _selfCharacter.GetCurrentWeapon().rpgWeaponR;
		rpgAnimationHandler.leftWeapon = _selfCharacter.GetCurrentWeapon().rpgWeaponL;
			
		foreach (var wpr in weaponRenderers)
		{
			if ((wpr.isLeft && rpgAnimationHandler.leftWeapon == wpr.rpgWeapon)
			    || (!wpr.isLeft && rpgAnimationHandler.rightWeapon == wpr.rpgWeapon))
			{
				wpr.gameObject.SetActive( (weaponSwitched && wpr.inHand) || (!weaponSwitched && wpr.offHand));
			}
			else
			{
				wpr.gameObject.SetActive(false);
			}
		}
	}

	private IEnumerator CheckForEnemies()
	{
		if (!weaponSwitched) yield break;
		yield return new WaitForSeconds(roomsReveleadSeconds);
		TrySwitchWeapons(_selfCharacter.CanSeeAnyEnemyAround());
	}
	
	private IEnumerator DoWaitSwitchWeapons(bool weaponsReady)
	{
		StopCoroutine("WaitToRelax");
		isSwitchingWeapons = true;
		if (debugMode > 0) Debug.Log(this + " DoWaitSwitchWeapons " + weaponsReady);
		
		if (!weaponsReady && hasSwitchOffTrigger)
		{
			if (debugMode > 0) Debug.Log(this + " DoWaitSwitchWeapons hasSwitchOffTrigger " + parameters.switchOffTrigger);
			animator.SetTrigger(parameters.switchOffTrigger);
			yield return WaitEvent(EventType.WeaponSwitch);
			isSwitchingWeapons = false;
			weaponSwitched = false;
			yield break; // returns
		}

		if (weaponsReady && hasSwitchOnTrigger)
		{
			if (debugMode > 0) Debug.Log(this + " DoWaitSwitchWeapons hasSwitchOnTrigger " + parameters.switchOnTrigger);
			animator.SetTrigger(parameters.switchOnTrigger);
			yield return WaitEvent(EventType.WeaponSwitch);
			isSwitchingWeapons = false;
			weaponSwitched = true;
			yield break; // returns
		}
		
		// RPG Character stuff

		var rightWp = _selfCharacter.GetCurrentWeapon()?.rpgWeaponR;
		//TODO var leftWp = Weapon.Relax;

		if (rightWp.HasValue)
		{
			if (weaponsReady)
			{
				if(debugMode > 0) Debug.Log("rpg UnsheathWeapon " + rightWp.Value);
				rpgAnimationHandler.UnsheathWeapon(rightWp.Value, false);
			}
			else if (rpgAnimationHandler.IsWeaponSwitchable(rightWp.Value))
			{
				if(debugMode > 0) Debug.Log("rpg SheathWeapon " + rightWp.Value);
				rpgAnimationHandler.SheathWeapon(rightWp.Value, Weapon.Relax, false);
			}
		}
		
		var startedTime = Time.time;
		do
		{
			yield return null;
			if (Time.time - startedTime > Consts.CharacterAnimTimeout)
			{
				Debug.LogWarning(this + " DoWaitSwitchWeapons timedout!");
				isSwitchingWeapons = false;
				yield break;
			}
		} while (rpgAnimationHandler.IsSwitchingWeapons());

		yield return new WaitForSeconds(switchWeaponExtraTime);
		
		weaponSwitched = weaponsReady;
		if (debugMode > 0) Debug.Log(this + " weaponSwitched=" + weaponSwitched);
		isSwitchingWeapons = false;
		UpdateWeaponRenderers();
		StartCoroutine("WaitToRelax");
	}

	private IEnumerator WaitToRelax()
	{
		var startTime = Time.time;
		while (Time.time - startTime < waitToRelaxTime)
		{
			if (wasDamaged || wasAttacking || wasMoving)
			{
				if(debugMode > 0) Debug.Log(this + " WaitToRelax reset time wasDamaged=" + wasDamaged + " wasAttacking="+ wasAttacking + " wasMoving=" + wasMoving);
				startTime = Time.time;
			}
			
			if(isSwitchingWeapons || GameTurnManager.Instance.IsTurnBasedMode())
				yield break;
			
			yield return null;
		}
		
		TrySwitchWeapons(false);
	}

	private void OnCharacterMoved()
	{
		wasMoving = true;
	}

	private void LateUpdate()
	{
		var localVelocity = _selfCharacter.GetVelocity();
		var isMoving = _selfCharacter.IsMoving();
		if (minMovePointsToRun >= 0f && _selfCharacter.HasUsedAction(CharacterAction.Run) && _selfCharacter.HasMovePoints(minMovePointsToRun) && _selfCharacter.IsCurrentTurn())
		{
			isMoving = true;
			localVelocity = Vector3.forward * _selfCharacter.GetAgent().GetMaxSpeed();
		}
		TrySetFloat(parameters.velocityX, 0);      
		TrySetFloat(parameters.velocityZ, localVelocity.magnitude * velocityScale);  
		TrySetBool(parameters.moving, isMoving);

		wasDamaged = wasAttacking = wasMoving = false;
	}

	// TODO keep for later?
	bool TrySetFloat(string param, float value)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.enabled = true;
		animator.SetFloat(param, value);
		return true;
	}

	bool TrySetBool(string param, bool value)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.enabled = true;
		animator.SetBool(param, value);
		return true;
	}

	bool TrySetInteger(string param, int value)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.enabled = true;
		animator.SetInteger(param, value);
		return true;
	}

	bool TrySetTrigger(string param)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.enabled = true;
		animator.SetTrigger(param);
		return true;
	}

	bool TryResetTrigger(string param)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.ResetTrigger(param);
		return true;
	}

	//void SetFloat(int param, float value) => AnimatorUtils.TrySetFloat(param, value, animators);
	//void SetBool(int param, bool value) => AnimatorUtils.TrySetBool(param, value, animators);
	//void SetInteger(int param, int value) => AnimatorUtils.TrySetInteger(param, value, animators);
	//void SetTrigger(int param) => AnimatorUtils.TrySetTrigger(param, animators);
	#endregion
}

