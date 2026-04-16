using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "GameData", order = 0)]
public class GameData : ScriptableObject
{
	public System.Action<CharacterData, int> characterAddedToParty;
	public System.Action<int> newTurnStarted;
	public System.Action<CharacterData> runAction;
	public System.Action<CharacterData> actionUsed;

	public void AddCharacter(CharacterData characterData)
	{
		Debug.Log("Adding character " + characterData + " to " + allCharacters.Count);
		if (allCharacters.Contains(characterData)) Debug.LogError(characterData + " is already added");
		allCharacters.Add(characterData);
	}
	
	public void AddCharacterToParty(CharacterData characterData)
	{
		if (!partyCharacters.Contains(characterData))
		{
			partyCharacters.Add(characterData);
			characterData.SetPartyIndex(partyCharacters.Count - 1);
			characterAddedToParty?.Invoke(characterData, partyCharacters.Count - 1);
		}
	}

	public void  RemoveCharacterFromParty(CharacterData characterData)
	{
		if (partyCharacters.Contains(characterData))
		{
			partyCharacters.Remove(characterData);
			characterData.SetPartyIndex(-1);
			characterAddedToParty?.Invoke(characterData, -1);
		}
	}
	
	public int GetCurrentTurn() => currentTurn;

	public void NextTurn()
	{
		currentTurn++;
		allCharacters.ForEach(c => c.TurnBegins(currentTurn));
		newTurnStarted?.Invoke(currentTurn);
	}

	private int currentTurn;
	private List<CharacterData> allCharacters = new List<CharacterData>();
	private List<CharacterData> partyCharacters = new List<CharacterData>();

	public void Init(GameView gameView)
	{
		partyCharacters = new List<CharacterData>();
		allCharacters = new List<CharacterData>();
		currentTurn = 0;
	}

	public void Dispose()
	{
		
	}
}