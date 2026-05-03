using System;
using UnityEngine;
using UnityEngine.UI;


public class ActionToggles : MonoBehaviour
{
	[SerializeField] private Toggle toggleStop;
	[SerializeField] private Toggle toggleAim;
	
	private void OnEnable()
	{
		if(toggleAim) toggleAim.onValueChanged.AddListener(OnToggleAim);
		if(toggleStop) toggleStop.onValueChanged.AddListener(OnToggleStop);
	}

	private void OnDisable()
	{
		if(toggleAim) toggleAim.onValueChanged.RemoveListener(OnToggleAim);
		if(toggleStop) toggleStop.onValueChanged.RemoveListener(OnToggleStop);
	}

	private void OnToggleAim(bool isAiming)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			selected.ToggleAim(isAiming);
		}
	}

	private void OnToggleStop(bool isStopped)
	{
		if (isStopped && CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			selected.Stop();
		}
	}

	private void LateUpdate()
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			if (toggleStop) toggleStop.interactable = selected.Routines.AnyActive();
			
			if (toggleAim) 
			{ 
				toggleAim.isOn = selected.IsAiming();
				toggleAim.interactable = !toggleStop.interactable;
			}
		}
		else
		{
			if (toggleStop) toggleStop.interactable = false;
			
			if (toggleAim)
			{
				toggleAim.isOn = false;
				toggleAim.interactable = false;
			}
		}
		
		if(toggleStop) toggleStop.isOn = false;
	}
}