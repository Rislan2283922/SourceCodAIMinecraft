using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class BedrockBlock : Block
    {
        public const int Index = 8;
        
        public override string DisplayName => "Bedrock";
        public override string SoundCategory => "stone";
        public override float Hardness => -1.0f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
