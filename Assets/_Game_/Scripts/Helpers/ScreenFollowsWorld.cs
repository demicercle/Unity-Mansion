using System;
using UnityEngine;


[RequireComponent(typeof(RectTransform))]
public class ScreenFollowsWorld : MonoBehaviour
{
	public Camera mainCamera;
	public Transform target;
	public Vector3 worldOffset;

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
		if(target) rectTransform.anchoredPosition = mainCamera.WorldToScreenPoint(target.position + worldOffset);
	}
}