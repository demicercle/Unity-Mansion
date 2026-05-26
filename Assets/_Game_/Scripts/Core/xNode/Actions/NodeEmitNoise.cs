using UnityEngine;

[CreateNodeMenu("Actions/Emit Noise")]
public class NodeEmitNoise : GameNode
{
	[Input] public NodeExec _in;
	[Input] public ObjectReference _target;
	[Input] public ObjectReference _instigator;
	
	public int rayCount = 16;
	public float rayLength = 6.0f;
	public float radius = 0.1f;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_target = GetInputValue("_target", new ObjectReference(Script.gameObject));
			if (!_target.TryGetComponent(out Transform transform))
				DebugLogWarning("No position found");
			
			_instigator = GetInputValue("_instigator", new ObjectReference(Script.gameObject));
			if (!_instigator.TryGetComponent(out CharacterEntity characterInstigator))
				DebugLogWarning("No instigator found");
			
			if (transform)
			{
				var noiseEmitter = new GameObject("NoiseEmitter").AddComponent<NoiseTrigger>();
				noiseEmitter.transform.position = transform.position;
				noiseEmitter.characterInstigator = characterInstigator;
				noiseEmitter.rayCount = rayCount;
				noiseEmitter.rayLength = rayLength;
				noiseEmitter.radius = radius;
				noiseEmitter.Emit();

				Utils.GetOrAddComponent<AutoDestroy>(noiseEmitter.gameObject).delay = 1.0f;
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}