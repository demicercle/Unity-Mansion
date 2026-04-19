using System;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SubCamera : MonoBehaviour
{
		[SerializeField] private Camera otherCamera;
		[SerializeField] private bool copyPosition, copyRotation, copyFOV;
		[SerializeField] private bool resizeRT;
		
		public GameCanvas canvas => GameCanvas.Instance;
		
		private Camera selfCamera;
		private Vector2 screenSize;
		private RenderTexture rtCopy;

		private void CreateRenderTexture()
		{
			if (rtCopy) Destroy(rtCopy);
			rtCopy = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
			Debug.Log(this + " CreateRenderTexture " + rtCopy.width + " " + rtCopy.height);
			selfCamera.targetTexture = rtCopy;
			canvas.SetRenderTexture(rtCopy);
		}

		private void OnDrawGizmosSelected()
		{
			if(!Application.isPlaying)
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
				
				if (Application.isPlaying)
				{
					selfCamera.enabled = otherCamera.enabled;
					
					if (resizeRT && (screenSize != new Vector2(Screen.width, Screen.height) || rtCopy == null))
					{
						screenSize.x =  Screen.width;
						screenSize.y = Screen.height;
						CreateRenderTexture();
					}
				}
			}
		}
}