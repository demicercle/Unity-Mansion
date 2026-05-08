using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
	[ReadOnly] public ItemData itemData;
	[Min(1f)] public float gravityScale = 1f;
	public int segments = 16;
	public float detectCollisionsDelay = 0.5f;
	[ReadOnly] public float calculatedDuration;

	public UnityEvent onHit;
	
	private Vector3 origin, destination;
	private float calculatedSpeed;
	private Vector3 newPosition;
	private Rigidbody rb;
	private bool destroysOnHit;
	private float time;
	private List<Vector3> points =  new List<Vector3>();

	public void DestroyNow() => Destroy(gameObject);

	public void StopSimulate()
	{
		lineRenderer.enabled = false;
		rb.isKinematic = true;
		rb.detectCollisions = false;
	}

	[SerializeField] private LineRenderer lineRenderer;
	public void Simulate(Vector3 startPoint, Vector3 targetPoint)
	{
		StopSimulate();

		var gravity = Physics.gravity * gravityScale;
		if (!ProjectileTrajectoryOptimized.CalculateLaunchVelocity(startPoint, targetPoint, out var initialVelocity, out var duration, gravity))
		{
			Debug.LogWarning(this + " unable CalculateLaunchVelocity ");
			return;
		}

		// Dessine la trajectoire
		Vector3 previousPoint = startPoint;
		
		points.Clear();
		for (int i = 1; i <= segments; i++)
		{
			float t = (duration * i) / segments;
			Vector3 currentPoint = startPoint + initialVelocity * t + gravity * (0.5f * t * t);
			//DebugDrawer.Line(previousPoint, currentPoint, Color.magenta, 1.0f);
			if (points.Count > 0)
			{
				previousPoint = points.Last();
				if (Physics.Linecast(currentPoint, previousPoint, out var hit, Layers.MaskProjectile))
				{
					break; // hit something
				}
			}
			
			points.Add(currentPoint);
		}
		
		lineRenderer.enabled = true;
		lineRenderer.useWorldSpace = true;
		lineRenderer.positionCount = points.Count;
		lineRenderer.SetPositions(points.ToArray());
	}

	private void OnCollisionEnter(Collision other)
	{
		if (destroysOnHit)
		{
			Debug.Log(this + " destroysOnHit " + other.collider);
			DestroyNow();
		}
	}

	public bool Launch(Vector3 startPoint, Vector3 targetPoint, out Projectile clone)
	{
		clone = null;
		
		var gravity = Physics.gravity * gravityScale;
		if (!ProjectileTrajectoryOptimized.CalculateLaunchVelocity(startPoint, targetPoint, out var initialVelocity, out var duration, gravity))
		{
			Debug.LogWarning(this + " unable CalculateLaunchVelocity ");
			return false;
		}

		clone = Instantiate(gameObject).GetComponent<Projectile>();
		if (!clone.rb) clone.rb = clone.GetComponent<Rigidbody>();
		clone.rb.position = startPoint;
		clone.rb.rotation = Quaternion.LookRotation(initialVelocity.normalized);
		clone.rb.isKinematic = false;
		clone.rb.linearVelocity = initialVelocity;
		clone.rb.mass *= gravityScale;
		clone.calculatedDuration = duration;
		return true;
	}

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		StopSimulate();
	}

	private IEnumerator Start()
	{
		if (!rb.isKinematic)
		{
			rb.detectCollisions = true;
			rb.includeLayers = Layers.MaskNavigation;
			rb.excludeLayers = ~rb.includeLayers;
			yield return new WaitForSeconds(detectCollisionsDelay);
			rb.includeLayers = Layers.MaskProjectile;
			rb.excludeLayers = ~rb.includeLayers;
		}
	}
}

public static class ProjectileTrajectoryOptimized
{
    /// <summary>
    /// Calcule la vitesse initiale nécessaire pour lancer un projectile d'un point à un autre,
    /// en tenant compte de la gravité de Unity.
    /// </summary>
    /// <param name="startPoint">Point de départ.</param>
    /// <param name="targetPoint">Point d'arrivée.</param>
    /// <param name="initialVelocity">Vitesse initiale à appliquer (en sortie).</param>
    /// <param name="flightDuration">Durée du vol (en sortie).</param>
    /// <param name="gravity">Gravité à utiliser (par défaut : Physics.gravity).</param>
    /// <returns>True si une solution existe, False sinon.</returns>
    public static bool CalculateLaunchVelocity(
        Vector3 startPoint,
        Vector3 targetPoint,
        out Vector3 initialVelocity,
        out float flightDuration,
        Vector3 gravity = default)
    {
        initialVelocity = Vector3.zero;
        flightDuration = 0f;

        if (gravity == default)
        {
            gravity = Physics.gravity;
        }

        Vector3 displacement = targetPoint - startPoint;
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0f, displacement.z);
        float horizontalDistance = horizontalDisplacement.magnitude;
        float heightDifference = displacement.y;
        float g = gravity.magnitude;

