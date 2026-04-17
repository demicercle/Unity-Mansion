using IngameDebugConsole;
using UnityEngine;

public class ConsoleEvents : MonoBehaviour
{
	static GameView gameView => GameView.GetInstance();
	static GameData gameData => gameView.gameData;
	static CharacterData GetCharacterAt(int charIndex) => gameData.GetCharacterAt(charIndex);

	static bool TryGetSelectedCharacterIndex(out int selected)
	{
		if (gameView.TryGetSelectedCharacter(out var characterView))
		{
			selected = characterView.GetCharacterIndex();
			return true;
		}

		selected = -1;
		return false;
	}

	[ConsoleMethod("apply_dmg", "")]
	static public void ApplyDamage(int amount)
	{
		if (TryGetSelectedCharacterIndex(out var selected)) ApplyDamage(selected, amount);
	}
	static public void ApplyDamage(int charIndex, float amount)
	{
		GetCharacterAt(charIndex).TakeDamage(amount);
	}

	[ConsoleMethod("heal", "")]
	static public void Heal(int amount)
	{
		if (TryGetSelectedCharacterIndex(out var selected)) Heal(selected, amount);
	}
	static public void Heal(int charIndex, float amount)
	{
		GetCharacterAt(charIndex).Heal(amount);
	}

	[ConsoleMethod("end_turn", "")]
	static public void EndTurn()
	{
		gameData.EndTurn();
	}
	
	static public void ConsumeMovePoints(int charIndex, float amount)
	{
		GetCharacterAt(charIndex).ConsumeMovePoints(amount);
	}

	static public void ConsumeActions(int charIndex, int amount)
	{
		GetCharacterAt(charIndex).ConsumeActions(amount);
	}

	static public void SetCharacterRotation(int charIndex, int rotation)
	{
		GetCharacterAt(charIndex).SetRotation(rotation);
	}

	static public void SetCharacterPosition(int charIndex, Vector3 newPos)
	{
		GetCharacterAt(charIndex).SetPosition(newPos);
	}
	
	static public void SetCharacterPartyIndex(int charIndex, int newIndex)
	{
		gameData.SetCharacterInParty(GetCharacterAt(charIndex), newIndex);
	}
}