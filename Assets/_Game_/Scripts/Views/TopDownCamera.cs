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
    
    [Header("Settings")]
    [SerializeField] private Light spotLight;
    [SerializeField] private Vector3 focusPosition;
    [SerializeField] private Bounds worldBounds;
    [SerializeField] private float height = 1.5f;
    [SerializeField] private float inputSpeed = 10.0f;
    [SerializeField] private float lerpSpeed = 1.5f;
    [SerializeField] private float panSpeed = 8.0f;
    [SerializeField] private float zoomSpeed = 1.0f;
    [SerializeField] private Vector2 rotationSpeed = new Vector2(3.0f, 6.0f);
    [SerializeField] private float lightRange = 0.0f;
    [SerializeField] private float lightRangeToAngle = Mathf.PI;
    [Min(0f)][SerializeField] private float forwardOffset = 0.0f;
    [SerializeField] private Vector2 headingOffset = new Vector2(1.0f, 0.0f);
    [SerializeField] private bool canRotateY = true;
    [SerializeField] private int screenPadding = 2;
    [SerializeField] private AnimationCurve heightForwardCurve = AnimationCurve.Constant(0f,0f,0f);
    [ReadOnly] [SerializeField] private float rotationStepAngle = 90.0f;
    
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
        yaw = (yaw - rotationStepAngle * step) % 360;
    }

    [Header("Debug")]
    [ReadOnly] [SerializeField] private Vector3 currentPosition;
    [ReadOnly] [SerializeField] private Quaternion desiredRotation;
    [ReadOnly] [SerializeField]  private float forwardValue;
    [ReadOnly] [SerializeField]  private Vector3 offsetedPosition;
    [ReadOnly] [SerializeField]  private float evaluatedHeight;
    [ReadOnly] [SerializeField]  private float currentYaw;
    [ReadOnly] [SerializeField]  private float yaw; 

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
        evaluatedHeight = heightForwardCurve.Evaluate(focusPosition.y);
        currentYaw = !Application.isPlaying ? yaw : Mathf.LerpAngle(currentYaw, yaw, Time.unscaledDeltaTime * rotationSpeed.y * Consts.RAD_TO_DEG);
        focusPosition = worldBounds.ClosestPoint(focusPosition);
        forwardValue = forwardOffset * evaluatedHeight;
        offsetedPosition = focusPosition - Quaternion.Euler(0, currentYaw, 0) * Vector3.forward * forwardValue;
        offsetedPosition.y += headingOffset.y * evaluatedHeight;
        currentPosition = Vector3.Lerp(currentPosition, offsetedPosition,
            Application.isPlaying ? Time.unscaledDeltaTime * lerpSpeed : 1);
        transform.position = currentPosition + Vector3.up * height;
        desiredRotation =
            Quaternion.LookRotation(((focusPosition + Quaternion.Euler(0, currentYaw, 0) * Vector3.forward * forwardValue * headingOffset.x) - (focusPosition + Vector3.up * height))
                .normalized);
        transform.rotation = Application.isPlaying ? Quaternion.Lerp(transform.rotation, desiredRotation, rotationSpeed.x * Time.unscaledDeltaTime) : desiredRotation;
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
