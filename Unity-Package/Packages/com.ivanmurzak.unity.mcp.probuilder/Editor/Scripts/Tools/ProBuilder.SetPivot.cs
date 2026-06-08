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
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// Pivot location options for SetPivot tool.
    /// </summary>
    public enum MeshPivotLocation
    {
        /// <summary>Center of mesh bounds</summary>
        Center,
        /// <summary>First vertex position</summary>
        FirstVertex,
        /// <summary>Custom world position</summary>
        Custom
    }

    public partial class Tool_ProBuilder
    {
        public const string ProBuilderSetPivotToolId = "probuilder-set-pivot";
        [AiTool
        (
            ProBuilderSetPivotToolId,
            Title = "Set the pivot point of a ProBuilder mesh",
            Enabled = false,
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Move the pivot (origin) of a `ProBuilderMesh` without shifting the visible " +
            "geometry. Choose `Center` (mesh bounds), `FirstVertex`, or `Custom` (world-space position). The " +
            "mesh data is rebaked so the visual position stays fixed.")]
        [AiSkillBody("Move the pivot (origin) of a `ProBuilderMesh` without shifting the visible " +
            "geometry. The mesh data is rebaked so the visual position stays fixed while the GameObject's " +
            "transform origin moves to the new pivot.\n\n" +
            "## Inputs\n\n" +
            "- `gameObjectRef` — the GameObject hosting the `ProBuilderMesh` component.\n" +
            "- `pivotLocation` — `MeshPivotLocation` enum: `Center` (mesh bounds center), `FirstVertex`, or " +
            "`Custom`.\n" +
            "- `customPosition` — required when `pivotLocation = Custom`. World-space pivot position.\n\n" +
            "## Examples\n\n" +
            "- Center the pivot: `pivotLocation=Center`.\n" +
            "- Set pivot to first vertex: `pivotLocation=FirstVertex`.\n" +
            "- Set custom pivot: `pivotLocation=Custom`, `customPosition=(0, 0, 0)`.\n\n" +
            "## Behavior\n\n" +
            "The mesh is rebuilt (`ToMesh` → `Refresh`), dirty-flagged, and the Editor repaints. The whole call " +
            "runs on the Unity main thread.")]
        [Description(@"Changes the pivot (origin) point of a ProBuilder mesh.
The mesh geometry is adjusted so the pivot moves without changing the visual position.

Examples:
- Center the pivot: pivotLocation=Center
- Set pivot to first vertex: pivotLocation=FirstVertex
- Set custom pivot: pivotLocation=Custom, customPosition=(0, 0, 0)")]
        public SetPivotResponse SetPivot
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Where to place the pivot.")]
            MeshPivotLocation pivotLocation = MeshPivotLocation.Center,
            [Description("Custom world position for pivot (only used when pivotLocation=Custom).")]
            Vector3? customPosition = null
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

                var oldPivot = go.transform.position;
                Vector3 newPivotWorld;

                switch (pivotLocation)
                {
                    case MeshPivotLocation.Center:
                        // Get mesh bounds center in world space
                        var meshFilter = go.GetComponent<MeshFilter>();
                        if (meshFilter == null || meshFilter.sharedMesh == null)
                            throw new Exception("No mesh found on GameObject.");
                        var boundsCenter = meshFilter.sharedMesh.bounds.center;
                        newPivotWorld = go.transform.TransformPoint(boundsCenter);
                        break;

                    case MeshPivotLocation.FirstVertex:
                        var positions = proBuilderMesh.positions;
                        if (positions == null || positions.Count == 0)
                            throw new Exception("Mesh has no vertices.");
                        newPivotWorld = go.transform.TransformPoint(positions[0]);
                        break;

                    case MeshPivotLocation.Custom:
                        if (!customPosition.HasValue)
                            throw new Exception("customPosition is required when pivotLocation is Custom.");
                        newPivotWorld = customPosition.Value;
                        break;

                    default:
                        throw new Exception($"Unknown pivot location: {pivotLocation}");
                }

                // Calculate offset in local space
                var offset = go.transform.InverseTransformPoint(newPivotWorld);

                try
                {
                    // Move all vertices by the negative offset
                    var vertexPositions = proBuilderMesh.positions.ToArray();
                    for (int i = 0; i < vertexPositions.Length; i++)
                    {
                        vertexPositions[i] -= offset;
                    }
                    proBuilderMesh.positions = vertexPositions;

                    // Move the transform to compensate
                    go.transform.position = newPivotWorld;

                    // Rebuild mesh
                    proBuilderMesh.ToMesh();
                    proBuilderMesh.Refresh();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to set pivot: {ex.Message}");
                }

                // Mark as dirty
                EditorUtility.SetDirty(proBuilderMesh);
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                return new SetPivotResponse
                {
                    pivotLocation = pivotLocation.ToString(),
                    oldPivot = FormatVector3(oldPivot),
                    newPivot = FormatVector3(newPivotWorld),
                    offsetApplied = FormatVector3(offset),
                    gameObjectName = go.name,
                    newPosition = FormatVector3(go.transform.position)
                };
            });
        }

        #region SetPivot Response Classes

        public class SetPivotResponse
        {
            public string pivotLocation = string.Empty;
            public string oldPivot = string.Empty;
            public string newPivot = string.Empty;
            public string offsetApplied = string.Empty;
            public string gameObjectName = string.Empty;
            public string newPosition = string.Empty;
        }

        #endregion
    }
}
#endif
