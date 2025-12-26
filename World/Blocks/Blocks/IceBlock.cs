using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class IceBlock : Block
    {
        public const int Index = 14;
        
        public override string DisplayName => "Ice";
        public override string SoundCategory => "ice";
        public override float Hardness => 0.5f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => true;
    }
}
