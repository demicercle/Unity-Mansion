using System;
using System.Collections;
using System.Linq;
using Oddworm.Framework;
using UnityEngine;

public class DebugGUI : MonoBehaviour
{
	public static bool DRAW_DEBUG = true;

	static public DebugGUI FloatingText(Vector3 position, string message, float duration = 1.0f, float off = 1.0f)
	{
		var label = new GameObject().AddComponent<DebugGUI>();
		label.duration = duration;
		label.worldPosition = position;
		label.offset = Vector3.up * off;
		label.onGUI += () =>
		{
			GUILayout.Label(message);
		};
		return label;
	}
	
	static public void WireArrow(Vector3 position, Vector3 direction, Vector3 scale, Color color, float duration = 0f)
	{
		if (!DRAW_DEBUG) return;
		DbgDraw.WireArrow(position, Quaternion.LookRotation(direction), scale, color, duration);
	}
	
	static public void Path(Vector3[] path, bool closedLoop, Color color, float duration = 0f)
	{
		if (!DRAW_DEBUG) return;
		if (path.Length <= 0)
			return;
		
		for (int i = 0; i < path.Length - 1; i++)
		{
			var p0 = path[i];
			var p1 = path[i + 1];
			Line(p0, p1, color, duration);
		}
		
		if (closedLoop && path.Length > 2)
			Line(path.Last(), path.First(), color, duration);
	}
	
	static public void Disc(Vector3 position, Vector3 direction, float scale, Color color, float duration = 0f)
	{
		if (!DRAW_DEBUG) return;
		DbgDraw.WireDisc(position, Quaternion.LookRotation(direction), scale, color, duration);
	}
	
	static public void Line(Vector3 start, Vector3 end, Color color, float duration = 0f)
	{
		if (!DRAW_DEBUG) return;
		DbgDraw.Line(start, end, color, duration);
	}
		public bool destroyGameObject = true;
		public float duration = 0.0f;
		public Transform target;
		public Vector2 size = Vector2.one * 100;
		public Vector3 worldPosition = Vector3.zero;
		public Vector2 offset = Vector2.zero;

		private IEnumerator Start()
		{
			if (duration < 0)
				yield break;

			if (target != null)
			{
				transform.SetParent(target);
				transform.localPosition = worldPosition;
			}
			else
			{
				transform.position = worldPosition;
			}
		
			if (duration > 0f)
				yield return new WaitForSeconds(duration);
			else
				yield return new WaitForEndOfFrame();
		
			if (destroyGameObject) Destroy(gameObject);
		}

		public System.Action onGUI;

		private void LateUpdate()
		{
			transform.position += (Vector3)offset * Time.unscaledDeltaTime;
		}

		private void OnGUI()
		{
			worldPosition = Camera.main.WorldToScreenPoint(transform.position);
			GUILayout.BeginArea(new Rect(worldPosition.x, Screen.height - worldPosition.y, size.x, size.y));
			onGUI?.Invoke();
			GUILayout.EndArea();
		}
}