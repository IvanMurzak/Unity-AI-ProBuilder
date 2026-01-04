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
    public sealed class ConnectEdgesToolTests : BaseTest
    {
        [Test]
        public void ConnectEdges_CreatesGeometry()
        {
            var name = $"McpProBuilder-Connect-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
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

            mesh = ToolTests.GetMesh(name);
            Assert.GreaterOrEqual(mesh.edgeCount, originalEdgeCount);
        }

        [Test]
        public void ConnectEdges_WithExplicitEdges_CreatesGeometry()
        {
            var name = $"McpProBuilder-Connect-Explicit-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
            var originalEdgeCount = mesh.edgeCount;
            var faceEdges = mesh.faces[0].edges;
            Assert.GreaterOrEqual(faceEdges.Count, 3);

            var edgeA = faceEdges[0];
            var edgeB = faceEdges[2];

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""edges"": [[{edgeA0}, {edgeA1}], [{edgeB0}, {edgeB1}]]
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{edgeA0}", edgeA.a },
                { "{edgeA1}", edgeA.b },
                { "{edgeB0}", edgeB.a },
                { "{edgeB1}", edgeB.b }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.ConnectEdges))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            mesh = ToolTests.GetMesh(name);
            Assert.GreaterOrEqual(mesh.edgeCount, originalEdgeCount);
        }

        [Test]
        public void ConnectEdges_WithSingleEdge_ReturnsError()
        {
            var name = $"McpProBuilder-Connect-Invalid-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var mesh = ToolTests.GetMesh(name);
            var edge = mesh.faces[0].edges[0];

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""edges"": [[{edgeA0}, {edgeA1}]]
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{edgeA0}", edge.a },
                { "{edgeA1}", edge.b }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.ConnectEdges))!, json);

            ToolTests.AssertToolError(result, "At least 2 edges");
        }
    }
}
