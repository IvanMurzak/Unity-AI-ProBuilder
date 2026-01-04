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
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests.Tools
{
    public class ToolTests : BaseTest
    {
        [Test]
        public void ToolTypeHasMcpAttribute()
        {
            Assert.IsTrue(HasAttributeByName(typeof(Tool_ProBuilder), "McpPluginToolTypeAttribute"));
        }

        [Test]
        public void ToolMethodsAreRegistered()
        {
            var toolMethods = GetToolMethods(typeof(Tool_ProBuilder));
            Assert.IsNotEmpty(toolMethods);
        }

        [Test]
        public void InvalidFaceIndex_ReturnsReadableErrorMessage()
        {
            var message = Tool_ProBuilder.Error.InvalidFaceIndex(2, 3);
            StringAssert.Contains("out of range", message);
        }

        internal static int CreateShapeInstance(string name, ShapeType shapeType = ShapeType.Cube)
        {
            var instanceId = 0;

            var json = JsonTestUtils.Fill(@"{
                ""shapeType"": ""{shapeType}"",
                ""name"": ""{name}""
            }", new Dictionary<string, object?>
            {
                { "{shapeType}", shapeType.ToString() },
                { "{name}", name }
            });

            new CallToolExecutor(
                    typeof(Tool_ProBuilder).GetMethod(nameof(Tool_ProBuilder.CreateShape))!, json)
                .AddChild(new ValidateToolResultExecutor())
                .AddChild(() =>
                {
                    var go = GameObject.Find(name);
                    Assert.IsNotNull(go, "Expected new ProBuilder GameObject.");
                    instanceId = go!.GetInstanceID();
                })
                .Execute();

            return instanceId;
        }

        internal static ProBuilderMesh GetMesh(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, $"Expected GameObject {name}");
            var mesh = go!.GetComponent<ProBuilderMesh>();
            Assert.IsNotNull(mesh, $"Expected ProBuilderMesh on {name}");
            return mesh!;
        }

        internal static MeshRenderer GetMeshRenderer(string name)
        {
            var go = GameObject.Find(name);
            Assert.IsNotNull(go, $"Expected GameObject {name}");
            var renderer = go!.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer, $"Expected MeshRenderer on {name}");
            return renderer!;
        }

        internal static bool IsToolInconclusive(string result, string toolName)
        {
            if (string.IsNullOrWhiteSpace(result))
                return true;

            if (!result.Contains("[Error]", StringComparison.OrdinalIgnoreCase))
                return false;

            return toolName switch
            {
                "Bridge" => true,
                "Bevel" => true,
                _ => false
            };
        }

        internal static MethodInfo[] GetToolMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(method => HasAttributeByName(method, "McpPluginToolAttribute"))
                .ToArray();
        }

        internal static bool HasAttributeByName(MemberInfo member, string attributeName)
        {
            return member.GetCustomAttributes(false).Any(attribute =>
                string.Equals(attribute.GetType().Name, attributeName, StringComparison.Ordinal));
        }

        internal static ResponseData<ResponseCallTool> RunToolRaw(MethodInfo toolMethod, string json)
        {
            var result = new CallToolExecutor(toolMethod, json).Execute();
            Assert.IsNotNull(result, "Tool execution returned null result.");
            return (ResponseData<ResponseCallTool>)result!;
        }

        internal static void AssertToolError(ResponseData<ResponseCallTool> result, string? expectedSubstring = null)
        {
            Assert.IsNotNull(result, "Expected tool result.");

            var message = result.Message ?? string.Empty;
            var isError = result.Status == ResponseStatus.Error
                || (result.Value != null && result.Value.Status == ResponseStatus.Error)
                || message.Contains("[Error]", StringComparison.OrdinalIgnoreCase);

            Assert.IsTrue(isError, $"Expected error response. Status: {result.Status}. Message: {message}");

            if (!string.IsNullOrEmpty(expectedSubstring))
                StringAssert.Contains(expectedSubstring, message);
        }
    }
}
