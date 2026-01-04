/*
┌──────────────────────────────────────────────────────────────────────────┐
│  Author: Tristyn Mackay (https://github.com/InMetaTech-Tristyn)          │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ProBuilder)  │
│  Copyright (c) 2025 Ivan Murzak                                          │
│  Licensed under the Apache License, Version 2.0.                         │
│  See the LICENSE file in the project root for more information.          │
└──────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable

using System;
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public sealed class BridgeToolTests : BaseTest
    {
        [Test]
        public void Bridge_Edges()
        {
            var name = $"McpProBuilder-Bridge-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name, ShapeType.Cube);
            var mesh = ToolTests.GetMesh(name);
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
            mesh = ToolTests.GetMesh(name);
            Assert.GreaterOrEqual(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void Bridge_Edges_DisallowNonManifold()
        {
            var name = $"McpProBuilder-Bridge-Strict-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name, ShapeType.Cube);
            var mesh = ToolTests.GetMesh(name);
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
                allowNonManifold: false);

            if (result.Contains("[Error]", StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Debug.LogWarning($"SMOKE: Bridge not supported for selected edges. {result}");
                StringAssert.Contains("Bridge failed", result);
                return;
            }

            StringAssert.Contains("[Success]", result);
            mesh = ToolTests.GetMesh(name);
            Assert.GreaterOrEqual(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void Bridge_WithInvalidEdgeDefinition_ReturnsError()
        {
            var name = $"McpProBuilder-Bridge-Invalid-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name, ShapeType.Cube);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""edgeA"": [0],
                ""edgeB"": [1, 2]
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.Bridge))!, json);

            ToolTests.AssertToolError(result, "edgeA");
        }
    }
}
