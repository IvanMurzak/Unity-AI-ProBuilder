# AGENTS.md — Unity-AI-ProBuilder

Unity package `com.ivanmurzak.unity.mcp.probuilder` — exposes ProBuilder mesh operations so AI can create and
manipulate 3D geometry through Unity-MCP's RQL tools (no extension MCP tools). An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the user-facing tool list is in `README.md`.

## Layout

- `Unity-Package/` — Unity project hosting the package; only `Packages/com.ivanmurzak.unity.mcp.probuilder/`
  ships.
  - `package.json` — the version source of truth and the dependency pins.
  - `Editor/Scripts/Rql/` — the `ProBuilderModule` (IRqlModule: `probuilder` source,
    `probuilder.*` functions, the ProBuilderMesh adapter, face / material views, the agent guide); replaces the former
    `probuilder-*` tools. New operations are adapter methods or `ProBuilderEditorApi` `[RqlCallable]` methods.
  - `Runtime/` — runtime assembly (currently empty).
  - `Tests/Editor/Rql/` (EditMode, `ProBuilderTestBase` + one file per adapter / view / source; a test runs every
    guide example) and `Tests/Runtime/` (PlayMode).
- `Unity-Tests/<unity-version>/` — one test project per supported Unity version, consuming the package via
  `file:`. A new version must also be added to `test_pull_request.yml` and `release.yml`.
- `Installer/` — Unity project that exports the `.unitypackage` installer
  (`Assets/com.IvanMurzak/<name> Installer/`).
- `commands/` — PowerShell helpers (below) and `check-versions.py` (synced in by the release cascade — do not
  hand-edit; CI runs it as the `version-consistency` gate).
- `.github/workflows/` — `test_pull_request.yml` (PR test matrix), `test_unity_plugin.yml` (reusable test
  job), `release.yml`, `bump_version.yml`, `claude.yml` (`@claude` mentions).
- `docs/` — user docs: `Deploy-{OpenUPM,GitHub,npmjs}.md`, `Manual-Package-Rename.md`, `openupm-signing.md`,
  `img/`, `promo/`.

## Commands (PowerShell, from the repo root)

```powershell
./commands/bump-version.ps1 -NewVersion "x.y.z"   # package.json + Installer.cs in lock-step; -WhatIf to preview
./commands/get-version.ps1
./commands/update-ai-game-developer.ps1           # bump the com.ivanmurzak.unity.mcp pin; -WhatIf to preview
```

Tests run inside the Unity Editor (NUnit + `[UnityTest]`); CI runs them with `game-ci/unity-test-runner` in
EditMode / PlayMode / Standalone for every Unity version. `release.yml` releases on push to `main` only when
the `package.json` version has no tag yet (installer, signed UPM tarball for OpenUPM, test matrix, tag, GitHub
Release); dispatch `bump_version.yml` to bump in CI. Under `pull_request_target`, tests need the
maintainer-applied `ci-ok` label and abort if the PR touches `.github/workflows/`.

## Critical invariants

- **Main thread only.** RQL runs on the main thread (Unity-MCP dispatches the RQL tools there).
- **After a mesh edit** call `ProBuilderOps.Rebuild(mesh)` (`ToMesh()` -> `Refresh()` -> `SetDirty`, and the mesh is
  rebuilt again after Undo / Redo — ProBuilder only does that for selected meshes). The engine records the call target
  for Undo; record any other object you change (renderer, transform) with `Undo.RecordObject` first.
- **Validate, never clamp.** Face indices, edges (must be edges of the mesh), amounts and counts are checked before the
  mesh changes; invalid input throws with the valid range.
- **Selections** are face index lists and `[a, b]` vertex pairs; `ProBuilderOps.Facing` (dot-product threshold 0.7,
  local space) backs `probuilder.facing` and the face views' `direction`.
- **Name clash.** Inside `com.IvanMurzak.Unity.MCP.*` namespaces, alias ProBuilder types inside the namespace block
  (`using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;`).
- Every C# file starts with the copyright box header and `#nullable enable`.
