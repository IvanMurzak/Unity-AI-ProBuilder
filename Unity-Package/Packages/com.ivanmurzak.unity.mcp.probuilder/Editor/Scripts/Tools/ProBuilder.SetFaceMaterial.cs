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
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public const string ProBuilderSetFaceMaterialToolId = "probuilder-set-face-material";
        [AiTool
        (
            ProBuilderSetFaceMaterialToolId,
            Title = "Set material on ProBuilder faces",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Assign a material to specific faces of a `ProBuilderMesh`, enabling " +
            "multi-material meshes (e.g., grass on top, dirt on sides). Supply either `faceIndices` (explicit) " +
            "or `faceDirection` (semantic); exactly one is required.")]
        [AiSkillBody("Assign a material to specific faces of a `ProBuilderMesh`, enabling multi-material " +
            "meshes where different faces have different materials (e.g., grass on top, dirt on the sides).\n\n" +
            "## Inputs\n\n" +
            "- `gameObjectRef` — the GameObject hosting the `ProBuilderMesh` component.\n" +
            "- `materialPath` — project path to the material asset (e.g., `Assets/Materials/Grass.mat`) or a " +
            "bare material name. Required.\n" +
            "- `faceIndices` — explicit array of face indices.\n" +
            "- `faceDirection` — semantic alternative (`Up`, `Down`, `Left`, `Right`, `Forward`, `Back`). " +
            "Exactly one of `faceIndices` / `faceDirection` is required.\n\n" +
            "## Examples\n\n" +
            "- Set material on the top face: `faceDirection=\"up\"`.\n" +
            "- Set material on specific faces: `faceIndices=[0, 2, 4]`.\n\n" +
            "## Behavior\n\n" +
            "The mesh is rebuilt (`ToMesh` → `Refresh`), dirty-flagged, and the Editor repaints. The whole call " +
            "runs on the Unity main thread.")]
        [Description(@"Assigns a material to specific faces of a ProBuilder mesh.
You can select faces by index OR by direction (semantic selection).
This enables multi-material meshes where different faces have different materials.

Examples:
- Set material on top face: faceDirection=""up""
- Set material on specific faces: faceIndices=[0, 2, 4]")]
        public SetFaceMaterialResponse SetFaceMaterial
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Path to the material asset (e.g., 'Assets/Materials/MyMaterial.mat') or material name.")]
            string materialPath,
            [Description("Array of face indices to apply the material to. Use this OR faceDirection, not both. Use ProBuilder_GetMeshInfo to get valid face indices.")]
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

                if (string.IsNullOrEmpty(materialPath))
                    throw new Exception("Material path is empty. Please provide a valid material path.");

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

                // Try to load the material
                Material? material = null;

                // First try as asset path
                if (materialPath.StartsWith("Assets/"))
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                }

                // If not found, try to find by name
                if (material == null)
                {
                    var guids = AssetDatabase.FindAssets($"t:Material {materialPath}");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    }
                }

                if (material == null)
                {
                    throw new Exception($"Material not found at path '{materialPath}'. Ensure the path is correct or the material exists in the project.");
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

                // Get current materials on the renderer
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    throw new Exception("No MeshRenderer found on the GameObject.");
                }

                var materials = renderer.sharedMaterials.ToList();

                // Find or add the material to the materials list
                var materialIndex = materials.IndexOf(material);
                if (materialIndex < 0)
                {
                    materialIndex = materials.Count;
                    materials.Add(material);
                    renderer.sharedMaterials = materials.ToArray();
                }

                // Assign the submesh index to the selected faces
                var selectedFaces = resolvedFaceIndices.Select(i => faces[i]).ToArray();
                foreach (var face in selectedFaces)
                {
                    face.submeshIndex = materialIndex;
                }

                // Rebuild mesh
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                // Mark as dirty
                EditorUtility.SetDirty(proBuilderMesh);
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                // Build materials list for response
                var meshMaterials = new List<MaterialInfo>();
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var mat = renderer.sharedMaterials[i];
                    meshMaterials.Add(new MaterialInfo
                    {
                        index = i,
                        name = mat != null ? mat.name : "null"
                    });
                }

                return new SetFaceMaterialResponse
                {
                    materialName = material.name,
                    materialIndex = materialIndex,
                    selectionMethod = selectionMethod,
                    facesUpdated = resolvedFaceIndices,
                    meshMaterials = meshMaterials
                };
            });
        }

        #region SetFaceMaterial Response Classes

        public class SetFaceMaterialResponse
        {
            public string materialName = string.Empty;
            public int materialIndex;
            public string selectionMethod = string.Empty;
            public int[]? facesUpdated;
            public List<MaterialInfo>? meshMaterials;
        }

        public class MaterialInfo
        {
            public int index;
            public string name = string.Empty;
        }

        #endregion
    }
}
#endif
