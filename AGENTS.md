# AGENTS.md — Unity-AI-ProBuilder

Unity package `com.ivanmurzak.unity.mcp.probuilder` — exposes ProBuilder mesh operations as MCP tools so AI
can create and manipulate 3D geometry (`probuilder-*`). An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the user-facing tool list is in `README.md`.

## Layout

- `Unity-Package/` — Unity project hosting the package; only `Packages/com.ivanmurzak.unity.mcp.probuilder/`
  ships.
  - `package.json` — the version source of truth and the dependency pins.
  - `Editor/Scripts/Tools/` — the MCP tools (`Tool_ProBuilder`); new tools go here.
  - `Runtime/` — runtime assembly (currently empty).
  - `Tests/Editor/` (EditMode) and `Tests/Runtime/` (PlayMode).
- `Unity-Tests/<unity-version>/` — one test project per supported Unity version, consuming the package via
  `file:`. A new version must also be added to `test_pull_request.yml`, `release.yml` and
  `open-all-projects-*`.
- `Installer/` — Unity project that exports the `.unitypackage` installer
  (`Assets/com.IvanMurzak/<name> Installer/`).
- `commands/` — the scripts below, plus `open-all-projects-unix.sh` and `check-versions.py` (synced in by the
  release cascade — do not hand-edit; CI runs it as the `version-consistency` gate).
- `.github/workflows/` — `test_pull_request.yml` (PR test matrix), `test_unity_plugin.yml` (reusable test
  job), `release.yml`, `bump_version.yml`, `claude.yml` (`@claude` mentions).
- `docs/` — user docs: `Deploy-{OpenUPM,GitHub,npmjs}.md`, `Manual-Package-Rename.md`, `openupm-signing.md`,
  `img/`, `promo/`.

## Commands (PowerShell, from the repo root)

```powershell
./commands/bump-version.ps1 -NewVersion "x.y.z"   # package.json + Installer.cs in lock-step; -WhatIf to preview
./commands/get-version.ps1
./commands/update-ai-game-developer.ps1           # bump the com.ivanmurzak.unity.mcp pin; -WhatIf to preview
./commands/open-all-projects-windows.ps1          # open the Unity projects (generates .meta files)
```

Tests run inside the Unity Editor (NUnit + `[UnityTest]`); CI runs them with `game-ci/unity-test-runner` in
EditMode / PlayMode / Standalone for every Unity version. `release.yml` releases on push to `main` only when
the `package.json` version has no tag yet (installer, signed UPM tarball for OpenUPM, test matrix, tag, GitHub
Release); dispatch `bump_version.yml` to bump in CI. Under `pull_request_target`, tests need the
maintainer-applied `ci-ok` label and abort if the PR touches `.github/workflows/`.

## Critical invariants

- **Main thread only.** Every Unity API call inside a tool MUST be wrapped in
  `MainThread.Instance.Run(() => { ... })` — MCP calls arrive off the main thread; only argument validation
  runs outside.
- **Tool shape.** All tools are methods of one `[AiToolType] partial class Tool_ProBuilder` (namespace
  `com.IvanMurzak.Unity.MCP.Editor.API`), one operation per file `ProBuilder.<ToolName>.cs`, with a
  `public const string <ToolName>ToolId = "probuilder-<tool-name>"`, `[AiTool(...)]`, `[AiSkillDescription]` /
  `[AiSkillBody]` and `[Description]`. Each tool returns its own response class, nested in the same file under
  `#region <ToolName> Response Classes`. GameObjects are referenced via `GameObjectRef`
  (`.FindGameObject(out var error)`).
- **Errors.** Common messages live in `Tool_ProBuilder.Error` (`ProBuilder.cs`); tools throw `Exception` with
  them and the MCP framework returns them to the client.
- **After a mesh edit** always: `ToMesh()` -> `Refresh()` -> `EditorUtility.SetDirty()` ->
  `EditorUtils.RepaintAllEditorWindows()`.
- **Face selection** (`ProBuilder.FaceSelectionHelper.cs`): tools accept `faceIndices` OR `faceDirection`
  (Up/Down/Left/Right/Forward/Back, dot-product threshold 0.7) — exactly one.
- **EntityId split.** Unity 6.5+ returns `UnityEngine.EntityId` from `GameObject.GetEntityId()`; pre-6.5
  returns `int` from `GetInstanceID()`. Files surfacing an instance ID ship as a `*.cs`
  (`#if UNITY_6000_5_OR_NEWER`) + `*.pre-Unity.6.5.cs` (`#if !UNITY_6000_5_OR_NEWER`) pair — e.g.
  `ProBuilder.Extrude.cs` / `ProBuilder.Extrude.pre-Unity.6.5.cs`. Keep both variants in sync.
- Every C# file starts with the copyright box header and `#nullable enable`.
