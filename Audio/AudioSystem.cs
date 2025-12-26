using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using NVorbis;
using EarthBound.World;
namespace EarthBound.Audio
{
    public class AudioSystem : IDisposable
    {
        private ALDevice device;
        private ALContext context;

        // Cache for loaded sound buffers (FilePath -> BufferID)
        private Dictionary<string, int> bufferCache = new Dictionary<string, int>();

        // List of sound sources
        private const int MAX_SOURCES = 32;
        private List<int> sources = new List<int>();

        // Material sound mappings
        private Dictionary<string, List<string>> footstepLibrary = new Dictionary<string, List<string>>();
        private string blockPlaceSound = "assets/audio/blocks/block_place.ogg";

        // Ambient timer
        private float ambientWaterTimer = 0.0f;

        public AudioSystem()
        {
            try
            {
                // Initialize OpenAL
                device = ALC.OpenDevice(null);
                context = ALC.CreateContext(device, (int[])null);
                ALC.MakeContextCurrent(context);

                // Check errors
                ALError error = AL.GetError();
                if (error != ALError.NoError)
                {
                    Console.WriteLine($"[AUDIO ERROR] Init failed: {error}");
                }

                // Generate Sources Pool
                for (int i = 0; i < MAX_SOURCES; i++)
                {
                    int src = AL.GenSource();
                    if (AL.GetError() == ALError.NoError)
                        sources.Add(src);
                }

                // Set Distance Model for 3D Audio
                AL.DistanceModel(ALDistanceModel.LinearDistanceClamped);

                Console.WriteLine("[AUDIO] System Initialized.");
                LoadLibrary();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUDIO CRITICAL] {ex.Message}");
            }
        }

        private void LoadLibrary()
        {
            // Scan asset folder for footsteps
            string root = Path.Combine("assets", "audio", "footsteps");
            if (!Directory.Exists(root)) return;

            string[] folders = Directory.GetDirectories(root);
            foreach (var folder in folders)
            {
                string category = new DirectoryInfo(folder).Name; // e.g., "grass", "wood", "snow", "ice"
                string[] files = Directory.GetFiles(folder, "*.ogg");

                if (files.Length > 0)
                {
                    footstepLibrary[category] = new List<string>(files);
                    foreach (var f in files) GetBuffer(f);
                }
            }
            // Preload place sound
            GetBuffer(blockPlaceSound);
        }
        // Get or Load Buffer from OGG
        private int GetBuffer(string filepath)
        {
            if (bufferCache.ContainsKey(filepath)) return bufferCache[filepath];

            if (!File.Exists(filepath))
            {
                // Fallback for paths without "assets/" prefix if called incorrectly
                if (File.Exists("assets/" + filepath)) filepath = "assets/" + filepath;
                else return 0;
            }

            try
            {
                using (var vorbis = new VorbisReader(filepath))
                {
                    // Read all samples
                    float[] samples = new float[vorbis.TotalSamples * vorbis.Channels];
                    vorbis.ReadSamples(samples, 0, samples.Length);

                    // Convert float samples to short (16-bit)
                    short[] shortSamples = new short[samples.Length];
                    for (int i = 0; i < samples.Length; i++)
                    {
                        int temp = (int)(32767f * samples[i]);
                        if (temp > short_max) temp = short_max;
                        if (temp < short_min) temp = short_min;
                        shortSamples[i] = (short)temp;
                    }

                    int buffer = AL.GenBuffer();
                    ALFormat format = vorbis.Channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;
                    AL.BufferData(buffer, format, shortSamples, vorbis.SampleRate);

                    bufferCache[filepath] = buffer;
                    return buffer;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[AUDIO LOAD ERROR] {filepath}: {e.Message}");
                return 0;
            }
        }

        private const short short_max = short.MaxValue;
        private const short short_min = short.MinValue;

        // Find a free or unimportant source
        private int GetFreeSource()
        {
            foreach (int src in sources)
            {
                AL.GetSource(src, ALGetSourcei.SourceState, out int state);
                if ((ALSourceState)state != ALSourceState.Playing)
                    return src;
            }
            // If all busy, steal the first one (simple logic)
            return sources[0];
        }

        public void UpdateListener(Vector3 position, Vector3 lookAt, Vector3 up)
        {
            AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);

            float[] orientation = { lookAt.X, lookAt.Y, lookAt.Z, up.X, up.Y, up.Z };
            AL.Listener(ALListenerfv.Orientation, orientation);
        }

        // --- GAMEPLAY AUDIO METHODS ---

        // Play a random footstep for a material
        public void PlayFootstep(string material, Vector3 pos, float volume = 1.0f, float speed = 1.0f)
        {
            if (!footstepLibrary.ContainsKey(material)) return; // No sounds for this

            var list = footstepLibrary[material];
            string file = list[new Random().Next(list.Count)];

            PlaySound(file, pos, 0.9f + (float)(new Random().NextDouble() * 0.2f), volume, speed);
        }

