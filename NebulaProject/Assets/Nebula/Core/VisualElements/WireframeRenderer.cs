using UnityEngine;
using System.Collections.Generic;

namespace Nebula.VisualElements
{
    [RequireComponent(typeof(MeshFilter))]
    public class WireframeRenderer : MonoBehaviour
    {
        public Color wireframeColor = Color.black;

        private MeshFilter meshFilter;
        private Mesh wireframeMesh;
        private Material wireframeMaterial;

        private void OnEnable()
        {
            meshFilter = GetComponent<MeshFilter>();
            CreateWireframeMesh();
            CreateWireframeMaterial();
        }

        private void CreateWireframeMesh()
        {
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Mesh originalMesh = meshFilter.sharedMesh;
            wireframeMesh = new Mesh();

            Vector3[] vertices = originalMesh.vertices;
            int[] triangles = originalMesh.triangles;

            List<Vector3> wireframeVertices = new List<Vector3>();
            List<int> wireframeIndices = new List<int>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Edge 1
                wireframeVertices.Add(vertices[triangles[i]]);
                wireframeVertices.Add(vertices[triangles[i + 1]]);
                wireframeIndices.Add(wireframeVertices.Count - 2);
                wireframeIndices.Add(wireframeVertices.Count - 1);

                // Edge 2
                wireframeVertices.Add(vertices[triangles[i + 1]]);
                wireframeVertices.Add(vertices[triangles[i + 2]]);
                wireframeIndices.Add(wireframeVertices.Count - 2);
                wireframeIndices.Add(wireframeVertices.Count - 1);

                // Edge 3
                wireframeVertices.Add(vertices[triangles[i + 2]]);
                wireframeVertices.Add(vertices[triangles[i]]);
                wireframeIndices.Add(wireframeVertices.Count - 2);
                wireframeIndices.Add(wireframeVertices.Count - 1);
            }

            wireframeMesh.vertices = wireframeVertices.ToArray();
            wireframeMesh.SetIndices(wireframeIndices.ToArray(), MeshTopology.Lines, 0);

            Debug.Log($"Wireframe Mesh: {wireframeVertices.Count} vertices, {wireframeIndices.Count / 2} lines");
        }

        private void CreateWireframeMaterial()
        {
            wireframeMaterial = new Material(Shader.Find("Nebula/Wireframe"));
            wireframeMaterial.hideFlags = HideFlags.HideAndDontSave;
            wireframeMaterial.SetColor("_WireColor", wireframeColor);
        }

        private void OnRenderObject()
        {
            if (!enabled || wireframeMesh == null || wireframeMaterial == null) return;

            RenderParams renderParams = new RenderParams(wireframeMaterial);
            Graphics.RenderMesh(renderParams, wireframeMesh, 0, transform.localToWorldMatrix);
        }

        public void EnableWireframe()
        {
            enabled = true;
        }

        public void DisableWireframe()
        {
            enabled = false;
        }

        private void OnDisable()
        {
            if (wireframeMesh != null)
            {
                Destroy(wireframeMesh);
            }
            if (wireframeMaterial != null)
            {
                Destroy(wireframeMaterial);
            }
        }
    }
}