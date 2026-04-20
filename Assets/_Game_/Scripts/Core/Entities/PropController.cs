using UnityEngine;


public class PropController : MonoBehaviour
{
	[SerializeField] [ReadOnly] private int interactionCount;

	public System.Action<int> onInteract;
	public void Interact()
	{
		Debug.Log(this + " Interact " + interactionCount);
		interactionCount++;
		onInteract?.Invoke(interactionCount);
	}
}