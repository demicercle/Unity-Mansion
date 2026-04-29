using System;
using UnityEngine;


[RequireComponent(typeof(RectTransform))]
public class ScreenFollowsWorld : MonoBehaviour
{
	public Camera mainCamera;
	public Transform target;
	public Vector3 worldOffset;
	public Vector2 screenSize;

	public ScreenFollowsWorld SetTarget(Transform newTarget)
	{
		target = newTarget;
		return this;
	}
	
	public ScreenFollowsWorld SetWorldOffset(Vector3 world)
	{
		worldOffset = world;
		return this;
	}

	private RectTransform rectTransform;
	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
		if(!mainCamera) mainCamera = Camera.main;
	}

	private void Update()
	{
		screenSize = GameCanvas.Instance.referenceResolution;
		
		if (target)
		{
			rectTransform.anchoredPosition = mainCamera.WorldToViewportPoint(target.position + worldOffset) * screenSize;
		}
		else
		{
			rectTransform.anchoredPosition = mainCamera.WorldToViewportPoint(worldOffset) * screenSize;
		}
	}
}