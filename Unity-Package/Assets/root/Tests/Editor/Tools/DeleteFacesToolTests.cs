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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public sealed class DeleteFacesToolTests : BaseTest
    {
        [Test]
        public void DeleteFaces_RemovesFace()
        {
            var name = $"McpProBuilder-Delete-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
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

            mesh = ToolTests.GetMesh(name);
            Assert.Less(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void DeleteFaces_WithIndices_DeduplicatesAndRemoves()
        {
            var name = $"McpProBuilder-Delete-Indices-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
            var originalFaceCount = mesh.faceCount;
            var indices = new[] { 0, 0, 1 };

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceIndices"": {faceIndices}
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{faceIndices}", $"[{string.Join(", ", indices)}]" }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.DeleteFaces))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            mesh = ToolTests.GetMesh(name);
            Assert.Less(mesh.faceCount, originalFaceCount);
        }

        [Test]
        public void DeleteFaces_WhenDeletingAllFaces_ReturnsError()
        {
            var name = $"McpProBuilder-Delete-All-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
            var allFaces = Enumerable.Range(0, mesh.faceCount).ToArray();

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceIndices"": {faceIndices}
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{faceIndices}", $"[{string.Join(", ", allFaces)}]" }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.DeleteFaces))!, json);

            ToolTests.AssertToolError(result, "Cannot delete all faces");
        }
    }
}
