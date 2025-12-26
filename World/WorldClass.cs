using EarthBound.World.Blocks;
using EarthBound.Graphics;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System;
using System.IO;

namespace EarthBound.World
{
    public enum BiomeType
    {
        OCEAN,
        FROZEN_OCEAN,
        PLAINS,
        FOREST,
        DESERT,
        SNOWY_PLAINS,
        SNOWY_FOREST,
        MOUNTAINS
    }

    public class WorldClass

    {
        public Dictionary<Vector2i, Chunk> Chunks = new Dictionary<Vector2i, Chunk>();
        private Queue<Vector2i> chunksToGenerate = new Queue<Vector2i>();
        private Queue<Chunk> chunksToBuildMesh = new Queue<Chunk>();
        private ViewFrustum viewFrustum = new ViewFrustum();
        private HashSet<Vector2i> loadedRegions = new HashSet<Vector2i>();

        // ШУМЫ
        public FastNoiseLite ContinentalNoise; // Высота (Океан/Суша)
        public FastNoiseLite PeaksNoise;       // Горы
        public FastNoiseLite TemperatureNoise; // Холод/Тепло
        public FastNoiseLite HumidityNoise;    // Сухо/Влажно
        public FastNoiseLite CaveNoise;        // Пещеры (пока не юзаем активно, но пусть будет)

        private HashSet<Vector3i> liquidUpdateSet = new HashSet<Vector3i>();
        private Queue<Vector3i> activeLiquidQueue = new Queue<Vector3i>();
        private float liquidTimer = 0.0f;
        private const float LIQUID_TICK_RATE = 0.15f;

        private int startGenTotal = 0;
        private int startGenCurrent = 0;

        public List<ItemEntity> ItemEntities = new List<ItemEntity>();
        private string worldName;
        private LightingEngine lighting;

        public WorldClass(string worldName, int seed)
        {
            this.worldName = worldName;
            lighting = new LightingEngine(this);

            // 1. Continental Noise (Land vs Ocean) - Smoother
            ContinentalNoise = new FastNoiseLite(seed);
            ContinentalNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            ContinentalNoise.SetFrequency(0.002f); // Lower freq for larger oceans/continents
            ContinentalNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
            ContinentalNoise.SetFractalOctaves(3); // Less detail for smoother transitions

            // 2. Peaks Noise (Mountains) - Much rarer and larger
            PeaksNoise = new FastNoiseLite(seed + 1);
            PeaksNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            PeaksNoise.SetFrequency(0.003f); // Lower freq = wider mountains
            PeaksNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
            PeaksNoise.SetFractalOctaves(4);

            // 3. Temperature (Biome distribution)
            TemperatureNoise = new FastNoiseLite(seed + 2);
            TemperatureNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            TemperatureNoise.SetFrequency(0.0015f); // Very slow transition

            // 4. Humidity
            HumidityNoise = new FastNoiseLite(seed + 3);
            HumidityNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            HumidityNoise.SetFrequency(0.0015f);
        }

        public BiomeType GetBiome(float temp, float humidity, float heightNoise)
        {
            // Этот метод теперь определяет только КЛИМАТИЧЕСКИЙ биом

            if (temp > 0.5f) return BiomeType.DESERT;
            if (temp < -0.3f) return humidity > 0.0f ? BiomeType.SNOWY_FOREST : BiomeType.SNOWY_PLAINS;

            // Если влажно - лес, сухо - равнина
            return humidity > 0.1f ? BiomeType.FOREST : BiomeType.PLAINS;
        }

