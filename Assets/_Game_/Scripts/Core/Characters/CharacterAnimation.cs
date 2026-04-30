using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		Scream
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
	[SerializeField] private bool startsWeaponsReady = true;
	[SerializeField] private bool shouldSwitchWeaponWhenDamaged = true;

	public bool IsSwitchingWeapons => isSwitchingWeapons;

	public bool HasAnimationEvent(EventType eventType) => eventTypes.Contains(eventType);
	public bool IsWaitingEvents => waitForEvents.Count > 0;

	public Coroutine WaitEvent(EventType eventType, float timeout = 6.0f) => StartCoroutine(DoWaitEvents(timeout, eventType));
	public Coroutine WaitEvents(params EventType[] eventTypes) => StartCoroutine(DoWaitEvents(6.0f, eventTypes));
	
	private IEnumerator DoWaitEvents(float timeout, params EventType[] eventTypes)
	{
		var startTime = Time.time;
		if (debugMode > 0) Debug.Log(this + " DoWaitEvent " + Utils.ArrayStr(eventTypes));
		waitForEvents.AddRange(eventTypes);
		while(waitForEvents.Count > 0)
		{
			if(_selfCharacter.IsDead())
				yield break;
			
			if (timeout > 0f && Time.time - startTime > timeout)
			{
				Debug.LogWarning(this + " timeout! ");
				yield break;
			}
			
			yield return null;
		} 
	}

	public void InstantSwitchWeapons(bool force = false)
	{
		if (WeaponsSwitched() && !force)
		{
			return;
		}
		
		if(debugMode >0 )Debug.Log(this + " InstantSwitchWeapons");
		if (rpgAnimationHandler) rpgAnimationHandler.InstantWeaponSwitch();
		TrySetTrigger(parameters.instantSwitchTrigger);
		weaponSwitched = true;
		UpdateWeaponRenderers();
	}

	private bool wantsWeaponsReady;
	public bool TrySwitchWeapons(bool weaponsReady) => TrySwitchWeapons(weaponsReady, _selfCharacter.GetCurrentWeapon().rpgWeaponR, false);
	public bool TrySwitchWeapons(bool weaponsReady, Weapon rightWp, bool force)
	{
		if (_selfCharacter.IsDead())
			return false;
		
		if (weaponSwitched == weaponsReady && !force)
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
		
		StartCoroutine(DoWaitSwitchWeapons(weaponsReady, rightWp)); // do not returns coroutine, use a bool "IsSwitchingWeapons"
		return true;
	}

	public bool WeaponsSwitched() => weaponSwitched;

	public void WeaponChanged()
	{
		UpdateWeaponRenderers(); // weapon changed
	}

	/*public void SetWeapon(Weapon rpgWeapon, bool isLeft)
	{
		
	}*/
	
	#region PRIVATE
	private CharacterAnimationEvent _characterAnimationEvent;
	private CharacterEntity _selfCharacter;
	private List<EventType> waitForEvents = new List<EventType>();
	private bool weaponSwitched;
	private bool hasSwitchOnTrigger, hasSwitchOffTrigger, isSwitchingWeapons;
	private WeaponRenderer[] weaponRenderers;
	
	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterEntity>();
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
		
		weaponSwitched = startsWeaponsReady;
		
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
			if(!weaponSwitched)
				rpgAnimationHandler.RelaxOnStart();
		}
		
		if(!TrySwitchWeapons(weaponSwitched))
			UpdateWeaponRenderers(); // instant update
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
		if (waitForEvents.Contains(eventType))
		{
			if(debugMode > 0) Debug.Log(this + " OnCharacterAnimationEvent " + eventType + ", clear waitForEvents");
			waitForEvents.Clear();
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

	//public UnityEvent onHealed;
	private void OnHealed(float amount)
	{
		_selfCharacter.FloatingText("Heal!");
		//onHealed?.Invoke();
	}

	//public UnityEvent onDamaged;
	private void OnDamaged(float amount)
	{
		_selfCharacter.FloatingText(Mathf.CeilToInt(amount).ToString());
		TrySetTrigger(parameters.getHitTrigger);
		//onDamaged?.Invoke();
		if (rpgAnimationHandler) rpgAnimationHandler.GetHit();
	}

	//public UnityEvent onDead;
	private void OnDead()
	{
		//TrySetTrigger(parameters.trigger);
		//TrySetInteger(parameters.triggerNumber, (int)AnimatorTrigger.DeathTrigger);
		TrySetTrigger(parameters.deathTrigger);
		if (rpgAnimationHandler) rpgAnimationHandler.Death();
		//onDead?.Invoke();
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

	private void OnDamagedBy(CharacterEntity other, float amount)
	{
		if(shouldSwitchWeaponWhenDamaged)
			InstantSwitchWeapons();
	}

	//private bool itemSwitched;
	private void UpdateWeaponRenderers()
	{
		if (!rpgAnimationHandler) return; // only for RPG weapon shit

		if (_selfCharacter.HasItemToUse(out var item) && weaponRenderers.Any(wp => wp.DisplaysItem(item)))
		{
			if (rpgAnimationHandler.rightWeapon != Weapon.RightItem)
			{
				if (weaponSwitched)
				{
					rpgAnimationHandler.InstantWeaponSwitch(Weapon.RightItem);
				}
				else
				{
					TrySwitchWeapons(true, Weapon.RightItem, true);
				}
			}
			
			//itemSwitched = true;
			rpgAnimationHandler.rightWeapon = Weapon.RightItem;
			rpgAnimationHandler.leftWeapon = default;
		}
		else
		{
			var rwp = _selfCharacter.GetCurrentWeapon().rpgWeaponR;
			var lwp = _selfCharacter.GetCurrentWeapon().rpgWeaponL;
			
			if (!GameTurnManager.Instance.IsTurnBasedMode() && !_selfCharacter.Routines.AnyActive())
				TrySwitchWeapons(false); // un-use item real time
			else
			{
				StartCoroutine(WaitChangeWeapon(rwp));// un-use item turn based
			}

			rpgAnimationHandler.rightWeapon = rwp;
			rpgAnimationHandler.leftWeapon = lwp;
			
			//itemSwitched = false;
		}
			
		foreach (var wpr in weaponRenderers)
		{
			if (item && wpr.rpgWeapon == Weapon.RightItem)
			{
				wpr.gameObject.SetActive(true);
			}
			
			else if ((wpr.isLeft && rpgAnimationHandler.leftWeapon == wpr.rpgWeapon)
			    || (!wpr.isLeft && rpgAnimationHandler.rightWeapon == wpr.rpgWeapon))
			{
				wpr.gameObject.SetActive( (wantsWeaponsReady && wpr.inHand) || (!wantsWeaponsReady  && wpr.offHand));
			}
			
			else
			{
				wpr.gameObject.SetActive(false);
			}
		}
	}

	private bool isChangingWeapon;
	private IEnumerator WaitChangeWeapon(Weapon rightWp)
	{
		if(!rpgAnimationHandler || rpgAnimationHandler.rightWeapon == rightWp)
			yield break;
		
		if (isChangingWeapon)
		{
			isChangingWeapon = false;
			yield return null;
		}

		isChangingWeapon = true;
		if (!GameTurnManager.Instance.IsTurnBasedMode())
		{
			if (debugMode > 0) Debug.Log(this + " WaitChangeWeapon rightWp=" + rightWp + " from=" + rpgAnimationHandler.rightWeapon);
			rpgAnimationHandler.SheathWeapon(Weapon.Relax, rightWp, false);
			do
			{
				yield return null;

				if (!isChangingWeapon)
				{
					if (debugMode > 0) Debug.Log(this + " WaitChangeWeapon aborted");
					yield break;
				}
			}
			while (rpgAnimationHandler.IsSwitchingWeapons()); 
			if(debugMode > 0) Debug.Log(this + " WaitChangeWeapon ended");
		}
		else
		{
			if (debugMode > 0) Debug.Log(this + " WaitChangeWeapon InstantWeaponSwitch rightWp=" + rightWp + " from=" + rpgAnimationHandler.rightWeapon);
			rpgAnimationHandler.InstantWeaponSwitch(rightWp);
			yield return null;
		}
		isChangingWeapon = false;
	}

	private IEnumerator CheckForEnemies()
	{
		if (!weaponSwitched) yield break;
		yield return new WaitForSeconds(roomsReveleadSeconds);
		TrySwitchWeapons(_selfCharacter.CanSeeAnyEnemyAround());
	}
	
	private IEnumerator DoWaitSwitchWeapons(bool weaponsReady, Weapon rightWp)
	{
		StopCoroutine("WaitToRelax");
		isSwitchingWeapons = true;
		wantsWeaponsReady = weaponsReady;
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

		//TODO var leftWp = Weapon.Relax;

		if (weaponsReady)
		{
			if(debugMode > 0) Debug.Log("rpg UnsheathWeapon " + rightWp);
			rpgAnimationHandler.UnsheathWeapon(rightWp, false);
		}
		else if (rpgAnimationHandler.IsWeaponSwitchable(rightWp))
		{
			if(debugMode > 0) Debug.Log("rpg SheathWeapon " + rightWp);
			rpgAnimationHandler.SheathWeapon(rightWp, Weapon.Relax, false);
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
		var time = 0f;
		while (weaponSwitched && time < waitToRelaxTime)
		{
			if(GameTurnManager.Instance.IsTurnBasedMode())
				yield break;
			
			if (_selfCharacter.Routines.AnyActive() || _selfCharacter.HasItemToUse(out var _))
			{
				time = 0f;
			}
			else
			{
				time += Time.deltaTime;
				if(debugMode > 0) Debug.Log(this + " WaitToRelax " + time + " / " + waitToRelaxTime);
			}
			
			yield return null;
		}
		
		TrySwitchWeapons(false);
	}

	private void OnCharacterMoved()
	{
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

