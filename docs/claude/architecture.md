# Architecture

## Repository Structure

```
Unity-Package/Packages/com.ivanmurzak.unity.mcp.probuilder/     # The distributable Unity package source
  Editor/Scripts/Tools/        # All MCP tool implementations (Editor-only assembly)
  Runtime/                     # Empty runtime assembly placeholder
  Tests/                       # Test stubs (real tests are in Unity-Tests/)
  package.json                 # Package manifest and version source of truth

Unity-Tests/                   # Unity test projects (one per supported Unity version)
  2022.3.62f3/
  2023.2.22f1/
  6000.3.1f1/

Installer/                     # Unity project that builds the .unitypackage installer

commands/                      # PowerShell utility scripts
  bump-version.ps1             # Update version across all files
  get-version.ps1              # Print current package version
  update-ai-game-developer.ps1 # Sync com.ivanmurzak.unity.mcp to latest GitHub release

docs/                          # Deployment guides (GitHub, OpenUPM, npmjs)
```

## Tool Pattern

All ProBuilder tools are partial methods on a single class `Tool_ProBuilder` in `Unity-Package/Packages/com.ivanmurzak.unity.mcp.probuilder/Editor/Scripts/Tools/`. Each `.cs` file contributes one tool operation.

Key conventions:
- The class is decorated with `[McpPluginToolType]` (from `com.IvanMurzak.McpPlugin`)
- Each method is decorated with `[McpPluginTool("tool-id", Title = "...")]` and `[Description(...)]`
- All Unity API calls must be wrapped in `MainThread.Instance.Run(() => { ... })` since MCP calls arrive off the main thread
- GameObjects are referenced via `GameObjectRef` (resolved with `.FindGameObject(out var error)`)
- Each tool method returns a dedicated response class defined as a nested class in the same file (under `#region ... Response Classes`)

## Face Selection

`FaceSelectionHelper` (in `ProBuilder.FaceSelectionHelper.cs`) provides two selection modes used by multiple tools:
- **By index**: direct `int[]` face indices
- **By direction** (`FaceDirection` enum): semantic Up/Down/Left/Right/Forward/Back, using dot product threshold of 0.7 (~45°)

Tools that accept face selection expose both `faceIndices` and `faceDirection` parameters — exactly one must be provided.

## Mesh Modification & Refresh Pattern

After any mesh modification, always call:
```csharp
proBuilderMesh.ToMesh();
proBuilderMesh.Refresh();
EditorUtility.SetDirty(proBuilderMesh);
EditorUtils.RepaintAllEditorWindows();
```

## Assembly Definitions

| Assembly | Contents |
|---|---|
| `com.IvanMurzak.Unity.MCP.ProBuilder.Editor` | All tool scripts (Editor platform only) |
| `com.IvanMurzak.Unity.MCP.ProBuilder.Runtime` | Empty — placeholder for any future runtime code |

The Editor assembly references `com.IvanMurzak.Unity.MCP.Editor`, `com.IvanMurzak.Unity.MCP.Runtime`, Unity.ProBuilder, and Unity.ProBuilder.Editor.
