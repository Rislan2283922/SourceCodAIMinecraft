using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class GoldOreBlock : Block
    {
        public const int Index = 17;
        
        public override string DisplayName => "Gold Ore";
        public override string SoundCategory => "stone";
        public override float Hardness => 3.0f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
