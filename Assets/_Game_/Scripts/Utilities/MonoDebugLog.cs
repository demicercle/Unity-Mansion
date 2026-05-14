using System;
using UnityEngine;

public class MonoDebugLog : MonoBehaviour
{
	public string message;
	public bool logOnEnable;

	private void OnEnable()
	{
		if(logOnEnable) Debug.Log(message);
	}

	public void DebugLogBool(bool value)
	{
		Debug.Log(this + " "  + message + " " + value);
	}
	
	public void DebugLog()
	{
		Debug.Log(this + " "  + message);
	}
}