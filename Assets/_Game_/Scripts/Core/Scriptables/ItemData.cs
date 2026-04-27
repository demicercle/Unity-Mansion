using UnityEngine;

public enum TargetType
{
	None,
	AnyCharacter,
}

[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData", order = 0)]
public class ItemData : ScriptableObject
{

	public static ItemData GetItemFromName(string itemName)
	{
		return Resources.Load<ItemData>(Consts.ResPath_Items + itemName);
	}
	
	[SerializeField] private LocalizedText displayName;
	[SerializeField] private Texture2D icon;
	[SerializeField] private TargetType targetType;
	[SerializeField] [Range(0f, 12f)] private float useRange;
	[SerializeField] private GameScriptGraph onUseScript;

	public float GetUseRange() => useRange;
	
	public bool CanBeUsedOn(GameObject target, out object objectTo)
	{
		if (target)
		{
			if (targetType == TargetType.AnyCharacter && target.TryGetComponent<CharacterController>(out var character))
			{
				objectTo = character;
				return true;
			}
		}

		objectTo = null;
		return false;
	}

	public void UseOn(GameObject target)
	{
		Debug.Log(this + " UseOn " + target);
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