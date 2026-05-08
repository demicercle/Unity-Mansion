using System;
using UnityEngine;


public class LockRotation : MonoBehaviour
{
	public Vector3 angleScale = new Vector3(1, 0, 1);

	private void OnEnable()
	{
		LateUpdate();
	}

	private void LateUpdate()
	{
		transform.eulerAngles = Vector3.Scale(angleScale, transform.eulerAngles);
	}
}