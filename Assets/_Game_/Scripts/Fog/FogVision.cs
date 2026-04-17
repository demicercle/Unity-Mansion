using System;
using UnityEngine;


public class FogVision : MonoBehaviour
{
	static public event System.Action<FogVision> onMoved;
	static public event System.Action<FogVision, bool> onToggled;
	
	public Vector3 GetEyesWorldPosition()
	{
		return character.GetEyesPosition();
	}

	[SerializeField] private float range = 2.0f;
	public float GetRange() => range;
	
	private bool wasEnabled;
	private Vector3Int lastCell;
	private CharacterController character;

	void Awake()
	{
		character = GetComponent<CharacterController>();
	}

	private void OnEnable()
	{
		onToggled?.Invoke(this, true);
	}

	private void OnDisable()
	{
		wasEnabled = false;
		onToggled?.Invoke(this, false);
	}

	private void LateUpdate()
	{
		var fog = FogProjector.GetInstance();
		var newCell = fog.WorldToCell(GetEyesWorldPosition());
		if (newCell != lastCell || wasEnabled != enabled)
		{
			onMoved?.Invoke(this);
			lastCell = newCell;
		}
		
		wasEnabled = enabled;
	}
}