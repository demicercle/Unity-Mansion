using System;
using UnityEngine;

namespace Game.Gizmos
{
	public class DrawGizmoArrow : MonoBehaviour
	{
		[SerializeField] private bool alwaysDraw = true;
		[SerializeField] private Vector3 offset = Vector3.zero;
		[Range(-1f, 1f)][SerializeField] private float scaleDirection = 1f;
		[SerializeField] private Color color = Color.blue;
		[Min(0f)][SerializeField] private float length = 0.25f;
		[Range(0f, 360f)][SerializeField] private float angle = 20f;

		void OnDrawGizmos()
		{
			if(alwaysDraw) DrawArrow();
		}

		void OnDrawGizmosSelected()
		{
			if(!alwaysDraw)  DrawArrow();
		}
		
		private void DrawArrow()
		{
			Utils.DrawGizmosArrow(transform.TransformPoint(offset), transform.forward * scaleDirection, color, length, angle);
		}
	}
}