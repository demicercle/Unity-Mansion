using System;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(fileName = "LevelData", menuName = "LevelData", order = 0)]
public class LevelData : ScriptableObject
{
	public static LevelData[] GetAllLevels() => Resources.LoadAll<LevelData>(Consts.ResPath_Levels).OrderBy(l => l.levelIndex).ToArray();

	public static LevelData GetLevel(int index)
	{
		return GetAllLevels().FirstOrDefault(l => l.levelIndex == index);
	}
	
	public bool IsName(string n) => name == n;
	public string GetName() => name;

	public int levelIndex;
	public string[] scenesToLoad = Array.Empty<string>();
}