using UnityEngine;

public enum TargetType
{
	None,
	AnyCharacter,
}

[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData", order = 0)]
public class ItemData : ScriptableObject
{
	[SerializeField] private LocalizedText displayName;
	[SerializeField] private Texture2D icon;
	[SerializeField] private TargetType targetType;
	[SerializeField] private GameScriptGraph onUseScript;

	public bool CanBeUsedOn(GameObject target)
	{
		if (target)
		{
			if (targetType == TargetType.AnyCharacter)
				return target.TryGetComponent(out CharacterController _);
		}

		return false;
	}

	public void UseOn(GameObject target)
	{
		target.AddComponent<GameScript>().SetGraph(onUseScript);
	}

	public string GetDisplayName() => displayName.GetValue(DialogFiles.items);
	
	public Texture2D GetIcon() => icon;
	
	[ContextMenu("Parse")]
	public void Parse()
	{
		displayName.Parse(DialogFiles.items);
		Utils.SetDirty(this);
	}
}