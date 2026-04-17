/*using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "GameData", order = 0)]
public class GameData : ScriptableObject
{
	public System.Action<int> newTurnStarted;
	public System.Action<CharacterData[]> partyChanged;

	[SerializeField] private List<LevelData> allLevels;
	
	[Header("ReadOnly")]
	[ReadOnly] [SerializeField] private int currentLevelIndex;
	[ReadOnly] [SerializeField] private int currentTurn;
	[ReadOnly] [SerializeField] private bool playerTurn; // TODO: "currentRound" (Players, Enemies, Neutral)
	[ReadOnly] [SerializeField] private List<CharacterData> allCharacters = new List<CharacterData>();
	[ReadOnly] [SerializeField] private List<CharacterData> partyCharacters = new List<CharacterData>();
	
	public CharacterData[] GetAllCharacters() => allCharacters.ToArray();

	public void AddCharacter(CharacterData character)
	{
		character.Init(allCharacters.Count);
		allCharacters.Add(character);
	}
	
	public CharacterData GetCharacterAt(int index) => allCharacters[index];
	
	public int GetCharacterIndex(CharacterData character) => allCharacters.IndexOf(character);
	
	public int GetCharacterPartyIndex(CharacterData character) => partyCharacters.IndexOf(character);

	public void SetCharacterInParty(CharacterData character, int partyIndex)
	{
		if (partyIndex >= 0)
		{
			partyCharacters.Insert(partyIndex, character);
			character.SetPartyIndex(partyIndex);
		}
		else
		{
			partyCharacters.Remove(character);
			character.SetPartyIndex(-1);
		}
		
		partyChanged?.Invoke(partyCharacters.ToArray());
	}
	
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
		ClearCharacters(false);
		currentLevelIndex = levelIndex;
	}

	public void ClearCharacters(bool clearParty)
	{
		foreach (var otherCharacter in allCharacters.ToArray())
		{
			if(clearParty || !partyCharacters.Contains(otherCharacter))
				allCharacters.Remove(otherCharacter);
		}
	}
	
	public LevelData GetCurrentLevel() => allLevels[currentLevelIndex];
	public int GetCurrentLevelIndex() => currentLevelIndex;

	public void Init()
	{
		ClearCharacters(true);
		
		currentTurn = -1;

		if (allLevels.Count == 0)
		{
			Debug.LogWarning(this + " has no levels, add one");
			allLevels.Add(CreateInstance<LevelData>());
		}
	}
}*/