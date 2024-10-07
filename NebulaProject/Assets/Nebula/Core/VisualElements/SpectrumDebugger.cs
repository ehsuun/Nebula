using UnityEngine;
using System.Collections.Generic;

namespace Nebula
{
    public class SpectrumDebugger : MonoBehaviour
    {
        public GameObject cubePrefab;
        public float maxHeight = 10f;
        public float spacing = 1f;
        public float smoothTime = 0.1f;

        private List<Transform> cubes = new List<Transform>();
        private List<float> velocities = new List<float>();
        private MusicProcessor musicProcessor;

        private void Start()
        {
            if (cubePrefab == null)
            {
                Debug.LogError("Cube prefab is not assigned to SpectrumDebugger!");
                return;
            }

            musicProcessor = FindObjectOfType<MusicProcessor>();
            if (musicProcessor == null)
            {
                Debug.LogError("MusicProcessor not found. Make sure it exists in the scene.");
                return;
            }

            InitializeCubes();
        }

        private void InitializeCubes()
        {
            if (musicProcessor == null) return;

            // Assuming MusicProcessor has a property or method to get the number of frequency bands
            int bandCount = 8; // Default to 8 bands if not available in MusicProcessor

            for (int i = 0; i < bandCount; i++)
            {
                GameObject cube = Instantiate(cubePrefab, transform);
                cube.transform.localPosition = new Vector3(i * spacing, 0, 0);
                cubes.Add(cube.transform);
                velocities.Add(0f);
            }
        }

        private void Update()
        {
            if (musicProcessor == null) return;

            float bassIntensity = musicProcessor.GetBassIntensity();
            float midIntensity = musicProcessor.GetMidIntensity();
            float highIntensity = musicProcessor.GetHighIntensity();

            // Assuming 8 bands: 3 for bass, 3 for mid, 2 for high
            float[] bands = new float[] 
            {
                bassIntensity, bassIntensity, bassIntensity,
                midIntensity, midIntensity, midIntensity,
                highIntensity, highIntensity
            };

            for (int i = 0; i < bands.Length; i++)
            {
                if (i >= cubes.Count) break;

                float targetHeight = bands[i] * maxHeight;
                Vector3 currentScale = cubes[i].localScale;
                float velocity = velocities[i];
                float newY = Mathf.SmoothDamp(currentScale.y, targetHeight, ref velocity, smoothTime);
                velocities[i] = velocity;

                Vector3 newScale = currentScale;
                newScale.y = newY;
                cubes[i].localScale = newScale;

                Vector3 newPosition = cubes[i].localPosition;
                newPosition.y = newY / 2;
                cubes[i].localPosition = newPosition;

                // Color the cube based on its height (red for low, green for high)
                float normalizedHeight = newY / maxHeight;
                cubes[i].GetComponent<Renderer>().material.color = Color.Lerp(Color.red, Color.green, normalizedHeight);
            }
        }

        private void OnGUI()
        {
            if (musicProcessor == null) return;

            GUI.Label(new Rect(10, 10, 200, 20), $"Bass: {musicProcessor.GetBassIntensity():F4}");
            GUI.Label(new Rect(10, 30, 200, 20), $"Mid: {musicProcessor.GetMidIntensity():F4}");
            GUI.Label(new Rect(10, 50, 200, 20), $"High: {musicProcessor.GetHighIntensity():F4}");
        }
    }
}
