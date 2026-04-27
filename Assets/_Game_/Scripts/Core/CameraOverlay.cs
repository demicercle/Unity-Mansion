using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraOverlay : MonoBehaviour
{
	private static Camera mainCamera => Camera.main;
	private Camera selfCamera;
	private RenderTexture rtCopy;

	private void Awake()
	{
		selfCamera = GetComponent<Camera>();
	}

	private IEnumerator Start()
	{
		do
		{
			yield return null;
		} while (GameTurnManager.Instance.IsLoading());

		yield return new WaitForSeconds(FadeScreen.FadeDuration); // avoid rendering conflict
		
		while (true)
		{
			if (mainCamera)
			{
				if (rtCopy == null || 
				    new Vector2(rtCopy.width, rtCopy.height) != new Vector2(Screen.width, Screen.height)) // automatically update render texture if screen size changed
				{
					CreateRenderTexture(); 
					GameCanvas.Instance.SetOverlayRenderTexture(selfCamera.targetTexture);
				}
			}
			
			yield return null;
		}
	}

	private void CreateRenderTexture()
	{
		if (rtCopy) Destroy(rtCopy);
		rtCopy = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
		selfCamera.targetTexture = rtCopy;
		selfCamera.enabled = true;
		Debug.Log(this + " CreateRenderTexture " + rtCopy.width + " " + rtCopy.height);
	}
	
}