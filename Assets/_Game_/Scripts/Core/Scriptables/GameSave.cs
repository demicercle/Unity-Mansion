using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSave", menuName = "GameSave", order = 0)]
public class GameSave : ScriptableObject
{
	public const int LastGameVersion = 2;
	
	public static int debugMode { get; set; }
	
	private static GameSave currentInstance;

	public static GameSave Current
	{
		get
		{
			if (!currentInstance)
			{
				currentInstance = Resources.LoadAll<GameSave>("").FirstOrDefault();
			
				if (!currentInstance)
				{
					Debug.LogWarning(" cannot find game save in ressources, create one");
					currentInstance = CreateInstance<GameSave>();
				}

				currentInstance.Clear();
			}
			
			return currentInstance;
		}
	}
	
	public static string folderPath => Application.persistentDataPath;
	public static string filePath => folderPath + "/GameSave.json";
	public static bool SaveExists() => System.IO.File.Exists(filePath);
	public static void DeleteSave() => System.IO.File.Delete(filePath);

	[System.Serializable]
	public class SavedLevel
	{
		public override string ToString() => base.ToString() + " (" + levelIndex + ")";
		
		public int levelIndex;
		public List<SavedCharacter> levelCharacters = new List<SavedCharacter>();
		public List<string> revealedRooms = new List<string>();
		public List<SavedProp> savedProps = new List<SavedProp>();
		public List<SavedInventory> savedInventories = new List<SavedInventory>();
		public List<string> destroyedProps = new List<string>();
		public List<SavedLoot> savedLoots = new List<SavedLoot>();
	}

	[System.Serializable]
	public class SavedPC
	{
		public override string ToString() => base.ToString() + " (" + name + ")";
		
		public string name;
		public Vector3 position;
		public int rotation;
		public bool isDead;
		public float damages;
		public string[] slots = Array.Empty<string>();

		public void Save(CharacterEntity character)
		{
			if(debugMode >0) Debug.Log(this + " Save " + character);
			name = character.name;
			position = character.GetSavedPosition();
			rotation = character.GetRotation();
			isDead = character.IsDead();
			
			damages = character.GetTotalDamages();

			if (character.TryGetComponent(out Inventory inv))
				slots = inv.SerializeToSlots();
		}

