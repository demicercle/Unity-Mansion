using System;
using UnityEngine;


public class LevelTransition : MonoBehaviour
{
	[SerializeField] private PropComponent prop;
	[SerializeField] private float characterDistance;
	[SerializeField] private int levelIndex;
	[SerializeField] private int waypointIndex;

	public void TryTravel()
	{
		if (characterDistance > 0f)
		{
			if (!CharacterEntity.PartyGathered(characterDistance))
			{
				Debug.Log(this + " PartyTravel YouMustGatherYourParty");
				GameLog.Instance.AppendLine(LanguageSettings.Main.YouMustGatherYourParty);
				return;
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