using System;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConsoleWrapper
{
	[ConsoleMethod("apply_dmg", "")]
	public static void _ApplyDamage(int amount)
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.TakeDamage(amount, character);
	}

	[ConsoleMethod("kill_all", "")]
	public static void _KillAll(float range)
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
	public static void _Heal(int amount)
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.Heal(amount);
	}

	[ConsoleMethod("rest", "")]
	public static void _Rest()
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.Rest();
	}

	[ConsoleMethod("use_all_actions", "")]
	public static void _UseAllActions(int value)
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
	public static void _Teleport() => _Teleport(0);
	[ConsoleMethod("teleport", "")]
	public static void _Teleport(int value)
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

	[ConsoleMethod("debug_draw", "")]
	public static void _DebugDraw(int value)
	{
		Debug.Log("DebugDrawer.debugMode=" + (DebugDrawer.debugMode = value>0));
	}
	
	[ConsoleMethod("debug_cursor", "")]
	public static void _DebugCursor(int value)
	{
		Debug.Log("GameInput.debugMode=" + (GameInput.debugMode = value));
	}
	
	[ConsoleMethod("debug_detection", "")]
	public static void _DebugDetection(int value)
	{
		Debug.Log("DetectionTrigger.debugMode=" + (CharacterDetection.debugMode = value));
	}

	/*[ConsoleMethod("debug_gui", "")]
	public static void _DebugGUI(int value)
	{
		DebugGUI.IsEnabled = value > 0;
	}*/

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
	public static void _DtSet(float value)
	{
		Time.timeScale = Mathf.Clamp(value, 0.1f, 10.0f);
	}

	[ConsoleMethod("explore", "")]
	public static void _Explore(float value)
	{
		RoomTrigger.activeRooms.ForEach((room) =>
		{
			room.Explore();
		});
	}
}