using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class ObsidianBlock : Block
    {
        public const int Index = 24;
        
        public override string DisplayName => "Obsidian";
        public override string SoundCategory => "stone";
        public override float Hardness => 50.0f;
        public override bool IsFlammable => false;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
