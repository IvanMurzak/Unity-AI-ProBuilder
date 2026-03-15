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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public const string ProBuilderExtrudeToolId = "probuilder-extrude";
        [McpPluginTool
        (
            ProBuilderExtrudeToolId,
            Title = "Extrude ProBuilder faces",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [Description(@"Extrudes selected faces of a ProBuilder mesh along their normals.
You can select faces by index OR by direction (semantic selection).
Extrusion creates new geometry by pushing faces outward (or inward with negative distance).

Examples:
- Extrude top face: faceDirection=""up""
- Extrude specific faces: faceIndices=[0, 2, 4]")]
        public ExtrudeResponse Extrude
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of face indices to extrude. Use this OR faceDirection, not both. Use ProBuilder_GetMeshInfo to get valid face indices.")]
            int[]? faceIndices = null,
            [Description("Semantic face selection by direction. Use this OR faceIndices, not both.")]
            FaceDirection? faceDirection = null,
            [Description("Distance to extrude the faces. Positive values extrude outward along face normals, negative values extrude inward.")]
            float distance = 0.5f,
            [Description("Extrusion method: IndividualFaces (each face extrudes independently), FaceNormal (faces extrude as a group along averaged normal), VertexNormal (vertices move along their normals).")]
            ExtrudeMethod extrudeMethod = ExtrudeMethod.FaceNormal
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

                // Resolve face indices from either direct indices or semantic direction
                int[] resolvedFaceIndices;
                string selectionMethod;

                if (faceIndices != null && faceIndices.Length > 0)
                {
                    resolvedFaceIndices = faceIndices;
                    selectionMethod = "by index";
                }
                else if (faceDirection.HasValue)
                {
                    var selectedIndices = FaceSelectionHelper.SelectFacesByDirection(proBuilderMesh, faceDirection.Value, out var selectionError);
                    if (selectionError != null)
                        throw new Exception(selectionError);
                    resolvedFaceIndices = selectedIndices!;
                    selectionMethod = $"by direction '{faceDirection.Value}'";
                }
                else
                {
                    throw new Exception("Either faceIndices or faceDirection must be provided.");
                }

                var faces = proBuilderMesh.faces;
                var faceCount = faces.Count();
                if (faceCount == 0)
                    throw new Exception(Error.MeshHasNoFaces());

                // Validate face indices
                var invalidIndices = resolvedFaceIndices.Where(i => i < 0 || i >= faceCount).ToList();
                if (invalidIndices.Any())
                {
                    throw new Exception($"Invalid face indices: {string.Join(", ", invalidIndices)}. Valid range: 0 to {faceCount - 1}.");
                }

                // Get the faces to extrude
                var facesToExtrude = resolvedFaceIndices.Select(i => faces[i]).ToArray();

                // Perform extrusion
                Face[]? newFaces = null;
                try
                {
                    newFaces = proBuilderMesh.Extrude(facesToExtrude, extrudeMethod, distance);
                }
                catch (Exception ex)
                {
                    throw new Exception(Error.ExtrusionFailed(ex.Message));
                }

                if (newFaces == null || newFaces.Length == 0)
                {
                    throw new Exception(Error.ExtrusionFailed("No new faces were created. The operation may not be valid for this mesh configuration."));
                }

                // Rebuild mesh
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                // Mark as dirty
                EditorUtility.SetDirty(proBuilderMesh);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                // Find new face indices
                var allFaces = proBuilderMesh.faces;
                var newFaceIndices = new List<int>();
                var allFaceCount = allFaces.Count();
                for (int i = 0; i < allFaceCount; i++)
                {
                    if (newFaces.Contains(allFaces[i]))
                        newFaceIndices.Add(i);
                }

                return new ExtrudeResponse
                {
                    extrudedFaceCount = facesToExtrude.Length,
                    selectionMethod = selectionMethod,
                    extrudedFaceIndices = resolvedFaceIndices,
                    extrudeMethod = extrudeMethod.ToString(),
                    distance = distance,
                    newFacesCreated = newFaces.Length,
                    newFaceIndices = newFaceIndices.ToArray(),
                    totalFaceCount = proBuilderMesh.faceCount,
                    totalVertexCount = proBuilderMesh.vertexCount,
                    totalEdgeCount = proBuilderMesh.edgeCount
                };
            });
        }

        #region Extrude Response Classes

        public class ExtrudeResponse
        {
            public int extrudedFaceCount;
            public string selectionMethod = string.Empty;
            public int[]? extrudedFaceIndices;
            public string extrudeMethod = string.Empty;
            public float distance;
            public int newFacesCreated;
            public int[]? newFaceIndices;
            public int totalFaceCount;
            public int totalVertexCount;
            public int totalEdgeCount;
        }

        #endregion
    }
}
