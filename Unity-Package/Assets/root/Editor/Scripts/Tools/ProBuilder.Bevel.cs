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
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public const string ProBuilderBevelToolId = "probuilder-bevel";
        [McpPluginTool
        (
            ProBuilderBevelToolId,
            Title = "Bevel ProBuilder edges",
            Enabled = false,
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [Description(@"Bevels selected edges of a ProBuilder mesh, creating chamfered corners.
Use ProBuilder_GetMeshInfo to identify edges by their vertex pairs.
Beveling replaces sharp edges with angled faces for a smoother appearance.")]
        public BevelResponse Bevel
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of edge definitions. Each edge is defined by two vertex indices [vertexA, vertexB]. Example: [[0,1], [2,3]] bevels edges from vertex 0 to 1 and from vertex 2 to 3.")]
            int[][] edges,
            [Description("Bevel amount from 0 (no bevel) to 1 (maximum bevel reaching face center). Recommended values: 0.05 to 0.2.")]
            float amount = 0.1f
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

                if (edges == null || edges.Length == 0)
                    throw new Exception(Error.NoEdgesProvided());

                // Validate and convert edges
                var edgeList = new List<Edge>();
                var vertexCount = proBuilderMesh.vertexCount;

                foreach (var edgeDef in edges)
                {
                    if (edgeDef == null || edgeDef.Length != 2)
                        throw new Exception("Each edge must be defined as an array of exactly 2 vertex indices. Example: [0, 1]");

                    var vertA = edgeDef[0];
                    var vertB = edgeDef[1];

                    if (vertA < 0 || vertA >= vertexCount)
                        throw new Exception($"Vertex index {vertA} is out of range. Valid range: 0 to {vertexCount - 1}.");
                    if (vertB < 0 || vertB >= vertexCount)
                        throw new Exception($"Vertex index {vertB} is out of range. Valid range: 0 to {vertexCount - 1}.");

                    edgeList.Add(new Edge(vertA, vertB));
                }

                // Clamp amount to valid range
                amount = Mathf.Clamp(amount, 0.001f, 0.999f);

                // Perform bevel
                List<Face>? newFaces = null;
                try
                {
                    newFaces = UnityEngine.ProBuilder.MeshOperations.Bevel.BevelEdges(proBuilderMesh, edgeList, amount);
                }
                catch (Exception ex)
                {
                    throw new Exception(Error.BevelFailed(ex.Message));
                }

                if (newFaces == null || newFaces.Count == 0)
                {
                    throw new Exception(Error.BevelFailed("No new faces were created. The edges may not be valid for beveling or may already be at maximum bevel."));
                }

                // Rebuild mesh
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                // Mark as dirty
                EditorUtility.SetDirty(proBuilderMesh);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                return new BevelResponse
                {
                    edgesBeveled = edgeList.Count,
                    bevelAmount = amount,
                    newFacesCreated = newFaces.Count,
                    totalFaceCount = proBuilderMesh.faceCount,
                    totalVertexCount = proBuilderMesh.vertexCount,
                    totalEdgeCount = proBuilderMesh.edgeCount
                };
            });
        }

        #region Bevel Response Classes

        public class BevelResponse
        {
            public int edgesBeveled;
            public float bevelAmount;
            public int newFacesCreated;
            public int totalFaceCount;
            public int totalVertexCount;
            public int totalEdgeCount;
        }

        #endregion
    }
}
