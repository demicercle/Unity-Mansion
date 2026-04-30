using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ReverseToggle : MonoBehaviour
{
	public Toggle toggle;
	public UnityEvent<bool> onValueChanged;

	public void SetValue(bool newValue) => onValueChanged?.Invoke(!newValue);

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
		onValueChanged?.Invoke(!isOn);
	}
}