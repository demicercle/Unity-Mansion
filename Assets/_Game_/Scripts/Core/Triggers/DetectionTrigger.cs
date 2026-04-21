using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DetectionTrigger : MonoBehaviour
{
	static public int debugMode { get; set; }
	
	[SerializeField] private CharacterController selfCharacter;
	[SerializeField][Range(0f, 16f)] private float hearingDistance = 3.0f;
	[SerializeField][Range(0f, 32f)] private float seeingDistance = 3.0f;
	[SerializeField][Range(0f, 360f)] private float maxAngle = 30f;
	[SerializeField][Range(0f, 360f)] private float looseTargetTime = 3.0f;
	[SerializeField] private bool noLOS = false;
	
#if UNITY_EDITOR
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
		else
		{
			return distance <= hearingDistance ||
			       (distance <= seeingDistance && AngleCheck(otherCharacter) && LOSCheck(otherCharacter));
		}
	}

	private void OnCharacterUsesAction(CharacterController otherCharacter, CharacterAction actionType)
	{
		if (otherCharacter != selfCharacter)
			OnCharacterMoved(otherCharacter);
	}

	private void OnCharacterMoved(CharacterController otherCharacter)
	{
		if (otherCharacter == selfCharacter)
		{
			if (debugMode > 1) Debug.Log(selfCharacter + " OnSelfMoved");

			foreach (var otherOther in CharacterController.AllCharacters)
			{
				if (otherOther == selfCharacter) continue;
				OnCharacterMoved(otherOther);
			}
			
			return;
		}
		
		if (debugMode > 1) 
			Debug.Log(selfCharacter + " OnCharacterMoved " + otherCharacter);
		
		if (detectedCharacters.ContainsKey(otherCharacter))
			return; // character already visible
		
		var eyesPosition = selfCharacter.GetEyesPosition();
		var otherPosition = otherCharacter.GetPosition();
		var otherCenter = otherCharacter.GetCenterPosition();

		if (IsCharacterVisible(otherCharacter))
		{
			var value = (Vector4)otherPosition;
			value.z = Time.time;
			detectedCharacters.Add(otherCharacter, value);
			
			Debug.Log(selfCharacter + " detects " + otherCharacter);

			if (debugMode > 0)
			{
				var factionColor = ColorSettings.GetCharacterColor(selfCharacter, false, false);
				DebugDrawCapsule(otherCharacter, Utils.ColorScale(ColorSettings.GetCharacterColor(otherCharacter), Mathf.PI * 0.1f), Mathf.PI);
				DebugDrawer.Line(eyesPosition, otherCenter, factionColor, Mathf.PI * 0.1f);
			}
		}
	}

	private void Update()
	{
		debugString = "";
		foreach (var otherCharacter in detectedCharacters.Keys.ToArray())
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
				lastKnownPosition.z -= Time.deltaTime;

				if (Time.time - lastKnownPosition.z >= looseTargetTime)
				{
					Debug.Log(selfCharacter + " forgets " + otherCharacter); // forgets
					detectedCharacters.Remove(otherCharacter);

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
						DebugDrawer.WorldLabel(
							(Vector3)lastKnownPosition + Vector3.up * otherCharacter.GetHeight() + Vector3.up / 2f,
							lastKnownPosition.z.ToString("N2"), debugDrawTime, 0);
					}
				}
			}
		}
	}
	
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
		}
	}

	private void OnGUI()
	{
		if (debugMode > 0)
		{
			Utils.DrawWorldGUI(transform.position, debugString);
		}
	}
#endif

	private void DebugDrawCapsule(CharacterController otherCharacter, Color color, float duration)
	{
		var eyesPosition = selfCharacter.GetEyesPosition();
		var otherCenter = otherCharacter.GetCenterPosition();
		DebugDrawer.Line(eyesPosition, otherCenter, color, duration);
		DebugDrawer.WireCapsule(otherCenter, Quaternion.identity, otherCharacter.GetRadius(),
			otherCharacter.GetHeight(), color, duration);
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

		if (debugMode > 1) DebugDrawer.WorldLabel(worldPosition, Mathf.RoundToInt(angle).ToString(), 0f, 0f);
		
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
			}
			
			return false;
		}
		
		if (debugMode > 0)
		{
			DebugDrawer.Line(eyesPos, otherPos, Color.green);
		}

		return true;
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

	const float debugDrawTime = 0.00001f;
}