using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LevelView : MonoBehaviour
{
	static public T FindObject<T>() where T : Object => Object.FindObjectsByType<T>(default).FirstOrDefault();
	static public T[] FindAllObjects<T>() where T : Object => Object.FindObjectsByType<T>(default);
	
	private LevelData levelData;
	private List<CharacterView> allCharacters;
	private List<CharacterView> partyCharacters;
	
	public List<CharacterView> GetAllCharacters() => allCharacters;
	public List<CharacterView> GetPartyCharacters() => partyCharacters;

	public IEnumerator LoadLevel(LevelData newData = null)
	{
		if(newData == null)
			newData = ScriptableObject.CreateInstance<LevelData>();

		var allScenes = newData.GetScenes();
		if (allScenes.Length <= 0)
		{
			Debug.LogWarning(this + " has no scenes!");
			yield break;
		}
		
		foreach (var scene in allScenes)
		{
			Debug.Log(this + " load scene: " + scene);
			var async = SceneManager.LoadSceneAsync(scene);
			while (!async.isDone)
				yield return null;
		}
		
		Init(newData);
	}
	
	public void Init(LevelData newData = null)
	{
		if(newData == null)
			newData = ScriptableObject.CreateInstance<LevelData>();
		
		levelData = newData;
		partyCharacters = new List<CharacterView>();
		allCharacters = new List<CharacterView>(FindAllObjects<CharacterView>());
		//TODO SpawnParty(); from save

		foreach (var character in allCharacters)
		{
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
}