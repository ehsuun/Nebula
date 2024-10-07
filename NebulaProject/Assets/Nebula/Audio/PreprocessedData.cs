using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PreprocessedAudioData", menuName = "Audio/Preprocessed Data", order = 1)]
public class PreprocessedData : ScriptableObject
{
    [System.Serializable]
    public class TimeSlice
    {
        public float time;
        public float amplitude;
        public float amplitudeBuffer;
        public float[] frequencyBands;
    }

    [System.Serializable]
    public class FrequencyBandData
    {
        public AnimationCurve bandCurve;
    }

    public List<TimeSlice> timeSlices = new List<TimeSlice>();
    public AnimationCurve amplitudeCurve;
    public AnimationCurve amplitudeBufferCurve;
    public List<FrequencyBandData> frequencyBandData;

    public float GetAmplitudeAtTime(float time)
    {
        return amplitudeCurve.Evaluate(time);
    }

    public float GetAmplitudeBufferAtTime(float time)
    {
        return amplitudeBufferCurve.Evaluate(time);
    }

    public float[] GetFrequencyBandsAtTime(float time)
    {
        float[] bands = new float[frequencyBandData.Count];
        for (int i = 0; i < frequencyBandData.Count; i++)
        {
            bands[i] = frequencyBandData[i].bandCurve.Evaluate(time);
        }
        return bands;
    }
}
