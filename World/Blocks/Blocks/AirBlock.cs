using System.Collections.Generic;
using OpenTK.Mathematics;

namespace EarthBound.World.Blocks
{
    public class AirBlock : Block
    {
        public const int Index = 0;
        public override bool IsSolid => false;
        public override bool IsTransparent => true;
        public override string DisplayName => "Air";
        public override void GenerateTerrainVertices(Chunk chunk, Vector3i localPos, Vector3 worldPos, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, WorldClass world) { }
    }
}
