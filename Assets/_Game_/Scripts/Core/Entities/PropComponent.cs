using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;//TODO
using UnityEngine;
using UnityEngine.Events;

public class PropComponent : MonoBehaviour
{
	private static List<PropComponent> allInstances = new List<PropComponent>();
	public static PropComponent[] GetSavedInstances() => allInstances.Where(p => p.saveProp).ToArray();
	
	public static int debugMode { get; set; }
	
	public System.Action onInteracted;
	public System.Action<CharacterEntity> onInteractedBy;

	public string GetName() => name;
	public bool IsName(string n) => name == n;
	
	[Header("Interaction")]
	[SerializeField] private CursorType cursorType = CursorType.OpenDoor;
	[SerializeField] private CharacterAction consumeAction = CharacterAction.Interact;
	[SerializeField] private bool saveProp = false;
	
	[Header("Trigger")]
	[SerializeField] private bool onlyOnce;
	[SerializeField] private bool disableTurnBased;
	//[SerializeField] private bool rebuildNavigation;  // TODO component NavMeshRebuilder
	[Min(0f)][SerializeField] private float autoTriggerDelay;
	[SerializeField] private string animatorTrigger;
	[SerializeField] private float triggerDelay = 0.0f; // TODO use anim events
	[SerializeField] private float animDuration = 1.0f;
	[SerializeField] private CharacterAnimation.EventType animationEvent = default;
	[SerializeField] private PropComponent linkedProp;
	
	[Header("Unity Events")]
	[SerializeField] private UnityEvent _interacted;
	
	[Header("ReadOnly")]
	[SerializeField] [ReadOnly] private int interactionCount;
	
	public float GetRadius()
	{
		worldBounds = selfCollider.bounds;
		return Mathf.Max(worldBounds.size.x, worldBounds.size.y, worldBounds.size.z) / 2f;
	}

	public bool IsDoor() => CompareTag(Tags.TagDoor);

	public bool AllowTurnBase() => !disableTurnBased;
	
	public bool AnimFinished() => animFinished;
	
	public CursorType GetCursorType() => cursorType;

	public Vector3 GetPosition() => transform.position;

	public int GetInteractionCount() => interactionCount;

	public bool IsOpened() => interactionCount % 2 != 0;
	
	public void SetInteractionCount(int newCount)
	{
		if(debugMode > 0) Debug.Log(this + " SetInteractionCount " + newCount);
		
		while (interactionCount < newCount)
		{
			interactionCount++;
		
			TriggerAnimators(true);
		
			animFinished = false;
			Invoke("OnAnimFinished", Mathf.Max(animDuration, 0.1f));
		
			onInteracted?.Invoke();
			_interacted?.Invoke(); //TODO in Unity
		}
		
		if (onlyOnce && interactionCount > 0)
		{
			SetDisabled(true);
		}
	}

	private void TriggerAnimators(bool setTrigger)
	{
		if (!string.IsNullOrEmpty(animatorTrigger))
		{
			foreach (var animator in GetComponents<Animator>().Concat(GetComponentsInChildren<Animator>())) //todo private
			{
				if (setTrigger)
				{
					if(debugMode > 0) Debug.Log(this + " SetTrigger " + animatorTrigger + " on " + animator);
					animator.SetTrigger(animatorTrigger);
				}
				else
				{
					if(debugMode > 0) Debug.Log(this + " ResetTrigger " + animatorTrigger + " on " + animator);
					animator.ResetTrigger(animatorTrigger);
				}
			}
		}
	}

	public void InteractedBy(CharacterEntity characterInstigator)
	{
		if(debugMode > 0) Debug.Log(this + " InteractedBy " + characterInstigator + " " + interactionCount);
		interactionCount++;
		
		TriggerAnimators(true);
		
		animFinished = false;
		Invoke("OnAnimFinished", Mathf.Max(animDuration, 0.1f));

		if (TryGetComponent(out Inventory selfInventory) && characterInstigator.TryGetComponent(out Inventory otherInventory))
		{
			if (!selfInventory.TransfertTo(otherInventory)) // pickup item
			{
				characterInstigator.FloatingText(LanguageSettings.Main.Full);
			}
		}

		if (TryGetComponent(out LevelTransition levelTransition))
		{
			if (!levelTransition.TryTravel()) // travel
			{
				characterInstigator.FloatingText_Cant();
			}
		}
		
		onInteracted?.Invoke();
		onInteractedBy?.Invoke(characterInstigator);
		_interacted?.Invoke(); //TODO in Unity
		
		if (onlyOnce && interactionCount > 0)
		{
			SetDisabled(true);
		}
		
	//	if(rebuildNavigation) 
	//		GameTurnManager.Instance.SetNavigationDirty();
	}
	
	public CharacterAction GetInteractAction() => consumeAction;
	
	public float GetTriggerDelay() => triggerDelay;
	
	public float GetAnimDuration() => animDuration; // todo: read the animation itself using events etc

	public Coroutine WaitInteraction(CharacterEntity characterInstigator) => StartCoroutine(DoWaitInteractionEvent(characterInstigator));
	
	#region PRIVATE
	private bool animFinished;
	private int defaultLayer;
	private Collider selfCollider;
	private Bounds worldBounds;

	private void OnAnimFinished()
	{
		CancelInvoke("OnAnimFinished");
		
		animFinished = true;

	//	if(rebuildNavigation) 
	//		GameTurnManager.Instance.SetNavigationDirty();
	}
	
	private void SetDisabled(bool value)
	{
		if (value)
		{
			gameObject.layer = Layers.LayerIgnore;
		}
		else
		{
			gameObject.layer = defaultLayer;
		}
	}

	private void Awake()
	{
		defaultLayer = gameObject.layer;
		selfCollider = GetComponent<Collider>();
		allInstances.Add(this);
		
		foreach (var wp in GetComponentsInChildren<Waypoint>())
			wp.ignoreNavigation = true;

		if (linkedProp) linkedProp.onInteractedBy += InteractedBy;
	}

	private void OnDestroy()
	{
		allInstances.Remove(this);
		
		if (linkedProp) linkedProp.onInteractedBy -= InteractedBy;
	}

	private IEnumerator Start()
	{
	//	if(rebuildNavigation) 
	//		GameTurnManager.Instance.SetNavigationDirty();

		if (autoTriggerDelay > 0f)
		{
			yield return new WaitForSeconds(autoTriggerDelay);
			InteractedBy(null);
		}
	}

	private IEnumerator DoWaitInteractionEvent(CharacterEntity characterInstigator)
	{
		if(debugMode > 0) Debug.Log(this + " DoWaitInteractionEvent " + animationEvent + " instigator=" + characterInstigator);
		
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

		if(debugMode > 0) Debug.Log("yield " + triggerDelay);
		yield return new WaitForSeconds(triggerDelay);
	}
	#endregion
	
	#if UNITY_EDITOR
	public void DrawGizmoBox() // public used by RoomTrigger
	{
		if (Application.isPlaying) return;
		var boxCollider = GetComponent<BoxCollider>();
		if (boxCollider != null)
		{
			var center = transform.TransformPoint(boxCollider.center);
			var size = transform.TransformVector(boxCollider.size);
			Gizmos.DrawCube(center, size);
		}
	}
	
	[ContextMenu("Update Obstacle")]
	private void UpdateObstacleEditor()
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