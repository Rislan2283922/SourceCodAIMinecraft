using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class StoneBlock : Block
    {
        public const int Index = 3;
        
        public override string DisplayName => "Stone";
        public override string SoundCategory => "stone";
        public override float Hardness => 1.5f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
