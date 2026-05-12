using System;
using UnityEngine;
using System.Collections;

public class CharacterFollows : MonoBehaviour
{
	private const float MinVelocityToLook = 0.5f;
	
	public static int debugMode { get; set; }
	
	[SerializeField] [Range(0.5f, 12.0f)] private float followDistance = 3.0f;
	[SerializeField] [Range(0.0f, 12.0f)] private float avoidDistance = 1.0f;
	[SerializeField] [Range(0.0f, 10f)] private float tickTime = 1.0f;
	[SerializeField] private float headingDistance = 0.5f;
	[SerializeField] [Min(0)] private float headingSpeed = 1.5f;

	public void SetLeader(CharacterEntity newLeader)
	{
		leaderCharacter = newLeader;
	}

	public CharacterEntity GetLeader() => leaderCharacter;

	private CharacterEntity selfCharacter;
	private CharacterEntity leaderCharacter;
	private CharacterAgent agent;
	private Transform dummy;
	private Vector3 headingPos;
	private Vector3 leaderWorldVelocity;
	private float distanceToDummy;
	private float distanceToLeader;

	private void Awake()
	{
		dummy = new GameObject("Follow Dummy (" + name + ")").transform;
	}

	private void Start()
	{
		enabled = false;
	}

	private void OnDestroy()
	{
		Destroy(dummy);
	}

	private void OnEnable()
	{
		if (debugMode>0) Debug.Log(this + " OnEnable");
		if (!selfCharacter)
		{
			selfCharacter = GetComponent<CharacterEntity>();
			if (!selfCharacter) Debug.LogError(this + " no character");
			agent = selfCharacter.GetAgent();
		}
		selfCharacter.Routines.onStarted += OnNewRoutineStarted;
		StartCoroutine(FollowCoroutine());
	}

	private void OnDisable() // enabled=false on assign (CharacterRoutines)
	{
		if (selfCharacter)
		{
			if (debugMode>0) Debug.Log(this + " OnDisable");
			selfCharacter.Routines.onStarted -= OnNewRoutineStarted;
			StopAllCoroutines();
		}
	}

	private void OnNewRoutineStarted()
	{
		if (debugMode>0) Debug.Log(this + " OnNewRoutineStarted, disable");
		enabled = false;
	}

	IEnumerator FollowCoroutine()
	{
		yield return null;

		dummy.position = transform.position;
		
		while (true)
		{
			if (tickTime > 0) 
				yield return new WaitForSeconds(tickTime);
			else 
				yield return null;

			if (!leaderCharacter || leaderCharacter.IsDead() || selfCharacter.IsDead() ||
			    !GameInput.Instance.IsLeaderMode())
			{
				yield break;
			}
			
			if (GameTurnManager.Instance.IsTurnBasedMode())
			{
				yield break;
			}
			
			headingPos = leaderCharacter.GetPosition();
			headingPos -= leaderCharacter.GetDirection() * headingDistance;
			
			agent.SetObstacle(false);

			distanceToDummy = selfCharacter.GetDistanceTo(dummy.position);
			distanceToLeader = selfCharacter.GetDistanceTo(leaderCharacter.GetPosition());

			if (distanceToLeader <= avoidDistance)
			{
				headingPos = selfCharacter.GetAvoidPosition(leaderCharacter.GetPosition(), avoidDistance);
			}
			
			if (distanceToDummy <= Mathf.Min(headingDistance, followDistance))
			{
				agent.StopMovement();
			}
			else
			{
				agent.AssignFollowTransform(dummy, followDistance);
				agent.StartMove();
			}
			
			if (debugMode > 0)
			{
				DebugDrawer.Line(selfCharacter.GetCenterPosition(), leaderCharacter.GetCenterPosition(), Utils.ColorAlpha(Color.black, 0.3f), tickTime);
				DebugDrawer.Line(selfCharacter.GetCenterPosition(), headingPos, Color.softBlue, tickTime);
				DebugDrawer.WireDisc(headingPos, followDistance, Color.softBlue, tickTime);
			}
		}
	}

	private void LateUpdate()
	{
		if (dummy)
		{
			dummy.SetParent(null, true);
			
			dummy.position = headingSpeed <= 0 ? headingPos :
				Vector3.MoveTowards(dummy.position, headingPos, Time.deltaTime * headingSpeed * selfCharacter.GetAgent().GetMaxSpeed());
		
			if (debugMode > 0)
			{
				DebugDrawer.WireDisc(headingPos, followDistance, Color.softBlue, 0);
				DebugDrawer.WireDisc(dummy.position, followDistance, Color.aquamarine, 0);
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!Utils.IsEditorPrefabStage() || Application.isPlaying) 
			return;
		
		Gizmos.color = Color.softBlue;
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * headingDistance);
		Utils.DrawGizmosCircle(transform.position + transform.forward * headingDistance, Vector3.up, followDistance);
	}
}