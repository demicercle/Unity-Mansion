using UnityEngine;

public class MonoDebugLog : MonoBehaviour
{
	public string message;

	public void DebugLogBool(bool value)
	{
		Debug.Log(this + " "  + message + " " + value);
	}
	
	public void DebugLog()
	{
		Debug.Log(this + " "  + message);
	}
}