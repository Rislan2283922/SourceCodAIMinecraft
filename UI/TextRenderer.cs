using System.Drawing;
using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using EarthBound.Graphics;

namespace EarthBound.UI
{
    internal class TextRenderer
    {
        private int textureId;
        private int vao, vbo, ebo;
        private ShaderProgram shader;
        private int bmpW, bmpH;

        public TextRenderer(int width, int height)
        {
            textureId = GL.GenTexture();
            shader = new ShaderProgram("UI.vert", "UI.frag");

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            // 4 verts * (2 pos + 3 tex) * float size = 4 * 5 * 4
            GL.BufferData(BufferTarget.ArrayBuffer, 4 * 5 * 4, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            uint[] indices = { 0, 1, 2, 2, 3, 0 };
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * 4, indices, BufferUsageHint.StaticDraw);

            // Pos: 2 floats, stride 5 floats
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // Tex: 3 floats (vec3), stride 5 floats, offset 2 floats
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.BindVertexArray(0);
        }

        public void UpdateText(string text)
        {
            int w = 1024; int h = 128;
            bmpW = w; bmpH = h;

            using (Bitmap bmp = new Bitmap(w, h))
            using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(bmp))
            {
                gfx.Clear(Color.Transparent);
                using (Font font = new Font(FontFamily.GenericMonospace, 24, FontStyle.Bold))
                {
                    gfx.DrawString(text, font, Brushes.Black, 2, 2);
                    gfx.DrawString(text, font, Brushes.White, 0, 0);
                }

                BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.BindTexture(TextureTarget.Texture2D, textureId);
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                bmp.UnlockBits(data);
            }
        }

        public void Render(float x, float y, float scale, Matrix4 projection)
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            shader.Bind();

            GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "projection"), false, ref projection);
            GL.Uniform3(GL.GetUniformLocation(shader.ID, "colorTint"), Vector3.One);
            GL.Uniform1(GL.GetUniformLocation(shader.ID, "alpha"), 1.0f);

            // Text uses 2D texture, so useArray = 0
            GL.Uniform1(GL.GetUniformLocation(shader.ID, "useArray"), 0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            float w = bmpW * scale;
            float h = bmpH * scale;

            // Updated verts to include Z=0 for texCoord
            float[] verts = {
                x,     y,     0.0f, 0.0f, 0.0f, // TL
                x + w, y,     1.0f, 0.0f, 0.0f, // TR
                x + w, y + h, 1.0f, 1.0f, 0.0f, // BR
                x,     y + h, 0.0f, 1.0f, 0.0f  // BL
            };

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, verts.Length * sizeof(float), verts);

            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Enable(EnableCap.DepthTest);
        }
    }
}