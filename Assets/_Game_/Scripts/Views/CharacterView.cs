using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Oddworm.Framework;
using Pathfinding;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour, ICursorHandler, IView<CharacterController>
{
	public CharacterController GetController() => Utils.GetOrAddComponent<CharacterController>(gameObject);


	private AgentWrapper agent;
	public AgentWrapper GetAgent() // TODO in character controller
	{
		return agent;
	}
	
	private bool inParty;
	private bool isEnemy;
	void Awake()
	{
		allInstances.Add(this);
		currentColor = defaultColor;

		if (TryGetComponent<AIPath>(out var aiPath))
		{
			agent = new AgentWrapper()
			{
				aiPath = aiPath
			};
			
			agent.SetCanMove(false);
			agent.SetObstacle(true);
		}
		else
		{
			Debug.LogError(this + " has no agent!");
		}
		
		UpdateComponents();
		
		GetController().onHealed += (amount) =>
		{
			FloatingText("Heal!");
		};

		GetController().onPartyIndexChanged += (newIndex) =>
		{
			inParty = GetController().InParty();
			isEnemy = GetController().IsEnemyToPlayer();
			SetColor(inParty ? partyColor : isEnemy ? enemyColor : defaultColor);
		};

		GetController().onDamaged += (amount) =>
		{
			FloatingText(Mathf.CeilToInt(amount).ToString());
		};

		GetController().onKilled += () =>
		{
			StopAgent();
			animator.SetTrigger(AnimationParameters.Trigger);
			animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.DeathTrigger);
			gameObject.layer = LayerMask.NameToLayer(GameView.LAYER_IGNORE);
			SetColor(deadColor);
			    
			var gameView = GameView.GetInstance();
			if (gameView.IsCharacterSelected(this))
			{
				gameView.SelectNextCharacter();
			}
		};

		GetController().onMoved += () =>
		{
			// Debug.Log(this + " onMoved")
		};

		GetController().onRotate += () =>
		{
			// Debug.Log(this + " onRotate")
		};
	}

	private void OnDestroy()
	{
		allInstances.Remove(this);
	}

	public void FloatingText(string msg) => DebugGUI.FloatingText(GetCenterPosition(), msg);
	
	static public List<CharacterView> allInstances  = new List<CharacterView>();
	
	
	private const string TAG_PLAYER = "Player";
	public bool IsPlayer() => CompareTag(TAG_PLAYER);
	
	private const string TAG_ENEMY = "Enemy";
	public bool IsEnemy() => CompareTag(TAG_ENEMY);

	public Vector3 GetPosition() => transform.position;

	public int GetRotation() => (int)transform.eulerAngles.y;

	public void CursorOver()
	{
		mouseOver = true;
	}

    public void OnGUIDebug()
    {
	    
    }

    public IEnumerator PerformsAttack(CharacterView target)
    {
	    Debug.Log(this + " PerformsAttack " + target);
	    DebugGUI.FloatingText(GetPosition(), "Take this!");
	    yield return new WaitForSeconds(1.0f);
    }
	
	[Header("Selection")]
	[SerializeField] private CircleRenderer selectionCircle;
	[SerializeField] private CircleRenderer movePointsCircle;
	[SerializeField] private Color defaultColor = Color.blue, partyColor = Color.green, enemyColor = Color.red, disabledColor = Color.gray, deadColor = Color.black;
	[SerializeField] private AnimationCurve alphaCurve = new AnimationCurve();
	
	[Header("Collision")]
	[SerializeField] private CapsuleCollider capsuleCollider;
	[SerializeField] private Rigidbody rigidbody;
	
	[Header("Navigation")]
	[SerializeField] private List<LineRenderer> pathRenderers;
	
	[Header("AI")]
	[SerializeField] private List<AIBrainData> aiBrains = new List<AIBrainData>();
	public List<AIBrainData> brainList => aiBrains;
	
	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private float velocityScale = 1.0f;

	private void UpdateComponents()
	{
		var controller = GetController();
		var radius = controller.GetRadius();
		var height = controller.GetHeight();
		
		if (TryGetComponent<AIPath>(out var aiPath))
		{
			aiPath.radius = radius;
			aiPath.height = height;
			aiPath.maxSpeed = controller.GetMaxSpeed();
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

	public void StopAgent()
	{
		agent?.StopMovement();
	}

	public CharacterView GetClosestCharacter(IEnumerable<CharacterView> characters)
	{
		return characters.OrderBy(c => Vector3.Distance(GetPosition(), c.GetPosition())).FirstOrDefault();
	}

	public bool WaypointReached(Waypoint wp)
	{
		return Vector3.Distance(GetPosition(), wp.GetPosition()) <= agent.reachDistance;
	} // TODO in game data
	
	
	private Color currentColor;
	private bool mouseOver;
	private bool wasSelected;
	private int partyIndex;
	private Vector3[] lastPath;

	private void UpdatePath()
	{
		lastPath = agent.GetCurrentPath();
		
		if (pathRenderers.Count > 0)
		{
			pathRenderers[0].positionCount = lastPath.Length;
			pathRenderers[0].SetPositions(lastPath);
		}

		if (pathRenderers.Count > 1)
		{
			lastPath = Utils.LimitPathTotalLength(lastPath, GetController().GetRemainingMovePoints());
			pathRenderers[1].positionCount = lastPath.Length;
			pathRenderers[1].SetPositions(lastPath);
		}
		
		pathRenderers.ForEach(p => p.enabled = p.positionCount > 0);
	}

	private void SetColor(Color newColor)
	{
		currentColor = newColor;
		pathRenderers.ForEach(p => p.startColor = p.endColor = Utils.ColorAlpha(newColor, Mathf.Lerp(0.2f, 0.8f, (float)pathRenderers.IndexOf(p))));
	}

    private void Start()
    {
	    gameObject.layer = LayerMask.NameToLayer(GameView.LAYER_CHARACTER);
		
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
    private bool killed;
    
    void LateUpdate()
    {
	    hasActions = GetController().HasActions();
	    alphaColor = alphaCurve.Evaluate(GetController().IsSelected() ? 1f : mouseOver ? 0.5f : 0.0f);
	    
	    selectionCircle.SetColor(hasActions ? currentColor : disabledColor, alphaColor);
	    movePointsCircle.SetColor(currentColor, alphaColor);
	    movePointsCircle.SetProgress(GetController().GetMovePointsProgress());

	    if (animator != null)
	    {
		    var localVelocity = agent.velocity;
		    animator.SetFloat(AnimationParameters.VelocityX, 0);      
		    animator.SetFloat(AnimationParameters.VelocityZ, localVelocity.magnitude * velocityScale);  
		    animator.SetBool(AnimationParameters.Moving, agent.isMoving);
	    }

	    mouseOver = false;
    }

    private void OnDrawGizmosSelected()
    {
	    UpdateComponents();
	    aiBrains.ForEach(b =>
	    {
		    b?.DrawGizmos(GetController());
	    });
    }
    
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

	    public void Teleport(Vector3 newPos, int newRot)
	    {
		    aiPath.Teleport(newPos, true);
		    aiPath.rotation = Quaternion.Euler(0, newRot, 0);
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

	    public Vector3[] GetCurrentPath()
	    {
		    if (!hasDestination)
			    return new Vector3[0];
			
		    var buffer = new List<Vector3>();
		    aiPath.GetRemainingPath(buffer, out var stale);
		    return buffer.ToArray();
	    }
		
	    public void AssignDestination(Vector3 hitPoint)
	    {
		    //Debug.Log(aiPath + " AssignDestination " + hitPoint);
		    aiPath.destination = hitPoint;
		    aiPath.SearchPath();
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
}
