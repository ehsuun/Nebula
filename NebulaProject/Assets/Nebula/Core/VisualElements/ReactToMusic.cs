using UnityEngine;
using System;

namespace Nebula.VisualElements
{
    public class ReactToMusic : MonoBehaviour
    {
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

        [Serializable]
        public class ReactionSettings
        {
            public ReactionMode mode;
            public float intensity = 1f;
            public float smoothing = 1f;

            // Scale-specific settings
            [HideInInspector] public Vector3 baseScale = Vector3.one;
            [HideInInspector] public Vector3 maxScaleMultiplier = Vector3.one * 1.5f;

            // RigidbodyForce-specific settings
            [HideInInspector] public bool isExplosive = false;
            [HideInInspector] public Vector3 forceDirection = Vector3.up;
            [HideInInspector] public float maxForce = 5f;
            [HideInInspector] public float explosiveRadius = 3f;

            // Orbit-specific settings
            [HideInInspector] public Vector3 orbitAxis = Vector3.up;
            [HideInInspector] public float orbitSpeed = 90f;

            // Rotate-specific settings
            [HideInInspector] public Vector3 rotationAxis = Vector3.up;
            [HideInInspector] public float maxRotationSpeed = 180f;

            // ColorChange-specific settings
            public Gradient colorGradient = new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.red, 1f)
                },
                alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            };

            // EmissionIntensity-specific settings
            [HideInInspector] public float minEmission = 0f;
            [HideInInspector] public float maxEmission = 1f;

            // ParticleEmission-specific settings
            [HideInInspector] public float minEmissionRate = 10f;
            [HideInInspector] public float maxEmissionRate = 50f;

            // ShaderParameter-specific settings
            [HideInInspector] public string shaderParameterName = "_Intensity";
            [HideInInspector] public float minParameterValue = 0f;
            [HideInInspector] public float maxParameterValue = 1f;

            // LightIntensity-specific settings
            [HideInInspector] public float minLightIntensity = 0.5f;
            [HideInInspector] public float maxLightIntensity = 1.5f;

            // CameraShake-specific settings
            [HideInInspector] public float shakeAmount = 0.1f;
            [HideInInspector] public float shakeDuration = 0.2f;

            // TextureOffset-specific settings
            [HideInInspector] public Vector2 textureOffsetSpeed = new Vector2(0.1f, 0.1f);

            // MeshDeformation-specific settings
            [HideInInspector] public float deformationAmount = 0.1f;
            [HideInInspector] public float deformationFrequency = 1f;
            public int deformationSeed = 0; // Visible in the inspector
            public Vector3 deformationDirection = Vector3.up; // Direction of noise movement
            public float deformationSpeed = 1f; // Speed of noise movement
        }

        public ReactionSettings[] reactions;

        private Vector3[] originalVertices;
        private Vector3[] baseNormals;

        private WireframeRenderer wireframeRenderer;

        private void Start()
        {
            wireframeRenderer = GetComponent<WireframeRenderer>();
        }

        private void UpdateReaction(ReactionSettings reaction, float musicIntensity)
        {
            float smoothedIntensity = Mathf.Lerp(0f, reaction.intensity * musicIntensity, reaction.smoothing);

            switch (reaction.mode)
            {
                case ReactionMode.Scale:
                    UpdateScale(smoothedIntensity);
                    break;
                case ReactionMode.RigidbodyForce:
                    ApplyRigidbodyForce(smoothedIntensity);
                    break;
                case ReactionMode.Orbit:
                    UpdateOrbit(smoothedIntensity);
                    break;
                case ReactionMode.Rotate:
                    UpdateRotation(smoothedIntensity);
                    break;
                case ReactionMode.ColorChange:
                    UpdateColor(smoothedIntensity);
                    break;
                case ReactionMode.EmissionIntensity:
                    UpdateEmission(smoothedIntensity);
                    break;
                case ReactionMode.ParticleEmission:
                    UpdateParticleEmission(smoothedIntensity);
                    break;
                case ReactionMode.ShaderParameter:
                    UpdateShaderParameter(smoothedIntensity);
                    break;
                case ReactionMode.LightIntensity:
                    UpdateLightIntensity(smoothedIntensity);
                    break;
                case ReactionMode.CameraShake:
                    ApplyCameraShake(smoothedIntensity);
                    break;
                case ReactionMode.TextureOffset:
                    UpdateTextureOffset(smoothedIntensity);
                    break;
                case ReactionMode.MeshDeformation:
                    UpdateMeshDeformation(smoothedIntensity);
                    break;
            }
        }

        public void ReactToMusicIntensity(float musicIntensity)
        {
            foreach (var reaction in reactions)
            {
                UpdateReaction(reaction, musicIntensity);
            }
        }

        // Implement individual update methods for each reaction mode
        private void UpdateScale(float intensity)
        {
            ReactionSettings scaleSettings = Array.Find(reactions, r => r.mode == ReactionMode.Scale);
            if (scaleSettings == null) return;

            // Calculate the target scale based on the intensity
            Vector3 scaleMultiplier = Vector3.Lerp(Vector3.one, scaleSettings.maxScaleMultiplier, intensity);
            Vector3 targetScale = Vector3.Scale(scaleSettings.baseScale, scaleMultiplier);

            // Apply smoothing
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSettings.smoothing);
        }

        private void ApplyRigidbodyForce(float intensity)
        {
            ReactionSettings forceSettings = Array.Find(reactions, r => r.mode == ReactionMode.RigidbodyForce);
            if (forceSettings == null) return;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            float forceMagnitude = forceSettings.maxForce * intensity;

            if (forceSettings.isExplosive)
            {
                // Apply explosive force
                Collider[] colliders = Physics.OverlapSphere(transform.position, forceSettings.explosiveRadius);
                foreach (Collider hit in colliders)
                {
                    Rigidbody targetRb = hit.GetComponent<Rigidbody>();
                    if (targetRb != null)
                    {
                        targetRb.AddExplosionForce(forceMagnitude, transform.position, forceSettings.explosiveRadius);
                    }
                }
            }
            else
            {
                // Apply directional force
                Vector3 force = forceSettings.forceDirection.normalized * forceMagnitude;
                
                rb.AddForce(force, ForceMode.Impulse);
            }
        }

        private void UpdateOrbit(float intensity) { /* Implementation */ }
        private void UpdateRotation(float intensity) { /* Implementation */ }
        private void UpdateColor(float intensity)
        {
            ReactionSettings colorSettings = Array.Find(reactions, r => r.mode == ReactionMode.ColorChange);
            if (colorSettings == null) return;

            // Evaluate the color from the gradient based on the intensity
            Color targetColor = colorSettings.colorGradient.Evaluate(intensity);

            // Apply the color to the object
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = targetColor;
            }
            else
            {
                // If there's no renderer, try to apply to a UI Image component
                UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.color = targetColor;
                }
            }
        }
        private void UpdateEmission(float intensity) { /* Implementation */ }
        private void UpdateParticleEmission(float intensity) { /* Implementation */ }
        private void UpdateShaderParameter(float intensity) { /* Implementation */ }
        private void UpdateLightIntensity(float intensity) { /* Implementation */ }
        private void ApplyCameraShake(float intensity) { /* Implementation */ }
        private void UpdateTextureOffset(float intensity) { /* Implementation */ }
        private void UpdateMeshDeformation(float intensity)
        {
            ReactionSettings deformSettings = Array.Find(reactions, r => r.mode == ReactionMode.MeshDeformation);
            if (deformSettings == null) return;

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.mesh == null) return;

            Mesh mesh = meshFilter.mesh;

            // Initialize originalVertices and baseNormals if not already done
            if (originalVertices == null || originalVertices.Length != mesh.vertices.Length)
            {
                originalVertices = mesh.vertices;
                baseNormals = mesh.normals;
            }

            Vector3[] newVertices = new Vector3[originalVertices.Length];

            // Calculate the movement offset based on time, direction, and speed
            Vector3 movementOffset = deformSettings.deformationDirection.normalized * Time.time * deformSettings.deformationSpeed;

            for (int i = 0; i < originalVertices.Length; i++)
            {
                Vector3 vertex = originalVertices[i];
                
                // Calculate noise coordinates, including the movement offset
                Vector3 noisePos = vertex + movementOffset;
                
                // Use improved noise function
                float noise = ImprovedNoise(
                    noisePos * deformSettings.deformationFrequency,
                    deformSettings.deformationSeed
                );

                // Calculate displacement
                float displacement = (noise - 0.5f) * deformSettings.deformationAmount;
                
                // Apply the displacement along the surface normal
                Vector3 deformedVertex = vertex + baseNormals[i] * displacement;
                
                // Smoothly interpolate between original and deformed vertex based on intensity
                newVertices[i] = Vector3.Lerp(vertex, deformedVertex, intensity);
            }

            // Apply the new vertices to the mesh
            mesh.vertices = newVertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

