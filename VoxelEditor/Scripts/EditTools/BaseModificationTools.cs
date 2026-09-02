using Godot;
using VoxelEditorForGodotDotNet.Core;

namespace VoxelEditorForGodotDotNet.EditTools
{
	public class BaseModificationTools
	{
		public interface IVoxelModifier
		{
			VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive);
		}

		public class TerrainConverter : IVoxelModifier
		{
			private readonly float[,] heightLookup;

			public TerrainConverter(float[,] heightLookup)
			{
				this.heightLookup = heightLookup;
			}

			public VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive)
			{
				if (distanceOutsideIsPositive > 0) return currentValue;

				float height = heightLookup[x, z];
				float newValueInsideIsPositive = height - y;

				if (currentValue.WeightInsideIsPositive > 0)
					return currentValue;

				if (newValueInsideIsPositive < -1)
					return currentValue;

				return currentValue.WithWeightInsideIsPositive(newValueInsideIsPositive);
			}
		}

		public class CopyModifier : IVoxelModifier
		{
			private readonly VoxelData[,,] currentDataCopy;
			private readonly Transform3D originalTransformWTL;
			private readonly Transform3D newTransformWTL;
			private readonly Transform3D referenceTransformWTL;

			public CopyModifier(VoxelData[,,] currentDataCopy, Transform3D originalTransformWTL, Transform3D newTransformWTL, Transform3D referenceTransformWTL)
			{
				this.currentDataCopy = currentDataCopy;
				this.originalTransformWTL = originalTransformWTL;
				this.newTransformWTL = newTransformWTL;
				this.referenceTransformWTL = referenceTransformWTL;
			}

			public virtual VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive)
			{
				if (distanceOutsideIsPositive > 0) return currentValue;

				// Convert voxel position to Vector3
				Vector3 originalPosition = new Vector3(x, y, z);
				originalPosition = referenceTransformWTL.AffineInverse() * originalPosition;

				// Apply transformation matrix
				Vector3 transformedPosition = TransformBetweenLocalSpaces(originalPosition, originalTransformWTL, newTransformWTL);
				transformedPosition = referenceTransformWTL * transformedPosition;

				// Get integer floor and ceiling of transformed position
				int x0 = Mathf.FloorToInt(transformedPosition.X);
				int x1 = Mathf.CeilToInt(transformedPosition.X);
				int y0 = Mathf.FloorToInt(transformedPosition.Y);
				int y1 = Mathf.CeilToInt(transformedPosition.Y);
				int z0 = Mathf.FloorToInt(transformedPosition.Z);
				int z1 = Mathf.CeilToInt(transformedPosition.Z);

				// Clamp indices to stay within array bounds
				x0 = Mathf.Clamp(x0, 0, currentDataCopy.GetLength(0) - 1);
				x1 = Mathf.Clamp(x1, 0, currentDataCopy.GetLength(0) - 1);
				y0 = Mathf.Clamp(y0, 0, currentDataCopy.GetLength(1) - 1);
				y1 = Mathf.Clamp(y1, 0, currentDataCopy.GetLength(1) - 1);
				z0 = Mathf.Clamp(z0, 0, currentDataCopy.GetLength(2) - 1);
				z1 = Mathf.Clamp(z1, 0, currentDataCopy.GetLength(2) - 1);

				// Get fractional parts for interpolation
				float xd = Mathf.Clamp(transformedPosition.X - x0, 0f, 1f);
				float yd = Mathf.Clamp(transformedPosition.Y - y0, 0f, 1f);
				float zd = Mathf.Clamp(transformedPosition.Z - z0, 0f, 1f);

				// Retrieve voxel weights at 8 surrounding corners
				float c000 = currentDataCopy[x0, y0, z0].WeightInsideIsPositive;
				float c100 = currentDataCopy[x1, y0, z0].WeightInsideIsPositive;
				float c010 = currentDataCopy[x0, y1, z0].WeightInsideIsPositive;
				float c110 = currentDataCopy[x1, y1, z0].WeightInsideIsPositive;
				float c001 = currentDataCopy[x0, y0, z1].WeightInsideIsPositive;
				float c101 = currentDataCopy[x1, y0, z1].WeightInsideIsPositive;
				float c011 = currentDataCopy[x0, y1, z1].WeightInsideIsPositive;
				float c111 = currentDataCopy[x1, y1, z1].WeightInsideIsPositive;

				float c00 = Mathf.Lerp(c000, c100, xd);
				float c01 = Mathf.Lerp(c001, c101, xd);
				float c10 = Mathf.Lerp(c010, c110, xd);
				float c11 = Mathf.Lerp(c011, c111, xd);

				float c0 = Mathf.Lerp(c00, c10, yd);
				float c1 = Mathf.Lerp(c01, c11, yd);

				float interpolatedWeight = Mathf.Lerp(c0, c1, zd);

				// --- Interpolate color ---
				Color col000 = currentDataCopy[x0, y0, z0].Color;
				Color col100 = currentDataCopy[x1, y0, z0].Color;
				Color col010 = currentDataCopy[x0, y1, z0].Color;
				Color col110 = currentDataCopy[x1, y1, z0].Color;
				Color col001 = currentDataCopy[x0, y0, z1].Color;
				Color col101 = currentDataCopy[x1, y0, z1].Color;
				Color col011 = currentDataCopy[x0, y1, z1].Color;
				Color col111 = currentDataCopy[x1, y1, z1].Color;

				Color col00 = col000.Lerp(col100, xd);
				Color col10 = col010.Lerp(col110, xd);
				Color col01 = col001.Lerp(col101, xd);
				Color col11 = col011.Lerp(col111, xd);

				Color col0 = col00.Lerp(col10, yd);
				Color col1 = col01.Lerp(col11, yd);

				Color interpolatedColor = col0.Lerp(col1, zd);

				return new VoxelData(interpolatedWeight, interpolatedColor);
			}

			private Vector3 TransformBetweenLocalSpaces(Vector3 worldPosition, Transform3D A_old, Transform3D A_new)
			{
				// Convert world position to local space of A_new
				Vector3 localPositionInAnew = A_new * worldPosition;

				// Convert local position (A_new) back to world position using A_old
				Vector3 transformedWorldPosition = A_old.AffineInverse() * localPositionInAnew;

				return transformedWorldPosition;
			}
		}

		public class AddShapeModifier : IVoxelModifier
		{
			public virtual VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive)
			{
				float newDistanceOutsideIsPositive = SDFMath.CombinationFunctionsOutsideIsPositive.Add(currentValue.DistanceOutsideIsPositive, distanceOutsideIsPositive);
				return currentValue.WithDistanceOutsideIsPositive(newDistanceOutsideIsPositive);
			}
		}

		public class SubtractShapeModifier : IVoxelModifier
		{
			public virtual VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive)
			{
				float newDistanceOutsideIsPositive = SDFMath.CombinationFunctionsOutsideIsPositive.Subtract(currentValue.DistanceOutsideIsPositive, distanceOutsideIsPositive);
				return currentValue.WithDistanceOutsideIsPositive(newDistanceOutsideIsPositive);
			}
		}

		public class ModifyShapeWithMaxHeightModifier : IVoxelModifier
		{
			private readonly float maxHeight;
			private readonly BooleanType booleanType;

			public enum BooleanType
			{
				AddOnly,
				SubtractOnly,
				AddAndSubtract
			}

			public ModifyShapeWithMaxHeightModifier(float maxHeight, BooleanType booleanType)
			{
				this.maxHeight = maxHeight;
				this.booleanType = booleanType;
			}

			public virtual VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive)
			{
				Vector3 samplePoint = new Vector3(x, y, z);
				float currentDistance = currentValue.WeightInsideIsPositive;
				float newDistance;

				switch (booleanType)
				{
					case BooleanType.AddOnly:
						newDistance = AddOnly(distanceOutsideIsPositive, samplePoint, currentValue);
						break;
					case BooleanType.SubtractOnly:
						newDistance = SubtractOnly(distanceOutsideIsPositive, samplePoint, currentValue);
						break;
					case BooleanType.AddAndSubtract:
						newDistance = AddOnly(distanceOutsideIsPositive, samplePoint, currentValue);
						newDistance = SubtractOnly(newDistance, samplePoint, currentValue);
						break;
					default:
						newDistance = currentDistance;
						break;
				}

				return currentValue.WithDistanceOutsideIsPositive(newDistance);
			}

			private float AddOnly(float distanceToShape, Vector3 samplePoint, VoxelData currentValue)
			{
				float floorDistance = SDFMath.ShapesDistanceOutsideIsPositive.PlaneFloor(samplePoint, maxHeight);
				distanceToShape = SDFMath.CombinationFunctionsOutsideIsPositive.Intersect(distanceToShape, floorDistance);
				return SDFMath.CombinationFunctionsOutsideIsPositive.Add(currentValue.DistanceOutsideIsPositive, distanceToShape);
			}

			private float SubtractOnly(float distanceToShape, Vector3 samplePoint, VoxelData currentValue)
			{
				float floorDistance = SDFMath.ShapesDistanceOutsideIsPositive.PlaneCeiling(samplePoint, maxHeight);
				distanceToShape = SDFMath.CombinationFunctionsOutsideIsPositive.Intersect(distanceToShape, floorDistance);
				return SDFMath.CombinationFunctionsOutsideIsPositive.Subtract(currentValue.DistanceOutsideIsPositive, distanceToShape);
			}
		}

		public class GaussianSmoothingModifier : IVoxelModifier
		{
			private readonly VoxelData[,,] currentDataCopy;
			private float[,,] gaussianKernel;
			private readonly float weightThreshold;
			private readonly int radius;
			private readonly float sigma;

			public GaussianSmoothingModifier(VoxelData[,,] currentDataCopy, float weightThreshold, int radius, float sigma)
			{
				this.currentDataCopy = currentDataCopy;
				this.weightThreshold = weightThreshold;
				this.radius = radius;
				this.sigma = sigma;

				GenerateGaussianKernel(radius, sigma);
			}

			public virtual VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive)
			{
				if (distanceOutsideIsPositive > 0) return currentValue;

				if (Mathf.Abs(currentValue.WeightInsideIsPositive - weightThreshold) > sigma)
					return currentValue;

				float newWeight = ApplyKernel(x, y, z, currentDataCopy, gaussianKernel, radius);
				return currentValue.WithWeightInsideIsPositive(newWeight);
			}

			private void GenerateGaussianKernel(int radius, float sigma)
			{
				int size = 2 * radius + 1;
				gaussianKernel = new float[size, size, size];
				float sigma2 = 2 * sigma * sigma;
				float normalization = 1f / Mathf.Pow(Mathf.Pi * sigma2, 1.5f);

				for (int x = -radius; x <= radius; x++)
				{
					for (int y = -radius; y <= radius; y++)
					{
						for (int z = -radius; z <= radius; z++)
						{
							float distance2 = x * x + y * y + z * z;
							gaussianKernel[x + radius, y + radius, z + radius] = normalization * Mathf.Exp(-distance2 / sigma2);
						}
					}
				}
			}

			private float ApplyKernel(int x, int y, int z, VoxelData[,,] voxelData, float[,,] kernel, int radius)
			{
				float sum = 0f;
				float weightSum = 0f;

				int maxX = voxelData.GetLength(0);
				int maxY = voxelData.GetLength(1);
				int maxZ = voxelData.GetLength(2);

				int minI = Mathf.Max(-radius, -x);
				int maxI = Mathf.Min(radius, maxX - x - 1);
				int minJ = Mathf.Max(-radius, -y);
				int maxJ = Mathf.Min(radius, maxY - y - 1);
				int minK = Mathf.Max(-radius, -z);
				int maxK = Mathf.Min(radius, maxZ - z - 1);

				for (int i = minI; i <= maxI; i++)
				{
					for (int j = minJ; j <= maxJ; j++)
					{
						for (int k = minK; k <= maxK; k++)
						{
							int nx = x + i;
							int ny = y + j;
							int nz = z + k;

							float weight = kernel[i + radius, j + radius, k + radius];
							sum += voxelData[nx, ny, nz].WeightInsideIsPositive * weight;
							weightSum += weight;
						}
					}
				}

				return sum / weightSum;
			}
		}

		public class WorldSpaceRougheningModifier : IVoxelModifier
		{
			private readonly VoxelData[,,] currentDataCopy;
			private readonly int radius;
			private readonly float intensity;
			private readonly float frequency;
			private readonly float falloffSharpness;
			private readonly Vector3 voxelOrigin;
			private readonly float voxelSize;

			// FastNoiseLite instance replaces Unity's Mathf.PerlinNoise
			private static readonly FastNoiseLite noise = new FastNoiseLite()
			{
				NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin
			};

			public WorldSpaceRougheningModifier(
				VoxelData[,,] currentDataCopy,
				int radius,
				float intensity,
				float frequency,
				float falloffSharpness,
				Vector3 voxelOrigin,
				float voxelSize)
			{
				this.currentDataCopy = currentDataCopy;
				this.radius = radius;
				this.intensity = intensity;
				this.frequency = frequency;
				this.falloffSharpness = falloffSharpness;
				this.voxelOrigin = voxelOrigin;
				this.voxelSize = voxelSize;
			}

			public virtual VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive)
			{
				if (distanceOutsideIsPositive > 0) return currentValue;

				bool hasDifferentSign = false;
				int offset = 1;

				int xMin = Mathf.Max(x - offset, 0);
				int xMax = Mathf.Min(x + offset, currentDataCopy.GetLength(0) - 1);
				int yMin = Mathf.Max(y - offset, 0);
				int yMax = Mathf.Min(y + offset, currentDataCopy.GetLength(1) - 1);
				int zMin = Mathf.Max(z - offset, 0);
				int zMax = Mathf.Min(z + offset, currentDataCopy.GetLength(2) - 1);

				for (int nx = xMin; nx <= xMax; nx++)
				{
					for (int ny = yMin; ny <= yMax; ny++)
					{
						for (int nz = zMin; nz <= zMax; nz++)
						{
							if (nx == x && ny == y && nz == z) continue;

							float neighborWeight = currentDataCopy[nx, ny, nz].WeightInsideIsPositive;

							if ((currentValue.WeightInsideIsPositive * neighborWeight) < 0)
							{
								hasDifferentSign = true;
								break;
							}
						}
						if (hasDifferentSign) break;
					}
					if (hasDifferentSign) break;
				}

				if (!hasDifferentSign) return currentValue;

				Vector3 worldPos = voxelOrigin + new Vector3(x, y, z) * voxelSize;

				// FastNoiseLite GetNoise2D returns values in the range [-1, 1]
				float noiseXY = noise.GetNoise2D(worldPos.X * frequency, worldPos.Y * frequency);
				float noiseYZ = noise.GetNoise2D(worldPos.Y * frequency, worldPos.Z * frequency);
				float noiseXZ = noise.GetNoise2D(worldPos.X * frequency, worldPos.Z * frequency);
				float noiseValue = (noiseXY + noiseYZ + noiseXZ) / 3f;

				float addition = noiseValue * intensity;
				float modifiedWeight = currentValue.WeightInsideIsPositive + addition;

				return currentValue.WithWeightInsideIsPositive(modifiedWeight);
			}
		}

		public class ChangeColorModifier : IVoxelModifier
		{
			private readonly Color color;
			private readonly Curve curve;
			private readonly bool modifyRed;
			private readonly bool modifyGreen;
			private readonly bool modifyBlue;
			private readonly bool modifyAlpha;
			private readonly bool modifyAll;

			public ChangeColorModifier(Color color, Curve curve, bool modifyRed, bool modifyGreen, bool modifyBlue, bool modifyAlpha)
			{
				this.color = color;
				this.curve = curve;
				this.modifyRed = modifyRed;
				this.modifyGreen = modifyGreen;
				this.modifyBlue = modifyBlue;
				this.modifyAlpha = modifyAlpha;
				modifyAll = modifyRed && modifyGreen && modifyBlue && modifyAlpha;
			}

			public virtual VoxelData ModifyVoxel(int x, int y, int z, VoxelData currentValue, float distanceOutsideIsPositive)
			{
				if (distanceOutsideIsPositive > 0) return currentValue;

				Color newColor;

				if (modifyAll)
				{
					newColor = color;
				}
				else
				{
					float newRed = modifyRed ? color.R : currentValue.Color.R;
					float newGreen = modifyGreen ? color.G : currentValue.Color.G;
					float newBlue = modifyBlue ? color.B : currentValue.Color.B;
					float newAlpha = modifyAlpha ? color.A : currentValue.Color.A;
					newColor = new Color(newRed, newGreen, newBlue, newAlpha);
				}

				float curveWeight = curve != null ? curve.Sample(distanceOutsideIsPositive) : 0f;
				newColor = newColor.Lerp(currentValue.Color, curveWeight);

				return currentValue.WithColor(newColor);
			}
		}
	}
}
