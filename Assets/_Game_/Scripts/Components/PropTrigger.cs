using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PropTrigger : MonoBehaviour
{
    public List<PropComponent> props = new List<PropComponent>();

    public bool disableObject;
    public UnityEvent onTrigger;

    private void OnDrawGizmosSelected()
    {
        props.ForEach(prop =>
        {
            Gizmos.color = Color.yellow;
            if (prop)
            {
                Gizmos.DrawLine(transform.position, prop.transform.position);
                prop.DrawGizmoBox();
            }
        });
    }

    private void OnEnable()
    {
        props.ForEach(p => p.interacted.AddListener(OnTriggered));
    }

    private void OnDisable()
    {
        props.ForEach(p => p.interacted.RemoveListener(OnTriggered));
    }

    void OnTriggered()
    {
        if(disableObject) gameObject.SetActive(false);
        onTrigger?.Invoke();
    }
}
