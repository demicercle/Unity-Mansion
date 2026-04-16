using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;


public class Waypoint : MonoBehaviour
{
	public const float MAX_SEARCH_DISTANCE = 10.0f;
	
		static public List<Waypoint> waypoints = new List<Waypoint>();

		static public bool GetBestWaypointFromPosition(Vector3 startPoint, out Waypoint bestWaypoint)
		{
			bestWaypoint = null;

			var bestPathLength = 0f;
			foreach (var wp in waypoints)
			{
				var endPoint = wp.GetPosition();

				if (Vector3.Distance(startPoint, endPoint) > MAX_SEARCH_DISTANCE)
					continue;
				
				// Crée une requête de chemin
				ABPath pathRequest = ABPath.Construct(startPoint, endPoint, null);

				// Envoie la requête au système de pathfinding
				AstarPath.StartPath(pathRequest);

				// Attends que le chemin soit calculé (asynchrone)
				pathRequest.BlockUntilCalculated();

				// Récupère le chemin calculé
				if (pathRequest.error)
				{
					Debug.LogError("Erreur lors du calcul du chemin : " + pathRequest.errorLog);
				}
				else
				{
					// Convertit le chemin en tableau de Vector3
					Vector3[] pathPoints = pathRequest.vectorPath.ToArray();
					float pathLength = Utils.PathLength(pathPoints);
					if (bestWaypoint == null || pathLength < bestPathLength)
					{
						bestPathLength = pathLength;
						bestWaypoint = wp;
					}

					// Tu peux maintenant utiliser 'waypoints' comme tu le souhaites
				}
			}
			
			return bestWaypoint != null;
		}

		private Vector3 GetPosition() => transform.position;

		private void OnEnable()
		{
			waypoints.Add(this);
		}

		void OnDisable()
		{
			waypoints.Remove(this);
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.cyan;
			Utils.DrawGizmosArrow(transform.position, transform.forward);
			Utils.DrawGizmosCircle(transform.position, transform.up, 0.5f, 16);
		}
}