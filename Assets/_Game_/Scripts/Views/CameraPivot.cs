using System.Linq;
using UnityEngine;
using LightType = UnityEngine.LightType;

public class CameraPivot : MonoBehaviour
{
#if UNITY_EDITOR
    //public Object controllerObj;
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(focusPosition, 0.5f);
        UpdateBounds();
        UpdatePosition();
        //UpdateAnimator();
        //UpdateSpotLight();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    }
    
    [ContextMenu("UpdateSunLight")]
    void UpdateSunLight()
    {
        //if (!updateSunLightPosition) return;
        if (!sunLight)
            sunLight = FindObjectsByType<Light>(default)
                .FirstOrDefault(l => l.type == LightType.Directional && l.CompareTag(Consts.TAG_PLAYER));
        if (sunLight)
        {
            sunLight.transform.rotation = transform.rotation;
        }
    }

    [ContextMenu("ResetFocus")]
    void ResetFocus()
    {
        focusPosition = worldBounds.center;
        editorFocusY = 0f;
        editorFocusX = editorFocusZ = 0.5f;
        Utils.SetDirty(this);
    }
#endif
    
    static private CameraPivot instance;

    static public CameraPivot Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectsByType<CameraPivot>(default).FirstOrDefault();
            }
            
            return instance;
        }
    }
    
    
    [ReadOnly] [SerializeField] private Vector3 focusPosition;
    
    [Header("Editor")] 
    [SerializeField] private bool enableEditorMovement;
    [Space]
    [Range(0f,1f)] [SerializeField] private float editorFocusY = 0f;
    [Range(0f,1f)] [SerializeField] private float editorFocusX = .5f;
    [Range(0f,1f)] [SerializeField] private float editorFocusZ = .5f;
    [Space]
    [Range(-180f, 180)][SerializeField]  private float editorYaw; 
    //[Space]
    //[Range(0f,1f)] [SerializeField] private float editorZoom = 0f;
    
    [Header("Bounds")]
    [SerializeField] private Bounds worldBounds;
    [SerializeField] private float worldY = 0f;
    [Min(0f)][SerializeField] private float worldHeight = 12f;
    
    [Header("Movement")]
    [SerializeField] private float lerpSpeed = 1.5f;
    [SerializeField] private float inputSpeed = 10.0f;
    [SerializeField] private float panSpeed = 8.0f;
    [SerializeField] private int screenPadding = 2;
    [Range(0f,1f)] [SerializeField] private float startZoom = .5f;
    [SerializeField] private float zoomSpeed = 1.0f;
    [SerializeField] private Vector3 rotationSpeed = new Vector2(3.0f, 6.0f);
    [SerializeField] private bool canRotateY = true;
    //[SerializeField] private float rotationStepAngle = 90.0f;
    
    //[Header("Heading Control")]
    //[Min(0f)][SerializeField] private float forwardOffset = 0.0f;
    //[SerializeField] private AnimationCurve zoomForwardCurve = AnimationCurve.Constant(0f,0f,0f);
    //[SerializeField] private Vector3 headingOffset = new Vector3(1.0f, 0.0f, 0.0f);
    
  /*  [Header("SpotLight")]
    [SerializeField] private Light spotLight;
    [SerializeField] private float lightRange = 0.0f;
    //TODO [SerializeField] private float lightRangeMinAngle = -1f;
    [SerializeField] private float lightRangeToAngle = Mathf.PI;
    //TODO [SerializeField] private AnimationCurve lightRangeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    //TODO [SerializeField] private AnimationCurve lightIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    //TODO [ReadOnly] [SerializeField] private bool updateSunLightPosition = true;*/

    [Header("References")] 
    [SerializeField] private Animator animator;

    [SerializeField] private string zoomParameter;
    [SerializeField] private Transform rotationPivot;
    
    #region PUBLIC
    public void SetWorldBounds(Bounds bounds, bool updatePosition = true)
    {
        worldBounds = bounds;
        if(updatePosition) UpdatePosition();
    }

    public void Focus(UnityEngine.Object go)
    {
        if(go is Component) Focus(((Component)go).transform.position);
        else if (go is GameObject) Focus(((GameObject)go).transform.position);
        else Debug.LogWarning(this + " invalid object to focus: " + go);
    }
    
    public void Focus(Vector3 world)
    {
        focusPosition = world;
    }
    
    public void Move(Vector2 readValue)
    {
        var world = new Vector3(readValue.x, 0, readValue.y);
        world = Quaternion.Euler(0, desiredYaw, 0) * world;
        //if (readValue.magnitude > 0.001f) Debug.Log(this + " Move " + readValue);
        focusPosition.x += world.x * inputSpeed;
        focusPosition.z += world.z * inputSpeed;
    }

    public void Zoom(float delta)
    {
        currentZoom = Mathf.Clamp01(currentZoom + zoomSpeed * delta * 0.1f);
        UpdateAnimator();
    }

    public void RotateLR(bool left)
    {
        Debug.LogWarning("do nothing!");
       // if (!canRotateY) return;
      //  desiredYaw = (desiredYaw - rotationStepAngle * (left ? -1 : 1));
      //  desiredYaw %= 360;
    }

    public void Rotate(float delta)
    {
        if (!canRotateY) return;
        desiredYaw += delta * rotationSpeed.z;
      // desiredYaw = Mathf.Clamp(-180)
    }
    
    public void PanWithMouse(Vector3 mousePosition, float scale)
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
            //Debug.Log(this + " " + mousePosition + " " + screen + " " + motion);
            focusPosition += Quaternion.Euler(0, desiredYaw, 0) * motion * panSpeed * scale;
        }
    }
    
    #endregion
    
    [Header("Debug")]
    [ReadOnly] [SerializeField] private Vector3 currentPosition;
    [ReadOnly] [SerializeField] private Quaternion desiredRotation;
    [ReadOnly] [SerializeField]  private float currentZoom; 
    //[ReadOnly] [SerializeField]  private float forwardValue;
  //  [ReadOnly] [SerializeField]  private Vector3 offsetedPosition;
 //   [ReadOnly] [SerializeField]  private float evaluatedHeight;
    [ReadOnly] [SerializeField]  private float desiredYaw;
    [ReadOnly] [SerializeField]  private float currentYaw;
    
    #region PRIVATE
    
    private Light sunLight;

    void Awake()
    {
        currentYaw = desiredYaw = startZoom;
    }

    private void Start()
    {
        UpdateAnimator();
    }

    private void Update()
    {
        UpdateBounds();
        UpdatePosition();
        //UpdateSpotLight();
    }

    private void UpdateAnimator()
    {
        if (animator && !string.IsNullOrEmpty(zoomParameter))
        {
            animator.SetFloat(zoomParameter, currentZoom);
        }
    }
    
    private void UpdateBounds()
    {
        if (worldHeight <= 0f) return;
        worldBounds.center = Vector3.up * worldHeight + Vector3.up * worldY;
        worldBounds.extents = new Vector3(worldBounds.extents.x, worldHeight, worldBounds.extents.z);
    }
    
    private void UpdatePosition()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (enableEditorMovement)
            {
                currentYaw = editorYaw;
                focusPosition.y = Mathf.Lerp(worldBounds.center.y - worldBounds.extents.y, worldBounds.center.y + worldBounds.extents.y, editorFocusY);
                focusPosition.x = Mathf.Lerp(worldBounds.center.x - worldBounds.extents.x, worldBounds.center.x + worldBounds.extents.x, editorFocusX);
                focusPosition.z = Mathf.Lerp(worldBounds.center.z - worldBounds.extents.z, worldBounds.center.z + worldBounds.extents.z, editorFocusZ);
            }
        }
