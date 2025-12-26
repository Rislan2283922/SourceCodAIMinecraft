using OpenTK.Mathematics;
using System.Collections.Generic;
using EarthBound.Graphics;

namespace EarthBound.World.Blocks
{
    public class ChristmasTreeBlock : Block
    {
        public const int Index = 52;

        public override bool IsSolid => false;
        public override bool IsTransparent => true;
        public override string DisplayName => "Christmas Tree";
        public override string SoundCategory => "grass";
        public override float Hardness => 0.2f;
        public override int LightEmission => 15; // Светится как лампа

        public override AABB GetBoundingBox(byte data)
        {
            // Хитбокс
            return new AABB(new Vector3(-0.4f, -0.5f, -0.4f), new Vector3(0.4f, 0.5f, 0.4f));
        }

        // --- ГЕНЕРАЦИЯ ДЛЯ МИРА ---
        public override void GenerateTerrainVertices(Chunk chunk, Vector3i localPos, Vector3 worldPos, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, WorldClass world)
        {
            ChunkBlock me = chunk.GetChunkBlockData(localPos.X, localPos.Y, localPos.Z);

            // Если Data == 0 -> Низ. Если Data == 1 -> Верх.
            bool isTop = (me.Data == 1);

            // Получаем слои текстур
            int layerBottom = GetTextureLayer(Faces.FRONT, 0); // spruce_bottom
            int layerTop = GetTextureLayer(Faces.TOP, 0);      // spruce_top

            int layer = isTop ? layerTop : layerBottom;

            // ЦВЕТ: Белый (чтобы текстура была своего цвета), а не красный
            Vector3 color = Vector3.One;

            // СВЕТ: Полная яркость (15, 15) -> "Светится"
            Vector2 lightVec = new Vector2(15, 15);

            float off = 0.5f;
            Vector3 center = worldPos;

            // Рисуем крест (2 диагонали)
            AddQuad(verts, uvs, colors, lights,
                center + new Vector3(-off, -0.5f, -off), center + new Vector3(off, -0.5f, off),
                center + new Vector3(off, 0.5f, off), center + new Vector3(-off, 0.5f, -off),
                layer, color, lightVec);

            AddQuad(verts, uvs, colors, lights,
                center + new Vector3(-off, -0.5f, off), center + new Vector3(off, -0.5f, -off),
                center + new Vector3(off, 0.5f, -off), center + new Vector3(-off, 0.5f, off),
                layer, color, lightVec);
        }

        // --- ГЕНЕРАЦИЯ ДЛЯ ИНВЕНТАРЯ (Сразу целая елка) ---
        public override void GenerateItemVertices(List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors)
        {
            int layerBottom = GetTextureLayer(Faces.FRONT, 0);
            int layerTop = GetTextureLayer(Faces.TOP, 0);

            // Цвет белый (обычный), без красного фильтра
            Vector3 col = Vector3.One;

            // УМЕНЬШАЕМ МАСШТАБ, чтобы влезла в слот (было 0.5f)
            float size = 0.35f;

            // Смещаем по Y, чтобы центрировать в слоте
            float yOffset = -0.1f;

            // 1. НИЖНЯЯ ЧАСТЬ
            AddItemCross(verts, uvs, colors, layerBottom, col, new Vector3(0, yOffset - size, 0), size);

            // 2. ВЕРХНЯЯ ЧАСТЬ
            AddItemCross(verts, uvs, colors, layerTop, col, new Vector3(0, yOffset + size, 0), size);
        }

        private void AddItemCross(List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, int layer, Vector3 col, Vector3 offset, float s)
        {
            // Quad 1
            AddItemQuad(verts, uvs, colors,
                offset + new Vector3(-s, -s, -s), offset + new Vector3(s, -s, s),
                offset + new Vector3(s, s, s), offset + new Vector3(-s, s, -s), layer, col);
            // Quad 2
            AddItemQuad(verts, uvs, colors,
                offset + new Vector3(-s, -s, s), offset + new Vector3(s, -s, -s),
                offset + new Vector3(s, s, -s), offset + new Vector3(-s, s, s), layer, col);
        }

        private void AddItemQuad(List<Vector3> v, List<Vector3> uv, List<Vector3> c, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, int layer, Vector3 col)
        {
            v.Add(p1); v.Add(p2); v.Add(p3); v.Add(p4);
            uv.Add(new Vector3(0, 0, layer)); uv.Add(new Vector3(1, 0, layer));
            uv.Add(new Vector3(1, 1, layer)); uv.Add(new Vector3(0, 1, layer));
            for (int i = 0; i < 4; i++) c.Add(col);
        }

        private void AddQuad(List<Vector3> v, List<Vector3> uv, List<Vector3> c, List<Vector2> l, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, int layer, Vector3 col, Vector2 light)
        {
            // Front face
            v.Add(p1); v.Add(p2); v.Add(p3); v.Add(p4);
            uv.Add(new Vector3(0, 0, layer)); uv.Add(new Vector3(1, 0, layer)); uv.Add(new Vector3(1, 1, layer)); uv.Add(new Vector3(0, 1, layer));
            for (int i = 0; i < 4; i++) { c.Add(col); l.Add(light); }

            // Back face (double sided)
            v.Add(p4); v.Add(p3); v.Add(p2); v.Add(p1);
            uv.Add(new Vector3(0, 1, layer)); uv.Add(new Vector3(1, 1, layer)); uv.Add(new Vector3(1, 0, layer)); uv.Add(new Vector3(0, 0, layer));
            for (int i = 0; i < 4; i++) { c.Add(col); l.Add(light); }
        }
    }
}