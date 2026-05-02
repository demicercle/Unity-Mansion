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
		CharacterSelection.selectionChanged += SelectionChanged;
		GameTurnManager.Instance.onTurnEnded += TurnChanged;
		GameTurnManager.Instance.onTurnStarted += TurnChanged;
	}

	private void OnDisable()
	{
		button.onClick.RemoveListener(OnButtonClick);
		CharacterSelection.selectionChanged -= SelectionChanged;
		GameTurnManager.Instance.onTurnEnded -= TurnChanged;
		GameTurnManager.Instance.onTurnStarted -= TurnChanged;
	}

	private void SelectionChanged()
	{
		RefreshState();
	}

	private void TurnChanged()
	{
		button.interactable = false;
		Invoke("RefreshState", 0.1f);
	}

	private void RefreshState()
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