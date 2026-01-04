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
    public sealed class SubdivideEdgesToolTests : BaseTest
    {
        [Test]
        public void SubdivideEdges_AddsVertices()
        {
            var name = $"McpProBuilder-Subdivide-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
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

            mesh = ToolTests.GetMesh(name);
            Assert.Greater(mesh.vertexCount, originalVertexCount);
        }

        [Test]
        public void SubdivideEdges_WithExplicitEdges_AddsVertices()
        {
            var name = $"McpProBuilder-Subdivide-Explicit-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
            var originalVertexCount = mesh.vertexCount;
            var faceEdges = mesh.faces[0].edges;
            Assert.IsNotEmpty(faceEdges);

            var edge = faceEdges[0];

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""edges"": [[{edgeA0}, {edgeA1}]],
                ""subdivisions"": 2
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{edgeA0}", edge.a },
                { "{edgeA1}", edge.b }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SubdivideEdges))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            mesh = ToolTests.GetMesh(name);
            Assert.Greater(mesh.vertexCount, originalVertexCount);
        }

        [Test]
        public void SubdivideEdges_WithInvalidSubdivision_ReturnsError()
        {
            var name = $"McpProBuilder-Subdivide-Invalid-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""faceDirection"": ""Up"",
                ""subdivisions"": 0
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SubdivideEdges))!, json);

            ToolTests.AssertToolError(result, "Subdivisions must be at least 1");
        }
    }
}
