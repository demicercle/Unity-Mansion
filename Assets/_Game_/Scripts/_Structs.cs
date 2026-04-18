using UnityEngine;

[System.Serializable]
public struct RangeF
{
	public float min;
	public float max;
	public float Evaluate(float clamp01) => Mathf.Lerp(min, max, clamp01);
}