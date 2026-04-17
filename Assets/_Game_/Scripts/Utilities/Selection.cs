using UnityEngine;
using System.Linq;

public static class Selection
{
	static CharacterController selectedCharacter;

	static public event System.Action selectionChanged;
	
	static public CharacterController GetSelectedCharacter() => selectedCharacter;

	static public bool TryGetSelectedCharacter(out CharacterController character)
	{
		character = GetSelectedCharacter();
		return character != null;
	}

	static public bool IsCharacterSelected(CharacterController character) => selectedCharacter == character;

	static public void SelectCharacter(CharacterController character)
	{
		Debug.Log("SelectCharacter: " + character);
		
		if(selectedCharacter != null)
			selectedCharacter.NotifySelected(false);
		
		selectedCharacter = character;
		
		if(selectedCharacter != null)
			selectedCharacter.NotifySelected(true);
		
		selectionChanged?.Invoke();
	}

	static public void SelectPartyCharacter(int index)
	{
		var party = CharacterController.GetPartyCharacters().ToArray();
		if(index >= 0 && index < party.Length) SelectCharacter(party[index]);
	}
	
	static public int GetSelectedPartyCharacterIndex()
	{
		return selectedCharacter == null ? -1 : CharacterController.GetPartyCharacters().ToList().IndexOf(selectedCharacter);
	}
	
	static public void SelectNextCharacter()
	{
		var party = CharacterController.GetPartyCharacters().OrderBy(c => c.GetPartyIndex()).ToArray();
		var startIndex = GetSelectedPartyCharacterIndex();
		if (startIndex < 0)
		{
			Debug.Log(" Select First Character");
			SelectCharacter(party.FirstOrDefault());
			return;
		}
		
		Debug.Log(" SelectNextCharacter " + startIndex);
		var partyCount = party.Length;
		while (partyCount-- > 0)
		{
			var newIndex = (partyCount + startIndex) % party.Length;
//			Debug.Log(" ? " + newIndex);
			var newChar = party[newIndex];
			if (!newChar.IsDead())
			{
				SelectCharacter(newChar);
				return;
			}
		}
		
		Debug.LogWarning(" Has no character to select next!");
	}
}