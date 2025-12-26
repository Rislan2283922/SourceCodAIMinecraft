using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class CoalOreBlock : Block
    {
        public const int Index = 19;
        
        public override string DisplayName => "Coal Ore";
        public override string SoundCategory => "stone";
        public override float Hardness => 3.0f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
