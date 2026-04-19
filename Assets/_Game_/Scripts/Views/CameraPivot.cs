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
        editorYaw = 0;
        editorFocusY = 0f;
        editorFocusX = editorFocusZ = 0.5f;
        UpdatePosition();
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
    
    [Header("Settings")]
    [Range(0f,1f)] [SerializeField] private float startZoom = .5f;
    [SerializeField] private bool canRotateY = true;
    [SerializeField] private float rotationStepAngle = 0.0f;
    [Space]
    [Header("User Settings")]
    [SerializeField] private float lerpSpeed = 1.5f;
    [SerializeField] private float inputSpeed = 10.0f;
    [SerializeField] private float panSpeed = 8.0f;
    [SerializeField] private int screenPadding = 2;
    [SerializeField] private float zoomSpeed = 1.0f;
    [SerializeField] private Vector2 rotationSpeed = new Vector2(3.0f, 6.0f);
    [SerializeField] private Vector2 orbitSpeed = new Vector2(3.0f, 6.0f);
    [SerializeField] private float orbitDrag = 0.1f;
    
    void UpdateSettings()
    {
        if (!userSettings) return;
        lerpSpeed = userSettings.lerpSpeed;
        inputSpeed = userSettings.inputSpeed;
        panSpeed = userSettings.panSpeed;
        screenPadding = userSettings.screenPadding;
        zoomSpeed = userSettings.zoomSpeed;
        rotationSpeed = userSettings.rotationSpeed;
        orbitSpeed = userSettings.orbitSpeed;
        orbitDrag = userSettings.orbitDrag;
    }

    [Header("References")] 
    [SerializeField] private Animator animator;
    [SerializeField] private string zoomParameter;
    [SerializeField] private Transform rotationPivot;
    
    #region PUBLIC

    public Bounds GetWorldBounds() => worldBounds;
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
    
    public void MoveFocus(Vector2 readValue, Space spaceTo)
    {
        if (spaceTo == Space.World)
        {
            focusPosition.x += readValue.x / Screen.width * orbitDrag;
            focusPosition.z += readValue.y / Screen.height * orbitDrag;
        }
        else
        {
            var world = new Vector3(readValue.x, 0, readValue.y);
            world = Quaternion.Euler(0, desiredYaw, 0) * world;
            focusPosition.x += world.x * inputSpeed;
            focusPosition.z += world.z * inputSpeed;
        }
    }

    public void Zoom(float delta)
    {
        currentZoom = Mathf.Clamp01(currentZoom + zoomSpeed * delta * 0.1f);
        UpdateAnimator();
    }

    public void Rotate(float delta)
    {
        //Debug.LogWarning("do nothing!");
        if (!canRotateY) return;
        desiredYaw = (desiredYaw - rotationStepAngle * delta);
    }

    public bool OrbitCanZoom() => orbitSpeed.y > 0;

    //public void Orbit(Vector2 delta) => Orbit(delta.x, delta.y);
    public void Orbit(float deltaX, float deltaY)
    {
        if (!canRotateY) return;
        desiredYaw += deltaX * orbitSpeed.x;
        Zoom(deltaY * orbitSpeed.y);
        // desiredYaw = Mathf.Clamp(-180)
    }

   // public void PanWithMouse(float mouseY, float scale) => PanWithMouse(new Vector2(Screen.width / 2f, mouseY), scale);
    public void PanWithMouse(Vector2 mousePosition, float scale)
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
    
    #region PRIVATE
    
    private Light sunLight;
    private float desiredYaw;
    private float currentYaw;
    private float currentZoom;
    private Quaternion desiredRotation;
    private Vector3 currentPosition;

    private UserSettings userSettings;
    
    void Awake()
    {
        currentYaw = desiredYaw = startZoom;
    }

    private void Start()
    {
        userSettings = UserSettings.GetCurrentSettings();
        UpdateAnimator();
    }

    private void Update()
    {
        UpdateSettings();
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
                focusPosition.y = Mathf.Lerp(worldBounds.center.y - worldBounds.extents.y, worldBounds.center.y + worldBounds.extents.y, editorFocusY);
                focusPosition.x = Mathf.Lerp(worldBounds.center.x - worldBounds.extents.x, worldBounds.center.x + worldBounds.extents.x, editorFocusX);
                focusPosition.z = Mathf.Lerp(worldBounds.center.z - worldBounds.extents.z, worldBounds.center.z + worldBounds.extents.z, editorFocusZ);
            }
        }
#endif
        
        focusPosition = worldBounds.ClosestPoint(focusPosition);
        
        // lerp rotation
        currentYaw = !Application.isPlaying ? enableEditorMovement ? editorYaw : desiredYaw : Mathf.LerpAngle(currentYaw, desiredYaw, Time.unscaledDeltaTime * rotationSpeed.y * Consts.RAD_TO_DEG);
        
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
