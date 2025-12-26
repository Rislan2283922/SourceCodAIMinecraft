using OpenTK.Mathematics;
using System.Collections.Generic;
using EarthBound.Graphics;

namespace EarthBound.World.Blocks
{
    public abstract class Block
    {
        public int BlockID;
        public virtual bool IsSolid => true;
        public virtual bool IsTransparent => false;
        public virtual int LightEmission => 0;
        public virtual float Hardness => 1.0f;
        public virtual string DisplayName => "Unknown Block";
        public virtual bool IsFlammable => false;
        public virtual string SoundCategory => "stone";
        public virtual string Description => "";
        public virtual bool IsItem => false;
        public virtual int Opacity => IsTransparent ? 0 : 15;

        public virtual void Initialize() { }

        // Генерация для МИРА (Чанков)
        public virtual void GenerateTerrainVertices(Chunk chunk, Vector3i localPos, Vector3 worldPos,
            List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights,
            WorldClass world)
        {
            CheckFace(Faces.FRONT, localPos, worldPos, new Vector3i(0, 0, 1), verts, uvs, colors, lights, chunk, world);
            CheckFace(Faces.BACK, localPos, worldPos, new Vector3i(0, 0, -1), verts, uvs, colors, lights, chunk, world);
            CheckFace(Faces.LEFT, localPos, worldPos, new Vector3i(-1, 0, 0), verts, uvs, colors, lights, chunk, world);
            CheckFace(Faces.RIGHT, localPos, worldPos, new Vector3i(1, 0, 0), verts, uvs, colors, lights, chunk, world);
            CheckFace(Faces.TOP, localPos, worldPos, new Vector3i(0, 1, 0), verts, uvs, colors, lights, chunk, world);
            CheckFace(Faces.BOTTOM, localPos, worldPos, new Vector3i(0, -1, 0), verts, uvs, colors, lights, chunk, world);
        }

        // --- ГЕНЕРАЦИЯ ДЛЯ ПРЕДМЕТА (Рука / Дроп / GUI) ---
        public virtual void GenerateItemVertices(List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors)
        {
            // Получаем слой текстуры (обычно передняя грань)
            int layer = GetTextureLayer(Faces.FRONT, 0);

            // --- ИСПРАВЛЕННАЯ ЛОГИКА ---
            if (IsItem)
            {
                // 1. ЭТО ПРЕДМЕТ (Меч, кирка, уголь и т.д.)
                // Рисуем как плоскую картинку (plane), которая смотрит на игрока
                float size = 0.5f;
                AddItemQuad(verts, uvs, colors,
                    new Vector3(-size, -size, 0), new Vector3(size, -size, 0),
                    new Vector3(size, size, 0), new Vector3(-size, size, 0), layer);
                // Обратная сторона
                AddItemQuad(verts, uvs, colors,
                    new Vector3(size, -size, 0), new Vector3(-size, -size, 0),
                    new Vector3(-size, size, 0), new Vector3(size, size, 0), layer);
            }
            else if (!IsSolid)
            {
                // 2. ЭТО РАСТЕНИЕ (Цветок, гриб, саженец)
                // Рисуем крестом (Cross)
                float size = 0.5f;
                // Крест (Diagonal 1)
                AddItemQuad(verts, uvs, colors,
                    new Vector3(-size, -size, -size), new Vector3(size, -size, size),
                    new Vector3(size, size, size), new Vector3(-size, size, -size), layer);
                // Крест (Diagonal 2)
                AddItemQuad(verts, uvs, colors,
                    new Vector3(-size, -size, size), new Vector3(size, -size, -size),
                    new Vector3(size, size, -size), new Vector3(-size, size, size), layer);
            }
            else
            {
                // 3. ОБЫЧНЫЙ БЛОК (Куб)
                AddItemFace(Faces.TOP, verts, uvs, colors, new Vector3(0, 1, 0));
                AddItemFace(Faces.BOTTOM, verts, uvs, colors, new Vector3(0, -1, 0));
                AddItemFace(Faces.FRONT, verts, uvs, colors, new Vector3(0, 0, 1));
                AddItemFace(Faces.BACK, verts, uvs, colors, new Vector3(0, 0, -1));
                AddItemFace(Faces.LEFT, verts, uvs, colors, new Vector3(-1, 0, 0));
                AddItemFace(Faces.RIGHT, verts, uvs, colors, new Vector3(1, 0, 0));
            }
        }

        // Хелпер для рисования грани куба в инвентаре
        protected void AddItemFace(Faces face, List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, Vector3 normal)
        {
            var rawVertices = FaceDataRaw.rawVertexData[face];
            int layer = GetTextureLayer(face, 0);

            // Небольшое затенение для объема в инвентаре
            Vector3 color = Vector3.One;
            if (face == Faces.BOTTOM) color *= 0.5f;
            else if (face != Faces.TOP) color *= 0.8f;

            // Тинт для травы/листвы (чтобы в инвентаре были зелеными)
            if (this.BlockID == 2 || this.BlockID == 6) color *= new Vector3(0.4f, 0.8f, 0.3f);

            foreach (var v in rawVertices) verts.Add(v);

            uvs.Add(new Vector3(0, 0, layer)); uvs.Add(new Vector3(1, 0, layer));
            uvs.Add(new Vector3(1, 1, layer)); uvs.Add(new Vector3(0, 1, layer));

            for (int i = 0; i < 4; i++) colors.Add(color);
        }

