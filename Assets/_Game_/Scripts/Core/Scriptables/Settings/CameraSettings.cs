using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "CameraSettings", order = 0)]
public class CameraSettings : ScriptableObject, IDebugGUI
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
	
	[Header("Camera")]
	public float lerpSpeed = 1.5f;
	public float inputSpeed = 10.0f;
	public float panSpeed = 8.0f;
	public int screenPadding = 2;
	//[Range(0f,1f)] public float startZoom = .5f;
	public float zoomSpeed = 1.0f;
	//public float rotationStepAngle = 0.0f;
	public Vector2 rotationSpeed = new Vector2(3.0f, 6.0f);
	public Vector2 orbitSpeed = new Vector2(3.0f, 6.0f);
	public float orbitDrag = 0.1f;
	[Min(0f)]public float cameraFocusLockTime = 1.0f;
	//public bool canRotateY = true;

	//private bool setDirtyEditor;
	public void OnDebugGUI()
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
		
		//if(setDirtyEditor) Utils.SetDirty(this);
	}
}