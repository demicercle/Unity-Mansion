using UnityEngine;

[System.Serializable]
public struct TextReference
{
	[SerializeField] private TMPro.TMP_Text tmpText;
	public GameObject GetGameObject() => tmpText.gameObject;

	public TextReference SetActive(bool value = true)
	{
		GetGameObject().SetActive(value);
		return this;
	}

	public TextReference SetWorldPosition(Vector3 position)
	{
		GetGameObject().transform.position = Camera.main.WorldToScreenPoint(position);
		return this;
	}

	public TextReference SetFollows(Transform target, Vector3 worldOffset)
	{
		Utils.GetOrAddComponent<ScreenFollowsWorld>(GetGameObject())
			.SetTarget(target)
			.SetWorldOffset(worldOffset);
		return this;
	}

	public TextReference SetParent(Transform parent)
	{
		GetGameObject().transform.SetParent(parent);
		return this;
	}
	
	public TextReference SetText(string newText)
	{
		tmpText.text = newText;
		return this;
	}

	public TextReference Duplicate()
	{
		return new TextReference()
		{
			tmpText = Object.Instantiate(tmpText.gameObject).GetComponent<TMPro.TMP_Text>(),
		};
	}
}