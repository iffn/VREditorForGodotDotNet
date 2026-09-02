using System;
using System.Threading.Tasks;
using Godot;
using static VoxelEditorForGodotDotNet.EditTools.BaseModificationTools;
using VoxelEditorForGodotDotNet.Core;

namespace VoxelEditorForGodotDotNet.EditTools
{
    public class ModificationManager
    {
        private readonly VoxelController linkedController;
        private readonly Node3D linkedControllerNode;

        public ModificationManager(VoxelController linkedController)
        {
            this.linkedController = linkedController;
            linkedControllerNode = linkedController;
        }

        public void ModifyData(EditShape shape, IVoxelModifier modifier)
        {
            (Vector3I minGrid, Vector3I maxGrid) = CalculateGridBoundsClamped(shape);

            // Modify model
            ModifyModel(shape, modifier, minGrid, maxGrid, linkedController.GetVoxelWithoutClamp, linkedController.SetDataPointWithoutSettingItToDirty);

            // Mark affected chunks as dirty
            linkedController.MarkRegionDirty(minGrid, maxGrid);

            // Update affected chunk meshes
            linkedController.UpdateAffectedChunks(minGrid, maxGrid);
        }

        public void SetPreviewDisplayState(VoxelChunkPreview.PreviewDisplayStates newState)
        {
            linkedController.Preview?.SetPreviewDisplayState(newState);
        }

        public void ShowPreviewData(EditShape shape, IVoxelModifier modifier)
        {
            (Vector3I minGrid, Vector3I maxGrid) = CalculateGridBoundsClamped(shape);

            linkedController.SetupPreviewZone(minGrid, maxGrid);

            // Modify the model
            ModifyModel(shape, modifier, minGrid, maxGrid, linkedController.GetVoxelWithoutClamp, linkedController.SetPreviewDataPoint);

            linkedController.UpdatePreviewShape();
        }

        public void ApplyPreviewChanges()
        {
            linkedController.ApplyPreviewChanges(); // Takes care of setting stuff to dirty
        }

        public (Vector3I minGrid, Vector3I maxGrid) CalculateGridBoundsClamped(EditShape shape)
        {
            // Precompute transformation matrices (Global space to local grid space)
            Transform3D worldToGrid = linkedControllerNode.GlobalTransform.AffineInverse();

            // Precompute shape transformation parameters
            shape.PrepareParameters(linkedControllerNode.GlobalTransform);

            // Get shape bounds in world space and transform to grid space
            (Vector3 worldMin, Vector3 worldMax) = shape.GetWorldBoundingBox();
            Vector3 gridMin = worldToGrid * worldMin;
            Vector3 gridMax = worldToGrid * worldMax;

            // Ensure correct min/max per component in case of inverted bounds
            Vector3 gridLower = gridMin.Min(gridMax);
            Vector3 gridUpper = gridMin.Max(gridMax);

            // Expand bounds by 1 unit due to rounding and clamp to valid grid range
            Vector3I unclampedMin = new Vector3I(
                Mathf.FloorToInt(gridLower.X) - 1,
                Mathf.FloorToInt(gridLower.Y) - 1,
                Mathf.FloorToInt(gridLower.Z) - 1
            );

            Vector3I unclampedMax = new Vector3I(
                Mathf.CeilToInt(gridUpper.X) + 1,
                Mathf.CeilToInt(gridUpper.Y) + 1,
                Mathf.CeilToInt(gridUpper.Z) + 1
            );

            Vector3I minGrid = new Vector3I(
                Mathf.Max(0, unclampedMin.X),
                Mathf.Max(0, unclampedMin.Y),
                Mathf.Max(0, unclampedMin.Z)
            );

            Vector3I maxGrid = new Vector3I(
                Mathf.Min(linkedController.MaxGrid.X, unclampedMax.X),
                Mathf.Min(linkedController.MaxGrid.Y, unclampedMax.Y),
                Mathf.Min(linkedController.MaxGrid.Z, unclampedMax.Z)
            );

            return (minGrid, maxGrid);
        }

        public void ModifySingleVoxel(int x, int y, int z, VoxelData newValue)
        {
            linkedController.VoxelDataReference[x, y, z] = newValue;

            Vector3I point = new Vector3I(x, y, z);

            linkedController.MarkRegionDirty(point);
            linkedController.UpdateAffectedChunks(point);
        }

        private void ModifyModel(EditShape shape, IVoxelModifier modifier, Vector3I minGrid, Vector3I maxGrid, Func<int, int, int, VoxelData> getDataPoint, Action<int, int, int, VoxelData> setDataPoint)
        {
            // Fix: Extract scale from the Basis or directly from the Node3D
            float worldToGridScaleFactor = linkedControllerNode.Scale.Length(); 
            // Alternatively: linkedControllerNode.GlobalTransform.Basis.GetScale().Length();

            // Parallel voxel processing across X slice
            Parallel.For(minGrid.X, maxGrid.X + 1, x =>
            {
                for (int y = minGrid.Y; y <= maxGrid.Y; y++)
                {
                    for (int z = minGrid.Z; z <= maxGrid.Z; z++)
                    {
                        Vector3 gridPoint = new Vector3(x, y, z);

                        // Calculate distance using the shape's transformation
                        float distanceOutsideIsPositive = shape.OptimizedDistanceOutsideIsPositive(gridPoint);

                        // Modify the voxel value
                        VoxelData newValue = modifier.ModifyVoxel(x, y, z, linkedController.VoxelDataReference[x, y, z], distanceOutsideIsPositive);
                        setDataPoint(x, y, z, newValue);
                    }
                }
            });
        }
    }
}