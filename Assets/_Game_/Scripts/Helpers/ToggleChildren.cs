using System;
using UnityEngine;


public class ToggleChildren : MonoBehaviour
{
	[SerializeField] private int currentIndex;
	[SerializeField] private GameObject[] children;
	
	[ContextMenu("Next")]
	public void Next()
	{
		currentIndex = (currentIndex + 1) % children.Length;
		ForceUpdate();
	}

	[ContextMenu("ForceUpdate")]
	public void ForceUpdate()
	{
		//Debug.Log(this + " ForceUpdate: " + currentIndex);
		for (int i = 0; i < children.Length; i++)
			children[i].SetActive(i == currentIndex);
	}

	void Start()
	{
		ForceUpdate();
	}
}