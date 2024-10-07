using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nebula
{
    public class MusicProcessor : MonoBehaviour
    {
        public enum ProcessingMode { Live, Preprocessed, Stems }
        public ProcessingMode processingMode = ProcessingMode.Live;

        public enum StemType
        {
            None,
            Bass,
            Drums,
            Other,
            Vocals
        }

        // Add this enum
        public enum FrequencyBand
        {
            Bass,
            Mid,
            High,
            All  // New enum value for overall intensity
        }

        public AudioSource mainAudioSource;
        public AudioSource bassAudioSource;
        public AudioSource drumsAudioSource;
        public AudioSource otherAudioSource;
        public AudioSource vocalsAudioSource;

        // Change these from private to public
        public float[] bassSpectrumData;
        public float[] drumsSpectrumData;
        public float[] otherSpectrumData;
        public float[] vocalsSpectrumData;
        private int sampleSize = 1024;
        private float sampleRate;

        // Energy variables for adaptive threshold
        private float[] energyHistory;
        public int energyHistorySize = 20;
        private int historyIndex;

        private float bassIntensity, midIntensity, highIntensity;
        public float kickAdaptiveThreshold;
        public float beatAdaptiveThreshold;
        public float kickSensitivity = 1.1f;
        public float beatSensitivity = 1.2f;

        public event Action OnBeatDetected;
        public event Action OnKickDetected;

        private float lastBeatTime;
        private float lastKickTime;
        private float previousEnergy = 0f;

        private Queue<float> lookaheadQueue;
        public int lookaheadSize = 5;

        [SerializeField]
        private bool useManualBPM = false;

        [SerializeField]
        private float manualBPM = 120f;

        private float smoothBPM;
        public float bpmSmoothingFactor = 0.1f;

        private double musicStartDspTime;
        private bool isPlaying = false;

        private const int NumBands = 8;
        private float[] bandIntensities;

        [SerializeField] [Range(0f, 2f)] private float bassScale = 0.7f;
        [SerializeField] [Range(0f, 2f)] private float midScale = 1f;
        [SerializeField] [Range(0f, 2f)] private float highScale = 1.2f;

        [SerializeField] private bool playOnStart = true;

        private bool stemLengthsVerified = false;

        public float stemOffset = 0f; // Renamed from drumStemOffset

        // Replace the 2D array with three 1D arrays
        private float[] stemBassIntensities;
        private float[] stemMidIntensities;
        private float[] stemHighIntensities;
        private float[] stemAllIntensities;  // New array for overall stem intensities

        [SerializeField] private AudioMixerGroup muteMixerGroup;

        public float currentBPM = 120f;
        private float bpmConfidence = 0f;
        private float lastPeakTime = 0f;
        private List<float> peakIntervals = new List<float>();
        private float analysisInterval = 5f;
        private float lastAnalysisTime = 0f;

        private const float MIN_PEAK_INTERVAL = 0.3f; // 200 BPM
        private const float MAX_PEAK_INTERVAL = 1f; // 60 BPM
        private const float CONFIDENCE_THRESHOLD = 0.8f;
        private const int MIN_PEAKS_FOR_ANALYSIS = 8;

        private float peakThreshold = 0.5f; // Adjust this value based on your audio
        private float minPeakDistance = 0.2f; // Minimum time between peaks in seconds

        private float adaptiveThreshold = 0.5f;
        private const float THRESHOLD_DECAY = 0.95f;
        private const float THRESHOLD_FACTOR = 1.5f;

        private void Reset()
        {
            EnsureAudioSourcesExist();
        }

        private void Awake()
        {
            EnsureAudioSourcesExist();
        }

        private void Start()
        {
            if (mainAudioSource == null)
            {
                Debug.LogError("Main AudioSource is not assigned.");
                return;
            }

            Initialize(mainAudioSource);

            if (bassAudioSource != null && drumsAudioSource != null && 
                otherAudioSource != null && vocalsAudioSource != null &&
                bassAudioSource.clip != null && drumsAudioSource.clip != null && 
                otherAudioSource.clip != null && vocalsAudioSource.clip != null)
            {
                processingMode = ProcessingMode.Stems;
                Debug.Log("Stem mode activated. All stem audio sources are assigned.");
            }
            else
            {
                Debug.Log("Using main audio source only. Stem mode is not active.");
            }

            if (playOnStart)
            {
                StartCoroutine(PlayMusic());
            }

            // Initialize the arrays
            int stemCount = System.Enum.GetValues(typeof(StemType)).Length;
            stemBassIntensities = new float[stemCount];
            stemMidIntensities = new float[stemCount];
            stemHighIntensities = new float[stemCount];
            stemAllIntensities = new float[stemCount];

            if (processingMode == ProcessingMode.Stems)
            {
                SetupStemAudioSources();
            }
        }

        public void Initialize(AudioSource source)
        {
            mainAudioSource = source;
            bassSpectrumData = new float[sampleSize];
            drumsSpectrumData = new float[sampleSize];
            otherSpectrumData = new float[sampleSize];
            vocalsSpectrumData = new float[sampleSize];
            energyHistory = new float[energyHistorySize];
            historyIndex = 0;
            sampleRate = AudioSettings.outputSampleRate;
            lookaheadQueue = new Queue<float>(lookaheadSize);
            bandIntensities = new float[NumBands];
        }

        private void Update()
        {
            if (processingMode == ProcessingMode.Stems)
            {
                if (!isPlaying && (bassAudioSource.isPlaying || drumsAudioSource.isPlaying || otherAudioSource.isPlaying || vocalsAudioSource.isPlaying))
                {
                    musicStartDspTime = AudioSettings.dspTime;
                    isPlaying = true;
                    Debug.Log("Stem audio started playing.");
                }
                else if (isPlaying && (!bassAudioSource.isPlaying && !drumsAudioSource.isPlaying && !otherAudioSource.isPlaying && !vocalsAudioSource.isPlaying))
                {
                    isPlaying = false;
                    Debug.Log("Stem audio stopped playing.");
                }
            }
            else
            {
                if (mainAudioSource == null || mainAudioSource.clip == null) return;

                if (!isPlaying && mainAudioSource.isPlaying)
                {
                    musicStartDspTime = AudioSettings.dspTime;
                    isPlaying = true;
                    Debug.Log("Main audio started playing.");
                }
                else if (isPlaying && !mainAudioSource.isPlaying)
                {
                    isPlaying = false;
                    Debug.Log("Main audio stopped playing.");
                }
            }

            if (processingMode != ProcessingMode.Preprocessed && isPlaying)
            {
                AnalyzeAudio();
                UpdateIntensities();
                DetectKick();
                DetectBeat();
                DetectDrumPeaks();
                UpdateBPM();
            }
        }

        private void AnalyzeAudio()
        {
            if (processingMode == ProcessingMode.Stems)
            {
                if (bassAudioSource != null && bassAudioSource.isPlaying)
                {
                    bassAudioSource.GetSpectrumData(bassSpectrumData, 0, FFTWindow.BlackmanHarris);
                    //Debug.log($"Bass spectrum sum: {bassSpectrumData.Sum()}");
                }
                if (drumsAudioSource != null && drumsAudioSource.isPlaying)
                {
                    drumsAudioSource.GetSpectrumData(drumsSpectrumData, 0, FFTWindow.BlackmanHarris);
                    //Debug.log($"Drums spectrum sum: {drumsSpectrumData.Sum()}");
                }
                if (otherAudioSource != null && otherAudioSource.isPlaying)
                {
                    otherAudioSource.GetSpectrumData(otherSpectrumData, 0, FFTWindow.BlackmanHarris);
                    //Debug.log($"Other spectrum sum: {otherSpectrumData.Sum()}");
                }
                if (vocalsAudioSource != null && vocalsAudioSource.isPlaying)
                {
                    vocalsAudioSource.GetSpectrumData(vocalsSpectrumData, 0, FFTWindow.BlackmanHarris);
                    //Debug.log($"Vocals spectrum sum: {vocalsSpectrumData.Sum()}");
                }
            }
            else
            {
                if (mainAudioSource != null && mainAudioSource.isPlaying)
                {
                    mainAudioSource.GetSpectrumData(bassSpectrumData, 0, FFTWindow.BlackmanHarris);
                    //Debug.log($"Main spectrum sum: {bassSpectrumData.Sum()}");
                }
            }

            // Calculate band intensities
            for (int i = 0; i < NumBands; i++)
            {
                bandIntensities[i] = CalculateFrequencyBandFromStems(GetBandLowFreq(i), GetBandHighFreq(i));
            }

            // Normalize band intensities
            NormalizeBandIntensities();

            // Combine bands for bass, mid, and high with scaling factors
            bassIntensity = ((bandIntensities[0] + bandIntensities[1]) / 2f) * bassScale;
            midIntensity = ((bandIntensities[2] + bandIntensities[3] + bandIntensities[4]) / 3f) * midScale;
            highIntensity = ((bandIntensities[5] + bandIntensities[6] + bandIntensities[7]) / 3f) * highScale;

            // Renormalize the final intensities
            float maxIntensity = Mathf.Max(bassIntensity, midIntensity, highIntensity);
            if (maxIntensity > 0)
            {
                bassIntensity /= maxIntensity;
                midIntensity /= maxIntensity;
                highIntensity /= maxIntensity;
            }

            //Debug.log($"Final Intensities - Bass: {bassIntensity}, Mid: {midIntensity}, High: {highIntensity}");
        }

        private float CalculateFrequencyBand(float lowFreq, float highFreq)
        {
            int lowBin = FrequencyToIndex(lowFreq);
            int highBin = FrequencyToIndex(highFreq);

            float sum = 0f;
            for (int i = lowBin; i <= highBin; i++)
            {
                sum += Mathf.Sqrt((bassSpectrumData[i] + drumsSpectrumData[i] + otherSpectrumData[i] + vocalsSpectrumData[i]) / 2f);
            }

            return sum / (highBin - lowBin + 1);
        }

        private void NormalizeBandIntensities()
        {
            float max = Mathf.Max(bandIntensities);
            if (max > 0)
            {
                for (int i = 0; i < NumBands; i++)
                {
                    bandIntensities[i] /= max;
                    bandIntensities[i] = Mathf.Pow(bandIntensities[i], 0.5f); // Apply some compression
                }
            }
        }

        private float GetBandLowFreq(int bandIndex)
        {
            return 20f * Mathf.Pow(2, bandIndex);
        }

        private float GetBandHighFreq(int bandIndex)
        {
            return GetBandLowFreq(bandIndex + 1);
        }

        private int FrequencyToIndex(float frequency)
        {
            // Convert frequency to logarithmic scale
            float logFreq = Mathf.Log(frequency / 20f) / Mathf.Log(2f);
            float logSampleRate = Mathf.Log(sampleRate / 40f) / Mathf.Log(2f);
            int index = Mathf.FloorToInt(logFreq / logSampleRate * sampleSize);
            return Mathf.Clamp(index, 0, sampleSize - 1);
        }

        // Kick detection with adaptive threshold and lookahead
        private void DetectKick()
        {
            float currentEnergy = bassIntensity; // Focus on bass intensity for kick detection

            // Add current energy to the lookahead queue
            lookaheadQueue.Enqueue(currentEnergy);
            if (lookaheadQueue.Count > lookaheadSize)
            {
                lookaheadQueue.Dequeue();
            }

            // Use the maximum energy from the lookahead buffer for transient detection
            float lookaheadEnergy = Mathf.Max(lookaheadQueue.ToArray());
            float energyDifference = lookaheadEnergy - previousEnergy;
            previousEnergy = lookaheadEnergy;

            float averageEnergy = CalculateAverageEnergy();
            kickAdaptiveThreshold = averageEnergy * kickSensitivity;

            if (energyDifference > kickAdaptiveThreshold * 1.05f && Time.time - lastKickTime > 0.08f) // Reduced threshold multiplier and interval for snappier kick detection
            {
                lastKickTime = Time.time;
                OnKickDetected?.Invoke();
            }

            energyHistory[historyIndex] = lookaheadEnergy;
            historyIndex = (historyIndex + 1) % energyHistorySize;
        }

        // General beat detection with mids and highs
        private void DetectBeat()
        {
            float currentEnergy = bassIntensity + 0.7f * midIntensity + 0.5f * highIntensity; // Contribution from bass, mids, and highs
            float averageEnergy = CalculateAverageEnergy();
            beatAdaptiveThreshold = averageEnergy * beatSensitivity;

            if (currentEnergy > beatAdaptiveThreshold * 1.1f && Time.time - lastBeatTime > 0.1f)
            {
                UpdateSmoothBPM();
                lastBeatTime = Time.time;
                OnBeatDetected?.Invoke();
            }
        }

        private void UpdateSmoothBPM()
        {
            if (useManualBPM)
            {
                smoothBPM = manualBPM;
            }
            else if (Time.time - lastBeatTime > 0)
            {
                float instantBPM = 60f / (Time.time - lastBeatTime);
                smoothBPM = Mathf.Lerp(smoothBPM, instantBPM, bpmSmoothingFactor);
            }
        }

        private float CalculateAverageEnergy()
        {
            float sum = 0f;
            foreach (float energy in energyHistory)
            {
                sum += energy;
            }
            return sum / energyHistory.Length;
        }

        public float GetBassIntensity() => bassIntensity;
        public float GetMidIntensity() => midIntensity;
        public float GetHighIntensity() => highIntensity;

        public float GetSmoothBPM()
        {
            return useManualBPM ? manualBPM : smoothBPM;
        }

        // New method to get the current music time
        public float GetMusicTime()
        {
            if (!isPlaying) return 0f;
            return (float)(AudioSettings.dspTime - musicStartDspTime);
        }

        // New method to get the current bar position
        public float GetBarPosition(float beatsPerBar = 4)
        {
            float bpm = GetSmoothBPM();
            if (bpm <= 0) return 0;

            float beatsPerSecond = bpm / 60f;
            float totalBeats = GetMusicTime() * beatsPerSecond;
            return (totalBeats % beatsPerBar) / beatsPerBar;
        }

        public void SetManualBPM(float bpm)
        {
            manualBPM = bpm;
            useManualBPM = true;
        }

        public void SetAutoDetectBPM()
        {
            useManualBPM = false;
        }

        public IEnumerator PlayMusic()
        {
            if (processingMode == ProcessingMode.Stems)
            {
                if (mainAudioSource == null || bassAudioSource == null || drumsAudioSource == null || 
                    otherAudioSource == null || vocalsAudioSource == null)
                {
                    yield break;
                }

                if (mainAudioSource.clip == null || bassAudioSource.clip == null || drumsAudioSource.clip == null || 
                    otherAudioSource.clip == null || vocalsAudioSource.clip == null)
                {
                    yield break;
                }

                mainAudioSource.Stop();
                bassAudioSource.Stop();
                drumsAudioSource.Stop();
                otherAudioSource.Stop();
                vocalsAudioSource.Stop();

                yield return null;

                float mainOffset = 0f;
                List<float> offsets = new List<float>() { mainOffset, stemOffset, stemOffset, stemOffset, stemOffset };
                float minOffset = Mathf.Min(offsets.ToArray());
                double dspStartTime = AudioSettings.dspTime + 0.5 - minOffset;
                musicStartDspTime = dspStartTime;

                ScheduleAudioPlayback(mainAudioSource, mainOffset, minOffset, dspStartTime);
                ScheduleAudioPlayback(bassAudioSource, stemOffset, minOffset, dspStartTime);
                ScheduleAudioPlayback(drumsAudioSource, stemOffset, minOffset, dspStartTime);
                ScheduleAudioPlayback(otherAudioSource, stemOffset, minOffset, dspStartTime);
                ScheduleAudioPlayback(vocalsAudioSource, stemOffset, minOffset, dspStartTime);

                isPlaying = true;

                yield return null;
            }
            else
            {
                if (mainAudioSource != null && mainAudioSource.clip != null && !mainAudioSource.isPlaying)
                {
                    mainAudioSource.Play();
                    musicStartDspTime = AudioSettings.dspTime;
                    isPlaying = true;
                }
            }
        }

        private void ScheduleAudioPlayback(AudioSource audioSource, float offset, float minOffset, double dspStartTime)
        {
            if (audioSource != null && audioSource.clip != null)
            {
                int sampleOffset = (int)((offset - minOffset) * audioSource.clip.frequency);
                sampleOffset = Mathf.Clamp(sampleOffset, 0, audioSource.clip.samples - 1);
                audioSource.timeSamples = sampleOffset;
                audioSource.PlayScheduled(dspStartTime);
            }
        }

        private IEnumerator CheckActualPlaybackStart(AudioSource audioSource, string stemName, double scheduledDspTime)
        {
            while (!audioSource.isPlaying)
            {
                yield return null;
            }

            double actualStartTime = AudioSettings.dspTime;
            //Debug.log($"[STEM PLAYBACK] {stemName} actually started playing at DSP time: {actualStartTime}");
            //Debug.log($"[STEM PLAYBACK] {stemName} delay: {actualStartTime - scheduledDspTime} seconds");
        }

        private void LogActualStartTime(AudioSource audioSource, string stemName)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                Debug.Log($"[DEBUG] {stemName} actual start time: {audioSource.time}");
            }
            else
            {
                Debug.Log($"[DEBUG] {stemName} is not playing");
            }
        }

        private IEnumerator CheckStemPlaybackStart()
        {
            float checkDuration = 5f; // Check for 5 seconds
            float elapsedTime = 0f;

            while (elapsedTime < checkDuration)
            {
                yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
                elapsedTime += 0.1f;

                LogStemPlaybackStatus(mainAudioSource, "Main");
                LogStemPlaybackStatus(bassAudioSource, "Bass");
                LogStemPlaybackStatus(drumsAudioSource, "Drums");
                LogStemPlaybackStatus(otherAudioSource, "Other");
                LogStemPlaybackStatus(vocalsAudioSource, "Vocals");
            }
        }

        private void LogStemPlaybackStatus(AudioSource audioSource, string stemName)
        {
            if (audioSource != null)
            {
                if (audioSource.isPlaying)
                {
                    Debug.Log($"[DEBUG] {stemName} stem started playing at time: {audioSource.time}");
                }
                else
                {
                    Debug.Log($"[DEBUG] {stemName} stem is not playing yet");
                }
            }
        }

        public void PauseMusic()
        {
            if (processingMode == ProcessingMode.Stems)
            {
                if (mainAudioSource != null) mainAudioSource.Pause();
                if (bassAudioSource != null) bassAudioSource.Pause();
                if (drumsAudioSource != null) drumsAudioSource.Pause();
                if (otherAudioSource != null) otherAudioSource.Pause();
                if (vocalsAudioSource != null) vocalsAudioSource.Pause();
                isPlaying = false;
            }
            else if (mainAudioSource != null && mainAudioSource.isPlaying)
            {
                mainAudioSource.Pause();
                isPlaying = false;
            }
        }

        public void StopMusic()
        {
            if (processingMode == ProcessingMode.Stems)
            {
                if (mainAudioSource != null) mainAudioSource.Stop();
                if (bassAudioSource != null) bassAudioSource.Stop();
                if (drumsAudioSource != null) drumsAudioSource.Stop();
                if (otherAudioSource != null) otherAudioSource.Stop();
                if (vocalsAudioSource != null) vocalsAudioSource.Stop();
                isPlaying = false;
            }
            else if (mainAudioSource != null)
            {
                mainAudioSource.Stop();
                isPlaying = false;
            }
        }

        // Add these methods to allow runtime adjustment of scaling factors
        public void SetBassScale(float scale) => bassScale = Mathf.Clamp(scale, 0f, 2f);
        public void SetMidScale(float scale) => midScale = Mathf.Clamp(scale, 0f, 2f);
        public void SetHighScale(float scale) => highScale = Mathf.Clamp(scale, 0f, 2f);

        private float CalculateFrequencyBandFromStems(float lowFreq, float highFreq)
        {
            int lowBin = FrequencyToIndex(lowFreq);
            int highBin = FrequencyToIndex(highFreq);

            float sum = 0f;
            for (int i = lowBin; i <= highBin; i++)
            {
                sum += Mathf.Sqrt(bassSpectrumData[i] + drumsSpectrumData[i] + otherSpectrumData[i] + vocalsSpectrumData[i]);
            }

            return sum / (highBin - lowBin + 1);
        }

        private void EnsureAudioSourcesExist()
        {
            if (mainAudioSource == null)
                mainAudioSource = GetOrAddAudioSource("MainAudioSource");
            
            if (bassAudioSource == null)
                bassAudioSource = GetOrAddAudioSource("BassAudioSource", true);
            
            if (drumsAudioSource == null)
                drumsAudioSource = GetOrAddAudioSource("DrumsAudioSource", true);
            
            if (otherAudioSource == null)
                otherAudioSource = GetOrAddAudioSource("OtherAudioSource", true);
            
            if (vocalsAudioSource == null)
                vocalsAudioSource = GetOrAddAudioSource("VocalsAudioSource", true);
        }

        private AudioSource GetOrAddAudioSource(string name, bool mute = false)
        {
            AudioSource audioSource = transform.Find(name)?.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                GameObject child = new GameObject(name);
                child.transform.SetParent(transform);
                audioSource = child.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            audioSource.mute = mute;
            return audioSource;
        }

        private IEnumerator VerifyStemLengths()
        {
            if (processingMode != ProcessingMode.Stems || stemLengthsVerified)
                yield break;

            // Wait for all clips to be fully loaded
            while (mainAudioSource.clip == null || bassAudioSource.clip == null || 
                   drumsAudioSource.clip == null || otherAudioSource.clip == null || 
                   vocalsAudioSource.clip == null)
            {
                yield return null;
            }

            float mainLength = mainAudioSource.clip.length;
            float bassLength = bassAudioSource.clip.length;
            float drumsLength = drumsAudioSource.clip.length;
            float otherLength = otherAudioSource.clip.length;
            float vocalsLength = vocalsAudioSource.clip.length;

            Debug.Log($"Audio clip lengths: Main: {mainLength}, Bass: {bassLength}, Drums: {drumsLength}, Other: {otherLength}, Vocals: {vocalsLength}");

            const float tolerance = 0.01f; // 10 milliseconds tolerance

            if (Mathf.Abs(mainLength - bassLength) > tolerance ||
                Mathf.Abs(mainLength - drumsLength) > tolerance ||
                Mathf.Abs(mainLength - otherLength) > tolerance ||
                Mathf.Abs(mainLength - vocalsLength) > tolerance)
            {
                Debug.LogError($"Stem lengths do not match the main audio length. Differences: " +
                               $"Bass: {mainLength - bassLength}, " +
                               $"Drums: {mainLength - drumsLength}, " +
                               $"Other: {mainLength - otherLength}, " +
                               $"Vocals: {mainLength - vocalsLength}");
                stemLengthsVerified = false;
            }
            else
            {
                stemLengthsVerified = true;
                Debug.Log("All stem lengths verified and match the main audio length within tolerance.");
            }
        }

        public void SetProcessingMode(ProcessingMode mode)
        {
            processingMode = mode;
        }

        // Add this new method to update all intensities
        private void UpdateIntensities()
        {
            //Debug.log("UpdateIntensities called");

            for (int stemIndex = 0; stemIndex < System.Enum.GetValues(typeof(StemType)).Length; stemIndex++)
            {
                float[] spectrumData = GetSpectrumDataForStem((StemType)stemIndex);
                stemBassIntensities[stemIndex] = CalculateBandIntensity(spectrumData, FrequencyBand.Bass);
                stemMidIntensities[stemIndex] = CalculateBandIntensity(spectrumData, FrequencyBand.Mid);
                stemHighIntensities[stemIndex] = CalculateBandIntensity(spectrumData, FrequencyBand.High);
                stemAllIntensities[stemIndex] = (stemBassIntensities[stemIndex] + stemMidIntensities[stemIndex] + stemHighIntensities[stemIndex]) / 3f;

                //Debug.log($"Stem {(StemType)stemIndex} - Bass: {stemBassIntensities[stemIndex]}, Mid: {stemMidIntensities[stemIndex]}, High: {stemHighIntensities[stemIndex]}, All: {stemAllIntensities[stemIndex]}");
            }

            // Update overall intensities
            bassIntensity = CalculateOverallIntensity(stemBassIntensities);
            midIntensity = CalculateOverallIntensity(stemMidIntensities);
            highIntensity = CalculateOverallIntensity(stemHighIntensities);

            //Debug.log($"Overall - Bass: {bassIntensity}, Mid: {midIntensity}, High: {highIntensity}");
        }

        private float[] GetSpectrumDataForStem(StemType stemType)
        {
            switch (stemType)
            {
                case StemType.Bass: return bassSpectrumData;
                case StemType.Drums: return drumsSpectrumData;
                case StemType.Other: return otherSpectrumData;
                case StemType.Vocals: return vocalsSpectrumData;
                default: return new float[sampleSize];
            }
        }

        private float CalculateBandIntensity(float[] spectrumData, FrequencyBand band)
        {
            int lowBin = FrequencyToIndex(GetBandLowFreq((int)band));
            int highBin = FrequencyToIndex(GetBandHighFreq((int)band));

            float sum = 0f;
            for (int i = lowBin; i <= highBin; i++)
            {
                sum += spectrumData[i];
            }
            float intensity = sum / (highBin - lowBin + 1);

            // Add debug logging
            //Debug.log($"Band {band} - Low: {lowBin}, High: {highBin}, Intensity: {intensity}");

            return intensity;
        }

        private float CalculateOverallIntensity(float[] intensities)
        {
            float sum = 0f;
            for (int i = 0; i < intensities.Length; i++)
            {
                sum += intensities[i];
            }
            return sum / intensities.Length;
        }

        // Update this method to use the 2D array
        public float GetStemIntensity(StemType stemType, FrequencyBand band)
        {
            if (processingMode != ProcessingMode.Stems)
            {
                Debug.LogWarning("Attempting to get stem intensity when not in Stems mode.");
                return 0f;
            }

            switch (band)
            {
                case FrequencyBand.Bass:
                    return stemBassIntensities[(int)stemType];
                case FrequencyBand.Mid:
                    return stemMidIntensities[(int)stemType];
                case FrequencyBand.High:
                    return stemHighIntensities[(int)stemType];
                case FrequencyBand.All:
                    return stemAllIntensities[(int)stemType];
                default:
                    return 0f;
            }
        }

        private void SetupStemAudioSources()
        {
            if (muteMixerGroup == null)
            {
                Debug.LogError("Mute Mixer Group is not assigned. Stem audio will be audible.");
                return;
            }

            SetAudioSourceMixer(bassAudioSource, muteMixerGroup);
            SetAudioSourceMixer(drumsAudioSource, muteMixerGroup);
            SetAudioSourceMixer(otherAudioSource, muteMixerGroup);
            SetAudioSourceMixer(vocalsAudioSource, muteMixerGroup);
        }

        private void SetAudioSourceMixer(AudioSource source, AudioMixerGroup mixerGroup)
        {
            if (source != null)
            {
                source.outputAudioMixerGroup = mixerGroup;
            }
        }

        private void DetectDrumPeaks()
        {
            float currentTime = Time.time;
            float drumBassIntensity = stemBassIntensities[(int)StemType.Drums];  // This is correct now

            // Update adaptive threshold
            adaptiveThreshold = Mathf.Max(drumBassIntensity, adaptiveThreshold * THRESHOLD_DECAY);

            if (IsDrumPeak(drumBassIntensity) && (currentTime - lastPeakTime) >= minPeakDistance)
            {
                float interval = currentTime - lastPeakTime;
                if (interval >= MIN_PEAK_INTERVAL && interval <= MAX_PEAK_INTERVAL)
                {
                    peakIntervals.Add(interval);
                    Debug.Log($"Peak detected. Intensity: {drumBassIntensity}, Threshold: {adaptiveThreshold}, Interval: {interval}");
                }
                lastPeakTime = currentTime;
            }
        }

        private void UpdateBPM()
        {
            if (Time.time - lastAnalysisTime < analysisInterval) return;

            if (peakIntervals.Count >= MIN_PEAKS_FOR_ANALYSIS)
            {
                // Remove outliers (intervals that are too short or too long)
                peakIntervals.Sort();
                int removeCount = Mathf.FloorToInt(peakIntervals.Count * 0.1f); // Remove 10% from each end
                peakIntervals.RemoveRange(0, removeCount);
                peakIntervals.RemoveRange(peakIntervals.Count - removeCount, removeCount);

                float averageInterval = peakIntervals.Average();
                float newBPM = 60f / averageInterval;

                // Smooth the BPM change
                currentBPM = Mathf.Lerp(currentBPM, newBPM, 0.2f);

                Debug.Log($"BPM updated: {currentBPM}, Based on {peakIntervals.Count} intervals");

                peakIntervals.Clear();
            }

            lastAnalysisTime = Time.time;
        }

        private bool IsDrumPeak(float intensity)
        {
            return intensity > adaptiveThreshold * THRESHOLD_FACTOR;
        }
    }
}