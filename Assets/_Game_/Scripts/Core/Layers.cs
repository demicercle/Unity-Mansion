using UnityEngine;

public static class Layers
{
	public static int LayerGround => LayerMask.NameToLayer("Ground");
	public static int LayerWall => LayerMask.NameToLayer("Wall");
	public static int LayerCharacter => LayerMask.NameToLayer("Character");
	public static int LayerIgnore => LayerMask.NameToLayer("Ignore Raycast");
	public static int LayerTrigger => LayerMask.NameToLayer("Trigger");
	public static int LayerDoor => LayerMask.NameToLayer("Door");
	
	public static int MaskInteraction => LayerMask.GetMask("Character", "Door");
	public static int MaskObstruction => LayerMask.GetMask("Ground", "Wall", "Door");
	public static int MaskNavigation => LayerMask.GetMask("Ground");
}