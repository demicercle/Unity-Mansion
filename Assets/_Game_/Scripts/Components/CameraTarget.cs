using System;
using System.Linq;
using UnityEngine;

public class CameraTarget : MonoBehaviour
{
	public bool editorOnly = true;

	private TopDownCamera topDownCamera;
	private void OnDrawGizmosSelected()
	{
		if (editorOnly && (Application.isPlaying || !Application.isEditor)) return;
		if (!topDownCamera) topDownCamera = FindObjectsByType<TopDownCamera>(default).FirstOrDefault();
		if (!topDownCamera) return;
		transform.position = Vector3.Scale(new Vector3(1, 0, 1), transform.position);
		topDownCamera.SetFocusPosition(transform.position);
	}

	private void Awake()
	{
		if (editorOnly && (Application.isPlaying || !Application.isEditor)) Destroy(gameObject);
	}
}