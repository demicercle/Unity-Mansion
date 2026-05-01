using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PatrolRoute : MonoBehaviour
{
	private static List<PatrolRoute> routes = new List<PatrolRoute>();
	void Awake() { routes.Add(this);}
	void OnDestroy() { routes.Remove(this); }

	public static bool FindRoute(CharacterEntity character, out PatrolRoute patrolRoute)
	{
		foreach (var route in routes)
		{
			if (route.assignedCharacter == character)
			{
				patrolRoute = route;
				return true;
			}
		}

		patrolRoute = null;
		return false;
	}

	[ContextMenu("Collect Waypoints")]
	public void CollectWaypoints()
	{
		waypoints = new List<Waypoint>(GetComponentsInChildren<Waypoint>());
		Utils.SetDirty(this);
	}

	[ContextMenu("Reset Position")]
	public void ResetPosition()
	{
		var wpChild = GetComponentsInChildren<Waypoint>().ToList();
		if (wpChild.Count <= 0) return;
		var newPosition = waypoints.FirstOrDefault().transform.position;
		waypoints.ForEach(wp => wp.transform.SetParent(null,true));
		transform.position = newPosition;
		waypoints.ForEach(wp => wp.transform.SetParent(transform, true));
		Utils.SetDirty(gameObject);
	}
	
	public Waypoint[] GetWaypoints() => waypoints.ToArray();
	
	public bool IsAssignedTo(CharacterEntity anyCharacter) => assignedCharacter == anyCharacter; 

	public void AssignPatrol(CharacterEntity newCharacter)
	{
		foreach (var otherPatrol in routes)
		{
			if (otherPatrol != this && otherPatrol.assignedCharacter == newCharacter)
			{
				otherPatrol.assignedCharacter = null;
			}
		}

		assignedCharacter = newCharacter;
	}

	[SerializeField] private CharacterEntity assignedCharacter;
	[SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();
	public bool closedLoop = false;
	public Color color = Color.blue;
		
	public Vector3[] GetPath() => waypoints
			.Where(wp => wp != null)
			.ToList()
			.ConvertAll<Vector3>(wp => wp.GetPosition()).ToArray();

		private void OnDrawGizmos()
		{
			Gizmos.color = color;
			Utils.DrawGizmosPath(GetPath(), closedLoop);
			if (assignedCharacter)
			{
				Gizmos.DrawLine(GetPath().FirstOrDefault(), assignedCharacter.GetPosition() + Vector3.up);
				Utils.DrawGizmosCircle(assignedCharacter.GetPosition() + Vector3.up, Vector3.up, 1.0f);
			}
		}
		
		
}