using System;
using Godot;

namespace VoxelEditorForGodotDotNet.Core
{
    public struct VoxelData
    {
        // Color8 handles 0-255 byte inputs
        public readonly static VoxelData Empty = new VoxelData(-1.0f, Color.Color8(255, 255, 255, 255));

        public VoxelData(float weight, Color color)
        {
            WeightInsideIsPositive = Mathf.Clamp(weight, -1f, 1f);
            Color = color;
        }

        public float WeightInsideIsPositive { get; private set; }
        public float DistanceOutsideIsPositive => -WeightInsideIsPositive;

        public Color Color { get; private set; }

        // Using R8, G8, B8, A8 for 0-255 byte outputs in string format
        public override string ToString() 
            => $"(w: {WeightInsideIsPositive}, (r: {Color.R8}, g: {Color.G8}, b: {Color.B8}, a: {Color.A8}))";

        public VoxelData WithWeightInsideIsPositive(float weightInsideIsPositive) => new VoxelData(weightInsideIsPositive, Color);
        public VoxelData WithDistanceOutsideIsPositive(float distanceOutsideIsPositive) => new VoxelData(-distanceOutsideIsPositive, Color);
        public VoxelData WithColor(Color color) => new VoxelData(WeightInsideIsPositive, color);

        public void Serialize(byte[] dst, int dstOffset)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(WeightInsideIsPositive), 0, dst, dstOffset, 4);
            
            // Access byte representations directly via R8/G8/B8/A8
            dst[dstOffset + 4] = (byte)Color.R8;
            dst[dstOffset + 5] = (byte)Color.G8;
            dst[dstOffset + 6] = (byte)Color.B8;
            dst[dstOffset + 7] = (byte)Color.A8;
        }
    }
}