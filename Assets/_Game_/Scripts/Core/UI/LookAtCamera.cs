using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
	[SerializeField] private new Camera camera;

	private Canvas canvas;
	
	private void LateUpdate()
	{
		if (!camera) camera = Camera.main;
		if (camera) transform.LookAt(camera.transform);
	}
}