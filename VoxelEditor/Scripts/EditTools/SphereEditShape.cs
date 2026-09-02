using Godot;

namespace VoxelEditorForGodotDotNet.EditTools
{
	[Tool]
	public partial class SphereEditShape : EditShape, IPlaceableByClick
	{
		public EditShape AsEditShape => this;

		public override OffsetTypes OffsetType => OffsetTypes.TowardsNormal;

		protected override float DistanceOutsideIsPositive(Vector3 localPoint)
		{
			// Transform the point into the shape's local space
			return SDFMath.ShapesDistanceOutsideIsPositive.Sphere(localPoint, 0.5f);
		}

		public override (Vector3 minOffset, Vector3 maxOffset) GetLocalBoundingBox()
		{
			return (-0.5f * Vector3.One, 0.5f * Vector3.One);
		}
	}
}
