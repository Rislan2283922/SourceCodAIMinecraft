using EarthBound.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using EarthBound.World.Blocks;

namespace EarthBound.World
{
    public class Chunk // <--- Changed to PUBLIC
    {
        public const int SIZE = 16;
        public const int HEIGHT = 128;
        public const int SEA_LEVEL = 40;

        public Vector2i Coord;
        public Vector3 Position;

        // Mesh Data
        private List<Vector3> solidVerts = new List<Vector3>();
        private List<Vector3> solidUVs = new List<Vector3>();
        private List<Vector3> solidColors = new List<Vector3>();
        private List<Vector2> solidLightLevels = new List<Vector2>();
        private List<uint> solidIndices = new List<uint>();
        private uint solidIndexCount = 0;

        private List<Vector3> waterVerts = new List<Vector3>();
        private List<Vector3> waterUVs = new List<Vector3>();
        private List<Vector3> waterColors = new List<Vector3>();
        private List<Vector2> waterLightLevels = new List<Vector2>();
        private List<uint> waterIndices = new List<uint>();
        private uint waterIndexCount = 0;

        private VAO vaoSolid, vaoWater;
        private VBO vboSolidPos, vboSolidUV, vboSolidColor, vboSolidLight;
        private VBO vboWaterPos, vboWaterUV, vboWaterColor, vboWaterLight;
        private IBO iboSolid, iboWater;

        private ChunkBlock[,,] chunkBlocks;
        public bool HasFire = false;

        public Chunk(Vector2i coord)
        {
            this.Coord = coord;
            this.Position = new Vector3(coord.X * SIZE, 0, coord.Y * SIZE);
            chunkBlocks = new ChunkBlock[SIZE, HEIGHT, SIZE];
        }

        public byte[] Serialize()
        {
            using (MemoryStream output = new MemoryStream())
            {
                byte[] rawData = new byte[SIZE * HEIGHT * SIZE * 3];
                int i = 0;
                for (int x = 0; x < SIZE; x++)
                    for (int z = 0; z < SIZE; z++)
                        for (int y = 0; y < HEIGHT; y++)
                        {
                            rawData[i] = (byte)chunkBlocks[x, y, z].Type;
                            rawData[i + 1] = chunkBlocks[x, y, z].Data;
                            rawData[i + 2] = chunkBlocks[x, y, z].Light;
                            i += 3;
                        }
                using (GZipStream dstream = new GZipStream(output, CompressionLevel.Optimal))
                {
                    dstream.Write(rawData, 0, rawData.Length);
                }
                return output.ToArray();
            }
        }

        public void Deserialize(byte[] data)
        {
            try
            {
                using (MemoryStream input = new MemoryStream(data))
                using (GZipStream dstream = new GZipStream(input, CompressionMode.Decompress))
                using (MemoryStream output = new MemoryStream())
                {
                    dstream.CopyTo(output);
                    ApplyRawData(output.ToArray());
                }
            }
            catch { ApplyRawData(data); }
        }

        private void ApplyRawData(byte[] data)
        {
            int i = 0;
            bool hasLight = data.Length >= (SIZE * HEIGHT * SIZE * 3);
            int step = hasLight ? 3 : 2;
            for (int x = 0; x < SIZE; x++)
                for (int z = 0; z < SIZE; z++)
                    for (int y = 0; y < HEIGHT; y++)
                    {
                        if (i + 1 >= data.Length) break;
                        BlockType type = (BlockType)data[i];
                        byte meta = data[i + 1];
                        byte light = hasLight ? data[i + 2] : (byte)0;
                        chunkBlocks[x, y, z] = new ChunkBlock(type, meta);
                        chunkBlocks[x, y, z].Light = light;
                        i += step;
                    }
        }

        public void SetBlock(int x, int y, int z, BlockType type, byte data = 0)
        {
            if (x >= 0 && x < SIZE && z >= 0 && z < SIZE && y >= 0 && y < HEIGHT)
                chunkBlocks[x, y, z] = new ChunkBlock(type, data);
        }

        public ChunkBlock GetChunkBlockData(int x, int y, int z)
        {
            if (x < 0 || x >= SIZE || z < 0 || z >= SIZE || y < 0 || y >= HEIGHT) return new ChunkBlock(BlockType.AIR);
            return chunkBlocks[x, y, z];
        }

