using EarthBound.World.Blocks;
using EarthBound.Graphics;
using EarthBound.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace EarthBound.UI
{
    internal class UIRenderer
    {
        private int vao;
        private int vbo;
        private int ebo;
        public Texture TexSliderTrack;
        public Texture TexSliderHandle;
        private ShaderProgram uiShader;
        private Matrix4 projectionMatrix;

        public Texture TexButtonDefault;
        public Texture TexButtonHover;
        public Texture TexBackground;
        public Texture TexCrosshair;
        public Texture TexHotbar;
        public Texture TexInventoryGrid; // Warning was here, unused, but kept for compatibility
        public Texture TexSlot;
        public Texture TexPanelBig;

        // Removed TexAtlas as we use TextureArray passed in

        public Texture TexHeartFull;
        public Texture TexHeartHalf;
        public Texture TexHeartEmpty;

        private int whiteTextureID;

        public UIRenderer(int width, int height)
        {
            uiShader = new ShaderProgram("UI.vert", "UI.frag");
            UpdateSize(width, height);

            TexButtonDefault = new Texture("UI/Buttons/btn_default.png");
            TexButtonHover = new Texture("UI/Buttons/btn_hover.png");
            TexBackground = new Texture("UI/Backgrounds/menu_bg.png");
            TexCrosshair = new Texture("UI/HUD/crosshair.png");

            TexHotbar = new Texture("UI/HUD/inventory/hotbar.png");
            TexSlot = new Texture("UI/HUD/inventory/slot.png");
            TexPanelBig = new Texture("UI/HUD/panels/panel_big.png");

            TexSliderTrack = new Texture("UI/sliders/slider_track.png");
            TexSliderHandle = new Texture("UI/sliders/slider_handle.png");

            TexHeartFull = new Texture("UI/HUD/hp/heart_full.png");
            TexHeartHalf = new Texture("UI/HUD/hp/heart_half.png");
            TexHeartEmpty = new Texture("UI/HUD/hp/heart_empty.png");

            whiteTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, whiteTextureID);
            byte[] whitePixel = new byte[] { 255, 255, 255, 255 };
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, whitePixel);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            // 4 verts * 5 floats (2 pos + 3 tex)
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 5 * 4, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            uint[] indices = { 0, 1, 2, 2, 3, 0 };
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);

            // Pos
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Tex (vec3)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        public void UpdateSize(int width, int height)
        {
            if (width == 0 || height == 0) return;
            projectionMatrix = Matrix4.CreateOrthographicOffCenter(0, width, height, 0, -1f, 1f);
        }

        public void RenderQuadCustomVerts(float[] verts, Texture texture, Vector3 color, float alpha)
        {
            uiShader.Bind();
            GL.Uniform1(GL.GetUniformLocation(uiShader.ID, "uiTexture"), 0);
            GL.Uniform1(GL.GetUniformLocation(uiShader.ID, "useArray"), 0); // Use 2D

            GL.UniformMatrix4(GL.GetUniformLocation(uiShader.ID, "projection"), false, ref projectionMatrix);
            GL.Uniform3(GL.GetUniformLocation(uiShader.ID, "colorTint"), color);
            GL.Uniform1(GL.GetUniformLocation(uiShader.ID, "alpha"), alpha);

            GL.ActiveTexture(TextureUnit.Texture0);
            if (texture != null) texture.Bind();
            else GL.BindTexture(TextureTarget.Texture2D, whiteTextureID);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, verts.Length * sizeof(float), verts);

            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        // Overload for TextureArray
        public void RenderQuadCustomVertsArray(float[] verts, TextureArray array, Vector3 color, float alpha)
        {
            uiShader.Bind();
            GL.Uniform1(GL.GetUniformLocation(uiShader.ID, "uiTextureArray"), 0);
            GL.Uniform1(GL.GetUniformLocation(uiShader.ID, "useArray"), 1); // Use Array

            GL.UniformMatrix4(GL.GetUniformLocation(uiShader.ID, "projection"), false, ref projectionMatrix);
            GL.Uniform3(GL.GetUniformLocation(uiShader.ID, "colorTint"), color);
            GL.Uniform1(GL.GetUniformLocation(uiShader.ID, "alpha"), alpha);

            GL.ActiveTexture(TextureUnit.Texture0);
            array.Bind();

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, verts.Length * sizeof(float), verts);

            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }

        private void RenderQuad(float x, float y, float w, float h, Texture texture, Vector3 color, float alpha, Vector2 tiling)
        {
            // Standard 2D quad, Z layer = 0
            float[] verts = {
                x,     y,       0.0f * tiling.X, 1.0f * tiling.Y, 0.0f,
                x + w, y,       1.0f * tiling.X, 1.0f * tiling.Y, 0.0f,
                x + w, y + h,   1.0f * tiling.X, 0.0f * tiling.Y, 0.0f,
                x,     y + h,   0.0f * tiling.X, 0.0f * tiling.Y, 0.0f
            };
            RenderQuadCustomVerts(verts, texture, color, alpha);
        }

        public void Prepare()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public void DrawRect(float x, float y, float w, float h, Vector3 color, float alpha)
        {
            RenderQuad(x, y, w, h, null, color, alpha, new Vector2(1, 1));
        }

        public void DrawButton(float x, float y, float w, float h, bool isHover)
        {
            Texture tex = isHover ? TexButtonHover : TexButtonDefault;
            RenderQuad(x, y, w, h, tex, Vector3.One, 1.0f, new Vector2(1, 1));
        }

        public void DrawTiledBackground(float screenW, float screenH)
        {
            RenderQuad(0, 0, screenW, screenH, TexBackground, new Vector3(0.5f), 1.0f, new Vector2(screenW / 64f, screenH / 64f));
        }

        public void RenderCrosshair(float screenW, float screenH)
        {
            Prepare();
            float size = 32f;
            float x = (screenW - size) / 2f;
            float y = (screenH - size) / 2f;
            RenderQuad(x, y, size, size, TexCrosshair, Vector3.One, 1.0f, new Vector2(1, 1));
        }

        public void RenderLoadingBar(float screenW, float screenH, float progress)
        {
            Prepare();
            float barW = 600f;
            float barH = 40f;
            float x = (screenW - barW) / 2f;
            float y = screenH - 150f;
            DrawRect(x, y, barW, barH, Vector3.Zero, 1.0f);
            DrawRect(x, y, barW * progress, barH, new Vector3(0, 1, 0), 1.0f);
        }

        public void RenderHearts(float currentHP, float maxHP, float screenW, float screenH)
        {
            float scale = 3.0f;
            float heartSize = 9 * scale;
            float padding = 2 * scale;
            float hotbarWidth = 182 * scale;
            float hotbarStartX = (screenW - hotbarWidth) / 2.0f;
            float hbH = 22 * scale;
            float hbY = screenH - hbH - 50;
            float heartsY = hbY - heartSize - (5 * scale);
            float startX = hotbarStartX;

            for (int i = 0; i < 10; i++)
            {
                float hpThreshold = (i * 2) + 1;
                Texture texToDraw = TexHeartEmpty;

                if (currentHP >= hpThreshold + 1) texToDraw = TexHeartFull;
                else if (currentHP >= hpThreshold) texToDraw = TexHeartHalf;

                RenderQuad(startX + (i * (heartSize + padding)), heartsY, heartSize, heartSize, texToDraw, Vector3.One, 1.0f, new Vector2(1, 1));
            }
        }

        // Найдите метод RenderIcon3D и замените его целиком на этот код:

        public void RenderIcon3D(float x, float y, float size, BlockType type, TextureArray array)
        {
            Prepare();

            Block block = EarthBound.World.Blocks.BlocksManager.GetBlock(type);

            // Генерируем геометрию предмета
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> uvs = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();

            block.GenerateItemVertices(verts, uvs, colors);

            if (verts.Count == 0) return;

            // Преобразуем List<Vector3> в формат float[] для RenderQuadCustomVertsArray
            // Формат: X, Y, U, V, Layer
            // Нам нужно спроецировать 3D вертексы в 2D пространство иконки

            // Центр иконки
            float cx = x + size / 2.0f;
            float cy = y + size / 2.0f;
            float scale = size * 0.45f; // Масштаб модели внутри слота

            // Для 3D вида блока мы имитируем изометрию (поворот)
            // Но если это плоский предмет (инструмент), то поворота быть не должно (кроме легкого наклона если надо)

            bool isFlat = block.IsItem || !block.IsSolid;

            // Матрица трансформации для иконки
            Matrix4 rotation;
            if (isFlat)
            {
                // Плоские предметы просто масштабируем, без поворота (или чуть-чуть)
                rotation = Matrix4.Identity;
                scale = size * 0.8f; // Чуть крупнее
            }
            else
            {
                // Блоки (включая ступеньки) поворачиваем, чтобы было видно объем
                rotation = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(30)) *
                           Matrix4.CreateRotationY(MathHelper.DegreesToRadians(45));
            }

            // Собираем массив вершин для квадов
            // В Block.cs вершины генерируются пачками по 4 (квады)
            // RenderQuadCustomVertsArray рисует 2 треугольника (6 вершин) из 4 входных вершин (по индексам IBO в UIRenderer)
            // НО! UIRenderer использует VBO фиксированного размера на 1 квад (4 вершины).
            // Поэтому мы должны рисовать каждый квад отдельно.

            for (int i = 0; i < verts.Count; i += 4)
            {
                float[] quadVerts = new float[4 * 5]; // 4 вертекса * 5 float

                for (int v = 0; v < 4; v++)
                {
                    Vector3 pos3 = verts[i + v];

                    // Вращаем
                    Vector3 rotPos = Vector3.TransformPosition(pos3, rotation);

                    // Проецируем на экран (просто сдвигаем и масштабируем)
                    float screenX = cx + (rotPos.X * scale);
                    float screenY = cy - (rotPos.Y * scale); // Y инвертирован в UI

                    quadVerts[v * 5 + 0] = screenX;
                    quadVerts[v * 5 + 1] = screenY;
                    quadVerts[v * 5 + 2] = uvs[i + v].X;
                    quadVerts[v * 5 + 3] = uvs[i + v].Y;
                    quadVerts[v * 5 + 4] = uvs[i + v].Z; // Layer
                }

                // Берем цвет из первой вершины квада
                Vector3 tint = colors[i];

                // Рисуем этот квад
                RenderQuadCustomVertsArray(quadVerts, array, tint, 1.0f);
            }
        }

        public void RenderInventory(InventorySystem inv, int selectedSlot, bool showBigInv, TextRenderer textRenderer, float screenW, float screenH, TextureArray array)
        {
            float scale = 3.0f;

            if (showBigInv)
            {
                DrawRect(0, 0, screenW, screenH, Vector3.Zero, 0.65f);
                float panelW = 176 * scale;
                float panelH = 166 * scale;
                float panelX = (screenW - panelW) / 2;
                float panelY = (screenH - panelH) / 2;

                RenderQuad(panelX, panelY, panelW, panelH, TexPanelBig, Vector3.One, 1f, new Vector2(1, 1));
                float slotSize = 18 * scale;
                float gridStartX = panelX + (7 * scale);
                float gridStartY = panelY + (17 * scale);

                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        float sx = gridStartX + (col * slotSize);
                        float sy = gridStartY + (row * slotSize);
                        RenderQuad(sx, sy, slotSize, slotSize, TexSlot, Vector3.One, 1f, new Vector2(1, 1));
                        int index = InventorySystem.HOTBAR_SIZE + (row * 9) + col;

                        ItemStack stack = inv.GetStack(index);
                        if (stack != null && stack != inv.DragStack)
                        {
                            RenderIcon3D(sx, sy, 16 * scale, stack.Type, array);
                            if (stack.Count > 1)
                            {
                                textRenderer.UpdateText(stack.Count.ToString());
                                textRenderer.Render(sx, sy, 0.3f, projectionMatrix);
                            }
                        }
                    }
                }

                float hbY = panelY + (142 * scale);
                for (int i = 0; i < 9; i++)
                {
                    float sx = gridStartX + (i * slotSize);
                    float sy = hbY;
                    RenderQuad(sx, sy, slotSize, slotSize, TexSlot, Vector3.One, 1f, new Vector2(1, 1));
                    if (i == selectedSlot) DrawRect(sx, sy, slotSize, slotSize, new Vector3(1, 1, 1), 0.2f);

                    ItemStack stack = inv.GetStack(i);
                    if (stack != null && stack != inv.DragStack)
                    {
                        RenderIcon3D(sx, sy, 16 * scale, stack.Type, array);
                        if (stack.Count > 1)
                        {
                            textRenderer.UpdateText(stack.Count.ToString());
                            textRenderer.Render(sx, sy, 0.3f, projectionMatrix);
                        }
                    }
                }
            }
            else
            {
                float hbW = 182 * scale;
                float hbH = 22 * scale;
                float hbX = (screenW - hbW) / 2;
                float hbY = screenH - hbH - 50;

                RenderQuad(hbX, hbY, hbW, hbH, TexHotbar, Vector3.One, 1f, new Vector2(1, 1));
                float slotStep = 20 * scale;
                float selX = hbX + (selectedSlot * slotStep) - (1 * scale);
                DrawRect(selX + (2 * scale), hbY - (1 * scale), 22 * scale, 24 * scale, new Vector3(1, 1, 1), 0.25f);

                for (int i = 0; i < InventorySystem.HOTBAR_SIZE; i++)
                {
                    ItemStack stack = inv.GetStack(i);
                    if (stack != null)
                    {
                        float ix = hbX + (3 * scale) + (i * 20 * scale);
                        float iy = hbY + (3 * scale);
                        RenderIcon3D(ix, iy, 16 * scale, stack.Type, array);
                        if (stack.Count > 1)
                        {
                            textRenderer.UpdateText(stack.Count.ToString());
                            textRenderer.Render(ix, iy + (8 * scale), 0.5f, projectionMatrix);
                        }
                    }
                }
            }
        }
        public void RenderSlider(float x, float y, float w, float h, float value, float min, float max)
        {
            Prepare();
            RenderQuad(x, y + (h / 2) - 10, w, 20, TexSliderTrack, new Vector3(1f), 1.0f, new Vector2(1, 1));
            float t = (value - min) / (max - min);
            float handleSize = 40.0f;
            float handleX = x + (t * w) - (handleSize / 2);
            float handleY = y + (h / 2) - (handleSize / 2);
            RenderQuad(handleX, handleY, handleSize, handleSize, TexSliderHandle, new Vector3(1f), 1.0f, new Vector2(1, 1));
        }
        public void RenderDraggedItem(ItemStack stack, float mx, float my, TextRenderer tr, TextureArray array)
        {
            if (stack == null) return;
            RenderIcon3D(mx - 20, my - 20, 40, stack.Type, array);
            if (stack.Count > 1)
            {
                tr.UpdateText(stack.Count.ToString());
                tr.Render(mx, my, 0.4f, projectionMatrix);
            }
        }

        public Matrix4 GetProjection() { return projectionMatrix; }
    }
}