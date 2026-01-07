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
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_ProBuilder
    {
        [McpPluginTool
        (
            "probuilder-get-mesh-info",
            Title = "Get ProBuilder mesh information"
        )]
        [Description(@"Retrieves information about a ProBuilder mesh including faces, vertices, and edges.
Use detail=""summary"" for a token-efficient overview showing face directions.
Use detail=""full"" for detailed face-by-face information.

TIP: With semantic face selection (faceDirection parameter) in Extrude/DeleteFaces/SetFaceMaterial,
you often don't need GetMeshInfo at all - just use faceDirection=""up"" etc. directly.")]
        public GetMeshInfoResponse GetMeshInfo
        (
            [Description("Reference to the GameObject with a ProBuilderMesh component.")]
            GameObjectRef gameObjectRef,
            [Description("Detail level for output.")]
            MeshInfoDetailLevel detail = MeshInfoDetailLevel.Summary,
            [Description("If true, includes detailed vertex positions for each face (only with detail='full').")]
            bool includeVertexPositions = false,
            [Description("If true, includes edge information for each face (only with detail='full').")]
            bool includeEdges = true,
            [Description("Maximum number of faces to include in detail (only with detail='full'). Use -1 for all faces.")]
            int maxFacesToShow = 20
        )
        => MainThread.Instance.Run(() =>
        {
            if (gameObjectRef?.IsValid != true)
                throw new Exception("Invalid GameObject reference provided.");

            var go = gameObjectRef.FindGameObject(out var error);
            if (error != null)
                throw new Exception(error);

            if (go == null)
                throw new Exception(Error.GameObjectNotFound());

            var proBuilderMesh = go.GetComponent<ProBuilderMesh>();
            if (proBuilderMesh == null)
                throw new Exception(Error.ProBuilderMeshNotFound(go.GetInstanceID()));

            var response = new GetMeshInfoResponse();

            // Basic info
            response.gameObjectName = go.name;
            response.instanceId = go.GetInstanceID();
            response.faceCount = proBuilderMesh.faceCount;
            response.vertexCount = proBuilderMesh.vertexCount;
            response.edgeCount = proBuilderMesh.edgeCount;
            response.triangleCount = proBuilderMesh.triangleCount;

            // Bounds
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var bounds = meshFilter.sharedMesh.bounds;
                response.bounds = new BoundsInfo
                {
                    center = FormatVector3(bounds.center),
                    size = FormatVector3(bounds.size),
                    min = FormatVector3(bounds.min),
                    max = FormatVector3(bounds.max)
                };
            }

            // Face directions
            var directionSummary = FaceSelectionHelper.GetFaceDirectionSummary(proBuilderMesh, out var otherFaces);
            var faces = proBuilderMesh.faces;
            var positions = proBuilderMesh.positions;

            response.faceDirections = new List<FaceDirectionInfo>();
            foreach (var kvp in directionSummary)
            {
                if (kvp.Value.Count > 0)
                {
                    var info = new FaceDirectionInfo
                    {
                        direction = kvp.Key.ToString(),
                        faceIndices = kvp.Value.ToArray()
                    };

                    // Get center of first face in this direction
                    if (kvp.Value.Count > 0 && kvp.Value[0] < faces.Count)
                    {
                        var center = FaceSelectionHelper.GetFaceCenter(faces[kvp.Value[0]], positions);
                        info.firstFaceCenter = FormatVector3(center);
                    }

                    response.faceDirections.Add(info);
                }
            }

            if (otherFaces.Count > 0)
            {
                response.faceDirections.Add(new FaceDirectionInfo
                {
                    direction = "other",
                    faceIndices = otherFaces.ToArray()
                });
            }

            // Full detail mode - detailed face-by-face info
            if (detail == MeshInfoDetailLevel.Full)
            {
                var faceCount = faces.Count();
                var facesToShow = maxFacesToShow < 0 ? faceCount : System.Math.Min(maxFacesToShow, faceCount);

                response.faces = new List<FaceInfo>();
                response.facesShown = facesToShow;
                response.facesTotal = faceCount;

                for (int i = 0; i < facesToShow; i++)
                {
                    var face = faces[i];
                    var faceVertices = face.distinctIndexes;
                    var faceEdges = face.edges;

                    // Calculate face center
                    var center = Vector3.zero;
                    foreach (var vertIndex in faceVertices)
                    {
                        center += positions[vertIndex];
                    }
                    var vertCount = faceVertices.Count();
                    center /= vertCount;

                    var faceInfo = new FaceInfo
                    {
                        index = i,
                        vertexCount = vertCount,
                        triangleCount = face.indexes.Count() / 3,
                        center = FormatVector3(center)
                    };

                    if (includeVertexPositions)
                    {
                        faceInfo.vertices = new List<VertexInfo>();
                        foreach (var vertIndex in faceVertices)
                        {
                            var pos = positions[vertIndex];
                            faceInfo.vertices.Add(new VertexInfo
                            {
                                index = vertIndex,
                                position = FormatVector3(pos)
                            });
                        }
                    }

                    if (includeEdges)
                    {
                        faceInfo.edges = new List<EdgeInfo>();
                        foreach (var edge in faceEdges)
                        {
                            var p1 = positions[edge.a];
                            var p2 = positions[edge.b];
                            faceInfo.edges.Add(new EdgeInfo
                            {
                                vertexA = edge.a,
                                vertexB = edge.b,
                                positionA = FormatVector3(p1),
                                positionB = FormatVector3(p2)
                            });
                        }
                    }

                    response.faces.Add(faceInfo);
                }

                // Unique edges summary
                if (includeEdges)
                {
                    var allEdges = proBuilderMesh.faces.SelectMany(f => f.edges).Distinct().ToList();
                    response.uniqueEdgeCount = allEdges.Count;
                }
            }

            return response;
        });

        private static string FormatVector3(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

        #region Response Data Classes

        public class GetMeshInfoResponse
        {
            public string gameObjectName = string.Empty;
            public int instanceId;
            public int faceCount;
            public int vertexCount;
            public int edgeCount;
            public int triangleCount;
            public BoundsInfo? bounds;
            public List<FaceDirectionInfo>? faceDirections;
            public List<FaceInfo>? faces;
            public int? facesShown;
            public int? facesTotal;
            public int? uniqueEdgeCount;
        }

        public class BoundsInfo
        {
            public string center = string.Empty;
            public string size = string.Empty;
            public string min = string.Empty;
            public string max = string.Empty;
        }

        public class FaceDirectionInfo
        {
            public string direction = string.Empty;
            public int[]? faceIndices;
            public string? firstFaceCenter;
        }

        public class FaceInfo
        {
            public int index;
            public int vertexCount;
            public int triangleCount;
            public string center = string.Empty;
            public List<VertexInfo>? vertices;
            public List<EdgeInfo>? edges;
        }

        public class VertexInfo
        {
            public int index;
            public string position = string.Empty;
        }

        public class EdgeInfo
        {
            public int vertexA;
            public int vertexB;
            public string positionA = string.Empty;
            public string positionB = string.Empty;
        }

        #endregion
    }
}
