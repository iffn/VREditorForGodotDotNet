using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using VoxelEditorForGodotDotNet.EditTools;

namespace VoxelEditorForGodotDotNet.Core
{
    public partial class VoxelController : Node3D
    {
        [Export] private PackedScene chunkPrefab;
        [Export] private VoxelChunkPreview previewView;
        [Export] private Node3D chunkHolder;
        [Export] private Material currentMainMaterial;
        [Export] private Godot.Collections.Array<Material> debugMaterials = new Godot.Collections.Array<Material>();

        public bool ShowGridOutline = true;

        private VoxelModel mainModel;
        private VoxelModel previewModelWithOldData;
        private static readonly Vector3I defaultChunkSize = new Vector3I(16, 16, 16);
        private Vector3I chunkSize = defaultChunkSize;
        private readonly List<VoxelChunkView> chunkViews = new List<VoxelChunkView>();

        public bool ViewsSetUp { get; private set; } = false;

        public List<string> MainMaterialNames
        {
            get
            {
                List<string> returnList = new List<string> { "Main material" };

                foreach (Material mat in debugMaterials)
                {
                    if (mat != null)
                    {
                        string name = string.IsNullOrEmpty(mat.ResourceName) ? "Debug Material" : mat.ResourceName;
                        returnList.Add(name);
                    }
                }

                return returnList;
            }
        }

        private int displayMaterialIndex = -1;
        public int DisplayMaterialIndex
        {
            get => displayMaterialIndex;
            set
            {
                if (chunkViews == null) return;

                Material currentMaterial;

                if (value == -1)
                    currentMaterial = currentMainMaterial;
                else if (value >= 0 && value < debugMaterials.Count)
                    currentMaterial = debugMaterials[value];
                else
                    return;

                foreach (VoxelChunkView view in chunkViews)
                {
                    view.CurrentMainMaterial = currentMaterial;
                }

                displayMaterialIndex = value;
            }
        }

        public Material MainDisplayMaterial
        {
            get
            {
                if (displayMaterialIndex >= 0 && displayMaterialIndex < debugMaterials.Count)
                    return debugMaterials[displayMaterialIndex];

                return currentMainMaterial;
            }
        }

        public Material CurrentMainMaterial
        {
            get => currentMainMaterial;
            set
            {
                if (currentMainMaterial == value) return;

                currentMainMaterial = value;

                if (chunkViews == null) return;

                foreach (VoxelChunkView view in chunkViews)
                {
                    view.CurrentMainMaterial = value;
                }
            }
        }

        private bool forceColliderOn = false;
        [Export]
        public bool ForceColliderOn
        {
            get => forceColliderOn;
            set
            {
                if (forceColliderOn == value) return;

                forceColliderOn = value;
                UpdateColliderStates();
            }
        }

        private bool enableAllColliders = false;
        public bool EnableAllColliders
        {
            set
            {
                if (enableAllColliders == value) return;

                enableAllColliders = value;
                UpdateColliderStates();
            }
        }

        public bool CollidersShouldBeOn => enableAllColliders || forceColliderOn;

        private bool invertNormals = false;
        public bool InvertAllNormals
        {
            get => invertNormals;
            set
            {
                if (invertNormals != value)
                    chunkViews.ForEach(chunk => chunk.InvertedNormals = value);

                invertNormals = value;
            }
        }

        public bool DisplayPreviewShape
        {
            get => previewView != null && previewView.Visible;
            set
            {
                if (previewView != null)
                    previewView.Visible = value;
            }
        }

        // Managers
        public ModificationManager ModificationManager { get; private set; }

        public List<VoxelChunkView> ChunkViews => chunkViews;
        public VoxelChunkPreview Preview => previewView;
        public bool IsInitialized => mainModel != null;
        public int GridResolutionX => mainModel.ResolutionX;
        public int GridResolutionY => mainModel.ResolutionY;
        public int GridResolutionZ => mainModel.ResolutionZ;
        public Vector3I MaxGrid => mainModel.MaxGrid;
        public VoxelData[,,] VoxelDataReference => mainModel.VoxelDataGrid;

        // Internal functions
        private void UpdateColliderStates()
        {
            foreach (VoxelChunkView chunkView in chunkViews)
            {
                chunkView.ColliderEnabled = CollidersShouldBeOn;
            }
        }

        private void GatherExistingChunks()
        {
            chunkViews.Clear();
            if (chunkHolder == null) return;

            foreach (Node child in chunkHolder.GetChildren())
            {
                if (child is VoxelChunkView view)
                {
                    if (view == previewView) continue;
                    chunkViews.Add(view);
                }
            }
        }

