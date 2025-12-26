using OpenTK.Mathematics;
using System;
using EarthBound.World.Blocks; // Важно

namespace EarthBound.World
{
    public struct RaycastResult
    {
        public Vector3i BlockPos;
        public Vector3i FaceNormal;
        public bool Hit;
        public float Distance;
    }

    internal static class Physics
    {
        public static RaycastResult Raycast(Vector3 origin, Vector3 direction, float maxDistance, WorldClass world)
        {
            RaycastResult result = new RaycastResult();
            result.Hit = false;

            Vector3 dir = direction.Normalized();
            Vector3 pos = origin;

            float step = 0.05f;

            for (float d = 0; d < maxDistance; d += step)
            {
                pos += dir * step;

                int bx = (int)MathF.Floor(pos.X + 0.5f);
                int by = (int)MathF.Floor(pos.Y + 0.5f);
                int bz = (int)MathF.Floor(pos.Z + 0.5f);

                BlockType type = world.GetBlock(new Vector3(bx, by, bz));

                if (type != BlockType.AIR)
                {
                    byte data = world.GetBlockData(new Vector3(bx, by, bz));

                    // --- ЗАМЕНА BLOCKSTATS ---
                    Block block = BlocksManager.GetBlock(type);
                    AABB box = block.GetBoundingBox(data);
                    // -------------------------

                    if (box.Max == box.Min) continue;

                    Vector3 blockCenter = new Vector3(bx, by, bz);
                    Vector3 localPos = pos - blockCenter;

                    if (localPos.X >= box.Min.X && localPos.X <= box.Max.X &&
                        localPos.Y >= box.Min.Y && localPos.Y <= box.Max.Y &&
                        localPos.Z >= box.Min.Z && localPos.Z <= box.Max.Z)
                    {
                        result.Hit = true;
                        result.BlockPos = new Vector3i(bx, by, bz);

                        Vector3 abs = new Vector3(MathF.Abs(localPos.X), MathF.Abs(localPos.Y), MathF.Abs(localPos.Z));

                        if (abs.X > abs.Y && abs.X > abs.Z) result.FaceNormal = new Vector3i(localPos.X > 0 ? 1 : -1, 0, 0);
                        else if (abs.Y > abs.X && abs.Y > abs.Z) result.FaceNormal = new Vector3i(0, localPos.Y > 0 ? 1 : -1, 0);
                        else result.FaceNormal = new Vector3i(0, 0, localPos.Z > 0 ? 1 : -1);

                        return result;
                    }
                }
            }
            return result;
        }
    }
}