# CLAUDE.md

## What this is

Unity package `com.ivanmurzak.unity.mcp.probuilder` — exposes ProBuilder mesh operations as MCP tools so AI assistants can create and manipulate 3D geometry in Unity. Built on top of [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`).

## Build / run

- Package source: `Unity-Package/Assets/root/` (Editor tools under `Editor/Scripts/Tools/`).
- Version source of truth: `Unity-Package/Assets/root/package.json`. Bump with `.\commands\bump-version.ps1 -NewVersion "x.y.z"`.
- Releases trigger on push to `main` when the version tag is new.

## Critical invariants

- **Main thread**: All Unity API calls inside tool methods MUST be wrapped in `MainThread.Instance.Run(() => { ... })` — MCP calls arrive off the main thread.
- After mesh edits: `ToMesh()` -> `Refresh()` -> `EditorUtility.SetDirty()` -> `EditorUtils.RepaintAllEditorWindows()`.
- Face-selection tools accept `faceIndices` OR `faceDirection` — exactly one.

## Find detail in

- `docs/claude/architecture.md` — repo layout, tool pattern, face selection, refresh pattern, assembly defs
- `docs/claude/new-tool.md` — 6-step checklist for adding a tool
- `docs/claude/error-handling.md` — `Tool_ProBuilder.Error` conventions
- `docs/claude/release.md` — version bump + dependency update commands
- `docs/claude/ci.md` — GitHub Actions release pipeline
