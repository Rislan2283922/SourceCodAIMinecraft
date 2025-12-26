using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class SandBlock : Block
    {
        public const int Index = 10;
        
        public override string DisplayName => "Sand";
        public override string SoundCategory => "sand";
        public override float Hardness => 0.5f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
