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
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public sealed class CreateMeshToolTests : BaseTest
    {
        [Test]
        public void CreateShape_CreatesProBuilderMesh()
        {
            var name = $"McpProBuilder-{Guid.NewGuid():N}";

            var json = JsonTestUtils.Fill(@"{
                ""shapeType"": ""Cube"",
                ""name"": ""{name}""
            }", new Dictionary<string, object?>
            {
                { "{name}", name }
            });

            new CallToolExecutor(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreateShape))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var go = GameObject.Find(name);
                    Assert.IsNotNull(go, "Expected new ProBuilder GameObject.");
                    Assert.IsNotNull(go!.GetComponent<ProBuilderMesh>(), "Expected ProBuilderMesh component.");
                })
                .Execute();
        }

        [Test]
        public void CreateShape_WithParentAndLocalSpace_SetsLocalTransform()
        {
            var parent = new GameObject($"McpProBuilder-Parent-{Guid.NewGuid():N}");
            var name = $"McpProBuilder-Child-{Guid.NewGuid():N}";
            var parentId = parent.GetInstanceID();

            var json = JsonTestUtils.Fill(@"{
                ""shapeType"": ""Cube"",
                ""name"": ""{name}"",
                ""parentGameObjectRef"": { ""instanceID"": {parentId} },
                ""position"": { ""x"": 1.25, ""y"": -0.5, ""z"": 2.5 },
                ""rotation"": { ""x"": 0, ""y"": 45, ""z"": 0 },
                ""scale"": { ""x"": 0.5, ""y"": 1.25, ""z"": 0.75 },
                ""isLocalSpace"": true
            }", new Dictionary<string, object?>
            {
                { "{name}", name },
                { "{parentId}", parentId }
            });

            new CallToolExecutor(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreateShape))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var go = GameObject.Find(name);
                    Assert.IsNotNull(go, "Expected new ProBuilder GameObject.");
                    Assert.AreEqual(parent.transform, go!.transform.parent);
                    Assert.That(Vector3.Distance(go.transform.localPosition, new Vector3(1.25f, -0.5f, 2.5f)),
                        Is.LessThan(0.001f));
                    Assert.That(Quaternion.Angle(go.transform.localRotation, Quaternion.Euler(0f, 45f, 0f)),
                        Is.LessThan(0.1f));
                    Assert.That(Vector3.Distance(go.transform.localScale, new Vector3(0.5f, 1.25f, 0.75f)),
                        Is.LessThan(0.001f));
                })
                .Execute();
        }

        [Test]
        public void CreateShape_WithInvalidParent_ReturnsError()
        {
            var name = $"McpProBuilder-InvalidParent-{Guid.NewGuid():N}";

            var json = JsonTestUtils.Fill(@"{
                ""shapeType"": ""Cube"",
                ""name"": ""{name}"",
                ""parentGameObjectRef"": { ""instanceID"": 999999 }
            }", new Dictionary<string, object?>
            {
                { "{name}", name }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreateShape))!, json);

            ToolTests.AssertToolError(result, "Not found GameObject");
        }

    }
}
