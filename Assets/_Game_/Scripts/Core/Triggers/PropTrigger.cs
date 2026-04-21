using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PropTrigger : MonoBehaviour // Used for rooms and "FogCube"
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
        props.ForEach(p => p.onInteracted += PropInteracted);
    }

    private void OnDisable()
    {
        props.ForEach(p => p.onInteracted -= PropInteracted);
    }

    private void PropInteracted()
    {
        if(disableObject) gameObject.SetActive(false);
        onTrigger?.Invoke();
    }
}
