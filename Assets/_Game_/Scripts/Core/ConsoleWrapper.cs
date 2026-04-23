using System;
using System.Linq;
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
	
	[ConsoleMethod("log", "")]
	public static void _Log(string message)
	{
		GameCanvas.Instance.gameLog.AppendLine(message);
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
	public static void _DtSet(float value)
	{
		Time.timeScale = Mathf.Clamp(value, 0.1f, 10.0f);
	}

	[ConsoleMethod("explore", "")]
	public static void _Explore(float value)
	{
		RoomTrigger.activeRooms.ToList().ForEach((room) =>
		{
			room.Explore();
		});
	}
}