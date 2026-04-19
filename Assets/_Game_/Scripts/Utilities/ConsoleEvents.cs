using IngameDebugConsole;
using UnityEngine;

public class ConsoleEvents : MonoBehaviour
{
	static GameView gameView => GameView.GetInstance();
	static GameController GetController() => gameView.GetController();

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
}