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
		public string reviveTrigger;
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
	[SerializeField] private float reloadAnimDuration = 2.0f;

	public float GetReloadAnimDuration() => reloadAnimDuration;
	
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

	public void InstantSwitchWeapons(bool value) =>
		InstantSwitchWeapons(value ? _selfCharacter.GetCurrentWeapon().rpgWeaponR : Weapon.Relax);

	public void InstantSwitchWeapons(Weapon newWeapon)
	{
		if (weaponInHand != newWeapon)
		{
			if(debugMode >0 )Debug.Log(this + " InstantSwitchWeapons " + newWeapon);
			if (rpgAnimationHandler)
			{
				rpgAnimationHandler.rightWeapon = newWeapon;
				rpgAnimationHandler.InstantWeaponSwitch();
			}
			TrySetTrigger(parameters.instantSwitchTrigger);
			weaponInHand = newWeapon;
			UpdateWeaponRenderers();
		}
	}

	public Coroutine SwitchWeapons(bool weaponsReady)
	{
		return SwitchWeapon(weaponsReady ? _selfCharacter.GetCurrentWeapon().rpgWeaponR : Weapon.Relax);
	}

	public Coroutine SwitchWeapon(Weapon newWeapon)
	{
		if (weaponInHand != newWeapon)
		{
			weaponInHand = newWeapon;
			StopCoroutine("DoWeaponSwitch");
			return StartCoroutine("DoWeaponSwitch");
		}
		else
		{
			return null;
		}
	}

	public bool WeaponsSwitched() => weaponInHand != Weapon.Relax;
	
	#region PRIVATE
	private CharacterAnimationEvent _characterAnimationEvent;
	private CharacterEntity _selfCharacter;
	private List<EventType> waitForEvents = new List<EventType>();
	private Weapon weaponInHand = Weapon.Relax;
	private bool hasSwitchOnTrigger, hasSwitchOffTrigger, isSwitchingWeapons;
	private float lastTimeWeaponSwitched;
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
		
		_characterAnimationEvent = animator.GetComponent<CharacterAnimationEvent>();
		if(!_characterAnimationEvent) _characterAnimationEvent = animator.gameObject.AddComponent<CharacterAnimationEvent>();
		
		if(!rpgAnimationHandler) rpgAnimationHandler = GetComponent<RPGAnimationHandler>();
		if(!rpgAnimationHandler) rpgAnimationHandler = GetComponentInChildren<RPGAnimationHandler>();

		weaponRenderers = GetComponentsInChildren<WeaponRenderer>();
	}
	
	private void OnEnable()
	{
		activeInstances.Add(this);
		
		_characterAnimationEvent.onCharacterAnimationEvent += OnCharacterAnimationEvent;
		RoomTrigger.onChanged += OnRoomsChanged;

		_selfCharacter.onMoved += OnCharacterMoved;
		_selfCharacter.onHealed += OnHealed;
		_selfCharacter.onDamaged += OnDamaged;
		_selfCharacter.onDead += OnDead;
		_selfCharacter.onRevive += OnRevive;
		_selfCharacter.onUseAction += OnUseAction;
		_selfCharacter.turnStarted += OnTurnStarted;
		_selfCharacter.turnEnded += OnTurnEnded;
		_selfCharacter.onDamagedBy += OnDamagedBy;
		_selfCharacter.onAiming += OnAiming;
	}

	private void OnDisable()
	{
		activeInstances.Remove(this);
		
		RoomTrigger.onChanged -= OnRoomsChanged;
		_characterAnimationEvent.onCharacterAnimationEvent -= OnCharacterAnimationEvent;
		
		_selfCharacter.onMoved -= OnCharacterMoved;
		_selfCharacter.onHealed -= OnHealed;
		_selfCharacter.onDamaged -= OnDamaged;
		_selfCharacter.onDead -= OnDead;
		_selfCharacter.onRevive -= OnRevive;
		_selfCharacter.onUseAction -= OnUseAction;
		_selfCharacter.turnStarted -= OnTurnStarted;
		_selfCharacter.turnEnded -= OnTurnEnded;
		_selfCharacter.onDamagedBy -= OnDamagedBy;
		_selfCharacter.onAiming -= OnAiming;
		
		StopAllCoroutines();
	}

	private void Start()
	{
		hasSwitchOnTrigger = !string.IsNullOrWhiteSpace(parameters.switchOnTrigger);
		hasSwitchOffTrigger = !string.IsNullOrWhiteSpace(parameters.switchOffTrigger);
		
		weaponInHand = Weapon.Relax;
		
		TrySetFloat(parameters.velocityX, 0.0f);      
		TrySetFloat(parameters.velocityZ, 0.0f);        
		TrySetBool(parameters.moving, false);

		if (rpgAnimationHandler)
		{
			rpgAnimationHandler.RelaxOnStart();
		}
		
		UpdateWeaponRenderers();
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
	}

	private void OnTurnEnded()
	{
	}

	private void OnRoomsChanged()
	{
	}

	//public UnityEvent onHealed;
	private void OnHealed(float amount)
	{
		_selfCharacter.FloatingText(LanguageSettings.Main.Heal);
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
	}

	private void OnRevive()
	{
		TrySetTrigger(parameters.reviveTrigger);
		if (rpgAnimationHandler)
		{
			InstantSwitchWeapons(true);
			rpgAnimationHandler.Revive();
		}
	}

	private void OnUseAction(CharacterAction actionType, object args = null)
	{
		if(debugMode > 0) Debug.Log(this + " OnUseAction " + actionType + " " + args);
		
		if (actionType == CharacterAction.Run)
		{
			//TrySetTrigger(parameters.trigger);
			//TrySetInteger(parameters.triggerNumber, (int)AnimatorTrigger.ReloadTrigger);
			TrySetTrigger(parameters.runTrigger);
			_selfCharacter.FloatingText(LanguageSettings.Main.Run);
		}
		
		else if (actionType == CharacterAction.Attack)
		{
			if (!TrySetTrigger(parameters.attackTrigger))
			{
				if (rpgAnimationHandler)
				{
					InstantSwitchWeapons(true);
					rpgAnimationHandler.Attack_Right(); // TODO
				}
			}

			if (args is CharacterEntity)
			{
				_selfCharacter.FloatingText(LanguageSettings.Main.TakeThis);
			}
		}
	}

	private void OnDamagedBy(CharacterEntity other, float amount)
	{
		if(shouldSwitchWeaponWhenDamaged)
			InstantSwitchWeapons(true);
	}

	private float timeAiming;

	private void OnAiming(bool value, Vector3 position)
	{
		if (!value) 
			timeAiming = 0f;
		
		if(CharacterEntity.debugAim > 0) Debug.Log(this + " OnAiming " + value + " " + position + " " + timeAiming.ToString("N2"));
		
		if (rpgAnimationHandler)
		{
			rpgAnimationHandler.Aim(value, position, rpgAnimationHandler.rightWeapon == Weapon.TwoHandBow ? Mathf.Clamp01(timeAiming) : 0f);
		}
		
		if (value) 
			timeAiming += Time.deltaTime;
	}

	//private bool itemSwitched;
	private void UpdateWeaponRenderers()
	{
		_selfCharacter.HasItemToUse(out var itemToUse);
		
		if(debugMode > 0) Debug.Log(this + " UpdateWeaponRenderers weaponInHand=" + weaponInHand + " itemToUse=" + itemToUse);
		
		foreach (var wpr in weaponRenderers)
		{
			if (itemToUse)
			{
				wpr.gameObject.SetActive(wpr.inHand && wpr.rpgWeapon == itemToUse.GetWeaponInHand() && itemToUse.CompareTags(wpr.itemTags));
			}
			else if (!wpr.isLeft)
			{
				wpr.gameObject.SetActive( (wpr.inHand && wpr.rpgWeapon == weaponInHand) 
					|| (wpr.offHand && wpr.rpgWeapon != weaponInHand && wpr.rpgWeapon == _selfCharacter.GetCurrentWeapon().rpgWeaponR));
			}
			else
			{
				wpr.gameObject.SetActive(false);
			}
		}
	}

	private IEnumerator DoWeaponSwitch()
	{
		isSwitchingWeapons = true;
		if (debugMode > 0) Debug.Log(this + " DoWaitSwitchWeapons > " + weaponInHand);
		
		if (weaponInHand == Weapon.Relax && hasSwitchOffTrigger)
		{
			if (debugMode > 0) Debug.Log(this + " DoWaitSwitchWeapons hasSwitchOffTrigger " + parameters.switchOffTrigger);
			animator.SetTrigger(parameters.switchOffTrigger);
			yield return WaitEvent(EventType.WeaponSwitch);
		}

		else if (weaponInHand != Weapon.Relax && hasSwitchOnTrigger)
		{
			if (debugMode > 0) Debug.Log(this + " DoWaitSwitchWeapons hasSwitchOnTrigger " + parameters.switchOnTrigger);
			animator.SetTrigger(parameters.switchOnTrigger);
			yield return WaitEvent(EventType.WeaponSwitch);
		}

		if (rpgAnimationHandler)
		{
			// RPG Character stuff
			
			while(rpgAnimationHandler.IsSwitchingWeapons())
				yield return null;
			
			//var isDual = weaponInHand.ToString().StartsWith("Left") || weaponInHand.ToString().StartsWith("Right");
			
			if (weaponInHand != Weapon.Relax)
			{
				if(debugMode > 0) Debug.Log(this + " rpg UnsheathWeapon " + weaponInHand);
				if(rpgAnimationHandler.instantSwitchWeapons.Contains(weaponInHand))
					rpgAnimationHandler.InstantWeaponSwitch(weaponInHand);
				else
				{
					rpgAnimationHandler.UnsheathWeapon(weaponInHand, false);
					rpgAnimationHandler.SetSheathLocation(weaponInHand.ToString().Contains("TwoHand"));
				}
			}
			else
			{
				var sheathedWeapon = _selfCharacter.GetCurrentWeapon().rpgWeaponR;
				if(debugMode > 0) Debug.Log(this +  "rpg SheathWeapon " + weaponInHand);
				if(rpgAnimationHandler.instantSwitchWeapons.Contains(weaponInHand))
					rpgAnimationHandler.InstantWeaponSwitch(Weapon.Relax);
				else
				{
					rpgAnimationHandler.SheathWeapon(weaponInHand, Weapon.Relax, false);
					rpgAnimationHandler.SetSheathLocation(sheathedWeapon.ToString().Contains("TwoHand"));
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
			
			rpgAnimationHandler.rightWeapon = weaponInHand;
			rpgAnimationHandler.SetSheathLocation(true);
		}

		UpdateWeaponRenderers();

		yield return new WaitForSeconds(switchWeaponExtraTime);
		
		isSwitchingWeapons = false;
		lastTimeWeaponSwitched = Time.time;

		while (waitToRelaxTime > 0 && !_selfCharacter.InBattle() && !_selfCharacter.IsDead())
		{
			yield return null;
			
			if (_selfCharacter.IsActing() || _selfCharacter.IsAiming() || _selfCharacter.HasItemToUse(out var _))
				lastTimeWeaponSwitched = Time.time;
			
			if (Time.time - lastTimeWeaponSwitched > waitToRelaxTime)
			{
				yield return SwitchWeapons(false);
			}
		}
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
			isMoving = true; // force running
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

