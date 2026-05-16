using System;
using System.Collections.Generic;
using UnityEngine;


public class CameraTrigger : MonoBehaviour
{
	[SerializeField] private float raycastDistance;
	[SerializeField] private float raycastRadius;
	[SerializeField] private LayerMask raycastMask;

	private List<RoofRenderer> _roofs = new List<RoofRenderer>();
	
	private void FixedUpdate()
	{
		foreach (var roof in _roofs)
			roof.SetBlend(true);

		_roofs.Clear();

		if (raycastRadius * raycastDistance > 0f)
		{
			foreach (var hit in Physics.SphereCastAll(new Ray(transform.position, transform.forward), raycastRadius, raycastDistance, raycastMask))
			{
				if (hit.collider.TryGetComponent(out RoofRenderer roof) && !_roofs.Contains(roof))
				{
					_roofs.Add(roof);
					roof.SetBlend(false);
				}
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * raycastDistance);
		Utils.DrawGizmosCircle(transform.position, transform.forward, raycastRadius);
		Utils.DrawGizmosCircle(transform.position + transform.forward * raycastDistance, transform.forward, raycastRadius);

		foreach (var roof in _roofs)
		{
			if (roof.TryGetComponent(out Renderer rd))
			{
				Gizmos.color = Color.softYellow;
				Gizmos.DrawWireCube(rd.bounds.center, rd.bounds.size);
			}
		}
	}
}