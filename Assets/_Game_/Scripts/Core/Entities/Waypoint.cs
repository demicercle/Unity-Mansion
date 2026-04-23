using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;


public class Waypoint : MonoBehaviour
{
	public Vector3 GetPosition() => transform.position;
	public int GetRotation() => (int)transform.eulerAngles.y;
	public Vector3 GetDirection() => transform.forward;
		
	[Range(0.1f, 1.0f)] public float radius = 0.4f;
	public Color color = Color.cyan;

	private void OnEnable()
	{
		//waypoints.Add(this);
	}

	private void OnDisable()
	{
		//waypoints.Remove(this);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		Utils.DrawGizmosArrow(transform.position, transform.forward);
		Utils.DrawGizmosCircle(transform.position, transform.up, radius, 16);
	}

	public void DrawDebug(float d =0f)
	{
		global::DebugDrawer.WireArrow(GetPosition(), GetDirection(), radius * Vector3.one, color, d);
		global::DebugDrawer.WireDisc(GetPosition(), GetDirection(), radius, color, d);
	}
}