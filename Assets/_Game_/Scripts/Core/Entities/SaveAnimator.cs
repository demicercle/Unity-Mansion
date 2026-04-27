using System.Collections.Generic;
using UnityEngine;

public class SaveAnimator : MonoBehaviour
{
	private static List<SaveAnimator> allInstances = new List<SaveAnimator>();
	
	public static SaveAnimator[] GetInstances() => allInstances.ToArray();
	
	public string GetName() => name;
	public bool IsName(string s) => s == name;
	
	[SerializeField] private Animator animator;
	[SerializeField] private List<GameSave.SavedValue> savedValues = new List<GameSave.SavedValue>();

	public GameSave.SavedValue[] SaveValues()
	{
		for (int i = 0; i < savedValues.Count; i++)
		{
			if(savedValues[i].valueType == GameSave.ValueType.Bool)
				savedValues[i].currentValue = animator.GetBool(savedValues[i].name);
		}

		return savedValues.ToArray();
	}

	public void LoadValues(GameSave.SavedValue[] newValues)
	{
		savedValues.Clear();
		savedValues.AddRange(newValues);
		
		for (int i = 0; i < savedValues.Count; i++)
		{
			if(savedValues[i].valueType == GameSave.ValueType.Bool)
				animator.SetBool(savedValues[i].name, savedValues[i].currentValue);
		}
	}
	

	private void Awake() => allInstances.Add(this);
	private void OnDestroy() => allInstances.Remove(this);
}