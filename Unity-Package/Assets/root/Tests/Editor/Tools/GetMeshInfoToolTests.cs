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
    public sealed class GetMeshInfoToolTests : BaseTest
    {
        [Test]
        public void GetMeshInfo_OnCreatedShape_Succeeds()
        {
            var name = $"McpProBuilder-Info-{Guid.NewGuid():N}";
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
                .Execute();
        }

        [Test]
        public void GetMeshInfo_FullDetail_WithVertexPositions_Succeeds()
        {
            var name = $"McpProBuilder-Info-Full-{Guid.NewGuid():N}";
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
                    ""detail"": ""Full"",
                    ""includeVertexPositions"": true,
                    ""includeEdges"": false,
                    ""maxFacesToShow"": 1
                }", new Dictionary<string, object?>
                {
                    { "{instanceId}", instanceId }
                }));

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
                .Execute();
        }

        [Test]
        public void GetMeshInfo_WithInvalidGameObject_ReturnsError()
        {
            var json = @"{
                ""gameObjectRef"": { ""instanceID"": 999999 },
                ""detail"": ""Summary""
            }";

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.GetMeshInfo))!, json);

            ToolTests.AssertToolError(result, "Not found GameObject");
        }
    }
}
