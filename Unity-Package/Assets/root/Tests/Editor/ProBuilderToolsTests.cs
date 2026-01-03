#nullable enable

using System;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.Unity.MCP.Editor.API;
using com.IvanMurzak.Unity.MCP.Editor.Tests;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.ProBuilder.Editor.Tests
{
    public class ProBuilderToolsTests : BaseTest
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

        private static MethodInfo[] GetToolMethods(Type type)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
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
