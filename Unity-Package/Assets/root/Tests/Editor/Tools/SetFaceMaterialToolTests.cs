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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public sealed class SetFaceMaterialToolTests : BaseTest
    {
        [Test]
        public void SetFaceMaterial_AssignsMaterial()
        {
            var name = $"McpProBuilder-Material-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

            var folderName = $"Unity-MCP-ProBuilder-Materials-{Guid.NewGuid():N}";
            var materialExecutor = new CreateMaterialExecutor(
                "McpProBuilderMaterial.mat",
                "Standard",
                "Assets",
                folderName);

            var materialPath = materialExecutor.AssetPath;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""materialPath"": ""{materialPath}"",
                ""faceDirection"": ""Up""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{materialPath}", materialPath }
            });

            materialExecutor
                .AddChild(new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SetFaceMaterial))!, json))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var renderer = ToolTests.GetMeshRenderer(name);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    Assert.IsNotNull(material);
                    CollectionAssert.Contains(renderer.sharedMaterials, material);
                })
                .Execute();
        }

        [Test]
        public void SetFaceMaterial_UsesLegacyMaterialAssetPath()
        {
            var name = $"McpProBuilder-Material-Legacy-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

            var folderName = $"Unity-MCP-ProBuilder-Materials-{Guid.NewGuid():N}";
            var materialExecutor = new CreateMaterialExecutor(
                "McpProBuilderMaterialLegacy.mat",
                "Standard",
                "Assets",
                folderName);

            var materialPath = materialExecutor.AssetPath;

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""materialPath"": """",
                ""materialAssetPath"": ""{materialPath}"",
                ""faceIndices"": [0]
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId },
                { "{materialPath}", materialPath }
            });

            materialExecutor
                .AddChild(new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SetFaceMaterial))!, json))
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var renderer = ToolTests.GetMeshRenderer(name);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    Assert.IsNotNull(material);
                    CollectionAssert.Contains(renderer.sharedMaterials, material);
                })
                .Execute();
        }

        [Test]
        public void SetFaceMaterial_WithMissingMaterial_ReturnsError()
        {
            var name = $"McpProBuilder-Material-Invalid-{Guid.NewGuid():N}";
            var instanceId = ToolTests.CreateShapeInstance(name);

            var json = JsonTestUtils.Fill(@"{
                ""gameObjectRef"": { ""instanceID"": {instanceId} },
                ""materialPath"": ""Assets/DoesNotExist.mat"",
                ""faceDirection"": ""Up""
            }", new Dictionary<string, object?>
            {
                { "{instanceId}", instanceId }
            });

            var result = ToolTests.RunToolRaw(
                typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.SetFaceMaterial))!, json);

            ToolTests.AssertToolError(result, "Material not found");
        }
    }
}
