using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleSelf : MonoBehaviour
{
	private Toggle selfToggle;
	
	private void Awake()
	{
		selfToggle = GetComponent<Toggle>();
		selfToggle.onValueChanged.AddListener(OnToggleValueChanged);
	}
	
	private void OnDestroy()
	{
		selfToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
	}

	private void OnToggleValueChanged(bool isOn)
	{
		gameObject.SetActive(isOn);
	}
}