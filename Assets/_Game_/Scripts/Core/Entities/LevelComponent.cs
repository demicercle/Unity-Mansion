using System.Collections.Generic;
using UnityEngine;


public class LevelComponent : MonoBehaviour
{
	[SerializeField] private int enemySpreadHP;
	[SerializeField][Min(1)] private int hpMaxSpread;
	
	public void Init(CharacterEntity[] levelCharacters)
	{
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