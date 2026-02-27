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
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public const string ProBuilderMergeObjectsToolId = "probuilder-merge-objects";
        [McpPluginTool
        (
            ProBuilderMergeObjectsToolId,
            Title = "Merge multiple ProBuilder meshes into one",
            DestructiveHint = true
        )]
        [Description(@"Combines multiple ProBuilder meshes into a single mesh.
Useful for optimizing draw calls or creating a unified object from parts.
The first mesh in the list becomes the target that others merge into.

Example: Merge a table made of separate leg and top meshes into one object.")]
        public MergeObjectsResponse MergeObjects
        (
            [Description("Array of GameObject references with ProBuilderMesh components to merge. First object becomes the merge target.")]
            GameObjectRef[] gameObjectRefs,
            [Description("If true, delete the source GameObjects after merging (except the target). Default is true.")]
            bool deleteSourceObjects = true
        )
        {
            if (gameObjectRefs == null)
                throw new ArgumentNullException(nameof(gameObjectRefs));

            if (gameObjectRefs.Length < 2)
                throw new ArgumentException("At least 2 GameObjects are required for merging.", nameof(gameObjectRefs));

            foreach (var goRef in gameObjectRefs)
            {
                if (goRef == null)
                    throw new ArgumentNullException("One of the GameObject references is null.", nameof(gameObjectRefs));

                if (!goRef.IsValid(out var gameObjectValidationError))
                    throw new ArgumentException(gameObjectValidationError, nameof(gameObjectRefs));
            }

            return MainThread.Instance.Run(() =>
            {

                // Find all GameObjects and their ProBuilderMesh components
                var meshes = new List<ProBuilderMesh>();
                var gameObjects = new List<GameObject>();

                for (int i = 0; i < gameObjectRefs.Length; i++)
                {
                    var goRef = gameObjectRefs[i];

                    var go = goRef.FindGameObject(out var error);
                    if (error != null)
                        throw new Exception(error);

                    if (go == null)
                        throw new Exception($"GameObject not found at index {i}.");

                    var pbMesh = go.GetComponent<ProBuilderMesh>();
                    if (pbMesh == null)
                        throw new Exception($"GameObject '{go.name}' at index {i} does not have a ProBuilderMesh component.");

                    meshes.Add(pbMesh);
                    gameObjects.Add(go);
                }

                var targetMesh = meshes[0];
                var targetGo = gameObjects[0];
                var originalNames = gameObjects.Select(g => g.name).ToList();

                // Calculate totals before merge
                var totalFacesBefore = meshes.Sum(m => m.faceCount);
                var totalVerticesBefore = meshes.Sum(m => m.vertexCount);

                // Perform merge
                List<ProBuilderMesh>? resultMeshes = null;
                try
                {
                    resultMeshes = CombineMeshes.Combine(meshes, targetMesh);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to merge meshes: {ex.Message}");
                }

                if (resultMeshes == null || resultMeshes.Count == 0)
                {
                    throw new Exception("Merge failed - no meshes returned.");
                }

                // Delete source objects if requested (skip the target)
                var deletedCount = 0;
                if (deleteSourceObjects)
                {
                    for (int i = 1; i < gameObjects.Count; i++)
                    {
                        if (gameObjects[i] != null)
                        {
                            UnityEngine.Object.DestroyImmediate(gameObjects[i]);
                            deletedCount++;
                        }
                    }
                }

                // Rebuild mesh
                foreach (var mesh in resultMeshes)
                {
                    mesh.ToMesh();
                    mesh.Refresh();
                    EditorUtility.SetDirty(mesh);
                    EditorUtility.SetDirty(mesh.gameObject);
                }

                EditorUtils.RepaintAllEditorWindows();

                // Build source objects info for response
                var sourceObjects = new List<SourceObjectInfo>();
                for (int i = 0; i < originalNames.Count; i++)
                {
                    sourceObjects.Add(new SourceObjectInfo
                    {
                        index = i,
                        name = originalNames[i],
                        status = i == 0 ? "target" : (deleteSourceObjects ? "deleted" : "kept")
                    });
                }

                // Build additional meshes info if multiple were created
                List<AdditionalMeshInfo>? additionalMeshes = null;
                if (resultMeshes.Count > 1)
                {
                    additionalMeshes = new List<AdditionalMeshInfo>();
                    for (int i = 1; i < resultMeshes.Count; i++)
                    {
                        additionalMeshes.Add(new AdditionalMeshInfo
                        {
                            name = resultMeshes[i].gameObject.name,
                            instanceId = resultMeshes[i].gameObject.GetInstanceID()
                        });
                    }
                }

                return new MergeObjectsResponse
                {
                    mergedMeshCount = meshes.Count,
                    resultMeshCount = resultMeshes.Count,
                    targetObjectName = targetGo.name,
                    targetInstanceId = targetGo.GetInstanceID(),
                    objectsDeleted = deletedCount,
                    totalFacesBefore = totalFacesBefore,
                    totalFacesAfter = resultMeshes.Sum(m => m.faceCount),
                    totalVerticesBefore = totalVerticesBefore,
                    totalVerticesAfter = resultMeshes.Sum(m => m.vertexCount),
                    sourceObjects = sourceObjects,
                    additionalMeshes = additionalMeshes
                };
            });
        }

        #region MergeObjects Response Classes

        public class MergeObjectsResponse
        {
            public int mergedMeshCount;
            public int resultMeshCount;
            public string targetObjectName = string.Empty;
            public int targetInstanceId;
            public int objectsDeleted;
            public int totalFacesBefore;
            public int totalFacesAfter;
            public int totalVerticesBefore;
            public int totalVerticesAfter;
            public List<SourceObjectInfo>? sourceObjects;
            public List<AdditionalMeshInfo>? additionalMeshes;
        }

        public class SourceObjectInfo
        {
            public int index;
            public string name = string.Empty;
            public string status = string.Empty;
        }

        public class AdditionalMeshInfo
        {
            public string name = string.Empty;
            public int instanceId;
        }

        #endregion
    }
}
