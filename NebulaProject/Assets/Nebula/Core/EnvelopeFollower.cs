using UnityEngine;
using UnityEngine.Events;
using Nebula;

namespace Nebula
{
    public class EnvelopeFollower : MonoBehaviour
    {
        [System.Serializable]
        public class FloatEvent : UnityEvent<float> { }

        private MusicProcessor musicProcessor;
        [SerializeField] private MusicProcessor.FrequencyBand frequencyBand = MusicProcessor.FrequencyBand.Bass;
        [SerializeField] private MusicProcessor.StemType stemType = MusicProcessor.StemType.None;
        [SerializeField] private float attackTime = 0.1f;
        [SerializeField] private float releaseTime = 0.1f;
        [SerializeField] private float sensitivity = 1f;
        [SerializeField] private float threshold = 0f;

        public FloatEvent OnValueChanged;

        private float envelopeValue = 0f;

        private void Awake()
        {
            if (musicProcessor == null)
            {
                musicProcessor = FindObjectOfType<MusicProcessor>();
            }

            if (musicProcessor == null)
            {
                Debug.LogError("MusicProcessor not found. EnvelopeFollower will not function correctly.");
            }
        }

        private void Update()
        {
            if (musicProcessor == null) return;

            float targetValue = GetIntensity() * sensitivity;
            targetValue = Mathf.Max(0, targetValue - threshold);

            float deltaTime = Time.deltaTime;
            if (targetValue > envelopeValue)
            {
                envelopeValue = Mathf.Lerp(envelopeValue, targetValue, deltaTime / attackTime);
            }
            else
            {
                envelopeValue = Mathf.Lerp(envelopeValue, targetValue, deltaTime / releaseTime);
            }

            OnValueChanged?.Invoke(envelopeValue);
        }

        private float GetIntensity()
        {
            if (stemType != MusicProcessor.StemType.None && musicProcessor.processingMode == MusicProcessor.ProcessingMode.Stems)
            {
                return musicProcessor.GetStemIntensity(stemType, frequencyBand);
            }
            else
            {
                return GetBandIntensity();
            }
        }

        private float GetBandIntensity()
        {
            switch (frequencyBand)
            {
                case MusicProcessor.FrequencyBand.Bass:
                    return musicProcessor.GetBassIntensity();
                case MusicProcessor.FrequencyBand.Mid:
                    return musicProcessor.GetMidIntensity();
                case MusicProcessor.FrequencyBand.High:
                    return musicProcessor.GetHighIntensity();
                case MusicProcessor.FrequencyBand.All:
                    return (musicProcessor.GetBassIntensity() + musicProcessor.GetMidIntensity() + musicProcessor.GetHighIntensity()) / 3f;
                default:
                    return 0f;
            }
        }
    }
}
