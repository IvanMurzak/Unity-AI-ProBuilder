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
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public const string ProBuilderCreatePolyShapeToolId = "probuilder-create-poly-shape";
        [McpPluginTool
        (
            ProBuilderCreatePolyShapeToolId,
            Title = "Create a ProBuilder shape from polygon points",
            Enabled = false,
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [Description(@"Creates a 3D mesh from a 2D polygon outline. Perfect for:
- Floor plans and room layouts
- Custom terrain patches
- Architectural elements (walls, platforms)
- Any shape that can be defined by a 2D outline

The polygon is defined by an array of 2D points (x,z coordinates) that form the outline.
The shape is then extruded upward by the specified height.

Examples:
- Rectangle: points=[[0,0], [4,0], [4,3], [0,3]] height=2.5
- L-shape: points=[[0,0], [3,0], [3,2], [1,2], [1,3], [0,3]] height=3
- Triangle: points=[[0,0], [2,0], [1,1.7]] height=1")]
        public CreatePolyShapeResponse CreatePolyShape
        (
            [Description("2D polygon points as [x,z] coordinates. Minimum 3 points. Points should be in clockwise or counter-clockwise order. Example: [[0,0], [4,0], [4,3], [0,3]] creates a 4x3 rectangle.")]
            float[][] points,
            [Description("Height to extrude the polygon upward. Default is 1.")]
            float height = 1f,
            [Description("Name of the new GameObject.")]
            string? name = null,
            [Description("Parent GameObject reference. If not provided, the shape will be created at the root of the scene.")]
            GameObjectRef? parentGameObjectRef = null,
            [Description("Position of the shape in world or local space.")]
            Vector3? position = null,
            [Description("Rotation of the shape in euler angles (degrees).")]
            Vector3? rotation = null,
            [Description("If true, flip the normals so the faces point inward instead of outward.")]
            bool flipNormals = false,
            [Description("If true, position/rotation are in local space relative to parent.")]
            bool isLocalSpace = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
                // Validate points
                if (points == null || points.Length < 3)
                    throw new Exception("At least 3 polygon points are required to create a shape.");

                // Validate each point has x,z coordinates
                for (int i = 0; i < points.Length; i++)
                {
                    if (points[i] == null || points[i].Length < 2)
                        throw new Exception($"Point at index {i} must have at least 2 coordinates [x,z].");
                }

                // Find parent if provided
                GameObject? parentGo = null;
                if (parentGameObjectRef?.IsValid(out _) == true)
                {
                    parentGo = parentGameObjectRef.FindGameObject(out var error);
                    if (error != null)
                        throw new Exception(error);
                }

                // Set defaults
                position ??= Vector3.zero;
                rotation ??= Vector3.zero;

                // Convert 2D points to 3D (x,z -> x,0,z)
                var points3D = new Vector3[points.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    points3D[i] = new Vector3(points[i][0], 0f, points[i][1]);
                }

                // Create the ProBuilder mesh
                var go = new GameObject();
                var proBuilderMesh = go.AddComponent<ProBuilderMesh>();

                if (proBuilderMesh == null)
                    throw new Exception("Failed to create ProBuilderMesh component.");

                // Create the shape from polygon
                try
                {
                    var result = proBuilderMesh.CreateShapeFromPolygon(points3D, height, flipNormals);
                    if (result.status != ActionResult.Status.Success)
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                        throw new Exception($"Failed to create polygon shape: {result.notification}");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    throw new Exception($"Failed to create polygon shape: {ex.Message}");
                }

                go.name = name ?? "ProBuilder PolyShape";

                // Set parent
                if (parentGo != null)
                    go.transform.SetParent(parentGo.transform, false);

                // Apply transform
                if (isLocalSpace)
                {
                    go.transform.localPosition = position.Value;
                    go.transform.localEulerAngles = rotation.Value;
                }
                else
                {
                    go.transform.position = position.Value;
                    go.transform.eulerAngles = rotation.Value;
                }

                // Mark as dirty for saving
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                // Calculate bounds for info
                var meshFilter = go.GetComponent<MeshFilter>();
                var bounds = meshFilter != null && meshFilter.sharedMesh != null
                    ? meshFilter.sharedMesh.bounds
                    : new Bounds();

                // Build input points for response
                var inputPoints = new List<PointInfo>();
                for (int i = 0; i < points.Length; i++)
                {
                    inputPoints.Add(new PointInfo
                    {
                        index = i,
                        x = points[i][0],
                        z = points[i][1]
                    });
                }

                return new CreatePolyShapeResponse
                {
                    gameObjectName = go.name,
                    instanceId = go.GetInstanceID(),
                    position = FormatVector3(go.transform.position),
                    rotation = FormatVector3(go.transform.eulerAngles),
                    pointCount = points.Length,
                    height = height,
                    flipNormals = flipNormals,
                    boundsSize = FormatVector3(bounds.size),
                    faceCount = proBuilderMesh.faceCount,
                    vertexCount = proBuilderMesh.vertexCount,
                    edgeCount = proBuilderMesh.edgeCount,
                    inputPoints = inputPoints
                };
            });
        }

        #region CreatePolyShape Response Classes

        public class CreatePolyShapeResponse
        {
            public string gameObjectName = string.Empty;
            public int instanceId;
            public string position = string.Empty;
            public string rotation = string.Empty;
            public int pointCount;
            public float height;
            public bool flipNormals;
            public string boundsSize = string.Empty;
            public int faceCount;
            public int vertexCount;
            public int edgeCount;
            public List<PointInfo>? inputPoints;
        }

        public class PointInfo
        {
            public int index;
            public float x;
            public float z;
        }

        #endregion
    }
}
