using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RPGCharacterAnims.Lookups;
using UnityEngine;

public class CharacterAnimation : MonoBehaviour, IView<CharacterController>
{
	static public List<CharacterAnimation> activeInstances  = new List<CharacterAnimation>();
	
	private CharacterController controller;
	public CharacterController GetController()
	{
		if(!controller) controller = GetComponent<CharacterController>();
		return controller;
	}
	
	[SerializeField] private Animator[] animators;
	
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
		SetTriggerAnimators(parameters.getHitTrigger);
	}

	void OnKilled()
	{
		SetTriggerAnimators(parameters.trigger);
		SetIntegerAnimators(parameters.triggerNumber, (int)AnimatorTrigger.DeathTrigger);
		SetTriggerAnimators(parameters.deathTrigger);
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_IGNORE);
	}

	void OnUseAction(CharacterAction actionType, object args = null)
	{
		if (actionType == CharacterAction.RunAction)
		{
			SetTriggerAnimators(parameters.trigger);
			SetIntegerAnimators(parameters.triggerNumber, (int)AnimatorTrigger.ReloadTrigger);
			SetTriggerAnimators(parameters.runTrigger);
			GetController().FloatingText("Run!");
		}
		
		else if (actionType == CharacterAction.AttackAction)
		{
			SetTriggerAnimators(parameters.attackTrigger);
			GetController().FloatingText("Take this!");
		}
	}
	
	private void Start()
	{
		enabled = animators.Length > 0;
		
		if (!enabled)
		{
			Debug.LogWarning(this + " disabled because no animators");
			return;
		}
		
		SetFloatAnimators(parameters.velocityX, 0.0f);      
		SetFloatAnimators(parameters.velocityZ, 0.0f);        
		SetIntegerAnimators(parameters.weapon, -1);
		SetIntegerAnimators(parameters.triggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
		SetTriggerAnimators(parameters.trigger);
		SetBoolAnimators(parameters.moving, false);
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
		SetFloatAnimators(parameters.velocityX, 0);      
		SetFloatAnimators(parameters.velocityZ, localVelocity.magnitude * velocityScale);  
		SetBoolAnimators(parameters.moving, isMoving);
	}

	// TODO keep for later?
	void SetFloatAnimators(string param, float value)
	{
		if (string.IsNullOrWhiteSpace(param)) return;
		foreach(var animator in animators) animator.SetFloat(param, value);
	}

	void SetBoolAnimators(string param, bool value)
	{
		if (string.IsNullOrWhiteSpace(param)) return;
		foreach(var animator in animators) animator.SetBool(param, value);
	}

	void SetIntegerAnimators(string param, int value)
	{
		if (string.IsNullOrWhiteSpace(param)) return;
		foreach(var animator in animators) animator.SetInteger(param, value);
	}

	void SetTriggerAnimators(string param)
	{
		if (string.IsNullOrWhiteSpace(param)) return;
		foreach(var animator in animators) animator.SetTrigger(param);
	}

	void ResetTriggerAnimators(string param)
	{
		if (string.IsNullOrWhiteSpace(param)) return;
		foreach(var animator in animators) animator.ResetTrigger(param);
	}

	//void SetFloat(int param, float value) => AnimatorUtils.TrySetFloat(param, value, animators);
	//void SetBool(int param, bool value) => AnimatorUtils.TrySetBool(param, value, animators);
	//void SetInteger(int param, int value) => AnimatorUtils.TrySetInteger(param, value, animators);
	//void SetTrigger(int param) => AnimatorUtils.TrySetTrigger(param, animators);
	#endregion
}
