using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ReverseToggle : MonoBehaviour
{
	public UnityEvent<bool> onValueChanged;
	
	private Toggle toggle;

	private void Awake()
	{
		toggle =  GetComponent<Toggle>();
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