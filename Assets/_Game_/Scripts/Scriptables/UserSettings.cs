using System;
using System.Collections;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "UserSettings", menuName = "UserSettings", order = 0)]
public class UserSettings : ScriptableObject, IDebugGUI
{
	static public UserSettings GetCurrentSettings() => Resources.LoadAll<UserSettings>("").First();
	
	[Header("General")]
	public bool showDebugGUI = true;
	public bool drawDebug = true;
	
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
	//public bool canRotateY = true;

	public void OnDebugGUI()
	{
		DebugGUI.BeginGroup("Debug GUI");
		showDebugGUI = !DebugGUI.Toggle(!showDebugGUI, "<- CLOSE");
		drawDebug = DebugGUI.Toggle(drawDebug, "drawDebug");
		DebugGUI.EndGroup();
		
		DebugGUI.BeginGroup("Camera Settings");
		lerpSpeed = DebugGUI.Field(lerpSpeed, "lerpSpeed");
		inputSpeed = DebugGUI.Field(inputSpeed, "inputSpeed");
		panSpeed = DebugGUI.Field(panSpeed, "panSpeed");
		screenPadding = DebugGUI.Field(screenPadding, "screenPadding");
		zoomSpeed = DebugGUI.Field(zoomSpeed, "zoomSpeed");
		rotationSpeed = DebugGUI.Field(rotationSpeed, "rotationSpeed");
		orbitDrag = DebugGUI.Field(orbitDrag, "orbitDrag");
		orbitSpeed = DebugGUI.Field(orbitSpeed, "orbitSpeed");
		DebugGUI.EndGroup();
	}
}