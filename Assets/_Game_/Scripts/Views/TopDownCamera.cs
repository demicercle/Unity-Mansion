using System;
using System.Linq;
using UnityEngine;
using LightType = UnityEngine.LightType;

public class TopDownCamera : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(focusPosition, 0.5f);
        UpdatePosition();
        UpdateSpotLight();
        UpdateBounds();
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
    }
#endif
    
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
    
    
    [ReadOnly] [SerializeField] private Vector3 focusPosition;
    
    [Header("Editor")] 
    [SerializeField] private bool enableEditorMovement;
    [Range(0f,1f)] [SerializeField] private float editorFocusX = .5f;
    [Range(0f,1f)] [SerializeField] private float editorFocusZ = .5f;
    [Range(0f,1f)] [SerializeField] private float editorZoom = 0f;
    
    [Header("Bounds")]
    [SerializeField] private Bounds worldBounds;
    [SerializeField] private float worldY = 0f;
    [Min(0f)][SerializeField] private float worldHeight = 12f;
    
    [Header("SpotLight")]
    [SerializeField] private bool updateSpotLight = true;
    [SerializeField] private float lightRange = 0.0f;
    [SerializeField] private float lightRangeToAngle = Mathf.PI;
    
    [Header("Inputs")]
    [SerializeField] private float inputSpeed = 10.0f;
    [SerializeField] private float panSpeed = 8.0f;
    [SerializeField] private int screenPadding = 2;
    
    [Header("Movement")]
    [SerializeField] private float height = 1.5f;
    [SerializeField] private float lerpSpeed = 1.5f;
    [SerializeField] private float zoomSpeed = 1.0f;
    [SerializeField] private Vector2 rotationSpeed = new Vector2(3.0f, 6.0f);
    
    [Header("Yaw Control")]
    [SerializeField] private bool canRotateY = true;
    [SerializeField] private float rotationStepAngle = 90.0f;
    
    [Header("Heading Control")]
    [SerializeField] private AnimationCurve heightForwardCurve = AnimationCurve.Constant(0f,0f,0f);
    [Min(0f)][SerializeField] private float forwardOffset = 0.0f;
    [SerializeField] private Vector2 headingOffset = new Vector2(1.0f, 0.0f);
    //[ReadOnly] [SerializeField] private bool updateSunLightPosition = true;
    
    #region PUBLIC
     public void SetFocusPosition(Vector3 pos, bool updatePosition = true)
    {
        focusPosition = pos;
        if(updatePosition) UpdatePosition();
    }

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
        world = Quaternion.Euler(0, yaw, 0) * world;
        //if (readValue.magnitude > 0.001f) Debug.Log(this + " Move " + readValue);
        focusPosition.x += world.x * inputSpeed;
        focusPosition.z += world.z * inputSpeed;
    }

    public void Zoom(float delta)
    {
        currentZoom = Mathf.Clamp01(currentZoom + zoomSpeed * delta);
    }

    public void Rotate(float step)
    {
        if (!canRotateY) return;
        yaw = (yaw - rotationStepAngle * step) % 360;
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
            Debug.Log(this + " " + mousePosition + " " + screen + " " + motion);
            focusPosition += Quaternion.Euler(0, yaw, 0) * motion * panSpeed * scale;
        }
    }
    
    public void UpdatePosition(float forcedEditorZoom = -1f)
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            if (forcedEditorZoom >= 0f)
            {
                currentZoom = editorZoom = forcedEditorZoom;
            }
            else if (enableEditorMovement)
            {
                currentZoom = editorZoom;
                
                focusPosition.x = Mathf.Lerp(worldBounds.center.x - worldBounds.extents.x, worldBounds.center.x + worldBounds.extents.x, editorFocusX);
                focusPosition.z = Mathf.Lerp(worldBounds.center.z - worldBounds.extents.z, worldBounds.center.z + worldBounds.extents.z, editorFocusZ);
            }
        }
        
        focusPosition.y = Mathf.Lerp(worldBounds.center.y - worldBounds.extents.y, worldBounds.center.y + worldBounds.extents.y, currentZoom);
        
        evaluatedHeight = heightForwardCurve.Evaluate(1f - currentZoom);
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
    
    #endregion

    [Header("Debug")]
    [ReadOnly] [SerializeField] private Vector3 currentPosition;
    [ReadOnly] [SerializeField] private Quaternion desiredRotation;
    [ReadOnly] [SerializeField]  private float currentZoom; 
    [ReadOnly] [SerializeField]  private float forwardValue;
    [ReadOnly] [SerializeField]  private Vector3 offsetedPosition;
    [ReadOnly] [SerializeField]  private float evaluatedHeight;
    [ReadOnly] [SerializeField]  private float currentYaw;
    [ReadOnly] [SerializeField]  private float yaw; 
    [ReadOnly][SerializeField] private Light spotLight;
    
    #region PRIVATE
    
    private Light sunLight; 

    private void Update()
    {
        UpdateBounds();
        UpdatePosition();
        UpdateSpotLight();
    }

    private void UpdateBounds()
    {
        if (worldHeight <= 0f) return;
        worldBounds.center = Vector3.up * worldHeight;
        worldBounds.extents = new Vector3(worldBounds.extents.x, worldHeight, worldBounds.extents.z);
    }

    private void UpdateSpotLight()
    {
        if (!updateSpotLight) return;
        if (spotLight == null) spotLight = gameObject.GetComponent<Light>();
        if (spotLight == null) return;
        if (spotLight.type != LightType.Spot || !spotLight.enabled) return;
        spotLight.range = currentPosition.y + height + lightRange;
        spotLight.spotAngle = spotLight.range * lightRangeToAngle;
    }
    
    #endregion
}

/*#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(TopDownCamera))]
public class TopDownCameraEditor : UnityEditor.Editor
{
    static private GameObject editorCameraTarget;
    private void DuringSceneGUI(object arg)
    {
        if(editorCameraTarget == null) editorCameraTarget = GameObject.Find("EditorCameraTarget");
        if(editorCameraTarget == null) editorCameraTarget = new GameObject("EditorCameraTarget");
        editorCameraTarget.hideFlags = HideFlags.DontSave;
        (target as TopDownCamera).SetFocusPosition(editorCameraTarget.transform.position);
    }

    private void OnEnable()
    {
        UnityEditor.SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        UnityEditor.SceneView.duringSceneGui -= DuringSceneGUI;
    }
}
#endif*/