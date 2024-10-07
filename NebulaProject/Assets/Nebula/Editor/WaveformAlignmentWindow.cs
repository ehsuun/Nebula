using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Nebula; // Add this to access the MusicProcessor class

public class WaveformAlignmentWindow : EditorWindow
{
    [MenuItem("Tools/Nebula/Waveform Alignment")]
    public static void ShowWindow()
    {
        GetWindow<WaveformAlignmentWindow>("Waveform Alignment");
    }

    private AudioClip mainClip;
    private AudioClip drumClip;
    private float[] mainSamples;
    private float[] drumSamples;
    private float offset = 0f;
    private Vector2 scrollPosition;
    private float zoom = 1f;
    private bool isDragging = false;
    private Vector2 lastMousePosition;
    private MusicProcessor musicProcessor;

    public event Action<float> OnAlignmentApplied;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Waveform Alignment Tool", EditorStyles.boldLabel);

        mainClip = (AudioClip)EditorGUILayout.ObjectField("Main Audio Clip", mainClip, typeof(AudioClip), false);
        drumClip = (AudioClip)EditorGUILayout.ObjectField("Drum Stem Clip", drumClip, typeof(AudioClip), false);

        if (GUILayout.Button("Load Waveforms"))
        {
            LoadWaveforms();
        }

        if (mainSamples != null && drumSamples != null)
        {
            zoom = EditorGUILayout.Slider("Zoom", zoom, 0.1f, 10f);
            
            EditorGUI.BeginChangeCheck();
            offset = EditorGUILayout.FloatField("Offset (seconds)", offset);
            if (EditorGUI.EndChangeCheck())
            {
                GUI.changed = true;
            }

            Rect waveformRect = GUILayoutUtility.GetRect(position.width, 200);
            DrawGrid(waveformRect);
            DrawWaveforms(waveformRect);

            HandleDragging(waveformRect);

            if (GUILayout.Button("Apply Alignment"))
            {
                ApplyAlignment();
                Close();
            }

            if (GUILayout.Button("Auto-Align"))
            {
                offset = AutoAlignDrumStem(mainClip, drumClip);
                GUI.changed = true;
            }

            EditorGUILayout.LabelField($"Current Offset: {offset:F3} seconds");
        }

