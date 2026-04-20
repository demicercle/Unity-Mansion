using System.Collections;
using System.Collections.Generic;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.Events;

public class CharacterAnimation : MonoBehaviour, IView<CharacterController>
{
	public enum EventType
	{
		Undefined,
		Hit,
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
		if (!animator)
		{
			Debug.LogWarning(this + " DoWaitEvent error: no animator!");
			yield break;
		}
	
		var animEventHandler = Utils.GetOrAddComponent<AnimationEventHandler>(animator.gameObject);
		var action = default(UnityEvent);
		
		if (eventType == EventType.Hit) 
			action = animEventHandler.onHit;
		else
		{
			Debug.LogError(this + " DoWaitEvent unsupported type: "  + eventType);
			yield break;
		}
		
		if (action != null)
		{
			var eventTriggered = false;
			var handler = new System.Action(() =>
			{
				eventTriggered = true;
			});
			
			Debug.Log(this + " DoWaitEvent: "  + eventType + " AddListener");
			action.AddListener(handler.Invoke);

			var startTime = Time.time;
			while (!eventTriggered)
			{
				yield return null;
				if (Time.time - startTime > Consts.CharacterWaitEventTimeout)
				{
					Debug.LogWarning(this + " Timed out! " + eventType);
					break;
				}
			}

			if (eventTriggered)
			{
				Debug.Log(this + " event triggered: " + eventType);
				action.RemoveListener(handler.Invoke);
			}
		}
	}
	
	#region PRIVATE
	private void OnEnable()
	{
		activeInstances.Add(this);
	}

	private void OnDisable()
	{
		activeInstances.Remove(this);
	}
	
	private void Awake()
	{
		controller = GetController();

		controller.onHealed += OnHealed;
		//controller.onPartyIndexChanged += (newIndex) =>
		controller.onDamaged += OnDamaged;
		controller.onKilled += OnKilled;
		//controller.onMoved += () =>
		//controller.onRotate += () =>
		controller.onUseAction += OnUseAction;
	}

	private void OnDestroy()
	{
		controller.onHealed -= OnHealed;
		//controller.onPartyIndexChanged -= (newIndex) =>
		controller.onDamaged -= OnDamaged;
		controller.onKilled -= OnKilled;
		//controller.onMoved -= () =>
		//controller.onRotate -= () =>
		controller.onUseAction -= OnUseAction;
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
			GetController().FloatingText("Take this!");
		}
	}
	
	private void Start()
	{
		enabled = animator != null;
		
		if (!enabled)
		{
			Debug.LogWarning(this + " disabled because no animator");
			return;
		}
		
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
		animator.SetFloat(param, value);
		return true;
	}

	bool TrySetBoolAnimators(string param, bool value)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.SetBool(param, value);
		return true;
	}

	bool TrySetIntegerAnimators(string param, int value)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
		animator.SetInteger(param, value);
		return true;
	}

	bool TrySetTriggerAnimators(string param)
	{
		if (string.IsNullOrWhiteSpace(param)) return false;
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
