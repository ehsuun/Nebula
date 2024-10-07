using UnityEngine;
using UnityEditor;
using Nebula;

namespace Nebula.Editor
{
    [CustomEditor(typeof(EnvelopeFollower))]
    public class EnvelopeFollowerEditor : UnityEditor.Editor
    {
        private EnvelopeFollower envelopeFollower;
        private float lastEnvelopeValue = 0f;

        private void OnEnable()
        {
            envelopeFollower = (EnvelopeFollower)target;
            EditorApplication.update += UpdateEnvelopeValue;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateEnvelopeValue;
        }

        private void UpdateEnvelopeValue()
        {
            if (envelopeFollower != null && !Application.isPlaying)
            {
                lastEnvelopeValue = 0f; // Reset to 0 when not playing
            }
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            // Display the envelope value bar
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Envelope Value", EditorStyles.boldLabel);
            
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            EditorGUI.DrawRect(r, new Color(0.1f, 0.1f, 0.1f));
            
            r.width *= lastEnvelopeValue;
            EditorGUI.DrawRect(r, new Color(0.2f, 0.8f, 0.2f));
            
            EditorGUILayout.LabelField($"Value: {lastEnvelopeValue:F2}");
            
            EditorGUILayout.Space(10);

            // Draw the default inspector
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            if (Application.isPlaying && envelopeFollower != null)
            {
                // Add a listener to update the envelope value
                envelopeFollower.OnValueChanged.AddListener(UpdateValue);
            }
        }

        private void UpdateValue(float value)
        {
            lastEnvelopeValue = value;
            Repaint();
        }
    }
}