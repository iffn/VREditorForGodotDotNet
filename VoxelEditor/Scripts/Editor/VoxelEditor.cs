using Godot;
using VoxelEditorForGodotDotNet.Core;
using VoxelEditorForGodotDotNet.EditTools;
using VoxelEditorForGodotDotNet.IO;
using static VoxelEditorForGodotDotNet.EditTools.BaseModificationTools;

namespace VoxelEditorForGodotDotNet.Core
{
	[GlobalClass]
	public partial class VoxelEditor : Node
	{
		[Export] private VoxelController controller;
		[Export] private Node3D shapeHolder;
		[Export] private SphereEditShape sphereShape;
		[Export] public XRController3D RightController { get; set; }

		[Export] private Vector3I gridSize = new Vector3I(32, 32, 32);
		[Export] private float shapeScale = 12.0f;
		[Export] private float scaleSpeed = 1f;

		// Assign your .json file here in the Inspector via drag-and-drop
		[Export] private Json voxelWorldFile;

		// --- Auto-Save Settings ---
		[Export] private bool enableAutoSave = true;
		[Export] private float autoSaveDelaySeconds = 2.0f; // Save 2 seconds after finishing paint stroke

		bool paintingActive = true;
		public bool PaintingActive
		{
			get
			{
				return paintingActive;
			}
			set
			{
				paintingActive = value;
				shapeHolder.Visible = value;
			}
		}
		public bool autosaveEnabled = true;
		
		private bool isDirty = false;
		private bool wasPaintingLastFrame = false;
		private float autoSaveTimer = 0f;

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

			// Try loading existing JSON data on startup
			bool loadedSuccessfully = false;
			if (voxelWorldFile != null)
			{
				loadedSuccessfully = LoadWorld();
			}

			// Fall back to default initialization if JSON was missing/empty
			if (!loadedSuccessfully)
			{
				GD.Print("VoxelEditor: Initializing default empty grid.");
				controller.Initialize(gridSize.X, gridSize.Y, gridSize.Z, setEmpty: true, skipViewSetup: false);
			}

			GD.Print("Voxel editor setup complete");
		}

		public override void _Notification(int what)
		{
			// Save on exit request or scene removal
			if (what == NotificationWMCloseRequest || what == NotificationExitTree)
			{
				if (isDirty)
				{
					GD.Print("VoxelEditor: Unsaved changes detected on exit. Saving...");
					SaveWorld();
				}

				if (what == NotificationWMCloseRequest)
				{
					GetTree().Quit();
				}
			}
		}

		public override void _Process(double delta)
		{
			if(paintingActive)
				HandlePainting((float)delta);
			
			if(autosaveEnabled)
				HandleAutoSaveTimer((float)delta);
		}

		private void HandlePainting(float delta)
		{
			if (RightController == null) return;

			bool rightModifier = RightController.IsButtonPressed("grip_click");
			bool shouldPaint = RightController.IsButtonPressed("trigger_click");

			float scaleInput = RightController.GetVector2("primary").Y;
			shapeHolder.Scale *= 1f + scaleInput * scaleSpeed * delta;

			// Active modification
			if (shouldPaint)
			{
				IVoxelModifier modifier = rightModifier ? new SubtractShapeModifier() : new AddShapeModifier();
				controller.ModificationManager.ModifyData(sphereShape, modifier);

				// Mark grid as modified
				isDirty = true;
				autoSaveTimer = autoSaveDelaySeconds; // Reset timer while actively painting
			}

			// Detect stroke release (user released the trigger this frame)
			if (wasPaintingLastFrame && !shouldPaint)
			{
				GD.Print("VoxelEditor: Edit stroke finished.");
			}

			wasPaintingLastFrame = shouldPaint;
		}

		private void HandleAutoSaveTimer(float delta)
		{
			if (!enableAutoSave || !isDirty) return;

			// Count down timer when not actively painting
			if (!wasPaintingLastFrame)
			{
				autoSaveTimer -= delta;

				if (autoSaveTimer <= 0f)
				{
					GD.Print("VoxelEditor: Auto-saving changes...");
					SaveWorld();
				}
			}
		}

		/// <summary>
		/// Saves the current 3D voxel grid to the assigned JSON file.
		/// </summary>
		public void SaveWorld()
		{
			if (voxelWorldFile == null)
			{
				GD.PushWarning("VoxelEditor: Cannot save. No JSON file assigned to voxelWorldFile!");
				return;
			}

			if (controller?.VoxelDataReference == null)
			{
				GD.PushWarning("VoxelEditor: Cannot save. Voxel data reference is null!");
				return;
			}

			VoxelJsonSaveData.SaveData(voxelWorldFile, controller.VoxelDataReference);
			isDirty = false; // Reset dirty flag
		}

		/// <summary>
		/// Loads the 3D voxel grid from the assigned JSON file.
		/// </summary>
		public bool LoadWorld()
		{
			if (voxelWorldFile == null)
			{
				GD.PushWarning("VoxelEditor: Cannot load. No JSON file assigned to voxelWorldFile!");
				return false;
			}

			VoxelData[,,] loadedValues = VoxelJsonSaveData.LoadData(voxelWorldFile);

			if (loadedValues != null && loadedValues.GetLength(0) > 0 && loadedValues.GetLength(1) > 0 && loadedValues.GetLength(2) > 0)
			{
				if (!controller.IsInitialized)
				{
					controller.Initialize(loadedValues.GetLength(0), loadedValues.GetLength(1), loadedValues.GetLength(2), setEmpty: false, skipViewSetup: false);
				}

				controller.SetAllGridDataAndUpdateMesh(loadedValues);
				GD.Print("VoxelEditor: World loaded successfully from JSON.");
				isDirty = false;
				return true;
			}

			GD.Print("VoxelEditor: JSON data is empty or uninitialized.");
			return false;
		}
	}
}
