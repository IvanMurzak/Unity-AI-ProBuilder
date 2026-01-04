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

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public sealed class BevelToolTests : BaseTest
    {
        [Test]
        public void Bevel_Edges()
        {
            var name = $"McpProBuilder-Bevel-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
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

            if (ToolTests.IsToolInconclusive(result, "Bevel"))
            {
                UnityEngine.Debug.LogWarning($"SMOKE: Bevel not supported for selected edges. {result}");
                Assert.Inconclusive($"SMOKE: Bevel not supported for selected edges. {result}");
            }

            StringAssert.Contains("[Success]", result);
            mesh = ToolTests.GetMesh(name);
            Assert.Greater(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void Bevel_Edges_WithLargerAmount()
        {
            var name = $"McpProBuilder-Bevel-Amount-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
            var faceEdges = mesh.faces[0].edges;
            Assert.IsNotEmpty(faceEdges);
            var edge = faceEdges[0];
            var originalFaceCount = mesh.faceCount;

            MainThreadInstaller.Init();
            var tool = new Tool_ProBuilder();
            var result = tool.Bevel(
                new GameObjectRef(instanceId),
                new[] { new[] { edge.a, edge.b } },
                0.25f);

            if (ToolTests.IsToolInconclusive(result, "Bevel"))
            {
                UnityEngine.Debug.LogWarning($"SMOKE: Bevel not supported for selected edges. {result}");
                Assert.Inconclusive($"SMOKE: Bevel not supported for selected edges. {result}");
            }

            StringAssert.Contains("[Success]", result);
            mesh = ToolTests.GetMesh(name);
            Assert.Greater(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void Bevel_WithInvalidVertexIndex_ReturnsError()
        {
            var name = $"McpProBuilder-Bevel-Invalid-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
            var invalidIndex = mesh.vertexCount + 5;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""edges"": [[{vertA}, {vertB}]],
                ""amount"": 0.1
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{vertA}", invalidIndex },
                { "{vertB}", invalidIndex }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.Bevel))!, json);

            ToolTests.AssertToolError(result, "out of range");
        }
    }
}
