using System;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class RoofRenderer : MonoBehaviour
{
	[SerializeField] private float tweenSpeed = 3.14f;
	
	private Renderer renderer;
	
	public void SetBlend(float value)
	{
		_targetBlend = value;
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
	}

	private void LateUpdate()
	{
		_currentBlend = Mathf.MoveTowards(_currentBlend, _targetBlend, Time.unscaledDeltaTime * tweenSpeed);
		_material.SetColor("_Color", Utils.ColorAlpha(_defaultColor, _currentBlend));
	}
}