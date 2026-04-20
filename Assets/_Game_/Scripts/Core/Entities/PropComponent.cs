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

	public CursorType GetCursorType() => cursorType;
	[SerializeField] private CursorType cursorType = CursorType.OpenDoor;
	
	public bool onlyOnce;
	[SerializeField] [ReadOnly] private CharacterAction TODO_consumeAction; // TODO = CharacterAction.InteractAction
	public CharacterAction consumeAction => CharacterAction.InteractAction;
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

	public void SetDisabled(bool value)
	{
		if (value)
		{
			gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_IGNORE);
		}
		else
		{
			gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_CHARACTER);
		}
	}

	public float triggerDelay = 0.0f;
	public float animDuration = 1.0f;

	void RebuildNavigationAsync() => NavUtility.RebuildNavigationAsync(Mathf.Max(0.1f, animDuration));
	private void OnInteract(int interactionCount)
	{
		if (rebuildNavigation) RebuildNavigationAsync();
		
		if (onlyOnce && interactionCount > 0)
		{
			SetDisabled(true);
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
		if (rebuildNavigation) RebuildNavigationAsync();
		
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_CHARACTER);

		if (autoTriggerDelay > 0f)
		{
			yield return new WaitForSeconds(autoTriggerDelay);
			GetController().Interact();
		}
	}

	public void DrawGizmoBox()
	{
		var boxCollider = GetComponent<BoxCollider>();
		if (boxCollider != null)
		{
			var center = transform.TransformPoint(boxCollider.center);
			var size = transform.TransformVector(boxCollider.size);
			Gizmos.DrawCube(center, size);
		}
	}
}