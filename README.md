# VR Voxel editor for Godot DotNet

## Based on
- https://github.com/iffn/VRMarchingCubeEditorForUnity
- https://github.com/iffn/MarchingCubeEditorForUnity

## Controls
- Left stick: Walk
- Right stick horizontal: Turn
- Right stick vertical: Scale tool size
- Right trigger: Add while held
- Right trigger + Right grip: Remove while held

## Setup
- Get Godot DotNet version (Tested with Godot 4.7.1.stable.mono)
- Create a new Godot project (Designed with Forward+ render pipeline)
- In the Asset Store, get `Godot XR Tools` https://store.godotengine.org/asset/godot-xr/godot-xr-tools/
- Project -> Project Settings...
  - Plugins -> Enable `Godot XR Tools`
  - General -> XR - OpenXR -> Enabled to On
  - General -> XR - Shaders -> Enabled to On (Restart Godot as prompted)
- Add this repository to the main folder.
- Create a C# script in the asset folder and delete it for Godot to compile .net (...)
- Select a test scene
- Start VR
- Run current scene
