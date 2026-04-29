using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LoadGameButton : MonoBehaviour
{
	private void LateUpdate()
	{
		GetComponent<Button>().interactable = GameSave.SaveExists() && !GameSave.IsDisabled();
	}
}