using UnityEngine;

namespace Nebula
{
    public abstract class VisualBehavior : MonoBehaviour
    {
        [SerializeField] protected float sensitivity = 1.0f;
        [SerializeField] protected float smoothing = 0.1f;

        protected MusicProcessor musicProcessor;

        protected virtual void Start()
        {
            musicProcessor = FindObjectOfType<MusicProcessor>();
            if (musicProcessor == null)
            {
                Debug.LogError("MusicProcessor not found. Make sure it exists in the scene.");
            }
        }

        protected virtual void Update()
        {
            ReactToMusic();
        }

        protected abstract void ReactToMusic();

        // These methods are now virtual and can be overridden in derived classes
        protected virtual bool OnBeat() => false;
        protected virtual bool OnHalfBeat() => false;
        protected virtual bool OnQuarterBeat() => false;

        protected virtual float GetBassIntensity() => musicProcessor != null ? musicProcessor.GetBassIntensity() * sensitivity : 0f;
        protected virtual float GetMidIntensity() => musicProcessor != null ? musicProcessor.GetMidIntensity() * sensitivity : 0f;
        protected virtual float GetHighIntensity() => musicProcessor != null ? musicProcessor.GetHighIntensity() * sensitivity : 0f;
    }
}