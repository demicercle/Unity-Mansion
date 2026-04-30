using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ReverseToggle : MonoBehaviour
{
	public Toggle toggle;
	public UnityEvent<bool> onValueChanged;
	

	private void Awake()
	{
		if(!toggle) toggle =  GetComponent<Toggle>();
	}

	private void OnEnable()
	{
		toggle.onValueChanged.AddListener(OnValueChanged);
	}

	private void OnDisable()
	{
		toggle.onValueChanged.RemoveListener(OnValueChanged);
	}

	private void OnValueChanged(bool isOn)
	{
		onValueChanged?.Invoke(!isOn);
	}
}