        // Cas impossible : cible à la même position
        if (horizontalDistance == 0f && heightDifference == 0f)
        {
            return false;
        }

        // Calcul du temps de vol optimal
        // On résout l'équation :
        // heightDifference = v0y * t - 0.5 * g * t^2
        // horizontalDistance = v0x * t
        // On exprime v0y en fonction de t :
        // v0y = (heightDifference + 0.5 * g * t^2) / t
        // La vitesse totale est v0 = sqrt(v0x^2 + v0y^2) = sqrt((horizontalDistance / t)^2 + v0y^2)
        // On cherche t qui minimise v0.

        // Solution analytique pour t :
        // t = sqrt(2 * horizontalDistance / g) * (1 + sqrt(1 + (2 * g * heightDifference) / (horizontalDistance * horizontalDistance)))
        // Mais cette formule ne fonctionne pas toujours. On utilise une approche numérique.

        // On utilise la formule de la portée maximale pour un angle de 45° :
        // Si la cible est à la même hauteur (heightDifference = 0), alors :
        // t = horizontalDistance / v0x
        // Et v0y = 0.5 * g * t
        // Donc v0 = sqrt(v0x^2 + v0y^2) = sqrt((horizontalDistance / t)^2 + (0.5 * g * t)^2)
        // On dérive par rapport à t et on trouve t = sqrt(2 * horizontalDistance / g)

        // Pour une cible à une hauteur différente, on utilise :
        float a = 0.5f * g;
        float b = -heightDifference;
        float c = -0.5f * g * horizontalDistance * horizontalDistance;

        // Équation quadratique : a * t^2 + b * t + c = 0
        // Mais ici, on a :
        // heightDifference = v0y * t - 0.5 * g * t^2
        // horizontalDistance = v0x * t
        // On suppose que v0x et v0y sont liés par l'angle theta : v0y = v0x * tan(theta)
        // On peut exprimer v0x = horizontalDistance / t
        // Donc v0y = (horizontalDistance / t) * tan(theta)
        // En substituant dans l'équation verticale :
        // heightDifference = (horizontalDistance / t) * tan(theta) * t - 0.5 * g * t^2
        // heightDifference = horizontalDistance * tan(theta) - 0.5 * g * t^2
        // On veut éliminer theta. On utilise le fait que tan(theta) = v0y / v0x = (v0y * t) / horizontalDistance
        // Mais cela ne simplifie pas les choses.

        // On utilise la solution de l'équation quadratique pour t :
        // 0.5 * g * t^2 - v0y * t + heightDifference = 0
        // Mais on ne connaît pas v0y. On utilise la relation v0y = (horizontalDistance / t) * tan(theta)
        // et on suppose que theta est l'angle optimal.

        // Solution alternative : on utilise la formule de la trajectoire parabolique en 3D.
        // On résout pour t dans :
        // 0.5 * g * t^2 - heightDifference = v0y * t
        // Et horizontalDistance = v0x * t
        // On suppose que v0x et v0y sont liés par l'angle theta, mais on peut aussi exprimer v0y en fonction de t :
        // v0y = (heightDifference + 0.5 * g * t^2) / t
        // La vitesse totale est :
        // v0 = sqrt((horizontalDistance / t)^2 + ((heightDifference + 0.5 * g * t^2) / t)^2)
        // On cherche t qui minimise v0. On dérive v0 par rapport à t et on trouve :
        // t = sqrt( (2 * (horizontalDistance^2 + heightDifference^2)) / (g * heightDifference) )
        // Mais cette formule ne fonctionne que si heightDifference > 0.

        // On utilise une approche numérique pour trouver t :
        float t = Mathf.Sqrt(2f * horizontalDistance / g);
        if (heightDifference > 0f)
        {
            t = Mathf.Sqrt((2f * (horizontalDistance * horizontalDistance + heightDifference * heightDifference)) / (g * heightDifference));
        }

        // On vérifie si t est valide
        if (float.IsNaN(t) || t <= 0f)
        {
            return false;
        }

        flightDuration = t;
        float v0x = horizontalDistance / t;
        float v0y = (heightDifference + 0.5f * g * t * t) / t;

        // Calcul de la vitesse initiale
        initialVelocity = horizontalDisplacement.normalized * v0x;
        initialVelocity.y = v0y;

        return true;
    }
}