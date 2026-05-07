using System;
using System.Collections;
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

	public void AlertAllies(CharacterEntity killedCharacter, CharacterEntity attacker)
	{
		StartCoroutine(WarnAllies(killedCharacter, attacker));
	}

	public CharacterEntity[] GetCharacters() => characters.ToArray();
	
	[SerializeField] private bool warnAllies = true;
	[SerializeField] private List<CharacterEntity> characters  = new List<CharacterEntity>();

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
			if (value && selfCharacter.TryGetComponent(out CharacterDetection selfDetection) && !selfDetection.IsCharacterDetected(otherCharacter))
			{
				selfDetection.SetCharacterDetected(otherCharacter, true);
				if(warnAllies) StartCoroutine(WarnAllies(selfCharacter, otherCharacter));
			}
		}

		private IEnumerator WarnAllies(CharacterEntity selfCharacter, CharacterEntity otherCharacter, bool value = true)
		{
			foreach (var allyCharacter in characters.OrderBy(ally => selfCharacter.GetDistanceTo(ally.GetPosition())))
			{
				if (!allyCharacter.gameObject.activeInHierarchy) continue; // skip disabled objects
				if (allyCharacter == selfCharacter) continue; // skip self
				if (allyCharacter.TryGetComponent(out CharacterDetection detection))
				{
					yield return null; // to increase time
					
					if(!detection.IsCharacterDetected(otherCharacter))
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
			if (Application.isPlaying) return;
			Gizmos.color = Color.indianRed;
			Gizmos.DrawSphere(transform.position, 0.2f);
			for (int i = 0; i < characters.Count; i++)
			{
				Gizmos.DrawLine(transform.position, characters[i].transform.position);
			}
		}
}