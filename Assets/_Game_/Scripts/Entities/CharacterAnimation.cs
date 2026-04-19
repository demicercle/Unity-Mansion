using System;
using System.Collections.Generic;
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
	
	[Header("Animation")]
	[SerializeField] private Animator animator;
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

		if (!animator) animator = GetComponent<Animator>();
		if (!animator) animator = GetComponentInChildren<Animator>();
		if (!animator)
		{
			Debug.LogWarning(this + " has no animator!");
			enabled = false;
			return;
		}

		controller.onHealed += OnHealed;
		//controller.onPartyIndexChanged += (newIndex) =>
		controller.onDamaged += OnDamaged;
		controller.onKilled += OnKilled;
		//controller.onMoved += () =>
		//controller.onRotate += () =>
		controller.onRunAction += OnRunAction;
		controller.onAttackAction += OnAttackAction;
	}

	private void OnDestroy()
	{
		controller.onHealed -= OnHealed;
		//controller.onPartyIndexChanged -= (newIndex) =>
		controller.onDamaged -= OnDamaged;
		controller.onKilled -= OnKilled;
		//controller.onMoved -= () =>
		//controller.onRotate -= () =>
		controller.onRunAction -= OnRunAction;
		controller.onAttackAction -= OnAttackAction;
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
		animator.SetTrigger(AnimationParameters.Trigger);
		animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.DeathTrigger);
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_IGNORE);
	}

	void OnRunAction()
	{
		animator.SetTrigger(AnimationParameters.Trigger);
		animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.ReloadTrigger);
		GetController().FloatingText("Run!");
	}

	void OnAttackAction(CharacterController other)
	{
		GetController().FloatingText("Take this!");
	}
	
	private void Start()
	{
		if (animator != null)
		{
			animator.SetFloat(AnimationParameters.VelocityX, 0.0f);      
			animator.SetFloat(AnimationParameters.VelocityZ, 0.0f);        
			animator.SetInteger(AnimationParameters.Weapon, -1);
			animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
			animator.SetTrigger(AnimationParameters.Trigger);
		}
	}
	
	void LateUpdate()
	{
		if (animator != null)
		{
			var localVelocity = controller.GetVelocity();
			var isMoving = controller.IsMoving();
			if (minMovePointsToRun >= 0f && controller.HasUsedAction(CharacterAction.RunAction) && controller.HasMovePoints(minMovePointsToRun))
			{
				isMoving = true;
				localVelocity = Vector3.forward * controller.GetAgent().GetMaxSpeed();
			}
			animator.SetFloat(AnimationParameters.VelocityX, 0);      
			animator.SetFloat(AnimationParameters.VelocityZ, localVelocity.magnitude * velocityScale);  
			animator.SetBool(AnimationParameters.Moving, isMoving);
		}
	}
	
    #endregion
}
