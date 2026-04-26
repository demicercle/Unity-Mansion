using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class ItemAnimation : MonoBehaviour
{
	[SerializeField] private float duration = 2.0f;
	[SerializeField] private AnimationCurve upCurve;
	[SerializeField] private float upScale = 2.0f;
	[SerializeField] private RawImage iconContainer;
	[SerializeField] private AnimationCurve alphaCurve;
	
	private Vector3 positionFrom, positionTo;
	private float time;
	private RectTransform rectTransform;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
		Inventory.onAnyItemTransfered += OnAnyItemTransfered;
	}

	private void OnDestroy()
	{
		Inventory.onAnyItemTransfered -= OnAnyItemTransfered;
	}

	private void OnAnyItemTransfered(Inventory fromInventory, Inventory toInventory, ItemData itemData)
	{
		var instance = Instantiate(this, transform.parent).GetComponent<ItemAnimation>();
		instance.gameObject.SetActive(true);
		instance.positionFrom = fromInventory.GetPosition();
		instance.positionTo = toInventory.GetPosition();
		instance.iconContainer.texture = itemData.GetIcon();
		instance.time = duration;
	}

	IEnumerator Start()
	{
		if (time <= 0)
		{
			gameObject.SetActive(false);
			yield break;
		}
		
		while (time > 0f)
		{
			var worldPosition = Vector3.Lerp(positionTo, positionFrom, time / duration);
			worldPosition.y += upScale * upCurve.Evaluate(1f - time / duration);
			var screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
			rectTransform.anchoredPosition = screenPosition;
			rectTransform.SetAsLastSibling();
			iconContainer.color = Utils.ColorAlpha(Color.white, alphaCurve.Evaluate(1f - time / duration));
			yield return null;
			time -= Time.deltaTime;
		}

		Destroy(gameObject);
	}
}