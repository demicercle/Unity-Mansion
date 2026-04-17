using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "GameData", order = 0)]
public class GameData : ScriptableObject
{
	public System.Action<int> newTurnStarted;

	[SerializeField] private List<LevelData> allLevels;
	
	[Header("ReadOnly")]
	[SerializeField] private int currentLevel;
	[ReadOnly] [SerializeField] private int currentTurn;
	[ReadOnly] [SerializeField] private bool playerTurn; // TODO: "currentRound" (Players, Enemies, Neutral)
	
	public bool IsPlayerTurn() => playerTurn;
	public int GetCurrentTurn() => currentTurn;
	
	public void EndTurn()
	{
		Debug.Log(this + " EndTurn");
		playerTurn = false;
	}
	
	public void NewTurn()
	{
		playerTurn = true;
		currentTurn++;
		Debug.Log(this + " NewTurn " + currentTurn);
		newTurnStarted?.Invoke(currentTurn);
	}

	public int GetLevelIndex(LevelData levelData) => allLevels.IndexOf(levelData);
	
	public void SetCurrentLevel(int levelIndex)
	{
		currentLevel = levelIndex;
	}
	
	public LevelData GetCurrentLevel() => allLevels[currentLevel];
	

	public void Init()
	{
		currentTurn = -1;

		if (allLevels.Count == 0)
		{
			Debug.LogWarning(this + " has no levels, add one");
			allLevels.Add(CreateInstance<LevelData>());
		}
	}
}