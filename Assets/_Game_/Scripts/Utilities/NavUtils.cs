using System.Collections;
using UnityEngine;


public class NavUtility : MonoBehaviour
{
	
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
	static public void RebuildNavigationAsync(float delay)
	{
		_delay = delay;
		if(instance!=null)Destroy(instance.gameObject);
			instance = new GameObject("NavUtility").AddComponent<NavUtility>();
	}

	static float _delay;
	IEnumerator Start()
	{
		yield return new WaitForSeconds(_delay);
		
		Debug.Log(this + " rebuild navigation async");
		foreach (var progress in AstarPath.active.ScanAsync()) {
//			Debug.Log("Scanning... " + progress.ToString());
			yield return null;
		}

	//	Debug.Log(this + " complete");
		Destroy(gameObject);
	}
}