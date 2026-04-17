using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Projector))]
public class FogProjector : MonoBehaviour
{
	private bool changed;
	[SerializeField] private Color visibleColor = Color.clear;
	[SerializeField] private Color exploredColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
	[SerializeField] private Color hiddenColor = Color.black;
	
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

	public int gridSize;
	public int textureSize;
	
	private Grid grid;
//orthoCamera;
	[SerializeField] private Vector3 cellSize;
	[SerializeField] private float refreshTime;
	[SerializeField] private LayerMask obstructionLayer;
	[SerializeField] private string textureParameter = "_MainTex";
	private List<Vector3Int> visibleCells  = new List<Vector3Int>();
	private List<Vector3Int> exploredCells = new List<Vector3Int>();
	private Texture2D texture;
	private Projector projector;

	private void Awake()
	{
		projector = GetComponent<Projector>();
	//	orthoCamera = GetComponent<Camera>();
	//	if(!orthoCamera) orthoCamera = gameObject.AddComponent<Camera>();
	//	orthoCamera.enabled = false;
	//	orthoCamera.orthographic = true;
		grid = new GameObject("FogGrid").AddComponent<Grid>();
		grid.cellSize = cellSize;
	}

	private void OnDestroy()
	{
		Destroy(grid.gameObject);
	}

	private void OnEnable()
	{
		CreateTexture();
		projector.enabled = true;
		StartCoroutine("DoUpdate");
	}

	private void OnDisable()
	{
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
		projector.material.SetTexture(textureParameter, texture);
		Debug.Log(this + " newTexture size=" + textureSize);
	}

	void SetCellVisible(Vector3Int cell, bool value)
	{
		if (value)
		{
			if(!exploredCells.Contains(cell)) exploredCells.Add(cell);
			if(!visibleCells.Contains(cell)) visibleCells.Add(cell);
		}
		else
		{
			visibleCells.Remove(cell);
		}
	}

	[SerializeField] private float minRange;
	private IEnumerator DoUpdate()
	{
		changed = true;
		
		while (enabled)
		{
			visibleCells.Clear();
			
			foreach (var character in CharacterController.instances)
			{
				if (character.IsDead()) continue;
				if (!character.InParty()) continue;

				var rot = transform.rotation;
				var world = character.GetEyesPosition();
				var range = character.GetVisionRange();
				
				world.y = 0;
				var cell = grid.WorldToCell(world);
				//DebugGUI.WireDBox(cell, rot,cellSize,Color.cyan,refreshTime);
				
				var delta = Mathf.CeilToInt(range);

				for (int dx = -delta; dx < delta; dx++)
				{
					for (int dz = -delta; dz < delta; dz++)
					{
						var otherCell = new Vector3Int(cell.x + dx, 0, cell.z + dz);
						if (visibleCells.Contains(otherCell)) continue;
						
						var otherWorld = grid.CellToWorld(otherCell)
						                 + Vector3.up * grid.cellSize.y / 2f;
					
						otherWorld.y = 0;
						var distance = Vector3.Distance(world, otherWorld);
						if (distance <= range)
						{
							if (distance <= minRange || !Physics.Linecast(world, otherWorld, obstructionLayer))
							{
								visibleCells.Add(otherCell);
								if(!exploredCells.Contains(otherCell)) exploredCells.Add(otherCell);
							}
						}
					}
				}
			}
			
			for (int x = 0; x < gridSize; x++)
			{
				for (int y = 0; y < gridSize; y++)
				{
					var cell = new Vector3Int(x - gridSize / 2, 0, y - gridSize / 2);
					var world =  grid.CellToWorld(cell);
					if (visibleCells.Contains(cell))
					{
						DebugGUI.WireDBox(world, transform.rotation, cellSize, Color.white, refreshTime);
						texture.SetPixel(x,y,Color.white);
					}
					else if (exploredCells.Contains(cell))
					{
						DebugGUI.WireDBox(world, transform.rotation, cellSize, Color.gray, refreshTime);
						texture.SetPixel(x,y,Color.gray);
					}
					else
					{
						DebugGUI.WireDBox(world, transform.rotation, cellSize, Color.black, refreshTime);
						texture.SetPixel(x, y, Color.black);
					}
				}
			}
					
	//		Debug.Log(this + " Apply() " + visibleCells.Count + " / " + exploredCells.Count + " " + aspect);
			texture.Apply();
			
			if(refreshTime <= 0) yield return new WaitForEndOfFrame();
			else yield return new WaitForSeconds(refreshTime);
		}
	}

	[SerializeField] private Vector2 screenOffset = default;
	[SerializeField] float scaleSize = 1.0f;
}