using System;
using UnityEngine;
using UnityEngine.UI;


public class ActionToggles : MonoBehaviour
{
	[SerializeField] private Toggle toggleStop;
	[SerializeField] private Toggle toggleAim;
	[SerializeField] private Toggle toggleReload;
	
	private void OnEnable()
	{
		if(toggleAim) toggleAim.onValueChanged.AddListener(OnToggleAim);
		if(toggleStop) toggleStop.onValueChanged.AddListener(OnToggleStop);
		if(toggleReload) toggleReload.onValueChanged.AddListener(OnToggleReload);
	}

	private void OnDisable()
	{
		if(toggleAim) toggleAim.onValueChanged.RemoveListener(OnToggleAim);
		if(toggleStop) toggleStop.onValueChanged.RemoveListener(OnToggleStop);
		if(toggleReload) toggleReload.onValueChanged.RemoveListener(OnToggleReload);
	}

	private void OnToggleReload(bool wantsReload)
	{
		if (wantsReload)
		{
			if (CharacterSelection.TryGetSelectedCharacter(out var selected))
			{
				selected.UseAction(CharacterAction.Reload);
			}
		}
	}

	private void OnToggleAim(bool isAiming)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			selected.SetAiming(isAiming);
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

			if (toggleReload)
			{
				toggleReload.gameObject.SetActive(selected.HasWeaponToReload());
				toggleReload.interactable = !toggleStop.interactable;
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

			if (toggleReload)
			{
				toggleReload.gameObject.SetActive(false);
				toggleReload.interactable = false;
			}
		}
		
		if(toggleStop) toggleStop.isOn = false;
		if(toggleReload) toggleReload.isOn = false;
	}
}