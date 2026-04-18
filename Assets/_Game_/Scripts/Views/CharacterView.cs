using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Oddworm.Framework;
using Pathfinding;
using Pathfinding.Drawing;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour, ICursorHandler, IView<CharacterController>
{
	static public List<CharacterView> allInstances  = new List<CharacterView>();
	
	private CharacterController characterController;
	public CharacterController GetController()
	{
		if(!characterController) characterController = GetComponent<CharacterController>();
		return characterController;
	}
	
	[Header("Selection")]
	[SerializeField] private CircleRenderer selectionCircle;
	[SerializeField] private CircleRenderer movePointsCircle;
	[SerializeField] private Color defaultColor = Color.blue, partyColor = Color.green, enemyColor = Color.red, disabledColor = Color.gray, deadColor = Color.black;
	[SerializeField] private AnimationCurve alphaCurve = new AnimationCurve();
	
	[Header("Collision")]
	[SerializeField] private CapsuleCollider capsuleCollider;
	[SerializeField] private new Rigidbody rigidbody;
	
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
	public Color GetCurrentColor() => currentColor;
	
	public void CursorOver()
	{
		mouseOver = true;
	}
	#endregion
	
	#region PRIVATE
	private bool inParty;
	private bool isEnemy;
	private Color currentColor;
	private bool mouseOver;
	private bool wasSelected;
	private int partyIndex;
	private bool hasActions;
	private float alphaColor;
	private bool killed;
	private Vector3[] lastPath;
	
	private void Awake()
	{
		var controller = GetController();
		
		allInstances.Add(this);
		currentColor = defaultColor;
		
		UpdateComponents();
		
		controller.onHealed += (amount) =>
		{
			FloatingText("Heal!");
		};

		controller.onPartyIndexChanged += (newIndex) =>
		{
		};

		controller.onDamaged += (amount) =>
		{
			FloatingText(Mathf.CeilToInt(amount).ToString());
		};

		controller.onKilled += () =>
		{
			animator.SetTrigger(AnimationParameters.Trigger);
			animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.DeathTrigger);
			gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_IGNORE);
			SetColor(deadColor);
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
			FloatingText("Run!");
		};

		controller.onAttackAction += (target) =>
		{
			FloatingText("Take this!");
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
	    
		inParty = controller.InParty();
		isEnemy = controller.IsEnemyToPlayer();
		killed = controller.IsDead();
		SetColor(killed ? deadColor : inParty ? partyColor : isEnemy ? enemyColor : defaultColor);
	    
		hasActions = controller.HasActions();
		alphaColor = alphaCurve.Evaluate(Selection.IsCharacterSelected(controller) ? 1f : mouseOver ? 0.5f : 0.0f);
	    
		selectionCircle.SetColor(hasActions ? currentColor : disabledColor, alphaColor);
		movePointsCircle.SetColor(currentColor, alphaColor);
		movePointsCircle.SetProgress(controller.GetMovePointsProgress());

		if (animator != null)
		{
			var localVelocity = controller.GetVelocity();
			var isMoving = controller.IsMoving();
			animator.SetFloat(AnimationParameters.VelocityX, 0);      
			animator.SetFloat(AnimationParameters.VelocityZ, localVelocity.magnitude * velocityScale);  
			animator.SetBool(AnimationParameters.Moving, isMoving);
		}

		mouseOver = false;
	}
	
	private void FloatingText(string msg) => DebugGUI.FloatingText(GetController().GetCenterPosition(), msg);

	private void UpdateComponents()
	{
		var controller = GetController();
		var agent = controller.GetAgent();
		var radius = agent.GetRadius();
		var height = agent.GetHeight();

		if (selectionCircle)
		{
			selectionCircle.radius = radius;
			selectionCircle.UpdateCircle();
		}

		if (movePointsCircle)
		{
			movePointsCircle.radius = radius;
			movePointsCircle.UpdateCircle();
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

	private void SetColor(Color newColor)
	{
		currentColor = newColor; // TODO event "color changed"
	}
    #endregion
}