        // Returns terrain height at x, z
        // Возвращает высоту поверхности в данной точке (ПЛАВНАЯ ВЕРСИЯ)
        public float GetTerrainHeight(float x, float z, out BiomeType biome)
        {
            float cont = ContinentalNoise.GetNoise(x, z); // Континенты (-1..1)
            float peaks = PeaksNoise.GetNoise(x, z);      // Рельеф (-1..1)

            // --- 1. РАСЧЕТ ВЫСОТЫ (Плавный) ---

            // Базовая высота дна океана и суши
            float baseHeight = 45;

            // Фактор океана (sigmoid для плавного берега)
            // Если cont < 0, мы уходим под воду. Если > 0, мы на суше.
            float landFactor = cont;

            // Делаем берег более плавным (S-curve)
            float landSmooth = (float)Math.Tanh(landFactor * 3.0f);

            // Высота ландшафта (без гор)
            float terrainHeight = baseHeight + (landSmooth * 30.0f); // +/- 30 блоков от уровня моря

            // Фактор гор (Peaks)
            // Мы используем peaks не как переключатель, а как множитель высоты
            // Ridged noise дает острые пики.
            float mountainInfluence = (peaks + 0.5f);
            if (mountainInfluence < 0) mountainInfluence = 0;

            // Возводим в степень, чтобы горы были редкими, но высокими
            mountainInfluence = mountainInfluence * mountainInfluence * mountainInfluence; // ^3

            // Но горы растут только на суше (landSmooth > 0.1)
            float landMask = Math.Max(0, landSmooth);

            // Финальная высота = Ландшафт + (Горы * Суша)
            float finalHeight = terrainHeight + (mountainInfluence * 60.0f * landMask);

            // --- 2. ОПРЕДЕЛЕНИЕ БИОМА (На основе высоты и климата) ---
            float temp = TemperatureNoise.GetNoise(x, z);
            float hum = HumidityNoise.GetNoise(x, z);

            // Если высоко - это горы (независимо от температуры внизу)
            if (finalHeight > 85)
            {
                biome = BiomeType.MOUNTAINS;
            }
            // Если низко - океан
            else if (finalHeight < 43)
            {
                biome = temp < -0.3f ? BiomeType.FROZEN_OCEAN : BiomeType.OCEAN;
            }
            // Иначе суша
            else
            {
                biome = GetBiome(temp, hum, 0); // Высота 0, так как мы уже определили горы выше
            }

            return finalHeight;
        }

        // Возвращает цвет травы/листвы для точки
        public Vector3 GetBiomeColor(float x, float z)
        {
            float temp = TemperatureNoise.GetNoise(x, z);
            float hum = HumidityNoise.GetNoise(x, z);
            BiomeType b = GetBiome(temp, hum, ContinentalNoise.GetNoise(x, z));

            switch (b)
            {
                case BiomeType.DESERT: return new Vector3(0.76f, 0.65f, 0.35f);
                case BiomeType.SNOWY_PLAINS:
                case BiomeType.SNOWY_FOREST:
                case BiomeType.FROZEN_OCEAN:
                case BiomeType.MOUNTAINS:
                    // Чисто белый, чтобы текстура травы стала серой (как снег)
                    // Текстура grass_side черно-белая маска, поэтому белый цвет сделает ее белой/светло-серой.
                    return new Vector3(1.5f, 1.5f, 1.5f); // Чуть ярче 1.0, чтобы компенсировать серость текстуры

                case BiomeType.FOREST: return new Vector3(0.3f, 0.7f, 0.2f);
                case BiomeType.PLAINS: return new Vector3(0.45f, 0.75f, 0.25f);
                default: return new Vector3(0.4f, 0.8f, 0.3f);
            }
        }

        public bool GenerateSpawnArea(int radius)
        {
            if (startGenTotal == 0)
            {
                for (int x = -radius; x <= radius; x++)
                    for (int z = -radius; z <= radius; z++)
                        chunksToGenerate.Enqueue(new Vector2i(x, z));
                startGenTotal = chunksToGenerate.Count;
            }
            for (int i = 0; i < 4; i++)
            {
                if (chunksToGenerate.Count > 0)
                {
                    Vector2i coord = chunksToGenerate.Dequeue();
                    LoadOrGenerateChunk(coord);
                    startGenCurrent++;
                }
                else return true;
            }
            return false;
        }

