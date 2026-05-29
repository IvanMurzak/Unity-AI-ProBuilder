/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Kieran Hannigan (https://github.com/KaiStarkk)          │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
#if !UNITY_6000_5_OR_NEWER

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public const string ProBuilderSubdivideEdgesToolId = "probuilder-subdivide-edges";
        [AiTool
        (
            ProBuilderSubdivideEdgesToolId,
            Title = "Subdivide edges in a ProBuilder mesh",
            Enabled = false,
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Insert new vertices on selected edges of a `ProBuilderMesh`, splitting each " +
            "edge into smaller segments. Supply either `edges` (explicit list) or `faceDirection` (subdivides " +
            "all edges of faces facing that direction); exactly one is required.")]
        [AiSkillBody("Insert new vertices on selected edges of a `ProBuilderMesh`, subdividing each into " +
            "smaller segments. Useful for adding detail to specific edges for further manipulation (extrude, " +
            "bevel, etc.).\n\n" +
            "## Inputs\n\n" +
            "- `gameObjectRef` — the GameObject hosting the `ProBuilderMesh` component.\n" +
            "- `edges` — explicit list of edges to subdivide, each as `[vertexA, vertexB]`. Use " +
            "'" + ProBuilderGetMeshInfoToolId + "' to discover valid edges.\n" +
            "- `faceDirection` — semantic alternative: subdivides all edges of faces pointing this direction " +
            "(`Up`, `Down`, `Left`, `Right`, `Forward`, `Back`). Exactly one of `edges` / `faceDirection` is " +
            "required.\n" +
            "- `subdivisions` — number of subdivisions per edge. `1` splits each edge in half, `2` into thirds, " +
            "and so on. Default `1`.\n\n" +
            "## Examples\n\n" +
            "- Subdivide all edges of the top face: `faceDirection=\"up\"`, `subdivisions=2`.\n" +
            "- Subdivide specific edges: `edges=[[0,1], [2,3]]`, `subdivisions=1`.")]
        [Description(@"Inserts new vertices on edges, subdividing them into smaller segments.
Useful for adding detail to specific edges for further manipulation.

Examples:
- Subdivide all edges of top face: faceDirection=""up"", subdivisions=2
- Subdivide specific edges: edges=[[0,1], [2,3]], subdivisions=1")]
        public SubdivideEdgesResponse SubdivideEdges
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of edge definitions. Each edge is [vertexA, vertexB]. Use ProBuilder_GetMeshInfo to get vertex indices.")]
            int[][]? edges = null,
            [Description("Semantic face selection - subdivide all edges of faces facing this direction.")]
            FaceDirection? faceDirection = null,
            [Description("Number of subdivisions per edge. 1 = splits edge in half, 2 = splits into thirds, etc. Default is 1.")]
            int subdivisions = 1
        )
        {
            if (gameObjectRef == null)
                throw new ArgumentNullException(nameof(gameObjectRef));

            if (!gameObjectRef.IsValid(out var gameObjectValidationError))
                throw new ArgumentException(gameObjectValidationError, nameof(gameObjectRef));

            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    throw new Exception(error);

                if (go == null)
                    throw new Exception(Error.GameObjectNotFound());

                var proBuilderMesh = go.GetComponent<ProBuilderMesh>();
                if (proBuilderMesh == null)
                    throw new Exception(Error.ProBuilderMeshNotFound(go.GetInstanceID()));

                if (subdivisions < 1)
                    throw new Exception("Subdivisions must be at least 1.");

                // Resolve edges from either direct indices or semantic direction
                List<Edge> edgesToSubdivide;
                string selectionMethod;

                if (edges != null && edges.Length > 0)
                {
                    // Validate edge definitions
                    foreach (var edge in edges)
                    {
                        if (edge == null || edge.Length < 2)
                            throw new Exception("Each edge must have exactly 2 vertex indices [vertexA, vertexB].");
                    }

                    edgesToSubdivide = edges.Select(e => new Edge(e[0], e[1])).ToList();
                    selectionMethod = "by vertex indices";
                }
                else if (faceDirection.HasValue)
                {
                    var selectedIndices = FaceSelectionHelper.SelectFacesByDirection(proBuilderMesh, faceDirection.Value, out var selectionError);
                    if (selectionError != null)
                        throw new Exception(selectionError);

                    // Get all edges from the selected faces
                    var faces = proBuilderMesh.faces;
                    edgesToSubdivide = new List<Edge>();
                    foreach (var faceIndex in selectedIndices!)
                    {
                        edgesToSubdivide.AddRange(faces[faceIndex].edges);
                    }
                    // Remove duplicates
                    edgesToSubdivide = edgesToSubdivide.Distinct().ToList();
                    selectionMethod = $"from faces facing '{faceDirection.Value}'";
                }
                else
                {
                    throw new Exception("Either edges or faceDirection must be provided.");
                }

                if (edgesToSubdivide.Count == 0)
                    throw new Exception("No edges found to subdivide.");

                var originalVertexCount = proBuilderMesh.vertexCount;
                var originalEdgeCount = proBuilderMesh.edgeCount;

                // Perform subdivision
                List<Edge>? newEdges = null;
                try
                {
                    newEdges = proBuilderMesh.AppendVerticesToEdge(edgesToSubdivide, subdivisions);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to subdivide edges: {ex.Message}");
                }

                if (newEdges == null || newEdges.Count == 0)
                {
                    throw new Exception("Subdivision failed - no new edges created. The edges may be invalid for this mesh.");
                }

                // Rebuild mesh
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                // Mark as dirty
                EditorUtility.SetDirty(proBuilderMesh);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                return new SubdivideEdgesResponse
                {
                    selectionMethod = selectionMethod,
                    edgesSubdivided = edgesToSubdivide.Count,
                    subdivisionsPerEdge = subdivisions,
                    newEdgesCreated = newEdges.Count,
                    vertexCountBefore = originalVertexCount,
                    vertexCountAfter = proBuilderMesh.vertexCount,
                    verticesAdded = proBuilderMesh.vertexCount - originalVertexCount,
                    edgeCountBefore = originalEdgeCount,
                    edgeCountAfter = proBuilderMesh.edgeCount,
                    edgesAdded = proBuilderMesh.edgeCount - originalEdgeCount,
                    totalFaceCount = proBuilderMesh.faceCount
                };
            });
        }

        #region SubdivideEdges Response Classes

        public class SubdivideEdgesResponse
        {
            public string selectionMethod = string.Empty;
            public int edgesSubdivided;
            public int subdivisionsPerEdge;
            public int newEdgesCreated;
            public int vertexCountBefore;
            public int vertexCountAfter;
            public int verticesAdded;
            public int edgeCountBefore;
            public int edgeCountAfter;
            public int edgesAdded;
            public int totalFaceCount;
        }

        #endregion
    }
}
#endif