        private void GenerateAndUpdateViewChunks()
        {
            ViewsSetUp = true;

            int resolutionX = mainModel.ResolutionX;
            int resolutionY = mainModel.ResolutionY;
            int resolutionZ = mainModel.ResolutionZ;

            GatherExistingChunks();

            chunkSize = defaultChunkSize;

            int chunksX = DivideAndRoundUp(resolutionX, chunkSize.X);
            int chunksY = DivideAndRoundUp(resolutionY, chunkSize.Y);
            int chunksZ = DivideAndRoundUp(resolutionZ, chunkSize.Z);
            int requiredChunks = chunksX * chunksY * chunksZ;

            static int DivideAndRoundUp(int value, int divisor)
            {
                return value / divisor + (value % divisor == 0 ? 0 : 1);
            }

            if (requiredChunks > chunkViews.Count)
            {
                int additionalChunks = requiredChunks - chunkViews.Count;

                for (int i = 0; i < additionalChunks; i++)
                {
                    if (chunkPrefab != null && chunkPrefab.Instantiate() is VoxelChunkView chunkView)
                    {
                        chunkHolder.AddChild(chunkView);
                        chunkViews.Add(chunkView);
                    }
                }
            }
            else if (requiredChunks < chunkViews.Count)
            {
                for (int i = requiredChunks; i < chunkViews.Count; i++)
                {
                    chunkViews[i].QueueFree();
                }

                chunkViews.RemoveRange(requiredChunks, chunkViews.Count - requiredChunks);
            }

            int counter = 0;

            for (int x = 0; x < resolutionX; x += chunkSize.X)
            {
                for (int y = 0; y < resolutionY; y += chunkSize.Y)
                {
                    for (int z = 0; z < resolutionZ; z += chunkSize.Z)
                    {
                        Vector3I gridBoundsMin = new Vector3I(x, y, z);
                        Vector3I gridBoundsMax = new Vector3I(
                            Mathf.Min(gridBoundsMin.X + chunkSize.X, mainModel.MaxGrid.X),
                            Mathf.Min(gridBoundsMin.Y + chunkSize.Y, mainModel.MaxGrid.Y),
                            Mathf.Min(gridBoundsMin.Z + chunkSize.Z, mainModel.MaxGrid.Z)
                        );

                        chunkViews[counter++].Initialize(gridBoundsMin, gridBoundsMax, CollidersShouldBeOn, MainDisplayMaterial);
                    }
                }
            }

            UpdateAllChunks();
            UpdateColliderStates();
        }

        private void UpdateAllChunks()
        {
            if (chunkViews.Count < 2)
            {
                foreach (VoxelChunkView view in chunkViews)
                {
                    view.MarkDirty();
                    view.UpdateMeshIfDirty(mainModel, false);
                }
            }
            else
            {
                Parallel.For(0, chunkViews.Count, i =>
                {
                    VoxelChunkView view = chunkViews[i];
                    view.MarkDirty();
                    view.UpdateMeshIfDirty(mainModel, true);
                });

                foreach (VoxelChunkView view in chunkViews)
                {
                    view.ApplyNonParallelMeshDataIfDirty();
                }
            }
        }

        // External functions
        public void Initialize(int resolutionX, int resolutionY, int resolutionZ, bool setEmpty, bool skipViewSetup)
        {
            ViewsSetUp = false;

            // Setup managers
            ModificationManager ??= new ModificationManager(this);

            // Create and setup model
            if (mainModel == null)
            {
                mainModel = new VoxelModel(resolutionX, resolutionY, resolutionZ);
            }
            else
            {
                mainModel.ChangeGridSizeIfNeeded(resolutionX, resolutionY, resolutionZ, !setEmpty);
            }

            if (setEmpty)
            {
                SetEmptyGrid(false);
            }

            GatherExistingChunks();

            if (!skipViewSetup)
            {
                GenerateAndUpdateViewChunks();
            }

            // Setup preview model
            if (previewModelWithOldData == null)
            {
                previewModelWithOldData = new VoxelModel(resolutionX, resolutionY, resolutionZ);
            }
            else
            {
                previewModelWithOldData.ChangeGridSizeIfNeeded(resolutionX, resolutionY, resolutionZ, false);
            }

            previewView?.Initialize(Vector3I.Zero, Vector3I.One, false);
            DisplayPreviewShape = false;
        }

        public void ClearAllViews()
        {
            if (chunkHolder != null)
            {
                foreach (Node child in chunkHolder.GetChildren())
                {
                    child.QueueFree();
                }
            }

            chunkViews.Clear();
        }

        public void MarkRegionDirty(Vector3I minGrid, Vector3I maxGrid)
        {
            for (int i = 0; i < chunkViews.Count; i++)
            {
                if (chunkViews[i].IsWithinBounds(minGrid, maxGrid))
                {
                    chunkViews[i].MarkDirty();
                }
            }
        }

        public void MarkRegionDirty(Vector3I gridPoint)
        {
            for (int i = 0; i < chunkViews.Count; i++)
            {
                if (chunkViews[i].IsWithinBounds(gridPoint))
                {
                    chunkViews[i].MarkDirty();
                }
            }
        }

