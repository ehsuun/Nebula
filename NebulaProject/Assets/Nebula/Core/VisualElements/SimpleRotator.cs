using UnityEngine;

namespace Nebula.VisualElements
{
    public class SimpleRotator : MonoBehaviour
    {
        public Vector3 rotationAxis = Vector3.up;
        public float rotationSpeed = 90f; // degrees per second

        private void Update()
        {
            // Calculate the rotation amount based on the speed and time since last frame
            float rotationAmount = rotationSpeed * Time.deltaTime;

            // Apply the rotation around the specified axis
            transform.Rotate(rotationAxis, rotationAmount, Space.Self);
        }
    }
}