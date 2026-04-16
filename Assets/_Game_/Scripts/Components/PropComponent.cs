using System;
using System.Collections.Generic;
using UnityEngine;

public class PropComponent : MonoBehaviour
{
	public Waypoint[] GetWaypoints() => GetComponentsInChildren<Waypoint>();

	public bool GetBestWaypoint(Vector3 worldPos, out Waypoint bestWaypoint)
	{
		return Waypoint.GetBestWaypointFromPosition(worldPos, out bestWaypoint, GetWaypoints());
	}

	private void Start()
	{
		gameObject.layer = LayerMask.NameToLayer(GameManager.LAYER_CHARACTER);
	}
}