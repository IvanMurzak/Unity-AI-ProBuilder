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
        public const string ProBuilderDeleteFacesToolId = "probuilder-delete-faces";
        [AiTool
        (
            ProBuilderDeleteFacesToolId,
            Title = "Delete ProBuilder faces",
            ReadOnlyHint = false,
            DestructiveHint = true,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Delete selected faces from a `ProBuilderMesh`, creating holes or removing " +
            "geometry. Supply either `faceIndices` (explicit list) or `faceDirection` (semantic selection); " +
            "exactly one is required.")]
        [AiSkillBody("Delete selected faces from a `ProBuilderMesh`, creating holes or removing geometry " +
            "entirely. Faces can be selected explicitly by index or semantically by direction.\n\n" +
            "## Inputs\n\n" +
            "- `gameObjectRef` — the GameObject hosting the `ProBuilderMesh` component.\n" +
            "- `faceIndices` — explicit array of face indices to delete. Use " +
            "'" + ProBuilderGetMeshInfoToolId + "' to discover valid indices.\n" +
            "- `faceDirection` — semantic alternative (`Up`, `Down`, `Left`, `Right`, `Forward`, `Back`). " +
            "Exactly one of `faceIndices` / `faceDirection` is required.\n\n" +
            "## Examples\n\n" +
            "- Delete the bottom face: `faceDirection=\"down\"`.\n" +
            "- Delete specific faces: `faceIndices=[0, 2, 4]`.\n\n" +
            "## Behavior\n\n" +
            "The mesh is rebuilt (`ToMesh` → `Refresh`), the `ProBuilderMesh` and GameObject are marked dirty, " +
            "and Editor windows repaint. The whole call runs on the Unity main thread.")]
        [Description(@"Deletes selected faces from a ProBuilder mesh.
You can select faces by index OR by direction (semantic selection).
Deleting faces creates holes in the mesh or removes geometry entirely.

Examples:
- Delete bottom face: faceDirection=""down""
- Delete specific faces: faceIndices=[0, 2, 4]")]
        public DeleteFacesResponse DeleteFaces
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of face indices to delete. Use this OR faceDirection, not both. Use ProBuilder_GetMeshInfo to get valid face indices.")]
            int[]? faceIndices = null,
            [Description("Semantic face selection by direction. Use this OR faceIndices, not both.")]
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

                // Get unique face indices to handle duplicates
                var uniqueFaceIndices = resolvedFaceIndices.Distinct().ToArray();

                // Validate face indices
                var invalidIndices = uniqueFaceIndices.Where(i => i < 0 || i >= faceCount).ToList();
                if (invalidIndices.Any())
                {
                    throw new Exception($"Invalid face indices: {string.Join(", ", invalidIndices)}. Valid range: 0 to {faceCount - 1}.");
                }

                // Check if we're deleting all faces
                if (uniqueFaceIndices.Length >= faceCount)
                {
                    throw new Exception("Cannot delete all faces from a mesh. At least one face must remain.");
                }

                var originalFaceCount = proBuilderMesh.faceCount;
                var originalVertexCount = proBuilderMesh.vertexCount;

                // Get the faces to delete
                var facesToDelete = uniqueFaceIndices.Select(i => faces[i]).ToArray();

                // Perform deletion
                try
                {
                    proBuilderMesh.DeleteFaces(facesToDelete);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete faces: {ex.Message}");
                }

                // Rebuild mesh
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                // Mark as dirty
                EditorUtility.SetDirty(proBuilderMesh);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                return new DeleteFacesResponse
                {
                    deletedFaceCount = uniqueFaceIndices.Length,
                    selectionMethod = selectionMethod,
                    deletedFaceIndices = uniqueFaceIndices,
                    facesRemoved = originalFaceCount - proBuilderMesh.faceCount,
                    verticesRemoved = originalVertexCount - proBuilderMesh.vertexCount,
                    totalFaceCount = proBuilderMesh.faceCount,
                    totalVertexCount = proBuilderMesh.vertexCount,
                    totalEdgeCount = proBuilderMesh.edgeCount
                };
            });
        }

        #region DeleteFaces Response Classes

        public class DeleteFacesResponse
        {
            public int deletedFaceCount;
            public string selectionMethod = string.Empty;
            public int[]? deletedFaceIndices;
            public int facesRemoved;
            public int verticesRemoved;
            public int totalFaceCount;
            public int totalVertexCount;
            public int totalEdgeCount;
        }

        #endregion
    }
}
#endif
