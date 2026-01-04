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
    public sealed class SetPivotToolTests : BaseTest
    {
        [Test]
        public void SetPivot_UpdatesTransform()
        {
            var name = $"McpProBuilder-Pivot-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var go = GameObject.Find(name)!;
            var originalPosition = go.transform.position;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""pivotLocation"": ""FirstVertex""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SetPivot))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            Assert.AreNotEqual(originalPosition, go.transform.position);
        }

        [Test]
        public void SetPivot_CustomPosition_UsesWorldPivot()
        {
            var name = $"McpProBuilder-Pivot-Custom-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);
            var customPosition = new Vector3(1.5f, -0.25f, 0.75f);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""pivotLocation"": ""Custom"",
                ""customPosition"": { ""x"": 1.5, ""y"": -0.25, ""z"": 0.75 }
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SetPivot))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            var go = GameObject.Find(name);
            Assert.IsNotNull(go);
            Assert.AreEqual(customPosition, go!.transform.position);
        }

        [Test]
        public void SetPivot_CustomWithoutPosition_ReturnsError()
        {
            var name = $"McpProBuilder-Pivot-Invalid-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""pivotLocation"": ""Custom""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SetPivot))!, json);

            ToolTests.AssertToolError(result, "customPosition");
        }
    }
}
