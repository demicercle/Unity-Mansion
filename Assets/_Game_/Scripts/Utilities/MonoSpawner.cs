using System;
using System.Collections.Generic;
using UnityEngine;


public class MonoSpawner : MonoBehaviour
{
		public List<GameObject> prefabs = new List<GameObject>();
		public bool instantiateAsChildren;
		public bool spawnAtStart;

		public void SpawnAll()
		{
			foreach (var prefab in prefabs)
			{
				if (!prefab) continue;
				var instance = Instantiate(prefab);
				if (instantiateAsChildren)
					instance.transform.SetParent(transform);
				instance.transform.position = transform.position;
				instance.transform.rotation = transform.rotation;
			}
		}

		private void Start()
		{
			if(spawnAtStart)
				SpawnAll();
		}
		
		#if UNITY_EDITOR
	private string gizmoString;
	private void OnDrawGizmos()
	{
		gizmoString = "";
		foreach (var prefab in prefabs)
		{
			if(!prefab) continue;
			gizmoString += prefab.name + "\n";
		}

		if (!string.IsNullOrWhiteSpace(gizmoString))
			UnityEditor.Handles.Label(transform.position, new GUIContent(gizmoString.Trim('\n')),
				UnityEditor.EditorStyles.centeredGreyMiniLabel);
	}
#endif
}