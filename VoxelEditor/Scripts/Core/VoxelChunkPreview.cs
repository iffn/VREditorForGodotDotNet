using Godot;

namespace VoxelEditorForGodotDotNet.Core
{
    [Tool]
    public partial class VoxelChunkPreview : VoxelChunkView
    {
        [Export] private Material additionPreviewMaterial;
        [Export] private Material subtractionPreviewMaterial;

        public enum PreviewDisplayStates
        {
            Addition,
            Subtraction
        }

        public void SetPreviewDisplayState(PreviewDisplayStates state)
        {
            switch (state)
            {
                case PreviewDisplayStates.Addition:
                    CurrentMainMaterial = additionPreviewMaterial;
                    break;
                case PreviewDisplayStates.Subtraction:
                    CurrentMainMaterial = subtractionPreviewMaterial;
                    break;
            }
        }
    }
}