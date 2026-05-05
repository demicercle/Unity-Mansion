using UnityEngine;

public class PrefabStageVisibility : MonoBehaviour
{
	#if UNITY_EDITOR
	[SerializeField] private bool hideEditor = true;
	private void OnDrawGizmos()
	{
		if (!hideEditor || Application.isPlaying) return;
		var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
		var sceneVisibility = UnityEditor.SceneVisibilityManager.instance;
		if(!prefabStage)
			sceneVisibility.Hide(gameObject, true);
		else if (sceneVisibility.IsHidden(gameObject))
			sceneVisibility.Show(gameObject, true);
	}
	#endif
}