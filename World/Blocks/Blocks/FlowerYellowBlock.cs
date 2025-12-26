using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class FlowerYellowBlock : Block
    {
        public const int Index = 32;
        public override bool IsSolid => false;
        public override bool IsTransparent => true;
        public override string DisplayName => "Dandelion";
        public override string SoundCategory => "grass";
        public override float Hardness => 0.0f;
        public override int LightEmission => 0;
        public override bool IsFlammable => true;

        public override AABB GetBoundingBox(byte data)
        {
            return new AABB(new Vector3(-0.2f, -0.5f, -0.2f), new Vector3(0.2f, 0.1f, 0.2f));
        }

        public override void GenerateTerrainVertices(Chunk chunk, Vector3i localPos, Vector3 worldPos, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, WorldClass world)
        {
            int layer = GetTextureLayer(Faces.FRONT, 0);
            int sunL = chunk.GetSunLight(localPos.X, localPos.Y, localPos.Z);
            int blockL = chunk.GetBlockLight(localPos.X, localPos.Y, localPos.Z);
            if (LightEmission > 0) blockL = 15;

            Vector3 center = worldPos;
            float off = 0.35f;

            AddQuad(verts, uvs, colors, lights,
                center + new Vector3(-off, -0.5f, -off), center + new Vector3(off, -0.5f, off),
                center + new Vector3(off, 0.5f, off), center + new Vector3(-off, 0.5f, -off),
                layer, Vector3.One, new Vector2(sunL, blockL));

            AddQuad(verts, uvs, colors, lights,
                center + new Vector3(-off, -0.5f, off), center + new Vector3(off, -0.5f, -off),
                center + new Vector3(off, 0.5f, -off), center + new Vector3(-off, 0.5f, off),
                layer, Vector3.One, new Vector2(sunL, blockL));
        }

        private void AddQuad(List<Vector3> v, List<Vector3> uv, List<Vector3> c, List<Vector2> l, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, int layer, Vector3 col, Vector2 light)
        {
            v.Add(p1); v.Add(p2); v.Add(p3); v.Add(p4);
            uv.Add(new Vector3(0, 0, layer)); uv.Add(new Vector3(1, 0, layer)); uv.Add(new Vector3(1, 1, layer)); uv.Add(new Vector3(0, 1, layer));
            for (int i = 0; i < 4; i++) { c.Add(col); l.Add(light); }
            v.Add(p4); v.Add(p3); v.Add(p2); v.Add(p1);
            uv.Add(new Vector3(0, 1, layer)); uv.Add(new Vector3(1, 1, layer)); uv.Add(new Vector3(1, 0, layer)); uv.Add(new Vector3(0, 0, layer));
            for (int i = 0; i < 4; i++) { c.Add(col); l.Add(light); }
        }
    }
}
