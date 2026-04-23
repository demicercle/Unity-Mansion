using System;
using UnityEngine;
using System.Collections;

public class CharacterFollows : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	[SerializeField] [Range(0.5f, 12.0f)] private float followDistance = 1.5f;
	[SerializeField] [Range(0.1f, 10f)] private float tickTime = 1.0f;

	public void SetLeader(CharacterController newLeader)
	{
		leaderCharacter = newLeader;
	}

	private CharacterController selfCharacter;
	private CharacterController leaderCharacter;
	private CharacterAgent agent;

	private void Awake()
	{
		enabled = false;
	}

	private void OnEnable()
	{
		if (debugMode>0) Debug.Log(this + " OnEnable");
		if (!selfCharacter)
		{
			selfCharacter = GetComponent<CharacterController>();
			if (!selfCharacter) Debug.LogError(this + " no character");
			agent = selfCharacter.GetAgent();
		}
		selfCharacter.Routines.OnStarted += OnNewRoutineStarted;
		StartCoroutine(FollowCoroutine());
	}

	private void OnDisable() // enabled=false on assign (CharacterRoutines)
	{
		if (selfCharacter)
		{
			if (debugMode>0) Debug.Log(this + " OnDisable");
			selfCharacter.Routines.OnStarted -= OnNewRoutineStarted;
			StopAllCoroutines();
			agent.AssignFollowTransform(null);
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
		
		while (leaderCharacter && !leaderCharacter.IsDead() && !selfCharacter.IsDead() && GameInput.Instance.IsLeaderMode())
		{
			agent.AssignFollowTransform(leaderCharacter.transform, followDistance);
			agent.SetObstacle(!agent.IsMoving());
			
			if (debugMode > 0)
			{
				DebugDrawer.Line(selfCharacter.GetCenterPosition(), leaderCharacter.GetCenterPosition(), Color.softBlue, tickTime);
				DebugDrawer.WireDisc(leaderCharacter.GetPosition() + Vector3.up * 0.1f, leaderCharacter.GetDirection(), followDistance, Color.softBlue, tickTime);
			}
			
			yield return new WaitForSeconds(tickTime);
		}

		enabled = false;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.softBlue;
		Utils.DrawGizmosCircle(transform.position, Vector3.up, followDistance);
	}
}