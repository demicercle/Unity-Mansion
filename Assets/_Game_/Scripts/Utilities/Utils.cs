using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using Pathfinding.Util;
using UnityEngine;


public static class Utils
{
	
	public static void DrawWorldGUI(Vector3 position, string debugString) =>
		DrawWorldGUI(position, debugString, Vector2.zero);
	public static void DrawWorldGUI(Vector3 position, string debugString, Vector2 offset) =>
		DrawWorldGUI(position, debugString, offset, new GUIStyle("textfield"));
	public static void DrawWorldGUI(Vector3 position, string debugString, GUIStyle guiStyle) =>
		DrawWorldGUI(position, debugString, Vector2.zero, guiStyle);
	public static void DrawWorldGUI(Vector3 position, string debugString, Vector2 offset, GUIStyle guiStyle)
	{
		var guiContent = new GUIContent(debugString);
		var guiRect = GUILayoutUtility.GetRect(guiContent, guiStyle);
		var guiPosition = (Vector2)Camera.main.WorldToScreenPoint(position);
		guiPosition.y = Screen.height - guiPosition.y;
		guiPosition += offset;
		GUI.Box(new Rect(guiPosition, guiRect.size), guiContent, guiStyle);
	}

	public static bool IsEditorPrefabStage()
	{
		#if UNITY_EDITOR
		return UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
		#else
		return false;
		#endif
	}

	public static void UndoRecord(string name, params Object[] objects)
	{
		#if UNITY_EDITOR
		if (objects.Length == 1) UnityEditor.Undo.RecordObject(objects[0], name);
		else if (objects.Length > 0) UnityEditor.Undo.RecordObjects(objects, name);
#endif
	}
	
	public static void SetDirty(params Object[] objects)
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			foreach (var obj in objects)
			{
				UnityEditor.EditorUtility.SetDirty(obj);
				UnityEditor.AssetDatabase.SaveAssetIfDirty(obj);
			}
		}
