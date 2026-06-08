# Version Management

The version is defined in `Unity-Package/Packages/com.ivanmurzak.unity.mcp.probuilder/package.json` and must be kept in sync with `Installer/Assets/com.IvanMurzak/AI ProBuilder Installer/Installer.cs` and the download URLs in both `README.md` files.

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
