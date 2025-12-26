using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
namespace EarthBound.Graphics
{
    public class TextureArray
    {
        public readonly int ID;
        public const int WIDTH = 32;
        public const int HEIGHT = 32;
        private const int MAX_LAYERS = 256;
        private readonly Dictionary<string, int> _textureLayers = new Dictionary<string, int>();
        private int _currentLayerCount = 0;

        public TextureArray()
        {
            ID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, ID);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 4, SizedInternalFormat.Rgba8, WIDTH, HEIGHT, MAX_LAYERS);

            byte[] missingData = GenerateMissingTexturePattern();
            GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, 0, WIDTH, HEIGHT, 1, PixelFormat.Rgba, PixelType.UnsignedByte, missingData);

            _textureLayers["missing"] = 0;
            _currentLayerCount = 1;

            Console.WriteLine($"[TEXTURE ARRAY] Initialized. Size: {WIDTH}x{HEIGHT}, Capacity: {MAX_LAYERS}");
        }

        public int GetOrLoadLayer(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return 0;
            string key = rawPath.Replace('\\', '/').ToLowerInvariant();

            if (_textureLayers.TryGetValue(key, out int layer)) return layer;

            if (_currentLayerCount >= MAX_LAYERS)
            {
                Console.WriteLine($"[TEXTURE ARRAY CRITICAL] Max layers ({MAX_LAYERS}) exceeded. Returning 0.");
                return 0;
            }

            return LoadTexture(rawPath, key);
        }

        private int LoadTexture(string originalPath, string normalizedKey)
        {
            // Используем умный поиск пути
            string fullPath = SmartResolvePath(originalPath);

            if (fullPath == null || !File.Exists(fullPath))
            {
                Console.WriteLine($"[TEXTURE ERROR] File not found: {originalPath} (Checked up to 4 dirs up)");
                return 0; // Return missing texture layer
            }

            try
            {
                using (Stream stream = File.OpenRead(fullPath))
                {
                    StbImage.stbi_set_flip_vertically_on_load(1);
                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                    byte[] finalData = image.Data;

                    if (image.Width != WIDTH || image.Height != HEIGHT)
                    {
                        finalData = ResizePixels(image.Data, image.Width, image.Height, WIDTH, HEIGHT);
                    }

                    int newLayerIndex = _currentLayerCount;

                    GL.TexSubImage3D(
                        TextureTarget.Texture2DArray,
                        0, 0, 0, newLayerIndex,
                        WIDTH, HEIGHT, 1,
                        PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        finalData
                    );

                    _textureLayers[normalizedKey] = newLayerIndex;
                    _currentLayerCount++;

                    return newLayerIndex;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEXTURE LOAD FAILED] {originalPath}: {ex.Message}");
                return 0;
            }
        }

        public void GenerateMipmaps()
        {
            GL.BindTexture(TextureTarget.Texture2DArray, ID);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
            Console.WriteLine("[TEXTURE ARRAY] Mipmaps generated.");
        }

        public void Bind()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2DArray, ID);
        }

        private byte[] ResizePixels(byte[] source, int w1, int h1, int w2, int h2)
        {
            byte[] result = new byte[w2 * h2 * 4];
            double xRatio = w1 / (double)w2;
            double yRatio = h1 / (double)h2;

            for (int i = 0; i < h2; i++)
            {
                for (int j = 0; j < w2; j++)
                {
                    int px = (int)(j * xRatio);
                    int py = (int)(i * yRatio);

                    int sourceIndex = (py * w1 + px) * 4;
                    int destIndex = (i * w2 + j) * 4;

                    result[destIndex + 0] = source[sourceIndex + 0];
                    result[destIndex + 1] = source[sourceIndex + 1];
                    result[destIndex + 2] = source[sourceIndex + 2];
                    result[destIndex + 3] = source[sourceIndex + 3];
                }
            }
            return result;
        }

        /// <summary>
        /// Robustly searches for a file by checking:
        /// 1. Direct path
        /// 2. Inside 'assets/'
        /// 3. Going up directory levels (../../) to find source folder
        /// </summary>
        public static string SmartResolvePath(string path)
        {
            // 1. Check direct
            if (File.Exists(path)) return path;

            // 2. Check assets/
            string assetPath = Path.Combine("assets", path);
            if (File.Exists(assetPath)) return assetPath;

            // 3. Crawl up to 5 levels (bin/Debug/net8.0/ -> ProjectRoot)
            string currentBase = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5; i++)
            {
                currentBase = Path.Combine(currentBase, "..");

                // Try raw path relative to parent
                string tryPath = Path.Combine(currentBase, path);
                if (File.Exists(tryPath)) return tryPath;

                // Try assets/path relative to parent
                string tryAssetPath = Path.Combine(currentBase, "assets", path);
                if (File.Exists(tryAssetPath)) return tryAssetPath;
            }

            return null; // Not found
        }

        private byte[] GenerateMissingTexturePattern()
        {
            byte[] data = new byte[WIDTH * HEIGHT * 4];
            for (int i = 0; i < WIDTH * HEIGHT; i++)
            {
                int x = i % WIDTH;
                int y = i / WIDTH;
                bool isMagenta = ((x / (WIDTH / 2)) + (y / (HEIGHT / 2))) % 2 == 0;

                int idx = i * 4;
                data[idx + 0] = isMagenta ? (byte)255 : (byte)0;
                data[idx + 1] = 0;
                data[idx + 2] = isMagenta ? (byte)255 : (byte)0;
                data[idx + 3] = 255;
            }
            return data;
        }
    }
}