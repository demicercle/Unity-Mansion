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
		enabled = true;
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
		dummy.gameObject.hideFlags = HideFlags.HideInHierarchy;
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
	}

	private void OnDisable() // enabled=false on assign (CharacterRoutines)
	{
		if (debugMode>0) Debug.Log(this + " OnDisable ");
		if (selfCharacter)
		{
			selfCharacter.Routines.onStarted -= OnNewRoutineStarted;
		}
	}

	private void OnNewRoutineStarted()
	{
		if (debugMode>0) Debug.Log(this + " OnNewRoutineStarted, disable");
		enabled = false;
	}

	private void LateUpdate()
	{
		if (dummy)
		{
			dummy.SetParent(null, true);
			
			headingPos = leaderCharacter.GetPosition();
			
			headingPos -= leaderCharacter.GetDirection() * (leaderCharacter.IsMoving() ? 1.0f : 0.0f) * headingDistance / Vector3.Distance(selfCharacter.GetPosition(), leaderCharacter.GetPosition());

			NavUtils.TryGetNearestNavPosition(headingPos, agent.GetRadius(), out headingPos);
			
			dummy.position = headingSpeed <= 0 ? headingPos : Vector3.MoveTowards(dummy.position, headingPos, Time.deltaTime * headingSpeed * selfCharacter.GetAgent().GetMaxSpeed());
		
			distanceToDummy = selfCharacter.GetDistanceTo(dummy.position);
			
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