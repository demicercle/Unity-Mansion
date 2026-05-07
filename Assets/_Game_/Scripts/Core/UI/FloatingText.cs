using System;
using TMPro;
using UnityEngine;


public class FloatingText : MonoBehaviour
{
	[SerializeField] private TMP_Text textComponent;
	
	public void Clone(Transform target, string text = null)
	{
		var clone = Instantiate(gameObject, transform.parent).GetComponent<FloatingText>();
		if(!string.IsNullOrEmpty(text)) clone.textComponent.text = text;
		clone.followTarget = target;
		clone.gameObject.SetActive(true);
	}

	public void Clone(Vector3 world, string text = null)
	{
		var clone = Instantiate(gameObject, transform.parent).GetComponent<FloatingText>();
		if(!string.IsNullOrEmpty(text)) clone.textComponent.text = text;
		clone.worldPosition = world;
		clone.gameObject.SetActive(true);
	}
	
	private Transform followTarget;
	private Vector3 worldPosition;

	private void LateUpdate()
	{
		if(followTarget)
			worldPosition = followTarget.position;
		
		transform.position = Camera.main.WorldToScreenPoint(worldPosition);
	}
}