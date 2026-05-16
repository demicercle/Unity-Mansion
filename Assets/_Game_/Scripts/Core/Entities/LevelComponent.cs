using System.Collections.Generic;
using UnityEngine;


public class LevelComponent : MonoBehaviour
{	
	[Header("Level Settings")] 
	[SerializeField] private AstarPath aStar;
	[SerializeField] private float worldHeight = 12f;
	[SerializeField] private float boundsExtraSize = 20.0f;

	[SerializeField]
	private FogCube[] worldBoundsFogCubes = new FogCube[8] { null, null, null, null, null, null, null, null };
	
	[Header("Balancing")]
	[SerializeField] private int enemySpreadHP;
	[SerializeField][Min(1)] private int hpMaxSpread;

	private void OnDrawGizmosSelected()
	{
		if (!Application.isPlaying)
		{
			UpdateBounds();
		}
	}

	[ContextMenu("Update Bounds")]
	private void UpdateBounds()
	{
		var cameraPivot = CameraPivot.Instance;
		if (!cameraPivot) cameraPivot = Utils.FindFirstObject<CameraPivot>();
		if (!cameraPivot)
		{
			Debug.LogError(this + " cannot find camera pivot to update bounds");
			return;
		}
		
		var bounds = default(Bounds);
		for(int i = 0; i < aStar.graphs.Length; i++)
		{
			if (i == 0) 
				bounds = aStar.graphs[i].bounds;
			else 
				bounds.Encapsulate(aStar.graphs[i].bounds);
		}

		bounds.size = new Vector3(bounds.size.x, worldHeight, bounds.size.z);
		bounds.center = new Vector3(bounds.center.x, worldHeight / 2f, bounds.center.z);
		cameraPivot.SetWorldBounds(bounds);

		for (int i = 0; i < worldBoundsFogCubes.Length; i++) // Left, Left Fwd, Fwd, Right Fwd, Right, Right Back, Back, Left Back
		{
			var cube =  worldBoundsFogCubes[i];
			if (!cube) continue;
			var center = bounds.center;
			var extends = bounds.extents;
			if (i == 0) // Left
			{
				cube.transform.position = center - extends;
				cube.transform.localScale = new Vector3(boundsExtraSize, cube.transform.localScale.y, extends.z * 4);
			}
			else if (i == 1) // Left Fwd
			{
				cube.transform.position = center - extends + Vector3.forward * extends.z * 2;
				cube.transform.localScale = new Vector3(boundsExtraSize, cube.transform.localScale.y, boundsExtraSize);
			}
			else if (i == 2) // Fwd
			{
				cube.transform.position = center + extends;
				cube.transform.localScale = new Vector3(extends.x * 4, cube.transform.localScale.y, boundsExtraSize);
			}
			else if (i == 3) // Right Fwd
			{
				cube.transform.position = center + extends;
				cube.transform.localScale = new Vector3(boundsExtraSize, cube.transform.localScale.y, boundsExtraSize);
			}
			else if (i == 4) // Right
			{
				cube.transform.position = center + extends;
				cube.transform.localScale = new Vector3(boundsExtraSize, cube.transform.localScale.y, extends.z * 4);
			}
			else if (i == 5) // Right Back
			{
				cube.transform.position = center + extends - Vector3.forward * extends.z * 2;
				cube.transform.localScale = new Vector3(boundsExtraSize, cube.transform.localScale.y, boundsExtraSize);
			}
			else if (i == 6) //  Back
			{
				cube.transform.position = center - extends;
				cube.transform.localScale = new Vector3(extends.x * 4, cube.transform.localScale.y, boundsExtraSize);
			}
			else if (i == 7) // Left Back
			{
				cube.transform.position = center - extends;
				cube.transform.localScale = new Vector3(boundsExtraSize, cube.transform.localScale.y, boundsExtraSize);
			}

			cube.transform.position = new Vector3(cube.transform.position.x, center.y - extends.y, cube.transform.position.z);
			cube.transform.localScale = new Vector3(cube.transform.localScale.x, worldHeight * 2, cube.transform.localScale.z);
		}
		
		if(Application.isPlaying)
			FogCube.UpdateAll();
	}
	
	public void Init(CharacterEntity[] levelCharacters)
	{
		UpdateBounds();
		
		if (enemySpreadHP > 0)
		{
			var rng = new System.Random(GameSave.Current.gameSeed);

			var enemies = new List<CharacterEntity>();
			foreach (var enemyCharacter in levelCharacters)
			{
				if (enemyCharacter.IsEnemyToPlayer())
				{
					for(int i = 0; i < hpMaxSpread; i++)
						enemies.Add(enemyCharacter);
				}
			}
			
			while (enemySpreadHP > 0 && enemies.Count > 0)
			{
				var index = rng.Next(0, enemies.Count);
				enemies[index].AddMaxHealth(1);
				enemySpreadHP--;
				enemies.RemoveAt(index);
			}
		}
	}
}