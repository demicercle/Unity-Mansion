using System.Collections;
using UnityEngine;


public class NavUtility : MonoBehaviour
{
	static private NavUtility instance;
	static public void RebuildNavigationAsync(float delay)
	{
		_delay = delay;
		if(instance!=null)Destroy(instance.gameObject);
			instance = new GameObject("NavUtility").AddComponent<NavUtility>();
	}

	static float _delay;
	IEnumerator Start()
	{
		yield return new WaitForSeconds(_delay);
		
		Debug.Log(this + " rebuild navigation async");
		foreach (var progress in AstarPath.active.ScanAsync()) {
//			Debug.Log("Scanning... " + progress.ToString());
			yield return null;
		}

	//	Debug.Log(this + " complete");
		Destroy(gameObject);
	}
}