        private void LoadOrGenerateChunk(Vector2i coord)
        {
            if (Chunks.ContainsKey(coord)) return;

            int rx = (int)Math.Floor((double)coord.X / 32.0);
            int rz = (int)Math.Floor((double)coord.Y / 32.0);
            Vector2i regionCoord = new Vector2i(rx, rz);

            if (!loadedRegions.Contains(regionCoord))
            {
                RegionManager.LoadRegion(worldName, rx, rz, Chunks);
                loadedRegions.Add(regionCoord);
            }

            bool justCreated = false;
            bool needsLightCalc = false;

            if (!Chunks.ContainsKey(coord))
            {
                Chunk c = new Chunk(coord);
                c.GenerateBlocks(this);
                Chunks.Add(coord, c);
                justCreated = true;
                needsLightCalc = true;
            }
            else
            {
                if (Chunks[coord].GetSunLight(0, Chunk.HEIGHT - 1, 0) == 0)
                    needsLightCalc = true;
            }

            if (needsLightCalc) lighting.InitializeSunlight(Chunks[coord]);

            if (justCreated || needsLightCalc)
            {
                QueueUpdate(new Vector2i(coord.X + 1, coord.Y));
                QueueUpdate(new Vector2i(coord.X - 1, coord.Y));
                QueueUpdate(new Vector2i(coord.X, coord.Y + 1));
                QueueUpdate(new Vector2i(coord.X, coord.Y - 1));
            }
        }

        public void BuildAllMeshes()
        {
            foreach (var c in Chunks.Values)
            {
                c.BuildMesh(this);
                c.UploadBuffers();
            }
        }

        public void SaveWorld(string folderName)
        {
            RegionManager.SaveChunks(folderName, Chunks);
        }

        public void UpdateChunksAroundPlayer(Vector3 playerPos, int renderDistance)
        {
            int cx = (int)Math.Floor(playerPos.X / Chunk.SIZE);
            int cz = (int)Math.Floor(playerPos.Z / Chunk.SIZE);

            if (chunksToBuildMesh.Count > 0)
            {
                Chunk c = chunksToBuildMesh.Dequeue();
                c.BuildMesh(this);
                c.UploadBuffers();
            }

            for (int x = cx - renderDistance; x <= cx + renderDistance; x++)
            {
                for (int z = cz - renderDistance; z <= cz + renderDistance; z++)
                {
                    Vector2i coord = new Vector2i(x, z);
                    if (!Chunks.ContainsKey(coord))
                    {
                        LoadOrGenerateChunk(coord);
                        chunksToBuildMesh.Enqueue(Chunks[coord]);
                        QueueUpdate(new Vector2i(x + 1, z));
                        QueueUpdate(new Vector2i(x - 1, z));
                        QueueUpdate(new Vector2i(x, z + 1));
                        QueueUpdate(new Vector2i(x, z - 1));
                        return;
                    }
                }
            }
        }

        private float fireTimer = 0.0f;
        private float fireAnimTimer = 0.0f;

        public void TickFire(float dt)
        {
            fireAnimTimer += dt;
            if (fireAnimTimer > 0.2f)
            {
                fireAnimTimer = 0;
                foreach (var c in Chunks.Values) if (c.HasFire) QueueUpdate(c.Coord);
            }

            fireTimer += dt;
            if (fireTimer < 1.0f) return;
            fireTimer = 0;

            List<Vector2i> keys = new List<Vector2i>(Chunks.Keys);
            Random rnd = new Random();

            for (int i = 0; i < 3; i++)
            {
                if (keys.Count == 0) break;
                Vector2i key = keys[rnd.Next(keys.Count)];
                Chunk c = Chunks[key];

                for (int j = 0; j < 15; j++)
                {
                    int x = rnd.Next(Chunk.SIZE);
                    int y = rnd.Next(Chunk.HEIGHT);
                    int z = rnd.Next(Chunk.SIZE);

                    if (c.GetBlockType(x, y, z) == BlockType.FIRE)
                    {
                        if (rnd.NextDouble() < 0.2)
                        {
                            c.SetBlock(x, y, z, BlockType.AIR);
                            QueueUpdate(key);
                            continue;
                        }
                        Vector3i wp = new Vector3i((int)c.Position.X + x, y, (int)c.Position.Z + z);
                        TrySpreadFire(wp + Vector3i.UnitX, rnd);
                        TrySpreadFire(wp - Vector3i.UnitX, rnd);
                        TrySpreadFire(wp + Vector3i.UnitZ, rnd);
                        TrySpreadFire(wp - Vector3i.UnitZ, rnd);
                        TrySpreadFire(wp + Vector3i.UnitY, rnd);
                    }
                }
            }
        }

