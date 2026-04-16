using UnityEngine;
using UnityEngine.EventSystems;

public class CursorHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log(this + " OnPointerEnter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log(this + " OnPointerExit");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(this + " OnPointerClick");
    }
}
