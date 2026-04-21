using System;
using System.Collections;
using UnityEngine;


public class NavUtility : MonoBehaviour
{
	public static bool IsRebuildingAsync() => instance != null;
	
	public static bool TryGetNearestNavPosition(Vector3 position, float maxDistance, out Vector3 navPosition)
	{
		navPosition = position;

		if (AstarPath.active == null)
			return false;

		// Crée une contrainte pour nœuds walkables uniquement
		Pathfinding.NNConstraint constraint = Pathfinding.NNConstraint.Walkable;
		constraint.constrainWalkability = true;
		constraint.walkable = true;

		// Cherche le nœud le plus proche
		Pathfinding.NNInfo nearest = AstarPath.active.GetNearest(position, constraint);

		if (nearest.node != null && nearest.node.Walkable)
		{
			float distance = Vector3.Distance(position, (Vector3)nearest.position);
			if (distance <= maxDistance)
			{
				navPosition = (Vector3)nearest.position;
				return true;
			}
		}

		return false;
	}
	
	
	static private NavUtility instance;
	static public void RebuildNavigationAsync(float delay=0f)
	{
		if(instance!=null)Destroy(instance.gameObject);
		instance = new GameObject("RebuildNavigationAsync").AddComponent<NavUtility>();
		instance.delay = delay;
	}

	private float delay;
	//private float progressValue;
	IEnumerator Start()
	{
		Debug.Log(this + " RebuildNavigationAsync Start delay=" + delay);
		if(delay >0f) yield return new WaitForSeconds(delay);
		foreach (var progress in AstarPath.active.ScanAsync()) {
			//progressValue = progress.progress;
			name = "RebuildNavigationAsync (" + progress.progress.ToString("N2") + ")";
//			Debug.Log("Scanning... " + progress.ToString());
			yield return null;
		}
		Destroy(gameObject);
	}

	/*private void OnGUI()
	{
		if (!Debug.isDebugBuild) return;
		GUILayout.Label(this + " rebuild navigation async: " + progressValue.ToString("N2"), new GUIStyle("textfield"));
	}*/
}