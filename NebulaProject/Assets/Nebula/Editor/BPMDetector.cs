using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Nebula.Editor
{
    public class BPMDetector : EditorWindow
    {
        private AudioClip audioClip;
        private float detectedBPM = 0f;
        private string statusMessage = "Drag and drop an audio file here to detect BPM.";

        // Add menu named "BPM Detector" to the Window menu
        [MenuItem("Tools/BPM Detector")]
        public static void ShowWindow()
        {
            GetWindow<BPMDetector>("BPM Detector");
        }

        private void OnGUI()
        {
            GUILayout.Label("BPM Detector", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Drag-and-Drop Area
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Audio File Here", EditorStyles.helpBox);

            HandleDragAndDrop(dropArea);

            GUILayout.Space(10);

            // Display Audio Clip Information
            if (audioClip != null)
            {
                GUILayout.Label($"Audio Clip: {audioClip.name}", EditorStyles.label);
                GUILayout.Label($"Length: {audioClip.length} seconds", EditorStyles.label);
            }

            GUILayout.Space(10);

            // Detect BPM Button
            if (audioClip != null)
            {
                if (GUILayout.Button("Detect BPM"))
                {
                    EditorApplication.delayCall += DetectBPM;
                }
            }

            GUILayout.Space(10);

            // Display Detected BPM
            GUILayout.Label($"Status: {statusMessage}", EditorStyles.wordWrappedLabel);
            GUILayout.Space(5);
            if (detectedBPM > 0)
            {
                GUILayout.Label($"Detected BPM: {detectedBPM}", EditorStyles.boldLabel);

                // Optionally, Apply BPM to MusicProcessors
                if (GUILayout.Button("Apply BPM to Selected MusicProcessors"))
                {
                    ApplyBPMAtoSelectedProcessors();
                }
            }
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is AudioClip clip)
                            {
                                audioClip = clip;
                                detectedBPM = 0f;
                                statusMessage = "Audio clip loaded. Ready to detect BPM.";
                                Repaint();
                                break;
                            }
                            else
                            {
                                statusMessage = "Invalid file type. Please drag an audio file.";
                            }
                        }
                    }
                    Event.current.Use();
                    break;
            }
        }

        private void DetectBPM()
        {
            if (audioClip == null)
            {
                statusMessage = "No audio clip loaded.";
                Repaint();
                return;
            }

            statusMessage = "Detecting BPM...";
            Repaint();

            // Extract audio samples
            float[] samples = new float[audioClip.samples * audioClip.channels];
            audioClip.GetData(samples, 0);

            // Convert stereo to mono if necessary
            if (audioClip.channels > 1)
            {
                samples = ConvertToMono(samples, audioClip.channels);
            }

            // Apply band-pass filter to focus on percussive frequencies
            samples = ApplyBandPassFilter(samples, audioClip.frequency, 40, 200); // Changed from low-pass to band-pass

            // Compute the energy envelope
            float[] energy = ComputeEnergy(samples);

            // Normalize energy
            energy = NormalizeEnergy(energy);

            // Perform onset detection with dynamic threshold
            List<int> onsets = DetectOnsets(energy, 1.5f); // Added multiplier for dynamic threshold

            // Calculate intervals between onsets
            List<float> intervals = CalculateIntervals(onsets, audioClip.frequency);

            // Perform tempo estimation with octave handling
            detectedBPM = EstimateTempo(intervals);

            statusMessage = $"BPM Detected: {detectedBPM:F2}";
            Repaint();
        }

        private float[] ApplyBandPassFilter(float[] samples, int sampleRate, float lowCut, float highCut)
        {
            float[] lowFiltered = ApplyLowPassFilter(samples, sampleRate, highCut);
            float[] bandFiltered = samples.Zip(lowFiltered, (s, f) => s - f).ToArray();
            bandFiltered = ApplyLowPassFilter(bandFiltered, sampleRate, lowCut);
            return bandFiltered;
        }

        private float[] ApplyLowPassFilter(float[] samples, int sampleRate, float cutoffFrequency)
        {
            float rc = 1.0f / (cutoffFrequency * 2 * Mathf.PI);
            float dt = 1.0f / sampleRate;
            float alpha = dt / (rc + dt);

            float[] filteredSamples = new float[samples.Length];
            filteredSamples[0] = samples[0];

            for (int i = 1; i < samples.Length; i++)
            {
                filteredSamples[i] = filteredSamples[i - 1] + (alpha * (samples[i] - filteredSamples[i - 1]));
            }

            return filteredSamples;
        }

        private float[] NormalizeEnergy(float[] energy)
        {
            float max = energy.Max();
            return energy.Select(e => e / max).ToArray();
        }

        private List<int> DetectOnsets(float[] energy, float thresholdMultiplier)
        {
            List<int> onsets = new List<int>();
            float mean = energy.Average();
            float stdDev = Mathf.Sqrt(energy.Average(e => Mathf.Pow(e - mean, 2)));
            float dynamicThreshold = mean + (thresholdMultiplier * stdDev);

            for (int i = 1; i < energy.Length - 1; i++)
            {
                if (energy[i] > energy[i - 1] && energy[i] > energy[i + 1] && energy[i] > dynamicThreshold)
                {
                    onsets.Add(i);
                }
            }

            return onsets;
        }

        private List<float> CalculateIntervals(List<int> onsets, int sampleRate)
        {
            List<float> intervals = new List<float>();
            for (int i = 1; i < onsets.Count; i++)
            {
                float interval = (onsets[i] - onsets[i - 1]) * 1024f / sampleRate;
                intervals.Add(interval);
            }
            return intervals;
        }

        private float EstimateTempo(List<float> intervals)
        {
            if (intervals.Count == 0)
            {
                return 0f;
            }

            Dictionary<float, int> tempoCandidates = new Dictionary<float, int>();

            foreach (float interval in intervals)
            {
                if (interval <= 0)
                    continue;

                float bpm = 60f / interval;

                // Consider BPM candidates and their octave multiples/submultiples
                List<float> candidateBPMS = new List<float>
                {
                    bpm,
                    bpm * 2,
                    bpm / 2,
                    bpm * 3,
                    bpm / 3
                };

                foreach (float candidate in candidateBPMS)
                {
                    float roundedBPM = Mathf.Round(candidate);
                    if (roundedBPM >= 60 && roundedBPM <= 180)
                    {
                        if (tempoCandidates.ContainsKey(roundedBPM))
                            tempoCandidates[roundedBPM]++;
                        else
                            tempoCandidates[roundedBPM] = 1;
                    }
                }
            }

            if (tempoCandidates.Count == 0)
            {
                return 0f;
            }

            // Select the BPM with the highest votes
            float estimatedTempo = tempoCandidates.OrderByDescending(kv => kv.Value).First().Key;

            return estimatedTempo;
        }

        private float[] ConvertToMono(float[] samples, int channels)
        {
            float[] mono = new float[samples.Length / channels];
            for (int i = 0; i < mono.Length; i++)
            {
                float sum = 0f;
                for (int j = 0; j < channels; j++)
                {
                    sum += samples[i * channels + j];
                }
                mono[i] = sum / channels;
            }
            return mono;
        }

        private float[] ComputeEnergy(float[] samples)
        {
            float[] energy = new float[samples.Length / 1024];
            for (int i = 0; i < energy.Length; i++)
            {
                float sum = 0f;
                for (int j = 0; j < 1024; j++)
                {
                    int index = i * 1024 + j;
                    if (index < samples.Length)
                    {
                        sum += samples[index] * samples[index];
                    }
                }
                energy[i] = Mathf.Sqrt(sum / 1024f);
            }
            return energy;
        }

        private float[] ComputeAutocorrelation(float[] energy)
        {
            int n = energy.Length;
            float[] autocorrelation = new float[n];

            for (int lag = 0; lag < n; lag++)
            {
                float sum = 0f;
                for (int i = 0; i < n - lag; i++)
                {
                    sum += energy[i] * energy[i + lag];
                }
                autocorrelation[lag] = sum / (n - lag);
            }

            return autocorrelation;
        }

        private void ApplyBPMAtoSelectedProcessors()
        {
            if (detectedBPM <= 0)
            {
                statusMessage = "No BPM detected to apply.";
                Repaint();
                return;
            }

            GameObject[] selectedObjects = Selection.gameObjects;
            int appliedCount = 0;

            foreach (var obj in selectedObjects)
            {
                MusicProcessor processor = obj.GetComponent<MusicProcessor>();
                if (processor != null)
                {
                    Undo.RecordObject(processor, "Apply BPM");
                    processor.GetType().GetField("manualBPM", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.SetValue(processor, detectedBPM);
                    processor.GetType().GetField("useManualBPM", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.SetValue(processor, true);
                    EditorUtility.SetDirty(processor);
                    appliedCount++;
                }
            }

            statusMessage = $"Applied BPM to {appliedCount} MusicProcessor(s).";
            Repaint();
        }
    }
}