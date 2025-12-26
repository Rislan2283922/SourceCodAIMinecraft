using EarthBound.World.Blocks;
using EarthBound.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
namespace EarthBound.World
{
    public class ItemEntity
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public BlockType Type;
        public int Count = 1;
        public float Age = 0;
        public bool IsDead = false;
        public float MergeTimer = 0.0f;
        private float rotationY = 0;
        private const float GRAVITY = 18.0f;

        private VAO vao;
        private VBO vboPos, vboUV, vboColor, vboLight;
        private IBO ibo;
        private int indexCount;

        private bool isFlatItem = false;

        public ItemEntity(Vector3 pos, BlockType type, int count = 1)
        {
            Position = pos;
            Type = type;
            Count = count;

            Random rnd = new Random();
            float rx = (float)rnd.NextDouble() * 2f - 1f;
            float rz = (float)rnd.NextDouble() * 2f - 1f;
            Velocity = new Vector3(rx * 1.5f, 4.0f, rz * 1.5f);

            BuildMesh();
        }

        public void SetType(BlockType newType)
        {
            if (Type != newType)
            {
                Type = newType;
                Delete();
                BuildMesh();
            }
        }

        private void BuildMesh()
        {
            Delete(); // Очистка старых буферов

            // Получаем блок, чтобы узнать его свойства (плоский или нет)
            Block block = BlocksManager.GetBlock(Type);

            // Если это предмет или не сплошной блок (цветы, факелы), считаем плоским
            isFlatItem = block.IsItem || !block.IsSolid;

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> uvs = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();
            List<Vector2> lights = new List<Vector2>();
            List<uint> indices = new List<uint>();

            // === ГЛАВНОЕ ИЗМЕНЕНИЕ ===
            // Вместо огромного if/else мы просим сам блок дать нам свою геометрию предмета.
            // Это позволяет ступенькам быть L-образными, а мечу — плоским.
            block.GenerateItemVertices(verts, uvs, colors);
            // =========================

            if (verts.Count == 0) return;

            // Генерируем индексы и свет (так как GenerateItemVertices дает только сырые вершины)
            // Проходимся по всем сгенерированным вершинам квадами (по 4 штуки)
            for (int i = 0; i < verts.Count; i += 4)
            {
                // Заполняем свет (полная яркость 15,15 для дропа)
                for (int k = 0; k < 4; k++)
                {
                    lights.Add(new Vector2(15, 15));
                }

                // Заполняем индексы (2 треугольника на 4 вершины)
                uint idx = (uint)i;
                indices.Add(idx);
                indices.Add(idx + 1);
                indices.Add(idx + 2);
                indices.Add(idx + 2);
                indices.Add(idx + 3);
                indices.Add(idx + 0);
            }

            // Масштабирование
            // Блоки уменьшаем до 0.25 (чтобы не были гигантскими), плоские предметы оставляем 0.5
            float scale = isFlatItem ? 0.5f : 0.25f;

            for (int i = 0; i < verts.Count; i++)
            {
                verts[i] *= scale;
            }

            indexCount = indices.Count;

            // Загрузка в OpenGL (код идентичен оригиналу)
            vao = new VAO();
            vao.Bind();

            vboPos = new VBO(verts);
            vao.LinkToVAO(0, 3, vboPos);

            vboUV = new VBO(uvs);
            vao.LinkToVAO(1, 3, vboUV);

            vboColor = new VBO(colors);
            vao.LinkToVAO(2, 3, vboColor);

            vboLight = new VBO(lights);
            vao.LinkToVAO(3, 2, vboLight);

            ibo = new IBO(indices);

            vao.Unbind();
        }

        private void AddQuad(List<Vector3> v, List<Vector3> uv, List<Vector3> c, List<Vector2> l, List<uint> i, ref uint idx,
            Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, int layer)
        {
            float s = 0.5f;
            v.Add(bl * s); v.Add(br * s); v.Add(tr * s); v.Add(tl * s);

            uv.Add(new Vector3(0, 0, layer));
            uv.Add(new Vector3(1, 0, layer));
            uv.Add(new Vector3(1, 1, layer));
            uv.Add(new Vector3(0, 1, layer));

            for (int k = 0; k < 4; k++)
            {
                c.Add(Vector3.One);
                l.Add(new Vector2(15, 15));
            }

            i.Add(idx); i.Add(idx + 1); i.Add(idx + 2);
            i.Add(idx + 2); i.Add(idx + 3); i.Add(idx + 0);
            idx += 4;
        }

        public void Update(float dt, WorldClass world)
        {
            Age += dt;
            MergeTimer += dt;
            Velocity.Y -= GRAVITY * dt;
            Vector3 nextPos = Position + Velocity * dt;

            if (world.IsBlockSolid(nextPos.X, nextPos.Y - 0.125f, nextPos.Z))
            {
                Velocity.Y = 0;
                Velocity.X *= 0.8f;
                Velocity.Z *= 0.8f;
                nextPos.Y = MathF.Floor(nextPos.Y) + 0.125f + 0.01f;
            }
            else
            {
                if (world.IsBlockSolid(nextPos.X, Position.Y, Position.Z)) Velocity.X = 0;
                if (world.IsBlockSolid(Position.X, Position.Y, nextPos.Z)) Velocity.Z = 0;
            }

            Position += Velocity * dt;
            rotationY += dt * 2.0f;
            if (Age > 300) IsDead = true;
        }

        public bool TryMerge(ItemEntity other)
        {
            if (other == this) return false;
            if (other.Type != this.Type) return false;
            if (other.IsDead || this.IsDead) return false;
            if (this.MergeTimer < 1.0f || other.MergeTimer < 1.0f) return false;
            if (this.Count + other.Count > ItemStack.MAX_STACK) return false;

            float dist = Vector3.Distance(this.Position, other.Position);
            if (dist < 1.0f)
            {
                this.Count += other.Count;
                other.IsDead = true;
                this.Velocity.Y = 3.0f;
                return true;
            }
            return false;
        }

        public void Render(ShaderProgram shader, TextureArray array)
        {
            if (vao == null) return;

            // Вращение и анимация подпрыгивания
            Matrix4 model = Matrix4.CreateRotationY(rotationY) *
                            Matrix4.CreateTranslation(Position + new Vector3(0, MathF.Sin(Age * 3f) * 0.1f, 0));

            RenderWithModel(shader, model, array);
        }

        public void RenderWithModel(ShaderProgram shader, Matrix4 modelMatrix, TextureArray array)
        {
            if (vao == null) return;

            GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "model"), true, ref modelMatrix);

            // Если предмет плоский, отключаем отсечение задних граней, чтобы видеть его с обеих сторон
            if (isFlatItem)
            {
                GL.Disable(EnableCap.CullFace);
            }

            vao.Bind();
            ibo.Bind();
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
            vao.Unbind();

            // Включаем обратно, чтобы не ломать рендер мира
            if (isFlatItem)
            {
                GL.Enable(EnableCap.CullFace);
            }
        }
        public void Delete()
        {
            if (vao != null) { vao.Delete(); vao = null; }
            if (vboPos != null) { vboPos.Delete(); vboPos = null; }
            if (vboUV != null) { vboUV.Delete(); vboUV = null; }
            if (vboColor != null) { vboColor.Delete(); vboColor = null; }
            if (vboLight != null) { vboLight.Delete(); vboLight = null; }
            if (ibo != null) { ibo.Delete(); ibo = null; }
        }
    }
}