using Pathfinding;
using UnityEngine;

public class AstarOptimizer : MonoBehaviour
{
	[Range(1f, 50f)]
	public float maxFrameTimeMS = 10f; // Temps max par frame en ms

	void Update()
	{
		// Applique la nouvelle limite de temps
		AstarPath.active.maxFrameTime = maxFrameTimeMS;
	}
}