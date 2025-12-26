using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class FurnaceBlock : Block
    {
        public const int Index = 29;
        
        public override string DisplayName => "Furnace";
        public override string SoundCategory => "stone";
        public override float Hardness => 3.5f;
        public override bool IsFlammable => false;
        public override int LightEmission => 13;
        public override bool IsTransparent => false;
    }
}
