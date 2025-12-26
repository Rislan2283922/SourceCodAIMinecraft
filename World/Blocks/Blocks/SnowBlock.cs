using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class SnowBlock : Block
    {
        public const int Index = 15;
        
        public override string DisplayName => "Snow Block";
        public override string SoundCategory => "snow";
        public override float Hardness => 0.2f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