        private void TrySpreadFire(Vector3i pos, Random rnd)
        {
            BlockType target = GetBlock(new Vector3(pos.X, pos.Y, pos.Z));
            if (BlocksManager.GetBlock(target).IsFlammable)
            {
                if (rnd.NextDouble() < 0.4) SetBlock(pos.X, pos.Y, pos.Z, BlockType.FIRE);
            }
        }

        public void SetBlock(int x, int y, int z, BlockType type, byte data = 0)
        {
            int cx = (int)Math.Floor((float)x / Chunk.SIZE);
            int cz = (int)Math.Floor((float)z / Chunk.SIZE);
            Vector2i coord = new Vector2i(cx, cz);

            if (Chunks.ContainsKey(coord))
            {
                int lx = x - (cx * Chunk.SIZE);
                int lz = z - (cz * Chunk.SIZE);
                if (lx < 0) lx += Chunk.SIZE;
                if (lz < 0) lz += Chunk.SIZE;

                if ((type == BlockType.WATER || type == BlockType.LAVA) && data == 0) data = 8;

                Chunks[coord].SetBlock(lx, y, lz, type, data);
                lighting.UpdateLightAt(new Vector3i(x, y, z));
                Chunks[coord].BuildMesh(this);
                Chunks[coord].UploadBuffers();

                Vector3i p = new Vector3i(x, y, z);
                ScheduleFluidUpdate(p);
                ScheduleFluidUpdate(p + Vector3i.UnitX);
                ScheduleFluidUpdate(p - Vector3i.UnitX);
                ScheduleFluidUpdate(p + Vector3i.UnitY);
                ScheduleFluidUpdate(p - Vector3i.UnitY);
                ScheduleFluidUpdate(p + Vector3i.UnitZ);
                ScheduleFluidUpdate(p - Vector3i.UnitZ);

                if (lx == 0) QueueUpdate(new Vector2i(cx - 1, cz));
                if (lx == Chunk.SIZE - 1) QueueUpdate(new Vector2i(cx + 1, cz));
                if (lz == 0) QueueUpdate(new Vector2i(cx, cz - 1));
                if (lz == Chunk.SIZE - 1) QueueUpdate(new Vector2i(cx, cz + 1));
            }
        }

        public void ScheduleFluidUpdate(Vector3i pos)
        {
            if (!liquidUpdateSet.Contains(pos))
            {
                liquidUpdateSet.Add(pos);
                activeLiquidQueue.Enqueue(pos);
            }
        }

        public void TickLiquids(float dt)
        {
            liquidTimer += dt;
            if (liquidTimer < LIQUID_TICK_RATE) return;
            liquidTimer = 0;

            int updatesToProcess = activeLiquidQueue.Count;
            if (updatesToProcess == 0) return;

            List<Vector3i> currentBatch = new List<Vector3i>();
            for (int i = 0; i < Math.Min(updatesToProcess, 500); i++)
            {
                if (activeLiquidQueue.Count > 0)
                {
                    Vector3i pos = activeLiquidQueue.Dequeue();
                    liquidUpdateSet.Remove(pos);
                    currentBatch.Add(pos);
                }
            }

            foreach (var pos in currentBatch) ProcessSingleLiquidBlock(pos);
        }

