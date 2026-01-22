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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public const string ProBuilderCreateShapeToolId = "probuilder-create-shape";
        [McpPluginTool
        (
            ProBuilderCreateShapeToolId,
            Title = "Create a ProBuilder shape"
        )]
        [Description(@"Creates a new ProBuilder mesh shape in the scene. ProBuilder shapes are editable 3D meshes
that can be modified using other ProBuilder tools like extrusion, beveling, etc.")]
        public CreateShapeResponse CreateShape
        (
            [Description("The type of shape to create.")]
            ShapeType shapeType,
            [Description("Name of the new GameObject.")]
            string? name = null,
            [Description("Parent GameObject reference. If not provided, the shape will be created at the root of the scene.")]
            GameObjectRef? parentGameObjectRef = null,
            [Description("Position of the shape in world or local space.")]
            Vector3? position = null,
            [Description("Rotation of the shape in euler angles (degrees).")]
            Vector3? rotation = null,
            [Description("Scale of the shape.")]
            Vector3? scale = null,
            [Description("Size of the shape (width, height, depth). Default is (1, 1, 1).")]
            Vector3? size = null,
            [Description("If true, position/rotation/scale are in local space relative to parent.")]
            bool isLocalSpace = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
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
                scale ??= Vector3.one;
                size ??= Vector3.one;

                // Create the ProBuilder shape
                var proBuilderMesh = ShapeGenerator.CreateShape(shapeType, PivotLocation.Center);

                if (proBuilderMesh == null)
                    throw new Exception($"Failed to create ProBuilder shape of type '{shapeType}'.");

                var go = proBuilderMesh.gameObject;
                go.name = name ?? $"ProBuilder {shapeType}";

                // Set parent
                if (parentGo != null)
                    go.transform.SetParent(parentGo.transform, false);

                // Apply transform
                if (isLocalSpace)
                {
                    go.transform.localPosition = position.Value;
                    go.transform.localEulerAngles = rotation.Value;
                    go.transform.localScale = scale.Value;
                }
                else
                {
                    go.transform.position = position.Value;
                    go.transform.eulerAngles = rotation.Value;
                    go.transform.localScale = scale.Value;
                }

                // Apply size by scaling vertices if size is different from default
                if (size.Value != Vector3.one)
                {
                    var positions = proBuilderMesh.positions;
                    var meshFilter = go.GetComponent<MeshFilter>();
                    var bounds = meshFilter != null ? meshFilter.sharedMesh.bounds : new Bounds();
                    var currentSize = bounds.size;

                    // Calculate scale factors
                    var scaleFactors = new Vector3(
                        currentSize.x > 0 ? size.Value.x / currentSize.x : 1,
                        currentSize.y > 0 ? size.Value.y / currentSize.y : 1,
                        currentSize.z > 0 ? size.Value.z / currentSize.z : 1
                    );

                    var posCount = positions.Count();
                    var newPositions = new Vector3[posCount];
                    for (int i = 0; i < posCount; i++)
                    {
                        newPositions[i] = Vector3.Scale(positions[i], scaleFactors);
                    }
                    proBuilderMesh.positions = newPositions;
                    proBuilderMesh.ToMesh();
                    proBuilderMesh.Refresh();
                }

                // Mark as dirty for saving
                EditorUtility.SetDirty(go);
                EditorUtils.RepaintAllEditorWindows();

                return new CreateShapeResponse
                {
                    gameObjectName = go.name,
                    instanceId = go.GetInstanceID(),
                    shapeType = shapeType.ToString(),
                    position = FormatVector3(go.transform.position),
                    rotation = FormatVector3(go.transform.eulerAngles),
                    scale = FormatVector3(go.transform.localScale),
                    faceCount = proBuilderMesh.faceCount,
                    vertexCount = proBuilderMesh.vertexCount,
                    edgeCount = proBuilderMesh.edgeCount
                };
            });
        }

        #region CreateShape Response Classes

        public class CreateShapeResponse
        {
            public string gameObjectName = string.Empty;
            public int instanceId;
            public string shapeType = string.Empty;
            public string position = string.Empty;
            public string rotation = string.Empty;
            public string scale = string.Empty;
            public int faceCount;
            public int vertexCount;
            public int edgeCount;
        }

        #endregion
    }
}