        public BlockType GetBlockType(int x, int y, int z)
        {
            if (x < 0 || x >= SIZE || z < 0 || z >= SIZE || y < 0 || y >= HEIGHT) return BlockType.AIR;
            return chunkBlocks[x, y, z].Type;
        }

        public byte GetBlockData(int x, int y, int z)
        {
            if (x < 0 || x >= SIZE || z < 0 || z >= SIZE || y < 0 || y >= HEIGHT) return 0;
            return chunkBlocks[x, y, z].Data;
        }

        public void SetSunLight(int x, int y, int z, int val)
        {
            if (x >= 0 && x < SIZE && z >= 0 && z < SIZE && y >= 0 && y < HEIGHT) chunkBlocks[x, y, z].SetSunLight(val);
        }
        public int GetSunLight(int x, int y, int z)
        {
            if (x < 0 || x >= SIZE || z < 0 || z >= SIZE || y < 0 || y >= HEIGHT) return 15;
            return chunkBlocks[x, y, z].GetSunLight();
        }
        public void SetBlockLight(int x, int y, int z, int val)
        {
            if (x >= 0 && x < SIZE && z >= 0 && z < SIZE && y >= 0 && y < HEIGHT) chunkBlocks[x, y, z].SetBlockLight(val);
        }
        public int GetBlockLight(int x, int y, int z)
        {
            if (x < 0 || x >= SIZE || z < 0 || z >= SIZE || y < 0 || y >= HEIGHT) return 0;
            return chunkBlocks[x, y, z].GetBlockLight();
        }

        public void GenerateBlocks(WorldClass world)
        {
            Random rnd = new Random(Coord.GetHashCode());

            for (int x = 0; x < SIZE; x++)
            {
                for (int z = 0; z < SIZE; z++)
                {
                    float gx = Position.X + x;
                    float gz = Position.Z + z;

                    BiomeType biome;
                    float height = world.GetTerrainHeight(gx, gz, out biome);
                    int h = (int)height;

                    for (int y = 0; y < HEIGHT; y++)
                    {
                        BlockType type = BlockType.AIR;
                        byte data = 0;

                        if (y <= h)
                        {
                            if (y == 0) type = BlockType.BEDROCK;
                            else if (y < h - 4) type = BlockType.STONE;
                            else
                            {
                                if (biome == BiomeType.DESERT) type = BlockType.SAND;
                                else if (biome == BiomeType.SNOWY_PLAINS || biome == BiomeType.SNOWY_FOREST)
                                {
                                    if (y == h) type = BlockType.SNOW;
                                    else type = BlockType.DIRT;
                                }
                                else if (biome == BiomeType.MOUNTAINS)
                                {
                                    if (y == h && y > 95) type = BlockType.SNOW;
                                    else type = BlockType.STONE;
                                }
                                else
                                {
                                    if (y == h) type = BlockType.GRASS;
                                    else type = BlockType.DIRT;
                                }
                            }
                        }
                        else if (y <= SEA_LEVEL)
                        {
                            if (biome == BiomeType.FROZEN_OCEAN || biome == BiomeType.SNOWY_PLAINS || biome == BiomeType.SNOWY_FOREST)
                            {
                                if (y == SEA_LEVEL) type = BlockType.ICE;
                                else { type = BlockType.WATER; data = 8; }
                            }
                            else
                            {
                                type = BlockType.WATER; data = 8;
                            }
                        }
                        else
                        {
                            if (y == h + 1)
                            {
                                if ((biome == BiomeType.SNOWY_PLAINS || biome == BiomeType.SNOWY_FOREST || biome == BiomeType.MOUNTAINS) && h >= SEA_LEVEL)
                                {
                                    if (rnd.NextDouble() < 0.95)
                                    {
                                        type = BlockType.SNOW_LAYER;
                                        data = (byte)rnd.Next(0, 2);
                                    }
                                }
                                else if ((biome == BiomeType.FOREST || biome == BiomeType.PLAINS) && h >= SEA_LEVEL)
                                {
                                    double roll = rnd.NextDouble();
                                    if (roll < 0.008) type = (rnd.Next(2) == 0) ? BlockType.FLOWER_RED : BlockType.FLOWER_YELLOW;
                                    else if (biome == BiomeType.FOREST && roll < 0.018) type = (rnd.Next(2) == 0) ? BlockType.MUSHROOM_RED : BlockType.MUSHROOM_BROWN;
                                }
                            }
                        }
                        chunkBlocks[x, y, z] = new ChunkBlock(type, data);
                    }
                }
            }

            // --- Trees ---
            BiomeType centerBiome = world.GetBiome(world.TemperatureNoise.GetNoise(Position.X + 8, Position.Z + 8), 0, 0);
            int treeCount = 0;
            if (centerBiome == BiomeType.FOREST) treeCount = rnd.Next(4, 9);
            else if (centerBiome == BiomeType.SNOWY_FOREST) treeCount = rnd.Next(4, 9);
            else if (centerBiome == BiomeType.PLAINS || centerBiome == BiomeType.SNOWY_PLAINS) treeCount = rnd.Next(0, 2);

            for (int t = 0; t < treeCount; t++)
            {
                int tx = rnd.Next(3, SIZE - 3);
                int tz = rnd.Next(3, SIZE - 3);
                int groundY = -1;
                for (int y = HEIGHT - 2; y > 0; y--)
                {
                    BlockType bt = chunkBlocks[tx, y, tz].Type;
                    if (bt == BlockType.GRASS || bt == BlockType.DIRT || bt == BlockType.SNOW) { groundY = y; break; }
                    if (bt == BlockType.WATER || bt == BlockType.SAND) break;
                }

                if (groundY != -1)
                {
                    float gx = Position.X + tx;
                    float gz = Position.Z + tz;
                    BiomeType b = world.GetBiome(world.TemperatureNoise.GetNoise(gx, gz), 0, 0);
                    bool snowy = (b == BiomeType.SNOWY_FOREST || b == BiomeType.SNOWY_PLAINS);
                    if (b != BiomeType.DESERT) GenerateTree(tx, groundY + 1, tz, rnd, snowy);
                }
            }
        }

