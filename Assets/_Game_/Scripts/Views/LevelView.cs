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
	public int GetPartyCharacterIndex(CharacterView characterView) => partyCharacters.IndexOf(characterView);

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
			Debug.Log(this + " found character: " + character);
			
			if (!character.HasSpawnData(out var spawnData))
			{
				Debug.LogWarning(character + " has no spawn data!");
				character.gameObject.SetActive(false);
				continue;
			}

			var partyIndex = character.IsPlayer() ? partyCharacters.Count : -1;
			
			var characterData = Instantiate(spawnData);
			characterData.Init(partyIndex);
			
			levelData.AddCharacter(characterData);
			
			if (partyIndex >= 0)
			{
				levelData.SetCharacterInParty(characterData, partyIndex);
				partyCharacters.Add(character);
			}
			
			character.Init(characterData);
			allCharacters.Add(character);
		}
	}

	private void PartyChanged(CharacterData[] obj)
	{
		partyChanged?.Invoke(partyCharacters.ToArray());
	}
}