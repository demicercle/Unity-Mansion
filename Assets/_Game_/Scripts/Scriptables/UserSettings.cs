using UnityEngine;

[CreateAssetMenu(fileName = "UserSettings", menuName = "UserSettings", order = 0)]
public class UserSettings : ScriptableObject, IDebugGUI
{
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
		DebugGUI.Header("Camera Settings");
		lerpSpeed = (float)DebugGUI.Field(lerpSpeed, "lerpSpeed");
		inputSpeed = (float)DebugGUI.Field(inputSpeed, "inputSpeed");
		panSpeed = (float)DebugGUI.Field(panSpeed, "panSpeed");
		screenPadding = (int)DebugGUI.Field(screenPadding, "screenPadding");
		zoomSpeed = (float)DebugGUI.Field(zoomSpeed, "zoomSpeed");
		rotationSpeed = (Vector2)DebugGUI.Field(rotationSpeed, "rotationSpeed");
		orbitDrag = (float)DebugGUI.Field(orbitDrag, "orbitDrag");
		orbitSpeed = (Vector2)DebugGUI.Field(orbitSpeed, "orbitSpeed");
	}
}