using System.Collections;
using UnityEngine;

[CreateNodeMenu("FlowControl/Destroy Script")]
public class NodeDestroyScript : GameNode
{
	[Input] public NodeExec _in;
	[Input] [Min(0f)] public float _delay = 0f;

	public bool destroyGameObject;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		
		_in.StartInput(this, "_in", () =>
		{
			script.StartCoroutine(WaitAndDestroy());
		});
	}

	private IEnumerator WaitAndDestroy()
	{
		_delay = GetInputValue("_delay", _delay);
		
		if (_delay > 0)
			yield return new WaitForSeconds(_delay);
		else
			yield return null;
		
		script?.DestroyScript(destroyGameObject);
	}
}