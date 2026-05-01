using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSave", menuName = "GameSave", order = 0)]
public class GameSave : ScriptableObject
{
	public const int LastGameVersion = 2;
	
	private static GameSave currentInstance;

	public static GameSave Current
	{
		get
		{
			if (!currentInstance) currentInstance = Resources.LoadAll<GameSave>("").FirstOrDefault();
			if (!currentInstance)
			{
				Debug.LogWarning(" cannot find game save in ressources, create one");
				currentInstance = CreateInstance<GameSave>();
			}

			return currentInstance;
		}
	}
	
	private static string filePath => Application.persistentDataPath + "/GameSave.json";
	public static bool SaveExists() => System.IO.File.Exists(filePath);
	public static void DeleteSave() => System.IO.File.Delete(filePath);

	[System.Serializable]
	public class SavedLevel
	{
		public int levelIndex;
		public List<SavedCharacter> levelCharacters = new List<SavedCharacter>();
		public List<string> revealedRooms = new List<string>();
		public List<SavedProp> savedProps = new List<SavedProp>();
		public List<SavedInventory> savedInventories = new List<SavedInventory>();
		public List<string> destroyedProps = new List<string>();
	}

	[System.Serializable]
	public class SavedPC
	{
		public string name;
		public Vector3 position;
		public int rotation;
		public bool isDead;
		public float damages;
		public string[] slots = Array.Empty<string>();

		public void Save(CharacterEntity character)
		{
			name = character.name;
			position = character.GetSavedPosition();
			rotation = character.GetRotation();
			isDead = character.IsDead();
			
			damages = character.GetTotalDamages();

			if (character.TryGetComponent(out Inventory inv))
				slots = inv.GetItemNames();
		}

		public void Load(CharacterEntity character)
		{
			character.Teleport(position, rotation);
			
			if (isDead)
				character.TryKill();

			character.SetDamages(damages);

			if (character.TryGetComponent(out Inventory inv))
				inv.SetItemNames(slots);
		}

		public SavedPC(CharacterEntity character = null)
		{
			if(character)
				Save(character);
		}
	}
	
	[System.Serializable]
	public class SavedCharacter
	{
		public string name;
		public Vector3 position;
		public int rotation;
		public bool isDead;

		public void Save(CharacterEntity character)
		{
			name = character.name;
			position = character.GetSavedPosition();
			rotation = character.GetRotation();
			isDead = character.IsDead();
		}

		public void Load(CharacterEntity character)
		{
			character.Teleport(position, rotation);
			
			if (isDead)
				character.TryKill();
		}

		public SavedCharacter(CharacterEntity character = null)
		{
			if(character)
				Save(character);
		}
	}

	[System.Serializable]
	public class SavedProp
	{
		public string name;
		public int count;

		public void Save(PropComponent prop)
		{
			name = prop.name;
			count = prop.GetInteractionCount();
		}

		public void Load(PropComponent prop)
		{
			if (prop.IsName(name))
			{
				prop.SetInteractionCount(count);
			}
		}

		public SavedProp(PropComponent prop = null)
		{
			if (prop)
				Save(prop);
		}
	}
	
	[System.Serializable]
	public class SavedInventory
	{
		public string name;
		public string[] slots = Array.Empty<string>();

		public void Save(Inventory inventory)
		{
			name = inventory.GetName();
			slots = inventory.GetItemNames();
		}

		public void Load(Inventory inventory)
		{
			if (inventory.IsName(name))
			{
				for (int i = 0; i < slots.Length; i++)
				{
					inventory.SetItemName(i, slots[i]);
				}
			}
		}

		public SavedInventory(Inventory inv = null)
		{
			if (inv)
			{
				Save(inv);
			}
		}
	}

	[System.Serializable]
	public class SavedValue
	{
		public static string Karma = "KARMA";
		
		public string name;
		public int value;
	}

	public static bool IsDisabled()
	{
		return Current.disableSave;
	}

	[SerializeField] private bool disableSave = false;

	[Min(0)] public int version;
	public List<SavedLevel> savedLevels = new List<SavedLevel>();
	public int selectedCharacter;
	public List<SavedPC> savedPCs = new List<SavedPC>();
	public List<SavedValue> savedValues = new List<SavedValue>();
	
	public bool IsEmpty() => version == 0;

	public void SetSavedValue(string valueName, int newValue)
	{
		for (int i = 0; i < savedValues.Count; i++)
		{
			if (savedValues[i].name == valueName)
			{
				savedValues[i].value = newValue;
				return;
			}
		}
		
		savedValues.Add(new SavedValue(){name = valueName, value = newValue});
	}

	public bool TryGetSavedValue(string valueName, out int outValue)
	{
		for (int i = 0; i < savedValues.Count; i++)
		{
			if (savedValues[i].name == valueName)
			{
				outValue = savedValues[i].value;
				return true;
			}
		}

		outValue = 0;
		return false;
	}

