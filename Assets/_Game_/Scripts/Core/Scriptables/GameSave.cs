using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSave", menuName = "GameSave", order = 0)]
public class GameSave : ScriptableObject
{
	public const int GAME_VERSION = 1;
	
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
		public List<SavedAnimator> savedAnimators = new List<SavedAnimator>();
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
	public class SavedProp
	{
		public string name;
		public int interactionCount;

		public void Save(PropComponent prop)
		{
			name = prop.name;
			interactionCount = prop.GetInteractionCount();
		}

		public void Load(PropComponent prop)
		{
			if (prop.IsName(name))
			{
				prop.SetInteractionCount(interactionCount);
			}
		}

		public SavedProp(PropComponent prop = null)
		{
			if (prop)
				Load(prop);
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
				Load(inv);
			}
		}
	}

	[System.Serializable]
	public class SavedAnimator
	{
		public string name;
		public SavedValue[] savedValues = Array.Empty<SavedValue>();
		
		public void Save(SaveAnimator prop)
		{
			name = prop.GetName();
			savedValues = prop.SaveValues();
		}

		public void Load(SaveAnimator prop)
		{
			if (prop.IsName(name))
			{
				prop.LoadValues(savedValues);
			}
		}

		public SavedAnimator(SaveAnimator anim = null)
		{
			if (anim)
			{
				Load(anim);
			}
		}
	}

	[System.Serializable]
	public class SavedValue
	{
		public string name;
		public ValueType valueType;
		[ReadOnly] public bool currentValue;
	}
	
	public enum ValueType
	{
		Bool,
		Int,
		Float,
		String
	}

	[Min(0)] public int version;
	public List<SavedLevel> savedLevels = new List<SavedLevel>();
	public int selectedCharacter;
	public List<SavedPC> savedPCs = new List<SavedPC>();

	public bool IsEmpty() => version == 0;

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

			foreach (var savedAnim in savedLevel.savedAnimators)
			{
				foreach (var sa in SaveAnimator.GetInstances())
				{
					if (sa.IsName(savedAnim.name))
					{
						savedAnim.Load(sa);
						break;
					}
				}
			}
		}
		
		CharacterSelection.SelectCharacterAt(selectedCharacter);
		
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
		
		Clear();

		version = GAME_VERSION;

		selectedCharacter = CharacterSelection.GetSelectedPartyCharacterIndex();
		
		foreach (var partyChar in CharacterController.GetPartyCharacters())
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

		foreach (var sa in SaveAnimator.GetInstances())
		{
			savedLevels[0].savedAnimators.Add(new SavedAnimator(sa));
		}
		
		Debug.Log(this + " Game saved");
	}

	[ContextMenu("Save To File")]
	public void SaveToFile()
	{
		var json = JsonUtility.ToJson(this, true);
		System.IO.File.WriteAllText(filePath, json);
		Debug.Log(this + " Saved To File " + filePath);
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
			return;
		}
		Debug.Log(this + " Loaded From File " + filePath);
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