using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SubCamera : MonoBehaviour
{
		[SerializeField] private Camera otherCamera;
		[SerializeField] private bool copyPosition, copyRotation, copyFOV;
		private Camera selfCamera;

		private void OnDrawGizmosSelected()
		{
			LateUpdate();
		}

		private void LateUpdate()
		{
			if(!selfCamera) selfCamera = GetComponent<Camera>();
			if(!otherCamera) otherCamera = GetComponentInParent<Camera>();
			if (otherCamera)
			{
				if(copyPosition) selfCamera.transform.position = otherCamera.transform.position;
				if(copyRotation) selfCamera.transform.rotation = otherCamera.transform.rotation;
				if(copyFOV) selfCamera.fieldOfView  = otherCamera.fieldOfView;
			}
		}
}