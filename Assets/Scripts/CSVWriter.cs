using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading;
public class CSVWriter : MonoBehaviour
{
    public static CSVWriter Instance { get; private set; }
    [Header("CSV Configuration")]
    [SerializeField] private int bufferSize = 8192; 
    [SerializeField] private int flushInterval = 100; 
    [SerializeField] private float autoFlushTime = 5.0f; 
    private string fileName;
    private string filePath;
    private StreamWriter streamWriter;
    private StringBuilder buffer;
    private ConcurrentQueue<string> writeQueue;
    private bool isWriting = false;
    private int entryCount = 0;
    private float lastFlushTime;
    private readonly object lockObject = new object();
    private string currentSubjectID = null;
    private bool isRecording = false;
    public string CurrentSubjectID => currentSubjectID;
    public bool IsRecording => isRecording;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeWriter();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void InitializeWriter()
    {
        buffer = new StringBuilder(bufferSize);
        writeQueue = new ConcurrentQueue<string>();
        lastFlushTime = Time.time;
    }
    public bool StartRecording(string subjectID = null)
    {
        try
        {
            if (isRecording)
            {
                StopRecording();
            }
            currentSubjectID = !string.IsNullOrEmpty(subjectID) ? subjectID : GenerateSubjectID();
            if (!CreateFilePath())
            {
                Debug.LogError("Failed to create file path for CSV recording");
                return false;
            }
            if (!InitializeStreamWriter())
            {
                Debug.LogError("Failed to initialize stream writer for CSV recording");
                return false;
            }
            isRecording = true;
            entryCount = 0;
            lastFlushTime = Time.time;
            Debug.Log($"Started CSV recording with Subject ID: {currentSubjectID}");
            Debug.Log($"File path: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error starting CSV recording: {ex.Message}");
            return false;
        }
    }
    public void StopRecording()
    {
        if (!isRecording) return;
        try
        {
            FlushBuffer();
            if (streamWriter != null)
            {
                streamWriter.Flush();
                streamWriter.Close();
                streamWriter.Dispose();
                streamWriter = null;
            }
            isRecording = false;
            Debug.Log($"Stopped CSV recording. Total entries written: {entryCount}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error stopping CSV recording: {ex.Message}");
        }
    }
    public void WriteHeader(string[] headers)
    {
        if (!isRecording)
        {
            Debug.LogWarning("Cannot write header - recording not started");
            return;
        }
        string headerLine = string.Join(",", headers);
        WriteLineInternal(headerLine);
    }
    public void WriteDataRow(string[] data)
    {
        if (!isRecording)
        {
            Debug.LogWarning("Cannot write data - recording not started");
            return;
        }
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] != null && (data[i].Contains(",") || data[i].Contains("\"") || data[i].Contains("\n")))
            {
                data[i] = "\"" + data[i].Replace("\"", "\"\"") + "\"";
            }
        }
        string dataLine = string.Join(",", data);
        WriteLineInternal(dataLine);
    }
    public void WriteLine(string csvLine)
    {
        if (!isRecording)
        {
            Debug.LogWarning("Cannot write line - recording not started");
            return;
        }
        WriteLineInternal(csvLine);
    }
    private void WriteLineInternal(string line)
    {
        lock (lockObject)
        {
            try
            {
                buffer.AppendLine(line);
                entryCount++;
                if (entryCount % flushInterval == 0 ||
                    buffer.Length > bufferSize ||
                    Time.time - lastFlushTime > autoFlushTime)
                {
                    FlushBuffer();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error writing line to buffer: {ex.Message}");
            }
        }
    }
    private void FlushBuffer()
    {
        if (streamWriter == null || buffer.Length == 0) return;
        try
        {
            streamWriter.Write(buffer.ToString());
            streamWriter.Flush();
            buffer.Clear();
            lastFlushTime = Time.time;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error flushing buffer: {ex.Message}");
        }
    }
    private bool CreateFilePath()
    {
        try
        {
            fileName = $"SensorData_{currentSubjectID}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string directoryPath;
            if (Application.platform == RuntimePlatform.Android)
            {
                directoryPath = Path.Combine(Application.persistentDataPath, "SensorData");
            }
            else
            {
                directoryPath = Path.Combine(Application.dataPath, "SensorData");
            }
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            filePath = Path.Combine(directoryPath, fileName);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating file path: {ex.Message}");
            return false;
        }
    }
    private bool InitializeStreamWriter()
    {
        try
        {
            var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize);
            streamWriter = new StreamWriter(fileStream, Encoding.UTF8, bufferSize);
            streamWriter.AutoFlush = false;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing stream writer: {ex.Message}");
            return false;
        }
    }
    private string GenerateSubjectID()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        var result = new StringBuilder(6);
        for (int i = 0; i < 6; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        return result.ToString();
    }
    void Update()
    {
        if (isRecording && Time.time - lastFlushTime > autoFlushTime)
        {
            lock (lockObject)
            {
                FlushBuffer();
            }
        }
    }
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isRecording)
        {
            FlushBuffer();
        }
    }
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isRecording)
        {
            FlushBuffer();
        }
    }
    void OnDestroy()
    {
        StopRecording();
    }
    void OnApplicationQuit()
    {
        StopRecording();
    }
}
