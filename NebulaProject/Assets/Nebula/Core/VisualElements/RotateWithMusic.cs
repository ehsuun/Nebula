using UnityEngine;

namespace Nebula.VisualElements{
    public class RotateWithMusic : VisualBehavior
    {
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float maxRotationSpeed = 360f; // degrees per second

        protected override void ReactToMusic()
        {
            float averageIntensity = (GetBassIntensity() + GetMidIntensity() + GetHighIntensity()) / 3f;
            float rotationSpeed = averageIntensity * maxRotationSpeed;
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }

        private void OnValidate()
        {
            rotationAxis = rotationAxis.normalized;
            maxRotationSpeed = Mathf.Max(0f, maxRotationSpeed);
        }
    }
}