using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class StoneStairsBlock : StairsBlock
    {
        public const int Index = 50;
        public override string DisplayName => "Stone Stairs";
        public override string SoundCategory => "stone";
        public override float Hardness => 1.5f;
        
        // Переопределяем получение текстуры, чтобы брать текстуру от родительского блока (например, от камня)
        // Для теста просто берем ID текстуры по ID этого блока, который мы добавим в TextureData.cs
    }
}