        // Simulating block break by slowing down and distorting footstep sounds + reverbish feel
        // Звук удара ПОКА ломаешь (быстрый, тихий, высокий питч)
        public void PlayHitSound(string material, Vector3 pos)
        {
            if (!footstepLibrary.ContainsKey(material)) return;
            var list = footstepLibrary[material];
            string file = list[new Random().Next(list.Count)];

            // Питч выше (1.2), громкость ниже (0.4)
            PlaySound(file, pos, 1.2f, 0.4f, 1.0f);
        }

        // Замени существующий PlayBreakSound на этот (громче и сочнее):
        public void PlayBreakSound(string material, Vector3 pos)
        {
            if (!footstepLibrary.ContainsKey(material)) return;

            var list = footstepLibrary[material];
            Random rnd = new Random();
            string file = list[rnd.Next(list.Count)];

            // Слой 1: Громкий, нормальный питч
            PlaySound(file, pos, 0.8f, 1.0f, 1.0f);

            // Слой 2: Чуть ниже питч для "баса" разрушения
            PlaySound(file, pos, 0.6f, 0.7f, 1.0f);

            // Слой 3: Звук "чпок" (универсальный block place, но быстро), добавляет щелчок
            PlaySound(blockPlaceSound, pos, 1.5f, 0.5f, 1.0f);
        }


        // Universal place sound with pitch variations based on block "hardness/feel"
        public void PlayPlaceSound(BlockType type, Vector3 pos)
        {
            float pitch = 1.0f;
            switch (type)
            {
                case BlockType.STONE:
                case BlockType.COBBLESTONE: pitch = 0.9f; break;
                case BlockType.PLANKS:
                case BlockType.LOG: pitch = 0.8f; break;
                case BlockType.GLASS: pitch = 1.4f; break;
                case BlockType.GRASS: pitch = 1.1f; break;
                case BlockType.SAND: pitch = 1.2f; break;
            }

            // Random variation
            pitch += (float)(new Random().NextDouble() * 0.1f) - 0.05f;

            PlaySound(blockPlaceSound, pos, pitch, 0.8f, 1.0f);
        }

        public void PlayWaterAmbience(Vector3 playerPos)
        {
            // Pick a random water step sound, play it very slowly and quietly
            if (!footstepLibrary.ContainsKey("water")) return;
            var list = footstepLibrary["water"];
            string file = list[new Random().Next(list.Count)];

            // Position it randomly around player to create "surround" feel
            Random rnd = new Random();
            float rx = (float)rnd.NextDouble() * 10 - 5;
            float rz = (float)rnd.NextDouble() * 10 - 5;
            Vector3 offset = new Vector3(rx, 0, rz);

            PlaySound(file, playerPos + offset, 0.5f, 0.3f, 1.0f);
        }

        // Core Play Method
        private void PlaySound(string file, Vector3 pos, float pitch, float gain, float refDist)
        {
            int buffer = GetBuffer(file);
            if (buffer == 0) return;

            int source = GetFreeSource();

            AL.Source(source, ALSourcei.Buffer, buffer);
            AL.Source(source, ALSource3f.Position, pos.X, pos.Y, pos.Z);
            AL.Source(source, ALSourcef.Pitch, pitch);
            AL.Source(source, ALSourcef.Gain, gain);

            // Attenuation
            AL.Source(source, ALSourcef.ReferenceDistance, 2.0f);
            AL.Source(source, ALSourcef.MaxDistance, 25.0f);
            AL.Source(source, ALSourcef.RolloffFactor, 1.0f);

            AL.SourcePlay(source);
        }

        // ИСПРАВЛЕНИЕ: internal вместо public
        internal void CheckOcclusion(int sourceId, WorldClass world, Vector3 listenerPos)
        {
            // Get Source Pos... Raycast to Listener... If hit solid -> Reduce Gain/Pitch
            // Requires Source tracking, skipping for optimization
        }

        // ИСПРАВЛЕНИЕ: internal вместо public
        internal void UpdateAmbient(float dt, WorldClass world, Vector3 playerPos)
        {
            // Water Breeze Logic
            ambientWaterTimer += dt;
            if (ambientWaterTimer > 2.0f) // Check every 2 seconds
            {
                ambientWaterTimer = 0;
                // Check if water is nearby (simple search)
                bool nearWater = false;
                for (int x = -4; x <= 4; x += 2)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        for (int z = -4; z <= 4; z += 2)
                        {
                            if (world.IsWater(playerPos.X + x, playerPos.Y + y, playerPos.Z + z))
                            {
                                nearWater = true;
                                break;
                            }
                        }
                    }
                }

                if (nearWater && new Random().NextDouble() > 0.4) // 60% chance if near water
                {
                    PlayWaterAmbience(playerPos);
                }
            }
        }

        public void Dispose()
        {
            foreach (var s in sources) AL.DeleteSource(s);
            foreach (var b in bufferCache.Values) AL.DeleteBuffer(b);

            if (context != ALContext.Null)
            {
                ALC.MakeContextCurrent(ALContext.Null);
                ALC.DestroyContext(context);
            }
            if (device != ALDevice.Null)
            {
                ALC.CloseDevice(device);
            }
        }
    }
}