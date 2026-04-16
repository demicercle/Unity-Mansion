using System;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class CircleRenderer : MonoBehaviour
{
    public int pointsCount;
    public Vector3 rotation;
    public float radius;
    public float widthScale = 0.5f;
    public Color color;
    public LineRenderer lineRenderer;

    public Vector3 center
    {
        get => transform.position;
        set => transform.position = value;
    }

    public void SetWidth(float v)
    {
        lineRenderer.startWidth = lineRenderer.endWidth = v * widthScale;
    }

    public Color GetColor() => lineRenderer.startColor;

    [ContextMenu("Update Color")]
    public void SetColor() => SetColor(color);
    public void SetColor(Color newColor, float a = -1f)
    {
        if (a >= 0f) newColor.a = a;
        lineRenderer.startColor = lineRenderer.endColor = newColor;
    }
    
    [ContextMenu("Update Circle")]
    public void UpdateCircle()
    {
        if (pointsCount > 0 && radius > 0)
        {
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = pointsCount;
            for (int i = 0; i < pointsCount; i++)
            {
                lineRenderer.SetPosition(i, Quaternion.Euler(rotation) * Quaternion.Euler(0, i * 360f / (float)pointsCount, 0) * Vector3.forward * radius);
            }
        }
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        #endif
    }

    private void Awake()
    {
        SetColor();
    }

    private void OnEnable()
    {
        lineRenderer.enabled = true;
        UpdateCircle();
    }

    private void OnDisable()
    {
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }
}