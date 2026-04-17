using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class GameController : MonoBehaviour
{
	//public CharacterController GetSelectedCharacter() => allCharacters.FirstOrDefault(c => c.IsSelected());

	//public CharacterController[] GetPartyCharacters() => allCharacters.Where(c => c.InParty()).ToArray();

	/*public CharacterController GetPartyCharacter(int index)
	{
		var party = GetPartyCharacters();//TODO not optim!
		return index >= 0 && index < party.Length ? party[index] : null;
	}*/

	public System.Action turnStarted, turnEnded, selectionChanged;
	public System.Action<CharacterController[]> partyChanged;
	
	//[ReadOnly] [SerializeField] private List<CharacterController> allCharacters =  new List<CharacterController>();
	[ReadOnly] [SerializeField] private int currentTurn;
	[ReadOnly] [SerializeField] private bool playerTurn;

	/*public int FindSelectedCharacterIndex()
	{
		for (int i = 0; i < allCharacters.Count; i++)
		{
			if (allCharacters[i].IsSelected())
				return i;
		}
		
		return -1;
	}*/

//	public void PartyChanged() => partyChanged?.Invoke(GetPartyCharacters());

/*	public void AddCharacter(CharacterController character)
	{
		Debug.Log(this + " AddCharacter " + character + " at " + allCharacters.Count);
		if(allCharacters.Contains(character)) Debug.LogError(this + " character already added!");
		character.name += " (" + allCharacters.Count + ")";
		allCharacters.Add(character);
	}*/
//	public CharacterController GetCharacterAt(int index) => allCharacters[index];
	
	public int GetCurrentTurn() => currentTurn;
	public bool IsPlayerTurn() => playerTurn;
	
	public void NewTurn()
	{
		currentTurn += 1;
		playerTurn = true;
		Debug.Log(this + " NewTurn " + currentTurn);
		turnStarted?.Invoke();
	}

	public void EndTurn()
	{
		playerTurn = false;
		Debug.Log(this + " EndTurn " + currentTurn);
		turnEnded?.Invoke();
	}
}