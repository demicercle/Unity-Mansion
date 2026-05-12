using System;
using UnityEngine;


public class DrawGizmoBox : MonoBehaviour
{
	public Color color = Color.cyan;
	public Vector3 size = Vector3.one;
	public Vector3 center = Vector3.zero;

	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		Gizmos.DrawWireCube(transform.TransformPoint(center), transform.TransformVector(size));
	}
}