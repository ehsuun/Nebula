using UnityEngine;
using Nebula;

namespace Nebula.VisualElements
{
    public class Spectrum : AudioReactiveElement
    {
        [SerializeField] private int numberOfBars = 64;
        [SerializeField] private float maxHeight = 10f;
        [SerializeField] private float barWidth = 0.5f;
        [SerializeField] private float spacing = 0.1f;
        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color peakColor = Color.red;
        [SerializeField] private MusicProcessor.StemType selectedStem = MusicProcessor.StemType.None;

        private GameObject[] bars;
        private float[] spectrumData;

        protected override void Start()
        {
            base.Start();
            InitializeSpectrum();
        }

        private void InitializeSpectrum()
        {
            bars = new GameObject[numberOfBars];
            spectrumData = new float[numberOfBars];

            for (int i = 0; i < numberOfBars; i++)
            {
                GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.transform.SetParent(transform);
                bar.transform.localPosition = new Vector3(i * (barWidth + spacing), 0, 0);
                bar.transform.localScale = new Vector3(barWidth, 0.1f, barWidth);
                bars[i] = bar;

                Renderer renderer = bar.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = baseColor;
            }
        }

        protected override void ReactToMusic()
        {
            if (musicProcessor == null) return;

            // Get the spectrum data based on the selected stem or main audio
            float[] rawSpectrumData = GetSpectrumData();

            // Process the raw spectrum data to fit our number of bars
            for (int i = 0; i < numberOfBars; i++)
            {
                int startIndex = Mathf.FloorToInt(i * rawSpectrumData.Length / numberOfBars);
                int endIndex = Mathf.FloorToInt((i + 1) * rawSpectrumData.Length / numberOfBars);
                float sum = 0f;
                for (int j = startIndex; j < endIndex; j++)
                {
                    sum += rawSpectrumData[j];
                }
                spectrumData[i] = sum / (endIndex - startIndex);
            }

            // Update bar heights and colors
            for (int i = 0; i < numberOfBars; i++)
            {
                float height = spectrumData[i] * maxHeight;
                bars[i].transform.localScale = new Vector3(barWidth, height, barWidth);
                bars[i].transform.localPosition = new Vector3(i * (barWidth + spacing), height / 2, 0);

                float t = Mathf.InverseLerp(0, maxHeight, height);
                bars[i].GetComponent<Renderer>().material.color = Color.Lerp(baseColor, peakColor, t);
            }
        }

        private float[] GetSpectrumData()
        {
            if (musicProcessor.processingMode != MusicProcessor.ProcessingMode.Stems || selectedStem == MusicProcessor.StemType.None)
            {
                // Use main audio spectrum
                float[] data = new float[1024];
                musicProcessor.mainAudioSource.GetSpectrumData(data, 0, FFTWindow.BlackmanHarris);
                return data;
            }
            else
            {
                // Use selected stem spectrum
                switch (selectedStem)
                {
                    case MusicProcessor.StemType.Bass:
                        return musicProcessor.bassSpectrumData;
                    case MusicProcessor.StemType.Drums:
                        return musicProcessor.drumsSpectrumData;
                    case MusicProcessor.StemType.Other:
                        return musicProcessor.otherSpectrumData;
                    case MusicProcessor.StemType.Vocals:
                        return musicProcessor.vocalsSpectrumData;
                    default:
                        return new float[1024];
                }
            }
        }

        public void SetSelectedStem(MusicProcessor.StemType stemType)
        {
            selectedStem = stemType;
        }
    }
}