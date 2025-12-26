using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class DirtBlock : Block
    {
        public const int Index = 1;
        
        public override string DisplayName => "Dirt";
        public override string SoundCategory => "grass";
        public override float Hardness => 0.5f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
