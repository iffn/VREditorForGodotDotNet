using System;
using Godot;

namespace VoxelEditorForGodotDotNet.Core
{
    public partial class VoxelChunkView : Node3D
    {
        private static readonly System.Diagnostics.Stopwatch PostProcessingStopwatch = new System.Diagnostics.Stopwatch();
        public static int ModifiedElements { get; private set; }
        public static int RemovedVertices { get; private set; }

        public static void ResetPostProcessingDiagnostics()
        {
            PostProcessingStopwatch.Reset();
            ModifiedElements = 0;
            RemovedVertices = 0;
        }

        public static double ElapsedPostProcessingTimeSeconds => PostProcessingStopwatch.Elapsed.TotalSeconds;

        private Vector3I gridBoundsMin;
        private Vector3I gridBoundsMax;
        private bool isDirty;
        private bool invertedNormals;

        private ArrayMesh mesh;
        private VoxelMeshData cachedMeshData;

        // Strategy reference (defaults to Marching Cubes)
        private IVoxelMesher currentMesher = new SurfaceNetsMesher();

        [Export] private MeshInstance3D linkedMeshInstance;
        [Export] private CollisionShape3D linkedCollisionShape;

        public IVoxelMesher Mesher
        {
            get => currentMesher;
            set
            {
                if (value != null && currentMesher != value)
                {
                    currentMesher = value;
                    MarkDirty();
                }
            }
        }

        public Material CurrentMainMaterial
        {
            get => linkedMeshInstance?.MaterialOverride;
            set
            {
                if (linkedMeshInstance != null)
                    linkedMeshInstance.MaterialOverride = value;
            }
        }

        public Vector3I GridBoundsMin => gridBoundsMin;
        public Vector3I GridBoundsMax => gridBoundsMax;
        public ArrayMesh SharedMesh => linkedMeshInstance?.Mesh as ArrayMesh;

        public bool ColliderEnabled
        {
            get => linkedCollisionShape != null && !linkedCollisionShape.Disabled;
            set
            {
                if (linkedCollisionShape != null)
                {
                    bool wasOn = !linkedCollisionShape.Disabled;
                    linkedCollisionShape.Disabled = !value;

                    if (value && !wasOn)
                        UpdateCollider();
                }
            }
        }

        public bool InvertedNormals
        {
            get => invertedNormals;
            set
            {
                if (invertedNormals != value)
                {
                    invertedNormals = value;
                    MarkDirty();
                }
            }
        }

        public void Initialize(
            Vector3I boundsMin, 
            Vector3I boundsMax, 
            bool colliderEnabled, 
            Material mainMaterial, 
            IVoxelMesher mesher = null)
        {
            if (mesher != null)
                currentMesher = mesher;

            Initialize(boundsMin, boundsMax, colliderEnabled);

            if (mainMaterial != null && linkedMeshInstance != null)
                linkedMeshInstance.MaterialOverride = mainMaterial;
        }

        public void Initialize(Vector3I boundsMin, Vector3I boundsMax, bool colliderEnabled)
        {
            gridBoundsMin = boundsMin;
            gridBoundsMax = boundsMax;

            Position = new Vector3(boundsMin.X, boundsMin.Y, boundsMin.Z);

            mesh = new ArrayMesh();
            linkedMeshInstance.Mesh = mesh;

            ColliderEnabled = colliderEnabled;
            isDirty = true;
        }

        public bool IsWithinBounds(Vector3I min, Vector3I max)
        {
            return !(gridBoundsMax.X < min.X || gridBoundsMin.X > max.X ||
                     gridBoundsMax.Y < min.Y || gridBoundsMin.Y > max.Y ||
                     gridBoundsMax.Z < min.Z || gridBoundsMin.Z > max.Z);
        }

        public bool IsWithinBounds(Vector3I point)
        {
            return !(gridBoundsMax.X < point.X || gridBoundsMin.X > point.X ||
                     gridBoundsMax.Y < point.Y || gridBoundsMin.Y > point.Y ||
                     gridBoundsMax.Z < point.Z || gridBoundsMin.Z > point.Z);
        }

        public void UpdateBounds(Vector3I min, Vector3I max)
        {
            gridBoundsMin = min;
            gridBoundsMax = max;
            Position = new Vector3(min.X, min.Y, min.Z);
        }

        public void MarkDirty()
        {
            isDirty = true;
        }

        public void UpdateMeshIfDirty(VoxelModel model, bool parallelCall)
        {
            if (!isDirty) return;

            // Generate mesh data using whichever IVoxelMesher strategy is assigned
            cachedMeshData = currentMesher.GenerateMesh(model, gridBoundsMin, gridBoundsMax, invertedNormals);

            if (!parallelCall)
                ApplyNonParallelMeshDataIfDirty();
        }

        public void ApplyNonParallelMeshDataIfDirty()
        {
            if (!isDirty || cachedMeshData == null) return;

            mesh.ClearSurfaces();

            if (cachedMeshData.Vertices.Count >= 3)
            {
                var surfaceArray = new Godot.Collections.Array();
                surfaceArray.Resize((int)Mesh.ArrayType.Max);

                surfaceArray[(int)Mesh.ArrayType.Vertex] = cachedMeshData.Vertices.ToArray();
                surfaceArray[(int)Mesh.ArrayType.Index] = cachedMeshData.Triangles.ToArray();

                if (cachedMeshData.Colors.Count > 0)
                    surfaceArray[(int)Mesh.ArrayType.Color] = cachedMeshData.Colors.ToArray();

                if (cachedMeshData.Normals.Count > 0)
                    surfaceArray[(int)Mesh.ArrayType.Normal] = cachedMeshData.Normals.ToArray();

                if (cachedMeshData.UVs.Count > 0)
                    surfaceArray[(int)Mesh.ArrayType.TexUV] = cachedMeshData.UVs.ToArray();

                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);

                if (ColliderEnabled)
                    UpdateCollider();
            }

            isDirty = false;
        }

        private void UpdateCollider()
        {
            if (linkedCollisionShape == null) return;

            if (mesh == null || mesh.GetSurfaceCount() == 0)
            {
                linkedCollisionShape.Shape = null;
                return;
            }

            linkedCollisionShape.Shape = mesh.CreateTrimeshShape();
        }
    }
}