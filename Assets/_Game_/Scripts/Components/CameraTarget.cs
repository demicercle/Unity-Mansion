using System;
using System.Linq;
using UnityEngine;

public class CameraTarget : MonoBehaviour
{
	
#if UNITY_EDITOR
	[ContextMenu("Move Editor Camera")]
	void MoveEditorCamera()
	{
		if (!topDownCamera) topDownCamera = FindObjectsByType<TopDownCamera>(default).FirstOrDefault();
		if (!topDownCamera) return;
		var sceneView = UnityEditor.SceneView.lastActiveSceneView;
		//var editorCamera = sceneView.camera;
		//editorCamera.transform.position = topDownCamera.transform.position;
		//editorCamera.transform.rotation = topDownCamera.transform.rotation;
		//Utils.SetDirty(editorCamera.transform);
		sceneView.pivot = topDownCamera.transform.position;
		sceneView.rotation = topDownCamera.transform.rotation;
		sceneView.Repaint();
	}
#endif
	
	[Header("Editor")]
	[ReadOnly] public bool editorOnly = true;
	public bool moveEditorCamera = false;
	
	[Header("Game")]
	public bool moveGameCamera = false;
	[Range(0f,1f)] [SerializeField] private float gameCameraZoom = 0f;
	
	private TopDownCamera topDownCamera;
	private void OnDrawGizmosSelected()
	{
		if (editorOnly && (Application.isPlaying || !Application.isEditor)) return;
		if (moveGameCamera)
		{
			if (!topDownCamera) topDownCamera = FindObjectsByType<TopDownCamera>(default).FirstOrDefault();
			if (!topDownCamera) return;
			transform.position = Vector3.Scale(new Vector3(1, 0, 1), transform.position);
			topDownCamera.SetFocusPosition(transform.position, false);
			topDownCamera.UpdatePosition(gameCameraZoom);
		}

		if (moveEditorCamera)
		{
			MoveEditorCamera();
		}
	}

	private void Awake()
	{
		if (editorOnly && (Application.isPlaying || !Application.isEditor)) Destroy(gameObject);
	}
}