using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;
using UnityEngine.Events;

public class PropComponent : MonoBehaviour, IView<PropController>
{
	private PropController controller;

	public PropController GetController()
	{
		if (controller == null) controller = Utils.GetOrAddComponent<PropController>(gameObject);
		return controller;
	}

	private void Awake()
	{
		var controller = GetController();
		controller.onInteract += OnInteract;
	}

	public Waypoint[] GetWaypoints() => GetComponentsInChildren<Waypoint>();

	public bool onlyOnce;
	[Min(0)] public int consumeActions;
	public bool rebuildNavigation;
	[Min(0f)]public float autoTriggerDelay;
	public string animatorTrigger;
	public UnityEvent interacted;

	[ContextMenu("Update Obstacle")]
	public void UpdateObstacle()
	{
		var points = new List<Vector3>();
		var boxCollider = GetComponent<BoxCollider>();
		if (boxCollider != null)
		{
			var size = boxCollider.size / 2f;
			points.Add(Vector3.Scale(size, new Vector3(1,0,1)));
			points.Add(Vector3.Scale(size, new Vector3(-1,0,1)));
			points.Add(Vector3.Scale(size, new Vector3(-1,0,-1)));
			points.Add(Vector3.Scale(size, new Vector3(1,0,-1)));
		}
		
		foreach (var graphUpdate in GetComponents<GraphUpdateScene>().Concat(GetComponentsInChildren<GraphUpdateScene>())) // todo private
		{
			Debug.Log("update: " + graphUpdate);
			graphUpdate.points = points.ToArray();
			Utils.SetDirty(graphUpdate);
		}
	}

	public bool GetBestWaypoint(Vector3 worldPos, out Waypoint bestWaypoint)
	{
		return Waypoint.GetBestWaypointFromPosition(worldPos, out bestWaypoint, GetWaypoints());
	}

	public float triggerDelay = 0.0f;
	public float animDuration = 1.0f;

	private void OnInteract(int interactionCount)
	{
		if (onlyOnce && interactionCount > 0)
		{
			gameObject.layer = LayerMask.NameToLayer(GameView.LAYER_IGNORE);
		}
		
		if (!string.IsNullOrEmpty(animatorTrigger))
		{
			foreach (var animator in GetComponents<Animator>().Concat(GetComponentsInChildren<Animator>())) //todo private
			{
				//Debug.Log(this + " animator SetTrigger " + animatorTrigger + " : " + animator);
				animator.SetTrigger(animatorTrigger);
			}
		}
		
		interacted?.Invoke();
	}

	private IEnumerator Start()
	{
		gameObject.layer = LayerMask.NameToLayer(GameView.LAYER_CHARACTER);

		if (autoTriggerDelay > 0f)
		{
			yield return new WaitForSeconds(autoTriggerDelay);
			GetController().Interact();
		}
	}
}