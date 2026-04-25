using UnityEngine;
using XNode;

public abstract class GameNode : Node
{
	protected override void Init()
	{
		if(!Application.isPlaying)
			name = name.Replace("Node", string.Empty).Trim(' ');
		
		base.Init();
	}

	protected void DebugLogThis(string msg) => Debug.Log("x_" + this + ": " + msg);
}