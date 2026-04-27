using System;
using UnityEngine;
using System.Collections;

public class CharacterFollows : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	[SerializeField] [Range(0.5f, 12.0f)] private float followDistance = 3.0f;
	[SerializeField] [Range(0.1f, 10f)] private float tickTime = 1.0f;
	[SerializeField] [Range(-10f, 0f)] private float minHeading = 1.5f;
	[SerializeField] [Range(-10f, 0f)] private float maxHeading = 1.5f;
	[SerializeField] [Min(0)] private float headingSpeed = 1.5f;
	[SerializeField] [Range(0f, 1f)] private float headingScale = 0.5f;

	public void SetLeader(CharacterController newLeader)
	{
		leaderCharacter = newLeader;
	}

	private CharacterController selfCharacter;
	private CharacterController leaderCharacter;
	private CharacterAgent agent;
	private Transform dummy;
	private float heading;
	private Vector3 headingPos;

	private void Awake()
	{
		enabled = false;
		dummy = new GameObject("Follow Dummy (" + name + ")").transform;
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

		dummy.position = transform.position;
		
		while (leaderCharacter && !leaderCharacter.IsDead() && !selfCharacter.IsDead() && GameInput.Instance.IsLeaderMode())
		{
			heading = Mathf.Lerp(minHeading, maxHeading, CharacterController.PartyCount <= 0 ? 0f :
				selfCharacter.GetPartyIndex() / CharacterController.PartyCount);

			headingPos = leaderCharacter.transform.position
			             + leaderCharacter.transform.forward * headingScale * heading;
			
			agent.AssignFollowTransform(dummy, followDistance);
			agent.SetObstacle(!agent.IsMoving());
			
			if (debugMode > 0)
			{
				DebugDrawer.Line(selfCharacter.GetCenterPosition(), leaderCharacter.GetCenterPosition(), Color.softBlue, tickTime);
				DebugDrawer.Line(selfCharacter.GetCenterPosition(), headingPos, Color.softBlue, tickTime);
				DebugDrawer.WireDisc(headingPos, followDistance, Color.softBlue, tickTime);
			}
			
			yield return new WaitForSeconds(tickTime);
		}

		enabled = false;
	}

	private void LateUpdate()
	{
		if (dummy)
		{
			dummy.SetParent(null, true);
			
			dummy.position = headingSpeed <= 0 ? headingPos :
				Vector3.Lerp(dummy.position, headingPos, Time.deltaTime * headingSpeed);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!Utils.IsEditorPrefabStage()) 
			return;
		
		minHeading = Mathf.Min(minHeading, maxHeading);
		maxHeading = Mathf.Max(maxHeading, minHeading);
		
		Gizmos.color = Color.softBlue;
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * headingScale * minHeading);
		Utils.DrawGizmosCircle(transform.position + transform.forward * headingScale * minHeading, Vector3.up, followDistance);
		Gizmos.color = Color.deepSkyBlue;
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * headingScale * maxHeading);
		Utils.DrawGizmosCircle(transform.position + transform.forward * headingScale * maxHeading, Vector3.up, followDistance);
		
		if (Application.isPlaying && dummy)
		{
			DebugDrawer.WireDisc(headingPos, followDistance, Color.softBlue, tickTime);
			DebugDrawer.WireDisc(dummy.position, followDistance, Color.aquamarine, tickTime);
		}
	}
}