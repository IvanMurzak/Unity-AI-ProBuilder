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
#if UNITY_6000_5_OR_NEWER

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
        public const string ProBuilderConnectEdgesToolId = "probuilder-connect-edges";
        [AiTool
        (
            ProBuilderConnectEdgesToolId,
            Title = "Connect edges in a ProBuilder mesh",
            Enabled = false,
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Insert new edges connecting the midpoints of selected edges within faces of a " +
            "`ProBuilderMesh` — adds edge loops and extra geometry detail. Supply either `edges` (explicit list) " +
            "or `faceDirection` (semantic selection); exactly one is required.")]
        [AiSkillBody("Insert new edges connecting the midpoints of selected edges within faces of a " +
            "`ProBuilderMesh`. When a face has more than two edges to connect, a center vertex is added. " +
            "Useful for creating new edge loops and adding geometry detail.\n\n" +
            "## Inputs\n\n" +
            "- `gameObjectRef` — the GameObject hosting the `ProBuilderMesh` component.\n" +
            "- `edges` — explicit list of edges to connect, each as `[vertexA, vertexB]`. Use " +
            "'" + ProBuilderGetMeshInfoToolId + "' to discover valid indices.\n" +
            "- `faceDirection` — semantic alternative: connects all edges of faces pointing this direction " +
            "(`Up`, `Down`, `Left`, `Right`, `Forward`, `Back`). Exactly one of `edges` / `faceDirection` is " +
            "required.\n\n" +
            "## Examples\n\n" +
            "- Connect opposite edges of the top face: `faceDirection=\"up\"`.\n" +
            "- Connect specific edges: `edges=[[0,1], [2,3]]`.")]
        [Description(@"Inserts new edges connecting the midpoints of selected edges within faces.
If a face has more than 2 edges to connect, a center vertex is added.
This is useful for creating new edge loops and adding geometry detail.

Examples:
- Connect opposite edges of top face: faceDirection=""up""
- Connect specific edges: edges=[[0,1], [2,3]]")]
        public ConnectEdgesResponse ConnectEdges
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of edge definitions. Each edge is [vertexA, vertexB]. Use ProBuilder_GetMeshInfo to get vertex indices.")]
            int[][]? edges = null,
            [Description("Semantic face selection - connect edges of faces facing this direction.")]
            FaceDirection? faceDirection = null
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
                    throw new Exception(Error.ProBuilderMeshNotFound(go.GetEntityId()));

                // Resolve edges from either direct indices or semantic direction
                List<Edge> edgesToConnect;
                string selectionMethod;

                if (edges != null && edges.Length > 0)
                {
                    // Validate edge definitions
                    foreach (var edge in edges)
                    {
                        if (edge == null || edge.Length < 2)
                            throw new Exception("Each edge must have exactly 2 vertex indices [vertexA, vertexB].");
                    }

                    edgesToConnect = edges.Select(e => new Edge(e[0], e[1])).ToList();
                    selectionMethod = "by vertex indices";
                }
                else if (faceDirection.HasValue)
                {
                    var selectedIndices = FaceSelectionHelper.SelectFacesByDirection(proBuilderMesh, faceDirection.Value, out var selectionError);
                    if (selectionError != null)
                        throw new Exception(selectionError);

                    // Get all edges from the selected faces
                    var faces = proBuilderMesh.faces;
                    edgesToConnect = new List<Edge>();
                    foreach (var faceIndex in selectedIndices!)
                    {
                        edgesToConnect.AddRange(faces[faceIndex].edges);
                    }
                    // Remove duplicates
                    edgesToConnect = edgesToConnect.Distinct().ToList();
                    selectionMethod = $"from faces facing '{faceDirection.Value}'";
                }
                else
                {
                    throw new Exception("Either edges or faceDirection must be provided.");
                }

                if (edgesToConnect.Count < 2)
                    throw new Exception("At least 2 edges are required for connection.");

                var originalFaceCount = proBuilderMesh.faceCount;
                var originalEdgeCount = proBuilderMesh.edgeCount;

                // Perform connection
                Face[]? newFaces = null;
                Edge[]? newEdges = null;
                try
                {
                    var result = ConnectElements.Connect(proBuilderMesh, edgesToConnect);
                    newFaces = result.item1;
                    newEdges = result.item2;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to connect edges: {ex.Message}");
                }

                if ((newFaces == null || newFaces.Length == 0) && (newEdges == null || newEdges.Length == 0))
                {
                    throw new Exception("Connection failed - no new geometry created. The edges may not be suitable for connection.");
                }

                // Rebuild mesh
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                // Mark as dirty
                EditorUtility.SetDirty(proBuilderMesh);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                return new ConnectEdgesResponse
                {
                    selectionMethod = selectionMethod,
                    edgesConnected = edgesToConnect.Count,
                    newFacesCreated = newFaces?.Length ?? 0,
                    newEdgesCreated = newEdges?.Length ?? 0,
                    faceCountBefore = originalFaceCount,
                    faceCountAfter = proBuilderMesh.faceCount,
                    facesAdded = proBuilderMesh.faceCount - originalFaceCount,
                    edgeCountBefore = originalEdgeCount,
                    edgeCountAfter = proBuilderMesh.edgeCount,
                    edgesAdded = proBuilderMesh.edgeCount - originalEdgeCount,
                    totalVertexCount = proBuilderMesh.vertexCount
                };
            });
        }

        #region ConnectEdges Response Classes

        public class ConnectEdgesResponse
        {
            public string selectionMethod = string.Empty;
            public int edgesConnected;
            public int newFacesCreated;
            public int newEdgesCreated;
            public int faceCountBefore;
            public int faceCountAfter;
            public int facesAdded;
            public int edgeCountBefore;
            public int edgeCountAfter;
            public int edgesAdded;
            public int totalVertexCount;
        }

        #endregion
    }
}
#endif
