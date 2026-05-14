using UnityEngine;

[CreateNodeMenu("Actions/Emit Noise")]
public class NodeEmitNoise : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _position;
	[Input] public SceneReference _instigator;
	
	public int rayCount = 16;
	public float rayLength = 6.0f;
	public float radius = 0.1f;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_position = GetInputValue("_position", _position);
			if (!_position.TryGetComponent(this, out Transform transform))
				DebugLogWarning("No position found");
			
			_instigator = GetInputValue("_instigator", _instigator);
			if (!_instigator.TryGetComponent(this, out CharacterEntity characterInstigator))
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