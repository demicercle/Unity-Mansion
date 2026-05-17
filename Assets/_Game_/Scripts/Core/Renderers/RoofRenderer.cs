using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RoofRenderer : MonoBehaviour
{
	[SerializeField] private float tweenSpeed = 3.14f;
	[SerializeField] private List<RoofRenderer> linkedRoofs =  new List<RoofRenderer>();
	
	private new Renderer renderer;
	
	public void SetBlend(bool visible)
	{
		_targetBlend = visible ? 1.0f : 0.0f;

		foreach (var linkedRoof in linkedRoofs)
		{
			if (linkedRoof)
				linkedRoof._targetBlend = _targetBlend;
		}
	}
	
	private float _currentBlend, _targetBlend;
	private Material _material;
	private Color _defaultColor;

	private void Awake()
	{
		renderer = GetComponent<Renderer>();
		_material = Instantiate(renderer.sharedMaterial);
		renderer.sharedMaterial = _material;
		_defaultColor = _material.GetColor("_Color");
	}

	private void Start()
	{
		_currentBlend = _targetBlend = 1.0f;

		var worldBounds = CameraPivot.Instance.GetWorldBounds();
		var position = transform.position;
		position.y = worldBounds.center.y;
		if (!worldBounds.Contains(position))
		{
			enabled = false;
		}
	}

	private void LateUpdate()
	{
		_currentBlend = Mathf.MoveTowards(_currentBlend, _targetBlend, Time.unscaledDeltaTime * tweenSpeed);
		_material.SetColor("_Color", Utils.ColorAlpha(_defaultColor, _currentBlend));
	}
}