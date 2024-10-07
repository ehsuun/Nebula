using UnityEngine;
using UnityEngine.Events;
using Nebula;

namespace Nebula
{
    public class LFO : MonoBehaviour
    {
        [System.Serializable]
        public class FloatEvent : UnityEvent<float> { }

         private MusicProcessor musicProcessor;
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, -1, 1, 1);
        [SerializeField] private float frequency = 1f;
        [SerializeField] private float amplitude = 1f;
        [SerializeField] private float phase = 0f;
        [SerializeField] private bool syncToMusic = true;
        [SerializeField] private int barsPerCycle = 1;
        [SerializeField] private int beatsPerBar = 4;

        public FloatEvent OnValueChanged;

        private float internalTime = 0f;

        private void Awake()
        {
            if (musicProcessor == null)
            {
                musicProcessor = FindObjectOfType<MusicProcessor>();
            }

            if (musicProcessor == null)
            {
                Debug.LogError("MusicProcessor not found. LFO will not function correctly.");
            }
        }

        private void Update()
        {
            if (musicProcessor == null) return;

            float time;
            if (syncToMusic)
            {
                float barPosition = musicProcessor.GetBarPosition(beatsPerBar);
                time = (barPosition + (musicProcessor.GetMusicTime() / (60f / musicProcessor.GetSmoothBPM() * beatsPerBar * barsPerCycle))) % 1f;
            }
            else
            {
                internalTime += Time.deltaTime * frequency;
                time = internalTime % 1f;
            }

            float value = CalculateLFOValue(time);
            OnValueChanged?.Invoke(value);
        }

        private float CalculateLFOValue(float time)
        {
            float t = (time + phase) % 1f;
            return curve.Evaluate(t) * amplitude;
        }
    }
}
