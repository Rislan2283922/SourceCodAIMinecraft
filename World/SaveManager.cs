using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;

namespace EarthBound.World
{
    public struct WorldMetadata
    {
        public string Name;
        public string FolderName;
        public int Seed;
        public string LastPlayed;
        public string GameMode;
    }

    internal static class SaveManager
    {
        private static string SavesPath = "saves";

        public static void Init()
        {
            if (!Directory.Exists(SavesPath))
            {
                Directory.CreateDirectory(SavesPath);
            }
        }

        public static List<WorldMetadata> GetWorlds()
        {
            List<WorldMetadata> worlds = new List<WorldMetadata>();
            if (!Directory.Exists(SavesPath)) return worlds;

            // ИСПРАВЛЕНИЕ: Сортируем папки по времени последнего изменения (от старых к новым)
            // Тогда последний элемент списка (Count - 1) ВСЕГДА будет тем миром, который мы только что создали или играли.
            var directories = new DirectoryInfo(SavesPath).GetDirectories()
                                .OrderBy(d => d.LastWriteTime)
                                .ToList();

            foreach (var dirInfo in directories)
            {
                string dir = dirInfo.FullName;
                string levelDat = Path.Combine(dir, "level.dat");
                if (File.Exists(levelDat))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(levelDat);
                        if (lines.Length >= 4)
                        {
                            worlds.Add(new WorldMetadata
                            {
                                FolderName = dirInfo.Name,
                                Name = lines[0],
                                Seed = int.Parse(lines[1]),
                                LastPlayed = lines[2],
                                GameMode = lines[3]
                            });
                        }
                    }
                    catch { }
                }
            }
            return worlds;
        }
        public static float LoadWorldTime(string folderName)
        {
            try
            {
                string path = Path.Combine(SavesPath, folderName, "level.dat");
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    if (lines.Length >= 5)
                    {
                        if (float.TryParse(lines[4], out float t)) return t;
                    }
                }
            }
            catch { }
            return 300.0f; // Default day
        }
        public static void CreateWorld(string name, int seed)
        {
            string safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrEmpty(safeName)) safeName = "World_" + new Random().Next(1000);

            string worldPath = Path.Combine(SavesPath, safeName);
            int counter = 1;
            while (Directory.Exists(worldPath))
            {
                worldPath = Path.Combine(SavesPath, safeName + "_" + counter);
                counter++;
            }

            Directory.CreateDirectory(worldPath);
            Directory.CreateDirectory(Path.Combine(worldPath, "regions"));
            Directory.CreateDirectory(Path.Combine(worldPath, "playerdata"));

            // Default time 300 (Day)
            SaveLevelData(worldPath, name, seed, "Survival", 300.0f);

            using (Bitmap bmp = new Bitmap(64, 64))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp)) { g.Clear(Color.Gray); }
                bmp.Save(Path.Combine(worldPath, "icon.png"), ImageFormat.Png);
            }
        }

        public static void UpdateWorldData(string folderName, string prettyName, int seed, float time)
        {
            string path = Path.Combine(SavesPath, folderName);
            if (Directory.Exists(path))
            {
                SaveLevelData(path, prettyName, seed, "Survival", time);
            }
        }

        private static void SaveLevelData(string path, string name, int seed, string mode, float time)
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(path, "level.dat")))
            {
                sw.WriteLine(name);
                sw.WriteLine(seed);
                sw.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                sw.WriteLine(mode);
                sw.WriteLine(time.ToString("0.00")); // Save time on line 5
            }
        }
        public static void DeleteWorld(string folderName)
        {
            string path = Path.Combine(SavesPath, folderName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public static void SavePlayer(string worldFolder, string playerName, PlayerData data)
        {
            string path = Path.Combine(SavesPath, worldFolder, "playerdata");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            string uuid = GenerateUUID(playerName);
            string file = Path.Combine(path, uuid + ".dat");

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(file, json);
        }

        public static PlayerData LoadPlayer(string worldFolder, string playerName)
        {
            string path = Path.Combine(SavesPath, worldFolder, "playerdata");
            string uuid = GenerateUUID(playerName);
            string file = Path.Combine(path, uuid + ".dat");

            if (File.Exists(file))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    return JsonSerializer.Deserialize<PlayerData>(json);
                }
                catch { return null; }
            }
            return null;
        }

        private static string GenerateUUID(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return new Guid(hashBytes).ToString();
            }
        }

        public static void CaptureIcon(string worldFolder, int width, int height)
        {
            try
            {
                string path = Path.Combine(SavesPath, worldFolder, "icon.png");

                Bitmap bmp = new Bitmap(width, height);
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);

                bmp.UnlockBits(data);
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

                Bitmap icon = new Bitmap(bmp, new Size(64, 64));
                icon.Save(path, ImageFormat.Png);

                bmp.Dispose();
                icon.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR] Failed to capture icon: {e.Message}");
            }
        }
    }
}