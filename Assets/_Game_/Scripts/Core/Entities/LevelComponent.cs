using System.Collections.Generic;
using UnityEngine;


public class LevelComponent : MonoBehaviour
{
	[Header("Level Settings")]
	[SerializeField] private AstarPath aStar;
	[SerializeField] private float worldHeight = 12f;
	
	[Header("Balancing")]
	[SerializeField] private int enemySpreadHP;
	[SerializeField][Min(1)] private int hpMaxSpread;

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