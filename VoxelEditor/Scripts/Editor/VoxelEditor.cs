using Godot;
using VoxelEditorForGodotDotNet.Core;
using VoxelEditorForGodotDotNet.EditTools;

namespace VoxelEditorForGodotDotNet.Core
{
    public partial class VoxelEditor : Node
    {
        [Export] private VoxelController controller;
        [Export] private SphereEditShape sphereShape;
        [Export] public XRController3D RightController { get; set; }

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

			GD.Print("Setup complete");
        }

        public override void _Process(double delta)
        {
            if (RightController == null) return;

            // Check continuous analog value (0.0 to 1.0)
            float triggerValue = RightController.GetFloat("trigger");

            // Check if digital trigger click is currently held down
            bool isTriggerPressed = RightController.IsButtonPressed("trigger_click");

            if (triggerValue > 0.5f)
            {
                var addModifier = new BaseModificationTools.AddShapeModifier();
                controller.ModificationManager.ModifyData(sphereShape, addModifier);
            }
        }
    }
}