using System.Collections.Generic;
using RPGCharacterAnims.Lookups;
using UnityEngine;

public class CharacterView : MonoBehaviour, ICursorHandler, IView<CharacterController>
{
	static public List<CharacterView> allInstances  = new List<CharacterView>();
	
	private CharacterController characterController;
	public CharacterController GetController()
	{
		if(!characterController) characterController = GetComponent<CharacterController>();
		return characterController;
	}
	
	[Header("Collision")]
	[SerializeField] private CapsuleCollider capsuleCollider;
	[SerializeField] private new Rigidbody rigidbody;

	[Header("Circles")] 
	[ReadOnly][SerializeField] private float defaultScale = 1f;
	[SerializeField] private AnimationCurve alphaCurve = new AnimationCurve();
	[SerializeField] private CircleRenderer selectionCircle;
	[SerializeField] private CircleRenderer movePointsCircle;
	
	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private float velocityScale = 1.0f;
	
	#region EDITOR
	private void OnDrawGizmosSelected()
	{
		UpdateComponents();
	}
	#endregion
	
	#region PUBLIC

	
	public void CursorOver()
	{
		mouseOver = true;
	}
	#endregion
	
	#region PRIVATE
	
	private Color currentColor;
	private bool mouseOver;
	private bool wasSelected;
	private int partyIndex;
	private float alphaColor;
	private Vector3[] lastPath;
	private void Awake()
	{
		var controller = GetController();
		
		allInstances.Add(this);
		
		UpdateComponents();
		
		controller.onHealed += (amount) =>
		{
			GetController().FloatingText("Heal!");
		};

		controller.onPartyIndexChanged += (newIndex) =>
		{
		};

		controller.onDamaged += (amount) =>
		{
			GetController().FloatingText(Mathf.CeilToInt(amount).ToString());
		};

		controller.onKilled += () =>
		{
			animator.SetTrigger(AnimationParameters.Trigger);
			animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.DeathTrigger);
			gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_IGNORE);
		};

		controller.onMoved += () =>
		{
			// Debug.Log(this + " onMoved")
		};

		controller.onRotate += () =>
		{
			// Debug.Log(this + " onRotate")
		};

		controller.onRunAction += () =>
		{
			animator.SetTrigger(AnimationParameters.Trigger);
			animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.ReloadTrigger);
			GetController().FloatingText("Run!");
		};

		controller.onAttackAction += (target) =>
		{
			GetController().FloatingText("Take this!");
		};
	}

	private void OnDestroy()
	{
		allInstances.Remove(this);
	}
	
	private void Start()
	{
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_CHARACTER);
		
		if (animator != null)
		{
			animator.SetFloat(AnimationParameters.VelocityX, 0.0f);      
			animator.SetFloat(AnimationParameters.VelocityZ, 0.0f);        
			animator.SetInteger(AnimationParameters.Weapon, -1);
			animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
			animator.SetTrigger(AnimationParameters.Trigger);
		}
	}
	void LateUpdate()//TODO optim
	{
		var controller = GetController();
		
		currentColor = ColorSettings.GetCharacterColor(GetController(), true, false);
		alphaColor = alphaCurve.Evaluate(PlayerSelection.IsCharacterSelected(controller) ? 1f : mouseOver ? 0.5f : 0.0f);
		
		if (selectionCircle)
		{
			selectionCircle.SetColor(currentColor, alphaColor);
		}

		if (movePointsCircle)
		{
			movePointsCircle.SetColor(currentColor, alphaColor);
			movePointsCircle.SetProgress(controller.GetMovePointsProgress());
		}

		if (animator != null)
		{
			var localVelocity = controller.GetVelocity();
			var isMoving = controller.IsMoving();
			animator.SetFloat(AnimationParameters.VelocityX, 0);      
			animator.SetFloat(AnimationParameters.VelocityZ, localVelocity.magnitude * velocityScale);  
			animator.SetBool(AnimationParameters.Moving, isMoving);
		}
		
		UpdateComponents();

		mouseOver = false;
	}
	
//8	private void FloatingText(string msg) => GetController().Fl

	private void UpdateComponents()
	{
		var controller = GetController();
		var agent = controller.GetAgent();
		var radius = agent.GetRadius();
		var height = agent.GetHeight();

		if (selectionCircle)
		{
			selectionCircle.transform.localScale = Vector3.one * radius * defaultScale;
		}
		
		if (capsuleCollider)
		{
			capsuleCollider.height = height;
			capsuleCollider.radius = radius;
			capsuleCollider.center = Vector3.up * height / 2f;
		}

		if (rigidbody)
		{
			rigidbody.isKinematic = true;
			rigidbody.useGravity = false;
		}
	}

	/*private void SetColor(Color newColor)
	{
		currentColor = newColor; // TODO event "color changed"
	}*/
    #endregion
}
