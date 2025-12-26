using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class LeavesBlock : Block
    {
        public const int Index = 6;
        public override bool IsTransparent => true;
        public override string DisplayName => "Leaves";
        public override string SoundCategory => "grass";
        public override float Hardness => 0.2f;
        public override bool IsFlammable => true;
        
        protected override void AddFaceGeometry(Faces face, Vector3 wPos, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, Chunk chunk, Vector3i localPos, WorldClass world, Vector3i nOff)
        {
            base.AddFaceGeometry(face, wPos, verts, uvs, colors, lights, chunk, localPos, world, nOff);
            Vector3 tint = world.GetBiomeColor(wPos.X, wPos.Z);
            for (int i = 1; i <= 4; i++) colors[colors.Count - i] *= tint;
        }
    }
}
