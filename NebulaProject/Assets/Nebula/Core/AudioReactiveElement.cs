using UnityEngine;
using System;
using System.Collections.Generic;

namespace Nebula
{
    public abstract class AudioReactiveElement : VisualBehavior
    {
        [Serializable]
        public class ReactionSettings
        {
            public ReactionMode mode;
            public float intensity = 1f;
            public float smoothing = 0.1f;
        }

        public enum ReactionMode
        {
            Scale,
            RigidbodyForce,
            Orbit,
            Rotate,
            ColorChange,
            EmissionIntensity,
            ParticleEmission,
            ShaderParameter,
            LightIntensity,
            CameraShake,
            TextureOffset,
            MeshDeformation
        }

        public enum ReactiveMode { All, Bass, Mid, High, Beat }

        [SerializeField] protected ReactiveMode reactiveMode = ReactiveMode.All;
        [SerializeField] protected List<ReactionSettings> reactions = new List<ReactionSettings>();

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

            ApplyReactions();
        }

        private void UpdateAudioData()
        {
            if (musicProcessor == null) return;

            bassIntensity = musicProcessor.GetBassIntensity() * sensitivity;
            midIntensity = musicProcessor.GetMidIntensity() * sensitivity;
            highIntensity = musicProcessor.GetHighIntensity() * sensitivity;
        }

        protected virtual void ApplyReactions()
        {
            float intensity = GetReactiveIntensity();
            foreach (var reaction in reactions)
            {
                ApplyReaction(reaction, intensity);
            }
        }

        protected virtual void ApplyReaction(ReactionSettings reaction, float intensity)
        {
            float smoothedIntensity = Mathf.Lerp(0f, reaction.intensity * intensity, reaction.smoothing);

            switch (reaction.mode)
            {
                case ReactionMode.Scale:
                    ApplyScale(smoothedIntensity);
                    break;
                case ReactionMode.RigidbodyForce:
                    ApplyRigidbodyForce(smoothedIntensity);
                    break;
                case ReactionMode.Orbit:
                    ApplyOrbit(smoothedIntensity);
                    break;
                case ReactionMode.Rotate:
                    ApplyRotation(smoothedIntensity);
                    break;
                case ReactionMode.ColorChange:
                    ApplyColorChange(smoothedIntensity);
                    break;
                case ReactionMode.EmissionIntensity:
                    ApplyEmissionIntensity(smoothedIntensity);
                    break;
                case ReactionMode.ParticleEmission:
                    ApplyParticleEmission(smoothedIntensity);
                    break;
                case ReactionMode.ShaderParameter:
                    ApplyShaderParameter(smoothedIntensity);
                    break;
                case ReactionMode.LightIntensity:
                    ApplyLightIntensity(smoothedIntensity);
                    break;
                case ReactionMode.CameraShake:
                    ApplyCameraShake(smoothedIntensity);
                    break;
                case ReactionMode.TextureOffset:
                    ApplyTextureOffset(smoothedIntensity);
                    break;
                case ReactionMode.MeshDeformation:
                    ApplyMeshDeformation(smoothedIntensity);
                    break;
            }
        }

        protected virtual float GetReactiveIntensity()
        {
            switch (reactiveMode)
            {
                case ReactiveMode.Bass:
                    return bassIntensity;
                case ReactiveMode.Mid:
                    return midIntensity;
                case ReactiveMode.High:
                    return highIntensity;
                case ReactiveMode.Beat:
                    return isBeat ? 1f : 0f;
                case ReactiveMode.All:
                default:
                    return (bassIntensity + midIntensity + highIntensity) / 3f;
            }
        }

        // Implement these methods in derived classes or provide default implementations
        protected virtual void ApplyScale(float intensity) { }
        protected virtual void ApplyRigidbodyForce(float intensity) { }
        protected virtual void ApplyOrbit(float intensity) { }
        protected virtual void ApplyRotation(float intensity) { }
        protected virtual void ApplyColorChange(float intensity) { }
        protected virtual void ApplyEmissionIntensity(float intensity) { }
        protected virtual void ApplyParticleEmission(float intensity) { }
        protected virtual void ApplyShaderParameter(float intensity) { }
        protected virtual void ApplyLightIntensity(float intensity) { }
        protected virtual void ApplyCameraShake(float intensity) { }
        protected virtual void ApplyTextureOffset(float intensity) { }
        protected virtual void ApplyMeshDeformation(float intensity) { }

        // Helper methods
        protected bool IsOnBeat() => isBeat;
        protected override float GetBassIntensity() => bassIntensity;
        protected override float GetMidIntensity() => midIntensity;
        protected override float GetHighIntensity() => highIntensity;
    }
}