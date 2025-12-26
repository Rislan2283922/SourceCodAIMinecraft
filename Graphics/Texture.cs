using System;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System.IO;

namespace EarthBound.Graphics
{
    internal class Texture
    {
        public int ID;
        public int Width, Height;

        public Texture(String filepath)
        {
            ID = GL.GenTexture();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, ID);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            StbImage.stbi_set_flip_vertically_on_load(1);

            // === ИСПРАВЛЕНИЕ ПУТЕЙ ===
            string fullPath = filepath; // Сначала пробуем как есть (для иконок сохранений)

            if (!File.Exists(fullPath))
            {
                fullPath = "../../../assets/" + filepath;
                if (!File.Exists(fullPath)) fullPath = "../../../Textures/" + filepath;
            }

            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[ERROR] Texture not found: {filepath}");
                // Чтобы не крашнулось, создаем пустую текстуру 1x1
                byte[] white = { 255, 0, 255, 255 };
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, white);
                return;
            }

            try
            {
                using (Stream stream = File.OpenRead(fullPath))
                {
                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                    Width = image.Width;
                    Height = image.Height;
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[TEXTURE ERROR] {fullPath}: {e.Message}");
            }

            Unbind();
        }

        public void Bind() { GL.BindTexture(TextureTarget.Texture2D, ID); }
        public void Unbind() { GL.BindTexture(TextureTarget.Texture2D, 0); }
        public void Delete() { GL.DeleteTexture(ID); }
    }
}