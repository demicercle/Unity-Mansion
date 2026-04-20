using System.Collections;
using System.Collections.Generic;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.Events;
using RPGCharacterAnims.Extensions;

public class CharacterAnimation : MonoBehaviour, IView<CharacterController>
{
	
	public enum EventType
	{
		Undefined,
		Hit,
		Shoot,
		WeaponSwitch,
	}
	
	//public const string EventHit = "Hit";
	
	static public List<CharacterAnimation> activeInstances  = new List<CharacterAnimation>();
	
	private CharacterController controller;
	public CharacterController GetController()
	{
		if(!controller) controller = GetComponent<CharacterController>();
		return controller;
	}
	
	[SerializeField] private Animator animator;
	[SerializeField] private WeaponsAnimation weaponsAnimation;
	
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
		[Header("RPG Anims")]
		public string weapon;
		public string triggerNumber;
		public string trigger;
	}
	
	[SerializeField] private Parameters parameters;
	[SerializeField] private List<EventType> eventTypes = new List<EventType>();

#if UNITY_EDITOR
	[ContextMenu("SetParameters/RPGCharacter")]
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
	}
	
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

	public bool IsSwitchingWeapons => isSwitchingWeapons;

	public bool HasAnimationEvent(EventType eventType) => eventTypes.Contains(eventType);

	public Coroutine WaitEvent(EventType eventType) => StartCoroutine(DoWaitEvent(eventType));
	
	private IEnumerator DoWaitEvent(EventType eventType)
	{
		waitForEvent = eventType;
		do
		{
			yield return null;
		} while (waitForEvent != EventType.Undefined);
	}

	public bool TrySwitchWeapons(bool weaponsReady)
	{
		if (WeaponsSwitched() == weaponsReady)
		{
			return false; // weapons already switched
		}
		
		StopAllCoroutines();
		isSwitchingWeapons = false;
		
		if (weaponsAnimation == null) // RPG Character Animation override
		{
			if (weaponsReady && !hasSwitchOnTrigger) return false; // no animator parameter for true
			if (!weaponsReady && !hasSwitchOffTrigger) return false; // no animator parameter for false
		}
		
		isSwitchingWeapons = true;
		StartCoroutine(DoWaitSwitchWeapons(weaponsReady)); // do not returns coroutine, use a bool "IsSwitchingWeapons"
		return true;
	}
	
	private IEnumerator DoWaitSwitchWeapons(bool weaponsReady)
	{
		if (!weaponsReady && hasSwitchOffTrigger)
		{
			Debug.Log(this + " DoWaitSwitchWeapons hasSwitchOffTrigger " + parameters.switchOffTrigger);
			animator.SetTrigger(parameters.switchOffTrigger);
			yield return WaitEvent(EventType.WeaponSwitch);
			isSwitchingWeapons = false;
			if (hasSwitchOnTrigger) weaponSwitched = true;
			yield break; // returns
		}

		if (weaponsReady && hasSwitchOnTrigger)
		{
			Debug.Log(this + " DoWaitSwitchWeapons hasSwitchOnTrigger " + parameters.switchOnTrigger);
			animator.SetTrigger(parameters.switchOnTrigger);
			yield return WaitEvent(EventType.WeaponSwitch);
			isSwitchingWeapons = false;
			if (hasSwitchOffTrigger) weaponSwitched = true;
			yield break; // returns
		}
		
		// RPG Character stuff
		
		if (weaponsReady)
			weaponsAnimation.UnsheathWeapon(weaponsAnimation.rightWeapon, false);
		else
			weaponsAnimation.SheathWeapon(weaponsAnimation.rightWeapon, Weapon.Relax, false);
		
		var startedTime = Time.time;
		do
		{
			yield return null;
			if (Time.time - startedTime > Consts.CharacterAnimTimeout)
			{
				Debug.LogWarning(this + " DoWaitSwitchWeapons timedout!");
				yield break;
			}
		} while (weaponsAnimation.IsSwitchingWeapons());
		
		weaponSwitched = weaponsReady;
		Debug.Log(this + " weaponSwitched=" + weaponSwitched);
		isSwitchingWeapons = false;
	}

	public bool WeaponsSwitched() => weaponSwitched;
	
	#region PRIVATE
	private CharacterAnimationEvent _characterAnimationEvent;
	private EventType waitForEvent;
	private bool weaponSwitched, hasSwitchOnTrigger, hasSwitchOffTrigger, isSwitchingWeapons;
	
	private void Awake()
	{
		enabled = animator != null;
		if (!enabled)
		{
			Debug.LogWarning(this + " disabled because no animator assigned");
			return;
		}
		
		_characterAnimationEvent = animator.GetComponent<CharacterAnimationEvent>();
		if(!_characterAnimationEvent) _characterAnimationEvent = animator.gameObject.AddComponent<CharacterAnimationEvent>();
		_characterAnimationEvent.onCharacterAnimationEvent += OnCharacterAnimationEvent;

		controller = GetController();
		controller.onHealed += OnHealed;
		//controller.onPartyIndexChanged += (newIndex) =>
		controller.onDamaged += OnDamaged;
		controller.onKilled += OnKilled;
		//controller.onMoved += () =>
		//controller.onRotate += () =>
		controller.onUseAction += OnUseAction;
		controller.turnStarted += OnTurnStarted;
		controller.turnEnded += OnTurnEnded;

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
		TrySetInteger(parameters.weapon, -1);
		TrySetInteger(parameters.triggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
		TrySetTrigger(parameters.trigger);
		TrySetBool(parameters.moving, false);

		TrySwitchWeapons(weaponSwitched);
		if(waitForEvent != EventType.Undefined) Debug.LogError(this + " has waitForEvent on start " + waitForEvent);
	}

	private void OnDisable()
	{
		activeInstances.Remove(this);
	}

	private void OnDestroy()
	{
		RoomTrigger.onChanged -= OnRoomsChanged;
		
		controller.onHealed -= OnHealed;
		//controller.onPartyIndexChanged -= (newIndex) =>
		controller.onDamaged -= OnDamaged;
		controller.onKilled -= OnKilled;
		//controller.onMoved -= () =>
		//controller.onRotate -= () =>
		controller.onUseAction -= OnUseAction;
		controller.turnStarted -= OnTurnStarted;
		controller.turnEnded -= OnTurnEnded;
		
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
		UpdateWeapons();
	}

	private void OnTurnEnded()
	{
		//UpdateWeapons();
	}

	private void OnRoomsChanged()
	{
		Invoke("UpdateWeapons", roomsReveleadSeconds);
	}

	private void UpdateWeapons()
	{
		CancelInvoke("UpdateWeapons");
		//var seeEnemies = controller.CanSeeAnyEnemyAround();
		//Debug.Log(this + " TryRelax seeEnemies=" + seeEnemies);
		TrySwitchWeapons(controller.CanSeeAnyEnemyAround());
	}

	private void OnHealed(float amount)
	{
		GetController().FloatingText("Heal!");
	}

	private void OnDamaged(float amount)
	{
		GetController().FloatingText(Mathf.CeilToInt(amount).ToString());
		TrySetTrigger(parameters.getHitTrigger);
	}

	private void OnKilled()
	{
		TrySetTrigger(parameters.trigger);
		TrySetInteger(parameters.triggerNumber, (int)AnimatorTrigger.DeathTrigger);
		TrySetTrigger(parameters.deathTrigger);
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_IGNORE);
	}

	private void OnUseAction(CharacterAction actionType, object args = null)
	{
		if (actionType == CharacterAction.RunAction)
		{
			TrySetTrigger(parameters.trigger);
			TrySetInteger(parameters.triggerNumber, (int)AnimatorTrigger.ReloadTrigger);
			TrySetTrigger(parameters.runTrigger);
			GetController().FloatingText("Run!");
		}
		
		else if (actionType == CharacterAction.AttackAction)
		{
			TrySetTrigger(parameters.attackTrigger);
			//TrySetTriggerAnimators(parameters.trigger);
			//TrySetIntegerAnimators(parameters.triggerNumber, (int)Animator)

			if (weaponsAnimation && !string.IsNullOrEmpty(parameters.trigger) && !string.IsNullOrWhiteSpace(parameters.triggerNumber))
			{
				animator.SetActionTrigger(weaponsAnimation.hasDualWeapons ? AnimatorTrigger.AttackDualTrigger : AnimatorTrigger.AttackTrigger,
					1); // TODO attack number
			}
			
			GetController().FloatingText("Take this!");
		}
	}
	
	private void LateUpdate()
	{
		var localVelocity = controller.GetVelocity();
		var isMoving = controller.IsMoving();
		if (minMovePointsToRun >= 0f && controller.HasUsedAction(CharacterAction.RunAction) && controller.HasMovePoints(minMovePointsToRun))
		{
			isMoving = true;
			localVelocity = Vector3.forward * controller.GetAgent().GetMaxSpeed();
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

