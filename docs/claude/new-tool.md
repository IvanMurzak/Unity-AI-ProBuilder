# Adding a New Tool

1. Create `Unity-Package/Assets/root/Editor/Scripts/Tools/ProBuilder.<ToolName>.cs`
2. Declare `public partial class Tool_ProBuilder` in namespace `com.IvanMurzak.Unity.MCP.Editor.API`
3. Add a `public const string <ToolName>ToolId = "probuilder-<tool-name>";` constant
4. Implement the method with `[McpPluginTool(...)]` and `[Description(...)]` attributes
5. Wrap all Unity API calls in `MainThread.Instance.Run()`
6. Define a response class nested in a `#region <ToolName> Response Classes` block
