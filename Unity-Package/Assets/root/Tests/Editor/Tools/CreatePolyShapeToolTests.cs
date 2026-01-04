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
using System.Reflection;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public class CreatePolyShapeToolTests : BaseTest
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
        public void CreatePolyShape_MinimumTriangle_CreatesMesh()
        {
            var name = $"McpProBuilder-Poly-Triangle-{Guid.NewGuid():N}";

            var json = JsonTestUtils.Fill(@"{
                ""points"": [[0,0], [1,0], [0.25,0.75]],
                ""height"": 0.05,
                ""flipNormals"": true,
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
        public void CreatePolyShape_WithInvalidPoint_ReturnsError()
        {
            var json = @"{
                ""points"": [[0], [1,0], [0,1]],
                ""height"": 0.25
            }";

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreatePolyShape))!, json);

            ToolTests.AssertToolError(result, "Point at index");
        }
    }
}
