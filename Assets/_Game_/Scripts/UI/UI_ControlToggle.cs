using System;
using UnityEngine;
using UnityEngine.UI;


public class UI_ControlToggle : MonoBehaviour
{
	public Toggle toggle;
	public Button actionButton_Run;

	private void OnDestroy()
	{
		if (toggle)
		{
			toggle.onValueChanged.RemoveAllListeners();
		}
	}

	void LateUpdate() // todo
	{
		if (!toggle)
		{
			toggle = new GameObject("ControlToggle").AddComponent<Toggle>();
			toggle.onValueChanged.AddListener(isOn => gameObject.SetActive(isOn));
		}
		
		var view = GameView.GetInstance();
		//var selectedCharacter = view.GetSelectedCharacter();
		var gameController = view.GetController();
		var selectedCharacter = view.GetSelectedCharacter();
		var isVisible = !view.HasCoroutines() 
		                      && gameController.IsPlayerTurn() 
		                      && selectedCharacter != null
		                      && selectedCharacter.IsControllable();

		toggle.isOn = isVisible;
		
		if (selectedCharacter)
		{
			actionButton_Run.interactable = selectedCharacter.CanUseRunAction();
		}
	}
}