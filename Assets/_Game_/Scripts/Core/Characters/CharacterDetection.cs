using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterDetection : MonoBehaviour
{
	public static int debugMode { get; set; }
	public static bool forceNoDetection { get; set; }

	public static event System.Action<CharacterEntity, CharacterEntity, bool> onAnyCharacterDetectionChanged;
	
	public System.Action<CharacterEntity> onCharacterDetected;
	public System.Action<CharacterEntity> onCharacterLost;

	public System.Action<float> onAwarenessChanged;
	
	[SerializeField][Range(0f, 16f)] private float hearingDistance = 3.0f;
	[SerializeField][Range(0f, 32f)] private float seeingDistance = 3.0f;
	[SerializeField][Range(0f, 360f)] private float maxAngle = 30f;
	[SerializeField] private bool setDetectedOnDamaged = true;
	[SerializeField] private bool lookAtDetected = true;
	[SerializeField] private bool warnOthersInRoom = true;
	[SerializeField] private bool drawGizmos;
	[SerializeField] [Min(0f)] private float looseTargetDistance;
	[SerializeField] [Min(0f)] private float awarenessScale = 1.0f;
	
	public bool IsCharacterDetected(CharacterEntity other) => other && detectedCharacters.Contains(other);

	public float GetMaxSeeingDistance() => Mathf.Max(hearingDistance, seeingDistance);
	public CharacterEntity[] GetDetectedCharacters() => detectedCharacters.ToArray();
	
	public void SetCharacterDetected(CharacterEntity otherCharacter, bool value)
	{
		if (value)
		{
			if (!detectedCharacters.Contains(otherCharacter))
			{
				awarenessMap.Remove(otherCharacter);
				awarenessMap.Add(otherCharacter, 1.0f);
				NotifyAwarenessChanged();
				
				if(debugMode > 0) Debug.Log(this + " detects " + otherCharacter);
				detectedCharacters.Add(otherCharacter);
				
				if (selfCharacter.IsEnemyTo(otherCharacter) && warnOthersInRoom)
				{
					if (RoomTrigger.TryGetRoom(selfCharacter, out var selfRoom))
					{
						var others = selfRoom.GetCharacters();
						foreach (var other in others)
						{
							if (other == selfCharacter) continue;
							if (other.GetCurrentFaction() == selfCharacter.GetCurrentFaction() 
							    && !other.IsDead()
							    && !other.HasDetectedOther(selfCharacter))
							{
								if (other.TryGetComponent(out CharacterDetection otherDetection))
								{
									if(debugMode > 0) Debug.Log(this + " warnOthersInRoom " + otherDetection + " SetDetected " + otherCharacter);
									otherDetection.SetCharacterDetected(otherCharacter, true);
								}
							}
						}
					}
				}
				
				onCharacterDetected?.Invoke(otherCharacter);
				onAnyCharacterDetectionChanged?.Invoke(selfCharacter, otherCharacter, true);
		
				if (lookAtDetected && !selfCharacter.InParty())
					CharacterRoutines.DoLookPositionOn(selfCharacter, otherCharacter.GetPosition());
			}
		}
		else
		{
			if (detectedCharacters.Contains(otherCharacter))
			{
				awarenessMap.Remove(otherCharacter);
				NotifyAwarenessChanged();
				
				if(debugMode > 0) Debug.Log(this + " looses " + otherCharacter);
				detectedCharacters.Remove(otherCharacter);
				onCharacterLost?.Invoke(otherCharacter);
				onAnyCharacterDetectionChanged?.Invoke(selfCharacter, otherCharacter, false);
			}
		}
	}

	public void ClearAll() => detectedCharacters.Clear();

	private List<CharacterEntity> detectedCharacters = new List<CharacterEntity>();
	private Dictionary<CharacterEntity, float> awarenessMap = new Dictionary<CharacterEntity, float>();
	private CharacterEntity selfCharacter;
	private string debugString;

	private void AddAwareness(CharacterEntity anyCharacter, float value)
	{
		if (value == 0f)
			return;
		
		if (!awarenessMap.TryAdd(anyCharacter, value))
		{
			awarenessMap[anyCharacter] += value;
		}

		if (awarenessMap[anyCharacter] >= 1f)
		{
			SetCharacterDetected(anyCharacter, true);
		}
		else if (awarenessMap[anyCharacter] <= 0f)
		{
			awarenessMap.Remove(anyCharacter);
		}
		
		NotifyAwarenessChanged();
	}

	private void NotifyAwarenessChanged() => onAwarenessChanged?.Invoke(awarenessMap.Values.OrderBy(v => v).LastOrDefault());
	
	private void Awake()
	{
		selfCharacter = GetComponent<CharacterEntity>();
		if (!selfCharacter)
		{
			Debug.LogWarning(this + " has no character controller!");
			enabled = false;
		}
	}

	private void OnEnable()
	{
		CharacterEntity.onAnyCharacterMoved += OnAnyCharacterMoved;
		CharacterEntity.onAnyCharacterUsesAction += OnAnyCharacterUsesAction;
		GameTurnManager.Instance.onTurnStarted += OnTurnStarted;
		GameTurnManager.Instance.onTurnEnded += OnTurnEnded;
		selfCharacter.onDamagedBy += OnDamagedBy;
	}

	private void OnDisable()
	{
		if (!selfCharacter) return;
		
		CharacterEntity.onAnyCharacterMoved -= OnAnyCharacterMoved;
		CharacterEntity.onAnyCharacterUsesAction -= OnAnyCharacterUsesAction;
		GameTurnManager.Instance.onTurnStarted -= OnTurnStarted;
		GameTurnManager.Instance.onTurnEnded -= OnTurnEnded;
		selfCharacter.onDamagedBy -= OnDamagedBy;
	}

	private void Start()
	{
		NotifyAwarenessChanged();
	}

	private void OnTurnStarted()  => OnAnyCharacterMoved(selfCharacter);
	
	private void OnTurnEnded()  => OnAnyCharacterMoved(selfCharacter);

	private void OnDamagedBy(CharacterEntity other, float amount)
	{
		if(debugMode > 1) Debug.Log(this + " OnDamagedBy " + other);
		
		if (setDetectedOnDamaged)
			SetCharacterDetected(other, true);
	}

	private void OnAnyCharacterUsesAction(CharacterEntity anyCharacter, CharacterAction _) =>
		OnAnyCharacterMoved(anyCharacter);
	
	private void OnAnyCharacterMoved(CharacterEntity anyCharacter)
	{
		if (anyCharacter.IsDead()) 
			return;

		if (debugMode > 1)
			Debug.Log(this + " revealed:" + selfCharacter.IsCharacterRevealed() + " OnAnyCharacterMoved "+ anyCharacter + " revealed:" + anyCharacter.IsCharacterRevealed());
		
		if (!selfCharacter.IsCharacterRevealed() || !anyCharacter.IsCharacterRevealed())  // ignore not revealed characters
			return; 
		
		if (anyCharacter == selfCharacter)
		{
			foreach (var otherCharacter in CharacterEntity.AllCharacters)
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
					AddAwareness(anyCharacter, anyCharacter.GetStealth() * awarenessScale * Time.deltaTime);
				}
				else
				{
					AddAwareness(anyCharacter, -anyCharacter.GetStealth() * awarenessScale * Time.deltaTime);
				}
			}
			else
			{
				if (looseTargetDistance > 0 && Vector3.Distance(selfCharacter.GetPosition(), anyCharacter.GetPosition()) > looseTargetDistance)
				{
					SetCharacterDetected(anyCharacter, false);
				}
			}
		}
	}

	private bool IsCharacterVisible(CharacterEntity otherCharacter)
	{
		if (selfCharacter.IsPlayer() && otherCharacter.IsPlayer())  // party characters will always detect each other
			return true;
		
		if (forceNoDetection)
			return false;
		
		if (!selfCharacter.IsCharacterRevealed() || !otherCharacter.IsCharacterRevealed()) // ignore not revealed characters
			return false;
		
		if(debugMode > 1) Debug.Log(this + " IsCharacterVisible " + otherCharacter);
		
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

	private bool AngleCheck(CharacterEntity other)
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
	
	private bool ObstructionCheck(CharacterEntity otherCharacter)
	{
		Vector3 eyesPos = selfCharacter.GetEyesPosition();
		Vector3 otherPos =  otherCharacter.GetCenterPosition();
		if(Physics.Linecast(eyesPos, otherPos, out var hit,Layers.MaskDetection))
		{
			if (debugMode > 0) DebugDrawer.Line(eyesPos, hit.point, Color.red);
			return false;
		}
		
		if (debugMode > 0) DebugDrawer.Line(eyesPos, otherPos, Utils.ColorAlpha(Color.green, Mathf.PI * 0.1f));
		return true;
	}
	
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

			foreach (var entry in awarenessMap)
			{
				DebugDrawer.Line(center, entry.Key.GetCenterPosition(), Utils.ColorAlpha(lineColor, entry.Value));
			}
		}
	}
	
	private void OnGUI()
	{
		if (debugMode > 0)
		{
			debugString = "";
			foreach (var entry in awarenessMap)
			{
				if (!string.IsNullOrEmpty(debugString)) debugString += "\n";
				debugString += entry.Key.name + " : " + entry.Value.ToString("N2");
			}
			Utils.DrawWorldGUI(selfCharacter.GetEyesPosition(), debugString);
		}
	}
	
#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (!drawGizmos) return;
		selfCharacter = GetComponent<CharacterEntity>();
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
			
			Gizmos.color = Color.darkOrchid;
			Utils.DrawGizmosCircle(transform.position, Vector3.up, looseTargetDistance);
		}
	}
	#endif
}