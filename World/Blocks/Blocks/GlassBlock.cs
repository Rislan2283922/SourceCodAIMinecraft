using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class GlassBlock : Block
    {
        public const int Index = 21;
        
        public override string DisplayName => "Glass";
        public override string SoundCategory => "metal";
        public override float Hardness => 0.3f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => true;
    }
}
