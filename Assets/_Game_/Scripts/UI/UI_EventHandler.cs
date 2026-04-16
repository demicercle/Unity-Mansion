using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UI_EventHandler : MonoBehaviour
{
    public Button button;

    public UnityEvent<bool> onButtonDisabled;

    private void OnEnable()
    {
        if (button == null)
        {
            Destroy(this);
            return;
        }
        
        interactable = button.interactable;
    }

    private bool interactable;
    private void LateUpdate()
    {
        if (interactable != button.interactable)
        {
            interactable = button.interactable;
            onButtonDisabled?.Invoke(!interactable);
        }
    }
}
