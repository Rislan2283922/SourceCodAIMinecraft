using System;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace EarthBound.World.Blocks
{
    public abstract class StairsBlock : Block
    {
        public override bool IsTransparent => true;
        public override bool IsSolid => true;

        // Генерация для МИРА (оставляем как было в Generator.py)
        public override void GenerateTerrainVertices(Chunk chunk, Vector3i localPos, Vector3 worldPos, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, WorldClass world)
        {
            AddBox(verts, uvs, colors, lights, chunk, localPos, worldPos, world, new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.0f, 0.5f));
            AddBox(verts, uvs, colors, lights, chunk, localPos, worldPos, world, new Vector3(-0.5f, 0.0f, 0.0f), new Vector3(0.5f, 0.5f, 0.5f));
        }

        // --- НОВЫЙ МЕТОД: Генерация для ИНВЕНТАРЯ/ДРОПА ---
        public override void GenerateItemVertices(List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors)
        {
            // Рисуем L-форму без привязки к миру и свету
            // 1. Нижняя плита
            AddItemBox(verts, uvs, colors, new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.0f, 0.5f));
            // 2. Верхняя ступенька
            AddItemBox(verts, uvs, colors, new Vector3(-0.5f, 0.0f, 0.0f), new Vector3(0.5f, 0.5f, 0.5f));
        }

        // Хелпер для рисования коробки в инвентаре
        private void AddItemBox(List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, Vector3 min, Vector3 max)
        {
            // Front
            AddItemQuadCustom(verts, uvs, colors, Faces.FRONT, new Vector3(min.X, min.Y, max.Z), new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z));
            // Back
            AddItemQuadCustom(verts, uvs, colors, Faces.BACK, new Vector3(max.X, min.Y, min.Z), new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, max.Y, min.Z), new Vector3(max.X, max.Y, min.Z));
            // Left
            AddItemQuadCustom(verts, uvs, colors, Faces.LEFT, new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, min.Y, max.Z), new Vector3(min.X, max.Y, max.Z), new Vector3(min.X, max.Y, min.Z));
            // Right
            AddItemQuadCustom(verts, uvs, colors, Faces.RIGHT, new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, max.Y, min.Z), new Vector3(max.X, max.Y, max.Z));
            // Top
            AddItemQuadCustom(verts, uvs, colors, Faces.TOP, new Vector3(min.X, max.Y, max.Z), new Vector3(max.X, max.Y, max.Z), new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, min.Z));
            // Bottom
            AddItemQuadCustom(verts, uvs, colors, Faces.BOTTOM, new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, min.Y, max.Z), new Vector3(min.X, min.Y, max.Z));
        }

        private void AddItemQuadCustom(List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, Faces face, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
        {
            int layer = GetTextureLayer(face, 0);
            verts.Add(bl); verts.Add(br); verts.Add(tr); verts.Add(tl);
            uvs.Add(new Vector3(0, 0, layer)); uvs.Add(new Vector3(1, 0, layer)); uvs.Add(new Vector3(1, 1, layer)); uvs.Add(new Vector3(0, 1, layer));

            Vector3 col = Vector3.One;
            if (face != Faces.TOP) col *= 0.8f;
            if (face == Faces.BOTTOM) col *= 0.6f;
            for (int i = 0; i < 4; i++) colors.Add(col);
        }

        // ... (Старый AddBox для мира оставляем без изменений) ...
        private void AddBox(List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, Chunk chunk, Vector3i localPos, Vector3 worldPos, WorldClass world, Vector3 min, Vector3 max)
        {
            // (Полная копия того, что было в Generator.py, чтобы мир работал)
            int sunL = chunk.GetSunLight(localPos.X, localPos.Y, localPos.Z);
            int blockL = chunk.GetBlockLight(localPos.X, localPos.Y, localPos.Z);
            Vector2 lightVec = new Vector2(sunL, blockL);

            AddQuad(verts, uvs, colors, lights, Faces.FRONT, worldPos, min, max, lightVec, new Vector3(min.X, min.Y, max.Z), new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, max.Y, max.Z), new Vector3(min.X, max.Y, max.Z));
            AddQuad(verts, uvs, colors, lights, Faces.BACK, worldPos, min, max, lightVec, new Vector3(max.X, min.Y, min.Z), new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, max.Y, min.Z), new Vector3(max.X, max.Y, min.Z));
            AddQuad(verts, uvs, colors, lights, Faces.LEFT, worldPos, min, max, lightVec, new Vector3(min.X, min.Y, min.Z), new Vector3(min.X, min.Y, max.Z), new Vector3(min.X, max.Y, max.Z), new Vector3(min.X, max.Y, min.Z));
            AddQuad(verts, uvs, colors, lights, Faces.RIGHT, worldPos, min, max, lightVec, new Vector3(max.X, min.Y, max.Z), new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, max.Y, min.Z), new Vector3(max.X, max.Y, max.Z));
            AddQuad(verts, uvs, colors, lights, Faces.TOP, worldPos, min, max, lightVec, new Vector3(min.X, max.Y, max.Z), new Vector3(max.X, max.Y, max.Z), new Vector3(max.X, max.Y, min.Z), new Vector3(min.X, max.Y, min.Z));
            AddQuad(verts, uvs, colors, lights, Faces.BOTTOM, worldPos, min, max, lightVec, new Vector3(min.X, min.Y, min.Z), new Vector3(max.X, min.Y, min.Z), new Vector3(max.X, min.Y, max.Z), new Vector3(min.X, min.Y, max.Z));
        }

        private void AddQuad(List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights, Faces face, Vector3 worldPos, Vector3 boxMin, Vector3 boxMax, Vector2 light, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
        {
            int layer = GetTextureLayer(face, 0);
            verts.Add(bl + worldPos); verts.Add(br + worldPos); verts.Add(tr + worldPos); verts.Add(tl + worldPos);
            uvs.Add(new Vector3(0, 0, layer)); uvs.Add(new Vector3(1, 0, layer)); uvs.Add(new Vector3(1, 1, layer)); uvs.Add(new Vector3(0, 1, layer));
            Vector3 col = Vector3.One;
            if (face != Faces.TOP) col *= 0.8f;
            if (face == Faces.BOTTOM) col *= 0.6f;
            for (int i = 0; i < 4; i++) { colors.Add(col); lights.Add(light); }
        }
    }
}