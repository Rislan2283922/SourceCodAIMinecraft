using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;
using EarthBound.World;
using StbImageSharp;
namespace EarthBound.Graphics
{
    internal static class ItemMeshGenerator
    {
        private static Dictionary<BlockType, VAO> meshCache = new Dictionary<BlockType, VAO>();
        private static Dictionary<BlockType, int> indexCountCache = new Dictionary<BlockType, int>();
        public static void Init() { }

        public static void RenderItem(BlockType type, ShaderProgram shader, Matrix4 modelMatrix, TextureArray array)
        {
            if (!meshCache.ContainsKey(type))
            {
                BuildMesh(type, array);
            }

            if (meshCache.ContainsKey(type))
            {
                GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "model"), true, ref modelMatrix);

                VAO vao = meshCache[type];
                int count = indexCountCache[type];

                if (vao != null && count > 0)
                {
                    GL.Disable(EnableCap.CullFace);
                    vao.Bind();
                    GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, 0);
                    vao.Unbind();
                    GL.Enable(EnableCap.CullFace);
                }
            }
        }

        private static void BuildMesh(BlockType type, TextureArray array)
        {
            string path = TextureData.GetPath(type, Faces.FRONT);

            // USE THE ROBUST SEARCH FROM TEXTUREARRAY
            string fullPath = TextureArray.SmartResolvePath(path);

            if (fullPath == null)
            {
                Console.WriteLine($"[ITEM MESH] Could not find texture for {type} at {path}");
                return;
            }

            try
            {
                using (var fs = File.OpenRead(fullPath))
                using (Bitmap bmp = new Bitmap(fs))
                {
                    Bitmap finalBmp = bmp;
                    if (bmp.Width != 16 || bmp.Height != 16)
                    {
                        finalBmp = new Bitmap(bmp, 16, 16);
                    }

                    GenerateMeshFromBitmap(finalBmp, type, array);

                    if (finalBmp != bmp) finalBmp.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ITEM MESH ERROR] {type}: {e.Message}");
            }
        }

        private static void GenerateMeshFromBitmap(Bitmap bmp, BlockType type, TextureArray array)
        {
            bool[,] pixels = new bool[16, 16];
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    Color c = bmp.GetPixel(x, y);
                    pixels[x, 15 - y] = c.A > 0;
                }
            }

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> uvCoords = new List<Vector3>();
            List<Vector3> colors = new List<Vector3>();

            // --- FIX: Add Light List ---
            List<Vector2> lights = new List<Vector2>();
            // ---------------------------

            List<uint> indices = new List<uint>();
            uint idx = 0;

            float pixelSize = 1.0f / 16.0f;
            float thickness = 1.0f / 16.0f;

            int layer = 0;
            if (TextureData.BlockLayerIndices.ContainsKey(type))
                layer = TextureData.BlockLayerIndices[type][Faces.FRONT];

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    if (!pixels[x, y]) continue;

                    float px = (x / 16.0f) - 0.5f;
                    float py = (y / 16.0f) - 0.5f;

                    float uMin = x / 16.0f;
                    float uMax = (x + 1) / 16.0f;
                    float vMin = y / 16.0f;
                    float vMax = (y + 1) / 16.0f;

                    // Front
                    AddQuad(verts, uvCoords, colors, lights, indices, ref idx,
                        new Vector3(px, py, thickness / 2),
                        new Vector3(px + pixelSize, py, thickness / 2),
                        new Vector3(px + pixelSize, py + pixelSize, thickness / 2),
                        new Vector3(px, py + pixelSize, thickness / 2),
                        uMin, vMin, uMax, vMax, layer
                    );

                    // Back
                    AddQuad(verts, uvCoords, colors, lights, indices, ref idx,
                        new Vector3(px + pixelSize, py, -thickness / 2),
                        new Vector3(px, py, -thickness / 2),
                        new Vector3(px, py + pixelSize, -thickness / 2),
                        new Vector3(px + pixelSize, py + pixelSize, -thickness / 2),
                        uMin, vMin, uMax, vMax, layer
                    );

                    if (x == 0 || !pixels[x - 1, y])
                        AddSide(verts, uvCoords, colors, lights, indices, ref idx, px, py, px, py + pixelSize, thickness, uMin, vMin, uMin, vMax, layer);
                    if (x == 15 || !pixels[x + 1, y])
                        AddSide(verts, uvCoords, colors, lights, indices, ref idx, px + pixelSize, py + pixelSize, px + pixelSize, py, thickness, uMax, vMax, uMax, vMin, layer);
                    if (y == 0 || !pixels[x, y - 1])
                        AddSide(verts, uvCoords, colors, lights, indices, ref idx, px + pixelSize, py, px, py, thickness, uMax, vMin, uMin, vMin, layer);
                    if (y == 15 || !pixels[x, y + 1])
                        AddSide(verts, uvCoords, colors, lights, indices, ref idx, px, py + pixelSize, px + pixelSize, py + pixelSize, thickness, uMin, vMax, uMax, vMax, layer);
                }
            }

            VAO newVao = new VAO();
            newVao.Bind();
            VBO vboP = new VBO(verts); newVao.LinkToVAO(0, 3, vboP);
            VBO vboU = new VBO(uvCoords); newVao.LinkToVAO(1, 3, vboU);
            VBO vboC = new VBO(colors); newVao.LinkToVAO(2, 3, vboC);

            // --- FIX: Bind Light VBO ---
            VBO vboL = new VBO(lights); newVao.LinkToVAO(3, 2, vboL);
            // ---------------------------

            IBO newIbo = new IBO(indices);
            newVao.Unbind();

            meshCache[type] = newVao;
            indexCountCache[type] = indices.Count;
        }

        private static void AddQuad(List<Vector3> v, List<Vector3> uv, List<Vector3> c, List<Vector2> l, List<uint> i, ref uint idx,
            Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, float uMin, float vMin, float uMax, float vMax, int layer)
        {
            v.Add(bl); v.Add(br); v.Add(tr); v.Add(tl);
            uv.Add(new Vector3(uMin, vMin, layer));
            uv.Add(new Vector3(uMax, vMin, layer));
            uv.Add(new Vector3(uMax, vMax, layer));
            uv.Add(new Vector3(uMin, vMax, layer));

            // Full Brightness for items
            for (int k = 0; k < 4; k++)
            {
                c.Add(Vector3.One);
                l.Add(new Vector2(15, 15));
            }

            i.Add(idx); i.Add(idx + 1); i.Add(idx + 2);
            i.Add(idx + 2); i.Add(idx + 3); i.Add(idx + 0);
            idx += 4;
        }

        private static void AddSide(List<Vector3> v, List<Vector3> uv, List<Vector3> c, List<Vector2> l, List<uint> i, ref uint idx,
           float x1, float y1, float x2, float y2, float thick, float u1, float v1, float u2, float v2, int layer)
        {
            v.Add(new Vector3(x1, y1, thick / 2));
            v.Add(new Vector3(x2, y2, thick / 2));
            v.Add(new Vector3(x2, y2, -thick / 2));
            v.Add(new Vector3(x1, y1, -thick / 2));

            uv.Add(new Vector3(u1, v1, layer));
            uv.Add(new Vector3(u2, v2, layer));
            uv.Add(new Vector3(u2, v2, layer));
            uv.Add(new Vector3(u1, v1, layer));

            // Full Brightness for items (sides slightly darker color but full light)
            for (int k = 0; k < 4; k++)
            {
                c.Add(new Vector3(0.7f));
                l.Add(new Vector2(15, 15));
            }

            i.Add(idx); i.Add(idx + 1); i.Add(idx + 2);
            i.Add(idx + 2); i.Add(idx + 3); i.Add(idx + 0);
            idx += 4;
        }
    }
}