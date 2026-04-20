using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HideInFog : MonoBehaviour
{
	[SerializeField] private string defaultLayer, hiddenLayer;
	[SerializeField] private float nodeSize = 0.5f;
	[SerializeField] private CharacterController character;
	[SerializeField] private List<Renderer> renderers = new List<Renderer>();

	private Vector3 lastPosition, newPosition;
	private int activeRoomsCount;
	private bool isHidden;
	private bool doPositionCheck;

	private void Awake()
	{
		if (character == null)
		{
			Debug.LogWarning(this + " has no character, disabling");
			enabled = false;
		}
		
		else if (string.IsNullOrEmpty(hiddenLayer) || string.IsNullOrEmpty(defaultLayer))
		{
			Debug.LogWarning(this + " has no layers set, disabling");
			enabled = false;
		}
	}

	private IEnumerator Start()
	{
		do
		{
			yield return null;
		} while (GameTurnManager.Instance.IsLoading());

		UpdateVisibility();
		doPositionCheck = true;
	}

	private void LateUpdate()
	{
		if (!doPositionCheck) return;
		newPosition = Snapping.Snap(transform.position, Vector3.one * nodeSize) * 100;
		if (newPosition != lastPosition || activeRoomsCount != RoomTrigger.activeRooms.Count)
		{
			UpdateVisibility();
			lastPosition = newPosition;
			activeRoomsCount = RoomTrigger.activeRooms.Count;
		}
	}

	private void UpdateVisibility()
	{
		isHidden = character && !(character.InParty() || character.IsCharacterVisible());
		renderers.ForEach(r => r.gameObject.layer = LayerMask.NameToLayer(isHidden ? hiddenLayer : defaultLayer));
	}
}