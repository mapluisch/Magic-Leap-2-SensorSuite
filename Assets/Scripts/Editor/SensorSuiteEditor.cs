using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(SensorSuite))]
public class SensorSuiteEditor : Editor
{
    private SensorSuite sensorSuite;
    private bool showDataCollectionSettings = true;
    private bool showSensorToggles = true;
    private bool showStatusInfo = true;
    private bool showAdvancedSettings = false;
    private readonly Color headerColor = new Color(0.7f, 0.85f, 1f, 1f);           
    private readonly Color sectionColor = new Color(0.85f, 0.92f, 1f, 0.3f);       
    private readonly Color accentColor = new Color(0.4f, 0.7f, 1f, 1f);            
    private readonly Color successColor = new Color(0.6f, 0.9f, 0.8f, 1f);         
    private readonly Color warningColor = new Color(1f, 0.9f, 0.6f, 1f);           
    private readonly Color errorColor = new Color(1f, 0.7f, 0.7f, 1f);             
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle buttonStyle;
    private GUIStyle toggleStyle;
    private GUIStyle statusStyle;
    void OnEnable()
    {
        sensorSuite = (SensorSuite)target;
    }
    private void InitializeStyles()
    {
        if (headerStyle != null) return; 
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        sectionStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(0, 0, 5, 5)
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            fixedHeight = 30
        };
        toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            fontSize = 11,
            fontStyle = FontStyle.Normal
        };
        statusStyle = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 11,
            padding = new RectOffset(8, 8, 8, 8)
        };
    }
    public override void OnInspectorGUI()
    {
        InitializeStyles();
        serializedObject.Update();
        DrawHeader();
        EditorGUILayout.Space(10);
        DrawQuickActions();
        EditorGUILayout.Space(10);
        DrawDataCollectionSettings();
        EditorGUILayout.Space(5);
        DrawSensorToggles();
        EditorGUILayout.Space(5);
        DrawStatusInfo();
        EditorGUILayout.Space(5);
        DrawAdvancedSettings();
        serializedObject.ApplyModifiedProperties();
    }
    private void DrawHeader()
    {
        Rect headerRect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(headerRect, headerColor);
        GUILayout.BeginArea(headerRect);
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("🔬 SENSOR SUITE", headerStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Magic Leap 2 Multi-Sensor Data Collection System",
            EditorStyles.centeredGreyMiniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
    private void DrawQuickActions()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.LabelField("⚡ Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        Color originalColor = GUI.backgroundColor;
        if (sensorSuite.IsCollecting)
        {
            GUI.backgroundColor = errorColor;
            if (GUILayout.Button("🛑 Stop Data Collection", buttonStyle))
            {
                if (Application.isPlaying)
                {
                    sensorSuite.StopDataCollection();
                }
            }
        }
        else
        {
            GUI.backgroundColor = successColor;
            if (GUILayout.Button("▶️ Start Data Collection", buttonStyle))
            {
                if (Application.isPlaying)
                {
                    sensorSuite.StartDataCollection();
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Playing",
                        "Data collection can only be started during Play mode.", "OK");
                }
            }
        }
        GUI.backgroundColor = originalColor;
        GUI.backgroundColor = accentColor;
        if (GUILayout.Button("👤 Start with Subject ID", buttonStyle))
        {
            if (Application.isPlaying)
            {
                string subjectID = EditorUtility.SaveFilePanel("Enter Subject ID", "", "SUBJ001", "");
                if (!string.IsNullOrEmpty(subjectID))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(subjectID);
                    sensorSuite.StartDataCollection(fileName);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Not Playing",
                    "Data collection can only be started during Play mode.", "OK");
            }
        }
        GUI.backgroundColor = originalColor;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
    private void DrawDataCollectionSettings()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        showDataCollectionSettings = EditorGUILayout.Foldout(showDataCollectionSettings,
            "⚙️ Data Collection Settings", true, EditorStyles.foldoutHeader);
        if (showDataCollectionSettings)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("📊 Sampling Frequencies", EditorStyles.boldLabel);
            DrawFrequencySlider("Eye Tracking", "eyeTrackingFrequency", 1f, 120f, "Hz");
            DrawFrequencySlider("IMU Sensors", "imuSamplingFrequency", 1f, 60f, "Hz");
            DrawFrequencySlider("Audio Level", "audioSamplingFrequency", 1f, 30f, "Hz");
            DrawFrequencySlider("Light Sensor", "lightSensorFrequency", 0.1f, 10f, "Hz");
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("⏱️ Capture Intervals", EditorStyles.boldLabel);
            DrawIntervalSlider("Video Capture", "videoCaptureInterval", 0.1f, 5f, "seconds");
            DrawIntervalSlider("Audio Recording Segments", "audioRecordingSegmentLength", 5f, 60f, "seconds");
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawSensorToggles()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        showSensorToggles = EditorGUILayout.Foldout(showSensorToggles,
            "🎛️ Sensor Collection Toggles", true, EditorStyles.foldoutHeader);
        if (showSensorToggles)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("👁️ Primary Sensors", EditorStyles.boldLabel);
            DrawSensorToggle("Eye Tracking Data", "collectEyeTrackingData", "👁️");
            DrawSensorToggle("Facial Expression Data", "collectFacialData", "😊");
            DrawSensorToggle("Camera Position Data", "collectCameraData", "📷");
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🏃 Motion Sensors", EditorStyles.boldLabel);
            DrawSensorToggle("IMU Data (Accel/Gyro)", "collectIMUData", "📱");
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🌍 Environmental Sensors", EditorStyles.boldLabel);
            DrawSensorToggle("Light Sensor Data", "collectLightData", "💡");
            DrawSensorToggle("Audio Level Monitoring", "collectAudioData", "🔊");
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🎥 Recording Systems", EditorStyles.boldLabel);
            DrawSensorToggle("Video Frame Capture", "collectVideoData", "🎬");
            DrawSensorToggle("Raw Audio Recording", "recordAudioFiles", "🎤");
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawStatusInfo()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        showStatusInfo = EditorGUILayout.Foldout(showStatusInfo,
            "📈 System Status", true, EditorStyles.foldoutHeader);
        if (showStatusInfo)
        {
            EditorGUILayout.Space(5);
            if (Application.isPlaying)
            {
                DrawStatusItem("Collection Active", sensorSuite.IsCollecting ? "✅ Running" : "⏸️ Stopped",
                    sensorSuite.IsCollecting ? successColor : warningColor);
                DrawStatusItem("Data Points Collected", sensorSuite.TotalDataPointsCollected.ToString("N0"),
                    accentColor);
                string currentSubject = "None";
                if (CSVWriter.Instance != null && !string.IsNullOrEmpty(CSVWriter.Instance.CurrentSubjectID))
                {
                    currentSubject = CSVWriter.Instance.CurrentSubjectID;
                }
                DrawStatusItem("Current Subject ID", currentSubject, headerColor);
                EditorGUILayout.Space(5);
                if (SensorAccess.Instance != null)
                {
                    var sensorAccess = SensorAccess.Instance;
                    EditorGUILayout.LabelField("🔐 Permissions Status", EditorStyles.boldLabel);
                    DrawPermissionStatus("Eye Tracking", sensorAccess.EyeTrackingPermissionGranted);
                    DrawPermissionStatus("Facial Expression", sensorAccess.FacialExpressionPermissionGranted);
                    DrawPermissionStatus("Audio Recording", sensorAccess.AudioRecordPermissionGranted);
                    DrawPermissionStatus("Camera", sensorAccess.CameraPermissionGranted);
                }
            }
            else
            {
                DrawStatusItem("Mode", "🔧 Editor Mode - Enter Play Mode to Start Collection", warningColor);
            }
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawAdvancedSettings()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings,
            "🔧 Advanced Settings", true, EditorStyles.foldoutHeader);
        if (showAdvancedSettings)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "⚠️ Advanced settings - modify with caution. These settings affect system performance and data quality.",
                MessageType.Warning);
            EditorGUILayout.Space(5);
            SerializedProperty prop = serializedObject.GetIterator();
            prop.NextVisible(true);
            while (prop.NextVisible(false))
            {
                if (IsPropertyShownElsewhere(prop.name)) continue;
                EditorGUILayout.PropertyField(prop, true);
            }
        }
        EditorGUILayout.EndVertical();
    }
    private void DrawFrequencySlider(string label, string propertyName, float min, float max, string unit)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));
            prop.floatValue = EditorGUILayout.Slider(prop.floatValue, min, max);
            EditorGUILayout.LabelField($"{prop.floatValue:F1} {unit}", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
    }
    private void DrawIntervalSlider(string label, string propertyName, float min, float max, string unit)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(120));
            prop.floatValue = EditorGUILayout.Slider(prop.floatValue, min, max);
            EditorGUILayout.LabelField($"{prop.floatValue:F1} {unit}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }
    }
    private void DrawSensorToggle(string label, string propertyName, string icon)
    {
        SerializedProperty prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{icon} {label}", GUILayout.Width(200));
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = prop.boolValue ? successColor : Color.white;
            prop.boolValue = EditorGUILayout.Toggle(prop.boolValue, toggleStyle);
            GUI.backgroundColor = originalColor;
            EditorGUILayout.LabelField(prop.boolValue ? "✅" : "⭕", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();
        }
    }
    private void DrawStatusItem(string label, string value, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(150));
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        EditorGUILayout.LabelField(value, statusStyle);
        GUI.backgroundColor = originalColor;
        EditorGUILayout.EndHorizontal();
    }
    private void DrawPermissionStatus(string permissionName, bool granted)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"  {permissionName}", GUILayout.Width(150));
        Color statusColor = granted ? successColor : errorColor;
        string statusText = granted ? "✅ Granted" : "❌ Denied";
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = statusColor;
        EditorGUILayout.LabelField(statusText, statusStyle);
        GUI.backgroundColor = originalColor;
        EditorGUILayout.EndHorizontal();
    }
    private bool IsPropertyShownElsewhere(string propertyName)
    {
        string[] shownProperties = {
            "eyeTrackingFrequency", "imuSamplingFrequency", "lightSensorFrequency",
            "audioSamplingFrequency", "videoCaptureInterval", "audioRecordingSegmentLength",
            "collectEyeTrackingData", "collectIMUData", "collectFacialData",
            "collectLightData", "collectCameraData", "collectAudioData",
            "collectVideoData", "recordAudioFiles",
            "m_Script" 
        };
        foreach (string shown in shownProperties)
        {
            if (propertyName == shown) return true;
        }
        return false;
    }
}