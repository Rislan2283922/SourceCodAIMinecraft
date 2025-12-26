using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class BricksBlock : Block
    {
        public const int Index = 25;
        
        public override string DisplayName => "Bricks";
        public override string SoundCategory => "stone";
        public override float Hardness => 2.0f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
