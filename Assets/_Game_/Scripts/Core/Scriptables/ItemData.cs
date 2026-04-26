using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData", order = 0)]
public class ItemData : ScriptableObject
{
	[SerializeField] private LocalizedText displayName;
	[SerializeField] private Texture2D icon;

	public string GetDisplayName() => displayName.GetValue(DialogFiles.items);
	
	public Texture2D GetIcon() => icon;
	
	[ContextMenu("Parse")]
	public void Parse()
	{
		displayName.Parse(DialogFiles.items);
		Utils.SetDirty(this);
	}
}