#endif
	}

	public static bool SnapCompareVector3(Vector3 a, Vector3 b, float snap)
	{
		return Snapping.Snap(a, Vector3.one * snap) == Snapping.Snap(b, Vector3.one * snap);
	}
	public static string SplitCamelCase(string input)
	{
		return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
	}
	public static Color ColorAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
	public static Color ColorScale(Color color, float alpha) => new Color(color.r * alpha, color.g * alpha, color.b * alpha, color.a * alpha);

	public static Color HTML_To_Color(string html)
	{
		return ColorUtility.TryParseHtmlString(html, out var c) ? c : default;
	}

	public static string GetTransformPath(Transform transform, string path = "")
	{
		path = transform.name + "/" + path;
		
		if (transform.parent)
		{
			return GetTransformPath(transform.parent, path);
		}
		else
		{
			return path;
		}
	}
	
	public static float PathLength(params Vector3[] path)
	{
		var length = 0f;
		for(int i = 0; i < path.Length -1; i++)
			length += (path[i] - path[i + 1]).magnitude;
		return length;
	}
	
	/// <summary>
	/// Limite la longueur totale d'un chemin (somme des distances entre les points) à une valeur maximale donnée.
	/// </summary>
	/// <param name="path">Le chemin à limiter (tableau de Vector3).</param>
	/// <param name="maxTotalLength">La longueur totale maximale souhaitée pour le chemin.</param>
	/// <returns>Un nouveau tableau de Vector3 dont la longueur totale est limitée à maxTotalLength.</returns>
	public static Vector3[] LimitPathTotalLength(Vector3[] path, float maxTotalLength)
	{
		if (path == null || path.Length <= 1 || maxTotalLength <= 0f)
		{
			return new Vector3[0];
		}

		List<Vector3> limitedPath = new List<Vector3> { path[0] };
		float currentTotalLength = 0f;

		for (int i = 1; i < path.Length; i++)
		{
			float segmentLength = Vector3.Distance(path[i - 1], path[i]);
			if (currentTotalLength + segmentLength > maxTotalLength)
			{
				// Si on dépasse la longueur max, on calcule le point final partiel pour atteindre exactement maxTotalLength
				float remainingLength = maxTotalLength - currentTotalLength;
				Vector3 direction = (path[i] - path[i - 1]).normalized;
				Vector3 lastPoint = path[i - 1] + direction * remainingLength;
				limitedPath.Add(lastPoint);
				break;
			}
			else
			{
				limitedPath.Add(path[i]);
				currentTotalLength += segmentLength;
			}
		}

		return limitedPath.ToArray();
	}
	
	public static void RebuildNavigation()
	{
		foreach (var astar in Object.FindObjectsByType<AstarPath>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID))
		{
			astar.Scan(astar.graphs);
		}
	}

	//public static Coroutine RebuildNavigationAsync() => AstarPath.active.StartCoroutine(DoRebuildNavigationAsync());
	
	public static IEnumerator RebuildNavigationAsync()
	{
		foreach (var progress in AstarPath.active.ScanAsync()) {
//			Debug.Log("Scanning... " + progress.ToString());
			yield return null;
		}
	}

	public static bool TryFindObject<T>(string withName, out T component) where T : Component
	{
		var go = GameObject.Find(withName);
		component = null;
		return go && go.TryGetComponent(out component);
	}

	public static T[] FindObjects<T>() where T : Object => Object.FindObjectsByType<T>(default);
	public static T FindFirstObject<T>() where T : Object => FindObjects<T>().FirstOrDefault();

	public static void DrawGizmosPath(Vector3[] path, bool closedLoop)
	{
		if (path.Length <= 0)
			return;
		
		for (int i = 0; i < path.Length - 1; i++)
		{
			var p0 = path[i];
			var p1 = path[i + 1];
			Gizmos.DrawLine(p0, p1);
		}
		
		if (closedLoop && path.Length > 2)
			Gizmos.DrawLine(path.Last(), path.First());
	}
	
	public static void DrawGizmosCircle(Vector3 pos, Vector3 normal, float radius, int numSegments = 16)
	{
		// I think of normal as conceptually in the Y direction.  We find the
		// "forward" and "right" axes relative to normal and I think of them 
		// as the X and Z axes, though they aren't in any particular direction.
		// All that matters is that they're perpendicular to each other and on
		// the plane defined by pos and normal.
		Vector3 temp = (normal.x < normal.z) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f);
		Vector3 forward = Vector3.Cross(normal, temp).normalized;
		Vector3 right = Vector3.Cross(forward, normal).normalized;

		Vector3 prevPt = pos + (forward * radius);
		float angleStep = (Mathf.PI * 2f) / numSegments;
		for (int i = 0; i < numSegments; i++)
		{
			// Get the angle for the end of this segment.  If it's the last segment,
			// use the angle of the first point so the last segment meets up with
			// the first point exactly (regardless of floating point imprecision).
			float angle = (i == numSegments - 1) ? 0f : (i + 1) * angleStep;

			// Get the segment end point in local space, i.e. pretend as if the
			// normal was (0, 1, 0), forward was (0, 0, 1), right was (1, 0, 0),
			// and pos was (0, 0, 0).
			Vector3 nextPtLocal = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;

			// Transform from local to world coords.  nextPtLocal's x,z are distances
			// along its axes, so we want those as the distances along our right and
			// forward axes.
			Vector3 nextPt = pos + (right * nextPtLocal.x) + (forward * nextPtLocal.z);

			Gizmos.DrawLine(prevPt, nextPt);

			prevPt = nextPt;
		}
	}
	
	public static void DrawDebugCircle(Vector3 pos, Vector3 normal, float radius, int numSegments = 16)
	{
		// I think of normal as conceptually in the Y direction.  We find the
		// "forward" and "right" axes relative to normal and I think of them 
		// as the X and Z axes, though they aren't in any particular direction.
		// All that matters is that they're perpendicular to each other and on
		// the plane defined by pos and normal.
		Vector3 temp = (normal.x < normal.z) ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f);
		Vector3 forward = Vector3.Cross(normal, temp).normalized;
		Vector3 right = Vector3.Cross(forward, normal).normalized;

		Vector3 prevPt = pos + (forward * radius);
		float angleStep = (Mathf.PI * 2f) / numSegments;
		for (int i = 0; i < numSegments; i++)
		{
			// Get the angle for the end of this segment.  If it's the last segment,
			// use the angle of the first point so the last segment meets up with
			// the first point exactly (regardless of floating point imprecision).
			float angle = (i == numSegments - 1) ? 0f : (i + 1) * angleStep;

			// Get the segment end point in local space, i.e. pretend as if the
			// normal was (0, 1, 0), forward was (0, 0, 1), right was (1, 0, 0),
			// and pos was (0, 0, 0).
			Vector3 nextPtLocal = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;

			// Transform from local to world coords.  nextPtLocal's x,z are distances
			// along its axes, so we want those as the distances along our right and
			// forward axes.
			Vector3 nextPt = pos + (right * nextPtLocal.x) + (forward * nextPtLocal.z);

			Debug.DrawLine(prevPt, nextPt);

			prevPt = nextPt;
		}
	}
	
	public static void DrawGizmosArrow(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Gizmos.DrawRay(pos, direction);
		
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
		Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
	}

	public static void DrawGizmosArrow(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Gizmos.color = color;
		Gizmos.DrawRay(pos, direction);
		
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
		Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
		Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
	}

	public static void DrawDebugArrow(Vector3 pos, Vector3 direction, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Debug.DrawRay(pos, direction);
		
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
		Debug.DrawRay(pos + direction, right * arrowHeadLength);
		Debug.DrawRay(pos + direction, left * arrowHeadLength);
	}
	public static void DrawDebugArrow(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Debug.DrawRay(pos, direction, color);
		
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180+arrowHeadAngle,0) * new Vector3(0,0,1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0,180-arrowHeadAngle,0) * new Vector3(0,0,1);
		Debug.DrawRay(pos + direction, right * arrowHeadLength, color);
		Debug.DrawRay(pos + direction, left * arrowHeadLength, color);
	}

	public static string ArrayStr(IEnumerable<object> objs, char  separator = ',')
	{
		var str = "";
		foreach (var obj in objs) str += obj?.ToString() + separator;
		return str;
	}
	
	public static string ArrayStr(IEnumerable objs, char  separator = ',')
	{
		var str = "";
		foreach (var obj in objs) str += obj?.ToString() + separator;
		return str;
	}
	
	public static bool TryGetParent<T>(GameObject gameObject, out T parent)
	{
		parent = gameObject.GetComponentInParent<T>();
		return parent != null;
	}

	public static T GetOrAddComponent<T>(GameObject gameObject) where T:Component
	{
		var t = gameObject.GetComponent<T>();
		if (t == null) t= gameObject.AddComponent<T>();
		return t;
	}
	
	public static float VDistance(Vector3 v1, Vector3 v2)
	{
		v1.x = v2.x;
		v1.z = v2.z;
		return Vector3.Distance(v1, v2);
	}
	
	public static float HDistance(Vector3 v1, Vector3 v2)
	{
		v1.y = v2.y;
		return Vector3.Distance(v1, v2);
	}

	public static float FlatDot(Vector3 dir1, Vector3 dir2)
	{
		dir1.y = dir2.y;
		return  Vector3.Dot(dir1, dir2);
	}

	public static string IntToStringWithZero(int value)
	{
		return RepeatChar('0', Mathf.FloorToInt((float)value / 10f)) + value.ToString();
	}

	private static string tempStr;
	public static string RepeatChar(char c, int count)
	{
		tempStr = "";
		while(count-- > 0) tempStr += c;
		return tempStr;
	}

	public static Rect RectScale(Rect r, Rect s)
	{
		return new Rect(r.x * s.x, r.y * s.y, r.width * s.width, r.height * s.height);
	}
}

