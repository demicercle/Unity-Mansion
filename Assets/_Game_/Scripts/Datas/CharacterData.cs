using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "CharacterStats", order = 0)]
public class CharacterData : ScriptableObject
{
	public System.Action<int> partyIndexChanged;
	public System.Action<Vector3> positionChanged;
	public System.Action<int> rotationChanged;
	public System.Action<int> actionsChanged;
	public System.Action<float> movesPointsChanged;
	public System.Action<int> patrolRouteChanged;
	
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
	[SerializeField] private CharacterView characterViewOwner;
	[SerializeField] private Vector3 position;
	[SerializeField] private int rotation;
	[SerializeField] private int partyIndex;

	public int GetPartyIndex() => partyIndex;

	public void SetPartyIndex(int index)
	{
		if (index != partyIndex)
		{
			partyIndex = index;
			partyIndexChanged?.Invoke(partyIndex);
		}
	}

	public CharacterView GetCharacter() => characterViewOwner;

	public void SetPosition(Vector3 newPos)
	{
		position = newPos;
		positionChanged?.Invoke(newPos);
	}

	public void SetRotation(int newRotation)
	{
		rotation = newRotation;
		rotationChanged?.Invoke(newRotation);
	}

	public void ConsumeMovePoints(float v)
	{
		if (v != 0f)
		{
			usedMovePoints += v;
			movesPointsChanged?.Invoke(v);
		}
	}
	
	public float GetTotalMovePoints() => maxMovePoints;
	
	public float GetRemainingMovePoints() => GetTotalMovePoints() - usedMovePoints;
	
	public float GetMovePointsProgress() => 1f - Mathf.Clamp01(maxMovePoints <= 0 ? 0f : usedMovePoints / GetTotalMovePoints());
	
	public int GetRemainingActions() => maxActions - usedActions;

	public void ConsumeActions(int q)
	{
		usedActions += q;
		actionsChanged?.Invoke(q);
		Debug.Log(this + " ConsumeActions x" + q + " remains " + GetRemainingActions());
	}
	
	public void TurnBegins(int turn)
	{
		Debug.Log(this + " TurnBegins " + turn);
		usedMovePoints = 0f;
		usedActions = 0;
	}

	public void RunAction()
	{
		ConsumeActions(1);
		usedMovePoints -= runActionExtraMovePoints;
	}

	public void Init(CharacterView c, int inParty = -1)
	{
		partyIndex = inParty;
		characterViewOwner = c;
		usedMovePoints = 0;
		usedActions = 0;
	}

	public void Dispose()
	{
		
	}
}