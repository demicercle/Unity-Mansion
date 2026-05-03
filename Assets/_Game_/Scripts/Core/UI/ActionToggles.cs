using UnityEngine;
using UnityEngine.UI;


public class ActionToggles : MonoBehaviour
{
	[SerializeField] private Toggle toggleAim;
	
	private Button button;
	
	private void OnEnable()
	{
		if(toggleAim) toggleAim.onValueChanged.AddListener(OnToggleAim);
		CharacterSelection.selectionChanged += SelectionChanged;	
	}

	private void OnDisable()
	{
		if(toggleAim) toggleAim.onValueChanged.RemoveListener(OnToggleAim);
		CharacterSelection.selectionChanged -= SelectionChanged;	
	}

	private void SelectionChanged()
	{
		CharacterSelection.TryGetSelectedCharacter(out var selected);
		if (toggleAim) toggleAim.isOn = selected && selected.IsAiming();
	}

	private void OnToggleAim(bool isAiming)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			selected.ToggleAim(isAiming);
		}
	}
}