		public void Load(CharacterEntity character)
		{
			if(debugMode >0) Debug.Log(this + " Load " + character);
			character.Teleport(position, rotation);
			
			if (isDead)
				character.TryKill();

			character.SetDamages(damages);

			if (character.TryGetComponent(out Inventory inv))
				inv.DeserializeFromSlots(slots);
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
		public override string ToString() => base.ToString() + " (" + name + ")";
		
		public string name;
		public Vector3 position;
		public int rotation;
		public bool isDead;

		public void Save(CharacterEntity character)
		{
			if(debugMode >0) Debug.Log(this + " Save " + character);
			name = character.name;
			position = character.GetSavedPosition();
			rotation = character.GetRotation();
			isDead = character.IsDead();
		}

		public void Load(CharacterEntity character)
		{
			if(debugMode >0) Debug.Log(this + " Load " + character);
			character.Teleport(position, rotation, false);
			
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
		public override string ToString() => base.ToString() + " (" + name + ")";
		
		public string name;
		public int count;

		public void Save(PropComponent prop)
		{
			if(debugMode >0) Debug.Log(this + " Save " + prop);
			name = prop.name;
			count = prop.GetInteractionCount();
		}

		public void Load(PropComponent prop)
		{
			if(debugMode >0) Debug.Log(this + " Load " + prop);
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
		public override string ToString() => base.ToString() + " (" + name + ")";
		
		public string name;
		public string[] slots = Array.Empty<string>();

		public void Save(Inventory inventory)
		{
			if(debugMode >0) Debug.Log(this + " Save " + inventory);
			name = inventory.GetName();
			slots = inventory.SerializeToSlots();
		}

		public void Load(Inventory inventory)
		{
			if(debugMode >0) Debug.Log(this + " Save " + inventory);
			if (inventory.IsName(name))
			{
				inventory.DeserializeFromSlots(slots);
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
	public class SavedLoot
	{
		public Vector3 position;
		public string[] itemNames;

		public void Load(Inventory lootBagPrefab)
		{
			var lootBag = Inventory.CreateLootBag(lootBagPrefab, position);
			lootBag.DeserializeFromSlots(itemNames);
		}

		public void Save(Inventory inv)
		{
			position = inv.GetPosition();
			itemNames = inv.SerializeToSlots();
		}

		public SavedLoot(Inventory inv = null)
		{
			if (inv) Save(inv);
		}
	}

	[System.Serializable]
	public class SavedValue
	{
		public override string ToString() => base.ToString() + " (" + name + ")";
		
		public static string Karma = "KARMA";
		//public static string MissCount = "MISS_COUNT";
		
		public string name;
		public int value;
	}

	public static bool IsDisabled()
	{
		return Current.disableSave;
	}

	[SerializeField] private bool disableSave = false;
	[SerializeField] private Inventory lootBagPrefab;
	[Space]
	[Header("Debug")]
	[ReadOnly] [Min(0)] public int version;
	[ReadOnly] public int gameSeed;
	[ReadOnly] public int selectedCharacter;
	[ReadOnly] public List<SavedPC> savedPCs = new List<SavedPC>();
	[ReadOnly] public List<SavedValue> savedValues = new List<SavedValue>();
	[ReadOnly] public List<SavedLevel> savedLevels = new List<SavedLevel>();

	private bool shouldLoad;
	public bool ShouldLoad() => shouldLoad;
	public void MarkAsLoad() => shouldLoad = true;
	
	private bool loadFirstLevel;
	public void MarkFirstLevel() => loadFirstLevel = true;
	public bool LoadFirstLevel() => loadFirstLevel;
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
	public void Clear() => Clear(true);
	public void Clear(bool newSeed)
	{
#if UNITY_EDITOR
		if(!Application.isPlaying) UnityEditor.Undo.RecordObject(this, "Clear");
#endif
		Debug.Log(this + " Clear newSeed=" + newSeed);
		savedLevels.Clear();
		savedPCs.Clear();
		savedValues.Clear();
		selectedCharacter = 0;
		version = 0;
		shouldLoad = false;
		if(newSeed) gameSeed = DicePool.GetRandomSeed();
		DicePool.SetSeed(gameSeed);
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

		if(gameSeed > 0)
			DicePool.SetSeed(gameSeed);
		else Debug.LogWarning(this + " invalid game seed: " + gameSeed);
		
		foreach (var savedValue in savedValues)
		{
			if (savedValue.name == SavedValue.Karma) DiceLauncher.AllowedDiceRolls = savedValue.value;
			//if (savedValue.name == SavedValue.MissCount) DiceLauncher.MissCount = savedValue.value;
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
			if (!GameTurnManager.Instance.IsLevelIndex(savedLevel.levelIndex))
			{
				continue;
			}

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

			foreach (var savedLoot in savedLevel.savedLoots)
			{
				savedLoot.Load(lootBagPrefab);
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

			foreach (var savedDestructibleName in savedLevel.destroyedProps)
			{
				var destructible = Destructible.allInstances.FirstOrDefault(d => d.IsName(savedDestructibleName));
				if (destructible)
				{
					destructible.Break();
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
		}
		
		if(selectedCharacter >= 0)
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

		version = LastGameVersion;

		gameSeed = DicePool.Seed;
		
		savedValues.Add(new SavedValue(){ name = SavedValue.Karma, value = DiceLauncher.AllowedDiceRolls});
		//savedValues.Add(new SavedValue(){ name = SavedValue.MissCount, value = DiceLauncher.MissCount});

		selectedCharacter = CharacterSelection.GetSelectedPartyCharacterIndex();
		
		foreach (var partyChar in CharacterEntity.GetPartyCharacters())
		{
			savedPCs.Add(new SavedPC(partyChar));
		}

		var savedLevel = new SavedLevel();
		savedLevel.levelIndex = GameTurnManager.Instance.GetCurrentLevelIndex();
		
		foreach (var levelChar in GameTurnManager.Instance.GetLevelCharacters())
		{
			if (levelChar.InParty()) continue;
			savedLevel.levelCharacters.Add(new SavedCharacter(levelChar));	
		}

		foreach (var destructible in Destructible.allInstances)
		{
			if(destructible.IsBroken())
				savedLevel.destroyedProps.Add(destructible.GetName());
		}

		foreach (var lootBag in Inventory.GetLootBags())
		{
			savedLevel.savedLoots.Add(new SavedLoot(lootBag));
		}

		foreach (var room in RoomTrigger.allRooms)
		{
			if(room.IsRevealed())
				savedLevel.revealedRooms.Add(room.GetName());
		}
		
		foreach (var inv in Inventory.GetSavedInventories())
		{
			savedLevel.savedInventories.Add(new SavedInventory(inv));
		}

		foreach (var prop in PropComponent.GetSavedInstances())
		{
			savedLevel.savedProps.Add(new SavedProp(prop));
		}

		AppendNewLevel(savedLevel);
		
		Debug.Log(this + " Game saved");
		
		if(toFile)
			SaveToFile();
	}

	private void AppendNewLevel(SavedLevel savedLevel)
	{
		for (int i = 0; i < savedLevels.Count; i++)
		{
			if (savedLevels[i].levelIndex == savedLevel.levelIndex)
			{
				savedLevels[i] = savedLevel;
				Debug.Log(this + " savedLevels[" + i + "] = " + savedLevel.levelIndex);
				return;
			}
		}
		
		savedLevels.Add(savedLevel);
		Debug.Log(this + " savedLevels Add " + savedLevel.levelIndex);
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
		var savedObjects = new Hashtable();
		savedObjects["lootBagPrefab"] = lootBagPrefab;
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
		lootBagPrefab = (Inventory)savedObjects["lootBagPrefab"];
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

	private void OnEnable()
	{
		if (Application.isPlaying)
			Clear();
	}
}