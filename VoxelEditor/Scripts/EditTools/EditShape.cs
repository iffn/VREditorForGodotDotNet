using System;
using System.Collections.Generic;
using Godot;

namespace VoxelEditorForGodotDotNet.EditTools
{
    [Tool]
    public abstract partial class EditShape : Node3D
    {
        public enum OffsetTypes
        {
            Vertical,
            TowardsNormal
        }

        public abstract OffsetTypes OffsetType { get; }

        private Transform3D worldToLocalMatrix;

        /// <summary>
        /// Precompute the world-to-local transformation matrix for optimized distance calculations.
        /// </summary>
        public virtual void PrepareParameters(Transform3D gridGlobalTransform)
        {
            // Godot equivalent of transform.worldToLocalMatrix * gridTransform.localToWorldMatrix
            worldToLocalMatrix = GlobalTransform.AffineInverse() * gridGlobalTransform;
        }

        public Vector3 ConvertWorldToOptimizedLocalPoint(Vector3 worldPoint)
        {
            return worldToLocalMatrix * worldPoint;
        }

        /// <summary>
        /// Calculate the distance to the shape surface in world space.
        /// </summary>
        public float OptimizedDistanceOutsideIsPositive(Vector3 worldPoint)
        {
            Vector3 localPoint = ConvertWorldToOptimizedLocalPoint(worldPoint);
            return DistanceOutsideIsPositive(localPoint);
        }

        /// <summary>
        /// Abstract method for distance calculation in local space.
        /// </summary>
        protected abstract float DistanceOutsideIsPositive(Vector3 localPoint);

        public Vector3 WorldPosition => GlobalPosition;
        public Vector3 LocalScale => Scale;

        public virtual void Initialize()
        {
            Visible = true;
        }

        public virtual string HelpText => string.Empty;

        public virtual void HandleInput(InputEvent @event)
        {
            // Godot input processing logic for editor shortcuts/gestures
        }

        /// <summary>
        /// Defines the bounding box of the shape in local space.
        /// </summary>
        public abstract (Vector3 minOffset, Vector3 maxOffset) GetLocalBoundingBox();

        /// <summary>
        /// Transforms the shape's local bounding box to world space AABB.
        /// </summary>
        public (Vector3 worldMin, Vector3 worldMax) GetWorldBoundingBox()
        {
            (Vector3 localMin, Vector3 localMax) = GetLocalBoundingBox();

            Vector3[] corners = new Vector3[8]
            {
                new Vector3(localMin.X, localMin.Y, localMin.Z),
                new Vector3(localMax.X, localMin.Y, localMin.Z),
                new Vector3(localMin.X, localMax.Y, localMin.Z),
                new Vector3(localMax.X, localMax.Y, localMin.Z),
                new Vector3(localMin.X, localMin.Y, localMax.Z),
                new Vector3(localMax.X, localMin.Y, localMax.Z),
                new Vector3(localMin.X, localMax.Y, localMax.Z),
                new Vector3(localMax.X, localMax.Y, localMax.Z)
            };

            Vector3 worldMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 worldMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            Transform3D globalXform = GlobalTransform;

            foreach (Vector3 corner in corners)
            {
                Vector3 worldCorner = globalXform * corner;
                worldMin = worldMin.Min(worldCorner);
                worldMax = worldMax.Max(worldCorner);
            }

            return (worldMin, worldMax);
        }
    }

    public interface IPlaceableByClick
    {
        EditShape AsEditShape { get; }
    }
}