using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateAssetMenu]
public class GameScriptGraph : NodeGraph
{
	public override void Clear()
	{
		/*if (Application.isPlaying) {
			for (int i = 0; i < nodes.Count; i++) {
				if (nodes[i] != null) Destroy(nodes[i]);
			}
		}*/
		nodes.Clear();
	}
}
