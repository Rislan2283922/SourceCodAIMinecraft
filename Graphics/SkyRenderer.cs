using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace EarthBound.Graphics
{
    public class SkyRenderer
    {
        private VAO vaoQuad; // For Sun and Moon
        private VAO vaoBox;  // For Stars (Skybox) doesn't need extra VBO, we reuse quad logic logically

        private ShaderProgram shader;
        private int textureSun, textureMoon, textureStars;

        public SkyRenderer()
        {
            shader = new ShaderProgram("UI.vert", "UI.frag");

            // --- 1. SETUP QUAD (For Sun/Moon) ---
            float[] quad = {
                -0.5f, -0.5f, 0, 0,
                 0.5f, -0.5f, 1, 0,
                 0.5f,  0.5f, 1, 1,
                -0.5f,  0.5f, 0, 1
            };

            vaoQuad = new VAO();
            vaoQuad.Bind();

            // Manually upload data (reusing VBO logic manually to keep it simple)
            int vboID = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            vaoQuad.Unbind();

            textureSun = CreateCircleTexture(255, 255, 200, 255);
            textureMoon = CreateCircleTexture(200, 200, 200, 255);
            textureStars = CreateStarTexture();
        }

        public void Render(Camera cam, World.WorldTime time)
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            shader.Bind();

            Matrix4 view = cam.GetViewMatrix();
            // Remove translation so sky stays around player
            view.Row3 = new Vector4(0, 0, 0, 1);
            Matrix4 proj = cam.GetProjectionMatrix();

            // --- 1. DRAW STARS (SKYBOX) ---
            if (time.StarAlpha > 0.01f)
            {
                GL.BindTexture(TextureTarget.Texture2D, textureStars);
                vaoQuad.Bind();

                // To make a skybox from a single quad, we draw it 6 times with rotations
                Matrix4[] faces = new Matrix4[]
                {
                    Matrix4.CreateTranslation(0, 0, -0.5f), // Front
                    Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(-0.5f, 0, 0), // Right
                    Matrix4.CreateRotationY(MathHelper.DegreesToRadians(180)) * Matrix4.CreateTranslation(0, 0, 0.5f), // Back
                    Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-90)) * Matrix4.CreateTranslation(0.5f, 0, 0), // Left
                    Matrix4.CreateRotationX(MathHelper.DegreesToRadians(90)) * Matrix4.CreateTranslation(0, 0.5f, 0), // Top
                    Matrix4.CreateRotationX(MathHelper.DegreesToRadians(-90)) * Matrix4.CreateTranslation(0, -0.5f, 0), // Bottom
                };

                // Sky rotation over time
                Matrix4 skyRot = Matrix4.CreateRotationY(time.CurrentTime * 0.002f);
                // Scale huge
                Matrix4 scale = Matrix4.CreateScale(300.0f);

                GL.Uniform3(GL.GetUniformLocation(shader.ID, "colorTint"), Vector3.One);
                GL.Uniform1(GL.GetUniformLocation(shader.ID, "alpha"), time.StarAlpha);

                foreach (var faceMat in faces)
                {
                    // Order: Face -> Scale (make box big) -> Rotate (Spin sky) -> View -> Proj
                    Matrix4 model = faceMat * scale * skyRot;
                    Matrix4 final = model * view * proj;

                    GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "projection"), false, ref final);
                    GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
                }
            }

            // --- 2. SUN & MOON ---
            float orbitRadius = 60.0f;

            Vector3 sunPos = new Vector3(
                MathF.Cos(time.SunAngle) * orbitRadius,
                MathF.Sin(time.SunAngle) * orbitRadius,
                0
            );

            // Draw Sun
            DrawCelestialBody(sunPos, view, proj, textureSun, 15.0f, new Vector3(1.0f, 0.9f, 0.6f));

            // Draw Moon
            DrawCelestialBody(-sunPos, view, proj, textureMoon, 12.0f, Vector3.One);

            vaoQuad.Unbind();
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
        }

        private void DrawCelestialBody(Vector3 pos, Matrix4 view, Matrix4 proj, int texID, float size, Vector3 color)
        {
            // FIX: "The Singularity"
            // When pos is (0, 60, 0) (directly up), LookAt(pos, Zero, UnitY) fails because Forward is parallel to Up.
            // We need a stable Up vector.

            Matrix4 billboard;

            // If the object is very high up or down (close to Y axis)
            if (MathF.Abs(pos.X) < 0.1f && MathF.Abs(pos.Z) < 0.1f)
            {
                // Use UnitZ as "Up" temporarily to avoid crash
                billboard = Matrix4.LookAt(pos, Vector3.Zero, Vector3.UnitZ).Inverted();
            }
            else
            {
                // Normal behavior
                billboard = Matrix4.LookAt(pos, Vector3.Zero, Vector3.UnitY).Inverted();
            }

            Matrix4 model = Matrix4.CreateScale(size) * billboard;
            Matrix4 final = model * view * proj;

            GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "projection"), false, ref final);
            GL.Uniform3(GL.GetUniformLocation(shader.ID, "colorTint"), color);
            GL.Uniform1(GL.GetUniformLocation(shader.ID, "alpha"), 1.0f);

            GL.BindTexture(TextureTarget.Texture2D, texID);
            vaoQuad.Bind();
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        }

        private int CreateCircleTexture(int r, int g, int b, int a)
        {
            int size = 128;
            Bitmap bmp = new Bitmap(size, size);
            using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(bmp))
            {
                gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                gfx.Clear(Color.Transparent);
                gfx.FillEllipse(new SolidBrush(Color.FromArgb(a, r, g, b)), 10, 10, size - 20, size - 20);
            }
            return LoadBmp(bmp);
        }

        private int CreateStarTexture()
        {
            int size = 512;
            Bitmap bmp = new Bitmap(size, size);
            using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(bmp))
            {
                gfx.Clear(Color.Transparent);
                Random rnd = new Random();
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    for (int i = 0; i < 400; i++)
                    {
                        int x = rnd.Next(size);
                        int y = rnd.Next(size);
                        int s = rnd.Next(2, 4);
                        // Make random transparency for twinkling effect
                        int alpha = rnd.Next(150, 255);
                        brush.Color = Color.FromArgb(alpha, 255, 255, 255);
                        gfx.FillRectangle(brush, x, y, s, s);
                    }
                }
            }
            return LoadBmp(bmp);
        }

        private int LoadBmp(Bitmap bmp)
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Fix for Skybox seams (Clamp to Edge)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            return id;
        }
    }
}