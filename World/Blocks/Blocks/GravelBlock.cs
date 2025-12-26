using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class GravelBlock : Block
    {
        public const int Index = 9;
        
        public override string DisplayName => "Gravel";
        public override string SoundCategory => "sand";
        public override float Hardness => 0.6f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
