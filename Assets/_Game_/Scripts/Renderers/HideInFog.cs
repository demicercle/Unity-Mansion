using System;
using System.Collections.Generic;
using UnityEngine;


public class HideInFog : MonoBehaviour
{
	[SerializeField] private CharacterController character;
	[SerializeField] private float nodeSize = 0.5f;
	[SerializeField] private List<Renderer> renderers = new List<Renderer>();
	[SerializeField] private string defaultLayer, hiddenLayer;

	private Vector3 lastPosition, newPosition;
	private int activeRoomsCount;
	private bool isHidden;

	private void LateUpdate()
	{
		newPosition = Snapping.Snap(transform.position, Vector3.one * nodeSize) * 100;
		if (newPosition != lastPosition || activeRoomsCount != RoomTrigger.activeRooms.Count)
		{
			UpdateVisibility(false);
			lastPosition = newPosition;
			activeRoomsCount = RoomTrigger.activeRooms.Count;
		}
	}

	private void UpdateVisibility(bool force)
	{
		isHidden = character && !(character.InParty() || character.IsCharacterVisible());
		renderers.ForEach(r => r.gameObject.layer = LayerMask.NameToLayer(isHidden ? hiddenLayer : defaultLayer));
	}
}