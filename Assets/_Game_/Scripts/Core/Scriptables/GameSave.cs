using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSave", menuName = "GameSave", order = 0)]
public class GameSave : ScriptableObject
{
	private string filePath => Application.persistentDataPath + "/GameSave.json";
	
	[System.Serializable]
	public class SavedCharacter
	{
		public string name;
		public Vector3 position;
		public int rotation;
		public bool isDead;

		virtual public void Save(CharacterController character)
		{
			name = character.name;
			position = Snapping.Snap(character.GetPosition(),Vector3.one * 0.1f);
			rotation = character.GetRotation();
			isDead = character.IsDead();
		}

		virtual public void Load(CharacterController character)
		{
			character.Teleport(position, rotation);
			if (isDead)
				character.TryKill();
		}

		public SavedCharacter(CharacterController character = null)
		{
			if(character)
				Save(character);
		}
	}

	[System.Serializable]
	public class SavedPC : SavedCharacter
	{
		public float damages;

		override public void Save(CharacterController character)
		{
			base.Save(character);
			damages = character.GetTotalDamages();
		}

		override public void Load(CharacterController character)
		{
			base.Load(character);
			character.SetDamages(damages);
		}

		public SavedPC(CharacterController character = null)
		{
			if(character)
				Save(character);
		}
	}

	[System.Serializable]
	public class SavedLevel
	{
		public int levelIndex;
		public List<SavedCharacter> levelCharacters = new List<SavedCharacter>();
	}

	[Min(0)] public int version;
	public List<SavedLevel> savedLevels = new List<SavedLevel>();
	public List<SavedPC> savedPCs = new List<SavedPC>();

	[ContextMenu("Load Game")]
	public void LoadGame()
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning(this + " LoadGame works only in play mode!");
			return;
		}

		var partyCharacters = CharacterController.GetPartyCharacters();
		foreach (var savedPC in savedPCs)
		{
			var partyChar = partyCharacters.FirstOrDefault(pc => pc.IsName(savedPC.name));
			if (!partyChar)
			{
				Debug.LogWarning(this + " cannot find savedPC " + savedPC.name);
				continue;
			}
			
			savedPC.Load(partyChar);
		}

		foreach (var savedLevel in savedLevels)
		{
			if (GameTurnManager.Instance.IsLevelIndex(savedLevel.levelIndex))
			{
				var levelCharacters = GameTurnManager.Instance.GetLevelCharacters();
				foreach (var savedChar in savedLevel.levelCharacters)
				{
					var levelChar = levelCharacters.FirstOrDefault(pc => pc.IsName(savedChar.name));
					if (!levelChar)
					{
						Debug.LogWarning(this + " cannot find savedChar " + savedChar.name);
						continue;
					}
					
					if (levelChar.InParty()) continue;
					
					savedChar.Load(levelChar);
				}
			}
		}
		
		Debug.Log(this + " Game loaded");
	}
	
	[ContextMenu("Save Game")]
	public void SaveGame()
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning(this + " SaveGame works only in play mode!");
			return;
		}
		
		savedPCs.Clear();
		
		foreach (var partyChar in CharacterController.GetPartyCharacters())
		{
			savedPCs.Add(new SavedPC(partyChar));
		}

		savedLevels.Clear();
		
		savedLevels.Insert(0, new SavedLevel());
		foreach (var levelChar in GameTurnManager.Instance.GetLevelCharacters())
		{
			if (levelChar.InParty()) continue;
			savedLevels[0].levelCharacters.Add(new SavedCharacter(levelChar));	
		}
		
		Debug.Log(this + " Game saved");
	}

	[ContextMenu("Save To File")]
	public void SaveToFile()
	{
		var json = JsonUtility.ToJson(this, true);
		System.IO.File.WriteAllText(filePath, json);
	}

	[ContextMenu("Load From File")]
	public void LoadFromFile()
	{
#if UNITY_EDITOR
		if(!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Load From File");
#endif
		var backup = JsonUtility.ToJson(this, true);
		var lastVersion = version;
		var json = System.IO.File.ReadAllText(filePath);
		JsonUtility.FromJsonOverwrite(json, this);
		if (lastVersion != version)
		{
			Debug.LogWarning(this + " incompatible version: " + lastVersion + " > " + version);
			JsonUtility.FromJsonOverwrite(backup, this);
		}
#if UNITY_EDITOR
		if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
	}

	[ContextMenu("Open file")]
	public void OpenFile()
	{
		Application.OpenURL("file://" + filePath);
	}
}