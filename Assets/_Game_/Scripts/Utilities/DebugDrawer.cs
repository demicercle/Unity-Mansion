using System;
using System.Collections;
using System.Linq;
using Oddworm.Framework;
using UnityEngine;

public class DebugDrawer : MonoBehaviour
{
	//private static bool shouldDraw => (GameInput.debugMode + CharacterDetection.debugMode > 0);
	private static bool shouldDraw => true;

	static public DebugDrawer FloatingText(Vector3 position, string message, float duration = 1.0f, float speedY = 1.0f)
	{
		return WorldLabel(position, message, duration, speedY);
	}

	static public DebugDrawer WorldLabel(Vector3 position, string message, float duration = 0f, float speedY = 0f)
	{
		if (!shouldDraw) return null;
		var label = new GameObject().AddComponent<DebugDrawer>();
		label.duration = duration;
		label.worldPosition = position;
		label.worldOffset = Vector3.Lerp(Camera.main.transform.forward, Camera.main.transform.up, 0.9f) * speedY;
		label.onGUI += () =>
		{
			GUILayout.Label(message);
		};
		return label;
	}

	static public void WireCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color, float duration = 0f)
	{
		if (!shouldDraw) return;
		DbgDraw.WireCapsule(position,rotation,radius,height,color,duration);
	}
	
	static public void WireArrow(Vector3 position, Vector3 direction, Vector3 scale, Color color, float duration = 0f)
	{
		if (!shouldDraw) return;
		DbgDraw.WireArrow(position, Quaternion.LookRotation(direction), scale, color, duration);
	}
	
	static public void Path(Vector3[] path, bool closedLoop, Color color, float duration = 0f)
	{
		if (!shouldDraw) return;
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
	
	static public void WireDisc(Vector3 position, Vector3 direction, float scale, Color color, float duration = 0f)
	{
		if (!shouldDraw) return;
		DbgDraw.WireDisc(position, Quaternion.LookRotation(direction), scale, color, duration);
	}
	
	static public void WireDBox(Vector3 position, Quaternion rot, Vector3 size, Color color, float duration = 0f)
	{
		if (!shouldDraw) return;
		DbgDraw.WireCube(position, rot, size, color, duration);
	}
	
	static public void Line(Vector3 start, Vector3 end, Color color, float duration = 0f)
	{
		if (!shouldDraw) return;
		DbgDraw.Line(start, end, color, duration);
	}
	
	public bool destroyGameObject = true;
	public float duration = 0.0f;
	public Transform target;
	public Vector2 size = Vector2.one * 100;
	public Vector3 worldPosition = Vector3.zero;
	public Vector3 worldOffset = Vector3.zero;

	private Vector3 screenPosition;
	private System.Action onGUI;
	
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
	
	private void LateUpdate()
	{
		worldPosition += worldOffset * Time.unscaledDeltaTime;
		transform.position = worldPosition;
		screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
	}
	
	private void OnGUI()
	{
		GUILayout.BeginArea(new Rect(screenPosition.x, Screen.height - screenPosition.y, size.x, size.y));
		onGUI?.Invoke();
		GUILayout.EndArea();
	}
}