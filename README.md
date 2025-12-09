<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-AI-ProBuilder">Unity AI ProBuilder</a></h1>

<img width="100%" alt="Stats" src="https://github.com/IvanMurzak/Unity-AI-ProBuilder/raw/main/docs/img/ai-probuilder-glitch.gif"/>

AI Tools for Unity ProBuilder. Let AI to work with ProBuilder features. Fast level prototyping.

## AI ProBuilder Tools

Core tools:
- `ProBuilder_CreateShape` - Create primitive shapes (cube, sphere, cylinder, etc.)
- `ProBuilder_GetMeshInfo` - Read mesh data (faces, vertices, edges)
- `ProBuilder_Extrude` - Extrude faces with various methods
- `ProBuilder_Bevel` - Bevel edges
- `ProBuilder_DeleteFaces` - Delete faces by index or direction
- `ProBuilder_SetFaceMaterial` - Apply materials to specific faces

Mesh operations:
- `ProBuilder_FlipNormals` - Reverse face normals
- `ProBuilder_SetPivot` - Change mesh pivot point
- `ProBuilder_MergeObjects` - Combine multiple ProBuilder meshes

Edge operations:
- `ProBuilder_SubdivideEdges` - Add vertices to edges
- `ProBuilder_ConnectEdges` - Connect edges with new geometry
- `ProBuilder_Bridge` - Bridge between edge selections

Advanced:
- `ProBuilder_CreatePolyShape` - Create custom polygon-based meshes


## Installation

### Option 1 - Installer

- **[⬇️ Download Installer](https://github.com/IvanMurzak/Unity-AI-ProBuilder/releases/download/1.0.0/Unity-AI-ProBuilder-Installer.unitypackage)**
- **📂 Import installer into Unity project**
  > - You can double-click on the file - Unity will open it automatically
  > - OR: Open Unity Editor first, then click on `Assets/Import Package/Custom Package`, and choose the file

### Option 2 - OpenUPM-CLI

- [⬇️ Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- 📟 Open the command line in your Unity project folder

```bash
openupm add com.ivanmurzak.unity.mcp.probuilder
```