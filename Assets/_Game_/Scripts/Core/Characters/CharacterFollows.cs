using System;
using UnityEngine;
using System.Collections;

public class CharacterFollows : MonoBehaviour
{
	private const float MinVelocityToLook = 0.5f;
	
	public static int debugMode { get; set; }
	
	[SerializeField] [Range(0.5f, 12.0f)] private float followDistance = 3.0f;
	[SerializeField] [Range(0.1f, 10f)] private float tickTime = 1.0f;
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
			agent.AssignFollowTransform(null);
			agent.updateRotation = true;
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
			headingPos = leaderCharacter.GetPosition();
			headingPos += leaderCharacter.GetDirectionTo(selfCharacter.GetPosition()) * headingDistance;
			
			agent.AssignFollowTransform(dummy, followDistance);
			agent.SetObstacle(!agent.IsMoving());
			
			if (debugMode > 0)
			{
				DebugDrawer.Line(selfCharacter.GetCenterPosition(), leaderCharacter.GetCenterPosition(), Utils.ColorAlpha(Color.black, 0.3f), tickTime);
				DebugDrawer.Line(selfCharacter.GetCenterPosition(), headingPos, Color.softBlue, tickTime);
				DebugDrawer.WireDisc(headingPos, followDistance, Color.softBlue, tickTime);
			}

			if (tickTime > 0) 
				yield return new WaitForSeconds(tickTime);
			else 
				yield return null;
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

			if (leaderCharacter && agent.velocity.magnitude <= MinVelocityToLook)
			{
				agent.updateRotation = false;
				//selfCharacter.HeadLookAt(leaderCharacter.GetEyesPosition(), Time.deltaTime);
			}
			else
			{
				agent.updateRotation = true;
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
		if (!Utils.IsEditorPrefabStage()) 
			return;
		
		Gizmos.color = Color.softBlue;
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * headingDistance);
		Utils.DrawGizmosCircle(transform.position + transform.forward * headingDistance, Vector3.up, followDistance);
	}
}