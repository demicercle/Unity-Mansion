using System;
using UnityEngine;


public class DrawGizmoCircle : MonoBehaviour
{
	public Color color = Color.cyan;
	public float radius = 2.0f;

	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		Utils.DrawGizmosCircle(transform.position, transform.up, radius);
	}
}