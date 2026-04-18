using System.Linq;
using UnityEngine;
using LightType = UnityEngine.LightType;

public class TopDownCamera : MonoBehaviour
{
    static private TopDownCamera instance;

    static public TopDownCamera Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectsByType<TopDownCamera>(default).FirstOrDefault();
            }
            
            return instance;
        }
    }
    
    public Light spotLight;
    public Vector3 focusPosition;
    public Bounds worldBounds;
    public float height = 1.5f;
    public float inputSpeed = 10.0f;
    public float lerpSpeed = 1.5f;
    public float panSpeed = 8.0f;
    public float zoomSpeed = 1.0f;
    public float rotationSpeed = 3.0f;
    public float lightRange = 0.0f;
    public float lightRangeToAngle = Mathf.PI;
    [Min(0f)]public float forwardOffset = 0.0f;
    public bool canRotateY = true;
    public int screenPadding = 2;
    public AnimationCurve heightForwardCurve = AnimationCurve.Constant(0f,0f,0f);
    
    public void Focus(GameObject go) => Focus(go.transform.position);
    public void Focus(Component comp) => Focus(comp.transform.position);
    public void Focus(Vector3 world)
    {
        focusPosition = world;
    }
    
    public void Move(Vector2 readValue)
    {
        var world = new Vector3(readValue.x, 0, readValue.y);
        world = Quaternion.Euler(0, yaw, 0) * world;
        //if (readValue.magnitude > 0.001f) Debug.Log(this + " Move " + readValue);
        focusPosition.x += world.x * inputSpeed;
        focusPosition.z += world.z * inputSpeed;
    }

    public void Zoom(float delta)
    {
        focusPosition.y -= zoomSpeed * delta;
    }

    public void Rotate(float step)
    {
        if (!canRotateY) return;
        yaw = (yaw + 90 * step) % 360;
    }

    private Vector3 currentPosition;
    private Quaternion desiredRotation;
    private float forwardValue;
    private Vector3 offsetedPosition;

    private float currentYaw;
    private float yaw; //TODO rotate camera?

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
        currentYaw = !Application.isPlaying ? yaw : Mathf.LerpAngle(currentYaw, yaw, Time.unscaledDeltaTime * rotationSpeed * Consts.RAD_TO_DEG);
        focusPosition = worldBounds.ClosestPoint(focusPosition);
        forwardValue = forwardOffset * heightForwardCurve.Evaluate(focusPosition.y);
        offsetedPosition = focusPosition - Quaternion.Euler(0, currentYaw, 0) * Vector3.forward * forwardValue;
        currentPosition = Vector3.Lerp(currentPosition, offsetedPosition,
            Application.isPlaying ? Time.unscaledDeltaTime * lerpSpeed : 1);
        transform.position = currentPosition + Vector3.up * height;
        desiredRotation =
            Quaternion.LookRotation(((focusPosition + Quaternion.Euler(0, currentYaw, 0) * Vector3.forward * forwardValue) - (focusPosition + Vector3.up * height))
                .normalized);
        transform.rotation = Application.isPlaying ? Quaternion.Lerp(transform.rotation, desiredRotation, rotationSpeed * Time.unscaledDeltaTime) : desiredRotation;
    }

    void UpdateSpotLight()
    {
        if (spotLight == null) return;
        spotLight.type = LightType.Spot;
        spotLight.range = currentPosition.y + height + lightRange;
        spotLight.spotAngle = spotLight.range * lightRangeToAngle;
    }

    public void MoveCameraWithCursor(Vector3 mousePosition, float scale)
    {
        if (mousePosition.x < 0 || mousePosition.y < 0) return;
        var screen = new Vector2(Screen.width, Screen.height);
        if (mousePosition.x > screen.x || mousePosition.y > screen.y) return;
        var motion = new Vector3();
        if (mousePosition.x - screenPadding <= 0f) motion.x = -1f;
        if (mousePosition.x + screenPadding >= screen.x) motion.x = 1f;
        if (mousePosition.y - screenPadding <= 0f) motion.z = -1f;
        if (mousePosition.y + screenPadding >= screen.y) motion.z = 1f;
        if (motion.sqrMagnitude > 0f)
        {
            Debug.Log(this + " " + mousePosition + " " + screen + " " + motion);
            focusPosition += Quaternion.Euler(0, yaw, 0) * motion * panSpeed * scale;
        }
    }
}
