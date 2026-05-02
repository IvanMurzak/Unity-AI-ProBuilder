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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public const string ProBuilderFlipNormalsToolId = "probuilder-flip-normals";
        [McpPluginTool
        (
            ProBuilderFlipNormalsToolId,
            Title = "Flip face normals in a ProBuilder mesh",
            Enabled = false,
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [Description(@"Reverses the normal direction of selected faces, flipping them inside-out.
Useful for creating interior spaces or fixing inverted faces.

Examples:
- Flip all faces: leave faceIndices and faceDirection empty
- Flip top face only: faceDirection=Up
- Flip specific faces: faceIndices=[0, 2, 4]")]
        public FlipNormalsResponse FlipNormals
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of face indices to flip. If empty and faceDirection is empty, flips all faces.")]
            int[]? faceIndices = null,
            [Description("Semantic face selection by direction. If empty and faceIndices is empty, flips all faces.")]
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
                    throw new Exception(Error.ProBuilderMeshNotFound(go.GetInstanceID()));

                var faces = proBuilderMesh.faces;
                var faceCount = faces.Count();
                if (faceCount == 0)
                    throw new Exception(Error.MeshHasNoFaces());

                // Resolve face indices
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
                    // Flip all faces
                    resolvedFaceIndices = Enumerable.Range(0, faceCount).ToArray();
                    selectionMethod = "all faces";
                }

                // Validate face indices
                var invalidIndices = resolvedFaceIndices.Where(i => i < 0 || i >= faceCount).ToList();
                if (invalidIndices.Any())
                {
                    throw new Exception($"Invalid face indices: {string.Join(", ", invalidIndices)}. Valid range: 0 to {faceCount - 1}.");
                }

                // Get faces to flip
                var facesToFlip = resolvedFaceIndices.Select(i => faces[i]).ToArray();

                // Flip normals by reversing the faces
                try
                {
                    foreach (var face in facesToFlip)
                    {
                        face.Reverse();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to flip normals: {ex.Message}");
                }

                // Rebuild mesh
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                // Mark as dirty
                EditorUtility.SetDirty(proBuilderMesh);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                return new FlipNormalsResponse
                {
                    facesFlipped = resolvedFaceIndices.Length,
                    selectionMethod = selectionMethod,
                    faceIndices = resolvedFaceIndices.Length <= 20 ? resolvedFaceIndices : null,
                    totalFaceCount = proBuilderMesh.faceCount,
                    totalVertexCount = proBuilderMesh.vertexCount
                };
            });
        }

        #region FlipNormals Response Classes

        public class FlipNormalsResponse
        {
            public int facesFlipped;
            public string selectionMethod = string.Empty;
            public int[]? faceIndices;
            public int totalFaceCount;
            public int totalVertexCount;
        }

        #endregion
    }
}
