using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DetectionTrigger : MonoBehaviour
{
	static public int debugMode { get; set; }

	public static System.Action<CharacterController,CharacterController,bool> onDetectionChanged;
	
	public System.Action<CharacterController> onCharacterDetected;
	public System.Action<CharacterController> onCharacterLost;
	
	[SerializeField] private CharacterController selfCharacter;
	[SerializeField][Range(0f, 16f)] private float hearingDistance = 3.0f;
	[SerializeField][Range(0f, 32f)] private float seeingDistance = 3.0f;
	[SerializeField][Range(0f, 360f)] private float maxAngle = 30f;
	[SerializeField][Min(-1f)] private float looseTargetTime = -1f;
	[SerializeField][Min(0)] private float tickTime = 0.5f;
	[SerializeField] private bool noLOS = false;

	public bool IsCharacterDetected(CharacterController character) => detectedCharacters.ContainsKey(character);

	public bool TryGetLastKnownPosition(CharacterController character, out Vector3 lastKnownPosition)
	{
		if (detectedCharacters.ContainsKey(character))
		{
			lastKnownPosition = (Vector3)detectedCharacters[character];
			if (Time.time - lastKnownPosition.z < looseTargetTime)
				return true;
		}

		lastKnownPosition = default;
		return false;
	}

	public CharacterController[] GetDetectedCharacters() => detectedCharacters.Keys.ToArray();
	
	#region PRIVATE

	private bool detectionUsesTime => tickTime > 0f || debugMode > 2;
	
	private string debugString;
	private Dictionary<CharacterController, Vector4> detectedCharacters = new Dictionary<CharacterController, Vector4>(); //Vector4 z is time TODO

	private void Awake()
	{
		if (!selfCharacter)
		{
			Debug.LogError(selfCharacter + " needs a CharacterController");
			enabled = false;
			return;
		}

		//selfCharacter.onMoved += OnSelfMoved;
		CharacterController.onCharacterMoved += OnCharacterMoved;
		CharacterController.onCharacterUsesAction += OnCharacterUsesAction;
	}

	private void OnDestroy()
	{
		//selfCharacter.onMoved -= OnSelfMoved;
		CharacterController.onCharacterMoved -= OnCharacterMoved;
		CharacterController.onCharacterUsesAction -= OnCharacterUsesAction;
	}

	/*private void OnEnable()
	{
		if (detectionUsesTime)
			StartCoroutine(Ticks());
	}*/

	/*private void OnDisable()
	{
		StopAllCoroutines();
	}*/

	/*private IEnumerator Ticks()
	{
		while (true)
		{
			DoUpdate();
			
			if (tickTime > 0) 
				yield return new WaitForSeconds(tickTime);
			else 
				yield return new WaitForEndOfFrame();
		}
	}*/

	private void OnCharacterUsesAction(CharacterController otherCharacter, CharacterAction actionType)
	{
		//DoUpdate();
	}

	private void OnCharacterMoved(CharacterController otherCharacter)
	{
		//DoUpdate();

		if (otherCharacter == selfCharacter)
		{
			SphereDetection();
		}
		else
		{
			//if (debugMode > 0)
			//	DebugDrawer.Line(selfCharacter.GetEyesPosition(), otherCharacter.GetCenterPosition(), Color.magenta);
			
			if (detectedCharacters.ContainsKey(otherCharacter))
			{
				if (!IsCharacterVisible(otherCharacter))
				{
					Debug.Log(this + " OnCharacterMoved " + selfCharacter + " looses " + otherCharacter);
					detectedCharacters.Remove(otherCharacter);
					//DebugDrawCapsule(otherCharacter, Color.gray, 0.5f);
				}
			}
			else
			{
				if (IsCharacterVisible(otherCharacter))
				{
					Debug.Log(this + " OnCharacterMoved " + selfCharacter + " detects " + otherCharacter);
					detectedCharacters.Add(otherCharacter, (Vector4)otherCharacter.GetPosition());
					//DebugDrawCapsule(otherCharacter, Color.magenta, 0.5f);
				}
			}
		}
	}

	private void SphereDetection()
	{
		//if (debugMode > 0) DebugDrawer.WireDisc(selfCharacter.GetPosition(), Vector3.forward, hearingDistance, Color.yellow);
		//if (debugMode > 0) DebugDrawer.WireDisc(selfCharacter.GetPosition(), Vector3.forward, seeingDistance, Color.magenta);
		foreach (var coll in Physics.OverlapSphere(selfCharacter.GetPosition(),
			         Mathf.Max(hearingDistance, seeingDistance), LayerMask.NameToLayer(Consts.LAYER_CHARACTER)))
		{
			if (coll.TryGetComponent(out CharacterController otherCharacter))
			{
				if (otherCharacter == selfCharacter) continue;
				if (otherCharacter.IsDead()) continue;

				OnCharacterMoved(otherCharacter);
			}
		}
	}

	/*private void DoUpdate()
	{
		debugString = "";
		foreach (var otherCharacter in CharacterController.AllCharacters)
		{
			if (otherCharacter == selfCharacter) continue;
			if (detectedCharacters.ContainsKey(otherCharacter)) // character already detected
			{
				var lastKnownPosition = detectedCharacters[otherCharacter];

				if (!string.IsNullOrEmpty(debugString)) debugString += "\n";
				debugString += "\n>" + otherCharacter.name + " " + (Time.time - lastKnownPosition.z).ToString("N2");

				if (IsCharacterVisible(otherCharacter))
				{
					// still visible
					lastKnownPosition.z = Time.time;
					detectedCharacters[otherCharacter] = lastKnownPosition;

					if (debugMode > 0) // draw lines
					{
						var factionColor = ColorSettings.GetCharacterColor(selfCharacter, false, false);
						DebugDrawer.Line(selfCharacter.GetEyesPosition(), otherCharacter.GetCenterPosition(),
							factionColor, debugDrawTime);
						DebugDrawCapsule(otherCharacter, Utils.ColorScale(factionColor, Mathf.PI * 0.1f), debugDrawTime);
					}
				}
				else
				{
					if (!detectionUsesTime)
						lastKnownPosition.z = Time.time;

					if (looseTargetTime > 0 && Time.time - lastKnownPosition.z >= looseTargetTime)
					{
						Debug.Log(selfCharacter + " forgets " + otherCharacter); // forgets
						detectedCharacters.Remove(otherCharacter);
						onCharacterLost?.Invoke(otherCharacter);
						onDetectionChanged?.Invoke(selfCharacter, otherCharacter, false);

						if (debugMode > 0)
						{
							DebugDrawCapsule(otherCharacter, Color.magenta, Mathf.PI);
						}
					}
					else
					{
						detectedCharacters[otherCharacter] = lastKnownPosition; //set detection time

						if (debugMode > 0) // draw lines with time label
						{
							DebugDrawer.Line(selfCharacter.GetEyesPosition(), otherCharacter.GetPosition(), Color.gray,
								debugDrawTime);
							DebugDrawer.WireDisc((Vector3)lastKnownPosition, Vector3.up, otherCharacter.GetRadius(),
								Color.yellow, debugDrawTime);
						}
					}
				}
			}

			else
			{
				var eyesPosition = selfCharacter.GetEyesPosition();
				var otherPosition = otherCharacter.GetPosition();
				var otherCenter = otherCharacter.GetCenterPosition();

				if (IsCharacterVisible(otherCharacter))
				{
					var value = (Vector4)otherPosition;
					value.z = Time.time;
					detectedCharacters.Add(otherCharacter, value);
					onCharacterDetected?.Invoke(otherCharacter);
					onDetectionChanged?.Invoke(selfCharacter, otherCharacter, true);
			
					Debug.Log(selfCharacter + " detects " + otherCharacter);

					if (debugMode > 0)
					{
						var factionColor = ColorSettings.GetCharacterColor(selfCharacter, false, false);
						DebugDrawCapsule(otherCharacter, Utils.ColorScale(ColorSettings.GetCharacterColor(otherCharacter), Mathf.PI * 0.1f), Mathf.PI);
						DebugDrawer.Line(eyesPosition, otherCenter, factionColor, Mathf.PI * 0.1f);
					}
				}
			}
			
		}
	}*/

	private bool IsCharacterVisible(CharacterController otherCharacter)
	{
		if (selfCharacter.IsPlayer() && otherCharacter.IsPlayer()) return true;
		
		var selfPosition = selfCharacter.GetPosition();
		var otherPosition = otherCharacter.GetPosition();
		
		var distance = Vector3.Distance(selfPosition, otherPosition);
		if (selfCharacter.IsPlayer())
		{
			return distance <= Mathf.Max(seeingDistance, hearingDistance) && otherCharacter.IsCharacterRevealed(); // characters player will see far and behind walls
		}
		
		if (distance <= seeingDistance)
		{
			if (LOSCheck(otherCharacter))
			{
				if (distance <= hearingDistance)
					return true;

				if (AngleCheck(otherCharacter))
					return true;
			}
		}

		return false;
	}

	private bool AngleCheck(CharacterController other)
	{
		if (maxAngle <= 0f) return false;
		if (maxAngle >= 360f) return true;
		
		Vector3 worldPosition = other.GetPosition();
		var direction = (worldPosition - selfCharacter.GetPosition());
		direction.y = 0;
		direction.Normalize();
		
		var forward = transform.forward;
		forward.y = 0;
		forward.Normalize();
		
		var angle = Vector3.Angle(direction, forward);
		//Debug.Log(selfCharacter + " angle to " + other + " = " + angle);
		//if (debugMode > 0) Utils.DrawWorldGUI(worldPosition, angle.ToString("N2"));
		
		return angle <= maxAngle;
	}
	
	private bool LOSCheck(CharacterController otherCharacter)
	{
		if (noLOS) return true;
		Vector3 eyesPos = selfCharacter.GetEyesPosition();
		Vector3 otherPos =  otherCharacter.GetCenterPosition();
		if(Physics.Linecast(eyesPos, otherPos, out var hit,
			   LayerMask.GetMask(Consts.LAYER_WALL, Consts.LAYER_GROUND)))
		{
			if (debugMode > 0)
			{
				DebugDrawer.Line(eyesPos, otherPos, Color.gray);
				DebugDrawer.Line(eyesPos, hit.point, Color.red);
				Debug.Log(otherCharacter + " hit " + hit.collider);
			}
			
			return false;
		}
		
		if (debugMode > 0)
		{
			DebugDrawer.Line(eyesPos, otherPos, Color.green);
		}

		return true;
	}

	private void DebugDrawCapsule(CharacterController otherCharacter, Color color, float duration)
	{
		if (debugMode <= 0) return;
		var eyesPosition = selfCharacter.GetEyesPosition();
		var otherCenter = otherCharacter.GetCenterPosition();
		DebugDrawer.Line(eyesPosition, otherCenter, color, duration);
		DebugDrawer.WireCapsule(otherCenter, Quaternion.identity, otherCharacter.GetRadius(),
			otherCharacter.GetHeight(), color, duration);
	}
	
	const float debugDrawTime = 0.00001f;
	#endregion
	
	#if UNITY_EDITOR
	private void LateUpdate()
	{
		if (debugMode > 0)
		{
			var center = transform.position;
			
			DebugDrawer.WireDisc(center, transform.forward, hearingDistance, Color.yellow);
			
			Gizmos.color = Color.magenta;
			if (maxAngle >= 360f)
			{
				DebugDrawer.WireDisc(center, transform.forward, seeingDistance, Color.yellow);
				return;
			}

			var lineColor = Utils.ColorScale(ColorSettings.GetCharacterColor(selfCharacter, false, false),
				Mathf.PI * 0.1f);
			var forward = center + transform.forward * seeingDistance;
			var left = center + transform.rotation * Quaternion.Euler(0, -maxAngle / 2f, 0) * Vector3.forward * seeingDistance;
			var right =  center + transform.rotation * Quaternion.Euler(0, maxAngle / 2f, 0) * Vector3.forward * seeingDistance;
			DebugDrawer.Line(center, forward, lineColor);
			DebugDrawer.Line(center, left, lineColor);
			DebugDrawer.Line(forward, right, lineColor);
			DebugDrawer.Line(right, forward, lineColor);
			DebugDrawer.Line(forward, left, lineColor);

			debugString = "";
			foreach (var otherCharacter in detectedCharacters.Keys)
			{
				if (!string.IsNullOrEmpty(debugString)) debugString += "\n";
				debugString += ">" + otherCharacter.name;
				if (debugMode > 0)
					DebugDrawer.Line(selfCharacter.GetEyesPosition(), otherCharacter.GetCenterPosition(),
						ColorSettings.GetCharacterColor(selfCharacter));
				//DebugDrawCapsule(otherCharacter, Color.magenta);
			}
		}
	}

	private void OnGUI()
	{
		if (debugMode > 0)
		{
			Utils.DrawWorldGUI(transform.position, debugString);
		}
	}
	
	private void OnDrawGizmosSelected()
	{
		if (selfCharacter)
		{
			if (maxAngle <= 0f) return;
			
			var center = transform.position;
			
			Gizmos.color = Color.yellow;
			Utils.DrawGizmosCircle(center, Vector3.up, hearingDistance);
			
			Gizmos.color = Color.magenta;
			if (maxAngle >= 360f)
			{
				Utils.DrawGizmosCircle(center, Vector3.up, seeingDistance);
				return;
			}

			var forward = center + transform.forward * seeingDistance;
			var left = center + transform.rotation * Quaternion.Euler(0, -maxAngle / 2f, 0) * Vector3.forward * seeingDistance;
			var right =  center + transform.rotation * Quaternion.Euler(0, maxAngle / 2f, 0) * Vector3.forward * seeingDistance;
			Gizmos.DrawLine(center, forward);
			Gizmos.DrawLine(center, left);
			Gizmos.DrawLine(forward, right);
			Gizmos.DrawLine(right, forward);
			Gizmos.DrawLine(forward, left);
			
			Gizmos.color = Utils.ColorScale(Color.magenta, Mathf.PI * 0.1f);
			Utils.DrawGizmosCircle((left + right) / 2, Vector3.up, (left - right).magnitude/2f);
			//Gizmos.DrawWireSphere(selfCharacter.GetPosition(), selfCharacter.GetRadius() + triggerRangeOffset);
		}
	}
	#endif
}