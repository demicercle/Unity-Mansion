using System.Collections;
using UnityEngine;

[CreateNodeMenu("Actions/Apply Damage Radius")]
public class NodeApplyDamageRadius : GameNode
{
	[Input] public NodeExec _in;
	[Input] public ObjectReference _target;
	[Input] public ObjectReference _instigator;
	[Input] public float _damage;
	[Input] public float _radius;
	
	public AnimationCurve _fallOff = AnimationCurve.EaseInOut(0, 1, 1, 0);
	[Min(0)] public float explosionForce = 0.0f;
	
	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		//_in = GetInputValue("_in", _in);
		_in.InitInput(this, "_in", () =>
		{
			_damage = GetInputValue("_damage", _damage);
			_radius = GetInputValue("_radius", _radius);
			_instigator = GetInputValue("_instigator", _instigator);
			
			_target = GetInputValue("_target", new ObjectReference(Script.gameObject));
			if (!_target.gameObject)
			{
				DebugLogWarning("invalid target");
				return;
			}

			DebugDrawer.WireCapsule(_target.transform.position, _target.transform.rotation, _radius, _radius, Color.red, 1.0f);
			
			foreach (var obj in Physics.OverlapSphere(_target.transform.position, _radius, Layers.MaskInteraction))
			{
				var distance = Vector3.Distance(_target.transform.position, obj.transform.position);
				if (obj.TryGetComponent(out IAttackable attackable))
				{
					var amount = _damage * _fallOff.Evaluate(Mathf.Clamp01(distance / _radius));
					
					attackable.TakeDamage(new DamageInfo()
					{
						amount = amount, isRanged = false
					}, _instigator.TryGetComponent(out CharacterEntity instigatorCharacter) ? instigatorCharacter : null);
				}
			}

			Script.StartCoroutine(DelayedExplosion(_target.transform));
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}

	private IEnumerator DelayedExplosion(Transform transform)
	{
		yield return null;
		
		if (explosionForce > 0)
		{
			foreach (var obj in Physics.OverlapSphere(transform.position, _radius, Layers.MaskPhysics))
			{
				if (!obj.CompareTag(Tags.TagCollide))
					continue;
					
				if (obj.TryGetComponent(out Rigidbody rigidbody))
				{
					DebugLog(this + " AddExplosionForce " + rigidbody + " " + explosionForce);
					rigidbody.isKinematic = false;
					rigidbody.WakeUp();
					rigidbody.AddExplosionForce(explosionForce, transform.position, _radius);
					Utils.GetOrAddComponent<NavmeshRebuilder>(rigidbody.gameObject);
				}
			}
		}
	}
}