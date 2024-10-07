using UnityEditor;
using UnityEngine;
using Nebula.VisualElements;

namespace Nebula.Editor
{
    [CustomEditor(typeof(ReactToMusic))]
    public class ReactToMusicEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ReactToMusic reactToMusic = (ReactToMusic)target;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("reactions"), true);

            if (reactToMusic.reactions != null)
            {
                for (int i = 0; i < reactToMusic.reactions.Length; i++)
                {
                    ReactToMusic.ReactionSettings reaction = reactToMusic.reactions[i];
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Reaction {i + 1}", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].mode"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].intensity"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].smoothing"));

                    switch (reaction.mode)
                    {
                        case ReactToMusic.ReactionMode.Scale:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].baseScale"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].maxScaleMultiplier"));
                            break;
                        case ReactToMusic.ReactionMode.RigidbodyForce:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].isExplosive"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].forceDirection"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].maxForce"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].explosiveRadius"));
                            break;
                        case ReactToMusic.ReactionMode.ColorChange:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].colorGradient"));
                            break;
                        case ReactToMusic.ReactionMode.MeshDeformation:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].deformationAmount"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].deformationFrequency"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].deformationSeed"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].deformationDirection"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty($"reactions.Array.data[{i}].deformationSpeed"));
                            break;
                        // ... (other cases remain unchanged)
                        // Add other cases as needed
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}