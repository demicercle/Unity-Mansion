using System;
using UnityEngine;


public class SetParent : MonoBehaviour
{
		[SerializeField] private Transform newParent;
		[SerializeField] private string findTag;
		[SerializeField] private bool worldPositionStays;
		[SerializeField] private bool raycastForGround;
		[SerializeField] private bool snapToNavmesh;
		
		private void Start()
		{
			if(!newParent && !string.IsNullOrEmpty(findTag))
				newParent = GameObject.FindGameObjectWithTag(findTag)?.transform;
			
			transform.SetParent(newParent, worldPositionStays);

			if (raycastForGround)
			{
				if (Physics.Raycast(transform.position, -Vector3.up, out var groundHit, Mathf.Infinity, Layers.MaskGround))
				{
					transform.position = groundHit.point;
				}
			}

			if (snapToNavmesh)
			{
				if (NavUtils.TryGetNearestNavPosition(transform.position, Mathf.Infinity, out var navPosition))
				{
					transform.position = navPosition;
				}
			}
		}
}