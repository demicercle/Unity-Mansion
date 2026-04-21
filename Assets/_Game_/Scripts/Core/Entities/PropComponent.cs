using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;//TODO
using UnityEngine;
using UnityEngine.Events;

public class PropComponent : MonoBehaviour
{
	public System.Action onInteracted;
	public System.Action<CharacterController> onInteractedBy;
	
	[Header("Interaction")]
	[SerializeField] private CursorType cursorType = CursorType.OpenDoor;
	[SerializeField] private CharacterAction consumeAction = CharacterAction.InteractAction;
	
	[Header("Trigger")]
	[SerializeField] private bool onlyOnce;
	[SerializeField] private bool rebuildNavigation;  // TODO component NavMeshRebuilder
	[Min(0f)][SerializeField] private float autoTriggerDelay;
	[SerializeField] private string animatorTrigger;
	[SerializeField] private float triggerDelay = 0.0f; // TODO use anim events
	[SerializeField] private float animDuration = 1.0f;
	[SerializeField] private CharacterAnimation.EventType animationEvent = default;
	
	[Header("Unity Events")]
	[SerializeField] private UnityEvent _interacted;
	
	[Header("ReadOnly")]
	[SerializeField] [ReadOnly] private int interactionCount;

	public bool AnimFinished() => animFinished;
	
	public CursorType GetCursorType() => cursorType;

	public void InteractedBy(CharacterController characterInstigator)
	{
		Debug.Log(this + " InteractedBy " + characterInstigator + " " + interactionCount);
		interactionCount++;
		
		if (!string.IsNullOrEmpty(animatorTrigger))
		{
			foreach (var animator in GetComponents<Animator>().Concat(GetComponentsInChildren<Animator>())) //todo private
			{
				Debug.Log(this + " SetTrigger " + animatorTrigger + " on " + animator);
				animator.SetTrigger(animatorTrigger);
			}
		}
		
		animFinished = false;
		Invoke("OnAnimFinished", Mathf.Max(animDuration, 0.1f));
		
		onInteracted?.Invoke();
		onInteractedBy?.Invoke(characterInstigator);
		_interacted?.Invoke(); //TODO in Unity
		
		if (onlyOnce && interactionCount > 0)
		{
			SetDisabled(true);
		}
		
		if (rebuildNavigation) RebuildNavigationAsync();
	}

	public bool GetBestWaypoint(Vector3 worldPos, out Waypoint bestWaypoint) // TODO "fake" public
	{
		return Waypoint.GetBestWaypointFromPosition(worldPos, out bestWaypoint, GetWaypoints());
	}
	
	public CharacterAction GetInteractAction() => consumeAction;
	
	public float GetTriggerDelay() => triggerDelay;
	
	public float GetAnimDuration() => animDuration; // todo: read the animation itself using events etc

	public Coroutine WaitInteraction(CharacterController characterInstigator) => StartCoroutine(DoWaitInteractionEvent(characterInstigator));
	
	#region PRIVATE
	private bool animFinished;

	private void OnAnimFinished()
	{
		CancelInvoke("OnAnimFinished");
		animFinished = true;

		if (rebuildNavigation)
		{
			RebuildNavigationAsync();
		}
	}
	
	private Waypoint[] GetWaypoints() => GetComponentsInChildren<Waypoint>();
	
	private void SetDisabled(bool value)
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
	
	private IEnumerator Start()
	{
		if (rebuildNavigation) RebuildNavigationAsync();
		
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_CHARACTER);

		if (autoTriggerDelay > 0f)
		{
			yield return new WaitForSeconds(autoTriggerDelay);
			InteractedBy(null);
		}
	}

	private void RebuildNavigationAsync() => NavUtility.RebuildNavigationAsync(Mathf.Max(0.1f, animDuration));

	private IEnumerator DoWaitInteractionEvent(CharacterController characterInstigator)
	{
		Debug.Log(this + " DoWaitInteractionEvent " + animationEvent + " instigator=" + characterInstigator);
		
		if (animationEvent != default)
		{
			if (characterInstigator && characterInstigator.TryGetAnimation(out var anim))
			{
				if (anim.HasAnimationEvent(animationEvent))
				{
					yield return anim.WaitEvent(animationEvent);
					yield break;
				}
			}
		}

		Debug.Log("yield " + triggerDelay);
		yield return new WaitForSeconds(triggerDelay);
	}
	#endregion
	
	#if UNITY_EDITOR
	public void DrawGizmoBox() // public used by RoomTrigger
	{
		var boxCollider = GetComponent<BoxCollider>();
		if (boxCollider != null)
		{
			var center = transform.TransformPoint(boxCollider.center);
			var size = transform.TransformVector(boxCollider.size);
			Gizmos.DrawCube(center, size);
		}
	}
	
	[ContextMenu("Update Obstacle")]
	private void UpdateObstacle()
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
	#endif
}