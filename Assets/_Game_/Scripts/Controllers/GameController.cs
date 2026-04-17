using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class GameController : MonoBehaviour
{
	public CharacterController GetSelectedCharacter() => allCharacters.FirstOrDefault(c => c.IsSelected());

	public CharacterController[] GetPartyCharacters() => allCharacters.Where(c => c.InParty()).ToArray();

	public System.Action turnStarted, turnEnded, selectionChanged;
	public System.Action<CharacterController[]> partyChanged;
	
	[ReadOnly] [SerializeField] private List<CharacterController> allCharacters =  new List<CharacterController>();
	[ReadOnly] [SerializeField] private int currentTurn;
	[ReadOnly] [SerializeField] private bool playerTurn;

	public int GetSelectedCharacterIndex()
	{
		for (int i = 0; i < allCharacters.Count; i++)
		{
			if (allCharacters[i].IsSelected())
				return i;
		}

		return -1;
	}
	
	public void SelectNextCharacter()
	{
		Debug.Log(this + " SelectNextCharacter");
		var selectNext = false;
		for (int i = 0; i < allCharacters.Count; i++)
		{
			var character = allCharacters[i];
			if (character.IsSelected())
			{
				selectNext = true;
			}
			else if (selectNext && character.InParty() && !character.IsDead())
			{
				SelectCharacter(character);
			}
		}
		
		Debug.LogWarning(this + " Has no character to select next!");
	}

	public void SelectCharacter(CharacterController character)
	{
		Debug.Log(this + " SelectCharacter " + character);
		for (int i = 0; i < allCharacters.Count; i++)
		{
			allCharacters[i].SetSelected(character == allCharacters[i]);
		}
	}

	public void PartyChanged() => partyChanged?.Invoke(GetPartyCharacters());
	public void AddCharacter(CharacterController character) =>  allCharacters.Add(character);
	public CharacterController GetCharacterAt(int index) => allCharacters[index];
	
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

	public bool CanControlCharacter(CharacterController selectedCharacter)
	{
		return IsPlayerTurn() && selectedCharacter.InParty() && !selectedCharacter.IsDead();
	}

	public void SelectionChanged()
	{
		selectionChanged?.Invoke();
	}
}