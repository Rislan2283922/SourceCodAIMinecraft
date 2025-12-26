using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public class WoodAxeItem : Block
    {
        public const int Index = 39;
        public override bool IsSolid => false;
        public override bool IsTransparent => true;
        public override bool IsItem => true;
        public override string DisplayName => "Wood Axe";
        public override int LightEmission => 0;

        public override void GenerateTerrainVertices(Chunk chunk, Vector3i localPos, Vector3 worldPos, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, WorldClass world) 
        {
            // Предметы не имеют геометрии в мире (как блоки)
        }
    }
}
