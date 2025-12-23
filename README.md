<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-AI-ProBuilder">Unity AI ProBuilder</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.probuilder?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.probuilder/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-AI-ProBuilder 'Stars')](https://github.com/IvanMurzak/Unity-AI-ProBuilder/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-AI-ProBuilder?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Stats" src="https://github.com/IvanMurzak/Unity-AI-ProBuilder/raw/main/docs/img/ai-probuilder-glitch.gif"/>

AI Tools for Unity ProBuilder. Let AI to work with ProBuilder features. Fast level prototyping. It is constructed on top of [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/releases/download/1.0.2/AI-ProBuilder-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                               | Playmode                                                                                                                                                                               | Standalone                                                                                                                                                                               |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.61f1   | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2022-3-61f1-editmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2022-3-61f1-playmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2022-3-61f1-standalone)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) |
| 2023.2.20f1   | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2023-2-20f1-editmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2023-2-20f1-playmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-2023-2-20f1-standalone)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml) |
| 6000.2.3f1    | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-6000-2-3f1-editmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-6000-2-3f1-playmode)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-ProBuilder/workflows/release/badge.svg?job=test-unity-6000-2-3f1-standalone)](https://github.com/IvanMurzak/Unity-AI-ProBuilder/actions/workflows/release.yml)  |

## AI ProBuilder Tools

Core tools:
- `probuilder-create-shape` - Create primitive shapes (cube, sphere, cylinder, etc.)
- `probuilder-get-mesh-info` - Read mesh data (faces, vertices, edges)
- `probuilder-extrude` - Extrude faces with various methods
- `probuilder-bevel` - Bevel edges
- `probuilder-delete-faces` - Delete faces by index or direction
- `probuilder-set-face-material` - Apply materials to specific faces

Mesh operations:
- `probuilder-flip-normals` - Reverse face normals
- `probuilder-set-pivot` - Change mesh pivot point
- `probuilder-merge-objects` - Combine multiple ProBuilder meshes

Edge operations:
- `probuilder-subdivide-edges` - Add vertices to edges
- `probuilder-connect-edges` - Connect edges with new geometry
- `probuilder-bridge` - Bridge between edge selections

Advanced:
- `probuilder-create-poly-shape` - Create custom polygon-based meshes


## Installation

### Option 1 - Installer

- **[⬇️ Download Installer](https://github.com/IvanMurzak/Unity-AI-ProBuilder/releases/download/1.0.2/AI-ProBuilder-Installer.unitypackage)**
- **📂 Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [⬇️ Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.probuilder
```
