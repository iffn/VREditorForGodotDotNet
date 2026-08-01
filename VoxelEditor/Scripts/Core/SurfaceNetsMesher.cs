using System.Collections.Generic;
using Godot;

namespace VoxelEditorForGodotDotNet.Core
{
    public class SurfaceNetsMesher : IVoxelMesher
    {
        // 8 local corner offsets of a unit cell
        private static readonly Vector3I[] CornerOffsets = new Vector3I[]
        {
            new Vector3I(0, 0, 0), // 0
            new Vector3I(1, 0, 0), // 1
            new Vector3I(1, 1, 0), // 2
            new Vector3I(0, 1, 0), // 3
            new Vector3I(0, 0, 1), // 4
            new Vector3I(1, 0, 1), // 5
            new Vector3I(1, 1, 1), // 6
            new Vector3I(0, 1, 1)  // 7
        };

        // 12 edges of a cube represented as pairs of corner indices (0..7)
        private static readonly (int CornerA, int CornerB)[] CubeEdges = new (int, int)[]
        {
            (0, 1), (1, 2), (2, 3), (3, 0), // Bottom face edges
            (4, 5), (5, 6), (6, 7), (7, 4), // Top face edges
            (0, 4), (1, 5), (2, 6), (3, 7)  // Vertical connecting edges
        };

