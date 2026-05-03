using System;
using UnityEngine;


public class DebugBreakAwake : MonoBehaviour
{
	private void Awake()
	{
		Debug.Break();
		Debug.DebugBreak();
	}

	private void Start()
	{
		Destroy(this);
	}
}