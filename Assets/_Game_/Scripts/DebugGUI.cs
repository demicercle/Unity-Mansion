using System;
using System.Collections.Generic;
using UnityEngine;


public class DebugGUI : MonoBehaviour
{
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