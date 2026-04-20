using UnityEngine;


public class PropController : MonoBehaviour
{
	[SerializeField] [ReadOnly] private int interactionCount;

	public System.Action<int> onInteract;
	public void Interact()
	{
		interactionCount++;
		onInteract?.Invoke(interactionCount);
	}
}