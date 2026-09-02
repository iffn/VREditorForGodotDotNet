using Godot;
using System.Collections.Generic;

namespace VoxelEditorForGodotDotNet.Core
{
	// Container for generated raw mesh arrays
	public class VoxelMeshData
	{
		public List<Vector3> Vertices { get; set; } = new List<Vector3>();
		public List<int> Triangles { get; set; } = new List<int>();
		public List<Color> Colors { get; set; } = new List<Color>();
		public List<Vector3> Normals { get; set; } = new List<Vector3>();
		public List<Vector2> UVs { get; set; } = new List<Vector2>();

		public void Clear()
		{
			Vertices.Clear();
			Triangles.Clear();
			Colors.Clear();
			Normals.Clear();
			UVs.Clear();
		}
	}

	// Generic interface for any voxel algorithm (Marching Cubes, Surface Nets, etc.)
	public interface IVoxelMesher
	{
		VoxelMeshData GenerateMesh(VoxelModel model, Vector3I boundsMin, Vector3I boundsMax, bool invertedNormals);
	}
}
