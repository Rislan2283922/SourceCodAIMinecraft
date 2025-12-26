using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class WorkbenchBlock : Block
    {
        public const int Index = 28;
        
        public override string DisplayName => "Workbench";
        public override string SoundCategory => "wood";
        public override float Hardness => 2.5f;
        public override bool IsFlammable => true;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
