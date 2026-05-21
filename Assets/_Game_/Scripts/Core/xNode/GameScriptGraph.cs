using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

[CreateAssetMenu]
public class GameScriptGraph : NodeGraph
{
	public GameScriptGraph Duplicate()
	{
		return Instantiate(this);
	}
	
	public override void Clear()
	{
		nodes.Clear();
	}
}
