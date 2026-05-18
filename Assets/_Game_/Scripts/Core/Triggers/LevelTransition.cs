using System;
using UnityEngine;


public class LevelTransition : MonoBehaviour
{
	[SerializeField] private PropComponent prop;
	[SerializeField] private CharacterTrigger trigger;
	[SerializeField] private int levelIndex;
	[SerializeField] private int waypointIndex;

	public void TryTravel()
	{
		if (trigger)
		{
			foreach (var partyChar in CharacterEntity.GetPartyCharacters())
			{
				if (!trigger.HasCharacter(partyChar))
				{
					// you must gather your party
					Debug.Log(this + " you must gather your party");
					GameLog.Instance.AppendLine(LanguageSettings.Main.YouMustGatherYourParty);
					return;
				}
			}
		}
			
		Debug.Log(this + " PartyTravel " + levelIndex + " " + waypointIndex);
		GameTurnManager.Instance.PartyTravel(levelIndex,waypointIndex);
	}

	private void Awake()
	{
		if (prop)
		{
			prop.onInteracted += OnPropInteracted;
			gameObject.SetActive(false);
		}
	}

	private void OnDestroy()
	{
		if (prop)
		{
			prop.onInteracted -= OnPropInteracted;
		}
	}

	private void OnPropInteracted()
	{
		gameObject.SetActive(!gameObject.activeSelf);
	}
}