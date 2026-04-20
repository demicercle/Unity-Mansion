using System;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConsoleWrapper
{
	static GameView gameView => GameView.GetInstance();

	[ConsoleMethod("apply_dmg", "")]
	static public void ApplyDamage(int amount)
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.TakeDamage(amount);
	}

	[ConsoleMethod("kill_all", "")]
	static public void KillAll(float range)
	{
		var center = CameraPivot.Instance.GetFocusPosition();
		foreach (var character in CharacterController.instances)
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
	static public void Heal(int amount)
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.Heal(amount);
	}

	[ConsoleMethod("rest", "")]
	static public void Rest()
	{
		if(CharacterSelection.TryGetSelectedCharacter(out CharacterController character))
			character.Rest();
	}

	[ConsoleMethod("use_all_actions", "")]
	static public void UseAllActions(int value)
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
	static public void Teleport() => Teleport(0);
	[ConsoleMethod("teleport", "")]
	static public void Teleport(int value)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out CharacterController partyChar))
		{
			var gameView = GameView.GetInstance();
			var result = gameView.CursorRaycast(out var hit);
			if (result == GameView.CURSOR_RAYCAST_RESULT_NAVIGATION)
			{
				partyChar.Teleport(hit.point, value > 0);
			}
		}
	}


	[ConsoleMethod("debug_draw", "")]
	static public void _DebugDraw(int value)
	{
		DebugDrawer.IsEnabled = value > 0;
	}

	[ConsoleMethod("debug_gui", "")]
	static public void _DebugGUI(int value)
	{
		DebugGUI.IsEnabled = value > 0;
	}

	[ConsoleMethod("quit_game", "")]
	static public void QuitGame()
	{
		Application.Quit();
	}

	[ConsoleMethod("hard_reset", "")]
	static public void ResetGame()
	{
		SceneManager.LoadScene(0);
	}

	[ConsoleMethod("dtset", "")]
	static public void _DtSet(float value)
	{
		Time.timeScale = Mathf.Clamp(value, 0.1f, 10.0f);
	}

	[ConsoleMethod("explore", "")]
	static public void _Explore(float value)
	{
		RoomTrigger.activeRooms.ForEach((room) =>
		{
			room.Explore();
		});
	}
}