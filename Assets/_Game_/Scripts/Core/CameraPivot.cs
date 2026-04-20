using System;
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

    [ContextMenu("FocusFirstCharacter")]
    void FocusFirstCharacter()
    {
        var firstCharacter = Utils.FindObjects<CharacterController>()
            .OrderBy(c => (c.CompareTag(Consts.TAG_PLAYER) ? 0 : 1) + c.transform.GetSiblingIndex())
            .FirstOrDefault();
        
        if (firstCharacter)
        {
            enableEditorMovement = false;
            Focus(firstCharacter);
            UpdatePosition();
            return;
        }
        
        Debug.LogWarning("no character found!");
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
    [SerializeField] private Transform focusTransform;
    [Space]
    [Range(0f,1f)] [SerializeField] private float editorFocusY = 0f;
    [Range(0f,1f)] [SerializeField] private float editorFocusX = .5f;
    [Range(0f,1f)] [SerializeField] private float editorFocusZ = .5f;
    [Space]
    [Range(-180f, 180)][SerializeField]  private float editorYaw; 
    [Space]
    [Header("Bounds")]
    [SerializeField] private Bounds worldBounds;
    [SerializeField] private float worldY = 0f;
    [Min(0f)][SerializeField] private float worldHeight = 12f;
    [Space]
    [Header("Settings")]
    [Range(0f,1f)] [SerializeField] private float startZoom = .5f;
    [Range(0f,1f)] [SerializeField] private  float awakeZoom = 0.2f;
    [SerializeField] private bool canRotateY = true;
    [SerializeField] private float clampedRotationAngle = 0.0f;
    [SerializeField] private bool orbitFromScreenCenter = true; // TODO in Settings
    [Space]
    [Header("ReadOnly")]
    [ReadOnly] [SerializeField] private float lerpSpeed = 1.5f;
    [ReadOnly] [SerializeField] private float inputSpeed = 10.0f;
    [ReadOnly] [SerializeField] private float panSpeed = 8.0f;
    [ReadOnly] [SerializeField] private int screenPadding = 2;
    [ReadOnly] [SerializeField] private float zoomSpeed = 1.0f;
    [ReadOnly] [SerializeField] private Vector2 rotationSpeed = new Vector2(3.0f, 6.0f);
    [ReadOnly] [SerializeField] private Vector2 orbitSpeed = new Vector2(3.0f, 6.0f);
    [ReadOnly] [SerializeField] private float orbitDrag = 0.1f;
    
    void UpdateSettings()
    {
        if (!_cameraSettings) return;
        lerpSpeed = _cameraSettings.lerpSpeed;
        inputSpeed = _cameraSettings.inputSpeed;
        panSpeed = _cameraSettings.panSpeed;
        screenPadding = _cameraSettings.screenPadding;
        zoomSpeed = _cameraSettings.zoomSpeed;
        rotationSpeed = _cameraSettings.rotationSpeed;
        orbitSpeed = _cameraSettings.orbitSpeed;
        orbitDrag = _cameraSettings.orbitDrag;
    }

    [Header("References")] 
    [SerializeField] private Animator animator;
    [SerializeField] private string zoomParameter;
    [SerializeField] private Transform rotationPivot;
    
    #region PUBLIC
    public Vector3 GetFocusPosition()
    {
        return focusPosition;
    }
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
        lastTimeFocused = Time.realtimeSinceStartup;
    }
    
    public void OrbitDrag(Vector2 readValue)
    {
        focusPosition.x += readValue.x / Screen.width * orbitDrag;
        focusPosition.z += readValue.y / Screen.height * orbitDrag;
    }

    public void MoveFromInput(Vector2 readValue)
    {
        var world = new Vector3(readValue.x, 0, readValue.y);
        world = Quaternion.Euler(0, desiredYaw, 0) * world;
        focusPosition.x += world.x * inputSpeed;
        focusPosition.z += world.z * inputSpeed;
    }

    public void Zoom(float delta)
    {
        desiredZoom = Mathf.Clamp01(desiredZoom + zoomSpeed * delta * 0.1f);
        UpdateAnimator();
    }

    public void SetStartZoom() => SetZoom(startZoom);
    public void SetZoom(float value)
    {
        desiredZoom = Mathf.Clamp01(value);
        UpdateAnimator();
    }

    public void ClampedRotation(float delta)
    {
        //Debug.LogWarning("do nothing!");
        if (!canRotateY) return;
        desiredYaw = (desiredYaw - clampedRotationAngle * delta);
    }

    public bool OrbitCanZoom() => orbitSpeed.y > 0;

    public bool OrbitFromScreenCenter() => orbitFromScreenCenter;

    //public void Orbit(Vector2 delta) => Orbit(delta.x, delta.y);
    public void Orbit(float deltaX, float deltaY, bool scaleWithInputs)
    {
        if (!canRotateY) return;
        desiredYaw += deltaX * orbitSpeed.x * (scaleWithInputs ? inputSpeed * Mathf.PI : 1.0f);
        Zoom(deltaY * orbitSpeed.y);
        // desiredYaw = Mathf.Clamp(-180)
    }

   // public void PanWithMouse(float mouseY, float scale) => PanWithMouse(new Vector2(Screen.width / 2f, mouseY), scale);
    public bool TryPanWithMouse(Vector2 mousePosition, float scale)
    {
        if (Time.realtimeSinceStartup - lastTimeFocused < CameraSettings.DefaultSettings.cameraFocusLockTime) return false;
        if (mousePosition.x < 0 || mousePosition.y < 0) return false;
        var screen = new Vector2(Screen.width, Screen.height);
        if (mousePosition.x > screen.x || mousePosition.y > screen.y) return false;
        var motion = new Vector3();
        if (mousePosition.x - screenPadding <= 0f) motion.x = -1f;
        if (mousePosition.x + screenPadding >= screen.x) motion.x = 1f;
        if (mousePosition.y - screenPadding <= 0f) motion.z = -1f;
        if (mousePosition.y + screenPadding >= screen.y) motion.z = 1f;
        if (motion.sqrMagnitude > 0f)
        {
            //Debug.Log(this + " " + mousePosition + " " + screen + " " + motion);
            focusPosition += Quaternion.Euler(0, desiredYaw, 0) * motion * panSpeed * scale;
            return true;
        }

        return false;
    }
    
    #endregion
    
    #region PRIVATE
    
    private Light sunLight;
    private float desiredYaw;
    private float currentYaw;
    private float currentZoom;
    private float desiredZoom;
    private float lastTimeFocused;
    private Quaternion desiredRotation;
    private Vector3 currentPosition;

    private CameraSettings _cameraSettings;

    void Awake()
    {
        desiredZoom = currentZoom = awakeZoom;
    }

    private void Start()
    {
        _cameraSettings = CameraSettings.DefaultSettings;
        UpdateAnimator();
    }

    private void LateUpdate()
    {
        UpdateSettings();
        UpdateBounds();
        UpdatePosition();
        UpdateAnimator();
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
        currentZoom = Mathf.Lerp(currentZoom, desiredZoom, Time.unscaledDeltaTime * Mathf.PI);
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (focusTransform)
            {
                Focus(focusTransform);
                focusTransform = null;
            }
            
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
    
    #endregion

    
}
