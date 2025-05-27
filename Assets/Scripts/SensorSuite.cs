using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using MagicLeap.OpenXR.Features.EyeTracker;
using MagicLeap.OpenXR.Features.FacialExpressions;
using UnityEngine.XR.MagicLeap;
using static UnityEngine.XR.MagicLeap.MLCameraBase.Metadata;
public class SensorSuite : MonoBehaviour
{
    public static SensorSuite Instance { get; private set; }
    [Header("Data Collection Settings")]
    [SerializeField] private float eyeTrackingFrequency = 60f; 
    [SerializeField] private float imuSamplingFrequency = 30f; 
    [SerializeField] private float lightSensorFrequency = 1f; 
    [SerializeField] private float audioSamplingFrequency = 10f; 
    [SerializeField] private float videoCaptureInterval = 0.5f; 
    [SerializeField] private float audioRecordingSegmentLength = 10f; 
    [SerializeField] private bool collectEyeTrackingData = true;
    [SerializeField] private bool collectIMUData = true;
    [SerializeField] private bool collectFacialData = true;
    [SerializeField] private bool collectLightData = true;
    [SerializeField] private bool collectCameraData = true;
    [SerializeField] private bool collectAudioData = true;
    [SerializeField] private bool collectVideoData = true;
    [SerializeField] private bool recordAudioFiles = true;
    [Header("Status")]
    [SerializeField] private bool isCollecting = false;
    [SerializeField] private int totalDataPointsCollected = 0;
    [SerializeField] private bool audioRecordingActive = false;
    [SerializeField] private float currentAudioLevel = 0f;
    [SerializeField] private bool cameraConnected = false;
    [SerializeField] private bool isCapturingVideo = false;
    [SerializeField] private bool audioFileRecordingActive = false;
    [SerializeField] private string currentAudioFileName = "";
    private SensorAccess sensorAccess;
    private CSVWriter csvWriter;
    private Camera mainCamera;
    private Coroutine eyeTrackingCoroutine;
    private Coroutine imuDataCoroutine;
    private Coroutine lightSensorCoroutine;
    private Coroutine audioDataCoroutine;
    private Coroutine videoCaptureCoroutine;
    private Coroutine audioFileRecordingCoroutine;
    private BlendShapeProperties[] blendShapeProperties;
    private MLAudioInput.BufferClip audioBufferClip;
    private readonly float[] audioSamples = new float[128];
    private int audioPosition = 0;
    private const int AUDIO_CLIP_LENGTH_SECONDS = 60;
    private const float AUDIO_SENSITIVITY = 0.02f;
    private MLAudioInput.StreamingClip audioStreamingClip;
    private string audioSaveDirectory;
    private float audioRecordingStartTime;
    private int audioFileCounter = 0;
    private MLCamera cameraDevice;
    private bool cameraDeviceAvailable = false;
    private string videoSaveDirectory;
    private const int VIDEO_CAPTURE_WIDTH = 1920;
    private const int VIDEO_CAPTURE_HEIGHT = 1080;
    private readonly string[] csvHeaders = {
        "Timestamp", "StudySelection", "TaskLoad", "PathType", "Event", "Value",
        "Eye", "Openness", "EyeInSkullX", "EyeInSkullY", "PupilDiameter",
        "GazeBehaviorType", "GazeAmplitude", "GazeDirection", "GazeVelocity",
        "GazeOnsetTime", "GazeDuration", "EyeWidthMax", "EyeHeightMax",
        "VergenceX", "VergenceY", "VergenceZ", "FixationConfidence",
        "LeftBlink", "RightBlink", "LeftCenterConfidence", "RightCenterConfidence",
        "AccelX", "AccelY", "AccelZ", "GyroX", "GyroY", "GyroZ",
        "LinearAccelX", "LinearAccelY", "LinearAccelZ", "Pitch", "Yaw", "Roll",
        "CamPosX", "CamPosY", "CamPosZ", "CamPitch", "CamYaw", "CamRoll",
        "CamForwardX", "CamForwardY", "CamForwardZ", "LuxValue",
        "AudioLevel", "AudioPeak", "AudioRMS", "AudioRecording",
        "GazePosX", "GazePosY", "GazePosZ", "GazeRotX", "GazeRotY", "GazeRotZ", "GazeRotW"
    };
    public bool IsCollecting => isCollecting;
    public int TotalDataPointsCollected => totalDataPointsCollected;
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
        InitializeSensorSuite();
    }
    private void InitializeSensorSuite()
    {
        Debug.Log("Initializing SensorSuite...");
        sensorAccess = SensorAccess.Instance;
        csvWriter = CSVWriter.Instance;
        mainCamera = Camera.main;
        if (sensorAccess == null)
        {
            Debug.LogError("SensorAccess instance not found! Make sure SensorAccess is in the scene.");
            return;
        }
        if (csvWriter == null)
        {
            Debug.LogError("CSVWriter instance not found! Make sure CSVWriter is in the scene.");
            return;
        }
        if (mainCamera == null)
        {
            Debug.LogWarning("Main Camera not found! Camera data will not be collected.");
        }
        sensorAccess.OnAllPermissionsReady += OnSensorAccessReady;
        Debug.Log("SensorSuite initialized successfully");
    }
    private void OnSensorAccessReady()
    {
        Debug.Log("Sensor access ready - initializing facial expression data structures");
        if (sensorAccess.FacialExpressionPermissionGranted && sensorAccess.FacialExpressionAvailable)
        {
            InitializeFacialExpressionData();
        }
        if (sensorAccess.AudioRecordPermissionGranted && sensorAccess.AudioInputAvailable)
        {
            InitializeAudioRecording();
            if (recordAudioFiles)
            {
                InitializeAudioFileRecording();
            }
        }
        if (sensorAccess.CameraPermissionGranted && sensorAccess.CameraAvailable)
        {
            StartCoroutine(InitializeVideoCamera());
        }
        Debug.Log("SensorSuite ready for data collection");
    }
    private void InitializeFacialExpressionData()
    {
        try
        {
            var allBlendShapes = Enum.GetValues(typeof(FacialBlendShape)) as FacialBlendShape[];
            blendShapeProperties = new BlendShapeProperties[allBlendShapes.Length];
            for (int i = 0; i < blendShapeProperties.Length; i++)
            {
                blendShapeProperties[i].FacialBlendShape = allBlendShapes[i];
                blendShapeProperties[i].Weight = 0;
                blendShapeProperties[i].Flags = BlendShapePropertiesFlags.None;
            }
            Debug.Log($"Initialized {blendShapeProperties.Length} facial blend shapes");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing facial expression data: {ex.Message}");
        }
    }
    private void InitializeAudioRecording()
    {
        try
        {
            var captureType = MLAudioInput.MicCaptureType.VoiceCapture;
            audioBufferClip = new MLAudioInput.BufferClip(
                captureType,
                AUDIO_CLIP_LENGTH_SECONDS,
                MLAudioInput.GetSampleRate(captureType)
            );
            audioPosition = 0;
            audioRecordingActive = true;
            Debug.Log($"Audio recording initialized - Sample Rate: {MLAudioInput.GetSampleRate(captureType)}, " +
                     $"Channels: {MLAudioInput.GetChannels(captureType)}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing audio recording: {ex.Message}");
            audioRecordingActive = false;
        }
    }
    private void InitializeAudioFileRecording()
    {
        try
        {
            audioSaveDirectory = Path.Combine(Application.persistentDataPath, "AudioRecordings");
            if (!Directory.Exists(audioSaveDirectory))
            {
                Directory.CreateDirectory(audioSaveDirectory);
            }
            audioFileCounter = 0;
            audioFileRecordingActive = true;
            Debug.Log($"Audio file recording initialized - Directory: {audioSaveDirectory}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing audio file recording: {ex.Message}");
            audioFileRecordingActive = false;
        }
    }
    private IEnumerator InitializeVideoCamera()
    {
        bool initializationSuccessful = false;
        Exception initException = null;
        try
        {
            videoSaveDirectory = Path.Combine(Application.persistentDataPath, "VideoFrames");
            if (!Directory.Exists(videoSaveDirectory))
            {
                Directory.CreateDirectory(videoSaveDirectory);
            }
            initializationSuccessful = true;
        }
        catch (Exception ex)
        {
            initException = ex;
        }
        if (!initializationSuccessful)
        {
            Debug.LogError($"Error initializing video camera: {initException?.Message}");
            cameraConnected = false;
            yield break;
        }
        while (!cameraDeviceAvailable)
        {
            MLResult availRes = MLCamera.GetDeviceAvailabilityStatus(MLCamera.Identifier.Main, out cameraDeviceAvailable);
            if (!availRes.IsOk)
            {
                Debug.LogError($"Error checking camera availability: {availRes}");
                cameraConnected = false;
                yield break;
            }
            yield return new WaitForSeconds(1.0f);
        }
        bool connectionSuccessful = false;
        MLCamera.StreamCapability selectedCap = default;
        try
        {
            MLCamera.ConnectContext connectContext = MLCamera.ConnectContext.Create();
            connectContext.CamId = MLCamera.Identifier.Main;
            connectContext.Flags = MLCamera.ConnectFlag.CamOnly;
            connectContext.EnableVideoStabilization = false;
            cameraDevice = MLCamera.CreateAndConnect(connectContext);
            if (cameraDevice == null)
            {
                Debug.LogError("Could not connect to MLCamera.");
                yield break;
            }
            cameraConnected = true;
            cameraDevice.OnRawImageAvailable += OnVideoCaptureComplete;
            MLCamera.StreamCapability[] streamCaps = MLCamera.GetImageStreamCapabilitiesForCamera(cameraDevice, MLCamera.CaptureType.Image);
            if (streamCaps == null || streamCaps.Length == 0)
            {
                Debug.LogError("No image capture stream capabilities available.");
                yield break;
            }
            if (!MLCamera.TryGetBestFitStreamCapabilityFromCollection(streamCaps, VIDEO_CAPTURE_WIDTH, VIDEO_CAPTURE_HEIGHT, MLCamera.CaptureType.Image, out selectedCap))
            {
                selectedCap = streamCaps[0];
            }
            MLCamera.CaptureConfig captureConfig = new MLCamera.CaptureConfig
            {
                CaptureFrameRate = MLCamera.CaptureFrameRate._30FPS,
                StreamConfigs = new MLCamera.CaptureStreamConfig[]
                {
                    MLCamera.CaptureStreamConfig.Create(selectedCap, MLCamera.OutputFormat.JPEG)
                }
            };
            MLResult prepRes = cameraDevice.PrepareCapture(captureConfig, out MLCamera.Metadata _);
            if (!prepRes.IsOk)
            {
                Debug.LogError($"PrepareCapture failed: {prepRes}");
                yield break;
            }
            connectionSuccessful = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error connecting to camera: {ex.Message}");
            cameraConnected = false;
            yield break;
        }
        if (connectionSuccessful)
        {
            Debug.Log($"Video camera initialized successfully - Resolution: {selectedCap.Width}x{selectedCap.Height}");
        }
    }
    public bool StartDataCollection(string subjectID = null)
    {
        if (isCollecting)
        {
            Debug.LogWarning("Data collection already in progress");
            return false;
        }
        if (sensorAccess == null || csvWriter == null)
        {
            Debug.LogError("Required components not available for data collection");
            return false;
        }
        if (!csvWriter.StartRecording(subjectID))
        {
            Debug.LogError("Failed to start CSV recording");
            return false;
        }
        var fullHeaders = csvHeaders.Concat(GetFacialExpressionHeaders()).ToArray();
        csvWriter.WriteHeader(fullHeaders);
        LogEvent("SUBJECT_ID", csvWriter.CurrentSubjectID);
        StartDataCollectionCoroutines();
        isCollecting = true;
        totalDataPointsCollected = 0;
        Debug.Log($"Started data collection with subject ID: {csvWriter.CurrentSubjectID}");
        return true;
    }
    public void StopDataCollection()
    {
        if (!isCollecting)
        {
            Debug.LogWarning("Data collection not in progress");
            return;
        }
        StopDataCollectionCoroutines();
        csvWriter.StopRecording();
        isCollecting = false;
        Debug.Log($"Stopped data collection. Total data points collected: {totalDataPointsCollected}");
    }
    private void StartDataCollectionCoroutines()
    {
        if (collectEyeTrackingData && sensorAccess.EyeTrackingPermissionGranted && sensorAccess.EyeTrackerAvailable)
        {
            eyeTrackingCoroutine = StartCoroutine(CollectEyeTrackingData());
        }
        if (collectIMUData)
        {
            imuDataCoroutine = StartCoroutine(CollectIMUData());
        }
        if (collectLightData && sensorAccess.LightSensorAvailable)
        {
            lightSensorCoroutine = StartCoroutine(CollectLightSensorData());
        }
        if (collectAudioData && sensorAccess.AudioRecordPermissionGranted && audioRecordingActive)
        {
            audioDataCoroutine = StartCoroutine(CollectAudioData());
        }
        if (collectVideoData && sensorAccess.CameraPermissionGranted && cameraConnected)
        {
            videoCaptureCoroutine = StartCoroutine(CaptureVideoFrames());
        }
        if (recordAudioFiles && sensorAccess.AudioRecordPermissionGranted && audioFileRecordingActive)
        {
            audioFileRecordingCoroutine = StartCoroutine(RecordAudioFiles());
        }
    }
    private void StopDataCollectionCoroutines()
    {
        if (eyeTrackingCoroutine != null)
        {
            StopCoroutine(eyeTrackingCoroutine);
            eyeTrackingCoroutine = null;
        }
        if (imuDataCoroutine != null)
        {
            StopCoroutine(imuDataCoroutine);
            imuDataCoroutine = null;
        }
        if (lightSensorCoroutine != null)
        {
            StopCoroutine(lightSensorCoroutine);
            lightSensorCoroutine = null;
        }
        if (audioDataCoroutine != null)
        {
            StopCoroutine(audioDataCoroutine);
            audioDataCoroutine = null;
        }
        if (videoCaptureCoroutine != null)
        {
            StopCoroutine(videoCaptureCoroutine);
            videoCaptureCoroutine = null;
        }
        if (audioFileRecordingCoroutine != null)
        {
            StopCoroutine(audioFileRecordingCoroutine);
            audioFileRecordingCoroutine = null;
        }
    }
    private IEnumerator CollectEyeTrackingData()
    {
        float interval = 1f / eyeTrackingFrequency;
        while (isCollecting)
        {
            try
            {
                CollectEyeTrackingDataFrame();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error collecting eye tracking data: {ex.Message}");
            }
            yield return new WaitForSeconds(interval);
        }
    }
    private void CollectEyeTrackingDataFrame()
    {
        if (sensorAccess.EyeTrackerFeature == null) return;
        try
        {
            EyeTrackerData data = sensorAccess.EyeTrackerFeature.GetEyeTrackerData();
            if (data.Equals(default(EyeTrackerData)) || data.PupilData == null || data.GeometricData == null)
                return;
            for (int i = 0; i < data.PupilData.Length && i < data.GeometricData.Length; i++)
            {
                var pupilData = data.PupilData[i];
                var geometricData = data.GeometricData[i];
                if (!pupilData.Valid || !geometricData.Valid) continue;
                var sensorData = CollectAllSensorData();
                var dataRow = CreateEyeTrackingDataRow(pupilData, geometricData, data.GazeBehaviorData, data.StaticData, sensorData);
                csvWriter.WriteDataRow(dataRow);
                totalDataPointsCollected++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in CollectEyeTrackingDataFrame: {ex.Message}");
        }
    }
    private IEnumerator CollectIMUData()
    {
        float interval = 1f / imuSamplingFrequency;
        while (isCollecting)
        {
            try
            {
                var sensorData = CollectAllSensorData();
                var dataRow = CreateIMUDataRow(sensorData);
                csvWriter.WriteDataRow(dataRow);
                totalDataPointsCollected++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error collecting IMU data: {ex.Message}");
            }
            yield return new WaitForSeconds(interval);
        }
    }
    private IEnumerator CollectLightSensorData()
    {
        float interval = 1f / lightSensorFrequency;
        while (isCollecting)
        {
            try
            {
                float luxValue = GetLuxValue();
                if (luxValue >= 0)
                {
                    LogEvent("LUX_READING", luxValue.ToString("F2"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error collecting light sensor data: {ex.Message}");
            }
            yield return new WaitForSeconds(interval);
        }
    }
    private IEnumerator CollectAudioData()
    {
        float interval = 1f / audioSamplingFrequency;
        while (isCollecting && audioRecordingActive)
        {
            try
            {
                CollectAudioDataFrame();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error collecting audio data: {ex.Message}");
            }
            yield return new WaitForSeconds(interval);
        }
    }
    private void CollectAudioDataFrame()
    {
        if (audioBufferClip == null) return;
        try
        {
            float maxAudioSample = 0f;
            float sumSquares = 0f;
            int totalSamples = 0;
            while (true)
            {
                int readSampleCount = audioBufferClip.GetData(audioSamples, audioPosition, out int nextPosition);
                if (readSampleCount == 0)
                {
                    break;
                }
                audioPosition = nextPosition;
                for (int i = 0; i < readSampleCount; i++)
                {
                    float sample = Mathf.Abs(audioSamples[i]);
                    maxAudioSample = Mathf.Max(maxAudioSample, sample);
                    sumSquares += sample * sample;
                    totalSamples++;
                }
            }
            float rms = totalSamples > 0 ? Mathf.Sqrt(sumSquares / totalSamples) : 0f;
            currentAudioLevel = rms;
            if (maxAudioSample > AUDIO_SENSITIVITY)
            {
                LogAudioEvent("AUDIO_ACTIVITY", maxAudioSample, rms);
            }
            else
            {
                LogAudioEvent("AUDIO_LEVEL", maxAudioSample, rms);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in CollectAudioDataFrame: {ex.Message}");
        }
    }
    private void LogAudioEvent(string eventType, float peak, float rms)
    {
        if (!isCollecting) return;
        var sensorData = CollectAllSensorData();
        var row = new string[csvHeaders.Length + GetFacialExpressionHeaders().Length];
        int index = 0;
        row[index++] = sensorData.timestamp;
        row[index++] = sensorData.studySelection;
        row[index++] = sensorData.taskLoad;
        row[index++] = sensorData.pathType;
        row[index++] = eventType;
        row[index++] = peak.ToString("F4");
        for (int i = 0; i < 21; i++) row[index++] = "N/A";
        AddSensorDataToRowWithAudio(row, ref index, sensorData, currentAudioLevel, peak, rms);
        csvWriter.WriteDataRow(row);
        totalDataPointsCollected++;
    }
    private IEnumerator CaptureVideoFrames()
    {
        while (isCollecting && cameraConnected)
        {
            try
            {
                if (!isCapturingVideo)
                {
                    CaptureVideoFrameAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in video capture loop: {ex.Message}");
            }
            yield return new WaitForSeconds(videoCaptureInterval);
        }
    }
    private async void CaptureVideoFrameAsync()
    {
        if (cameraDevice == null || !cameraConnected) return;
        isCapturingVideo = true;
        try
        {
            MLResult aewbRes = await cameraDevice.PreCaptureAEAWBAsync();
            if (!aewbRes.IsOk)
            {
                Debug.LogWarning($"PreCaptureAEAWBAsync failed: {aewbRes}");
            }
            else
            {
                MLResult captureRes = await cameraDevice.CaptureImageAsync(1);
                if (!captureRes.IsOk)
                {
                    Debug.LogError($"CaptureImageAsync failed: {captureRes}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in CaptureVideoFrameAsync: {ex.Message}");
        }
        finally
        {
            isCapturingVideo = false;
        }
    }
    private void OnVideoCaptureComplete(MLCamera.CameraOutput capturedImage, MLCamera.ResultExtras extras, MLCamera.Metadata metadataHandle)
    {
        try
        {
            bool aeConverged = metadataHandle.GetControlAEStateResultMetadata(out MLCameraBase.Metadata.ControlAEState aeState).IsOk &&
                               (aeState == MLCameraBase.Metadata.ControlAEState.Converged || aeState == MLCameraBase.Metadata.ControlAEState.Locked);
            bool awbConverged = metadataHandle.GetControlAWBStateResultMetadata(out MLCameraBase.Metadata.ControlAWBState awbState).IsOk &&
                                (awbState == MLCameraBase.Metadata.ControlAWBState.Converged || awbState == MLCameraBase.Metadata.ControlAWBState.Locked);
            if (!aeConverged || !awbConverged) return;
            if (capturedImage.Format != MLCamera.OutputFormat.JPEG) return;
            byte[] imageData = capturedImage.Planes[0].Data;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_ffff");
            string fileName = $"frame_{timestamp}.jpg";
            string filePath = Path.Combine(videoSaveDirectory, fileName);
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    File.WriteAllBytes(filePath, imageData);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        LogEvent("VIDEO_FRAME_CAPTURED", fileName);
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error saving video frame {fileName}: {e}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in OnVideoCaptureComplete: {ex.Message}");
        }
    }
    private IEnumerator RecordAudioFiles()
    {
        while (isCollecting && audioFileRecordingActive)
        {
            bool segmentCompleted = false;
            Exception recordingException = null;
            Coroutine segmentCoroutine = null;
            try
            {
                segmentCoroutine = StartCoroutine(RecordAudioSegment());
                segmentCompleted = true;
            }
            catch (Exception ex)
            {
                recordingException = ex;
            }
            if (segmentCompleted && segmentCoroutine != null)
            {
                yield return segmentCoroutine;
            }
            if (!segmentCompleted && recordingException != null)
            {
                Debug.LogError($"Error in audio file recording: {recordingException.Message}");
                yield return new WaitForSeconds(1.0f); 
            }
        }
    }
    private IEnumerator RecordAudioSegment()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HH_mm_ss");
        string fileName = $"audio_{timestamp}_{audioFileCounter:D4}.wav";
        currentAudioFileName = fileName;
        audioFileCounter++;
        bool recordingSuccessful = false;
        Exception recordingException = null;
        try
        {
            var captureType = MLAudioInput.MicCaptureType.VoiceCapture;
            audioStreamingClip = new MLAudioInput.StreamingClip(
                captureType,
                (int)audioRecordingSegmentLength,
                MLAudioInput.GetSampleRate(captureType)
            );
            LogEvent("AUDIO_RECORDING_START", fileName);
            audioRecordingStartTime = Time.time;
            recordingSuccessful = true;
        }
        catch (Exception ex)
        {
            recordingException = ex;
        }
        if (recordingSuccessful)
        {
            yield return new WaitForSeconds(audioRecordingSegmentLength);
            yield return StartCoroutine(SaveAudioClip(fileName));
            LogEvent("AUDIO_RECORDING_END", fileName);
        }
        else if (recordingException != null)
        {
            Debug.LogError($"Error recording audio segment {fileName}: {recordingException.Message}");
        }
        if (audioStreamingClip != null)
        {
            audioStreamingClip.Dispose();
            audioStreamingClip = null;
        }
        currentAudioFileName = "";
    }
    private IEnumerator SaveAudioClip(string fileName)
    {
        if (audioStreamingClip?.UnityAudioClip == null)
        {
            Debug.LogError($"No audio clip to save for {fileName}");
            yield break;
        }
        bool saveInitiated = false;
        Exception saveException = null;
        try
        {
            string filePath = Path.Combine(audioSaveDirectory, fileName);
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    SaveAudioClipAsWAV(audioStreamingClip.UnityAudioClip, filePath);
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        LogEvent("AUDIO_FILE_SAVED", fileName);
                        Debug.Log($"Audio file saved: {fileName}");
                    });
                }
                catch (Exception e)
                {
                    MainThreadDispatcher.Enqueue(() =>
                    {
                        Debug.LogError($"Error saving audio file {fileName}: {e.Message}");
                    });
                }
            });
            saveInitiated = true;
        }
        catch (Exception ex)
        {
            saveException = ex;
        }
        if (!saveInitiated && saveException != null)
        {
            Debug.LogError($"Error initiating audio save for {fileName}: {saveException.Message}");
        }
        yield return null;
    }
    private void SaveAudioClipAsWAV(AudioClip clip, string filePath)
    {
        if (clip == null) return;
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        short[] intData = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767);
        }
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fileStream))
        {
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + intData.Length * 2);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1); 
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(intData.Length * 2);
            foreach (short sample in intData)
            {
                writer.Write(sample);
            }
        }
    }
    private SensorDataFrame CollectAllSensorData()
    {
        var frame = new SensorDataFrame
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            studySelection = "DefaultStudy", 
            taskLoad = "Medium", 
            pathType = "Default" 
        };
        if (sensorAccess.AccelerometerAvailable && Accelerometer.current != null)
        {
            frame.acceleration = Accelerometer.current.acceleration.ReadValue();
        }
        if (sensorAccess.GyroscopeAvailable && UnityEngine.InputSystem.Gyroscope.current != null)
        {
            frame.angularVelocity = UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue();
        }
        if (sensorAccess.LinearAccelerationAvailable && LinearAccelerationSensor.current != null)
        {
            frame.linearAcceleration = LinearAccelerationSensor.current.acceleration.ReadValue();
        }
        if (sensorAccess.AttitudeSensorAvailable && AttitudeSensor.current != null)
        {
            frame.attitude = AttitudeSensor.current.attitude.ReadValue();
        }
        if (collectCameraData && mainCamera != null)
        {
            frame.cameraPosition = mainCamera.transform.position;
            frame.cameraRotation = mainCamera.transform.rotation.eulerAngles;
            frame.cameraForward = mainCamera.transform.forward;
        }
        frame.luxValue = GetLuxValue();
        if (collectFacialData && sensorAccess.FacialExpressionPermissionGranted &&
            sensorAccess.FacialExpressionAvailable && blendShapeProperties != null)
        {
            try
            {
                sensorAccess.FacialExpressionFeature.GetBlendShapesInfo(ref blendShapeProperties);
                frame.facialExpressions = blendShapeProperties.Select(p =>
                    p.Flags.HasFlag(BlendShapePropertiesFlags.ValidBit) &&
                    p.Flags.HasFlag(BlendShapePropertiesFlags.TrackedBit)
                        ? p.Weight.ToString("F4")
                        : "0").ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error collecting facial expression data: {ex.Message}");
                frame.facialExpressions = new string[Enum.GetNames(typeof(FacialBlendShape)).Length];
                for (int i = 0; i < frame.facialExpressions.Length; i++)
                    frame.facialExpressions[i] = "0";
            }
        }
        else
        {
            frame.facialExpressions = new string[Enum.GetNames(typeof(FacialBlendShape)).Length];
            for (int i = 0; i < frame.facialExpressions.Length; i++)
                frame.facialExpressions[i] = "0";
        }
        return frame;
    }
    private string[] CreateEyeTrackingDataRow(PupilData pupilData, GeometricData geometricData,
        GazeBehavior gazeBehavior, StaticData staticData, SensorDataFrame sensorData)
    {
        var row = new string[csvHeaders.Length + GetFacialExpressionHeaders().Length];
        int index = 0;
        row[index++] = sensorData.timestamp;
        row[index++] = sensorData.studySelection;
        row[index++] = sensorData.taskLoad;
        row[index++] = sensorData.pathType;
        row[index++] = "PUPIL_DATA";
        row[index++] = ""; 
        row[index++] = pupilData.Eye.ToString();
        row[index++] = geometricData.EyeOpenness.ToString("F4");
        row[index++] = geometricData.EyeInSkullPosition.x.ToString("F4");
        row[index++] = geometricData.EyeInSkullPosition.y.ToString("F4");
        row[index++] = pupilData.PupilDiameter.ToString("F4");
        row[index++] = gazeBehavior.MetaData.Valid ? gazeBehavior.GazeBehaviorType.ToString() : "Invalid";
        row[index++] = gazeBehavior.MetaData.Valid ? gazeBehavior.MetaData.Amplitude.ToString("F4") : "N/A";
        row[index++] = gazeBehavior.MetaData.Valid ? gazeBehavior.MetaData.Direction.ToString("F4") : "N/A";
        row[index++] = gazeBehavior.MetaData.Valid ? gazeBehavior.MetaData.Velocity.ToString("F4") : "N/A";
        row[index++] = gazeBehavior.MetaData.Valid ? gazeBehavior.OnsetTime.ToString() : "N/A";
        row[index++] = gazeBehavior.MetaData.Valid ? gazeBehavior.Duration.ToString("F4") : "N/A";
        bool staticDataValid = !staticData.Equals(default(StaticData));
        row[index++] = staticDataValid ? staticData.EyeWidthMax.ToString("F4") : "N/A";
        row[index++] = staticDataValid ? staticData.EyeHeightMax.ToString("F4") : "N/A";
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        AddSensorDataToRow(row, ref index, sensorData);
        return row;
    }
    private string[] CreateIMUDataRow(SensorDataFrame sensorData)
    {
        var row = new string[csvHeaders.Length + GetFacialExpressionHeaders().Length];
        int index = 0;
        row[index++] = sensorData.timestamp;
        row[index++] = sensorData.studySelection;
        row[index++] = sensorData.taskLoad;
        row[index++] = sensorData.pathType;
        row[index++] = "IMU_DATA";
        row[index++] = ""; 
        for (int i = 0; i < 21; i++) 
        {
            row[index++] = "N/A";
        }
        AddSensorDataToRow(row, ref index, sensorData);
        return row;
    }
    private void AddSensorDataToRow(string[] row, ref int index, SensorDataFrame sensorData)
    {
        row[index++] = sensorData.acceleration.x.ToString("F4");
        row[index++] = sensorData.acceleration.y.ToString("F4");
        row[index++] = sensorData.acceleration.z.ToString("F4");
        row[index++] = sensorData.angularVelocity.x.ToString("F4");
        row[index++] = sensorData.angularVelocity.y.ToString("F4");
        row[index++] = sensorData.angularVelocity.z.ToString("F4");
        row[index++] = sensorData.linearAcceleration.x.ToString("F4");
        row[index++] = sensorData.linearAcceleration.y.ToString("F4");
        row[index++] = sensorData.linearAcceleration.z.ToString("F4");
        Vector3 eulerAngles = sensorData.attitude.eulerAngles;
        row[index++] = eulerAngles.x.ToString("F4");
        row[index++] = eulerAngles.y.ToString("F4");
        row[index++] = eulerAngles.z.ToString("F4");
        row[index++] = sensorData.cameraPosition.x.ToString("F4");
        row[index++] = sensorData.cameraPosition.y.ToString("F4");
        row[index++] = sensorData.cameraPosition.z.ToString("F4");
        row[index++] = sensorData.cameraRotation.x.ToString("F4");
        row[index++] = sensorData.cameraRotation.y.ToString("F4");
        row[index++] = sensorData.cameraRotation.z.ToString("F4");
        row[index++] = sensorData.cameraForward.x.ToString("F4");
        row[index++] = sensorData.cameraForward.y.ToString("F4");
        row[index++] = sensorData.cameraForward.z.ToString("F4");
        row[index++] = sensorData.luxValue >= 0 ? sensorData.luxValue.ToString("F2") : "N/A";
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = !string.IsNullOrEmpty(currentAudioFileName) ? currentAudioFileName : "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        for (int i = 0; i < sensorData.facialExpressions.Length; i++)
        {
            row[index++] = sensorData.facialExpressions[i];
        }
    }
    private void AddSensorDataToRowWithAudio(string[] row, ref int index, SensorDataFrame sensorData,
        float audioLevel, float audioPeak, float audioRMS)
    {
        row[index++] = sensorData.acceleration.x.ToString("F4");
        row[index++] = sensorData.acceleration.y.ToString("F4");
        row[index++] = sensorData.acceleration.z.ToString("F4");
        row[index++] = sensorData.angularVelocity.x.ToString("F4");
        row[index++] = sensorData.angularVelocity.y.ToString("F4");
        row[index++] = sensorData.angularVelocity.z.ToString("F4");
        row[index++] = sensorData.linearAcceleration.x.ToString("F4");
        row[index++] = sensorData.linearAcceleration.y.ToString("F4");
        row[index++] = sensorData.linearAcceleration.z.ToString("F4");
        Vector3 eulerAngles = sensorData.attitude.eulerAngles;
        row[index++] = eulerAngles.x.ToString("F4");
        row[index++] = eulerAngles.y.ToString("F4");
        row[index++] = eulerAngles.z.ToString("F4");
        row[index++] = sensorData.cameraPosition.x.ToString("F4");
        row[index++] = sensorData.cameraPosition.y.ToString("F4");
        row[index++] = sensorData.cameraPosition.z.ToString("F4");
        row[index++] = sensorData.cameraRotation.x.ToString("F4");
        row[index++] = sensorData.cameraRotation.y.ToString("F4");
        row[index++] = sensorData.cameraRotation.z.ToString("F4");
        row[index++] = sensorData.cameraForward.x.ToString("F4");
        row[index++] = sensorData.cameraForward.y.ToString("F4");
        row[index++] = sensorData.cameraForward.z.ToString("F4");
        row[index++] = sensorData.luxValue >= 0 ? sensorData.luxValue.ToString("F2") : "N/A";
        row[index++] = audioLevel.ToString("F4");
        row[index++] = audioPeak.ToString("F4");
        row[index++] = audioRMS.ToString("F4");
        row[index++] = !string.IsNullOrEmpty(currentAudioFileName) ? currentAudioFileName : "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        row[index++] = "N/A"; 
        for (int i = 0; i < sensorData.facialExpressions.Length; i++)
        {
            row[index++] = sensorData.facialExpressions[i];
        }
    }
    private float GetLuxValue()
    {
        if (sensorAccess.LightSensorAvailable && LightSensor.current != null && LightSensor.current.enabled)
        {
            return LightSensor.current.lightLevel.ReadValue();
        }
        return -1f;
    }
    private string[] GetFacialExpressionHeaders()
    {
        return Enum.GetNames(typeof(FacialBlendShape));
    }
    public void LogEvent(string eventName, string value)
    {
        if (!isCollecting) return;
        var sensorData = CollectAllSensorData();
        var row = new string[csvHeaders.Length + GetFacialExpressionHeaders().Length];
        int index = 0;
        row[index++] = sensorData.timestamp;
        row[index++] = sensorData.studySelection;
        row[index++] = sensorData.taskLoad;
        row[index++] = sensorData.pathType;
        row[index++] = eventName;
        row[index++] = value;
        if (eventName == "LUX_READING")
        {
            for (int i = 0; i < 21; i++) row[index++] = "N/A";
            AddSensorDataToRow(row, ref index, sensorData);
            row[csvHeaders.Length - 12] = value; 
        }
        else
        {
            for (int i = 0; i < 21; i++) row[index++] = "N/A";
            AddSensorDataToRow(row, ref index, sensorData);
        }
        csvWriter.WriteDataRow(row);
        totalDataPointsCollected++;
    }
    void OnDestroy()
    {
        StopDataCollection();
        if (audioBufferClip != null)
        {
            audioBufferClip.Dispose();
            audioBufferClip = null;
        }
        if (audioStreamingClip != null)
        {
            audioStreamingClip.Dispose();
            audioStreamingClip = null;
        }
        if (cameraDevice != null)
        {
            cameraDevice.OnRawImageAvailable -= OnVideoCaptureComplete;
            if (cameraConnected)
            {
                cameraDevice.Disconnect();
            }
            cameraDevice = null;
        }
        if (sensorAccess != null)
        {
            sensorAccess.OnAllPermissionsReady -= OnSensorAccessReady;
        }
    }
    private struct SensorDataFrame
    {
        public string timestamp;
        public string studySelection;
        public string taskLoad;
        public string pathType;
        public Vector3 acceleration;
        public Vector3 angularVelocity;
        public Vector3 linearAcceleration;
        public Quaternion attitude;
        public Vector3 cameraPosition;
        public Vector3 cameraRotation;
        public Vector3 cameraForward;
        public float luxValue;
        public string[] facialExpressions;
    }
}
