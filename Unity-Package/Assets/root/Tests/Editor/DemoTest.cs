/*
┌──────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                     │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ProBuilder)  │
│  Copyright (c) 2025 Ivan Murzak                                          │
│  Licensed under the Apache License, Version 2.0.                         │
│  See the LICENSE file in the project root for more information.          │
└──────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using com.IvanMurzak.Unity.MCP.Editor.API;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests
{
    public class ProBuilderToolRegistrationTests
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
        public void ErrorMessagesStayReadable()
        {
            var message = Tool_ProBuilder.Error.InvalidFaceIndex(2, 3);
            StringAssert.Contains("out of range", message);
        }

        private static MethodInfo[] GetToolMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => HasAttributeByName(method, "McpPluginToolAttribute"))
                .ToArray();
        }

        private static bool HasAttributeByName(MemberInfo member, string attributeName)
        {
            return member.GetCustomAttributes(false).Any(attribute =>
                string.Equals(attribute.GetType().Name, attributeName, StringComparison.Ordinal));
        }
    }
}
