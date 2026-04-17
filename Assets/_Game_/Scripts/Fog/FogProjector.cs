using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Projector))]
public class FogProjector : MonoBehaviour
{
	void OnMoved(FogVision vision)
	{
		changed = true;
	}

	void OnToggled(FogVision vision, bool add)
	{
		if(add) visions.Add(vision); else visions.Remove(vision);
	}

	private bool changed;
	private List<FogVision> visions = new List<FogVision>();
	private Color visibleColor = Color.clear;
	private Color exploredColor = new Color(0.2f, 0.2f, 0.2f, 0.2f);
	private Color hiddenColor = Color.black;
	
	static private FogProjector instance;

	static public FogProjector GetInstance()
	{
		if (instance == null) instance = FindFirstObjectByType<FogProjector>(default);
		return instance;
	}

	public Vector3Int WorldToCell(Vector3 worldPosition)
	{
		return grid.WorldToCell(Vector3.Scale(new Vector3(1, 0, 1), worldPosition));
	}

	public bool IsPositionVisible(Vector3 position)
	{
		return visibleCells.Contains(WorldToCell(position));
	}

	public bool IsPositionExplored(Vector3 position)
	{
		return exploredCells.Contains(WorldToCell(position));
	}

	public void Clear()
	{
		visibleCells.Clear();
		exploredCells.Clear();
		changed = true;
	}

	public bool IsCellExplored(Vector3Int cell) => exploredCells.Contains(cell);
	public bool IsCellVisible(Vector3Int cell) => visibleCells.Contains(cell);
	public bool CellExists(Vector3Int cell)
	{
		var tx = cell.x + textureSize / 2;
		var ty = cell.y + textureSize / 2;
		return tx >= 0 && tx < textureSize && ty >= 0 && ty < textureSize;
	}

	public int textureSize;
	
	private Grid grid;
	private Camera orthoCamera;
	[SerializeField] private Vector3 cellSize;
	[SerializeField] private float refreshTime;
	[SerializeField] private LayerMask obstructionLayer;
	private List<Vector3Int> visibleCells  = new List<Vector3Int>();
	private List<Vector3Int> exploredCells = new List<Vector3Int>();
	private Texture2D texture;
	private Projector projector;

	private void Awake()
	{
		projector = GetComponent<Projector>();
		orthoCamera = GetComponent<Camera>();
		if(!orthoCamera) orthoCamera = gameObject.AddComponent<Camera>();
		orthoCamera.enabled = false;
		orthoCamera.orthographic = true;
		grid = GetComponent<Grid>();
		if(!grid) grid = gameObject.AddComponent<Grid>();
		grid.cellSize = cellSize;
	}

	private void OnDestroy()
	{
		Destroy(grid.gameObject);
	}

	private void OnEnable()
	{
		FogVision.onMoved += OnMoved;
		FogVision.onToggled += OnToggled;
		
		CreateTexture();
		projector.enabled = true;
		StartCoroutine("DoUpdate");
	}

	private void OnDisable()
	{
		FogVision.onMoved -= OnMoved;
		FogVision.onToggled -= OnToggled;
		
		DisposeTexture();
		projector.enabled = false;
		StopCoroutine("DoUpdate");
	}

	private void DisposeTexture()
	{
		if (texture) Destroy(texture);
	}

	private void CreateTexture()
	{
		texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
		for (int x = 0; x < textureSize; x++)
		{
			for (int y = 0; y < textureSize; y++)
			{
				texture.SetPixel(x,y,Color.black);
			}
		}
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.Apply();
		projector.material.SetTexture("_MainTex", texture);
		Debug.Log(this + " newTexture size=" + textureSize);
	}

	void SetCellVisible(Vector3Int cell, bool value)
	{
		if(value)
			visibleCells.Add(cell);
		else
		{
			visibleCells.Remove(cell);
			if(!exploredCells.Contains(cell))
				exploredCells.Add(cell);
		}
	}

	private IEnumerator DoUpdate()
	{
		changed = true;
		
		while (enabled)
		{
			if (changed)
			{
				Debug.Log(this + " changed");
				changed = false;
				
				foreach (var vision in visions)
				{
					var world = vision.GetEyesWorldPosition();
				
					var cell = grid.WorldToCell(world);
					cell.x += textureSize / 2;
					cell.z += textureSize / 2;
				
					var range = vision.GetRange();
					var delta = Mathf.CeilToInt(range);

					for (int d = -delta; d < delta; d++)
					{
						var otherCell = new Vector3Int(cell.x + d, 0, cell.z+ d);
						
						var otherWorld = grid.CellToWorld(otherCell)
						                 + Vector3.up * grid.cellSize.y / 2f;

						var distance = Vector3.Distance(world, otherWorld);
						if (distance <= range)
						{
							if (!Physics.Linecast(world, otherWorld, obstructionLayer))
							{
								SetCellVisible(otherCell, true);
								continue;
							}
						}
							
						SetCellVisible(otherCell, false);
						
						yield return new WaitForEndOfFrame();
					}

					for (int x = 0; x < textureSize; x++)
					{
						for (int y = 0; y < textureSize; y++)
						{
							var screenPos = new Vector2(x, y);
							var worldPos = orthoCamera.ScreenToWorldPoint(screenPos);
							//var texCell = new Vector3Int(x + textureSize/2, 0, y + textureSize/2);
							if(IsPositionVisible(worldPos)) texture.SetPixel(x,y, visibleColor);
							else if(IsPositionExplored(worldPos)) texture.SetPixel(x,y, exploredColor);
							else texture.SetPixel(x,y, hiddenColor);
						}
					}
				}
			}
			
			if(refreshTime <= 0) yield return new WaitForEndOfFrame();
			else yield return new WaitForSeconds(refreshTime);
		}
	}
}