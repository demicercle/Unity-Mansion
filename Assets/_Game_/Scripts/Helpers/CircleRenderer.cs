using System;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class CircleRenderer : MonoBehaviour
{
    [Range(0f, 1f)] public float progress = 1.0f;
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

    public void SetProgress(float p)
    {
        if (!Mathf.Approximately(p, progress))
        {
            progress = p;
            UpdateCircle();
        }
    }

    public void SetRadius(float newValue)
    {
        if (!Mathf.Approximately(newValue, radius))
        {
            radius = newValue;
            UpdateCircle();
        }
    }
    
    [ContextMenu("Update Circle")]
    public void UpdateCircle()
    {
        if (pointsCount > 0 && radius > 0)
        {
            var pCount = Mathf.FloorToInt(pointsCount * Mathf.Clamp01(progress));
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = pCount == pointsCount;
            lineRenderer.positionCount = pCount;
            for (int i = 0; i < pCount; i++)
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