	[ContextMenu("Clear")]
	public void Clear()
	{
#if UNITY_EDITOR
		if(!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Clear");
#endif
		savedLevels.Clear();
		savedPCs.Clear();
		selectedCharacter = 0;
		version = 0;
		if(!Application.isPlaying) Utils.SetDirty(this);
	}

	[ContextMenu("Load Game")]
	public void LoadGame() => LoadGame(false);
	public void LoadGame(bool fromFile)
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning(this + " LoadGame works only in play mode!");
			return;
		}
		
		if (fromFile)
			LoadFromFile();

		foreach (var savedValue in savedValues)
		{
			if (savedValue.name == SavedValue.Karma) DiceLauncher.AllowedDiceRolls = savedValue.value;
		}
		
		var partyCharacters = CharacterEntity.GetPartyCharacters();
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

			foreach (var roomName in savedLevel.revealedRooms)
			{
				foreach (var room in RoomTrigger.allRooms)
				{
					if (!room.IsRevealed() && room.IsName(roomName))
					{
						room.Explore();
						break;
					}
				}
			}

			foreach (var savedProp in savedLevel.savedProps)
			{
				foreach (var prop in PropComponent.GetSavedInstances())
				{
					if (prop.IsName(savedProp.name))
					{
						savedProp.Load(prop);
						break;
					}
				}
			}

			foreach (var savedInv in savedLevel.savedInventories)
			{
				foreach (var inv in Inventory.GetSavedInventories())
				{
					if (inv.IsName(savedInv.name))
					{
						savedInv.Load(inv);
						break;
					}
				}
			}

			foreach (var savedDestructibleName in savedLevel.destroyedProps)
			{
				var destructible = Destructible.allInstances.FirstOrDefault(d => d.IsName(savedDestructibleName));
				if (destructible)
				{
					destructible.Break();
				}
			}
		}
		
		CharacterSelection.SelectCharacterAt(selectedCharacter);
		
		Debug.Log(this + " Game loaded");
	}
	
	[ContextMenu("Save Game")]
	public void SaveGame()=>SaveGame(false);
	public void SaveGame(bool toFile)
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning(this + " SaveGame works only in play mode!");
			return;
		}
		
		Clear();

		version = LastGameVersion;
		
		savedValues.Add(new SavedValue(){ name = SavedValue.Karma, value = DiceLauncher.AllowedDiceRolls});

		selectedCharacter = CharacterSelection.GetSelectedPartyCharacterIndex();
		
		foreach (var partyChar in CharacterEntity.GetPartyCharacters())
		{
			savedPCs.Add(new SavedPC(partyChar));
		}
		
		savedLevels.Insert(0, new SavedLevel());
		foreach (var levelChar in GameTurnManager.Instance.GetLevelCharacters())
		{
			if (levelChar.InParty()) continue;
			savedLevels[0].levelCharacters.Add(new SavedCharacter(levelChar));	
		}

		foreach (var room in RoomTrigger.allRooms)
		{
			if(room.IsRevealed())
				savedLevels[0].revealedRooms.Add(room.GetName());
		}

		foreach (var prop in PropComponent.GetSavedInstances())
		{
			savedLevels[0].savedProps.Add(new SavedProp(prop));
		}
		
		foreach (var inv in Inventory.GetSavedInventories())
		{
			savedLevels[0].savedInventories.Add(new SavedInventory(inv));
		}

		foreach (var destructible in Destructible.allInstances)
		{
			if(destructible.IsBroken())
				savedLevels[0].destroyedProps.Add(destructible.GetName());
		}
		
		Debug.Log(this + " Game saved");
		
		if(toFile)
			SaveToFile();
	}

	[ContextMenu("Save To File")]
	public void SaveToFile()
	{
		var json = JsonUtility.ToJson(this, true);
		System.IO.File.WriteAllText(filePath, json);
		Debug.Log(this + " Saved To File " + filePath);
	}

	[ContextMenu("Load From File")]
	public bool LoadFromFile()
	{
#if UNITY_EDITOR
		if(!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Load From File");
#endif
		var backup = JsonUtility.ToJson(this, true);
		var json = System.IO.File.ReadAllText(filePath);
		JsonUtility.FromJsonOverwrite(json, this);
		if (LastGameVersion != version)
		{
			Debug.LogWarning(this + " incompatible version: " + version + " > LastGameVersion: " + LastGameVersion);
			JsonUtility.FromJsonOverwrite(backup, this);
			return false;
		}
		Debug.Log(this + " Loaded From File " + filePath);
#if UNITY_EDITOR
		if(!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
#endif
		return true;
	}

	[ContextMenu("Open file")]
	public void OpenFile()
	{
		Application.OpenURL("file://" + filePath);
	}
}