using System.Linq;
using System.Collections.Generic;
using Oddworm.Framework;
using Pathfinding;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour, ICursorHandler
{
	private const string TAG_PLAYER = "Player";
	private const string TAG_ENEMY = "Enemy";
	public bool IsPlayer() => CompareTag(TAG_PLAYER);
	public bool IsEnemy() => CompareTag(TAG_ENEMY);

	public bool IsDead() => data.IsDead();
	
	static public CharacterView SpawnPlayerAt(CharacterView characterViewPrefab, CharacterData characterData, Waypoint waypoint)
	{
		var newChar = Instantiate(characterViewPrefab.gameObject).GetComponent<CharacterView>();
		newChar.data = characterData;
		return newChar;
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

	public const float MIN_FACING_DOT = 0.9f;
	
	[Header("Stats")]  // todo use scriptable
	[SerializeField] private CharacterData data;

	public bool TryGetData(out CharacterData outData)
	{
		outData = data;
		return outData != null;
	}
	
	public float GetAttackRange() => data.attackRange;
	public float GetRadius() => radius;
	public Texture GetPortraitTexture() => data.portraitTexture;
	public string GetDisplayName() => data.displayName;
	public bool CanMove() => data.GetRemainingMovePoints() > 0f;
	public bool CanAttack() => data.CanAttack();
	
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
	[SerializeField] [Range(0.01f, 0.50f)] private float minNavDistance = 0.01f; // put this in character view
	[SerializeField] private List<LineRenderer> pathRenderers;
	
	[Header("AI")]
	[SerializeField] private List<AIBrainData> aiBrains = new List<AIBrainData>();
	public List<AIBrainData> brainList => aiBrains;
	
	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private float velocityScale = 1.0f;

	private void OnDrawGizmosSelected()
	{
		UpdateComponents();
		aiBrains.ForEach(b =>
		{
			b?.DrawGizmos(this);
		});
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

	public void StopAgent()
	{
		agent?.StopMovement();
	}

	public int GetCurrentHealthInt() => (int)data.GetRemainingHealth();
	public int GetMaxHealth() => data.GetMaxHealth();
	public float GetDamageProgress() => data.GetDamageProgress();

	public void TakeDamage(float amount, bool setData)
	{
		Debug.Log(this + " TakeDamage " + amount);
		DebugGUI.FloatingText(GetCenterPosition(), amount.ToString("N2"));
		if(setData) data.TakeDamage(amount);
	}

	public bool IsAtAttackRange(CharacterView target, bool losCheck, float drawDuration = 0.0f)
	{
		var atkRange = GetAttackRange();
		atkRange = Mathf.Max(atkRange, GetRadius() + target.GetRadius() + minNavDistance);
		if (Vector3.Distance(GetPosition(), target.GetPosition()) <= atkRange)
		{
			return !losCheck || LOSCheck(target, drawDuration);
		}
		else
		{
			return false;
		}
	}

	bool LOSCheck(CharacterView other, float drawDuration = 0.0f)
	{
		var start = GetEyesPosition();
		var end = other.GetEyesPosition();
		if(Physics.Linecast(start, end, out var hit,
			   LayerMask.GetMask(GameView.LAYER_WALL, GameView.LAYER_GROUND)))
		{
			Debug.Log(this + " LOSCheck " + other + " collide " + hit.collider);
			DebugGUI.Line(start, end, Color.red, drawDuration);
			return false;
		}

		DebugGUI.Line(start, end, Color.green, drawDuration);
		return true;
	}

	public CharacterView GetClosestCharacter(IEnumerable<CharacterView> characters)
	{
		return characters.OrderBy(c => Vector3.Distance(GetPosition(), c.GetPosition())).FirstOrDefault();
	}

	public List<CharacterView> GetNearbyLivingCharacters(float range, float drawDuration = 0f, Color drawColor = default)
	{
		var outList = new List<CharacterView>();
		var center = GetPosition();
		DebugGUI.Disc(center, Vector3.up, range, drawColor, drawDuration);
		foreach (var coll in Physics.OverlapSphere(center, range, LayerMask.GetMask(GameView.LAYER_CHARACTER)))
		{
			if (coll.TryGetComponent(out CharacterView otherCharacter) && otherCharacter != this && !otherCharacter.IsDead())
			{
				outList.Add(otherCharacter);
			}
		}

		return outList;
	}

	private AgentWrapper agent;
	private Color currentColor;
	private bool mouseOver;
	private bool isSelected;
	private int partyIndex;
	private Vector3[] lastPath;

	private void SetPartyIndex(int newIndex)
	{
//		Debug.Log(this + " SetPartyIndex " + newIndex);
		partyIndex = newIndex;
		SetColor(partyIndex >= 0 ? partyColor : IsEnemy() ? enemyColor : defaultColor);
	}

	private void Awake()
	{
		currentColor = defaultColor;

		if (data == null)
		{
			Debug.LogError(this + " stats is null");
			data = ScriptableObject.CreateInstance<CharacterData>();
		}
		
		if (TryGetComponent<AIPath>(out var aiPath))
		{
			agent = new AgentWrapper()
			{
				aiPath = aiPath
			};
			
			agent.SetCanMove(false);
			agent.SetObstacle(true);
		}
	}

	void OnDestroy()
	{
		data.Dispose();
		RegisterEvents(false);
	}

	public void Init(CharacterData newData = null)
	{
		if (newData != null)
			data = newData;
		
		UpdateComponents();
		RegisterEvents(true);
		data.Init(this);
	}

	void RegisterEvents(bool value)
	{
		Debug.Log(this + " RegisterEvents " + value);
		var gameView = GameView.GetInstance();
		
		if (value)
		{
			data.partyIndexChanged += SetPartyIndex;
			gameView.characterSelected += CharacterSelected;
			//TODO for network?		GameData.characterMoved += CharacterMoved;
		}
		else
		{
			data.partyIndexChanged -= SetPartyIndex;
			gameView.characterSelected -= CharacterSelected;
			//TODO for network?		GameData.characterMoved -= CharacterMoved;
		}
	}

	private void CharacterSelected(CharacterView arg1, bool arg2)
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

	public bool WaypointReached(Waypoint wp)
	{
		return Vector3.Distance(GetPosition(), wp.GetPosition()) <= agent.reachDistance;
	}

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
			lastPath = Utils.LimitPathTotalLength(lastPath, data.GetRemainingMovePoints());
			pathRenderers[1].positionCount = lastPath.Length;
			pathRenderers[1].SetPositions(lastPath);
		}
		
		pathRenderers.ForEach(p => p.enabled = p.positionCount > 0);
	}

	void SetColor(Color newColor)
	{
		currentColor = newColor;
		pathRenderers.ForEach(p => p.startColor = p.endColor = Utils.ColorAlpha(newColor, Mathf.Lerp(0.2f, 0.8f, (float)pathRenderers.IndexOf(p))));
	}

	void Start()
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
    void LateUpdate()
    {
	    hasActions = data.GetRemainingActions() > 0;
	    alphaColor = alphaCurve.Evaluate(isSelected ? 1f : mouseOver ? 0.5f : 0.0f);
	    
	    selectionCircle.SetColor(hasActions ? currentColor : disabledColor, alphaColor);
	    movePointsCircle.SetColor(currentColor, alphaColor);
	    movePointsCircle.SetProgress(data.GetMovePointsProgress());

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

    public void SetTeleport(Waypoint wp)
    {
	    var newPos = wp.GetPosition();
	    var newRot = wp.GetRotation();

	    data.SetPosition(newPos);
	    data.SetRotation(newRot);
	    agent.Teleport(newPos, newRot);
    }

    public void SetRotateTowards(Vector3 worldPosition, float f = -1f)
    {
	    LookDirection(worldPosition - GetPosition(), f);
    }

    private void LookDirection(Vector3 getDirection, float f)
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
	    
	    data.SetRotation(GetRotation());
    }

    public Vector3 GetEyesPosition() => transform.position + Vector3.up * height * 0.9f;
    public Vector3 GetCenterPosition() => transform.position + Vector3.up * height * 0.5f;

    public int GetRotation() => (int)transform.eulerAngles.y;

    public bool HasActions() => data.GetRemainingActions() > 0;

    public bool CanInteract()
    {
	    return HasActions();
    }

    public bool CanUseRunAction()
    {
	    return  HasActions();
    }

    public bool GetPath(out Vector3[] path)
    {
	    path = lastPath;
		return path != null && path.Length > 0;
    }

    public bool IsFacingDirection(Vector3 dir)
    {
	    dir.y = 0;
	    dir.Normalize();
	    var dot = Vector3.Dot(dir, transform.forward);
	    //Debug.Log(dot);
	   // DebugGUI.FloatingText(transform.position, dot.ToString("N2"));
	    return dot >= MIN_FACING_DOT;
    }

    public void OnGUIDebug()
    {
	    GUILayout.Label(GetDisplayName());
	    GUILayout.Label("actions: " + data.GetRemainingActions());
    }

    public bool TryGetAIBrain(out AIBrainData aiBrain)
    {
	    aiBrains = aiBrains.OrderBy(b => b.GetPriority(this)).ToList();
	    Debug.Log(this + " TryGetAIBrain " + Utils.ArrayStr(aiBrains));
	    return (aiBrain = aiBrains.FirstOrDefault()) != null;
    }

    public void PerformsAttack(CharacterView target)
    {
	    data.ConsumeActions(1);
    }
}
