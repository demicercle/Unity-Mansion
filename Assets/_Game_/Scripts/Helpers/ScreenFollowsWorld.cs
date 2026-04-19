using System;
using UnityEngine;


public class ScreenFollowsWorld : MonoBehaviour
{
	public Camera mainCamera;
	public Transform target;

	private void Awake()
	{
		if(!mainCamera) mainCamera = Camera.main;
	}

	private void Update()
	{
		if(target) transform.position = mainCamera.WorldToScreenPoint(target.position);
	}
}