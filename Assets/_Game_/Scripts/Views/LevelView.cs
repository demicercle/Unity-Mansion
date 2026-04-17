using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LevelView : MonoBehaviour
{
	public System.Action<CharacterView[]> partyChanged;
	
	private LevelData levelData = null;
	private List<CharacterView> allCharacters = new List<CharacterView>();
	private List<CharacterView> partyCharacters = new List<CharacterView>();
	
	public List<CharacterView> GetAllCharacters() => allCharacters;
	public List<CharacterView> GetPartyCharacters() => partyCharacters;

	public IEnumerator LoadLevel(LevelData newData = null)
	{
		if (newData == null)
		{
			Debug.LogWarning(this + " Init no data, create one");
			newData = ScriptableObject.CreateInstance<LevelData>();
		}
		
		var allScenes = newData.GetScenes();
		if (allScenes.Length <= 0)
		{
			Debug.LogWarning(newData + " has no scenes!");
		}
		
		foreach (var scene in allScenes)
		{
			Debug.Log(this + " load scene: " + scene);
			var async = SceneManager.LoadSceneAsync(scene);
			do
				yield return null;
			while (!async.isDone);
		}
		
		Init(newData);
	}
	
	void Init(LevelData newData)
	{
		if (levelData != null)
		{
			Debug.Log(this +" Dispose " + levelData);
			levelData.partyChanged -= PartyChanged;
		}
		
		Debug.Log(this + " Init " + newData);
		levelData = newData;
		levelData.partyChanged += PartyChanged;
		
		partyCharacters = new List<CharacterView>();
		allCharacters = new List<CharacterView>();
		//TODO SpawnParty(); from save

		foreach (var character in CharacterView.allInstances)
		{
			allCharacters.Add(character);
			Debug.Log(this + " found character: " + character);
			
			character.Init(null);
			
			var characterData = character.GetData();
			levelData.AddCharacter(characterData);
			
			if (character.IsPlayer())
			{
				levelData.SetCharacterInParty(characterData, partyCharacters.Count);
				partyCharacters.Add(character);
			}
		}
	}

	private void PartyChanged(CharacterData[] obj)
	{
		partyChanged?.Invoke(partyCharacters.ToArray());
	}
}