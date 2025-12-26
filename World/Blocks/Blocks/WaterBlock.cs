using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class WaterBlock : Block
    {
        public const int Index = 12;

        public override bool IsSolid => false;
        public override bool IsTransparent => true;
        public override string DisplayName => "Water";
        public override string SoundCategory => "water";
        public override float Hardness => 100f;
        public override int LightEmission => 0;

        protected override bool ShouldDrawFace(Block neighbor)
        {
            if (neighbor.GetType() == this.GetType()) return false;
            return !neighbor.IsSolid || neighbor.IsTransparent;
        }

        public override void GenerateTerrainVertices(Chunk chunk, Vector3i localPos, Vector3 worldPos, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, WorldClass world)
        {
            ChunkBlock me = chunk.GetChunkBlockData(localPos.X, localPos.Y, localPos.Z);
            float drop = (me.Data > 0 && me.Data < 8) ? me.Data * 0.1f : 0.0f;

            CheckFluidFace(Faces.FRONT, localPos, worldPos, new Vector3i(0, 0, 1), verts, uvs, colors, lights, chunk, world, drop);
            CheckFluidFace(Faces.BACK, localPos, worldPos, new Vector3i(0, 0, -1), verts, uvs, colors, lights, chunk, world, drop);
            CheckFluidFace(Faces.LEFT, localPos, worldPos, new Vector3i(-1, 0, 0), verts, uvs, colors, lights, chunk, world, drop);
            CheckFluidFace(Faces.RIGHT, localPos, worldPos, new Vector3i(1, 0, 0), verts, uvs, colors, lights, chunk, world, drop);
            CheckFluidFace(Faces.TOP, localPos, worldPos, new Vector3i(0, 1, 0), verts, uvs, colors, lights, chunk, world, drop);
            CheckFluidFace(Faces.BOTTOM, localPos, worldPos, new Vector3i(0, -1, 0), verts, uvs, colors, lights, chunk, world, drop);
        }

        private void CheckFluidFace(Faces face, Vector3i localPos, Vector3 worldPos, Vector3i off, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, Chunk chunk, WorldClass world, float drop)
        {
            int nx = localPos.X + off.X;
            int ny = localPos.Y + off.Y;
            int nz = localPos.Z + off.Z;
            BlockType nt;
            if (nx >= 0 && nx < Chunk.SIZE && ny >= 0 && ny < Chunk.HEIGHT && nz >= 0 && nz < Chunk.SIZE)
                nt = chunk.GetChunkBlockData(nx, ny, nz).Type;
            else
                nt = world.GetBlock(worldPos + new Vector3(off.X, off.Y, off.Z));

            if (nt != (BlockType)Index && (BlocksManager.GetBlock(nt).IsTransparent || !BlocksManager.GetBlock(nt).IsSolid))
            {
                var raw = FaceDataRaw.rawVertexData[face];
                int layer = GetTextureLayer(face, 0);
                Vector3 tint = (Index == 12) ? new Vector3(0.3f, 0.5f, 0.9f) : Vector3.One;
                
                Vector3 nw = worldPos + new Vector3(off.X, off.Y, off.Z);
                int sunL = world.GetSunLight((int)nw.X, (int)nw.Y, (int)nw.Z);
                int blockL = world.GetBlockLight((int)nw.X, (int)nw.Y, (int)nw.Z);
                if (LightEmission > 0) blockL = 15;

                foreach (var v in raw)
                {
                    float vy = v.Y;
                    if (vy > 0) vy -= drop;
                    verts.Add(new Vector3(v.X, vy, v.Z) + worldPos);
                }
                uvs.Add(new Vector3(0, 0, layer)); uvs.Add(new Vector3(1, 0, layer));
                uvs.Add(new Vector3(1, 1, layer)); uvs.Add(new Vector3(0, 1, layer));
                for (int i = 0; i < 4; i++) { colors.Add(tint); lights.Add(new Vector2(sunL, blockL)); }
            }
        }
    }
}
