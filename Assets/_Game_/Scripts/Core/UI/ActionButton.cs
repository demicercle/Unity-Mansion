using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ActionButton : MonoBehaviour
{
	[SerializeField] private CharacterAction actionType;

	private Button button;
	
	private void OnEnable()
	{
		button = GetComponent<Button>();
		button.onClick.AddListener(OnButtonClick);
	}

	private void OnDisable()
	{
		button.onClick.RemoveListener(OnButtonClick);
	}

	private void LateUpdate()
	{
		button.interactable = CharacterSelection.TryGetSelectedCharacter(out var selected) 
		                      && selected.IsControllableByPlayer() 
		                      && selected.CanUseAction(actionType);
	}

	private void OnButtonClick()
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			if (selected.CanUseAction(actionType))
			{
				selected.Routines.AssignUseAction(actionType);
			}
			else
			{
				selected.FloatingText_Cant();
			}
		}
	}
}