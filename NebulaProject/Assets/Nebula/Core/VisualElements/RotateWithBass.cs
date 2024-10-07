using UnityEngine;

namespace Nebula.VisualElements
{
    public class RotateWithBass : VisualBehavior
    {
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float maxRotationSpeed = 360f;

        protected override void ReactToMusic()
        {
            float bassIntensity = GetBassIntensity();
            float rotationSpeed = bassIntensity * maxRotationSpeed;
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }
}