public static float ImprovedNoise(Vector3 pos, int seed)
{
    // Initialize permutation array
    int[] p = new int[256];
    for (int i = 0; i < 256; i++) p[i] = i;

    // Shuffle the array using the seed
    System.Random rand = new System.Random(seed);
    for (int i = 255; i > 0; i--)
    {
        int swapIndex = rand.Next(i + 1);
        int temp = p[i];
        p[i] = p[swapIndex];
        p[swapIndex] = temp;
    }

    // Duplicate the permutation array
    int[] permutation = new int[512];
    for (int i = 0; i < 512; i++) permutation[i] = p[i % 256];

    // Helper functions
    int FastFloor(float x)
    {
        return x > 0 ? (int)x : (int)x - 1;
    }

    float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }

    float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    // Calculate noise
    float x = pos.x;
    float y = pos.y;
    float z = pos.z;

    int X = FastFloor(x) & 255;
    int Y = FastFloor(y) & 255;
    int Z = FastFloor(z) & 255;

    x -= FastFloor(x);
    y -= FastFloor(y);
    z -= FastFloor(z);

    float u = Fade(x);
    float v = Fade(y);
    float w = Fade(z);

    int A = permutation[X] + Y;
    int AA = permutation[A] + Z;
    int AB = permutation[A + 1] + Z;
    int B = permutation[X + 1] + Y;
    int BA = permutation[B] + Z;
    int BB = permutation[B + 1] + Z;

    float res = Lerp(w,
                Lerp(v,
                    Lerp(u, Grad(permutation[AA], x, y, z),
                            Grad(permutation[BA], x - 1, y, z)),
                    Lerp(u, Grad(permutation[AB], x, y - 1, z),
                            Grad(permutation[BB], x - 1, y - 1, z))),
                Lerp(v,
                    Lerp(u, Grad(permutation[AA + 1], x, y, z - 1),
                            Grad(permutation[BA + 1], x - 1, y, z - 1)),
                    Lerp(u, Grad(permutation[AB + 1], x, y - 1, z - 1),
                            Grad(permutation[BB + 1], x - 1, y - 1, z - 1)))
                );

    return res;
}

    }
}