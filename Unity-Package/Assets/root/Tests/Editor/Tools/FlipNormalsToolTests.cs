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

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public sealed class FlipNormalsToolTests : BaseTest
    {
        [Test]
        public void FlipNormals_UpdatesFaces()
        {
            var name = $"McpProBuilder-Flip-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

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

            Assert.IsNotNull(ToolTests.GetMesh(name));
        }

        [Test]
        public void FlipNormals_WhenNoSelection_FlipsAllFaces()
        {
            var name = $"McpProBuilder-Flip-All-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} }
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.FlipNormals))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            Assert.IsNotNull(ToolTests.GetMesh(name));
        }

        [Test]
        public void FlipNormals_WithInvalidFaceIndex_ReturnsError()
        {
            var name = $"McpProBuilder-Flip-Invalid-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceIndices"": [999]
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.FlipNormals))!, json);

            ToolTests.AssertToolError(result, "Invalid face indices");
        }
    }
}
