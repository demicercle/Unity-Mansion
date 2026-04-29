using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "CameraSettings", order = 0)]
public class CameraSettings : ScriptableObject/*, IDebugGUI*/
{
	static private CameraSettings defaultSettingsInstance;
	static public CameraSettings DefaultSettings
	{
		get
		{
			if(!defaultSettingsInstance) defaultSettingsInstance = Resources.LoadAll<CameraSettings>(Consts.ResPath_Settings).FirstOrDefault();
			if (!defaultSettingsInstance)
			{
				Debug.LogWarning("no CameraSettings found, create new one");
				defaultSettingsInstance = CreateInstance<CameraSettings>();
			}
			return defaultSettingsInstance;
		}
	}

	public bool editorUpdate;
	
	[Space]
	
	[Header("Movement")]
	public float lerpSpeed = 1.5f;
	[Min(0f)]public float cameraFocusLockTime = 1.0f;
	
	[Header("Inputs")]
	public float inputSpeed = 14.0f;
	public int screenPadding = 2;
	public float panSpeed = 8.0f;
	public float zoomSpeed = 1.0f;
	public Vector2 rotationSpeed = new Vector2(3.0f, 6.0f);
	public Vector2 orbitSpeed = new Vector2(3.0f, 6.0f);
	public Vector2 orbitMaxSpeed = new Vector2(-1.0f, -1.0f);
	public float orbitInertia = 6.0f;

	//private bool setDirtyEditor;
	/*public void OnDebugGUI()
	{
		DebugGUI.BeginGroup("Camera Settings");
		lerpSpeed = DebugGUI.Range(lerpSpeed, 0, 16, "lerpSpeed");
		inputSpeed = DebugGUI.Range(inputSpeed, 0, 32, "inputSpeed");
		panSpeed = DebugGUI.Range(panSpeed, 0, 32, "panSpeed");
		screenPadding = DebugGUI.Range(screenPadding, 0, 8, "screenPadding");
		zoomSpeed = DebugGUI.Range(zoomSpeed, 0, 2, "zoomSpeed");
		rotationSpeed.x = DebugGUI.Range(rotationSpeed.x, 0, 4, "rotationSpeedX");
		rotationSpeed.y = DebugGUI.Range(rotationSpeed.y, 0, 32, "rotationSpeedY");
		orbitSpeed.x = DebugGUI.Range(orbitSpeed.x, 0, 1, "orbitSpeedX");
		orbitSpeed.y = DebugGUI.Range(orbitSpeed.y, 0, 32, "orbitSpeedY");
		orbitDrag = DebugGUI.Range(orbitDrag, 0, 1, "orbitDrag");
		DebugGUI.EndGroup();
	}*/
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(CameraSettings))]
public class CameraSettingsEditor : UnityEditor.Editor
{
	private CameraSettings cameraSettings => target as CameraSettings;
	private CameraPivot cameraPivot;
	
	override public void OnInspectorGUI()
	{
		if (cameraSettings.editorUpdate)
		{
			if (!cameraPivot) cameraPivot = FindObjectsByType<CameraPivot>(default).FirstOrDefault();
			if (!cameraPivot)
			{
				GUILayout.Label("no camera found", UnityEditor.EditorStyles.boldLabel);
			}
			else
			{
				cameraPivot.ApplySettings(cameraSettings);
				UnityEditor.EditorUtility.SetDirty(cameraSettings);
			}
		}
		
		base.OnInspectorGUI();
	}
}
#endif