        public VoxelMeshData GenerateMesh(VoxelModel model, Vector3I boundsMin, Vector3I boundsMax, bool invertedNormals)
        {
            var meshData = new VoxelMeshData();

            int sizeX = boundsMax.X - boundsMin.X;
            int sizeY = boundsMax.Y - boundsMin.Y;
            int sizeZ = boundsMax.Z - boundsMin.Z;

            if (sizeX <= 0 || sizeY <= 0 || sizeZ <= 0)
                return meshData;

            // Look-up grid for cell vertex indices (-1 means cell contains no surface vertex)
            int[,,] vertexIndexGrid = new int[sizeX, sizeY, sizeZ];
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        vertexIndexGrid[x, y, z] = -1;
                    }
                }
            }

            VoxelData[] tempCorners = new VoxelData[8];

            // ------------------------------------------------------------------
            // PASS 1: Calculate single vertex per surface-crossing cell
            // ------------------------------------------------------------------
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int worldX = x + boundsMin.X;
                        int worldY = y + boundsMin.Y;
                        int worldZ = z + boundsMin.Z;

                        model.GetCubeWeights(worldX, worldY, worldZ, tempCorners);

                        int mask = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            if (tempCorners[i].WeightInsideIsPositive >= 0.0f)
                                mask |= (1 << i);
                        }

                        if (mask == 0 || mask == 255)
                            continue;

                        Vector3 edgeSum = Vector3.Zero;
                        Color colorSum = new Color(0, 0, 0, 0);
                        int edgeCount = 0;

                        for (int e = 0; e < 12; e++)
                        {
                            var edge = CubeEdges[e];
                            float wA = tempCorners[edge.CornerA].WeightInsideIsPositive;
                            float wB = tempCorners[edge.CornerB].WeightInsideIsPositive;

                            if ((wA >= 0.0f) != (wB >= 0.0f))
                            {
                                float delta = wB - wA;
                                float t = (Mathf.Abs(delta) > 0.00001f) ? -wA / delta : 0.5f;

                                Vector3 posA = (Vector3)CornerOffsets[edge.CornerA];
                                Vector3 posB = (Vector3)CornerOffsets[edge.CornerB];
                                Vector3 intersection = posA.Lerp(posB, t);

                                edgeSum += intersection;

                                Color colA = tempCorners[edge.CornerA].Color;
                                Color colB = tempCorners[edge.CornerB].Color;
                                colorSum += colA.Lerp(colB, t);

                                edgeCount++;
                            }
                        }

                        if (edgeCount > 0)
                        {
                            Vector3 localCellVertex = edgeSum / edgeCount;
                            Vector3 chunkVertexPos = new Vector3(x, y, z) + localCellVertex;

                            int newVertexIndex = meshData.Vertices.Count;
                            vertexIndexGrid[x, y, z] = newVertexIndex;

                            meshData.Vertices.Add(chunkVertexPos);
                            meshData.Colors.Add(colorSum / edgeCount);
                        }
                    }
                }
            }

            // ------------------------------------------------------------------
            // PASS 2: Generate quads across active cell edges
            // ------------------------------------------------------------------
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int worldX = x + boundsMin.X;
                        int worldY = y + boundsMin.Y;
                        int worldZ = z + boundsMin.Z;

                        VoxelData currentVoxel = model.GetVoxelWithClamp(worldX, worldY, worldZ);
                        bool currentInside = currentVoxel.WeightInsideIsPositive >= 0.0f;

                        // Check X-axis edge crossing
                        if (x + 1 < sizeX && y > 0 && z > 0)
                        {
                            VoxelData neighborX = model.GetVoxelWithClamp(worldX + 1, worldY, worldZ);
                            bool neighborInside = neighborX.WeightInsideIsPositive >= 0.0f;

                            if (currentInside != neighborInside)
                            {
                                int v0 = vertexIndexGrid[x, y, z];
                                int v1 = vertexIndexGrid[x, y - 1, z];
                                int v2 = vertexIndexGrid[x, y - 1, z - 1];
                                int v3 = vertexIndexGrid[x, y, z - 1];

                                if (v0 != -1 && v1 != -1 && v2 != -1 && v3 != -1)
                                {
                                    bool reverse = currentInside ^ invertedNormals;
                                    AddQuad(meshData, v0, v1, v2, v3, reverse);
                                }
                            }
                        }

                        // Check Y-axis edge crossing
                        if (y + 1 < sizeY && x > 0 && z > 0)
                        {
                            VoxelData neighborY = model.GetVoxelWithClamp(worldX, worldY + 1, worldZ);
                            bool neighborInside = neighborY.WeightInsideIsPositive >= 0.0f;

                            if (currentInside != neighborInside)
                            {
                                int v0 = vertexIndexGrid[x, y, z];
                                int v1 = vertexIndexGrid[x, y, z - 1];
                                int v2 = vertexIndexGrid[x - 1, y, z - 1];
                                int v3 = vertexIndexGrid[x - 1, y, z];

                                if (v0 != -1 && v1 != -1 && v2 != -1 && v3 != -1)
                                {
                                    bool reverse = currentInside ^ invertedNormals;
                                    AddQuad(meshData, v0, v1, v2, v3, reverse);
                                }
                            }
                        }

                        // Check Z-axis edge crossing
                        if (z + 1 < sizeZ && x > 0 && y > 0)
                        {
                            VoxelData neighborZ = model.GetVoxelWithClamp(worldX, worldY, worldZ + 1);
                            bool neighborInside = neighborZ.WeightInsideIsPositive >= 0.0f;

                            if (currentInside != neighborInside)
                            {
                                int v0 = vertexIndexGrid[x, y, z];
                                int v1 = vertexIndexGrid[x - 1, y, z];
                                int v2 = vertexIndexGrid[x - 1, y - 1, z];
                                int v3 = vertexIndexGrid[x, y - 1, z];

                                if (v0 != -1 && v1 != -1 && v2 != -1 && v3 != -1)
                                {
                                    bool reverse = currentInside ^ invertedNormals;
                                    AddQuad(meshData, v0, v1, v2, v3, reverse);
                                }
                            }
                        }
                    }
                }
            }

            CalculateSmoothNormals(meshData);

            return meshData;
        }

        private static void AddQuad(VoxelMeshData meshData, int v0, int v1, int v2, int v3, bool reverse)
        {
            if (reverse)
            {
                meshData.Triangles.Add(v0);
                meshData.Triangles.Add(v3);
                meshData.Triangles.Add(v2);

                meshData.Triangles.Add(v0);
                meshData.Triangles.Add(v2);
                meshData.Triangles.Add(v1);
            }
            else
            {
                meshData.Triangles.Add(v0);
                meshData.Triangles.Add(v1);
                meshData.Triangles.Add(v2);

                meshData.Triangles.Add(v0);
                meshData.Triangles.Add(v2);
                meshData.Triangles.Add(v3);
            }
        }

        private static void CalculateSmoothNormals(VoxelMeshData meshData)
        {
            meshData.Normals.Clear();
            Vector3[] normals = new Vector3[meshData.Vertices.Count];

            for (int i = 0; i < meshData.Triangles.Count; i += 3)
            {
                int i0 = meshData.Triangles[i];
                int i1 = meshData.Triangles[i + 1];
                int i2 = meshData.Triangles[i + 2];

                Vector3 v0 = meshData.Vertices[i0];
                Vector3 v1 = meshData.Vertices[i1];
                Vector3 v2 = meshData.Vertices[i2];

                Vector3 faceNormal = (v1 - v0).Cross(v2 - v0);

                normals[i0] += faceNormal;
                normals[i1] += faceNormal;
                normals[i2] += faceNormal;
            }

            for (int i = 0; i < normals.Length; i++)
            {
                meshData.Normals.Add(normals[i].Normalized());
            }
        }
    }
}