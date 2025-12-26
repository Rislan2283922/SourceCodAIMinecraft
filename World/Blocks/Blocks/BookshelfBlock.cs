using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class BookshelfBlock : Block
    {
        public const int Index = 27;
        
        public override string DisplayName => "Bookshelf";
        public override string SoundCategory => "wood";
        public override float Hardness => 1.5f;
        public override bool IsFlammable => true;
        public override int LightEmission => 0;
        public override bool IsTransparent => false;
    }
}