        private void ProcessSingleLiquidBlock(Vector3i pos)
        {
            BlockType type = GetBlock(pos);
            if (type != BlockType.WATER && type != BlockType.LAVA) return;
            byte currentData = GetBlockData(pos);
            if (currentData == 0) return;

            if (type == BlockType.LAVA && new Random().NextDouble() > 0.25)
            {
                ScheduleFluidUpdate(pos);
                return;
            }

            Vector3i down = pos - Vector3i.UnitY;
            BlockType downBlock = GetBlock(down);

            if (!BlocksManager.GetBlock(downBlock).IsSolid && downBlock != type)
            {
                SetBlockSimple(down.X, down.Y, down.Z, type, 8);
                return;
            }
            else if (downBlock == type) return;

            int decay = (type == BlockType.LAVA) ? 2 : 1;
            if (currentData > decay)
            {
                byte nextData = (byte)(currentData - decay);
                FlowTo(pos + Vector3i.UnitX, nextData, type);
                FlowTo(pos - Vector3i.UnitX, nextData, type);
                FlowTo(pos + Vector3i.UnitZ, nextData, type);
                FlowTo(pos - Vector3i.UnitZ, nextData, type);
            }
        }

        private void FlowTo(Vector3i pos, byte level, BlockType fluidType)
        {
            BlockType target = GetBlock(pos);
            if (target == BlockType.AIR || target == BlockType.DEAD_BUSH || target == BlockType.FLOWER_RED || target == BlockType.FLOWER_YELLOW || target == BlockType.SNOW_LAYER)
            {
                SetBlockSimple(pos.X, pos.Y, pos.Z, fluidType, level);
            }
            else if (target == fluidType)
            {
                byte targetLevel = GetBlockData(pos);
                if (targetLevel < level) SetBlockSimple(pos.X, pos.Y, pos.Z, fluidType, level);
            }
        }

        private void SetBlockSimple(int x, int y, int z, BlockType type, byte data)
        {
            int cx = (int)Math.Floor((float)x / Chunk.SIZE);
            int cz = (int)Math.Floor((float)z / Chunk.SIZE);
            Vector2i coord = new Vector2i(cx, cz);

            if (Chunks.ContainsKey(coord))
            {
                int lx = x - (cx * Chunk.SIZE);
                int lz = z - (cz * Chunk.SIZE);
                if (lx < 0) lx += Chunk.SIZE;
                if (lz < 0) lz += Chunk.SIZE;

                Chunks[coord].SetBlock(lx, y, lz, type, data);
                QueueUpdate(coord);

                ScheduleFluidUpdate(new Vector3i(x, y, z));
                ScheduleFluidUpdate(new Vector3i(x + 1, y, z));
                ScheduleFluidUpdate(new Vector3i(x - 1, y, z));
                ScheduleFluidUpdate(new Vector3i(x, y + 1, z));
                ScheduleFluidUpdate(new Vector3i(x, y - 1, z));
                ScheduleFluidUpdate(new Vector3i(x, y, z + 1));
                ScheduleFluidUpdate(new Vector3i(x, y, z - 1));
                ScheduleFluidUpdate(new Vector3i(x, y - 1, z));
            }
        }

        public byte GetBlockData(Vector3i pos) => GetBlockData(new Vector3(pos.X, pos.Y, pos.Z));
        public byte GetBlockData(Vector3 pos)
        {
            int cx = (int)Math.Floor(pos.X / Chunk.SIZE);
            int cz = (int)Math.Floor(pos.Z / Chunk.SIZE);
            Vector2i chunkCoord = new Vector2i(cx, cz);
            if (Chunks.ContainsKey(chunkCoord))
            {
                int lx = (int)pos.X - (cx * Chunk.SIZE);
                int lz = (int)pos.Z - (cz * Chunk.SIZE);
                if (lx < 0) lx += Chunk.SIZE;
                if (lz < 0) lz += Chunk.SIZE;
                return Chunks[chunkCoord].GetBlockData(lx, (int)pos.Y, lz);
            }
            return 0;
        }

