using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "GameData", order = 0)]
public class GameData : ScriptableObject
{
	public System.Action partyChanged;
	public System.Action<int> newTurnStarted;
	
	/* TODO
	 
	 ViewEvents: (listened by views)		GameEvents:
	 - partyChanged							- PartyChanged()
	 - newTurnStarted						- TurnStarted(int)
	 - characterDamaged						- CharacterDamaged(id,amount)
	 - characterHealed						- CharacterHealed(id)
	 - characterKilled						- CharacterKilled(id)
	 - characterMoved						- CharacterMoved(id,newPos)
	 - characterUsedActions					- CharacterUsedActions(id,int)
	 
	 // check in "GameData"
	 -> GetCharacterByID(id):CharData
		-> charData.GetRemainingActions(id)
		-> charData.ConsumeActions(int)
		-> charData.GetRemainingMovePoints(id)
		-> charData.ConsumeMovePoints(value)
		-> charData.AddDamage(value)
		-> charData.Heal()
		-> charData.Kill()
		etc
	
	// notifies in GameController
	
	 
	 */

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
			partyChanged?.Invoke();
			characterData.SetPartyIndex(partyCharacters.Count - 1);
		}
	}

	public void  RemoveCharacterFromParty(CharacterData characterData)
	{
		if (partyCharacters.Contains(characterData))
		{
			partyCharacters.Remove(characterData);
			partyChanged?.Invoke();
			characterData.SetPartyIndex(-1);
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