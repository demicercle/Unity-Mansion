using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class NotSelectable : MonoBehaviour
{
	private EventSystem eventSystem => EventSystem.current;
	private Selectable selectable;

	private void Awake()
	{
		selectable = GetComponent<Selectable>();
	}

	private void Update()
	{
		if (eventSystem.currentSelectedGameObject == selectable.gameObject)
		{
			eventSystem.SetSelectedGameObject(null);
		}
	}
}