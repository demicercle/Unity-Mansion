using IngameDebugConsole;
using UnityEngine;

public class ConsoleWrapper : MonoBehaviour
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

	[ConsoleMethod("debug_draw", "")]
	static public void DebugDraw(bool value)
	{
		global::DrawDebug.IsEnabled = value;
	}

	[ConsoleMethod("debug_gui", "")]
	static public void DebugGUI(bool value)
	{
		global::DebugGUI.IsEnabled = value;
	}
}