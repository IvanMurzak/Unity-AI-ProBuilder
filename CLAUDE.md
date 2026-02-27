# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity AI ProBuilder is a Unity package (`com.ivanmurzak.unity.mcp.probuilder`) that exposes ProBuilder mesh operations as MCP (Model Context Protocol) tools, allowing AI assistants to create and manipulate 3D geometry in Unity through natural language commands. It is built on top of the [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) platform (`com.ivanmurzak.unity.mcp`).

## Repository Structure

```
Unity-Package/Assets/root/     # The distributable Unity package source
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

## Version Management

The version is defined in `Unity-Package/Assets/root/package.json` and must be kept in sync with `Installer/Assets/com.IvanMurzak/AI ProBuilder Installer/Installer.cs` and the download URLs in both `README.md` files.

**Bump version across all files:**
```powershell
.\commands\bump-version.ps1 -NewVersion "1.0.35"
# Preview only (no changes applied):
.\commands\bump-version.ps1 -NewVersion "1.0.35" -WhatIf
```

**Update the Unity-MCP dependency to latest:**
```powershell
.\commands\update-ai-game-developer.ps1
```

Releases are triggered by pushing to `main` — the CI creates a GitHub release when the version tag does not yet exist.

## Architecture

### Tool Pattern

All ProBuilder tools are partial methods on a single class `Tool_ProBuilder` in `Unity-Package/Assets/root/Editor/Scripts/Tools/`. Each `.cs` file contributes one tool operation.

Key conventions:
- The class is decorated with `[McpPluginToolType]` (from `com.IvanMurzak.McpPlugin`)
- Each method is decorated with `[McpPluginTool("tool-id", Title = "...")]` and `[Description(...)]`
- All Unity API calls must be wrapped in `MainThread.Instance.Run(() => { ... })` since MCP calls arrive off the main thread
- GameObjects are referenced via `GameObjectRef` (resolved with `.FindGameObject(out var error)`)
- Each tool method returns a dedicated response class defined as a nested class in the same file (under `#region ... Response Classes`)

### Face Selection

`FaceSelectionHelper` (in `ProBuilder.FaceSelectionHelper.cs`) provides two selection modes used by multiple tools:
- **By index**: direct `int[]` face indices
- **By direction** (`FaceDirection` enum): semantic Up/Down/Left/Right/Forward/Back, using dot product threshold of 0.7 (~45°)

Tools that accept face selection expose both `faceIndices` and `faceDirection` parameters — exactly one must be provided.

### Error Handling

Common error messages are centralized in the `Tool_ProBuilder.Error` static class (`ProBuilder.cs`). Tools throw `Exception` with these messages; the MCP framework serializes them back to the AI client.

After any mesh modification, always call:
```csharp
proBuilderMesh.ToMesh();
proBuilderMesh.Refresh();
EditorUtility.SetDirty(proBuilderMesh);
EditorUtils.RepaintAllEditorWindows();
```

### Assembly Definitions

| Assembly | Contents |
|---|---|
| `com.IvanMurzak.Unity.MCP.ProBuilder.Editor` | All tool scripts (Editor platform only) |
| `com.IvanMurzak.Unity.MCP.ProBuilder.Runtime` | Empty — placeholder for any future runtime code |

The Editor assembly references `com.IvanMurzak.Unity.MCP.Editor`, `com.IvanMurzak.Unity.MCP.Runtime`, Unity.ProBuilder, and Unity.ProBuilder.Editor.

## CI/CD

GitHub Actions (`release.yml`) triggers on push to `main`:
1. Reads version from `Unity-Package/Assets/root/package.json`
2. Skips if the version tag already exists on GitHub
3. Builds the `.unitypackage` installer using Unity 2022.3.62f3
4. Runs EditMode, PlayMode, and Standalone tests for each supported Unity version
5. Creates a GitHub release and publishes to OpenUPM

Test projects in `Unity-Tests/` pull the package from the local `Unity-Package/` directory and are used exclusively by CI.

## Adding a New Tool

1. Create `Unity-Package/Assets/root/Editor/Scripts/Tools/ProBuilder.<ToolName>.cs`
2. Declare `public partial class Tool_ProBuilder` in namespace `com.IvanMurzak.Unity.MCP.Editor.API`
3. Add a `public const string <ToolName>ToolId = "probuilder-<tool-name>";` constant
4. Implement the method with `[McpPluginTool(...)]` and `[Description(...)]` attributes
5. Wrap all Unity API calls in `MainThread.Instance.Run()`
6. Define a response class nested in a `#region <ToolName> Response Classes` block