        public void UpdateAffectedChunks(Vector3I gridPoint)
        {
            foreach (VoxelChunkView chunkView in chunkViews)
            {
                if (chunkView.IsWithinBounds(gridPoint))
                {
                    chunkView.UpdateMeshIfDirty(mainModel, false);
                }
            }
        }

        public void UpdateAffectedChunks(Vector3I minGrid, Vector3I maxGrid)
        {
            foreach (VoxelChunkView chunkView in chunkViews)
            {
                if (chunkView.IsWithinBounds(minGrid, maxGrid))
                {
                    chunkView.UpdateMeshIfDirty(mainModel, false);
                }
            }
        }

        public void SetEmptyGrid(bool updateModel)
        {
            for (int x = 0; x < mainModel.ResolutionX; x++)
            {
                for (int y = 0; y < mainModel.ResolutionY; y++)
                {
                    for (int z = 0; z < mainModel.ResolutionZ; z++)
                    {
                        mainModel.SetVoxel(x, y, z, VoxelData.Empty);
                    }
                }
            }

            if (updateModel) UpdateAllChunks();
        }

        public void SetAllGridDataAndUpdateMesh(VoxelData[,,] newData)
        {
            mainModel.SetDataAndResizeIfNeeded(newData);
            previewModelWithOldData.ChangeGridSizeIfNeeded(GridResolutionX, GridResolutionY, GridResolutionZ, false);
            GenerateAndUpdateViewChunks();
        }

        public VoxelData GetVoxelWithoutClamp(int x, int y, int z) => mainModel.GetVoxelWithoutClamp(x, y, z);
        public VoxelData GetVoxelWithClamp(int x, int y, int z) => mainModel.GetVoxelWithClamp(x, y, z);
        public VoxelData GetVoxelWithClamp(float x, float y, float z) => mainModel.GetVoxelWithClamp(x, y, z);

        public void SetDataPointWithSettingItToDirty(int x, int y, int z, VoxelData value)
        {
            SetDataPointWithoutSettingItToDirty(x, y, z, value);
            MarkRegionDirty(new Vector3I(x, y, z));
        }

        public void SetDataPointWithoutSettingItToDirty(int x, int y, int z, VoxelData value)
        {
            mainModel.SetVoxel(x, y, z, value);
        }

        public void SetupPreviewZone(Vector3I minGrid, Vector3I maxGrid)
        {
            previewModelWithOldData.CopyRegion(mainModel, minGrid, maxGrid);
            previewView?.UpdateBounds(minGrid, maxGrid);
            DisplayPreviewShape = true;
        }

        public void SetPreviewDataPoint(int x, int y, int z, VoxelData value)
        {
            previewModelWithOldData.SetVoxel(x, y, z, value);
        }

        public void UpdatePreviewShape()
        {
            if (previewView == null) return;
            previewView.MarkDirty();
            previewView.UpdateMeshIfDirty(previewModelWithOldData, false);
        }

        public void ApplyPreviewChanges()
        {
            if (previewView == null) return;

            Vector3I gridBoundsMin = previewView.GridBoundsMin;
            Vector3I gridBoundsMax = previewView.GridBoundsMax;

            mainModel.CopyRegion(previewModelWithOldData, gridBoundsMin, gridBoundsMax);
            MarkRegionDirty(gridBoundsMin, gridBoundsMax);
            UpdateAffectedChunks(gridBoundsMin, gridBoundsMax);
        }

        public enum ExpansionDirections
        {
            XPos, YPos, ZPos,
            XNeg, YNeg, ZNeg
        }

        public void ExpandGrid(int offset, ExpansionDirections expansionDirection)
        {
            int offsetX = 0;
            int offsetY = 0;
            int offsetZ = 0;

            int resolutionX = mainModel.ResolutionX;
            int resolutionY = mainModel.ResolutionY;
            int resolutionZ = mainModel.ResolutionZ;

            switch (expansionDirection)
            {
                case ExpansionDirections.XPos: resolutionX += offset; break;
                case ExpansionDirections.YPos: resolutionY += offset; break;
                case ExpansionDirections.ZPos: resolutionZ += offset; break;
                case ExpansionDirections.XNeg: resolutionX += offset; offsetX = offset; break;
                case ExpansionDirections.YNeg: resolutionY += offset; offsetY = offset; break;
                case ExpansionDirections.ZNeg: resolutionZ += offset; offsetZ = offset; break;
            }

            mainModel.ChangeGridSize(resolutionX, resolutionY, resolutionZ, offsetX, offsetY, offsetZ);
            previewModelWithOldData.ChangeGridSizeIfNeeded(resolutionX, resolutionY, resolutionZ, false);

            GenerateAndUpdateViewChunks();
        }
    }
}