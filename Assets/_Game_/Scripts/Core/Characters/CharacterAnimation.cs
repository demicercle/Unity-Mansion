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
		public string deathTrigger;
		public string getHitTrigger;
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
	
	[Header("Animation Settings")]
	[SerializeField] private float velocityScale = 1.0f;
	[SerializeField] private float minMovePointsToRun = 0.1f;

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
	
	public Coroutine TrySwitchWeapons(bool value) => StartCoroutine(DoWaitSwitchWeapons(value));
	private IEnumerator DoWaitSwitchWeapons(bool value)
	{
		if (!weaponsAnimation)
		{
			//Debug.Log(this + " DoWaitSwitchWeapons=" + value + " break ; no weaponsAnimation");
			yield break;
		}

		if (weaponsAnimation.Switched() == value)
		{
			//if(value) Debug.Log(this + " DoWaitSwitchWeapons=" + value + " already switched");
			//else Debug.Log(this + " DoWaitSwitchWeapons=" + value + " already sheated");
			yield break;
		}
		
		//Debug.Log(this + " DoWaitSwitchWeapons=" + value);
		
		if (value)
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

		weaponsAnimation.ToggleSwitched();
		Debug.Log(this + " Switched Weapons " + weaponsAnimation.Switched());
	}
	
	#region PRIVATE
	private CharacterAnimationEvent _characterAnimationEvent;
	private EventType waitForEvent;
	
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

	void OnCharacterAnimationEvent(EventType eventType)
	{
		if (waitForEvent != EventType.Undefined)
		{
			if (waitForEvent == eventType)
			{
				waitForEvent = EventType.Undefined;
			}
		}
	}
	
	void OnTurnStarted()
	{
		//CanSeeAnyEnemyAround
		UpdateWeapons();
	}

	void OnTurnEnded()
	{
		UpdateWeapons();
	}

	void OnRoomsChanged()
	{
		Invoke("UpdateWeapons", Consts.DefaultActionsSeconds);
	}

	void UpdateWeapons()
	{
		CancelInvoke("UpdateWeapons");
		//var seeEnemies = controller.CanSeeAnyEnemyAround();
		//Debug.Log(this + " TryRelax seeEnemies=" + seeEnemies);
		TrySwitchWeapons(controller.CanSeeAnyEnemyAround());
	}

	void OnHealed(float amount)
	{
		GetController().FloatingText("Heal!");
	}

	void OnDamaged(float amount)
	{
		GetController().FloatingText(Mathf.CeilToInt(amount).ToString());
		TrySetTriggerAnimators(parameters.getHitTrigger);
	}

	void OnKilled()
	{
		TrySetTriggerAnimators(parameters.trigger);
		TrySetIntegerAnimators(parameters.triggerNumber, (int)AnimatorTrigger.DeathTrigger);
		TrySetTriggerAnimators(parameters.deathTrigger);
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_IGNORE);
	}

	void OnUseAction(CharacterAction actionType, object args = null)
	{
		if (actionType == CharacterAction.RunAction)
		{
			TrySetTriggerAnimators(parameters.trigger);
			TrySetIntegerAnimators(parameters.triggerNumber, (int)AnimatorTrigger.ReloadTrigger);
			TrySetTriggerAnimators(parameters.runTrigger);
			GetController().FloatingText("Run!");
		}
		
		else if (actionType == CharacterAction.AttackAction)
		{
			TrySetTriggerAnimators(parameters.attackTrigger);
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
	
	private void Start()
	{
		TrySetFloatAnimators(parameters.velocityX, 0.0f);      
		TrySetFloatAnimators(parameters.velocityZ, 0.0f);        
		TrySetIntegerAnimators(parameters.weapon, -1);
		TrySetIntegerAnimators(parameters.triggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
		TrySetTriggerAnimators(parameters.trigger);
		TrySetBoolAnimators(parameters.moving, false);
	}
	
	void LateUpdate()
	{
		var localVelocity = controller.GetVelocity();
		var isMoving = controller.IsMoving();
		if (minMovePointsToRun >= 0f && controller.HasUsedAction(CharacterAction.RunAction) && controller.HasMovePoints(minMovePointsToRun))
		{
			isMoving = true;
			localVelocity = Vector3.forward * controller.GetAgent().GetMaxSpeed();
		}
		TrySetFloatAnimators(parameters.velocityX, 0);      
		TrySetFloatAnimators(parameters.velocityZ, localVelocity.magnitude * velocityScale);  
		TrySetBoolAnimators(parameters.moving, isMoving);
	}

	// TODO keep for later?
	bool TrySetFloatAnimators(string param, float value)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.enabled = true;
		animator.SetFloat(param, value);
		return true;
	}

	bool TrySetBoolAnimators(string param, bool value)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.enabled = true;
		animator.SetBool(param, value);
		return true;
	}

	bool TrySetIntegerAnimators(string param, int value)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.enabled = true;
		animator.SetInteger(param, value);
		return true;
	}

	bool TrySetTriggerAnimators(string param)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.enabled = true;
		animator.SetTrigger(param);
		return true;
	}

	bool TryResetTriggerAnimators(string param)
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

