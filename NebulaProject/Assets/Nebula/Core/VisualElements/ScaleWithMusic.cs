using UnityEngine;

namespace Nebula.VisualElements
{
    public class ScaleWithMusic : MonoBehaviour
    {
        public Vector3 baseScale = Vector3.one;
        public Vector3 maxScaleMultiplier = Vector3.one * 2f;

        [Range(0f, 1f)]
        public float smoothing = 0.1f;

        private Vector3 targetScale;

        public void UpdateScale(float intensity)
        {
            // Clamp the intensity value between 0 and 1
            intensity = Mathf.Clamp01(intensity);

            // Calculate the target scale based on the intensity
            Vector3 scaleMultiplier = Vector3.Lerp(Vector3.one, maxScaleMultiplier, intensity);
            targetScale = Vector3.Scale(baseScale, scaleMultiplier);

            // Apply smoothing
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, smoothing);
        }
    }
}
