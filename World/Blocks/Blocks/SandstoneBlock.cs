using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class SandstoneBlock : Block
    {
        public const int Index = 11;
        
        public override string DisplayName => "Sandstone";
        public override string SoundCategory => "stone";
        public override float Hardness => 0.8f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
