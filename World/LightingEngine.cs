using OpenTK.Mathematics;
using System.Collections.Generic;
using EarthBound.World.Blocks;

namespace EarthBound.World
{
    internal class LightingEngine
    {
        private WorldClass world;
        private Queue<Vector3i> blockLightQueue = new Queue<Vector3i>();
        private Queue<LightNode> blockLightRemovalQueue = new Queue<LightNode>();
        private Queue<Vector3i> sunLightQueue = new Queue<Vector3i>();
        private Queue<LightNode> sunLightRemovalQueue = new Queue<LightNode>();

        private struct LightNode
        {
            public Vector3i Pos;
            public int Val;
            public LightNode(Vector3i p, int v) { Pos = p; Val = v; }
        }

        public LightingEngine(WorldClass world)
        {
            this.world = world;
        }

        public void InitializeSunlight(Chunk chunk)
        {
            for (int x = 0; x < Chunk.SIZE; x++)
            {
                for (int z = 0; z < Chunk.SIZE; z++)
                {
                    int currentLight = 15;
                    for (int y = Chunk.HEIGHT - 1; y >= 0; y--)
                    {
                        BlockType type = chunk.GetBlockType(x, y, z);
                        // Using 'type' here because we are iterating chunks
                        int opacity = BlocksManager.GetBlock(type).Opacity;

                        if (opacity > 0)
                        {
                            currentLight -= opacity;
                            if (currentLight < 0) currentLight = 0;
                        }

                        chunk.SetSunLight(x, y, z, currentLight);

                        if (currentLight < 15 && currentLight > 0)
                        {
                            int wx = (int)chunk.Position.X + x;
                            int wz = (int)chunk.Position.Z + z;
                            sunLightQueue.Enqueue(new Vector3i(wx, y, wz));
                        }
                    }
                }
            }
            PropagateSunLight();
        }

        public void UpdateLightAt(Vector3i pos)
        {
            BlockType type = world.GetBlock(new Vector3(pos.X, pos.Y, pos.Z));
            Block block = BlocksManager.GetBlock(type);
            int opacity = block.Opacity;
            int emission = block.LightEmission;

            int oldBlockLight = world.GetBlockLight(pos.X, pos.Y, pos.Z);
            world.SetBlockLight(pos.X, pos.Y, pos.Z, 0);
            blockLightRemovalQueue.Enqueue(new LightNode(pos, oldBlockLight));
            ProcessBlockLightRemoval();

            if (emission > 0)
            {
                world.SetBlockLight(pos.X, pos.Y, pos.Z, emission);
                blockLightQueue.Enqueue(pos);
            }
            PropagateBlockLight();

            int oldSunLight = world.GetSunLight(pos.X, pos.Y, pos.Z);
            world.SetSunLight(pos.X, pos.Y, pos.Z, 0);
            sunLightRemovalQueue.Enqueue(new LightNode(pos, oldSunLight));
            ProcessSunLightRemoval();

            if (opacity == 0)
            {
                int valAbove = world.GetSunLight(pos.X, pos.Y + 1, pos.Z);
                if (valAbove > 0)
                {
                    world.SetSunLight(pos.X, pos.Y, pos.Z, valAbove);
                    sunLightQueue.Enqueue(pos);
                }
            }
            PropagateSunLight();
        }

        private void ProcessBlockLightRemoval()
        {
            while (blockLightRemovalQueue.Count > 0)
            {
                LightNode node = blockLightRemovalQueue.Dequeue();
                Vector3i pos = node.Pos;
                int val = node.Val;
                Vector3i[] neighbors = GetNeighbors(pos);
                foreach (Vector3i n in neighbors)
                {
                    int neighborLevel = world.GetBlockLight(n.X, n.Y, n.Z);
                    if (neighborLevel != 0 && neighborLevel < val)
                    {
                        world.SetBlockLight(n.X, n.Y, n.Z, 0);
                        blockLightRemovalQueue.Enqueue(new LightNode(n, neighborLevel));
                    }
                    else if (neighborLevel >= val)
                    {
                        blockLightQueue.Enqueue(n);
                    }
                }
            }
        }

        private void PropagateBlockLight()
        {
            while (blockLightQueue.Count > 0)
            {
                Vector3i pos = blockLightQueue.Dequeue();
                int val = world.GetBlockLight(pos.X, pos.Y, pos.Z);
                Vector3i[] neighbors = GetNeighbors(pos);
                foreach (Vector3i n in neighbors)
                {
                    BlockType nType = world.GetBlock(new Vector3(n.X, n.Y, n.Z));
                    // Using 'nType' here because we are checking neighbor
                    int opacity = BlocksManager.GetBlock(nType).Opacity;
                    if (opacity >= 15) continue;

                    int expectedVal = val - 1 - opacity;
                    if (world.GetBlockLight(n.X, n.Y, n.Z) < expectedVal && expectedVal > 0)
                    {
                        world.SetBlockLight(n.X, n.Y, n.Z, expectedVal);
                        blockLightQueue.Enqueue(n);
                    }
                }
            }
        }

        private void ProcessSunLightRemoval()
        {
            while (sunLightRemovalQueue.Count > 0)
            {
                LightNode node = sunLightRemovalQueue.Dequeue();
                Vector3i pos = node.Pos;
                int val = node.Val;
                Vector3i[] neighbors = GetNeighbors(pos);
                foreach (Vector3i n in neighbors)
                {
                    int neighborLevel = world.GetSunLight(n.X, n.Y, n.Z);
                    if (neighborLevel != 0 && neighborLevel < val)
                    {
                        world.SetSunLight(n.X, n.Y, n.Z, 0);
                        sunLightRemovalQueue.Enqueue(new LightNode(n, neighborLevel));
                    }
                    else if (neighborLevel >= val)
                    {
                        sunLightQueue.Enqueue(n);
                    }
                }
            }
        }

        private void PropagateSunLight()
        {
            while (sunLightQueue.Count > 0)
            {
                Vector3i pos = sunLightQueue.Dequeue();
                int val = world.GetSunLight(pos.X, pos.Y, pos.Z);
                Vector3i[] neighbors = GetNeighbors(pos);
                foreach (Vector3i n in neighbors)
                {
                    BlockType nType = world.GetBlock(new Vector3(n.X, n.Y, n.Z));
                    // Using 'nType' here
                    int opacity = BlocksManager.GetBlock(nType).Opacity;
                    if (opacity >= 15) continue;

                    int currentNVal = world.GetSunLight(n.X, n.Y, n.Z);
                    int expectedVal = val - 1 - opacity;
                    if (n.Y == pos.Y - 1 && opacity == 0 && val == 15) expectedVal = 15;

                    if (currentNVal < expectedVal && expectedVal > 0)
                    {
                        world.SetSunLight(n.X, n.Y, n.Z, expectedVal);
                        sunLightQueue.Enqueue(n);
                    }
                }
            }
        }

        private Vector3i[] GetNeighbors(Vector3i pos)
        {
            return new Vector3i[] {
                pos + Vector3i.UnitX, pos - Vector3i.UnitX,
                pos + Vector3i.UnitY, pos - Vector3i.UnitY,
                pos + Vector3i.UnitZ, pos - Vector3i.UnitZ
            };
        }
    }
}
