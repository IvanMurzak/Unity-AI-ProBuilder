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
    public sealed class MergeObjectsToolTests : BaseTest
    {
        [Test]
        public void MergeObjects_MergesMeshes()
        {
            var nameA = $"McpProBuilder-Merge-A-{Guid.NewGuid():N}";
            var nameB = $"McpProBuilder-Merge-B-{Guid.NewGuid():N}";
            var instanceA = ToolTests.CreateShapeInstance(nameA);
            var instanceB = ToolTests.CreateShapeInstance(nameB);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRefs"": [
                    { ""instanceID"": {instanceA} },
                    { ""instanceID"": {instanceB} }
                ]
            }", new Dictionary<string, object?>
            {
                { "{instanceA}", instanceA },
                { "{instanceB}", instanceB }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.MergeObjects))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            Assert.IsNull(GameObject.Find(nameB), "Expected merged object to delete the source.");
            Assert.IsNotNull(GameObject.Find(nameA), "Expected target object to remain.");
        }

        [Test]
        public void MergeObjects_WhenKeepingSources_KeepsAllObjects()
        {
            var nameA = $"McpProBuilder-Merge-Keep-A-{Guid.NewGuid():N}";
            var nameB = $"McpProBuilder-Merge-Keep-B-{Guid.NewGuid():N}";
            var instanceA = ToolTests.CreateShapeInstance(nameA);
            var instanceB = ToolTests.CreateShapeInstance(nameB);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRefs"": [
                    { ""instanceID"": {instanceA} },
                    { ""instanceID"": {instanceB} }
                ],
                ""deleteSourceObjects"": false
            }", new Dictionary<string, object?>
            {
                { "{instanceA}", instanceA },
                { "{instanceB}", instanceB }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.MergeObjects))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .Execute();

            Assert.IsNotNull(GameObject.Find(nameA), "Expected target object to remain.");
            Assert.IsNotNull(GameObject.Find(nameB), "Expected source object to remain.");
        }

        [Test]
        public void MergeObjects_WithSingleObject_ReturnsError()
        {
            var nameA = $"McpProBuilder-Merge-Invalid-{Guid.NewGuid():N}";
            var instanceA = ToolTests.CreateShapeInstance(nameA);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRefs"": [
                    { ""instanceID"": {instanceA} }
                ]
            }", new Dictionary<string, object?>
            {
                { "{instanceA}", instanceA }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.MergeObjects))!, json);

            ToolTests.AssertToolError(result, "At least 2 GameObjects");
        }
    }
}