public static class NavUtils
{
	public static float NavDistance(Vector3 from, Vector3 to, float decimals = 100f)
	{
		from = (Vector3)RoundVec3(from * decimals) / decimals;
		to = (Vector3)RoundVec3(to * decimals) / decimals;
		return Vector3.Distance(from, to);
	}
	
	public static float NavHDistance(Vector3 from, Vector3 to, float decimals = 100f)
	{
		return NavDistance(new Vector3(from.x, 0, from.z), new Vector3(to.x, 0, to.z), decimals);
	}
	
	public static float NavVDistance(Vector3 from, Vector3 to, float decimals = 100f)
	{
		return NavDistance(new Vector3(0, from.y, 0), new Vector3(0, to.y, 0), decimals);
	}

	public static Vector3Int RoundVec3(Vector3 v) => new Vector3Int((int)v.x, (int)v.y, (int)v.z);
	
	/// <summary>
	/// Récupère un point aléatoire accessible sur le graphe, dans un rayon donné autour d'un centre.
	/// </summary>
	/// <param name="center">Centre de la zone de recherche.</param>
	/// <param name="radius">Rayon de la zone de recherche.</param>
	/// <param name="graphIndex">Index du graphe à utiliser (par défaut : 0, le premier graphe).</param>
	/// <returns>Un point aléatoire accessible, ou null si aucun n'est trouvé.</returns>
	public static bool TryGetRandomPointOnNavmesh(Vector3 center, float radius, float maxDistance, out Vector3 randomPoint)
	{
		randomPoint = center + Quaternion.Euler(0, Random.Range(0, 360f), 0) * Vector3.forward * radius;
		return TryGetNearestNavPosition(randomPoint, maxDistance,  out randomPoint);
	}
	
	public static bool TryGetNearestNavPosition(Vector3 position, float maxDistance, out Vector3 navPosition)
	{
		navPosition = position;

		if (AstarPath.active == null)
			return false;

		// Crée une contrainte pour nœuds walkables uniquement
		Pathfinding.NNConstraint constraint = Pathfinding.NNConstraint.Walkable;
		constraint.constrainWalkability = true;
		constraint.walkable = true;

		// Cherche le nœud le plus proche
		Pathfinding.NNInfo nearest = AstarPath.active.GetNearest(position, constraint);

		if (nearest.node != null && nearest.node.Walkable)
		{
			float distance = Vector3.Distance(position, (Vector3)nearest.position);
			if (distance <= maxDistance)
			{
				navPosition = (Vector3)nearest.position;
				return true;
			}
		}

		return false;
	}
}