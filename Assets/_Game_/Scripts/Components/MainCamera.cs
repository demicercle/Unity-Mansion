using UnityEngine;
using LightType = UnityEngine.LightType;

public class MainCamera : MonoBehaviour
{
    public Light spotLight;
    public Vector3 focusPosition;
    public Bounds worldBounds;
    public float height = 1.5f;
    public float speed = 1.5f;
    public float zoomSpeed = 1.0f;
    public float lightRange = 0.0f;
    public float lightRangeToAngle = Mathf.PI;

    public void Focus(GameObject go) => Focus(go.transform.position);
    public void Focus(Component comp) => Focus(comp.transform.position);
    public void Focus(Vector3 world)
    {
        focusPosition = world;
    }
    
    public void Move(Vector2 readValue)
    {
        focusPosition.x += readValue.x;
        focusPosition.z += readValue.y;
    }

    public void Zoom(float delta)
    {
        focusPosition.y -= zoomSpeed * delta;
    }

    private Vector3 currentPosition;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(focusPosition, 0.5f);
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        if (spotLight == null) spotLight = gameObject.AddComponent<Light>();
        UpdatePosition();
        UpdateSpotLight();
    }

    private void Update()
    {
        UpdatePosition();
        UpdateSpotLight();
    }

    // Update is called once per frame
    void UpdatePosition()
    {
        focusPosition = worldBounds.ClosestPoint(focusPosition);
        currentPosition = Vector3.Lerp(currentPosition, focusPosition,
            Application.isPlaying ? Time.unscaledDeltaTime * speed : 1);
        transform.position = currentPosition + Vector3.up * height;
        transform.rotation = Quaternion.LookRotation(-Vector3.up);
    }

    void UpdateSpotLight()
    {
        if (spotLight == null) return;
        spotLight.type = LightType.Spot;
        spotLight.range = currentPosition.y + height + lightRange;
        spotLight.spotAngle = spotLight.range * lightRangeToAngle;
    }
}
