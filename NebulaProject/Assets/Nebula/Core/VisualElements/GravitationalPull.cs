using UnityEngine;
using System.Collections.Generic;

namespace Nebula.VisualElements
{
    public class GravitationalPull : MonoBehaviour
    {
        [System.Serializable]
        public class ParticleSettings
        {
            public GameObject prefab;
            public int count = 10;
        }

        public List<ParticleSettings> particleSettings = new List<ParticleSettings>();
        public float baseGravityStrength = 9.8f;
        public float variableGravityStrength = 5f;
        [Range(-1f, 1f)]
        public float intensity = 0f;
        public float spawnRadius = 5f;
        public float minInitialVelocity = 1f;
        public float maxInitialVelocity = 5f;

        private List<Rigidbody> particles = new List<Rigidbody>();

        void Start()
        {
            SpawnParticles();
        }

        void FixedUpdate()
        {
            ApplyGravitationalForce();
        }

        void SpawnParticles()
        {
            foreach (var settings in particleSettings)
            {
                for (int i = 0; i < settings.count; i++)
                {
                    Vector3 randomPosition = Random.insideUnitSphere * spawnRadius + transform.position;
                    GameObject particle = Instantiate(settings.prefab, randomPosition, Quaternion.identity);
                    Rigidbody rb = particle.GetComponent<Rigidbody>();
                    
                    if (rb == null)
                    {
                        rb = particle.AddComponent<Rigidbody>();
                    }

                    // Add initial velocity
                    Vector3 randomVelocity = Random.insideUnitSphere * Random.Range(minInitialVelocity, maxInitialVelocity);
                    rb.linearVelocity = randomVelocity;

                    particles.Add(rb);
                }
            }
        }

        void ApplyGravitationalForce()
        {
            float totalGravityStrength = baseGravityStrength + (variableGravityStrength * intensity);
            Vector3 center = transform.position;
            foreach (var rb in particles)
            {
                if (rb != null)
                {
                    Vector3 direction = center - rb.position;
                    rb.AddForce(direction.normalized * totalGravityStrength);
                }
            }
        }

        public void SetIntensity(float newIntensity)
        {
            intensity = Mathf.Clamp(newIntensity, -1f, 1f);
        }
    }
}