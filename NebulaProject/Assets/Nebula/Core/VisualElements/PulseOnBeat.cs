using UnityEngine;

namespace Nebula.VisualElements
{
    public class PulseOnBeat : VisualBehavior
    {
        [SerializeField] private Vector3 baseScale = Vector3.one;
        [SerializeField] private Vector3 pulseScale = Vector3.one * 1.2f;
        
        private Vector3 targetScale;

        protected override void ReactToMusic()
        {
            if (OnBeat())
            {
                targetScale = pulseScale;
            }
            else
            {
                targetScale = baseScale;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, smoothing);
        }
    }
}