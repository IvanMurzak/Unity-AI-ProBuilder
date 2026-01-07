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

using System;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        [McpPluginTool
        (
            "probuilder-bridge",
            Title = "Bridge two edges in a ProBuilder mesh"
        )]
        [Description(@"Creates a new face connecting two edges.
Useful for connecting separate parts of geometry or filling gaps.

Example:
- edgeA=[0,1], edgeB=[4,5] creates a quad face between the two edges")]
        public BridgeResponse Bridge
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("First edge as [vertexA, vertexB].")]
            int[] edgeA,
            [Description("Second edge as [vertexA, vertexB].")]
            int[] edgeB,
            [Description("If true, allows creation of non-manifold geometry (edges shared by more than 2 faces).")]
            bool allowNonManifold = false
        )
        => MainThread.Instance.Run(() =>
        {
            if (gameObjectRef?.IsValid != true)
                throw new Exception("Invalid GameObject reference provided.");

            var go = gameObjectRef.FindGameObject(out var error);
            if (error != null)
                throw new Exception(error);

            if (go == null)
                throw new Exception(Error.GameObjectNotFound());

            var proBuilderMesh = go.GetComponent<ProBuilderMesh>();
            if (proBuilderMesh == null)
                throw new Exception(Error.ProBuilderMeshNotFound(go.GetInstanceID()));

            // Validate edges
            if (edgeA == null || edgeA.Length < 2)
                throw new Exception("edgeA must have exactly 2 vertex indices [vertexA, vertexB].");
            if (edgeB == null || edgeB.Length < 2)
                throw new Exception("edgeB must have exactly 2 vertex indices [vertexA, vertexB].");

            var edge1 = new Edge(edgeA[0], edgeA[1]);
            var edge2 = new Edge(edgeB[0], edgeB[1]);

            var originalFaceCount = proBuilderMesh.faceCount;

            // Perform bridge
            Face? newFace = null;
            try
            {
                newFace = proBuilderMesh.Bridge(edge1, edge2, allowNonManifold);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to bridge edges: {ex.Message}");
            }

            if (newFace == null)
            {
                throw new Exception("Bridge failed - could not create face between the specified edges. Ensure the edges are valid and not already connected.");
            }

            // Rebuild mesh
            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            // Mark as dirty
            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            // Find new face index
            var faces = proBuilderMesh.faces;
            var newFaceIndex = -1;
            for (int i = 0; i < faces.Count; i++)
            {
                if (faces[i] == newFace)
                {
                    newFaceIndex = i;
                    break;
                }
            }

            return new BridgeResponse
            {
                edgeA = new int[] { edgeA[0], edgeA[1] },
                edgeB = new int[] { edgeB[0], edgeB[1] },
                newFaceIndex = newFaceIndex,
                allowNonManifold = allowNonManifold,
                faceCountBefore = originalFaceCount,
                faceCountAfter = proBuilderMesh.faceCount,
                facesAdded = proBuilderMesh.faceCount - originalFaceCount,
                totalVertexCount = proBuilderMesh.vertexCount,
                totalEdgeCount = proBuilderMesh.edgeCount
            };
        });

        #region Bridge Response Classes

        public class BridgeResponse
        {
            public int[]? edgeA;
            public int[]? edgeB;
            public int newFaceIndex;
            public bool allowNonManifold;
            public int faceCountBefore;
            public int faceCountAfter;
            public int facesAdded;
            public int totalVertexCount;
            public int totalEdgeCount;
        }

        #endregion
    }
}
