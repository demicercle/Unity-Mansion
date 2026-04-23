using UnityEngine;

public static class Layers
{
	public static int LayerGround => LayerMask.NameToLayer("Ground");
	public static int LayerWall => LayerMask.NameToLayer("Wall");
	public static int LayerCharacter => LayerMask.NameToLayer("Character");
	public static int LayerIgnore => LayerMask.NameToLayer("Ignore Raycast");
	public static int LayerTrigger => LayerMask.NameToLayer("Trigger");
	public static int LayerProp => LayerMask.NameToLayer("Prop");
	public static int LayerHidden => LayerMask.NameToLayer("Hidden");
	public static int LayerTransparentFX => LayerMask.NameToLayer("TransparentFX");
	
	public static int MaskWall => LayerMask.GetMask("Ground", "Wall", "Fog");
	public static int MaskNavigation => LayerMask.GetMask("Ground");
	public static int MaskInteraction => LayerMask.GetMask("Character", "Prop");
	public static int MaskObstruction => LayerMask.GetMask("Ground", "Wall", "Fog");
}

public static class Tags
{
	public const string TagPlayer = "Player";
	public const string TagDoor = "Door";
}