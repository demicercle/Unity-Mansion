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
		public bool isMoving => !aiPath.isStopped;
		public bool reachedDestination => aiPath.reachedDestination;
		public float reachDistance => aiPath.endReachedDistance;
		public bool pathPending => aiPath.pathPending;

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
//				Debug.Log(aiPath + " ClearDestination()");
				hasDestination = false;
				aiPath.destination = aiPath.transform.position;
				onSearchPath?.Invoke();
			}
		}

		public void StopMovement()
		{
			aiPath.isStopped = true;
			ClearDestination();
		}
	}

	public const float MIN_FACING_DOT = 0.9f;
	
	[Header("Stats")]  // todo use scriptable
	[SerializeField] private CharacterStats stats;
	public Texture GetPortraitTexture() => stats.portraitTexture;
	public string GetDisplayName() => stats.displayName;
	public bool CanMove() => stats.GetRemainingMovePoints() > 0f;
	
	[Header("Selection")]
	[SerializeField] private CircleRenderer selectionCircle;
	[SerializeField] private CircleRenderer movePointsCircle;
	[SerializeField] private Color defaultColor = Color.blue, partyColor = Color.green, enemyColor = Color.red, disabledColor = Color.gray;
	[SerializeField] private AnimationCurve alphaCurve = new AnimationCurve();
	
	[Header("Collision")]
	[SerializeField] private CapsuleCollider capsuleCollider;
	[SerializeField] private Rigidbody rigidbody;
	
	[Header("Navigation")]
	[SerializeField] [Range(0.0f, 10.0f)] private float moveSpeed = 4.5f;
	//[SerializeField] [Range(0.0f, 10.0f)] private float rotationSpeed = 1.0f;
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

	private AgentWrapper agent;
	private Color currentColor;
	private bool mouseOver;
	private bool isSelected;
	private bool inParty;
	private float usedMovePoints;

	private void Awake()
	{
		currentColor = defaultColor;

		if (stats == null)
		{
			Debug.LogError(this + " stats is null");
			stats = ScriptableObject.CreateInstance<CharacterStats>();
		}

		stats.Init();
		
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
		
		RegisterEvents(true);
	}

	void OnDestroy()
	{
		stats.Dispose();
		RegisterEvents(false);
	}

	void RegisterEvents(bool value)
	{
		if (value)
		{
			GameEvents.characterAddedToParty += CharacterAddedToParty;
			GameEvents.characterSelected += CharacterSelected;
			GameEvents.newTurnStarted += TurnBegins;
			GameEvents.characterMoved += CharacterMoved;
		}
		else
		{
			GameEvents.characterAddedToParty -= CharacterAddedToParty;
			GameEvents.characterSelected -= CharacterSelected;
			GameEvents.newTurnStarted -= TurnBegins;
			GameEvents.characterMoved -= CharacterMoved;
		}
	}

	private Vector3 lastPosition;
	private void CharacterMoved(Character character, Vector3 newPos)
	{
		if (character == this)
		{
			var deltaPos = (newPos - lastPosition).magnitude;
			lastPosition = newPos;
			stats.ConsumeMovePoints(deltaPos);
			if (stats.GetRemainingMovePoints() <= 0f)
			{
				agent.StopMovement();
			}
		}
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
				agent.onSearchPath += UpdatePath;
			}
			else
			{
				pathRenderers.ForEach(p => p.enabled = false);
				agent.SetObstacle(true);
				agent.onSearchPath -= UpdatePath;
			}
		}
	}

	private Vector3[] lastPath;
	public void UpdatePath()
	{
		lastPath = agent.GetCurrentPath();
		
		if (pathRenderers.Count > 0)
		{
			pathRenderers[0].positionCount = lastPath.Length;
			pathRenderers[0].SetPositions(lastPath);
		}

		if (pathRenderers.Count > 1)
		{
			lastPath = Utils.LimitPathTotalLength(lastPath, stats.GetRemainingMovePoints());
			pathRenderers[1].positionCount = lastPath.Length;
			pathRenderers[1].SetPositions(lastPath);
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
		gameObject.layer = LayerMask.NameToLayer(GameManager.LAYER_CHARACTER);
		
		lastPosition = GetPosition();
		
	    if (animator != null)
	    {
		    animator.SetFloat(AnimationParameters.VelocityX, 0.0f);      
		    animator.SetFloat(AnimationParameters.VelocityZ, 0.0f);        
		    animator.SetInteger(AnimationParameters.Weapon, -1);
		    animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
		    animator.SetTrigger(AnimationParameters.Trigger);
	    }
    }

	private bool hasActions;
	private float alphaColor;
    void LateUpdate()
    {
	    hasActions = stats.GetRemainingActions() > 0;
	    alphaColor = alphaCurve.Evaluate(isSelected ? 1f : mouseOver ? 0.5f : 0.0f);
	    
	    selectionCircle.SetColor(hasActions ? currentColor : disabledColor, alphaColor);
	    movePointsCircle.SetColor(currentColor, alphaColor);
	    movePointsCircle.SetProgress(stats.GetMovePointsProgress());

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

    private void TurnBegins(int turnIndex)
    {
	    usedMovePoints = 0.0f;
    }

    public void RotateTowards(Vector3 worldPosition, float f = -1f)
    {
	    LookDirection(worldPosition - GetPosition(), f);
    }

    public void LookDirection(Vector3 getDirection, float f)
    {
	    getDirection.y = 0;
	    getDirection.Normalize();

	    if (getDirection.sqrMagnitude < 0.01f)
	    {
		    Debug.LogWarning(this + " invalid direction");
		    return;
	    }
	    
	    if (f < 0f)
	    {
		    DebugGUI.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 1.0f);
		    transform.rotation = Quaternion.LookRotation(getDirection);
	    }
	    else
	    {
		    DebugGUI.WireArrow(GetEyesPosition(), getDirection, new Vector3(radius + 1.0f, 1, 1), Color.cyan, 0.0f);
		    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(getDirection), f * agent.aiPath.rotationSpeed); //TODO property "rotationSpeed" separated from agent
	    }
    }

    public Vector3 GetEyesPosition() => transform.position + Vector3.up * height * 0.9f;

    public void ConsumeActions(int consumeActions)
    {
	    stats.ConsumeActions(consumeActions);
    }

    public bool CanInteract()
    {
	    return stats.GetRemainingActions() > 0;
    }

    public bool GetFirstPathPoint(out Vector3 firstPoint, Vector3[] path = null)
    {
	    if (path == null)
	    {
		    path = lastPath;
	    }
	    
	    if (path.Length > 1)
	    {
		    firstPoint = path[1];
		    return true;
	    }

	    firstPoint = default(Vector3);
	    return false;
    }

    public bool IsFacingDirection(Vector3 dir)
    {
	    dir.y = 0;
	    dir.Normalize();
	    var dot = Vector3.Dot(dir, transform.forward);
	    //Debug.Log(dot);
	    DebugGUI.WorldLabel(transform.position, dot.ToString("N2"));
	    return dot >= MIN_FACING_DOT;
    }
}
