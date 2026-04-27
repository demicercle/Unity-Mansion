using System;
using System.Collections.Generic;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConsoleCommands : MonoBehaviour
{
	[ConsoleMethod("apply_dmg", "")]
	public static void ApplyDamage(int amount)
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.TakeDamage(amount, character);
	}

	[ConsoleMethod("kill_all", "")]
	public static void KillAll()
	{
		foreach (var character in GameTurnManager.Instance.GetTurnBasedCharacters())
		{
			if (!character.IsDead() && !character.InParty() && character.IsEnemyToPlayer())
				character.TryKill();
		}
	}

	[ConsoleMethod("kill_range", "")]
	public static void KillRange(float range)
	{
		var center = CameraPivot.Instance.GetFocusPosition();
		foreach (var character in CharacterController.AllCharacters)
		{
			if (!character.IsDead() && character.IsEnemyToPlayer())
			{
				var dist = Vector3.Distance(character.GetPosition(), center);
				if (dist <= range)
				{
					character.TryKill();
				}
			}
		}
	}

	[ConsoleMethod("heal", "")]
	public static void Heal(int amount)
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.Heal(amount);
	}

	[ConsoleMethod("rest", "")]
	public static void Rest()
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.Rest();
	}

	[ConsoleMethod("use_all_actions", "")]
	public static void UseAllActions(int value)
	{
		if (value == 0) return;
		if (value == 1)
		{
			if (CharacterSelection.TryGetSelectedCharacter(out CharacterController partyChar))
				partyChar.UseAllActions();
		}
		else
		{
			foreach(var partyChar in CharacterController.GetPartyCharacters())
				partyChar.UseAllActions();
		}
	}

	[ConsoleMethod("tp", "")]
	public static void Teleport() => Teleport(0);
	[ConsoleMethod("teleport", "")]
	public static void Teleport(int value)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out CharacterController partyChar))
		{
			var result = GameInput.Instance.CursorRaycast(out var hit);
			if (result == GameInput.CursorRaycastResult_Navigation)
			{
				partyChar.Teleport(hit.point, value > 0);
			}
		}
	}

	[ConsoleMethod("quit_game", "")]
	public static void QuitGame()
	{
		Application.Quit();
	}

	[ConsoleMethod("hard_reset", "")]
	public static void ResetGame()
	{
		SceneManager.LoadScene(0);
	}

	[ConsoleMethod("dtset", "")]
	public static void DtSet(float value)
	{
		Time.timeScale = Mathf.Clamp(value, 0.1f, 10.0f);
	}

	[ConsoleMethod("give_item", "")]
	public static void GiveItem(string itemName)
	{
		var itemData = Resources.LoadAll<ItemData>(Consts.ResPath_Items)
			.FirstOrDefault(i => i.name == itemName);

		if (itemData == null)
		{
			Debug.LogWarning("Item not found: " + itemName);
			return;
		}

		if (!CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
		{
			Debug.LogWarning("No selected character");
			return;
		}

		if (!selectedCharacter.TryGetComponent(out Inventory inventory))
		{
			Debug.LogWarning("Selected character " + selectedCharacter + " has no inventory");
			return;
		}

		if (!inventory.TryAddItem(itemData))
		{
			Debug.LogWarning("Selected character " + selectedCharacter + " inventory full");
			return;
		}
		
		Debug.Log("Item added to inventory: " + itemData + " > " + inventory);
	}
	
	[ConsoleMethod("log", "")]
	public static void Log(string message)
	{
		GameCanvas.Instance.gameLog.AppendLine(message);
	}
	
	#region DEBUG

	[ConsoleMethod("debug_all", "")]
	public static void Debug_All(int value)
	{
		_DebugAgent(value);
		_DebugAnim(value);
		_DebugBrains(value);
		_DebugBrains(value);
		_DebugCamera(value);
		_DebugDetection(value);
		_DebugFollows(value);
		_DebugInput(value);
		_DebugNav(value);
		_DebugRoutines(value);
		_DebugTriggers(value);
		_DebugGtm(value);
	}

	[ConsoleMethod("debug_script", "")]
	public static void _DebugScript(int value)
	{
		Debug.Log("debug_script=" + (GameScript.debugMode = value));
	}

	[ConsoleMethod("debug_inventory", "")]
	public static void _DebugInventory(int value)
	{
		Debug.Log("debug_inventory=" + (Inventory.debugMode = value));
	}

	[ConsoleMethod("debug_gtm", "")]
	public static void _DebugGtm(int value)
	{
		Debug.Log("debug_gtm=" + (GameTurnManager.debugMode = value));
	}

	[ConsoleMethod("debug_stats", "")]
	public static void _DebugStats(int value)
	{
		Debug.Log("debug_stats=" + (CharacterController.debugStats= value));
	}

	[ConsoleMethod("debug_anim", "")]
	public static void _DebugAnim(int value)
	{
		Debug.Log("debug_anim=" + (CharacterAnimation.debugMode = value));
	}

	[ConsoleMethod("debug_routines", "")]
	public static void _DebugRoutines(int value)
	{
		Debug.Log("debug_routines=" + (CharacterRoutines.debugMode = value));
	}
	
	[ConsoleMethod("debug_cursor", "")]
	public static void _DebugInput(int value)
	{
		Debug.Log("debug_cursor=" + (GameInput.debugMode = value));
	}
	
	[ConsoleMethod("debug_follows", "")]
	public static void _DebugFollows(int value)
	{
		Debug.Log("debug_follows=" + (CharacterFollows.debugMode = value));
	}
	
	[ConsoleMethod("debug_camera", "")]
	public static void _DebugCamera(int value)
	{
		Debug.Log("debug_camera=" + (CameraPivot.debugMode = value));
	}
	
	[ConsoleMethod("debug_agent", "")]
	public static void _DebugAgent(int value)
	{
		Debug.Log("debug_agent=" + (CharacterAgent.debugMode = value));
	}
	
	[ConsoleMethod("debug_detection", "")]
	public static void _DebugDetection(int value)
	{
		Debug.Log("debug_detection=" + (CharacterDetection.debugMode = value));
	}

	[ConsoleMethod("debug_brains", "")]
	public static void _DebugBrains(int value)
	{
		Debug.Log("debug_brains=" + (CharacterBrain.debugMode = value));
	}

	[ConsoleMethod("debug_triggers", "")]
	public static void _DebugTriggers(int value)
	{
		Debug.Log("debug_triggers=" + (CharacterTrigger.debugMode = value));
	}
	
	[ConsoleMethod("debug_nav", "")]
	public static void _DebugNav(int value)
	{
		Debug.Log("debug_nav=" + (NavmeshRebuilder.debugMode = value));
	}
	
	#endregion

	public int skipCommands;
	public List<string> commandsOnStart = new List<string>();
	
	private void Start()
	{
		if (skipCommands < 0) return;
		for (int i = skipCommands; i < commandsOnStart.Count; i++)
		{
			DebugLogConsole.ExecuteCommand(commandsOnStart[i]);
		}
	}
}