        if (GUI.changed)
        {
            Repaint();
        }
    }

    private void LoadWaveforms()
    {
        if (mainClip == null || drumClip == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign both audio clips before loading waveforms.", "OK");
            return;
        }

        UpdateAudioClipImportSettings(mainClip);
        UpdateAudioClipImportSettings(drumClip);

        mainSamples = GetAudioData(mainClip);
        drumSamples = GetAudioData(drumClip);
    }

    private void DrawWaveforms(Rect rect)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        float mainStartX = rect.x;
        float drumStartX = rect.x - offset * zoom * mainClip.frequency;

        DrawWaveform(mainSamples, rect, mainStartX, Color.green);
        DrawWaveform(drumSamples, rect, drumStartX, Color.cyan);
    }

    private void DrawWaveform(float[] samples, Rect rect, float startX, Color color)
    {
        int visibleSamples = Mathf.Min(samples.Length, Mathf.CeilToInt(rect.width / zoom));
        Vector3[] points = new Vector3[visibleSamples];

        for (int i = 0; i < visibleSamples; i++)
        {
            float x = startX + i * zoom;
            float y = rect.y + rect.height / 2 + samples[i] * rect.height / 2;
            points[i] = new Vector3(x, y, 0);
        }

        Handles.color = color;
        Handles.DrawAAPolyLine(2f, points);
    }

    private void UpdateAudioClipImportSettings(AudioClip clip)
    {
        string assetPath = AssetDatabase.GetAssetPath(clip);
        AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer != null)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.DecompressOnLoad)
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                Debug.Log($"Updated import settings for {clip.name}");
            }
        }
    }

    private static float[] GetAudioData(AudioClip clip)
    {
        int sampleCount = clip.samples;
        float[] data = new float[sampleCount];

        if (clip.channels > 1)
        {
            // For multi-channel audio, we'll average the channels
            float[] multiChannelData = new float[sampleCount * clip.channels];
            clip.GetData(multiChannelData, 0);
            for (int i = 0; i < sampleCount; i++)
            {
                float sum = 0f;
                for (int channel = 0; channel < clip.channels; channel++)
                {
                    sum += multiChannelData[i * clip.channels + channel];
                }
                data[i] = sum / clip.channels;
            }
        }
        else
        {
            // For mono audio, we can directly get the data
            clip.GetData(data, 0);
        }
        return data;
    }

    private void DrawGrid(Rect rect)
    {
        Handles.color = new Color(1f, 1f, 1f, 0.2f);
        float secondsPerPixel = mainClip.length / (rect.width * zoom);
        float markerInterval = GetAppropriateMarkerInterval(secondsPerPixel);

        for (float t = 0; t < mainClip.length; t += markerInterval)
        {
            float x = rect.x + (t / mainClip.length) * rect.width * zoom;
            Handles.DrawLine(new Vector3(x, rect.y, 0), new Vector3(x, rect.y + rect.height, 0));

            // Draw time labels
            if (markerInterval >= 1f)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(x - 20, rect.y, 40, 20), FormatTime(t));
            }
        }
    }

    private float GetAppropriateMarkerInterval(float secondsPerPixel)
    {
        float[] intervals = { 0.001f, 0.01f, 0.1f, 0.5f, 1f, 5f, 10f, 30f, 60f };
        foreach (float interval in intervals)
        {
            if (interval / secondsPerPixel > 50f)
            {
                return interval;
            }
        }
        return 60f;
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes}:{remainingSeconds:D2}";
    }

    private void HandleDragging(Rect waveformRect)
    {
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown:
                if (waveformRect.Contains(e.mousePosition))
                {
                    isDragging = true;
                    lastMousePosition = e.mousePosition;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (isDragging)
                {
                    float delta = e.mousePosition.x - lastMousePosition.x;
                    offset -= delta / (zoom * mainClip.frequency);
                    lastMousePosition = e.mousePosition;
                    Repaint();
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                isDragging = false;
                break;
        }
    }

    public static float AutoAlignDrumStem(AudioClip mainClip, AudioClip drumClip)
    {
        if (mainClip == null || drumClip == null)
        {
            Debug.LogError("Main clip or drum clip is null.");
            return 0f;
        }

        float[] mainData = GetAudioData(mainClip);
        float[] drumData = GetAudioData(drumClip);

        int windowSize = mainClip.frequency / 10; // 100ms window
        int maxLag = Mathf.Min(mainClip.frequency / 2, mainData.Length, drumData.Length); // Max 0.5 second offset or clip length
        float bestOffset = 0f;
        float minLoss = float.MaxValue;

        // Find initial alignment
        for (int lag = 0; lag < maxLag; lag += windowSize / 2)
        {
            float loss = CalculateLoss(mainData, drumData, lag, windowSize);
            if (loss < minLoss)
            {
                minLoss = loss;
                bestOffset = lag;
            }
        }

        // Refine alignment
        int refinementWindow = windowSize / 4;
        for (int lag = Mathf.Max(0, (int)bestOffset - refinementWindow); 
             lag <= Mathf.Min(bestOffset + refinementWindow, maxLag); 
             lag++)
        {
            float loss = CalculateLoss(mainData, drumData, lag, windowSize);
            if (loss < minLoss)
            {
                minLoss = loss;
                bestOffset = lag;
            }
        }

        return bestOffset / mainClip.frequency;
    }

    private static float CalculateLoss(float[] mainData, float[] drumData, int lag, int windowSize)
    {
        float sum = 0f;
        int compareLength = Mathf.Min(windowSize, mainData.Length - lag, drumData.Length);
        for (int i = 0; i < compareLength; i++)
        {
            int mainIndex = i + lag;
            int drumIndex = i;
            float diff = mainData[mainIndex] - drumData[drumIndex];
            sum += diff * diff;
        }
        return sum / compareLength;
    }

    public void SetClips(AudioClip main, AudioClip drum)
    {
        mainClip = main;
        drumClip = drum;
        LoadWaveforms();
    }

    private void ApplyAlignment()
    {
        if (musicProcessor == null)
        {
            musicProcessor = FindObjectOfType<MusicProcessor>();
        }

        if (musicProcessor != null)
        {
            musicProcessor.stemOffset = offset;
            EditorUtility.SetDirty(musicProcessor);
            Debug.Log($"Applied alignment: Stem offset set to {offset} seconds");
        }
        else
        {
            Debug.LogError("MusicProcessor not found in the scene. Unable to apply alignment.");
        }

        OnAlignmentApplied?.Invoke(offset);
    }
}