using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ReverseToggle : MonoBehaviour
{
	public Toggle toggle;
	public bool debugLog;
	public UnityEvent<bool> onValueChanged;

	public void SetValue(bool newValue)
	{
		if (toggle)
			toggle.isOn = newValue;
		else 
			OnValueChanged(newValue);
	}

	private void OnEnable()
	{
		if(!toggle) toggle =  GetComponent<Toggle>();
		toggle?.onValueChanged.AddListener(OnValueChanged);
	}

	private void OnDisable()
	{
		toggle?.onValueChanged.RemoveListener(OnValueChanged);
	}

	private void OnValueChanged(bool isOn)
	{
		if(debugLog) Debug.Log(this + " OnValueChanged " + isOn);
		onValueChanged?.Invoke(!isOn);
	}
}