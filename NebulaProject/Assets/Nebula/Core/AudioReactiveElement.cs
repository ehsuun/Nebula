using UnityEngine;
using System;
using Nebula.VisualElements;

namespace Nebula
{
    public abstract class AudioReactiveElement : VisualBehavior
    {
        [SerializeField] protected ReactiveMode reactiveMode = ReactiveMode.All;

        public enum ReactiveMode { All, Bass, Mid, High, Beat }

        protected float bassIntensity;
        protected float midIntensity;
        protected float highIntensity;

        protected bool isBeat;
        protected float timeSinceLastBeat;

        protected override void Start()
        {
            base.Start();
            if (musicProcessor != null)
            {
                musicProcessor.OnBeatDetected += HandleBeatDetected;
            }
        }

        protected void OnDestroy()
        {
            if (musicProcessor != null)
            {
                musicProcessor.OnBeatDetected -= HandleBeatDetected;
            }
        }

        private void HandleBeatDetected()
        {
            isBeat = true;
            timeSinceLastBeat = 0f;
        }

        protected override void Update()
        {
            UpdateAudioData();
            base.Update();

            timeSinceLastBeat += Time.deltaTime;
            if (timeSinceLastBeat > 0.1f) // Reset beat flag after a short duration
            {
                isBeat = false;
            }
        }

        private void UpdateAudioData()
        {
            if (musicProcessor == null) return;

            bassIntensity = musicProcessor.GetBassIntensity() * sensitivity;
            midIntensity = musicProcessor.GetMidIntensity() * sensitivity;
            highIntensity = musicProcessor.GetHighIntensity() * sensitivity;
        }

        protected float GetAverageIntensity()
        {
            return (bassIntensity + midIntensity + highIntensity) / 3f;
        }

        protected bool IsOnBeat() => isBeat;

        // Override these methods from VisualBehavior to use the cached values
        protected override float GetBassIntensity() => bassIntensity;
        protected override float GetMidIntensity() => midIntensity;
        protected override float GetHighIntensity() => highIntensity;
    }
}