        private void QueueUpdate(Vector2i coord)
        {
            if (Chunks.ContainsKey(coord) && !chunksToBuildMesh.Contains(Chunks[coord]))
                chunksToBuildMesh.Enqueue(Chunks[coord]);
        }

        public float GetLoadingProgress() => startGenTotal == 0 ? 0 : (float)startGenCurrent / startGenTotal;

        public bool IsBlockSolid(float x, float y, float z)
        {
            BlockType t = GetBlock(new Vector3(x, y, z));
            return BlocksManager.GetBlock(t).IsSolid;
        }
        public bool IsWater(float x, float y, float z) => GetBlock(new Vector3(x, y, z)) == BlockType.WATER;

        public BlockType GetBlock(Vector3 pos)
        {
            int cx = (int)Math.Floor(pos.X / Chunk.SIZE);
            int cz = (int)Math.Floor(pos.Z / Chunk.SIZE);
            Vector2i chunkCoord = new Vector2i(cx, cz);

            if (Chunks.ContainsKey(chunkCoord))
            {
                int lx = (int)pos.X - (cx * Chunk.SIZE);
                int lz = (int)pos.Z - (cz * Chunk.SIZE);
                if (lx < 0) lx += Chunk.SIZE;
                if (lz < 0) lz += Chunk.SIZE;
                int ly = (int)Math.Floor(pos.Y);
                return Chunks[chunkCoord].GetBlockType(lx, ly, lz);
            }
            return BlockType.AIR;
        }

        public void RenderSolid(ShaderProgram shader, Matrix4 viewProjection, Vector3 camPos, TextureArray array)
        {
            viewFrustum.Update(viewProjection);
            shader.Bind();
            Chunk.BindChunkTexture(array);
            float maxDist = (Settings.RenderDistance * Chunk.SIZE) + 8.0f;
            float maxSq = maxDist * maxDist;

            foreach (var c in Chunks.Values)
            {
                if (Vector3.DistanceSquared(camPos, c.Position + new Vector3(8, 64, 8)) > maxSq) continue;
                if (viewFrustum.IsBoxVisible(c.Position.X, 0, c.Position.Z, c.Position.X + Chunk.SIZE, Chunk.HEIGHT, c.Position.Z + Chunk.SIZE))
                    c.RenderSolid(shader);
            }
        }

        public void RenderTransparent(ShaderProgram shader, Matrix4 viewProjection, Vector3 camPos, TextureArray array)
        {
            shader.Bind();
            Chunk.BindChunkTexture(array);
            float maxDist = (Settings.RenderDistance * Chunk.SIZE) + 8.0f;
            float maxSq = maxDist * maxDist;

            foreach (var c in Chunks.Values)
            {
                if (Vector3.DistanceSquared(camPos, c.Position + new Vector3(8, 64, 8)) > maxSq) continue;
                if (viewFrustum.IsBoxVisible(c.Position.X, 0, c.Position.Z, c.Position.X + Chunk.SIZE, Chunk.HEIGHT, c.Position.Z + Chunk.SIZE))
                    c.RenderWater(shader);
            }
        }

        public void Explode(Vector3i center)
        {
            int r = 2;
            for (int x = -r; x <= r; x++)
                for (int y = -r; y <= r; y++)
                    for (int z = -r; z <= r; z++)
                    {
                        if (x * x + y * y + z * z <= r * r + 1)
                        {
                            Vector3i pos = center + new Vector3i(x, y, z);
                            BlockType b = GetBlock(new Vector3(pos.X, pos.Y, pos.Z));
                            if (b != BlockType.BEDROCK && b != BlockType.AIR)
                                SetBlock(pos.X, pos.Y, pos.Z, BlockType.AIR);
                        }
                    }
        }

        public void SpawnItem(Vector3 pos, BlockType type)
        {
            ItemEntities.Add(new ItemEntity(pos, type, 1));
        }

