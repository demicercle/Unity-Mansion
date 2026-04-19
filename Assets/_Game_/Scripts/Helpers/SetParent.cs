using System;
using UnityEngine;


public class SetParent : MonoBehaviour
{
		[SerializeField] private Transform newParent;
		[SerializeField] private bool worldPositionStays;

		private void Start()
		{
			transform.SetParent(newParent, worldPositionStays);
		}
}