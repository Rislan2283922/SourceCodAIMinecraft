using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class SpongeBlock : Block
    {
        public const int Index = 22;
        
        public override string DisplayName => "Sponge";
        public override string SoundCategory => "grass";
        public override float Hardness => 0.6f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
