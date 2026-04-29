using UnityEngine;

[CreateNodeMenu("Actions/Apply Damage Radius")]
public class NodeApplyDamageRadius : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _position;
	[Input] public float _damage;
	[Input] public float _radius;
	
	public AnimationCurve _fallOff = AnimationCurve.EaseInOut(0, 1, 1, 0);
	[Min(0)] public float explosionForce = 0.0f;
	
	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.StartInput(this, "_in", () =>
		{
			
			_damage = GetInputValue("_damage", _damage);
			_radius = GetInputValue("_radius", _radius);
			_position = GetInputValue("_position", _position);
			
			if (!_position.TryGetComponent(this, out Transform transform))
			{
				DebugWarningThis("invalid position");
				return;
			}

			DebugDrawer.WireCapsule(transform.position, transform.rotation, _radius, _radius, Color.red, 1.0f);
			
			foreach (var obj in Physics.OverlapSphere(transform.position, _radius, Layers.MaskInteraction))
			{
				var distance = Vector3.Distance(transform.position, obj.transform.position);
				if (obj.TryGetComponent(out IAttackable attackable))
				{
					var amount = _damage * _fallOff.Evaluate(Mathf.Clamp01(distance / _radius));
					
					attackable.TakeDamage(new DamageInfo()
					{
						amount = amount, isRanged = false
					}, script.gameObject);
				}
			}

			if (explosionForce > 0)
			{
				foreach (var obj in Physics.OverlapSphere(transform.position, _radius, Layers.MaskPhysics))
				{
					if (!obj.CompareTag(Tags.TagCollide))
						continue;
					
					if (obj.TryGetComponent(out Rigidbody rigidbody))
					{
						DebugLogThis(this + " AddExplosionForce " + rigidbody + " " + explosionForce);
						rigidbody.isKinematic = false;
						rigidbody.WakeUp();
						rigidbody.AddExplosionForce(explosionForce, transform.position, _radius);
					}
				}
			}
		});
	}
}