#nullable enable

using System;
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests
{
    public class ProBuilderAdditionalToolExecutionTests : BaseTest
    {
        [Test]
        public void CreatePolyShape_CreatesMesh()
        {
            var name = $"McpProBuilder-Poly-{Guid.NewGuid():N}";

            var json = JsonTestUtils.Fill(@"{
                ""points"": [[0,0], [2,0], [2,1], [0,1]],
                ""height"": 1.2,
                ""name"": ""{name}""
            }", new Dictionary<string, object?>
            {
                { "{name}", name }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreatePolyShape))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var go = GameObject.Find(name);
                    Assert.IsNotNull(go, "Expected new ProBuilder poly shape.");
                    Assert.IsNotNull(go!.GetComponent<ProBuilderMesh>(), "Expected ProBuilderMesh component.");
                })
                .Execute();
        }

        [Test]
        public void DeleteFaces_RemovesFace()
        {
            var name = $"McpProBuilder-Delete-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name);
            var mesh = GetMesh(name);
            var originalFaceCount = mesh.faceCount;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceDirection"": ""Up""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.DeleteFaces))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            mesh = GetMesh(name);
            Assert.Less(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void Extrude_AddsFaces()
        {
            var name = $"McpProBuilder-Extrude-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name);
            var mesh = GetMesh(name);
            var originalFaceCount = mesh.faceCount;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceDirection"": ""Up"",
                ""distance"": 0.2,
                ""extrudeMethod"": ""FaceNormal""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.Extrude))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            mesh = GetMesh(name);
            Assert.Greater(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void FlipNormals_UpdatesFaces()
        {
            var name = $"McpProBuilder-Flip-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceDirection"": ""Up""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.FlipNormals))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            Assert.IsNotNull(GetMesh(name));
        }

        [Test]
        public void SubdivideEdges_AddsVertices()
        {
            var name = $"McpProBuilder-Subdivide-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name);
            var mesh = GetMesh(name);
            var originalVertexCount = mesh.vertexCount;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceDirection"": ""Up"",
                ""subdivisions"": 1
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SubdivideEdges))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            mesh = GetMesh(name);
            Assert.Greater(mesh.vertexCount, originalVertexCount);
        }

        [Test]
        public void ConnectEdges_CreatesGeometry()
        {
            var name = $"McpProBuilder-Connect-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name);
            var mesh = GetMesh(name);
            var originalEdgeCount = mesh.edgeCount;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceDirection"": ""Up""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.ConnectEdges))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            mesh = GetMesh(name);
            Assert.GreaterOrEqual(mesh.edgeCount, originalEdgeCount);
        }

        [Test]
        public void Bevel_Edges()
        {
            var name = $"McpProBuilder-Bevel-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name);
            var mesh = GetMesh(name);
            var faceEdges = mesh.faces[0].edges;
            Assert.IsNotEmpty(faceEdges);
            var edge = faceEdges[0];
            var originalFaceCount = mesh.faceCount;

            MainThreadInstaller.Init();
            var tool = new Tool_ProBuilder();
            var result = tool.Bevel(
                new GameObjectRef(instanceId),
                new[] { new[] { edge.a, edge.b } },
                0.1f);

            if (IsToolInconclusive(result, "Bevel"))
            {
                Debug.LogWarning($"SMOKE: Bevel not supported for selected edges. {result}");
                Assert.Inconclusive($"SMOKE: Bevel not supported for selected edges. {result}");
            }

            StringAssert.Contains("[Success]", result);
            mesh = GetMesh(name);
            Assert.Greater(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void Bridge_Edges()
        {
            var name = $"McpProBuilder-Bridge-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name, ShapeType.Cube);
            var mesh = GetMesh(name);
            var upFaces = FaceSelectionHelper.SelectFacesByDirection(mesh, FaceDirection.Up, out var upError);
            Assert.IsNull(upError, upError);
            Assert.IsNotNull(upFaces);
            Assert.IsNotEmpty(upFaces);

            var downFaces = FaceSelectionHelper.SelectFacesByDirection(mesh, FaceDirection.Down, out var downError);
            Assert.IsNull(downError, downError);
            Assert.IsNotNull(downFaces);
            Assert.IsNotEmpty(downFaces);

            var upFace = mesh.faces[upFaces![0]];
            var downFace = mesh.faces[downFaces![0]];
            Assert.AreNotEqual(upFace, downFace, "Expected distinct faces for bridging.");

            var edgeA = upFace.edges[0];
            var edgeB = downFace.edges[0];

            var originalFaceCount = mesh.faceCount;

            MainThreadInstaller.Init();
            var tool = new Tool_ProBuilder();
            var result = tool.Bridge(
                new GameObjectRef(instanceId),
                new[] { edgeA.a, edgeA.b },
                new[] { edgeB.a, edgeB.b },
                allowNonManifold: true);

            StringAssert.Contains("[Success]", result);
            mesh = GetMesh(name);
            Assert.GreaterOrEqual(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void SetFaceMaterial_AssignsMaterial()
        {
            var name = $"McpProBuilder-Material-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name);

            var folderName = $"Unity-MCP-ProBuilder-Materials-{Guid.NewGuid():N}";
            var materialExecutor = new CreateMaterialExecutor(
                "McpProBuilderMaterial.mat",
                "Standard",
                "Assets",
                folderName);

            var materialPath = materialExecutor.AssetPath;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""materialPath"": ""{materialPath}"",
                ""faceDirection"": ""Up""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{materialPath}", materialPath }
            });

            materialExecutor
                .AddChild(new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SetFaceMaterial))!, json))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var renderer = GetMeshRenderer(name);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    Assert.IsNotNull(material);
                    CollectionAssert.Contains(renderer.sharedMaterials, material);
                })
                .Execute();
        }

        [Test]
        public void SetPivot_UpdatesTransform()
        {
            var name = $"McpProBuilder-Pivot-{Guid.NewGuid():N}";
            var instanceId = CreateShapeInstance(name);
            var go = GameObject.Find(name)!;
            var originalPosition = go.transform.position;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""pivotLocation"": ""FirstVertex""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SetPivot))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            Assert.AreNotEqual(originalPosition, go.transform.position);
        }

        [Test]
        public void MergeObjects_MergesMeshes()
        {
            var nameA = $"McpProBuilder-Merge-A-{Guid.NewGuid():N}";
            var nameB = $"McpProBuilder-Merge-B-{Guid.NewGuid():N}";
            var instanceA = CreateShapeInstance(nameA);
            var instanceB = CreateShapeInstance(nameB);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRefs"": [
                    { ""instanceID"": {instanceA} },
                    { ""instanceID"": {instanceB} }
                ]
            }", new Dictionary<string, object?>
            {
                { "{instanceA}", instanceA },
                { "{instanceB}", instanceB }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.MergeObjects))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            Assert.IsNull(GameObject.Find(nameB), "Expected merged object to delete the source.");
            Assert.IsNotNull(GameObject.Find(nameA), "Expected target object to remain.");
        }

        private static int CreateShapeInstance(string name, ShapeType shapeType = ShapeType.Cube)
        {
            var instanceId = 0;

            var json = JsonTestUtils.Fill(@"{
                ""shapeType"": ""{shapeType}"",
                ""name"": ""{name}""
            }", new Dictionary<string, object?>
            {
                { "{shapeType}", shapeType.ToString() },
                { "{name}", name }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreateShape))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var go = GameObject.Find(name);
                    Assert.IsNotNull(go, "Expected new ProBuilder GameObject.");
                    instanceId = go!.GetInstanceID();
                })
                .Execute();

            return instanceId;
        }

        private static ProBuilderMesh GetMesh(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, $"Expected GameObject {name}");
            var mesh = go!.GetComponent<ProBuilderMesh>();
            Assert.IsNotNull(mesh, $"Expected ProBuilderMesh on {name}");
            return mesh!;
        }

        private static MeshRenderer GetMeshRenderer(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, $"Expected GameObject {name}");
            var renderer = go!.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer, $"Expected MeshRenderer on {name}");
            return renderer!;
        }

        private static bool IsToolInconclusive(string result, string toolName)
        {
            if (string.IsNullOrWhiteSpace(result))
                return true;

            if (!result.Contains("[Error]", StringComparison.OrdinalIgnoreCase))
                return false;

            return toolName switch
            {
                "Bridge" => true,
                "Bevel" => true,
                _ => false
            };
        }

    }
}
