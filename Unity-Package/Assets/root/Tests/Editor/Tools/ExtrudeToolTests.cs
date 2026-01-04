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
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public sealed class ExtrudeToolTests : BaseTest
    {
        [Test]
        public void Extrude_AddsFaces()
        {
            var name = $"McpProBuilder-Extrude-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
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

            mesh = ToolTests.GetMesh(name);
            Assert.Greater(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void Extrude_WithIndicesAndNegativeDistance_AddsFaces()
        {
            var name = $"McpProBuilder-Extrude-Negative-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
            var originalFaceCount = mesh.faceCount;
            var indices = new[] { 0 };

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceIndices"": {faceIndices},
                ""distance"": -0.15,
                ""extrudeMethod"": ""IndividualFaces""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{faceIndices}", $"[{string.Join(", ", indices)}]" }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.Extrude))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            mesh = ToolTests.GetMesh(name);
            Assert.Greater(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void Extrude_WithoutSelection_ReturnsError()
        {
            var name = $"McpProBuilder-Extrude-Invalid-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""distance"": 0.2
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.Extrude))!, json);

            ToolTests.AssertToolError(result, "Either faceIndices or faceDirection");
        }
    }
}
