using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;


public class Waypoint : MonoBehaviour
{
	private static List<Waypoint> waypoints = new List<Waypoint>();
	
	public Vector3 GetPosition() => transform.position;
	public int GetRotation() => (int)transform.eulerAngles.y;
	public Vector3 GetDirection() => transform.forward;

	public static Waypoint[] GetOrderedWaypointsFrom(Vector3 world, float heightPenalty = 2f)
	{
		return waypoints.OrderBy(wp => NavUtils.NavHDistance(wp.GetPosition(), world) + NavUtils.NavVDistance(wp.GetPosition(), world) * heightPenalty).ToArray();
	}
		
	[Range(0.1f, 10.0f)] public float radius = 0.4f;
	public bool ignoreRotation;
	public Color color = Color.cyan;

	[ContextMenu("Assign Wandering/True")]
	private void AssignWandering_True() => AssignWandering(true);
	[ContextMenu("Assign Wandering/False")]
	private void AssignWandering_False() => AssignWandering(false);
	private void AssignWandering(bool value)
	{
		var characters = Utils.FindObjects<CharacterEntity>();
		foreach (var character in characters)
		{
			if (!character.TryGetComponent(out CharacterBrain brain)) 
				continue;

			if (!value && brain.HasWanderingPoint(this))
			{
				Debug.Log(this + " Un-Assign to " + brain); 
				brain.AssignWanderingPoint(null);
				Utils.SetDirty(brain);
				return;
			}
			
			if (value && Vector3.Distance(character.GetPosition(), GetPosition()) <= radius * 2)
			{
				Debug.Log(this + " Assign to " + brain); 
				brain.AssignWanderingPoint(this);
				Utils.SetDirty(brain);
				return;
			}
		}
	}

	private void OnEnable()
	{
		waypoints.Add(this);
	}

	private void OnDisable()
	{
		waypoints.Remove(this);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		if(!ignoreRotation) Utils.DrawGizmosArrow(transform.position, transform.forward);
		Utils.DrawGizmosCircle(transform.position, transform.up, radius, 16);
	}

	public void DrawDebug(float d =0f)
	{
		if(!ignoreRotation)DebugDrawer.WireArrow(GetPosition(), GetDirection(), radius * Vector3.one, color, d);
		DebugDrawer.WireDisc(GetPosition(), GetDirection(), radius, color, d);
	}
}