using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class LogBlock : Block
    {
        public const int Index = 5;
        
        public override string DisplayName => "Log";
        public override string SoundCategory => "wood";
        public override float Hardness => 2.0f;
        public override bool IsFlammable => true;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
