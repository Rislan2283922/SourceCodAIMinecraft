using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class PlanksStairsBlock : StairsBlock
    {
        public const int Index = 51;
        public override string DisplayName => "Planks Stairs";
        public override string SoundCategory => "wood";
        public override float Hardness => 2.0f;
        
        // Переопределяем получение текстуры, чтобы брать текстуру от родительского блока (например, от камня)
        // Для теста просто берем ID текстуры по ID этого блока, который мы добавим в TextureData.cs
    }
}