#endif
        
        // clamp
        //focusPosition.y = Mathf.Lerp(worldBounds.center.y - worldBounds.extents.y, worldBounds.center.y + worldBounds.extents.y, currentZoom);
        focusPosition = worldBounds.ClosestPoint(focusPosition);
        
        // lerp rotation
        currentYaw = !Application.isPlaying ? enableEditorMovement ? editorYaw : desiredYaw : Mathf.LerpAngle(currentYaw, desiredYaw, Time.unscaledDeltaTime * rotationSpeed.y * Consts.RAD_TO_DEG);
        
        // zooming
       // evaluatedHeight = zoomForwardCurve.Evaluate(1f - currentZoom);
        
        // heading
       // forwardValue = forwardOffset * evaluatedHeight;
        //offsetedPosition = focusPosition - Quaternion.Euler(0, currentYaw, 0) * Vector3.forward * forwardValue;
       // offsetedPosition.y += headingOffset.y * evaluatedHeight;
        
        // calculate desired rotation look at
     /*   desiredRotation =
            Quaternion.LookRotation(((focusPosition + Quaternion.Euler(0, currentYaw, 0) * Vector3.forward * forwardValue * headingOffset.x) - (focusPosition + Vector3.up * height))
                .normalized);*/
        
        // move forward
       // offsetedPosition += desiredRotation * Vector3.forward * headingOffset.z;
        
        // lerp
        currentPosition = Vector3.Lerp(currentPosition, focusPosition,
            Application.isPlaying ? Time.unscaledDeltaTime * lerpSpeed : 1);
        
        // set
        transform.position = currentPosition;
        transform.rotation = Quaternion.Euler(
            transform.eulerAngles.x,
            currentYaw,
            transform.eulerAngles.z
        );
        //transform.rotation = Application.isPlaying ? Quaternion.Lerp(transform.rotation, desiredRotation, rotationSpeed.x * Time.unscaledDeltaTime) : desiredRotation;
        // transform.position += transform.rotation * Vector3.forward * headingOffset.z;
    }

   /* private void UpdateSpotLight()
    {
        if (spotLight == null) return;
        if (spotLight.type != LightType.Spot || !spotLight.enabled) return;
        //if (lightRange > 0f) spotLight.range = currentPosition.y + height + lightRange;
        if (lightRangeToAngle > 0f) spotLight.spotAngle = spotLight.range * lightRangeToAngle;
        //TODO if (lightRangeCurve.keys.Length>0) spotLight.spotAngle = Mathf.Lerp(lightRangeMinAngle, spotLight.range, lightRangeCurve.Evaluate(currentZoom));
        //TODO if (lightIntensityCurve.keys.Length>0) spotLight.intensity = Mathf.Lerp(minIntensity, minIntensity + intensityDelta, lightIntensityCurve.Evaluate(currentZoom));
    }*/
    
    #endregion
}
