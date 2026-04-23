using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterDetection : MonoBehaviour
{
	static public int debugMode { get; set; }
	
	public System.Action<CharacterController> onCharacterDetected;
	public System.Action<CharacterController> onCharacterLost;
	
	public bool IsCharacterDetected(CharacterController other) => detectedCharacters.Contains(other);
	
	public CharacterController[] GetDetectedCharacters() => detectedCharacters.ToArray();
	
	[SerializeField][Range(0f, 16f)] private float hearingDistance = 3.0f;
	[SerializeField][Range(0f, 32f)] private float seeingDistance = 3.0f;
	[SerializeField][Range(0f, 360f)] private float maxAngle = 30f;
	[SerializeField] private LayerMask obstructionLayers;
	[SerializeField] private bool drawGizmos;

	private List<CharacterController> detectedCharacters = new List<CharacterController>();
	private CharacterController selfCharacter;
	private string debugString;
	
	private void Awake()
	{
		selfCharacter = GetComponent<CharacterController>();
		if (!selfCharacter)
		{
			Debug.LogWarning(this + " has no character controller!");
			enabled = false;
		}
	}

	private void OnEnable()
	{
		CharacterController.onAnyCharacterMoved += OnAnyCharacterMoved;
		CharacterController.onAnyCharacterUsesAction += OnAnyCharacterUsesAction;
		GameTurnManager.Instance.onTurnStarted += OnTurnStarted;
		GameTurnManager.Instance.onTurnEnded += OnTurnEnded;
	}

	private void OnDisable()
	{
		if (!selfCharacter) return;
		
		CharacterController.onAnyCharacterMoved -= OnAnyCharacterMoved;
		CharacterController.onAnyCharacterUsesAction -= OnAnyCharacterUsesAction;
		GameTurnManager.Instance.onTurnStarted -= OnTurnStarted;
		GameTurnManager.Instance.onTurnEnded -= OnTurnEnded;
	}

	private void OnTurnStarted()  => OnAnyCharacterMoved(selfCharacter);
	private void OnTurnEnded()  => OnAnyCharacterMoved(selfCharacter);

	private void OnAnyCharacterUsesAction(CharacterController anyCharacter, CharacterAction _) =>
		OnAnyCharacterMoved(anyCharacter);
	
	private void OnAnyCharacterMoved(CharacterController anyCharacter)
	{
		if (anyCharacter.IsDead()) return;
		
		if (anyCharacter == selfCharacter)
		{
			foreach (var otherCharacter in CharacterController.AllCharacters)
			{
				if (otherCharacter == selfCharacter) continue;
				OnAnyCharacterMoved(otherCharacter);
			}
		}
		else
		{
			if (!detectedCharacters.Contains(anyCharacter))
			{
				if (IsCharacterVisible(anyCharacter))
				{
					detectedCharacters.Add(anyCharacter);
					if(debugMode > 0) Debug.Log(this + " detects " + anyCharacter);
					onCharacterDetected?.Invoke(anyCharacter);
				}
			}
			else
			{
				if (!IsCharacterVisible(anyCharacter))
				{
					detectedCharacters.Remove(anyCharacter);
					if(debugMode > 0) Debug.Log(this + " looses " + anyCharacter);
					onCharacterLost?.Invoke(anyCharacter);
				}
			}
		}
	}

	private bool IsCharacterVisible(CharacterController otherCharacter)
	{
		if (selfCharacter.IsPlayer() && otherCharacter.IsPlayer()) return true;
		
		var selfPosition = selfCharacter.GetPosition();
		var otherPosition = otherCharacter.GetPosition();
		
		var distance = Vector3.Distance(selfPosition, otherPosition);
		
		if (selfCharacter.IsPlayer())
		{
			return distance <= Mathf.Max(seeingDistance, hearingDistance)
			       && otherCharacter.IsCharacterRevealed(); // characters player can see through walls and behind
		}
		
		if (distance <= seeingDistance)
		{
			if (ObstructionCheck(otherCharacter))
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
		return angle <= maxAngle;
	}
	
	private bool ObstructionCheck(CharacterController otherCharacter)
	{
		Vector3 eyesPos = selfCharacter.GetEyesPosition();
		Vector3 otherPos =  otherCharacter.GetCenterPosition();
		if(Physics.Linecast(eyesPos, otherPos, out var hit,obstructionLayers))
		{
			if (debugMode > 0) DebugDrawer.Line(eyesPos, hit.point, Color.red);
			return false;
		}
		
		if (debugMode > 0) DebugDrawer.Line(eyesPos, otherPos, Utils.ColorAlpha(Color.green, Mathf.PI * 0.1f));
		return true;
	}
	
	#if UNITY_EDITOR
	private void LateUpdate()
	{
		if (debugMode > 0)
		{
			var center = selfCharacter.GetPosition();
			
			DebugDrawer.WireDisc(center, transform.forward, hearingDistance, Color.yellow);
			
			if (maxAngle >= 360f)
			{
				DebugDrawer.WireDisc(center, transform.forward, seeingDistance, Color.yellow);
				return;
			}

			center = selfCharacter.GetEyesPosition();
			
			var lineColor = Utils.ColorScale(ColorSettings.GetCharacterColor(selfCharacter, false, false),
				Mathf.PI * 0.1f);
			var forward = center + transform.forward * seeingDistance;
			var left = center + transform.rotation * Quaternion.Euler(0, -maxAngle / 2f, 0) * Vector3.forward * seeingDistance;
			var right =  center + transform.rotation * Quaternion.Euler(0, maxAngle / 2f, 0) * Vector3.forward * seeingDistance;
			DebugDrawer.Line(center, left, lineColor);
			DebugDrawer.Line(left, forward, lineColor);
			DebugDrawer.Line(forward, right, lineColor);
			DebugDrawer.Line(right, center, lineColor);

			debugString = "";
			foreach (var otherCharacter in detectedCharacters)
			{
				if (!string.IsNullOrEmpty(debugString)) debugString += "\n";
				debugString += ">" + otherCharacter.name;
				if (debugMode > 0)
					DebugDrawer.Line(selfCharacter.GetEyesPosition(), otherCharacter.GetCenterPosition(), 
						ColorSettings.GetCharacterColor(selfCharacter));
			}
		}
	}
	
	private void OnGUI()
	{
		if (debugMode > 0)
		{
			Utils.DrawWorldGUI(selfCharacter.GetEyesPosition(), debugString);
		}
	}
	
	private void OnDrawGizmosSelected()
	{
		if (!drawGizmos) return;
		selfCharacter = GetComponent<CharacterController>();
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
			
			center = selfCharacter.GetEyesPosition();

			var forward = center + transform.forward * seeingDistance;
			var left = center + transform.rotation * Quaternion.Euler(0, -maxAngle / 2f, 0) * Vector3.forward * seeingDistance;
			var right =  center + transform.rotation * Quaternion.Euler(0, maxAngle / 2f, 0) * Vector3.forward * seeingDistance;
			Gizmos.DrawLine(center, left);
			Gizmos.DrawLine(left, forward);
			Gizmos.DrawLine(forward, right);
			Gizmos.DrawLine(right, center);
			
			Gizmos.color = Utils.ColorScale(Color.magenta, Mathf.PI * 0.1f);
			Utils.DrawGizmosCircle((left + right) / 2, Vector3.up, (left - right).magnitude/2f);
			//Gizmos.DrawWireSphere(selfCharacter.GetPosition(), selfCharacter.GetRadius() + triggerRangeOffset);
		}
	}
	#endif
}