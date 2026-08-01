using Godot;
using VoxelEditorForGodotDotNet.Core;
using VoxelEditorForGodotDotNet.EditTools;

namespace VoxelEditorForGodotDotNet.Core
{
    public partial class VoxelEditor : Node
    {
        [Export] private VoxelController controller;
        [Export] private SphereEditShape sphereShape;

        [Export] private Vector3I gridSize = new Vector3I(32, 32, 32);
        [Export] private float sphereScale = 12.0f;

        public override void _Ready()
        {
            if (controller == null)
            {
                GD.PushError("VoxelEditor: Controller is not assigned in the Inspector!");
                return;
            }

            if (sphereShape == null)
            {
                GD.PushError("VoxelEditor: SphereEditShape is not assigned in the Inspector!");
                return;
            }

            // 1. Initialize the voxel grid (resolution, setEmpty = true, skipViewSetup = false)
            controller.Initialize(gridSize.X, gridSize.Y, gridSize.Z, setEmpty: true, skipViewSetup: false);

            // 2. Position and scale the sphere edit shape in the center of the grid
            Vector3 center = new Vector3(gridSize.X / 2f, gridSize.Y / 2f, gridSize.Z / 2f);
            
            sphereShape.Position = center;
            sphereShape.Scale = new Vector3(sphereScale, sphereScale, sphereScale);

            // 3. Create the modifier and apply it via the ModificationManager
            var addModifier = new BaseModificationTools.AddShapeModifier();
            controller.ModificationManager.ModifyData(sphereShape, addModifier);

			GD.Print("Done");
        }
    }
}