using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Squad : MonoBehaviour
{
	private static List<Squad> activeInstances = new List<Squad>();

	public static bool TryFindSquads(CharacterEntity character, out List<Squad> squads)
	{
		return (squads = activeInstances.Where(s => s.characters.Contains(character)).ToList()).Count > 0;
	}

	public CharacterEntity[] GetCharacters() => characters.ToArray();

	public bool AddCharacter(CharacterEntity newChar)
	{
		if (!characters.Contains(newChar))
		{
			characters.Add(newChar);
			WarnAllies();
			return true;
		}

		return false;
	}

	public bool RemoveCharacter(CharacterEntity character)
	{
		if (characters.Contains(character))
		{
			characters.Remove(character);
			return true;
		}

		return false;
	}
	
		[SerializeField] private List<CharacterEntity> characters  = new List<CharacterEntity>();

		private void WarnAllies()
		{
			foreach (var ally1 in characters)
			{
				if (!ally1.gameObject.activeInHierarchy) continue; // skip disabled objects
				
				if (ally1.TryGetComponent(out CharacterDetection detection1))
				{
					foreach (var otherCharacter in detection1.GetDetectedCharacters())
					{
						foreach (var ally2 in characters)
						{
							if (ally2 != ally1)
							{
								if (ally2.TryGetComponent(out CharacterDetection detection2))
								{
									detection2.SetCharacterDetected(otherCharacter, true);
								}
							}	
						}
					}
				}
			}
		}

		private void OnEnable()
		{
			activeInstances.Add(this);
			CharacterDetection.onAnyCharacterDetectionChanged += OnAnyCharacterDetectionChanged;
		}

		private void OnDisable()
		{
			activeInstances.Remove(this);
			CharacterDetection.onAnyCharacterDetectionChanged -= OnAnyCharacterDetectionChanged;
		}

		private void OnAnyCharacterDetectionChanged(CharacterEntity selfCharacter, CharacterEntity otherCharacter, bool value)
		{
			foreach (var squadCharacter in characters)
			{
				if (!squadCharacter.gameObject.activeInHierarchy) continue; // skip disabled objects
				if (squadCharacter == selfCharacter)
				{
					if(value)
						WarnAll(otherCharacter);
					
					return;
				}
			}
		}

		private void WarnAll(CharacterEntity otherCharacter, bool value = true)
		{
			foreach (var allyCharacter in characters)
			{
				if (!allyCharacter.gameObject.activeInHierarchy) continue; // skip disabled objects
				if (allyCharacter.TryGetComponent(out CharacterDetection detection))
				{
					detection.SetCharacterDetected(otherCharacter, value);
				}
			}
		}

		[ContextMenu("Update Position")]
		private void UpdatePosition()
		{
			if (characters.Count <= 0) return;
			#if UNITY_EDITOR
			UnityEditor.Undo.RecordObject(transform, "UpdatePosition");
			#endif
			var center = Vector3.zero;
			foreach(var character in characters)
				center += character.GetPosition();
			transform.position = center / (float)characters.Count;
			Utils.SetDirty(transform);
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.indianRed;
			Gizmos.DrawSphere(transform.position, 0.2f);
			for (int i = 0; i < characters.Count; i++)
			{
				Gizmos.DrawLine(transform.position, characters[i].transform.position);
			}
		}
}