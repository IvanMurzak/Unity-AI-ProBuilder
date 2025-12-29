/*
Copyright (c) 2025 Ivan Murzak
Licensed under the Apache License, Version 2.0.
See the LICENSE file in the project root for more information.
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        public enum UvProjectionMode
        {
            [Description("Automatically unwrap faces")]
            Auto,
            [Description("Box projection")]
            Box,
            [Description("Planar projection")]
            Planar
        }

        [McpPluginTool(
            "probuilder-generate-uvs",
            Title = "Generate UVs for ProBuilder mesh"
        )]
        [Description(@"Generates UVs for a ProBuilder mesh using auto/box/planar projection.
You can target specific faces by index or direction.")]
        public string GenerateUVs(
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Array of face indices to unwrap. Use this OR faceDirection, not both.")]
            int[]? faceIndices = null,
            [Description("Semantic face selection by direction. Use this OR faceIndices, not both.")]
            FaceDirection? faceDirection = null,
            [Description("UV projection mode.")]
            UvProjectionMode projection = UvProjectionMode.Auto,
            [Description("If true, uses all faces when no faceIndices or faceDirection is provided.")]
            bool applyToAllFacesIfNoneProvided = true
        )
        => MainThread.Instance.Run(() =>
        {
            if (gameObjectRef?.IsValid != true)
                return "[Error] Invalid GameObject reference provided.";

            var go = gameObjectRef.FindGameObject(out var error);
            if (error != null)
                return $"[Error] {error}";

            if (go == null)
                return Error.GameObjectNotFound();

            var proBuilderMesh = go.GetComponent<ProBuilderMesh>();
            if (proBuilderMesh == null)
                return Error.ProBuilderMeshNotFound(go.GetInstanceID());

            var faces = proBuilderMesh.faces;
            var faceCount = faces.Count();
            if (faceCount == 0)
                return Error.MeshHasNoFaces();

            List<Face> facesToUnwrap;
            string selectionMethod;

            if (faceIndices != null && faceIndices.Length > 0)
            {
                var invalidIndices = faceIndices.Where(i => i < 0 || i >= faceCount).ToList();
                if (invalidIndices.Any())
                    return $"[Error] Invalid face indices: {string.Join(", ", invalidIndices)}. Valid range: 0 to {faceCount - 1}.";

                facesToUnwrap = faceIndices.Select(i => faces[i]).ToList();
                selectionMethod = "by index";
            }
            else if (faceDirection.HasValue)
            {
                var selectedIndices = FaceSelectionHelper.SelectFacesByDirection(proBuilderMesh, faceDirection.Value, out var selectionError);
                if (selectionError != null)
                    return $"[Error] {selectionError}";

                facesToUnwrap = selectedIndices!.Select(i => faces[i]).ToList();
                selectionMethod = $"by direction '{faceDirection.Value}'";
            }
            else if (applyToAllFacesIfNoneProvided)
            {
                facesToUnwrap = faces.ToList();
                selectionMethod = "all faces";
            }
            else
            {
                return "[Error] Either faceIndices or faceDirection must be provided.";
            }

            if (!TryProjectUvs(proBuilderMesh, facesToUnwrap, projection, out var unwrapError))
                return $"[Error] {unwrapError}";

            proBuilderMesh.ToMesh();
            proBuilderMesh.Refresh();

            EditorUtility.SetDirty(proBuilderMesh);
            EditorUtility.SetDirty(go);

            var sb = new StringBuilder();
            sb.AppendLine($"[Success] Generated UVs for {facesToUnwrap.Count} face(s) {selectionMethod}.");
            sb.AppendLine();
            sb.AppendLine("# Result:");
            sb.AppendLine($"- Projection: {projection}");
            sb.AppendLine($"- Face Selection: {selectionMethod}");
            sb.AppendLine($"- Face Count: {facesToUnwrap.Count}");
            sb.AppendLine();
            sb.AppendLine("# Updated Mesh Info:");
            sb.AppendLine($"- Total Face Count: {proBuilderMesh.faceCount}");
            sb.AppendLine($"- Total Vertex Count: {proBuilderMesh.vertexCount}");
            sb.AppendLine($"- Total Edge Count: {proBuilderMesh.edgeCount}");

            return sb.ToString();
        });

        private static bool TryProjectUvs(ProBuilderMesh mesh, IList<Face> faces, UvProjectionMode projection, out string? error)
        {
            error = null;

            var uvEditingType = Type.GetType("UnityEngine.ProBuilder.MeshOperations.UVEditing, Unity.ProBuilder");
            if (uvEditingType == null)
            {
                error = "UV editing API not found. Ensure ProBuilder is installed.";
                return false;
            }

            var methodCandidates = projection switch
            {
                UvProjectionMode.Auto => new[] { "ProjectFacesAuto", "ProjectFaces" },
                UvProjectionMode.Box => new[] { "ProjectFacesBox", "ProjectFaces" },
                UvProjectionMode.Planar => new[] { "ProjectFacesPlanar", "ProjectFaces" },
                _ => new[] { "ProjectFacesAuto" }
            };

            foreach (var methodName in methodCandidates)
            {
                if (TryInvokeUvEditing(uvEditingType, methodName, mesh, faces))
                    return true;
            }

            error = $"No compatible UV projection method found for '{projection}'.";
            return false;
        }

        private static bool TryInvokeUvEditing(Type uvEditingType, string methodName, ProBuilderMesh mesh, IList<Face> faces)
        {
            var methods = uvEditingType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.Name == methodName);

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 2)
                    continue;

                if (!parameters[0].ParameterType.IsAssignableFrom(typeof(ProBuilderMesh)))
                    continue;

                var faceArg = BuildFaceArgument(parameters[1].ParameterType, faces);
                if (faceArg == null)
                    continue;

                method.Invoke(null, new object[] { mesh, faceArg });
                return true;
            }

            return false;
        }

        private static object? BuildFaceArgument(Type parameterType, IList<Face> faces)
        {
            if (parameterType == typeof(Face[]))
                return faces.ToArray();

            if (parameterType.IsAssignableFrom(typeof(List<Face>)))
                return faces.ToList();

            if (parameterType.IsAssignableFrom(typeof(Face[])))
                return faces.ToArray();

            return null;
        }
    }
}
