using System;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class WorldCanvas : MonoBehaviour
{
	[SerializeField] private Vector3 lookAtAxis = Vector3.up;

	private Canvas canvas;
	
	private void LateUpdate()
	{
		if(!canvas) canvas = GetComponent<Canvas>();
		canvas.worldCamera = Camera.main;

		canvas.transform.eulerAngles = Vector3.Scale(lookAtAxis, Camera.main.transform.eulerAngles);
	}
}