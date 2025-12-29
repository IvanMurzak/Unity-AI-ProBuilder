/*
Copyright (c) 2025 Ivan Murzak
Licensed under the Apache License, Version 2.0.
See the LICENSE file in the project root for more information.
*/

#nullable enable

using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginPromptType]
    public partial class Prompt_ProBuilder
    {
        [McpPluginPrompt(Name = "generate-probuilder-uvs", Role = Role.User)]
        [Description("Generate UVs for a ProBuilder mesh using auto/box/planar projection.")]
        public string GenerateProBuilderUvs()
        {
            return "Generate UVs on a ProBuilder mesh using the probuilder-generate-uvs tool. Ask for the target GameObject, optional face indices or face direction, projection mode, and whether to apply to all faces when none are specified. Report the updated mesh details.";
        }
    }
}