        protected void AddItemQuad(List<Vector3> v, List<Vector3> uv, List<Vector3> c, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, int layer)
        {
            v.Add(p1); v.Add(p2); v.Add(p3); v.Add(p4);
            uv.Add(new Vector3(0, 0, layer)); uv.Add(new Vector3(1, 0, layer));
            uv.Add(new Vector3(1, 1, layer)); uv.Add(new Vector3(0, 1, layer));
            for (int i = 0; i < 4; i++) c.Add(Vector3.One);
        }

        // ... Остальной код (GenerateTerrainVertices, GetBoundingBox и т.д.) без изменений ...
        public virtual AABB GetBoundingBox(byte data)
        {
            if (!IsSolid) return new AABB(Vector3.Zero, Vector3.Zero);
            return new AABB(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f));
        }

        public virtual int GetTextureLayer(Faces face, byte data)
        {
            int myIndex = (int)this.GetType().GetField("Index").GetValue(null);
            if (TextureData.BlockLayerIndices.ContainsKey((BlockType)myIndex))
            {
                if (TextureData.BlockLayerIndices[(BlockType)myIndex].ContainsKey(face))
                    return TextureData.BlockLayerIndices[(BlockType)myIndex][face];
            }
            return 0;
        }

        protected void CheckFace(Faces face, Vector3i localPos, Vector3 worldPos, Vector3i neighborOffset,
            List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights,
            Chunk chunk, WorldClass world)
        {
            int nx = localPos.X + neighborOffset.X;
            int ny = localPos.Y + neighborOffset.Y;
            int nz = localPos.Z + neighborOffset.Z;
            Block neighborBlock;

            if (nx >= 0 && nx < Chunk.SIZE && ny >= 0 && ny < Chunk.HEIGHT && nz >= 0 && nz < Chunk.SIZE)
            {
                var data = chunk.GetChunkBlockData(nx, ny, nz);
                neighborBlock = BlocksManager.GetBlock(data.Type);
            }
            else
            {
                Vector3 nWorldPos = worldPos + new Vector3(neighborOffset.X, neighborOffset.Y, neighborOffset.Z);
                BlockType nt = world.GetBlock(nWorldPos);
                neighborBlock = BlocksManager.GetBlock(nt);
            }

            if (ShouldDrawFace(neighborBlock))
                AddFaceGeometry(face, worldPos, verts, uvs, colors, lights, chunk, localPos, world, neighborOffset);
        }

        protected virtual bool ShouldDrawFace(Block neighbor)
        {
            return !neighbor.IsSolid || neighbor.IsTransparent;
        }

        protected virtual void AddFaceGeometry(Faces face, Vector3 wPos,
            List<Vector3> verts, List<Vector3> uvs, List<Vector3> colors, List<Vector2> lights,
            Chunk chunk, Vector3i localPos, WorldClass world, Vector3i nOff)
        {
            var rawVertices = FaceDataRaw.rawVertexData[face];
            int layer = GetTextureLayer(face, 0);

            int sunL = 15, blockL = 0;
            Vector3i nPos = new Vector3i(localPos.X + nOff.X, localPos.Y + nOff.Y, localPos.Z + nOff.Z);

            if (nPos.X >= 0 && nPos.X < Chunk.SIZE && nPos.Y >= 0 && nPos.Y < Chunk.HEIGHT && nPos.Z >= 0 && nPos.Z < Chunk.SIZE)
            {
                sunL = chunk.GetSunLight(nPos.X, nPos.Y, nPos.Z);
                blockL = chunk.GetBlockLight(nPos.X, nPos.Y, nPos.Z);
            }
            else
            {
                Vector3 gw = wPos + new Vector3(nOff.X, nOff.Y, nOff.Z);
                sunL = world.GetSunLight((int)gw.X, (int)gw.Y, (int)gw.Z);
                blockL = world.GetBlockLight((int)gw.X, (int)gw.Y, (int)gw.Z);
            }

            if (LightEmission > 0) blockL = 15;

            Vector3 color = Vector3.One;
            if (face == Faces.BOTTOM) color *= 0.5f;
            else if (face != Faces.TOP) color *= 0.8f;

            foreach (var v in rawVertices) verts.Add(v + wPos);
            uvs.Add(new Vector3(0, 0, layer)); uvs.Add(new Vector3(1, 0, layer));
            uvs.Add(new Vector3(1, 1, layer)); uvs.Add(new Vector3(0, 1, layer));
            for (int i = 0; i < 4; i++) { colors.Add(color); lights.Add(new Vector2(sunL, blockL)); }
        }
    }
}