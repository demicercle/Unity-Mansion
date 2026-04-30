using UnityEngine;
using UnityEngine.Events;


public class ToggleExtensions : MonoBehaviour
{
	public UnityEvent<bool> onValueChanged;

	public void OnFloatChanged(float value) => onValueChanged?.Invoke(value > 0f);
}