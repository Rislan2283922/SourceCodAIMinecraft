using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;

namespace EarthBound.World
{
    internal static class RegionManager
    {
        // === СОХРАНЕНИЕ ===
        public static void SaveChunks(string worldFolder, Dictionary<Vector2i, Chunk> activeChunks)
        {
            string regionPath = Path.Combine("saves", worldFolder, "regions");
            if (!Directory.Exists(regionPath)) Directory.CreateDirectory(regionPath);

            // 1. Группируем чанки по файлам регионов (32x32 чанка)
            var regionsToUpdate = new Dictionary<Vector2i, List<Chunk>>();

            foreach (var chunk in activeChunks.Values)
            {
                // Если чанк не менялся (IsModified), можно было бы пропускать, но для надежности пишем всё
                int rx = (int)Math.Floor((double)chunk.Coord.X / 32.0);
                int rz = (int)Math.Floor((double)chunk.Coord.Y / 32.0);
                Vector2i rCoord = new Vector2i(rx, rz);

                if (!regionsToUpdate.ContainsKey(rCoord)) regionsToUpdate[rCoord] = new List<Chunk>();
                regionsToUpdate[rCoord].Add(chunk);
            }

            // 2. Проходим по каждому региону
            foreach (var kvp in regionsToUpdate)
            {
                Vector2i rCoord = kvp.Key;
                List<Chunk> chunksInMemory = kvp.Value;
                string filename = Path.Combine(regionPath, $"r.{rCoord.X}.{rCoord.Y}.bin");

                // Словарь: Координата -> Байты данных
                // Сюда мы сложим ВСЕ чанки (и старые с диска, и новые из памяти)
                Dictionary<Vector2i, byte[]> finalData = new Dictionary<Vector2i, byte[]>();

                // А. СЧИТЫВАЕМ СТАРЫЙ ФАЙЛ (чтобы не потерять чанки, которые сейчас не загружены, но были там)
                if (File.Exists(filename))
                {
                    try
                    {
                        using (FileStream fs = new FileStream(filename, FileMode.Open))
                        using (BinaryReader reader = new BinaryReader(fs))
                        {
                            int count = reader.ReadInt32();
                            for (int i = 0; i < count; i++)
                            {
                                int cx = reader.ReadInt32();
                                int cz = reader.ReadInt32();
                                int len = reader.ReadInt32();
                                byte[] data = reader.ReadBytes(len);
                                finalData[new Vector2i(cx, cz)] = data;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[SAVE ERROR] Corrupt region file {filename}: {e.Message}");
                    }
                }

                // Б. ОБНОВЛЯЕМ ДАННЫМИ ИЗ ИГРЫ
                foreach (Chunk c in chunksInMemory)
                {
                    // Serialize превращает блоки в байты. 
                    // Если ты поставил блок, он в chunkBlocks, и Serialize это вернет.
                    finalData[c.Coord] = c.Serialize();
                }

                // В. ЗАПИСЫВАЕМ ВСЁ В ФАЙЛ (через временный)
                string tempFile = filename + ".tmp";
                try
                {
                    using (FileStream fs = new FileStream(tempFile, FileMode.Create))
                    using (BinaryWriter writer = new BinaryWriter(fs))
                    {
                        writer.Write(finalData.Count);
                        foreach (var entry in finalData)
                        {
                            writer.Write(entry.Key.X);
                            writer.Write(entry.Key.Y);
                            writer.Write(entry.Value.Length);
                            writer.Write(entry.Value);
                        }
                    }

                    // Атомарная замена файла
                    if (File.Exists(filename)) File.Delete(filename);
                    File.Move(tempFile, filename);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[SAVE ERROR] Failed to write {filename}: {e.Message}");
                }
            }
        }

        // === ЗАГРУЗКА ===
        public static void LoadRegion(string worldFolder, int rX, int rZ, Dictionary<Vector2i, Chunk> loadedChunks)
        {
            string path = Path.Combine("saves", worldFolder, "regions", $"r.{rX}.{rZ}.bin");
            if (!File.Exists(path)) return;

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        int cx = reader.ReadInt32();
                        int cz = reader.ReadInt32();
                        int len = reader.ReadInt32();
                        byte[] data = reader.ReadBytes(len);

                        Vector2i coord = new Vector2i(cx, cz);

                        // Если чанка нет в памяти, создаем и наполняем его
                        if (!loadedChunks.ContainsKey(coord))
                        {
                            Chunk chunk = new Chunk(coord);
                            chunk.Deserialize(data);
                            loadedChunks[coord] = chunk;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[LOAD ERROR] Failed to load region {rX}.{rZ}: {e.Message}");
            }
        }
    }
}