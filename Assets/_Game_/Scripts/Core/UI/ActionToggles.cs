using System;
using UnityEngine;
using UnityEngine.UI;


public class ActionToggles : MonoBehaviour
{
	[SerializeField] private Toggle toggleAim;
	
	private void OnEnable()
	{
		if(toggleAim) toggleAim.onValueChanged.AddListener(OnToggleAim);
	}

	private void OnDisable()
	{
		if(toggleAim) toggleAim.onValueChanged.RemoveListener(OnToggleAim);
	}

	private void OnToggleAim(bool isAiming)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			selected.ToggleAim(isAiming);
		}
	}

	private void LateUpdate()
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			if (toggleAim) toggleAim.isOn = selected.IsAiming();
		}
		else
		{
			if (toggleAim) toggleAim.isOn = false;
		}
	}
}