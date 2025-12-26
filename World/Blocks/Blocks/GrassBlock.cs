using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class GrassBlock : Block
    {
        public const int Index = 2;
        public override string DisplayName => "Grass Block";
        public override string SoundCategory => "grass";
        public override float Hardness => 0.6f;

        protected override void AddFaceGeometry(Faces face, Vector3 wPos, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, Chunk chunk, Vector3i localPos, WorldClass world, Vector3i nOff)
        {
            base.AddFaceGeometry(face, wPos, verts, uvs, colors, lights, chunk, localPos, world, nOff);
            Vector3 tint = Vector3.One;
            if (face == Faces.TOP) tint = world.GetBiomeColor(wPos.X, wPos.Z);
            else if (face != Faces.BOTTOM) tint = world.GetBiomeColor(wPos.X, wPos.Z) * 0.85f;
            else tint = new Vector3(0.5f);
            for (int i = 1; i <= 4; i++) colors[colors.Count - i] = tint;
        }
    }
}
