#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests
{
    public class ProBuilderToolExecutionTests : BaseTest
    {
        [Test]
        public void CreateShape_CreatesProBuilderMesh()
        {
            var name = $"McpProBuilder-{Guid.NewGuid():N}";

            var json = JsonTestUtils.Fill(@"{
                ""shapeType"": ""Cube"",
                ""name"": ""{name}""
            }", new Dictionary<string, object?>
            {
                { "{name}", name }
            });

            new CallToolExecutor(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreateShape))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var go = GameObject.Find(name);
                    Assert.IsNotNull(go, "Expected new ProBuilder GameObject.");
                    Assert.IsNotNull(go!.GetComponent<ProBuilderMesh>(), "Expected ProBuilderMesh component.");
                })
                .Execute();
        }

        [Test]
        public void GetMeshInfoAndGenerateUvs_OnCreatedShape_Succeeds()
        {
            var previousIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            var name = $"McpProBuilder-Uvs-{Guid.NewGuid():N}";
            var instanceId = 0;

            var createJson = JsonTestUtils.Fill(@"{
                ""shapeType"": ""Cube"",
                ""name"": ""{name}""
            }", new Dictionary<string, object?>
            {
                { "{name}", name }
            });

            var getInfoJson = new DynamicCallToolExecutor(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.GetMeshInfo))!,
                () => JsonTestUtils.Fill(@"{
                    ""gameObjectRef"": {
                        ""instanceID"": {instanceId}
                    },
                    ""detail"": ""Summary""
                }", new Dictionary<string, object?>
                {
                    { "{instanceId}", instanceId }
                }));

            try
            {
                new CallToolExecutor(
                        typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreateShape))!, createJson)
                    .AddChild(new ValidateToolResultExecutor())
                    .AddChild(() =>
                    {
                        var go = GameObject.Find(name);
                        Assert.IsNotNull(go, "Expected new ProBuilder GameObject.");
                        instanceId = go!.GetInstanceID();
                    })
                    .AddChild(getInfoJson)
                    .AddChild(new ValidateToolResultExecutor())
                    .AddChild(() =>
                    {
                        MainThreadInstaller.Init();
                        var tool = new Tool_ProBuilder();
                        var result = tool.GenerateUVs(new GameObjectRef(instanceId), projection: Tool_ProBuilder.UvProjectionMode.Auto);

                        if (IsUvProjectionUnsupported(result))
                        {
                            Debug.LogWarning($"SMOKE: UV projection API not available. {result}");
                            Assert.Inconclusive($"SMOKE: UV projection API not available. {result}");
                        }

                        StringAssert.Contains("[Success]", result);
                    })
                    .Execute();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        [Test]
        public void GenerateUvs_OnBoundaryFaces_Succeeds()
        {
            var previousIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;

            var name = $"McpProBuilder-UvsBoundary-{Guid.NewGuid():N}";
            var instanceId = 0;
            List<int> boundaryFaces = new();
            HashSet<int> boundaryVertices = new();

            var createJson = JsonTestUtils.Fill(@"{
                ""shapeType"": ""Cube"",
                ""name"": ""{name}""
            }", new Dictionary<string, object?>
            {
                { "{name}", name }
            });

            var deleteJson = new DynamicCallToolExecutor(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.DeleteFaces))!,
                () => JsonTestUtils.Fill(@"{
                    ""gameObjectRef"": { ""instanceID"": {instanceId} },
                    ""faceDirection"": ""Up""
                }", new Dictionary<string, object?>
                {
                    { "{instanceId}", instanceId }
                }));

            var generateUvsJson = new DynamicCallToolExecutor(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.GenerateUVs))!,
                () => JsonTestUtils.Fill(@"{
                    ""gameObjectRef"": { ""instanceID"": {instanceId} },
                    ""faceIndices"": {faceIndices},
                    ""projection"": ""Auto""
                }", new Dictionary<string, object?>
                {
                    { "{instanceId}", instanceId },
                    { "{faceIndices}", $"[{string.Join(", ", boundaryFaces)}]" }
                }));

            try
            {
                new CallToolExecutor(
                        typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreateShape))!, createJson)
                    .AddChild(new ValidateToolResultExecutor())
                    .AddChild(() =>
                    {
                        var go = GameObject.Find(name);
                        Assert.IsNotNull(go, "Expected new ProBuilder GameObject.");
                        instanceId = go!.GetInstanceID();
                    })
                    .AddChild(deleteJson)
                    .AddChild(new ValidateToolResultExecutor())
                    .AddChild(() =>
                    {
                        var mesh = GetMesh(name);
                        mesh.ToMesh();
                        mesh.Refresh();

                        boundaryFaces = GetBoundaryFaceIndices(mesh);
                        Assert.IsNotEmpty(boundaryFaces, "Expected faces adjacent to boundary edges.");
                        boundaryVertices = GetBoundaryVertexIndices(mesh, boundaryFaces);
                        Assert.IsNotEmpty(boundaryVertices, "Expected boundary faces to include vertex indices.");

                        SetAllUvsToZero(mesh);
                    })
                    .AddChild(generateUvsJson)
                    .AddChild(new LazyNodeExecutor().SetAction<object, object>(result =>
                    {
                        AssertUvToolResult(result);
                        return result;
                    }))
                    .AddChild(() =>
                    {
                        var mesh = GetMesh(name);
                        AssertUvsUpdated(mesh, boundaryVertices);
                    })
                    .Execute();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }

        private static bool IsUvProjectionUnsupported(string result)
        {
            return result.Contains("UV editing API not found", StringComparison.OrdinalIgnoreCase)
                   || result.Contains("No compatible UV projection method found", StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertUvToolResult(object? result)
        {
            var message = TryGetMessage(result);
            Debug.Log($"Tool execution result:\n{message}");

            if (message.Contains("[Error]", StringComparison.OrdinalIgnoreCase))
            {
                if (IsUvProjectionUnsupported(message))
                {
                    Debug.LogWarning($"SMOKE: UV projection API not available. {message}");
                    Assert.Inconclusive($"SMOKE: UV projection API not available. {message}");
                }

                Assert.Fail($"UV tool call failed: {message}");
            }

            StringAssert.Contains("[Success]", message);
        }

        private static string TryGetMessage(object? result)
        {
            if (result == null)
                return string.Empty;

            var messageProp = result.GetType().GetProperty("Message");
            if (messageProp != null && messageProp.PropertyType == typeof(string))
                return (string)(messageProp.GetValue(result) ?? string.Empty);

            return result.ToString() ?? string.Empty;
        }

        private static ProBuilderMesh GetMesh(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, $"Expected GameObject {name}");
            var mesh = go!.GetComponent<ProBuilderMesh>();
            Assert.IsNotNull(mesh, $"Expected ProBuilderMesh on {name}");
            return mesh!;
        }

        private static void SetAllUvsToZero(ProBuilderMesh mesh)
        {
            var zeroUvs = new List<Vector4>(mesh.vertexCount);
            for (int i = 0; i < mesh.vertexCount; i++)
                zeroUvs.Add(Vector4.zero);

            mesh.SetUVs(0, zeroUvs);
            mesh.ToMesh();
            mesh.Refresh();
        }

        private static void AssertUvsUpdated(ProBuilderMesh mesh, HashSet<int> vertexIndices)
        {
            var uvs = new List<Vector4>();
            mesh.GetUVs(0, uvs);
            Assert.IsNotEmpty(uvs, "Expected UV list to be populated after projection.");
            Assert.IsTrue(vertexIndices.Count > 0, "Expected vertices to validate UV projection.");

            var anyUpdated = vertexIndices.Any(index => index >= 0 && index < uvs.Count && uvs[index] != Vector4.zero);
            Assert.IsTrue(anyUpdated, "Expected boundary face UVs to change from zero after projection.");
        }

        private static List<int> GetBoundaryFaceIndices(ProBuilderMesh mesh)
        {
            var boundaryEdges = GetBoundaryEdges(mesh);
            var boundaryKeys = new HashSet<EdgeKey>(boundaryEdges.Select(edge => new EdgeKey(edge)));
            var faces = mesh.faces;
            var result = new List<int>();

            for (int i = 0; i < faces.Count; i++)
            {
                foreach (var edge in faces[i].edges)
                {
                    if (boundaryKeys.Contains(new EdgeKey(edge)))
                    {
                        result.Add(i);
                        break;
                    }
                }
            }

            return result;
        }

        private static HashSet<int> GetBoundaryVertexIndices(ProBuilderMesh mesh, IEnumerable<int> faceIndices)
        {
            var result = new HashSet<int>();
            var faces = mesh.faces;

            foreach (var faceIndex in faceIndices)
            {
                if (faceIndex < 0 || faceIndex >= faces.Count)
                    continue;

                foreach (var index in faces[faceIndex].distinctIndexes)
                    result.Add(index);
            }

            return result;
        }

        private static List<Edge> GetBoundaryEdges(ProBuilderMesh mesh)
        {
            var edges = new Dictionary<EdgeKey, Edge>();
            var counts = new Dictionary<EdgeKey, int>();

            foreach (var face in mesh.faces)
            {
                foreach (var edge in face.edges)
                {
                    var key = new EdgeKey(edge);
                    edges[key] = edge;
                    counts.TryGetValue(key, out var count);
                    counts[key] = count + 1;
                }
            }

            var boundary = new List<Edge>();
            foreach (var pair in counts)
            {
                if (pair.Value == 1 && edges.TryGetValue(pair.Key, out var edge))
                    boundary.Add(edge);
            }

            return boundary;
        }

        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            readonly int a;
            readonly int b;

            public EdgeKey(Edge edge)
            {
                if (edge.a < edge.b)
                {
                    a = edge.a;
                    b = edge.b;
                }
                else
                {
                    a = edge.b;
                    b = edge.a;
                }
            }

            public bool Equals(EdgeKey other)
                => a == other.a && b == other.b;

            public override bool Equals(object? obj)
                => obj is EdgeKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (a * 397) ^ b;
                }
            }
        }
    }
}