        public void UpdateEntities(float dt, Vector3 playerPos, InventorySystem inventory)
        {
            Vector3 playerCenter = playerPos - new Vector3(0, 0.8f, 0);
            for (int i = ItemEntities.Count - 1; i >= 0; i--)
            {
                var ent = ItemEntities[i];
                ent.Update(dt, this);
                if (Vector3.Distance(playerCenter, ent.Position) < 2.0f && ent.Age > 0.5f)
                {
                    if (inventory.AddItem(ent.Type, ent.Count)) ent.IsDead = true;
                }
                if (ent.IsDead)
                {
                    ent.Delete();
                    ItemEntities.RemoveAt(i);
                }
            }
            for (int i = 0; i < ItemEntities.Count; i++)
            {
                var entA = ItemEntities[i];
                if (entA.IsDead) continue;
                for (int j = i + 1; j < ItemEntities.Count; j++)
                {
                    var entB = ItemEntities[j];
                    if (entB.IsDead) continue;
                    if (entA.TryMerge(entB)) break;
                }
            }
        }

        public int GetSunLight(int x, int y, int z)
        {
            if (y >= Chunk.HEIGHT) return 15;
            if (y < 0) return 0;
            int cx = (int)Math.Floor((float)x / Chunk.SIZE);
            int cz = (int)Math.Floor((float)z / Chunk.SIZE);
            Vector2i coord = new Vector2i(cx, cz);
            if (Chunks.ContainsKey(coord))
            {
                int lx = x - (cx * Chunk.SIZE);
                int lz = z - (cz * Chunk.SIZE);
                if (lx < 0) lx += Chunk.SIZE;
                if (lz < 0) lz += Chunk.SIZE;
                return Chunks[coord].GetSunLight(lx, y, lz);
            }
            return 15;
        }

        public void SetSunLight(int x, int y, int z, int val)
        {
            if (y >= Chunk.HEIGHT || y < 0) return;
            int cx = (int)Math.Floor((float)x / Chunk.SIZE);
            int cz = (int)Math.Floor((float)z / Chunk.SIZE);
            Vector2i coord = new Vector2i(cx, cz);
            if (Chunks.ContainsKey(coord))
            {
                int lx = x - (cx * Chunk.SIZE);
                int lz = z - (cz * Chunk.SIZE);
                if (lx < 0) lx += Chunk.SIZE;
                if (lz < 0) lz += Chunk.SIZE;
                Chunks[coord].SetSunLight(lx, y, lz, val);
                QueueUpdate(coord);
            }
        }

        public int GetBlockLight(int x, int y, int z)
        {
            if (y >= Chunk.HEIGHT || y < 0) return 0;
            int cx = (int)Math.Floor((float)x / Chunk.SIZE);
            int cz = (int)Math.Floor((float)z / Chunk.SIZE);
            Vector2i coord = new Vector2i(cx, cz);
            if (Chunks.ContainsKey(coord))
            {
                int lx = x - (cx * Chunk.SIZE);
                int lz = z - (cz * Chunk.SIZE);
                if (lx < 0) lx += Chunk.SIZE;
                if (lz < 0) lz += Chunk.SIZE;
                return Chunks[coord].GetBlockLight(lx, y, lz);
            }
            return 0;
        }

        public void SetBlockLight(int x, int y, int z, int val)
        {
            if (y >= Chunk.HEIGHT || y < 0) return;
            int cx = (int)Math.Floor((float)x / Chunk.SIZE);
            int cz = (int)Math.Floor((float)z / Chunk.SIZE);
            Vector2i coord = new Vector2i(cx, cz);
            if (Chunks.ContainsKey(coord))
            {
                int lx = x - (cx * Chunk.SIZE);
                int lz = z - (cz * Chunk.SIZE);
                if (lx < 0) lx += Chunk.SIZE;
                if (lz < 0) lz += Chunk.SIZE;
                Chunks[coord].SetBlockLight(lx, y, lz, val);
                QueueUpdate(coord);
            }
        }

        public void RenderEntities(ShaderProgram shader, TextureArray array)
        {
            foreach (var ent in ItemEntities) ent.Render(shader, array);
        }
    }
}