using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "CharacterStats", order = 0)]
public class CharacterStats : ScriptableObject
{
	public string displayName;
	public Texture2D portraitTexture;
	public float maxMovePoints;
	public int maxActions;

	private float usedMovePoints;

	public void ConsumeMovePoints(float v)
	{
		usedMovePoints += v;
	}
	
	public float GetTotalMovePoints() => maxMovePoints;
	
	public float GetRemainingMovePoints() => GetTotalMovePoints() - usedMovePoints;
	
	public float GetMovePointsProgress() => 1f - Mathf.Clamp01(maxMovePoints <= 0 ? 0f : usedMovePoints / GetTotalMovePoints());

	private int usedActions;
	
	public int GetRemainingActions() => maxActions - usedActions;

	public void ConsumeActions(int consumeActions = 1)
	{
		usedActions += consumeActions;
	}
	
	public void TurnBegins(int turn)
	{
		Debug.Log(this + " TurnBegins " + turn);
		usedMovePoints = 0f;
		usedActions = 0;
	}

	public void Init()
	{
		usedMovePoints = 0;
		usedActions = 0;
		GameEvents.newTurnStarted += TurnBegins;
	}

	public void Dispose()
	{
		GameEvents.newTurnStarted -= TurnBegins;
	}
}