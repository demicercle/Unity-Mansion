using UnityEngine;

[ExecuteInEditMode]
public class HideChildInPrefab : MonoBehaviour
{
	#if UNITY_EDITOR
	static bool IsInPrefabStage()
	{
		var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
		if (prefabStage == null)
		{
			//Debug.LogWarning("no prefab stage");
			return false;
		}

		return true;
	}

	[ReadOnly] [SerializeField] private bool setHidden;
	
	[ContextMenu("Show")]
	void Show()
	{
		setHidden = false;
		SetHidden(false);
		Utils.SetDirty(this);
	}

	[ContextMenu("Hide")]
	void Hide()
	{
		setHidden = true;
		Utils.SetDirty(this);
	}
	
	void SetHidden(bool value)
	{
		for (int i = 0; i < transform.childCount; i++)
		{
			transform.GetChild(i).gameObject.hideFlags = value ? HideFlags.HideInHierarchy : HideFlags.None;
		}
	}

	private void Awake()
	{
		if (Application.isPlaying)
		{
			SetHidden(false);
			Destroy(this);
		}
	}

	private void Update()
	{
		SetHidden(setHidden && !IsInPrefabStage());
	}
#endif
}