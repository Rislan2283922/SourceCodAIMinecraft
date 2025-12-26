using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class TNTBlock : Block
    {
        public const int Index = 26;
        
        public override string DisplayName => "TNT";
        public override string SoundCategory => "wood";
        public override float Hardness => 0.0f;
        public override bool IsFlammable => true;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
