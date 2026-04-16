using System.Linq;
using System.Collections.Generic;
using Oddworm.Framework;
using Pathfinding;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour, ICursorHandler
{
	public class AgentWrapper
	{
		public AIPath aiPath;
		public Vector3 velocity => aiPath.velocity;
		public bool isMoving => aiPath.isStopped == false;
		public bool reachedDestination => aiPath.reachedDestination;

		private bool hasDestination;

		public System.Action onSearchPath
		{
			get => aiPath.onSearchPath;
			set => aiPath.onSearchPath = value;
		}

		public void SetCanMove(bool value)
		{
			aiPath.simulateMovement = value;
			aiPath.isStopped = !value;
		}

		public void SetObstacle(bool value)
		{
			if (aiPath.TryGetComponent(out GraphUpdateScene graphUpdate))
			{
				graphUpdate.enabled = value;
				if (value)
				{
					graphUpdate.points = new[]
					{
						Vector3.forward * aiPath.radius,
						Vector3.right * aiPath.radius,
						-Vector3.forward * aiPath.radius,
						-Vector3.right * aiPath.radius,
					};
				}
			}
		}

		public Vector3[] GetCurrentPath(bool ignoreLimit = false)
		{
			if (!hasDestination)
				return new Vector3[0];
			
			var buffer = new List<Vector3>();
			aiPath.GetRemainingPath(buffer, out var stale);
			return buffer.ToArray();
		}
		
		public void AssignDestination(Vector3 hitPoint)
		{
			aiPath.destination = hitPoint;
			hasDestination = true;
			onSearchPath?.Invoke();
			DbgDraw.Disc(hitPoint, Quaternion.identity, aiPath.radius, Utils.ColorAlpha(Color.cyan, 0.2f), 0f);
		}

		public void ClearDestination()
		{
			if (hasDestination)
			{
				Debug.Log(aiPath + " ClearDestination()");
				hasDestination = false;
				aiPath.destination = aiPath.transform.position;
				onSearchPath?.Invoke();
			}
		}
	}
	
	[Header("Stats")]  // todo use scriptable
	[SerializeField] private string displayName;
	public string GetDisplayName() => displayName;
	[SerializeField] private Texture2D portrait;
	public Texture GetPortraitTexture() => portrait;
	[SerializeField] private float movePoints;
	[SerializeField] private float moveSpeed;
	
	[Header("Selection Circle")]
	[SerializeField] private CircleRenderer circleRenderer;
	[SerializeField] private Color defaultColor = Color.blue, partyColor = Color.green, enemyColor = Color.red;
	[SerializeField] private AnimationCurve alphaCurve = new AnimationCurve();
	
	[Header("Collision")]
	[SerializeField] private CapsuleCollider capsuleCollider;
	[SerializeField] private Rigidbody rigidbody;
	
	[Header("Navigation")]
	[SerializeField] [Range(0.1f, 2.0f)] private float radius = 0.5f;
	[SerializeField] [Range(0.1f, 3.0f)] private float height = 1.5f;
	[SerializeField] private List<LineRenderer> pathRenderers;
	
	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private float velocityScale = 1.0f;

	private void OnDrawGizmosSelected()
	{
		UpdateComponents();
	}

	private void UpdateComponents()
	{
		if (TryGetComponent<AIPath>(out var aiPath))
		{
			aiPath.radius = radius;
			aiPath.height = height;
			aiPath.maxSpeed = moveSpeed;
		}

		if (circleRenderer)
		{
			circleRenderer.radius = radius;
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

		foreach (var pathRenderer in pathRenderers)
		{
			pathRenderer.enabled = false;
			pathRenderer.useWorldSpace = true;
		}
	}

	public void CursorOver()
	{
		mouseOver = true;
	}

	public bool TryGetAgent(out AgentWrapper out_agent)
	{
		out_agent = agent;
		return out_agent.aiPath != null;
	}

	public float GetRemainingMovePoints() => movePoints - usedMovePoints;

	public void ConsumeMovePoints(float delta)
	{
		usedMovePoints += Mathf.Abs(delta);
	}

	private AgentWrapper agent;
	private Color currentColor;
	private bool mouseOver;
	private bool isSelected;
	private bool inParty;
	private float usedMovePoints;

	private void Awake()
	{
		currentColor = defaultColor;

		GameEvents.characterAddedToParty += CharacterAddedToParty;
		GameEvents.characterSelected += CharacterSelected;

		if (TryGetComponent<AIPath>(out var aiPath))
		{
			agent = new AgentWrapper()
			{
				aiPath = aiPath
			};
			
			agent.SetCanMove(false);
			agent.SetObstacle(true);
		}
		
		UpdateComponents();
	}

	void OnDestroy()
	{
		GameEvents.characterAddedToParty -= CharacterAddedToParty;
		GameEvents.characterSelected -= CharacterSelected;
	}

	private void CharacterSelected(Character arg1, bool arg2)
	{
		if (arg1 == this)
		{
			isSelected = arg2;

//			Debug.Log(this + " isSelected=" + isSelected);
			if (isSelected)
			{
				agent.SetObstacle(false);
				agent.onSearchPath += DrawPath;
			}
			else
			{
				pathRenderers.ForEach(p => p.enabled = false);
				agent.SetObstacle(true);
				agent.onSearchPath -= DrawPath;
			}
		}
	}

	public void DrawPath()
	{
		var points = agent.GetCurrentPath();
		
		if (pathRenderers.Count > 0)
		{
			pathRenderers[0].positionCount = points.Length;
			pathRenderers[0].SetPositions(points);
		}

		if (pathRenderers.Count > 1)
		{
			points = Utils.LimitPathTotalLength(points, GetRemainingMovePoints());
			pathRenderers[1].positionCount = points.Length;
			pathRenderers[1].SetPositions(points);
		}
		
		pathRenderers.ForEach(p => p.enabled = p.positionCount > 0);
	}

	private void CharacterAddedToParty(Character arg1, int arg2)
	{
		if (arg1 == this)
		{
			inParty = true;
			SetColor(partyColor);
		}
	}

	void SetColor(Color newColor)
	{
		currentColor = newColor;
		pathRenderers.ForEach(p => p.startColor = p.endColor = Utils.ColorAlpha(newColor, Mathf.Lerp(0.2f, 0.8f, (float)pathRenderers.IndexOf(p))));
	}

	void Start()
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
	    circleRenderer.SetColor(currentColor, alphaCurve.Evaluate(isSelected ? 1f : mouseOver ? 0.5f : 0.0f));

	    if (animator != null)
	    {
		    var localVelocity = agent.velocity;
		    animator.SetFloat(AnimationParameters.VelocityX, 0);      
		    animator.SetFloat(AnimationParameters.VelocityZ, localVelocity.magnitude * velocityScale);  
		    animator.SetBool(AnimationParameters.Moving, agent.isMoving);
	    }

	    mouseOver = false;
    }

    public Vector3 GetPosition()
    {
	    return transform.position;
    }
}
