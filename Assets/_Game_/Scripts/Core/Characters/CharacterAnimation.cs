using System;
using System.Collections.Generic;
using RPGCharacterAnims.Lookups;
using UnityEngine;

public class CharacterAnimation : MonoBehaviour, IView<CharacterController>
{
	[System.Serializable]
	public struct Parameters
	{
		public string moving;
		public string velocityX;
		public string velocityZ;
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
			velocityX = "VelocityX",
			velocityZ = "VelocityZ",
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
	
	static public List<CharacterAnimation> activeInstances  = new List<CharacterAnimation>();
	
	private CharacterController controller;
	public CharacterController GetController()
	{
		if(!controller) controller = GetComponent<CharacterController>();
		return controller;
	}
	
	[Header("Animation")]
	[SerializeField] private Animator[] animators;
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
	}

	void OnKilled()
	{
		SetTrigger(parameters.trigger);
		SetInteger(parameters.triggerNumber, (int)AnimatorTrigger.DeathTrigger);
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_IGNORE);
	}

	void OnUseAction(CharacterAction actionType, object args = null)
	{
		if (actionType == CharacterAction.RunAction)
		{
			SetTrigger(parameters.trigger);
			SetInteger(parameters.triggerNumber, (int)AnimatorTrigger.ReloadTrigger);
			GetController().FloatingText("Run!");
		}
		
		else if (actionType == CharacterAction.AttackAction)
		{
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
		
		SetFloat(parameters.velocityX, 0.0f);      
		SetFloat(parameters.velocityZ, 0.0f);        
		SetInteger(parameters.weapon, -1);
		SetInteger(parameters.triggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
		SetTrigger(parameters.trigger);
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
		SetFloat(parameters.velocityX, 0);      
		SetFloat(parameters.velocityZ, localVelocity.magnitude * velocityScale);  
		SetBool(parameters.moving, isMoving);
	}

	// TODO keep for later?
	void SetFloat(string param, float value) => AnimatorUtils.TrySetFloat(param, value, animators);
	void SetBool(string param, bool value) => AnimatorUtils.TrySetBool(param, value, animators);
	void SetInteger(string param, int value) => AnimatorUtils.TrySetInteger(param, value, animators);
	void SetTrigger(string param) => AnimatorUtils.TrySetTrigger(param, animators);

	//void SetFloat(int param, float value) => AnimatorUtils.TrySetFloat(param, value, animators);
	//void SetBool(int param, bool value) => AnimatorUtils.TrySetBool(param, value, animators);
	//void SetInteger(int param, int value) => AnimatorUtils.TrySetInteger(param, value, animators);
	//void SetTrigger(int param) => AnimatorUtils.TrySetTrigger(param, animators);
	#endregion
}
