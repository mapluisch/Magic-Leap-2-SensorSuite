using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using MagicLeap.OpenXR.Features.EyeTracker;
using MagicLeap.OpenXR.Features.FacialExpressions;
using MagicLeap.Android;
using UnityEngine.InputSystem;
using UnityEngine.XR.MagicLeap;
public class SensorAccess : MonoBehaviour
{
    public static SensorAccess Instance { get; private set; }
    [Header("Permission Status")]
    [SerializeField] private bool eyeTrackingPermissionGranted = false;
    [SerializeField] private bool pupilSizePermissionGranted = false;
    [SerializeField] private bool facialExpressionPermissionGranted = false;
    [SerializeField] private bool audioRecordPermissionGranted = false;
    [SerializeField] private bool cameraPermissionGranted = false;
    [Header("Sensor Availability")]
    [SerializeField] private bool eyeTrackerAvailable = false;
    [SerializeField] private bool facialExpressionAvailable = false;
    [SerializeField] private bool lightSensorAvailable = false;
    [SerializeField] private bool accelerometerAvailable = false;
    [SerializeField] private bool gyroscopeAvailable = false;
    [SerializeField] private bool linearAccelerationAvailable = false;
    [SerializeField] private bool attitudeSensorAvailable = false;
    [SerializeField] private bool audioInputAvailable = false;
    [SerializeField] private bool cameraAvailable = false;
    public event Action<bool> OnEyeTrackingPermissionChanged;
    public event Action<bool> OnFacialExpressionPermissionChanged;
    public event Action<bool> OnAudioRecordPermissionChanged;
    public event Action<bool> OnCameraPermissionChanged;
    public event Action OnAllPermissionsReady;
    private MagicLeapEyeTrackerFeature eyeTrackerFeature;
    private MagicLeapFacialExpressionFeature facialExpressionFeature;
    public bool EyeTrackingPermissionGranted => eyeTrackingPermissionGranted;
    public bool PupilSizePermissionGranted => pupilSizePermissionGranted;
    public bool FacialExpressionPermissionGranted => facialExpressionPermissionGranted;
    public bool AudioRecordPermissionGranted => audioRecordPermissionGranted;
    public bool CameraPermissionGranted => cameraPermissionGranted;
    public bool EyeTrackerAvailable => eyeTrackerAvailable;
    public bool FacialExpressionAvailable => facialExpressionAvailable;
    public bool LightSensorAvailable => lightSensorAvailable;
    public bool AccelerometerAvailable => accelerometerAvailable;
    public bool GyroscopeAvailable => gyroscopeAvailable;
    public bool LinearAccelerationAvailable => linearAccelerationAvailable;
    public bool AttitudeSensorAvailable => attitudeSensorAvailable;
    public bool AudioInputAvailable => audioInputAvailable;
    public bool CameraAvailable => cameraAvailable;
    public MagicLeapEyeTrackerFeature EyeTrackerFeature => eyeTrackerFeature;
    public MagicLeapFacialExpressionFeature FacialExpressionFeature => facialExpressionFeature;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        StartCoroutine(InitializeSensorAccess());
    }
    private IEnumerator InitializeSensorAccess()
    {
        Debug.Log("Starting sensor access initialization...");
        CheckSensorAvailability();
        yield return StartCoroutine(RequestAllPermissions());
        yield return StartCoroutine(InitializeSensorFeatures());
        Debug.Log("Sensor access initialization complete");
        OnAllPermissionsReady?.Invoke();
    }
    private void CheckSensorAvailability()
    {
        Debug.Log("Checking sensor availability...");
        lightSensorAvailable = LightSensor.current != null;
        accelerometerAvailable = Accelerometer.current != null;
        gyroscopeAvailable = UnityEngine.InputSystem.Gyroscope.current != null;
        linearAccelerationAvailable = LinearAccelerationSensor.current != null;
        attitudeSensorAvailable = AttitudeSensor.current != null;
        audioInputAvailable = Application.platform == RuntimePlatform.Android;
        cameraAvailable = Application.platform == RuntimePlatform.Android;
        eyeTrackerFeature = OpenXRSettings.Instance?.GetFeature<MagicLeapEyeTrackerFeature>();
        eyeTrackerAvailable = eyeTrackerFeature != null && eyeTrackerFeature.enabled;
        facialExpressionFeature = OpenXRSettings.Instance?.GetFeature<MagicLeapFacialExpressionFeature>();
        facialExpressionAvailable = facialExpressionFeature != null && facialExpressionFeature.enabled;
        Debug.Log($"Sensor availability - Light: {lightSensorAvailable}, " +
                 $"Accel: {accelerometerAvailable}, Gyro: {gyroscopeAvailable}, " +
                 $"LinearAccel: {linearAccelerationAvailable}, Attitude: {attitudeSensorAvailable}, " +
                 $"EyeTracker: {eyeTrackerAvailable}, Facial: {facialExpressionAvailable}, " +
                 $"Audio: {audioInputAvailable}, Camera: {cameraAvailable}");
    }
    private IEnumerator RequestAllPermissions()
    {
        Debug.Log("Requesting all permissions...");
        if (eyeTrackerAvailable)
        {
            yield return StartCoroutine(RequestEyeTrackingPermissions());
        }
        if (facialExpressionAvailable)
        {
            yield return StartCoroutine(RequestFacialExpressionPermissions());
        }
        if (audioInputAvailable)
        {
            yield return StartCoroutine(RequestAudioRecordPermissions());
        }
        if (cameraAvailable)
        {
            yield return StartCoroutine(RequestCameraPermissions());
        }
        yield return new WaitForSeconds(1.0f);
        VerifyPermissions();
    }
    private IEnumerator RequestEyeTrackingPermissions()
    {
        Debug.Log("Requesting eye tracking permissions...");
        bool permissionRequestComplete = false;
        try
        {
            Permissions.RequestPermissions(
                new string[] { Permissions.EyeTracking, Permissions.PupilSize },
                (permission) =>
                {
                    Debug.Log($"Eye tracking permission granted: {permission}");
                    if (permission == Permissions.EyeTracking)
                        eyeTrackingPermissionGranted = true;
                    if (permission == Permissions.PupilSize)
                        pupilSizePermissionGranted = true;
                    permissionRequestComplete = true;
                },
                (permission) =>
                {
                    Debug.LogError($"Eye tracking permission denied: {permission}");
                    permissionRequestComplete = true;
                }
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error requesting eye tracking permissions: {ex.Message}");
            permissionRequestComplete = true;
        }
        float timeout = 10.0f;
        float elapsed = 0.0f;
        while (!permissionRequestComplete && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        if (elapsed >= timeout)
        {
            Debug.LogWarning("Eye tracking permission request timed out");
        }
    }
    private IEnumerator RequestFacialExpressionPermissions()
    {
        Debug.Log("Requesting facial expression permissions...");
        bool permissionRequestComplete = false;
        try
        {
            Permissions.RequestPermission(
                Permissions.FacialExpression,
                (permission) =>
                {
                    Debug.Log($"Facial expression permission granted: {permission}");
                    facialExpressionPermissionGranted = true;
                    permissionRequestComplete = true;
                },
                (permission) =>
                {
                    Debug.LogError($"Facial expression permission denied: {permission}");
                    permissionRequestComplete = true;
                }
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error requesting facial expression permissions: {ex.Message}");
            permissionRequestComplete = true;
        }
        float timeout = 10.0f;
        float elapsed = 0.0f;
        while (!permissionRequestComplete && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        if (elapsed >= timeout)
        {
            Debug.LogWarning("Facial expression permission request timed out");
        }
    }
    private IEnumerator RequestAudioRecordPermissions()
    {
        Debug.Log("Requesting audio record permissions...");
        bool permissionRequestComplete = false;
        try
        {
            var callbacks = new MLPermissions.Callbacks();
            callbacks.OnPermissionGranted += (permission) =>
            {
                Debug.Log($"Audio record permission granted: {permission}");
                audioRecordPermissionGranted = true;
                permissionRequestComplete = true;
            };
            callbacks.OnPermissionDenied += (permission) =>
            {
                Debug.LogError($"Audio record permission denied: {permission}");
                permissionRequestComplete = true;
            };
            callbacks.OnPermissionDeniedAndDontAskAgain += (permission) =>
            {
                Debug.LogError($"Audio record permission denied and don't ask again: {permission}");
                permissionRequestComplete = true;
            };
            MLPermissions.RequestPermission(MLPermission.RecordAudio, callbacks);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error requesting audio record permissions: {ex.Message}");
            permissionRequestComplete = true;
        }
        float timeout = 10.0f;
        float elapsed = 0.0f;
        while (!permissionRequestComplete && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        if (elapsed >= timeout)
        {
            Debug.LogWarning("Audio record permission request timed out");
        }
    }
    private IEnumerator RequestCameraPermissions()
    {
        Debug.Log("Requesting camera permissions...");
        bool permissionRequestComplete = false;
        try
        {
            var callbacks = new MLPermissions.Callbacks();
            callbacks.OnPermissionGranted += (permission) =>
            {
                Debug.Log($"Camera permission granted: {permission}");
                cameraPermissionGranted = true;
                permissionRequestComplete = true;
            };
            callbacks.OnPermissionDenied += (permission) =>
            {
                Debug.LogError($"Camera permission denied: {permission}");
                permissionRequestComplete = true;
            };
            callbacks.OnPermissionDeniedAndDontAskAgain += (permission) =>
            {
                Debug.LogError($"Camera permission denied and don't ask again: {permission}");
                permissionRequestComplete = true;
            };
            MLPermissions.RequestPermission(MLPermission.Camera, callbacks);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error requesting camera permissions: {ex.Message}");
            permissionRequestComplete = true;
        }
        float timeout = 10.0f;
        float elapsed = 0.0f;
        while (!permissionRequestComplete && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        if (elapsed >= timeout)
        {
            Debug.LogWarning("Camera permission request timed out");
        }
    }
    private void VerifyPermissions()
    {
        eyeTrackingPermissionGranted = Permissions.CheckPermission(Permissions.EyeTracking);
        pupilSizePermissionGranted = Permissions.CheckPermission(Permissions.PupilSize);
        facialExpressionPermissionGranted = Permissions.CheckPermission(Permissions.FacialExpression);
        audioRecordPermissionGranted = MLPermissions.CheckPermission(MLPermission.RecordAudio).IsOk;
        cameraPermissionGranted = MLPermissions.CheckPermission(MLPermission.Camera).IsOk;
        Debug.Log($"Final permission status - EyeTracking: {eyeTrackingPermissionGranted}, " +
                 $"PupilSize: {pupilSizePermissionGranted}, " +
                 $"FacialExpression: {facialExpressionPermissionGranted}, " +
                 $"AudioRecord: {audioRecordPermissionGranted}, " +
                 $"Camera: {cameraPermissionGranted}");
        OnEyeTrackingPermissionChanged?.Invoke(eyeTrackingPermissionGranted && pupilSizePermissionGranted);
        OnFacialExpressionPermissionChanged?.Invoke(facialExpressionPermissionGranted);
        OnAudioRecordPermissionChanged?.Invoke(audioRecordPermissionGranted);
        OnCameraPermissionChanged?.Invoke(cameraPermissionGranted);
    }
    private IEnumerator InitializeSensorFeatures()
    {
        Debug.Log("Initializing sensor features...");
        if (eyeTrackingPermissionGranted && pupilSizePermissionGranted && eyeTrackerAvailable)
        {
            yield return StartCoroutine(InitializeEyeTracker());
        }
        if (facialExpressionPermissionGranted && facialExpressionAvailable)
        {
            yield return StartCoroutine(InitializeFacialExpressions());
        }
        EnableInputSystemSensors();
    }
    private IEnumerator InitializeEyeTracker()
    {
        Debug.Log("Initializing eye tracker...");
        bool initializationSuccessful = false;
        try
        {
            if (eyeTrackerFeature != null)
            {
                eyeTrackerFeature.CreateEyeTracker();
                Debug.Log("Eye tracker initialized successfully");
                initializationSuccessful = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing eye tracker: {ex.Message}");
            eyeTrackerAvailable = false;
            yield break;
        }
        if (initializationSuccessful)
        {
            yield return new WaitForSeconds(0.5f);
            try
            {
                var testData = eyeTrackerFeature.GetEyeTrackerData();
                Debug.Log($"Eye tracker test - Data valid: {!testData.Equals(default(EyeTrackerData))}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Eye tracker test failed: {ex.Message}");
            }
        }
    }
    private IEnumerator InitializeFacialExpressions()
    {
        Debug.Log("Initializing facial expressions...");
        bool initializationSuccessful = false;
        try
        {
            if (facialExpressionFeature != null)
            {
                var allBlendShapes = Enum.GetValues(typeof(FacialBlendShape)) as FacialBlendShape[];
                facialExpressionFeature.CreateClient(allBlendShapes);
                Debug.Log("Facial expression feature initialized successfully");
                initializationSuccessful = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing facial expressions: {ex.Message}");
            facialExpressionAvailable = false;
            yield break;
        }
        if (initializationSuccessful)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }
    private void EnableInputSystemSensors()
    {
        Debug.Log("Enabling input system sensors...");
        try
        {
            if (lightSensorAvailable && LightSensor.current != null)
            {
                InputSystem.EnableDevice(LightSensor.current);
                LightSensor.current.samplingFrequency = 60;
                Debug.Log("Light sensor enabled");
            }
            if (accelerometerAvailable && Accelerometer.current != null)
            {
                InputSystem.EnableDevice(Accelerometer.current);
                Debug.Log("Accelerometer enabled");
            }
            if (gyroscopeAvailable && UnityEngine.InputSystem.Gyroscope.current != null)
            {
                InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
                Debug.Log("Gyroscope enabled");
            }
            if (linearAccelerationAvailable && LinearAccelerationSensor.current != null)
            {
                InputSystem.EnableDevice(LinearAccelerationSensor.current);
                Debug.Log("Linear acceleration sensor enabled");
            }
            if (attitudeSensorAvailable && AttitudeSensor.current != null)
            {
                InputSystem.EnableDevice(AttitudeSensor.current);
                Debug.Log("Attitude sensor enabled");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error enabling input system sensors: {ex.Message}");
        }
    }
    public void RetryPermissions()
    {
        StartCoroutine(RequestAllPermissions());
    }
    public bool AllPermissionsGranted()
    {
        return eyeTrackingPermissionGranted && pupilSizePermissionGranted &&
               facialExpressionPermissionGranted && audioRecordPermissionGranted &&
               cameraPermissionGranted;
    }
    void OnDestroy()
    {
        try
        {
            if (eyeTrackerFeature != null)
            {
                eyeTrackerFeature.DestroyEyeTracker();
            }
            if (facialExpressionFeature != null)
            {
                facialExpressionFeature.DestroyClient();
            }
            if (lightSensorAvailable && LightSensor.current != null)
                InputSystem.DisableDevice(LightSensor.current);
            if (accelerometerAvailable && Accelerometer.current != null)
                InputSystem.DisableDevice(Accelerometer.current);
            if (gyroscopeAvailable && UnityEngine.InputSystem.Gyroscope.current != null)
                InputSystem.DisableDevice(UnityEngine.InputSystem.Gyroscope.current);
            if (linearAccelerationAvailable && LinearAccelerationSensor.current != null)
                InputSystem.DisableDevice(LinearAccelerationSensor.current);
            if (attitudeSensorAvailable && AttitudeSensor.current != null)
                InputSystem.DisableDevice(AttitudeSensor.current);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error during sensor cleanup: {ex.Message}");
        }
    }
}
