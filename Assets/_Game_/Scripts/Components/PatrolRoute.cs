using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PatrolRoute : MonoBehaviour
{
	static public List<PatrolRoute> routes = new List<PatrolRoute>();
	void Awake() { routes.Add(this);}
	void OnDestroy() { routes.Remove(this); }

	static public bool FindRoute(CharacterController character, out PatrolRoute patrolRoute)
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

	public CharacterController assignedCharacter;
	public List<Waypoint> waypoints = new List<Waypoint>();
	public bool closedLoop = false;
	public Color color = Color.blue;
	public AIBrain_Patrol defaultBrain = null;
		
	public Vector3[] GetPath() => waypoints
			.Where(wp => wp != null)
			.ToList()
			.ConvertAll<Vector3>(wp => wp.GetPosition()).ToArray();

		public void DebugDraw(float duration = 0f)
		{
			DebugGUI.Path(GetPath(), closedLoop, color, duration);
		}

		private void Start() // TODO init patrol in GameData
		{
			if(assignedCharacter == null)
				assignedCharacter = GetComponentInParent<CharacterController>();
			
			transform.SetParent(null, true); // to prevent waypoints to move in game
			
			if (assignedCharacter == null)
			{
				Debug.LogWarning(this + " has no assigned character");
				gameObject.SetActive(false);
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = color;
			Utils.DrawGizmosPath(GetPath(), closedLoop);
			if (assignedCharacter)
			{
				Gizmos.DrawLine(transform.position, assignedCharacter.GetPosition() + Vector3.up);
				Utils.DrawGizmosCircle(assignedCharacter.GetPosition() + Vector3.up, Vector3.up, 1.0f);
			}
		}
		
		
}