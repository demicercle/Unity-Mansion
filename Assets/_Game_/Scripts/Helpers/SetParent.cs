using System;
using UnityEngine;


public class SetParent : MonoBehaviour
{
		[SerializeField] private Transform newParent;
		[SerializeField] private string findTag;
		[SerializeField] private bool worldPositionStays;
		
		private void Start()
		{
			if(!newParent && !string.IsNullOrEmpty(findTag))
				newParent = GameObject.FindGameObjectWithTag(findTag)?.transform;
			
			transform.SetParent(newParent, worldPositionStays);
		}
}