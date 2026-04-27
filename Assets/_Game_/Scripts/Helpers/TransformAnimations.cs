using UnityEngine;

public static class TransformAnimations
{
	static public void BounceRealtime(Transform transform, bool bouncing = true) =>
		BounceRealtime(transform, Mathf.PI, bouncing);
	static public void BounceRealtime(Transform transform, float speed, bool bouncing = true) =>
		Bounce(transform, bouncing ? Mathf.PingPong(Time.realtimeSinceStartup * speed, 1f) : 1f);
	static public void Bounce(Transform transform, float value, float speed = 1f)
	{
		transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.0f, value);
	}
}