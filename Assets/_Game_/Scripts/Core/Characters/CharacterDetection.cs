using System;
using System.Collections;
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
	
	[SerializeField] [Min(0f)] private float awarenessScaleUp = 1.0f;
	[SerializeField] [Min(0f)] private float awarenessScaleDown = 1.0f;
	[SerializeField] [Min(0f)] private float awarenessHoldUpTime = 1.0f; 
	[SerializeField][Range(0f, 16f)] private float hearingDistance = 3.0f;
	[SerializeField][Range(0f, 32f)] private float seeingDistance = 3.0f;
	[SerializeField][Range(0f, 360f)] private float maxAngle = 30f;
	[SerializeField] private AnimationCurve distanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
	[SerializeField] private bool setDetectedOnDamaged = true;
	[SerializeField] private bool lookAtDetected = true;
	[SerializeField] private bool drawGizmos;

	private float lastTimeAwarenessUp;
	private float lastTimeDetected;
	public float GetLastTimeDetectedInterval() => currentTime - lastTimeDetected;
	private float currentTime => Time.realtimeSinceStartup;
	
	public bool IsCharacterDetected(CharacterEntity other) => other && detectedCharacters.ContainsKey(other);

	public float GetMaxSeeingDistance() => Mathf.Max(hearingDistance, seeingDistance);
	public CharacterEntity[] GetDetectedCharacters() => detectedCharacters.Keys.ToArray();
	
	public void SetCharacterDetected(CharacterEntity otherCharacter, bool value)
	{
		if (!otherCharacter)
		{
			Debug.LogWarning(this + " SetCharacterDetected no other character!");
			return;
		}

		
		if (value)
		{
			if (!detectedCharacters.ContainsKey(otherCharacter))
			{
				awarenessMap.Remove(otherCharacter);
				awarenessMap.Add(otherCharacter, 1.0f);
				NotifyAwarenessChanged();
				
				if(debugMode > 0) Debug.Log(this + " detects " + otherCharacter);
				detectedCharacters.Add(otherCharacter, currentTime);
				lastTimeDetected = currentTime;
				
				onCharacterDetected?.Invoke(otherCharacter);
				onAnyCharacterDetectionChanged?.Invoke(selfCharacter, otherCharacter, true);
		
				if (lookAtDetected && !selfCharacter.InParty())
					CharacterRoutines.DoLookPositionOn(selfCharacter, otherCharacter.GetPosition());
			}
		}
		else
		{
			if (detectedCharacters.ContainsKey(otherCharacter))
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

	public float GetAwarenessValueMax() => awarenessMap.Values.OrderBy(v => v).LastOrDefault();

	private Dictionary<CharacterEntity, float> detectedCharacters =  new Dictionary<CharacterEntity, float>(); // last time detected
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
			return;
		}

		selfCharacter.onDead += OnCharacterDead;
		selfCharacter.onRevive += OnCharacterRevive;
	}

	private void OnDestroy()
	{
		if (!selfCharacter) return;
		selfCharacter.onDead -= OnCharacterDead;
		selfCharacter.onRevive -= OnCharacterRevive;
	}

	private void OnEnable()
	{
		selfCharacter.onDamagedBy += OnDamagedBy;
		StartCoroutine("UpdateDetection");
	}

	private void OnDisable()
	{
		if (!selfCharacter) return;
		StopCoroutine("UpdateDetection");
		selfCharacter.onDamagedBy -= OnDamagedBy;
	}

	private void Start()
	{
		NotifyAwarenessChanged();
	}

	private void OnCharacterDead()
	{
		ClearAll();
		enabled = false;
	}

	private void OnCharacterRevive()
	{
		enabled = true;
	}
	
	private void OnDamagedBy(CharacterEntity other, float amount)
	{
		if(debugMode > 1) Debug.Log(this + " OnDamagedBy " + other);
		
		if (setDetectedOnDamaged)
			SetCharacterDetected(other, true);
	}

	private IEnumerator UpdateDetection()
	{
		while (true)
		{
			yield return null;
			
			if (GameTurnManager.Instance.IsLoading())
				continue;

			if (selfCharacter.InParty() || !selfCharacter.IsCharacterRevealed() || selfCharacter.IsDead())
				continue;

			var deltaTime = Time.deltaTime;
			foreach (var anyCharacter in CharacterEntity.AllCharacters)
			{
				if (anyCharacter == selfCharacter) continue;
				if (anyCharacter.IsDead()) continue; // ignore deads
				if (selfCharacter.IsAllyTo(anyCharacter)) continue; // ignore allies
				if (IsCharacterDetected(anyCharacter)) continue;

				if (debugMode > 1) Debug.Log(this + " UpdateDetection " + anyCharacter);
				
				var selfPosition = selfCharacter.GetPosition();
				var otherPosition = anyCharacter.GetPosition();
				var distance = Vector3.Distance(selfPosition, otherPosition);
				var maxDistance = Mathf.Max(hearingDistance, seeingDistance);
				if (distance >= maxDistance) continue;
				
				if (IsCharacterVisible(anyCharacter, distance))
				{
					lastTimeAwarenessUp = Time.time;

					var scale = distanceCurve.Evaluate(distance / maxDistance)
					            * anyCharacter.GetStealth();
					
					AddAwareness(anyCharacter, scale * awarenessScaleUp * deltaTime);
				}
				else if (Time.time - lastTimeAwarenessUp > awarenessHoldUpTime)
				{
					AddAwareness(anyCharacter, -awarenessScaleDown * deltaTime);
				}
				
				yield return null;
			}
		}
	}
	
	private bool IsCharacterVisible(CharacterEntity otherCharacter, float distance)
	{
		if (selfCharacter.InParty() && otherCharacter.InParty())  // party characters will always detect each other
			return true;
		
		if (forceNoDetection)
			return false;
		
		if (!selfCharacter.IsCharacterRevealed() || !otherCharacter.IsCharacterRevealed()) // ignore not revealed characters
			return false;
		
		if(debugMode > 1) Debug.Log(this + " IsCharacterVisible " + otherCharacter);
		
		if (selfCharacter.InParty())
		{
			return distance <= Mathf.Max(seeingDistance, hearingDistance)
			       && otherCharacter.IsCharacterRevealed(); // characters player can see through walls and behind
		}
		
		if (distance <= hearingDistance)
			return ObstructionCheck(otherCharacter);
		
		if (distance <= seeingDistance)
			return AngleCheck(otherCharacter) && ObstructionCheck(otherCharacter);

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
		Vector3[] otherPositions = new Vector3[]
		{
			otherCharacter.GetPosition(),
			otherCharacter.GetPosition() + otherCharacter.GetHeight() * Vector3.up
		};
		foreach (var otherPos in otherPositions)
		{
			if(!Physics.Linecast(eyesPos, otherPos, out var hit,Layers.MaskDetection))
			{
				if (debugMode > 0) DebugDrawer.Line(eyesPos, otherPos, Utils.ColorAlpha(Color.yellow, 0.9f));
				return true;
			}
			
			if (debugMode > 0) DebugDrawer.Line(eyesPos, hit.point, Utils.ColorAlpha(Color.gray, 0.8f));
		}
		
		return false;
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
			//debugString = "Time: " + lastTimeDetected.ToString("N3") + "\n";
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
		if (!drawGizmos || Application.isPlaying) return;
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
		}
	}
	#endif
}