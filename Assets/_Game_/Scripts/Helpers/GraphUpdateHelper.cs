using System;
using Pathfinding;
using UnityEngine;

[RequireComponent (typeof (GraphUpdateScene))]
public class GraphUpdateHelper : MonoBehaviour
{
	public Vector3 size = new Vector3(0.6f, 0f, 0.2f);

	public void SetObstacle(bool value)
	{
		var graphUpdate = GetComponent<GraphUpdateScene>();
		graphUpdate.modifyWalkability = value;
	}
	
	#if UNITY_EDITOR
	[ContextMenu("Apply")]
	private void Apply()
	{
		UnityEditor.Undo.RecordObject(this, "ApplyGraphUpdate");
		var graphUpdate = GetComponent<GraphUpdateScene>();
		graphUpdate.points = new Vector3[]
		{
			Vector3.Scale(size, new Vector3(1, 0, 1)),
			Vector3.Scale(size, new Vector3(-1, 0, 1)),
			Vector3.Scale(size, new Vector3(-1, 0, -1)),
			Vector3.Scale(size, new Vector3(1, 0, -1)),
		};
		UnityEditor.EditorUtility.SetDirty(graphUpdate);
	}
	#endif
}