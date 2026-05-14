using UnityEditor;
using UnityEditor.SceneManagement;

public static class OpenEditorScenes
{
	private const string SCENE_PATH = "Assets/_Game_/Scenes/";
	
	[MenuItem("[Scenes]/StartScene")]
	static void OpenScenes_StartScene() => OpenScenes("StartScene");
	[MenuItem("[Scenes]/GameScene")]
	static void OpenScenes_GameScene() => OpenScenes("GameScene");
	
	[MenuItem("[Scenes]/Levels/Level_0")]
	static void OpenScenes_Levels_Level_0() => OpenScenes("GameScene", "Level_Globals", "Level_0", "Level_0_Env");
	
	[MenuItem("[Scenes]/Levels/Level_1")]
	static void OpenScenes_Levels_Level_1() => OpenScenes("GameScene", "Level_Globals", "Level_1", "Level_1_Env");

	static void OpenScenes(params string[] scenes)
	{
		for (int i = 0; i < scenes.Length; i++)
		{
			EditorSceneManager.OpenScene(SCENE_PATH + scenes[i] + ".unity", i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
		}
	}
}