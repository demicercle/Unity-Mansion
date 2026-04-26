[CreateNodeMenu("Actions/Heal")]
public class NodeHeal : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _character;
	[Input] public float _amount;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.StartInput(this, "_in", () =>
		{
			_amount = GetInputValue<float>("_amount", _amount);
			
			if (_character.TryGetComponent(this, out CharacterController character))
			{
				character.Heal(_amount);
			}
		});
	}
}