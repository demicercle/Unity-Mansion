using System.Collections;
using UnityEngine;


public class NavmeshRebuilder : MonoBehaviour
{
	public static int debugMode { get; set; }

	public static void SetNavigationDirty() => navmeshDirty = true;
	public static bool IsNavigationDirty() => navmeshDirty || isNavmeshRebuild;

	private bool isMainInstance;
	private static bool navmeshDirty;
	private static bool isNavmeshRebuild;

	public void SetDirty()
	{
		SetNavigationDirty();
	}
	
	private void OnEnable()
	{
		SetNavigationDirty();
	}

	private void OnDisable()
	{
		SetNavigationDirty();
	}

	private IEnumerator Start()
	{
		isMainInstance = GetComponent<GameTurnManager>();
		if(!isMainInstance)
			yield break;
		
		var rb = GetComponent<Rigidbody>();
		if (rb)
		{
			do yield return null;
			while (rb.linearVelocity.magnitude > Consts.MinMagnitude);
			
			rb.Sleep();
		}

		while (true)
		{
			if (!navmeshDirty) yield return null;
			else
			{
				navmeshDirty = false;
				isNavmeshRebuild = true;
				while (!AstarPath.active) yield return null;
				foreach (var pr in AstarPath.active.ScanAsync())
				{
					if (NavmeshRebuilder.debugMode > 0) Debug.Log(this + " Scanning... " + pr.progress);
					yield return null;
				}
				isNavmeshRebuild = false;
			}
		}
	}
}