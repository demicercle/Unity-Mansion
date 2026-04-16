using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "CharacterStats", order = 0)]
public class CharacterStats : ScriptableObject
{
	[Header("Profile")]
	public string displayName;
	public Texture2D portraitTexture;
	
	[Header("Actions")]
	public int maxActions;
	public float maxMovePoints;
	public float runActionExtraMovePoints = 3.0f;

	[Header("Debug")]
	[SerializeField] private float usedMovePoints;
	[SerializeField] private int usedActions;
	[SerializeField] private Character characterOwner;

	public void ConsumeMovePoints(float v)
	{
		usedMovePoints += v;
	}
	
	public float GetTotalMovePoints() => maxMovePoints;
	
	public float GetRemainingMovePoints() => GetTotalMovePoints() - usedMovePoints;
	
	public float GetMovePointsProgress() => 1f - Mathf.Clamp01(maxMovePoints <= 0 ? 0f : usedMovePoints / GetTotalMovePoints());
	
	public int GetRemainingActions() => maxActions - usedActions;

	public void ConsumeActions()
	{
		usedActions += 1;
		Debug.Log(this + " ConsumeActions " + usedActions + " remains " + GetRemainingActions());
		GameEvents.actionUsed?.Invoke(this);
	}
	
	public void TurnBegins(int turn)
	{
		Debug.Log(this + " TurnBegins " + turn);
		usedMovePoints = 0f;
		usedActions = 0;
	}

	public void RunAction(Character character)
	{
		if (character == characterOwner)
		{
			ConsumeActions();
			usedMovePoints -= runActionExtraMovePoints;
		}
	}


	public void Init(Character c)
	{
		characterOwner = c;
		usedMovePoints = 0;
		usedActions = 0;
		GameEvents.newTurnStarted += TurnBegins;
		GameEvents.runAction += RunAction;
	}

	public void Dispose()
	{
		GameEvents.newTurnStarted -= TurnBegins;
		GameEvents.runAction -= RunAction;
	}
}