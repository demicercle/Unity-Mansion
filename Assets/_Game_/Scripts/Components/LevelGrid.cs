using System;
using System.Linq;
using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class LevelGrid : MonoBehaviour
{
	[SerializeField] private Color cellFillColor;
	[SerializeField] private Color cellLineColor;
	[SerializeField] private Vector2Int extends;
	[SerializeField] private float cellSize = 2.5f;
	[SerializeField] private float nodeSize = .5f;
	[SerializeField] private Vector3 offset = Vector2.one * 2.5f / 2f;

	private Grid grid;

	[ContextMenu("Update AStar")]
	void UpdateAStar()
	{
		var astar = AstarPath.active;
		if (!astar)
		{
			Debug.LogError(this + " AstarPath not found");
			return;
		}
		
		foreach (var graph in astar.graphs)
		{
			if (graph is GridGraph gridGraph)
			{
			//	gridGraph.center = offset;
			var gridDim = new Vector2(extends.x, extends.y) * 2;
			if(nodeSize >0) gridDim *= cellSize / nodeSize;
				gridGraph.SetDimensions((int)gridDim.x , (int)gridDim.y, nodeSize > 0f ?  nodeSize : gridGraph.nodeSize);
				Utils.SetDirty(graph.active);
			}
		}
	}

	[ContextMenu("Update Camera Bounds")]
	void UpdateCameraBounds()
	{
		var cameraPivot = FindObjectsByType<CameraPivot>(default).FirstOrDefault();
		if (cameraPivot != null)
		{ 
			cameraPivot.SetWorldBounds(new Bounds(default, new Vector3(extends.x * 2 * cellSize, cameraPivot.GetWorldBounds().size.y, extends.y * 2 * cellSize)));
			Utils.SetDirty(cameraPivot);
		}
	}

	private void OnDrawGizmos()
	{
		if (!enabled) return;
		if (!grid) grid = GetComponent<Grid>();
		grid.cellSize = new Vector3(cellSize,0,cellSize);
		for (int x = -extends.x; x < extends.x; x++)
		{
			for (int y = -extends.y; y < extends.y; y++)
			{
				var cell = new Vector3Int(x, 0, y);
				var world = grid.CellToWorld(cell) + offset;
				Gizmos.color = cellFillColor;
				Gizmos.DrawCube(world, grid.cellSize);
				Gizmos.color = cellLineColor;
				Gizmos.DrawWireCube(world, grid.cellSize);
			}
		}
	}

	private void Awake()
	{
		grid = GetComponent<Grid>();
	}
}