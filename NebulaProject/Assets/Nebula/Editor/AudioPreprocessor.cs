#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic; // Add this line

public class AudioPreprocessor : MonoBehaviour
{
    public AudioClip audioClip;
    public int sampleSize = 1024;
    public int numFrequencyBands = 8;

    public void PreprocessAudio()
    {
        if (audioClip == null)
        {
            Debug.LogError("Audio clip is not assigned.");
            return;
        }

        // Create a temporary AudioSource for spectrum analysis
        GameObject tempGO = new GameObject("TempAudioSource");
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.Play();
        audioSource.Pause(); // Pause immediately to avoid actual audio playback

        // Create a new PreprocessedData asset
        PreprocessedData data = ScriptableObject.CreateInstance<PreprocessedData>();
        data.amplitudeCurve = new AnimationCurve();
        data.amplitudeBufferCurve = new AnimationCurve();
        data.frequencyBandData = new List<PreprocessedData.FrequencyBandData>();

        for (int i = 0; i < numFrequencyBands; i++)
        {
            data.frequencyBandData.Add(new PreprocessedData.FrequencyBandData { bandCurve = new AnimationCurve() });
        }

        float[] samples = new float[sampleSize];
        float[] spectrum = new float[sampleSize];
        float clipLength = audioClip.length;
        int totalSamples = audioClip.samples;
        int step = sampleSize;

        float amplitudeHighest = 0f;

        for (int i = 0; i < totalSamples; i += step)
        {
            audioSource.time = (float)i / audioClip.frequency;
            audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

            // Process spectrum data into frequency bands
            float[] frequencyBands = new float[numFrequencyBands];
            int count = 0;
            for (int j = 0; j < numFrequencyBands; j++)
            {
                float average = 0;
                int sampleCount = (int)Mathf.Pow(2, j) * 2;

                if (count + sampleCount > sampleSize)
                {
                    sampleCount = sampleSize - count;
                }

                for (int k = 0; k < sampleCount; k++)
                {
                    average += spectrum[count] * (count + 1);
                    count++;
                }

                average /= count;
                frequencyBands[j] = average * 10;
            }

            // Calculate amplitude
            float amplitude = 0f;
            for (int j = 0; j < numFrequencyBands; j++)
            {
                amplitude += frequencyBands[j];
            }

            if (amplitude > amplitudeHighest)
            {
                amplitudeHighest = amplitude;
            }

            // Store amplitude and frequency band data in curves
            float time = (float)i / audioClip.frequency;

            data.amplitudeCurve.AddKey(time, amplitude / amplitudeHighest);
            for (int j = 0; j < numFrequencyBands; j++)
            {
                data.frequencyBandData[j].bandCurve.AddKey(time, frequencyBands[j] / amplitudeHighest);
            }
        }

        // Clean up temporary AudioSource
        DestroyImmediate(tempGO);

        // Save the asset
        string path = "Assets/Resources/PreprocessedAudioData.asset";
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        Debug.Log("Preprocessed audio data saved to: " + path);
    }
}
#endif