        private void GenerateTree(int x, int y, int z, Random rnd, bool snowy)
        {
            int height = rnd.Next(4, 7);
            for (int i = 0; i < height; i++)
                if (y + i < HEIGHT) chunkBlocks[x, y + i, z] = new ChunkBlock(BlockType.LOG);

            int leafStart = y + height - 3;
            int leafTop = y + height + 1;

            for (int ly = leafStart; ly <= leafTop; ly++)
            {
                int radius = (ly == leafTop) ? 2 : 3;
                if (ly == leafStart) radius = 2;
                for (int lx = x - radius; lx <= x + radius; lx++)
                {
                    for (int lz = z - radius; lz <= z + radius; lz++)
                    {
                        if (Vector2.Distance(new Vector2(lx, lz), new Vector2(x, z)) > radius - 0.3f) continue;
                        if (lx == x && lz == z && ly < y + height) continue;

                        if (lx >= 0 && lx < SIZE && lz >= 0 && lz < SIZE && ly < HEIGHT)
                        {
                            if (chunkBlocks[lx, ly, lz].Type == BlockType.AIR || chunkBlocks[lx, ly, lz].Type == BlockType.SNOW_LAYER)
                            {
                                chunkBlocks[lx, ly, lz] = new ChunkBlock(BlockType.LEAVES);
                                if (snowy && ly + 1 < HEIGHT && chunkBlocks[lx, ly + 1, lz].Type == BlockType.AIR)
                                {
                                    if (rnd.NextDouble() < 0.3)
                                        chunkBlocks[lx, ly + 1, lz] = new ChunkBlock(BlockType.SNOW_LAYER);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void BuildMesh(WorldClass world)
        {
            solidVerts.Clear(); solidUVs.Clear(); solidColors.Clear(); solidIndices.Clear(); solidLightLevels.Clear();
            solidIndexCount = 0;
            waterVerts.Clear(); waterUVs.Clear(); waterColors.Clear(); waterIndices.Clear(); waterLightLevels.Clear();
            waterIndexCount = 0;
            HasFire = false;

            for (int x = 0; x < SIZE; x++)
                for (int y = 0; y < HEIGHT; y++)
                    for (int z = 0; z < SIZE; z++)
                    {
                        ChunkBlock data = chunkBlocks[x, y, z];
                        if (data.Type == BlockType.AIR) continue;
                        if (data.Type == BlockType.FIRE) HasFire = true;

                        Block blockLogic = BlocksManager.GetBlock(data.Type);
                        Vector3 wPos = Position + new Vector3(x, y, z);
                        Vector3i lPos = new Vector3i(x, y, z);

                        if (blockLogic.IsTransparent && !blockLogic.IsSolid && (data.Type == BlockType.WATER || data.Type == BlockType.LAVA))
                        {
                            int startV = waterVerts.Count;
                            blockLogic.GenerateTerrainVertices(this, lPos, wPos, waterVerts, waterUVs, waterColors, waterLightLevels, world);
                            int added = waterVerts.Count - startV;
                            for (int i = 0; i < added; i += 4)
                            {
                                waterIndices.Add(waterIndexCount + 0);
                                waterIndices.Add(waterIndexCount + 1);
                                waterIndices.Add(waterIndexCount + 2);
                                waterIndices.Add(waterIndexCount + 2);
                                waterIndices.Add(waterIndexCount + 3);
                                waterIndices.Add(waterIndexCount + 0);
                                waterIndexCount += 4;
                            }
                        }
                        else
                        {
                            int startV = solidVerts.Count;
                            blockLogic.GenerateTerrainVertices(this, lPos, wPos, solidVerts, solidUVs, solidColors, solidLightLevels, world);
                            int added = solidVerts.Count - startV;
                            for (int i = 0; i < added; i += 4)
                            {
                                solidIndices.Add(solidIndexCount + 0);
                                solidIndices.Add(solidIndexCount + 1);
                                solidIndices.Add(solidIndexCount + 2);
                                solidIndices.Add(solidIndexCount + 2);
                                solidIndices.Add(solidIndexCount + 3);
                                solidIndices.Add(solidIndexCount + 0);
                                solidIndexCount += 4;
                            }
                        }
                    }
        }

        public static void BindChunkTexture(TextureArray array) { array.Bind(); }

        public void UploadBuffers()
        {
            if (solidVerts.Count > 0)
            {
                if (vaoSolid == null) vaoSolid = new VAO();
                vaoSolid.Bind();
                if (vboSolidPos != null) vboSolidPos.Delete(); vboSolidPos = new VBO(solidVerts); vaoSolid.LinkToVAO(0, 3, vboSolidPos);
                if (vboSolidUV != null) vboSolidUV.Delete(); vboSolidUV = new VBO(solidUVs); vaoSolid.LinkToVAO(1, 3, vboSolidUV);
                if (vboSolidColor != null) vboSolidColor.Delete(); vboSolidColor = new VBO(solidColors); vaoSolid.LinkToVAO(2, 3, vboSolidColor);
                if (vboSolidLight != null) vboSolidLight.Delete(); vboSolidLight = new VBO(solidLightLevels); vaoSolid.LinkToVAO(3, 2, vboSolidLight);
                if (iboSolid != null) iboSolid.Delete(); iboSolid = new IBO(solidIndices);
                vaoSolid.Unbind();
            }
            if (waterVerts.Count > 0)
            {
                if (vaoWater == null) vaoWater = new VAO();
                vaoWater.Bind();
                if (vboWaterPos != null) vboWaterPos.Delete(); vboWaterPos = new VBO(waterVerts); vaoWater.LinkToVAO(0, 3, vboWaterPos);
                if (vboWaterUV != null) vboWaterUV.Delete(); vboWaterUV = new VBO(waterUVs); vaoWater.LinkToVAO(1, 3, vboWaterUV);
                if (vboWaterColor != null) vboWaterColor.Delete(); vboWaterColor = new VBO(waterColors); vaoWater.LinkToVAO(2, 3, vboWaterColor);
                if (vboWaterLight != null) vboWaterLight.Delete(); vboWaterLight = new VBO(waterLightLevels); vaoWater.LinkToVAO(3, 2, vboWaterLight);
                if (iboWater != null) iboWater.Delete(); iboWater = new IBO(waterIndices);
                vaoWater.Unbind();
            }
        }

        public void RenderSolid(ShaderProgram program)
        {
            if (solidIndices.Count == 0 || vaoSolid == null) return;
            vaoSolid.Bind(); iboSolid.Bind();
            GL.DrawElements(PrimitiveType.Triangles, solidIndices.Count, DrawElementsType.UnsignedInt, 0);
        }

        public void RenderWater(ShaderProgram program)
        {
            if (waterIndices.Count == 0 || vaoWater == null) return;
            vaoWater.Bind(); iboWater.Bind();
            GL.DrawElements(PrimitiveType.Triangles, waterIndices.Count, DrawElementsType.UnsignedInt, 0);
        }
    }
}