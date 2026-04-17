using IngameDebugConsole;
using UnityEngine;
using System.Collections;

public class ConsoleEvents : MonoBehaviour
{
	
	IEnumerator Start()
	{
		DontDestroyOnLoad(this);
		var gameView = GameView.GetInstance();
		var gameData = gameView.gameData;//TODO
		
		DebugLogConsole.AddCommand<int>("apply_dmg", "damage selected character", (amount) =>
		{
			if (gameView.TryGetSelectedCharacter(out var selected))
				selected.GetData().TakeDamage(amount);
		});
		
		DebugLogConsole.AddCommand<float>("heal", "heal selected character", (amount) =>
		{
			if (gameView.TryGetSelectedCharacter(out var selected) )
				selected.GetData().Heal(amount);
		});

		var wasVisible = DebugLogManager.Instance.IsLogWindowVisible;
		while (true)
		{
			if (wasVisible != DebugLogManager.Instance.IsLogWindowVisible)
			{
				wasVisible = DebugLogManager.Instance.IsLogWindowVisible;
				if (DebugLogManager.Instance.IsLogWindowVisible)
				{
					gameView.inputSystem.Disable();
				}
				else
				{
					gameView.inputSystem.Enable();
				}
			}
			yield return null;
		}
	}
}