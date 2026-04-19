using UnityEngine;

[CreateAssetMenu(fileName = "UserSettings", menuName = "UserSettings", order = 0)]
public class UserSettings : ScriptableObject
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
}