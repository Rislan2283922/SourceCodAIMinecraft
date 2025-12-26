using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class IronOreBlock : Block
    {
        public const int Index = 18;
        
        public override string DisplayName => "Iron Ore";
        public override string SoundCategory => "stone";
        public override float Hardness => 3.0f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
