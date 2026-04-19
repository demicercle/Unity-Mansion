using System;
using IngameDebugConsole;
using UnityEngine;

public static class ConsoleWrapper
{
	static GameView gameView => GameView.GetInstance();

	[ConsoleMethod("apply_dmg", "")]
	static public void ApplyDamage(int amount)
	{
		if(PlayerSelection.TryGetSelectedCharacter(out CharacterController character))
			character.TakeDamage(amount);
	}

	[ConsoleMethod("heal", "")]
	static public void Heal(int amount)
	{
		if(PlayerSelection.TryGetSelectedCharacter(out CharacterController character))
			character.Heal(amount);
	}

	[ConsoleMethod("restore", "")]
	static public void Restore()
	{
		if(PlayerSelection.TryGetSelectedCharacter(out CharacterController character))
			character.Restore();
	}

	[ConsoleMethod("use_all_actions", "")]
	static public void UseAllActions(int value)
	{
		if (value == 0) return;
		if (value == 1)
		{
			if (PlayerSelection.TryGetSelectedCharacter(out CharacterController partyChar))
				UseCharacterAllActions(partyChar);
		}
		else
		{
			foreach(var partyChar in CharacterController.GetPartyCharacters())
				UseCharacterAllActions(partyChar);
		}
	}

	static void UseCharacterAllActions(CharacterController partyChar)
	{
		partyChar.ConsumeMovePoints(partyChar.GetRemainingMovePoints());
		partyChar.ConsumeActions(partyChar.GetRemaingActions());
	}

	[ConsoleMethod("debug_draw", "")]
	static public void DebugDraw(int value)
	{
		UserSettings.GetCurrentSettings().drawDebug = value > 0;
	}

	[ConsoleMethod("debug_gui", "")]
	static public void DebugGUI(int value)
	{
		UserSettings.GetCurrentSettings().drawDebug = value > 0;
	}
}