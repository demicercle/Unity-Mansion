using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData", order = 0)]
public class ItemData : ScriptableObject
{
	public LocalizedText displayName;
	public Texture2D icon;

	[ContextMenu("Parse")]
	public void Parse()
	{
		displayName.Parse();
	}
}