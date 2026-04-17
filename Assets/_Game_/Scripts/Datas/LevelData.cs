using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "LevelData", order = 0)]
public class LevelData : ScriptableObject
{
	public System.Action<CharacterData[]> partyChanged;
	
	[SerializeField] private List<string> scenes = new List<string>();
	
	[Header("ReadOnly")]
	[ReadOnly] [SerializeField] private List<CharacterData> allCharacters = new List<CharacterData>();
	[ReadOnly] [SerializeField] private List<CharacterData> partyCharacters = new List<CharacterData>();
	
	public string[] GetScenes() => scenes.ToArray();
	
	public CharacterData[] GetAllCharacters() => allCharacters.ToArray();
	
	public int GetCharacterIndex(CharacterData character) => allCharacters.IndexOf(character);
	
	public bool TryGetCharacter(int index, out CharacterData character)
	{
		if(index >= 0 && index < allCharacters.Count)
			character = allCharacters[index];
		else 
			character = null;
		return character != null;
	}
	
	public bool TryGetPartyCharacter(int partyIndex, out CharacterData character)
	{
		character = default;
		
		if (partyIndex >= 0 && partyIndex < allCharacters.Count)
		{
			character = partyCharacters[partyIndex];
		}

		return character != null;
	}

	public void AddCharacter(CharacterData character)
	{
		allCharacters.Add(character);
	}

	public void RemoveCharacter(CharacterData character)
	{
		allCharacters.Remove(character);
	}
	
	public void SetCharacterInParty(CharacterData character, int partyIndex)
	{
		character.SetPartyIndex(partyIndex);
		partyCharacters = allCharacters.OrderBy(c => c.GetPartyIndex()).Where(c => c.GetPartyIndex() >= 0).ToList();
		partyChanged?.Invoke(partyCharacters.ToArray());
	}
}