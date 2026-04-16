using System;
using System.Collections.Generic;
using Oddworm.Framework;
using UnityEngine;


public class DebugGUI : MonoBehaviour
{
	public static bool DRAW_DEBUG = true;

	static public void WorldLabel(Vector3 position, string message, float duration = 0f)
	{
		var label = new GameObject().AddComponent<GUILabel>();
		label.message = message;
		label.duration = duration;
		label.worldPosition = position;
	}
	
	static public void WireArrow(Vector3 position, Vector3 direction, Vector3 scale, Color color, float duration = 0f)
	{
		DbgDraw.WireArrow(position, Quaternion.LookRotation(direction), scale, color, duration);
	}
	
	static public void Line(Vector3 start, Vector3 end, Color color, float duration = 0f)
	{
		DbgDraw.Line(start, end, color, duration);
	}
	
		static private DebugGUI _instance;
		
		static public void Log(string message)
		{
			Debug.Log("> " + message);
			GetInstance().lines.Add(message);
		}

		static DebugGUI GetInstance()
		{
			if (_instance == null)
			{
				_instance = new GameObject("DebugGUI").AddComponent<DebugGUI>();
			}

			return _instance;
		}

		private List<string> lines = new List<string>();
		
		private void OnGUI()
		{
			
			
			if (lines.Count == 0) return;
			GUILayout.BeginArea(new Rect(Vector2.zero, new Vector2(Screen.width, Screen.height)));
			//GUILayout.BeginVertical("textfield");
			foreach (var line in lines)
			{
				GUILayout.Label(line);
			}
			//GUILayout.EndVertical();
			GUILayout.EndArea();
		}

		private void LateUpdate()
		{
			lines.Clear();
		}
}