using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "CharacterStats", order = 0)]
public class CharacterStats : ScriptableObject
{
	public string displayName;
	public Texture2D portraitTexture;
	public float maxMovePoints;

	public void TurnBegins(int turn)
	{
		Debug.Log(this + " TurnBegins " + turn);
		usedMovePoints = 0f;
	}

	public void ConsumeUsePoints(float v)
	{
		usedMovePoints += v;
	}
	
	public float GetRemainingMovePoints() => maxMovePoints - usedMovePoints;
	
	public float GetTotalMovePoints() => maxMovePoints;
	
	public float GetMovePointsProgress() => maxMovePoints <= 0 ? 1f : usedMovePoints / maxMovePoints;

	public void Init()
	{
		GameEvents.newTurnStarted += TurnBegins;
	}

	public void Dispose()
	{
		GameEvents.newTurnStarted -= TurnBegins;
	}

	private float usedMovePoints;
}