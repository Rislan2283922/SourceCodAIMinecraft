using EarthBound.World.Blocks;
using EarthBound.Graphics;
using EarthBound.UI;
using EarthBound.World;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.IO;
using StbImageSharp; // Для загрузки картинки
using EarthBound.Audio; // <-- NEW

namespace EarthBound
{
    public enum GameState
    {
        MainMenu,
        WorldSelect,
        CreateWorld,
        Settings,
        Loading,
        Playing,
        Paused,
        InventoryOpen,
        Dead,
        LicenseMenu, // <-- NEW

    }

    internal class Game : GameWindow
    {
        private const string GAME_VERSION = "Earthbound Indev 0.90";
        ShaderProgram shader;
        Camera camera;
        WorldClass world;
        UIRenderer uiRenderer;
        TextRenderer textRenderer;
        SkyRenderer skyRenderer;
        WorldTime worldTime;
        // --- AUDIO ---
        AudioSystem audio;
        float nextStepDistance = 0.0f;
        float hitSoundTimer = 0.0f; // Таймер для ритмичных звуков "тук-тук" при ломании
        // --- TICK SYSTEM & DEBUG ---
        private const double TIME_PER_TICK = 1.0 / 20.0; // 20 тиков в секунду (как в Minecraft)
        private double accumulatedTime = 0.0;
        private long totalTicks = 0;
        private int tps = 20;
        private int tickCounter = 0;
        private double oneSecondTimer = 0.0;

        private bool showDebugInfo = false; // Переключатель F3
        // --- LICENSE MENU ---
        struct LicenseFile { public string Name; public string Path; public string Content; }
        List<LicenseFile> licenseFiles = new List<LicenseFile>();
        float licenseScroll = 0;
        Texture texBtnDocument;
        public GameState CurrentState = GameState.MainMenu;
        bool isGameLoaded = false;

        List<WorldMetadata> savedWorlds = new List<WorldMetadata>();
        int selectedWorldIndex = -1;
        string inputWorldName = "New World";
        string inputSeed = "";
        bool isTypingName = false;
        bool isTypingSeed = false;

        double lastClickTime = 0;
        string currentWorldFolder = "";
        int currentSeed = 0;
        // --- BURNING LOGIC ---
        bool isBurning = false;
        float burnTimer = 0.0f;

        int fps = 0;
        int frames = 0;
        double frameTime = 0;
        // To remember where to return after clicking "Back" in Settings
        private GameState previousState = GameState.MainMenu;

        Dictionary<string, Texture> worldIcons = new Dictionary<string, Texture>();

        InventorySystem inventory;
        int selectedHotbarSlot = 0;
        bool isBreaking = false;
        float breakingTimer = 0.0f;
        Vector3i breakingTarget;

        // --- ДЛЯ РУКИ ---
        ItemEntity handItemEntity;
        float handSwing = 0.0f;
        float playerHealth = 20.0f;
        // --- ДЛЯ ТРЕЩИН ---
        VAO crackVAO;
        IBO crackIBO;
        bool crackInit = false;
        VBO crackVBO, crackUV, crackColor, crackLight; // Добавили crackLight
        // --- ДЛЯ ОБВОДКИ (OUTLINE) ---
        VAO outlineVAO;
        VBO outlineVBO;
        bool outlineInit = false;
        // --- TEXTURE ARRAY ---
        TextureArray globalTextureArray;


        public Game(int width, int height)
    : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = new Vector2i(width, height),
        Title = "Earthbound",
        NumberOfSamples = 0,

        // ВОТ ЭТА СТРОЧКА РЕШАЕТ ПРОБЛЕМУ:
        Icon = LoadWindowIcon("earthbound.ico")
    })

        {
            VSync = VSyncMode.On;
            CenterWindow(new Vector2i(width, height));
            SaveManager.Init();
        }
        private static WindowIcon LoadWindowIcon(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[ICON ERROR] Файл не найден: {path}");
                return null;
            }

            try
            {
                // Используем System.Drawing.Bitmap (он умеет читать .ico)
                using (var bitmap = new System.Drawing.Bitmap(path))
                {
                    // Блокируем биты изображения для чтения
                    var data = bitmap.LockBits(
                        new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    int byteCount = data.Stride * data.Height;
                    byte[] bytes = new byte[byteCount];

                    // Копируем сырые байты из Bitmap в массив
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, byteCount);

                    bitmap.UnlockBits(data);

                    // ВАЖНО: System.Drawing грузит как BGRA, а OpenTK нужно RGBA.
                    // Меняем местами Синий (B) и Красный (R) каналы.
                    for (int i = 0; i < bytes.Length; i += 4)
                    {
                        byte b = bytes[i];
                        byte r = bytes[i + 2];

                        bytes[i] = r;     // R
                        bytes[i + 2] = b; // B
                    }

                    // Создаем картинку для OpenTK
                    var iconImage = new OpenTK.Windowing.Common.Input.Image(bitmap.Width, bitmap.Height, bytes);
                    return new WindowIcon(iconImage);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ICON ERROR] Ошибка загрузки иконки: {e.Message}");
                return null;
            }
        }
        protected override void OnFocusedChanged(FocusedChangedEventArgs e)
        {
            base.OnFocusedChanged(e);
            // Если потеряли фокус и мы в игре - ставим паузу
            if (!e.IsFocused && CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
                CursorState = CursorState.Normal;
            }
        }

        private void RenderItemInfoPanel(ItemStack stack)
        {
            float x = 20;
            float y = 20;
            float w = 400;
            float h = 120;
            uiRenderer.DrawRect(x, y, w, h, new Vector3(0.1f, 0.1f, 0.1f), 0.8f);

            float iconSize = 60;
            // Pass globalTextureArray
            uiRenderer.RenderIcon3D(x + 15, y + 25, iconSize, stack.Type, globalTextureArray);

            string name = EarthBound.World.Blocks.BlocksManager.GetBlock(stack.Type).DisplayName;
            textRenderer.UpdateText(name);
            textRenderer.Render(x + 90, y + 20, 0.5f, uiRenderer.GetProjection());

            string desc = BlocksManager.GetBlock(stack.Type).Description;
            string[] lines = desc.Split('\n');
            float textY = y + 50;
            foreach (var line in lines)
            {
                textRenderer.UpdateText(line);
                textRenderer.Render(x + 90, textY, 0.35f, uiRenderer.GetProjection());
                textY += 20;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (world != null && (CurrentState == GameState.Playing || CurrentState == GameState.Paused || CurrentState == GameState.Dead))
            {
                PerformSaveAndExit();
            }
            if (audio != null) audio.Dispose();
            // Delete Texture Array resources if needed (optional)
            base.OnClosing(e);
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(TriangleFace.Back);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ClearColor(0.5f, 0.7f, 1f, 1f);

            shader = new ShaderProgram("Default.vert", "Default.frag");

            uiRenderer = new UIRenderer(Size.X, Size.Y);
            textRenderer = new TextRenderer(Size.X, Size.Y);
            // Init Audio
            audio = new AudioSystem();

            // Load License Button Icon (Document)
            texBtnDocument = new Texture("UI/Buttons/document.png");
            camera = new Camera(Size.X, Size.Y, new Vector3(0, 100, 0));
            inventory = new InventorySystem();

            

            // Initialize Texture Array and Layers
            globalTextureArray = new TextureArray();
            TextureData.InitLayers(globalTextureArray);
            // --- NEW: INITIALIZE BLOCKS MANAGER ---
            EarthBound.World.Blocks.BlocksManager.Initialize();
            handItemEntity = new ItemEntity(Vector3.Zero, BlockType.DIRT);
            // Инициализация трещин
            crackVAO = new VAO();
            crackVAO.Bind();

            crackVBO = new VBO(new List<Vector3>());
            crackUV = new VBO(new List<Vector3>()); // Used to be Vec2, now Vec3
            crackColor = new VBO(new List<Vector3>());

            // --- FIX: Initialize crackLight HERE ---
            crackLight = new VBO(new List<Vector2>()); // Init empty
            // ---------------------------------------

            crackIBO = new IBO(new List<uint> {
                0, 1, 2, 2, 3, 0,
                4, 5, 6, 6, 7, 4,
                8, 9, 10, 10, 11, 8,
                12, 13, 14, 14, 15, 12,
                16, 17, 18, 18, 19, 16,
                20, 21, 22, 22, 23, 20
            });

            crackVAO.LinkToVAO(0, 3, crackVBO);
            crackVAO.LinkToVAO(1, 3, crackUV); // Make sure this matches 3 floats
            crackVAO.LinkToVAO(2, 3, crackColor);

            // --- FIX: Link Light Attribute ---
            crackVAO.LinkToVAO(3, 2, crackLight);
            // ---------------------------------

            crackVAO.Unbind();
            crackInit = true;


            // Инициализация обводки (Outline)
            outlineVAO = new VAO();
            outlineVAO.Bind();
            outlineVBO = new VBO(new List<Vector3>());
            outlineVAO.LinkToVAO(0, 3, outlineVBO);
            outlineVAO.LinkToVAO(1, 2, new VBO(new List<Vector2>()));
            outlineVAO.LinkToVAO(2, 3, new VBO(new List<Vector3>()));
            outlineVAO.Unbind();
            outlineInit = true;

            // --- ADDED LIGHTER & FIRE ---
            // Just ensuring we start clean
            isBurning = false;
            burnTimer = 0;

            CursorState = CursorState.Normal;
            savedWorlds = SaveManager.GetWorlds();
            skyRenderer = new SkyRenderer();
            worldTime = new WorldTime();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            // --- TICK SYSTEM LOGIC ---
            accumulatedTime += args.Time;

            // Выполняем фиксированные тики (физика, логика мира)
            while (accumulatedTime >= TIME_PER_TICK)
            {
                GameTick();
                accumulatedTime -= TIME_PER_TICK;
            }

            // Подсчет FPS и TPS (обновляем раз в секунду)
            oneSecondTimer += args.Time;
            frames++;
            if (oneSecondTimer >= 1.0)
            {
                fps = frames;
                tps = tickCounter;
                frames = 0;
                tickCounter = 0;
                oneSecondTimer = 0;
            }

            // Переключение F3
            if (KeyboardState.IsKeyPressed(Keys.F3))
            {
                showDebugInfo = !showDebugInfo;
            }
            // --- КНОПКА СПАСЕНИЯ (F4) ---
            if (KeyboardState.IsKeyPressed(Keys.F4))
            {
                // 1. Тепаем высоко
                camera.position = new Vector3(camera.position.X, 120, camera.position.Z);
                // 2. Сбрасываем скорость, чтобы не лететь вверх/вниз по инерции
                camera.velocity = Vector3.Zero;
                // 3. Сбрасываем логику падения
                camera.ResetFallState();
                // 4. ВАЖНО: Говорим камере, что "пиковая высота" сейчас здесь, в небе.
                // Иначе при падении с 120 до земли она насчитает урон.
                // А ResetFallState дает 1 сек неуязвимости (safetyTimer), так что успеем упасть.
                isBurning = false;
                burnTimer = 0;
            }


            float dt = (float)args.Time;

            if (handSwing > 0)
            {
                handSwing -= dt * 5.0f;
                if (handSwing < 0) handSwing = 0;
            }

            // Inventory logic
            if (CurrentState == GameState.InventoryOpen)
            {
                HandleInventoryInput();
            }

            switch (CurrentState)
            {
                case GameState.Loading:
                    UpdateLoading();
                    break;

                case GameState.Playing:
                    if (IsFocused && CursorState == CursorState.Grabbed)
                    {
                        // Check Fall Damage from Camera
                        // 1. Урон от падения
                        if (camera.PendingDamage > 0)
                        {
                            playerHealth -= camera.PendingDamage;
                            camera.PendingDamage = 0;
                            camera.TriggerDamageEffect();

                            if (playerHealth <= 0)
                            {
                                playerHealth = 0;
                                CurrentState = GameState.Dead;
                                CursorState = CursorState.Normal;
                            }
                        }

                        // 2. Логика Огня и Лавы
                        Vector3 pPos = camera.position;
                        BlockType feetBlock = world.GetBlock(pPos);
                        BlockType legBlock = world.GetBlock(new Vector3(pPos.X, pPos.Y - 1, pPos.Z));

                        if (feetBlock == BlockType.LAVA || legBlock == BlockType.LAVA)
                        {
                            isBurning = true;
                            burnTimer = 10.0f;
                            if (hitSoundTimer <= 0)
                            {
                                playerHealth -= 4.0f;
                                camera.TriggerDamageEffect();
                                hitSoundTimer = 0.5f;
                            }
                        }
                        else if (feetBlock == BlockType.FIRE || legBlock == BlockType.FIRE)
                        {
                            isBurning = true;
                            burnTimer = 5.0f;
                        }

                        if (isBurning)
                        {
                            burnTimer -= dt;
                            if (burnTimer <= 0) isBurning = false;

                            // Урон от горения раз в ~20 кадров (или привяжем к тикам позже)
                            if (totalTicks % 20 == 0 && tickCounter == 0) // Раз в секунду примерно
                            {
                                playerHealth -= 1.0f;
                                camera.TriggerDamageEffect();
                            }

                            if (feetBlock == BlockType.WATER || legBlock == BlockType.WATER)
                            {
                                isBurning = false;
                                burnTimer = 0;
                                audio.PlayFootstep("water", pPos);
                            }
                        }

                        // 3. Обновление камеры и аудио (Камера должна быть плавной, оставляем в Update)
                        camera.Update(KeyboardState, MouseState, args, world, false);
                        audio.UpdateListener(camera.position, camera.Front, Vector3.UnitY);
                        audio.UpdateAmbient((float)args.Time, world, camera.position);

                        // !!! ВАЖНО: Удалены TickFire, TickLiquids, UpdateEntities отсюда.
                        // Они теперь вызываются внутри GameTick() !!!

                        // 4. ЗВУК ПРИЗЕМЛЕНИЯ
                        if (camera.JustLanded)
                        {
                            camera.JustLanded = false;
                            Vector3 below = camera.position - Vector3.UnitY * 1.6f;
                            BlockType bLand = world.GetBlock(below);
                            if (bLand != BlockType.AIR)
                            {
                                string mat = EarthBound.World.Blocks.BlocksManager.GetBlock(bLand).SoundCategory;
                                audio.PlayFootstep(mat, camera.position, 1.0f, 1.2f);
                            }
                        }

                        // 5. ЗВУКИ ШАГОВ
                        if (camera.WalkDistance > nextStepDistance)
                        {
                            nextStepDistance = camera.WalkDistance + 2.5f;
                            bool feetInWater = world.IsWater(camera.position.X, camera.position.Y - 0.5f, camera.position.Z);

                            if (feetInWater)
                            {
                                audio.PlayFootstep("water", camera.position, 0.8f);
                            }
                            else
                            {
                                Vector3 underFeet = camera.position - new Vector3(0, 0.1f, 0);
                                BlockType bType = world.GetBlock(underFeet);

                                if (bType == BlockType.AIR || bType == BlockType.WATER || bType == BlockType.FIRE)
                                {
                                    Vector3 below = camera.position - new Vector3(0, 1.1f, 0);
                                    bType = world.GetBlock(below);
                                }

                                if (bType != BlockType.AIR && bType != BlockType.WATER)
                                {
                                    string mat = EarthBound.World.Blocks.BlocksManager.GetBlock(bType).SoundCategory;
                                    audio.PlayFootstep(mat, camera.position, 0.5f);
                                }
                            }
                        }

                        // Чанки обновляем каждый кадр для плавности загрузки
                        world.UpdateChunksAroundPlayer(camera.position, Settings.RenderDistance);
                    }

                    // Время обновляем плавно для неба
                    worldTime.Update(dt);

                    // Тут строка была которую я удаллииил

                    if (MouseState.ScrollDelta.Y > 0) selectedHotbarSlot--;
                    if (MouseState.ScrollDelta.Y < 0) selectedHotbarSlot++;
                    if (selectedHotbarSlot < 0) selectedHotbarSlot = 8;
                    if (selectedHotbarSlot > 8) selectedHotbarSlot = 0;

                    for (int i = 0; i < 9; i++) if (KeyboardState.IsKeyPressed(Keys.D1 + i)) selectedHotbarSlot = i;

                    if (KeyboardState.IsKeyPressed(Keys.G))
                    {
                        ItemStack current = inventory.GetStack(selectedHotbarSlot);
                        if (current != null)
                        {
                            inventory.ConsumeItem(selectedHotbarSlot, 1);
                            world.SpawnItem(camera.position + camera.GetViewMatrix().Inverted().Row2.Xyz * -1.0f, current.Type);
                        }
                    }

                    if (KeyboardState.IsKeyPressed(Keys.E))
                    {
                        CurrentState = GameState.InventoryOpen;
                        CursorState = CursorState.Normal;
                    }

                    if (KeyboardState.IsKeyPressed(Keys.Escape))
                    {
                        CurrentState = GameState.Paused;
                        CursorState = CursorState.Normal;
                    }

                    RaycastResult hit = Physics.Raycast(camera.GetEyePosition(), camera.Front, 5.0f, world);

                    if (MouseState.IsButtonDown(MouseButton.Left))
                    {
                        if (handSwing == 0) handSwing = 1.0f;

                        // Логика зажигалки
                        ItemStack held = inventory.GetStack(selectedHotbarSlot);
                        if (MouseState.IsButtonPressed(MouseButton.Left))
                        {
                            if (held != null)
                            {
                                if (held.Type == BlockType.LIGHTER) { held.Type = BlockType.LIGHTER_ON; audio.PlayPlaceSound(BlockType.IRON_ORE, camera.position); return; }
                                else if (held.Type == BlockType.LIGHTER_ON) { held.Type = BlockType.LIGHTER; audio.PlayPlaceSound(BlockType.IRON_ORE, camera.position); return; }
                            }
                        }

                        if (hit.Hit)
                        {
                            BlockType targetType = world.GetBlock(new Vector3(hit.BlockPos.X, hit.BlockPos.Y, hit.BlockPos.Z));

                            // Тушим огонь сразу
                            if (targetType == BlockType.FIRE)
                            {
                                world.SetBlock(hit.BlockPos.X, hit.BlockPos.Y, hit.BlockPos.Z, BlockType.AIR);
                                audio.PlayFootstep("grass", camera.position);
                                isBreaking = false;
                            }
                            else
                            {
                                // Обычное ломание
                                if (!isBreaking || breakingTarget != hit.BlockPos)
                                {
                                    isBreaking = true;
                                    breakingTarget = hit.BlockPos;
                                    breakingTimer = 0;
                                    hitSoundTimer = 0;
                                }

                                // ИСПОЛЬЗУЕМ НОВЫЙ МЕНЕДЖЕР
                                float hardness = EarthBound.World.Blocks.BlocksManager.GetBlock(targetType).Hardness;
                                ItemStack heldItem = inventory.GetStack(selectedHotbarSlot);
                                BlockType toolType = (heldItem != null) ? heldItem.Type : BlockType.AIR;

                                // Используем локальный метод (мы его добавили скриптом)
                                float speedMultiplier = GetMiningSpeed(toolType, targetType);

                                breakingTimer += dt * speedMultiplier;
                                hitSoundTimer -= dt;
                                if (hitSoundTimer <= 0)
                                {
                                    string mat = EarthBound.World.Blocks.BlocksManager.GetBlock(targetType).SoundCategory;
                                    audio.PlayHitSound(mat, camera.position);
                                    hitSoundTimer = 0.25f;
                                }

                                if (breakingTimer >= hardness)
                                {
                                    // --- ЛОГИКА МУЛЬТИ-БЛОКОВ (ЕЛКА) ---
                                    if (targetType == BlockType.CHRISTMAS_TREE)
                                    {
                                        byte data = world.GetBlockData(hit.BlockPos);
                                        // Если ломаем низ (Data=0), удаляем и верх
                                        if (data == 0)
                                        {
                                            world.SetBlock(hit.BlockPos.X, hit.BlockPos.Y + 1, hit.BlockPos.Z, BlockType.AIR);
                                        }
                                        // Если ломаем верх (Data=1), удаляем и низ
                                        else if (data == 1)
                                        {
                                            world.SetBlock(hit.BlockPos.X, hit.BlockPos.Y - 1, hit.BlockPos.Z, BlockType.AIR);
                                        }
                                    }
                                    // -----------------------------------

                                    world.SetBlock(hit.BlockPos.X, hit.BlockPos.Y, hit.BlockPos.Z, BlockType.AIR);

                                    // Спавним предмет (только если это не верхняя часть елки, чтобы не дюпать)
                                    // Но так как мы ломаем тот блок, на который смотрим, спавним из него.
                                    // Единственный нюанс: если сломали верх, выпадет елка. Если низ — тоже. 
                                    world.SpawnItem(new Vector3(hit.BlockPos.X + 0.5f, hit.BlockPos.Y + 0.5f, hit.BlockPos.Z + 0.5f), targetType);

                                    string mat = EarthBound.World.Blocks.BlocksManager.GetBlock(targetType).SoundCategory;
                                    audio.PlayBreakSound(mat, new Vector3(hit.BlockPos.X, hit.BlockPos.Y, hit.BlockPos.Z));

                                    isBreaking = false;
                                    breakingTimer = 0;
                                }
                            }
                        }
                        else { isBreaking = false; breakingTimer = 0; }
                    }
                    else { isBreaking = false; breakingTimer = 0; }


                    if (MouseState.IsButtonPressed(MouseButton.Right))
                    {
                        if (handSwing == 0) handSwing = 1.0f;
                        if (hit.Hit)
                        {
                            ItemStack current = inventory.GetStack(selectedHotbarSlot);
                            if (current != null)
                            {
                                // --- IGNITE LOGIC ---
                                if (current.Type == BlockType.LIGHTER_ON || current.Type == BlockType.LIGHTER)
                                {
                                    if (current.Type == BlockType.LIGHTER) { current.Type = BlockType.LIGHTER_ON; return; }

                                    BlockType hitBlock = world.GetBlock(new Vector3(hit.BlockPos.X, hit.BlockPos.Y, hit.BlockPos.Z));
                                    if (hitBlock == BlockType.TNT)
                                    {
                                        world.Explode(hit.BlockPos);
                                        audio.PlayBreakSound("wood", camera.position);
                                    }
                                    else
                                    {
                                        Vector3i placePos = hit.BlockPos + hit.FaceNormal;
                                        if (world.GetBlock(new Vector3(placePos.X, placePos.Y, placePos.Z)) == BlockType.AIR)
                                        {
                                            // ... (старая логика огня без изменений) ...
                                            world.SetBlock(placePos.X, placePos.Y, placePos.Z, BlockType.FIRE);
                                            audio.PlayPlaceSound(BlockType.GRASS, new Vector3(placePos.X, placePos.Y, placePos.Z));
                                        }
                                    }
                                }
                                // --- SNOW LAYER STACKING LOGIC ---
                                else if (current.Type == BlockType.SNOW_LAYER)
                                {
                                    BlockType target = world.GetBlock(new Vector3(hit.BlockPos.X, hit.BlockPos.Y, hit.BlockPos.Z));
                                    byte data = world.GetBlockData(hit.BlockPos);

                                    // Если кликнули по слою снега тем же снегом -> увеличиваем слой
                                    if (target == BlockType.SNOW_LAYER && data < 7 && hit.FaceNormal.Y == 1)
                                    {
                                        world.SetBlock(hit.BlockPos.X, hit.BlockPos.Y, hit.BlockPos.Z, BlockType.SNOW_LAYER, (byte)(data + 1));
                                        audio.PlayPlaceSound(BlockType.SNOW, new Vector3(hit.BlockPos.X, hit.BlockPos.Y, hit.BlockPos.Z));
                                        inventory.ConsumeItem(selectedHotbarSlot, 1);
                                    }
                                    else
                                    {
                                        // Обычная установка
                                        Vector3i placePos = hit.BlockPos + hit.FaceNormal;
                                        // Проверяем, не пытаемся ли мы заменить слой снега на тот же слой (если кликнули сбоку)
                                        BlockType placeTarget = world.GetBlock(new Vector3(placePos.X, placePos.Y, placePos.Z));
                                        if (placeTarget == BlockType.SNOW_LAYER)
                                        {
                                            byte placeData = world.GetBlockData(placePos);
                                            if (placeData < 7)
                                            {
                                                world.SetBlock(placePos.X, placePos.Y, placePos.Z, BlockType.SNOW_LAYER, (byte)(placeData + 1));
                                                inventory.ConsumeItem(selectedHotbarSlot, 1);
                                            }
                                        }
                                        else if (!EarthBound.World.Blocks.BlocksManager.GetBlock(current.Type).IsItem)

                                        {
                                            world.SetBlock(placePos.X, placePos.Y, placePos.Z, current.Type);
                                            audio.PlayPlaceSound(current.Type, new Vector3(placePos.X, placePos.Y, placePos.Z));
                                            inventory.ConsumeItem(selectedHotbarSlot, 1);
                                        }
                                    }
                                }
                                // --- BLOCK PLACEMENT ---
                                else if (!BlocksManager.GetBlock(current.Type).IsItem)
                                {
                                    Vector3i placePos = hit.BlockPos + hit.FaceNormal;
                                    Vector3 pPos = new Vector3(placePos.X + 0.5f, placePos.Y + 0.5f, placePos.Z + 0.5f);

                                    // Проверка дистанции до игрока (чтобы не застрять в блоке)
                                    if (Vector3.Distance(camera.position + new Vector3(0, 0.5f, 0), pPos) > 0.8f)
                                    {
                                        // --- ЛОГИКА ЕЛКИ (ВЫСОТА 2 БЛОКА) ---
                                        if (current.Type == BlockType.CHRISTMAS_TREE)
                                        {
                                            Vector3i topPos = placePos + new Vector3i(0, 1, 0);
                                            BlockType above = world.GetBlock(new Vector3(topPos.X, topPos.Y, topPos.Z));

                                            // Проверяем, свободен ли блок сверху
                                            bool canPlaceTop = (above == BlockType.AIR || above == BlockType.WATER ||
                                                                !BlocksManager.GetBlock(above).IsSolid);

                                            // Также проверяем, не стоит ли игрок в верхнем блоке
                                            Vector3 pPosTop = new Vector3(topPos.X + 0.5f, topPos.Y + 0.5f, topPos.Z + 0.5f);
                                            bool playerClearOfTop = Vector3.Distance(camera.position + new Vector3(0, 0.5f, 0), pPosTop) > 0.8f;

                                            if (canPlaceTop && playerClearOfTop)
                                            {
                                                // Ставим НИЗ (Data = 0)
                                                world.SetBlock(placePos.X, placePos.Y, placePos.Z, current.Type, 0);
                                                // Ставим ВЕРХ (Data = 1)
                                                world.SetBlock(topPos.X, topPos.Y, topPos.Z, current.Type, 1);

                                                audio.PlayPlaceSound(current.Type, new Vector3(placePos.X, placePos.Y, placePos.Z));
                                                inventory.ConsumeItem(selectedHotbarSlot, 1);
                                            }
                                        }
                                        else
                                        {
                                            // ОБЫЧНАЯ УСТАНОВКА (1 БЛОК)
                                            world.SetBlock(placePos.X, placePos.Y, placePos.Z, current.Type);
                                            audio.PlayPlaceSound(current.Type, new Vector3(placePos.X, placePos.Y, placePos.Z));
                                            inventory.ConsumeItem(selectedHotbarSlot, 1);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;

                case GameState.Dead:
                    // Camera updates in Dead mode (Orbiting)
                    camera.Update(KeyboardState, MouseState, args, world, true);
                    break;

                case GameState.InventoryOpen:
                    if (KeyboardState.IsKeyPressed(Keys.E) || KeyboardState.IsKeyPressed(Keys.Escape))
                    {
                        if (inventory.DragStack != null)
                        {
                            inventory.AddItem(inventory.DragStack.Type, inventory.DragStack.Count);
                            inventory.DragStack = null;
                        }
                        CurrentState = GameState.Playing;
                        CursorState = CursorState.Grabbed;
                    }
                    // Render dragged item on top
                    if (inventory.DragStack != null)
                    {
                        uiRenderer.RenderDraggedItem(inventory.DragStack, MouseState.Position.X, MouseState.Position.Y, textRenderer, globalTextureArray);
                    }
                    break;


                case GameState.Paused:
                    if (KeyboardState.IsKeyPressed(Keys.Escape))
                    {
                        CurrentState = GameState.Playing;
                        CursorState = CursorState.Grabbed;
                    }
                    break;
            }
        }

        private void HandleInventoryInput()
        {
            float scale = 3.0f;
            float panelW = 176 * scale;
            float panelH = 166 * scale;
            float panelX = (Size.X - panelW) / 2;
            float panelY = (Size.Y - panelH) / 2;
            float cellSize = 18 * scale;
            float gridStartX = panelX + (7 * scale);
            float gridStartY = panelY + (17 * scale);
            float hbY = panelY + (142 * scale);

            int hoveredSlot = -1;
            Vector2 m = MouseState.Position;

            // Проверка Хотбара (внизу панели)
            for (int i = 0; i < InventorySystem.HOTBAR_SIZE; i++)
            {
                float ix = gridStartX + (i * cellSize);
                float iy = hbY;
                if (m.X >= ix && m.X < ix + cellSize && m.Y >= iy && m.Y < iy + cellSize) hoveredSlot = i;
            }

            // Проверка Инвентаря (3 ряда по 9)
            for (int i = InventorySystem.HOTBAR_SIZE; i < InventorySystem.TOTAL_SIZE; i++)
            {
                int gridIndex = i - InventorySystem.HOTBAR_SIZE;
                int row = gridIndex / 9;
                int col = gridIndex % 9;
                float ix = gridStartX + (col * cellSize);
                float iy = gridStartY + (row * cellSize);
                if (m.X >= ix && m.X < ix + cellSize && m.Y >= iy && m.Y < iy + cellSize) hoveredSlot = i;
            }

            // --- ЛОГИКА DRAG & DROP ---

            // Если нажали ЛКМ и ничего не тащим -> Берем предмет
            if (MouseState.IsButtonDown(MouseButton.Left) && inventory.DragStack == null)
            {
                if (hoveredSlot != -1)
                {
                    ItemStack clicked = inventory.GetStack(hoveredSlot);
                    if (clicked != null)
                    {
                        inventory.DragStack = clicked;
                        inventory.SetStack(hoveredSlot, null);
                    }
                }
            }

            // Если отпустили ЛКМ и тащим предмет -> Кладем предмет
            if (!MouseState.IsButtonDown(MouseButton.Left) && inventory.DragStack != null)
            {
                if (hoveredSlot != -1)
                {
                    ItemStack target = inventory.GetStack(hoveredSlot);

                    if (target == null)
                    {
                        inventory.SetStack(hoveredSlot, inventory.DragStack);
                        inventory.DragStack = null;
                    }
                    else if (target.Type == inventory.DragStack.Type)
                    {
                        int space = ItemStack.MAX_STACK - target.Count;
                        int toAdd = Math.Min(space, inventory.DragStack.Count);
                        target.Count += toAdd;
                        inventory.DragStack.Count -= toAdd;
                        if (inventory.DragStack.Count <= 0) inventory.DragStack = null;
                    }
                    else
                    {
                        ItemStack temp = target;
                        inventory.SetStack(hoveredSlot, inventory.DragStack);
                        inventory.DragStack = temp;
                    }
                }
            }
        }

        private void UpdateLoading()
        {
            if (world.GenerateSpawnArea(6))
            {
                PlayerData loadedData = SaveManager.LoadPlayer(currentWorldFolder, "Player");

                if (loadedData != null && loadedData.Y > -50)
                {
                    // Pass burning state
                    loadedData.ApplyToGame(camera, inventory, out isBurning, out burnTimer);
                    playerHealth = loadedData.Health;

                    camera.ResetFallState();

                    if (playerHealth <= 0)
                    {
                        playerHealth = 0;
                        camera.SetDeathState(camera.position);
                        CurrentState = GameState.Dead;
                        CursorState = CursorState.Normal;
                        return;
                    }
                }
                else
                {
                    int sX = 0;
                    int sZ = 0;
                    for (int y = 120; y > 0; y--)
                    {
                        BlockType b = world.GetBlock(new Vector3(sX, y, sZ));
                        if (b != BlockType.AIR && b != BlockType.WATER && b != BlockType.LEAVES && b != BlockType.LOG)
                        {
                            BlockType up1 = world.GetBlock(new Vector3(sX, y + 1, sZ));
                            BlockType up2 = world.GetBlock(new Vector3(sX, y + 2, sZ));

                            if (up1 != BlockType.LOG && up2 != BlockType.LOG)
                            {
                                camera.position = new Vector3(sX + 0.5f, y + 2, sZ + 0.5f);
                                break;
                            }
                        }
                    }

                    camera.ResetFallState();
                    playerHealth = 20.0f;
                    isBurning = false;
                    burnTimer = 0;

                    // Save initial
                    PlayerData newData = new PlayerData();
                    newData.SetFromGame(camera, inventory, false, 0);
                    newData.Health = playerHealth;
                    SaveManager.SavePlayer(currentWorldFolder, "Player", newData);
                }


                world.BuildAllMeshes();
                isGameLoaded = true;
                CurrentState = GameState.Playing;
                CursorState = CursorState.Grabbed;

                if (selectedWorldIndex >= 0 && selectedWorldIndex < savedWorlds.Count)
                {
                    SaveManager.UpdateWorldData(currentWorldFolder, savedWorlds[selectedWorldIndex].Name, currentSeed, worldTime.CurrentTime);
                }
            }
        }
       
        private void GameTick()
        {
            tickCounter++;
            totalTicks++;

            if (CurrentState == GameState.Playing && world != null)
            {
                // Обновляем жидкости и огонь с фиксированным шагом времени
                // Передаем TIME_PER_TICK (0.05f), чтобы физика была стабильной
                world.TickFire((float)TIME_PER_TICK);
                world.TickLiquids((float)TIME_PER_TICK);

                // Обновляем сущности (дроп)
                world.UpdateEntities((float)TIME_PER_TICK, camera.position, inventory);
            }
        }

        private void RenderVersionWatermark()
        {
            // 1. ВАЖНО: Включаем режим 2D (отключаем тест глубины), 
            // иначе текст может перекрыться фоном или не отрисоваться
            uiRenderer.Prepare();

            textRenderer.UpdateText(GAME_VERSION);

            float scale = 0.35f;

            // 2. ИСПРАВЛЕННЫЙ РАСЧЕТ ШИРИНЫ
            // Шрифт жирный, берем 22 пикселя на символ (с запасом), умножаем на масштаб
            float charWidth = 22.0f * scale;
            float totalWidth = GAME_VERSION.Length * charWidth;

            // X: Ширина экрана - Ширина текста - Отступ
            float x = Size.X - totalWidth - 10;
            // Y: Самый низ экрана - Высота строки (примерно 30 при таком масштабе)
            float y = Size.Y - 25;

            textRenderer.Render(x, y, scale, uiRenderer.GetProjection());
        }
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            switch (CurrentState)
            {
                case GameState.MainMenu:
                    RenderMainMenu();
                    break;
                case GameState.LicenseMenu:
                    RenderLicenseMenu();
                    break;
                case GameState.WorldSelect:
                    RenderWorldSelect();
                    break;
                case GameState.CreateWorld:
                    RenderCreateWorld();
                    break;
                case GameState.Settings:
                    RenderSettings();
                    break;
                case GameState.Loading:
                    RenderLoading();
                    break;
                case GameState.Playing:
                case GameState.InventoryOpen:
                case GameState.Paused:
                case GameState.Dead:
                    RenderGame();
                    if (CurrentState == GameState.Paused) RenderPauseMenu();
                    if (CurrentState == GameState.Dead) RenderDeathScreen();

                    // --- F3 DEBUG INFO ---
                    if (showDebugInfo)
                    {
                        // Формируем текст
                        string debugText = $"FPS: {fps} | TPS: {tps} | Ticks: {totalTicks}\n" +
                                           $"XYZ: {camera.position.X:0.0} / {camera.position.Y:0.0} / {camera.position.Z:0.0}";

                        textRenderer.UpdateText(debugText);

                        float textScale = 0.5f;

                        // Ищем самую длинную строку, чтобы сдвинуть текст влево
                        string[] lines = debugText.Split('\n');
                        int maxLen = 0;
                        foreach (var line in lines) if (line.Length > maxLen) maxLen = line.Length;

                        // ИСПРАВЛЕНИЕ: Берем 20 пикселей на символ (было 14), 
                        // чтобы текст гарантированно влез и не обрезался справа
                        float textWidth = maxLen * (20f * textScale);

                        // Рисуем в правом верхнем углу
                        textRenderer.Render(Size.X - textWidth - 10, 10, textScale, uiRenderer.GetProjection());
                    }
                    break;
            }

            // --- ВЕРСИЯ ИГРЫ (Рисуется в самом конце поверх всего) ---
            RenderVersionWatermark();
            // --------------------------------------------------------

            Context.SwapBuffers();
        }
        private void RenderGame()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            bool isUnderwater = world != null && world.IsWater(camera.position.X, camera.position.Y + 1.62f, camera.position.Z);

            Vector3 finalFogColor = isUnderwater ? new Vector3(0, 0.1f, 0.4f) : worldTime.SkyColor;

            // Red tint if burning
            if (isBurning) finalFogColor = Vector3.Lerp(finalFogColor, new Vector3(0.6f, 0.1f, 0.0f), 0.5f);

            GL.ClearColor(finalFogColor.X, finalFogColor.Y, finalFogColor.Z, 1.0f);

            if (!isUnderwater && world != null)
            {
                skyRenderer.Render(camera, worldTime);
            }

            shader.Bind();
            GL.Uniform1(GL.GetUniformLocation(shader.ID, "time"), (float)TimeSinceStart());
            GL.Uniform3(GL.GetUniformLocation(shader.ID, "fogColor"), finalFogColor);
            GL.Uniform1(GL.GetUniformLocation(shader.ID, "fogDensity"), isUnderwater ? 0.15f : 0.007f);

            Vector3 globalLight = isUnderwater ? worldTime.GlobalLight * 0.6f : worldTime.GlobalLight;
            // Dim light if burning to make fire look brighter
            if (isBurning) globalLight *= 0.8f;
            GL.Uniform3(GL.GetUniformLocation(shader.ID, "globalLight"), globalLight);

            // Add RED overlay intensity if burning
            float flash = camera.DamageFlash;
            if (isBurning) flash = Math.Max(flash, 0.3f + (MathF.Sin((float)TimeSinceStart() * 10) * 0.1f));

            GL.Uniform4(GL.GetUniformLocation(shader.ID, "overlayColor"), new Vector4(1, 0, 0, flash));
            GL.Uniform1(GL.GetUniformLocation(shader.ID, "isCrack"), 0);

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjectionMatrix();

            GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "model"), true, ref model);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "view"), true, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "projection"), true, ref projection);

            if (world != null)
            {
                Matrix4 viewProj = view * projection;

                GL.Disable(EnableCap.Blend);
                // Pass TextureArray to render methods
                world.RenderSolid(shader, viewProj, camera.position, globalTextureArray);

                GL.Enable(EnableCap.Blend);
                world.RenderEntities(shader, globalTextureArray);


                Matrix4 modelReset = Matrix4.Identity;
                GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "model"), true, ref modelReset);

                world.RenderTransparent(shader, viewProj, camera.position, globalTextureArray);

                RaycastResult hit = Physics.Raycast(camera.GetEyePosition(), camera.Front, 5.0f, world);
                if (hit.Hit)
                {
                    RenderSelectionOutline(hit.BlockPos);
                }
            }

            if (isBreaking && crackInit)
            {
                BlockType targetType = world.GetBlock(new Vector3(breakingTarget.X, breakingTarget.Y, breakingTarget.Z));
                float hardness = BlocksManager.GetBlock(targetType).Hardness;
                if (hardness > 0)
                {
                    int stage = (int)((breakingTimer / hardness) * 10.0f);
                    if (stage > 9) stage = 9;
                    if (stage >= 0)
                    {
                        RenderBreakOverlay(breakingTarget, stage);
                    }
                }
            }

            // Рендер руки
            ItemStack held = inventory.GetStack(selectedHotbarSlot);
            if (held != null && CurrentState == GameState.Playing)
            {
                GL.Clear(ClearBufferMask.DepthBufferBit);
                // Угол обзора для руки (60 градусов)
                Matrix4 handProj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60), Size.X / (float)Size.Y, 0.1f, 100f);
                Matrix4 handView = Matrix4.LookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);

                float swingAngle = MathF.Sin(handSwing * MathF.PI) * 0.8f;
                Matrix4 handModel;

                bool isFlat = BlocksManager.GetBlock(held.Type).IsItem ||
                              held.Type == BlockType.FLOWER_RED || held.Type == BlockType.FLOWER_YELLOW ||
                              held.Type == BlockType.MUSHROOM_RED || held.Type == BlockType.MUSHROOM_BROWN ||
                              held.Type == BlockType.DEAD_BUSH || held.Type == BlockType.FIRE;

                if (isFlat)
                {
                    // --- ФИКС ПОЗИЦИИ ---
                    // Scale(0.85f) -> Сделали покрупнее (было 0.6)
                    // Translate(0.5f, -0.4f, -1.0f) -> Подняли выше (Y=-0.4) и подвинули ближе к центру (X=0.5)
                    handModel = Matrix4.CreateScale(0.85f) *
                                Matrix4.CreateRotationY(MathHelper.DegreesToRadians(90)) *
                                Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(50) + swingAngle) *
                                Matrix4.CreateTranslation(0.5f, -0.4f + (swingAngle * 0.2f), -1.0f - (swingAngle * 0.4f));
                }
                else
                {
                    // Блоки
                    handModel = Matrix4.CreateRotationZ(swingAngle) *
                                Matrix4.CreateRotationY(MathHelper.DegreesToRadians(45)) *
                                Matrix4.CreateRotationX(MathHelper.DegreesToRadians(10)) *
                                Matrix4.CreateTranslation(0.5f - (swingAngle * 0.4f), -0.6f + (swingAngle * 0.2f), -1.0f - (swingAngle * 0.4f));
                }

                GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "projection"), true, ref handProj);
                GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "view"), true, ref handView);

                handItemEntity.SetType(held.Type);
                handItemEntity.RenderWithModel(shader, handModel, globalTextureArray);
            }



            uiRenderer.Prepare();
            if (CurrentState == GameState.Playing)
            {
                uiRenderer.RenderCrosshair(Size.X, Size.Y);
                uiRenderer.RenderHearts(playerHealth, 20, Size.X, Size.Y);

                // --- FIRE OVERLAY ---
                if (isBurning)
                {
                    // Retrieve fire layer
                    int layer = 0;
                    if (TextureData.BlockLayerIndices.ContainsKey(BlockType.FIRE))
                        layer = TextureData.BlockLayerIndices[BlockType.FIRE][Faces.FRONT];
                    // Full Quad with vec3 UVs
                    float[] fVerts = {
                    0, Size.Y - 300,  0.0f, 0.0f, layer,
                    Size.X, Size.Y - 300, 1.0f, 0.0f, layer,
                    Size.X, Size.Y,       1.0f, 1.0f, layer,
                    0, Size.Y,            0.0f, 1.0f, layer
                };

                    // Use the Array Render method
                    uiRenderer.RenderQuadCustomVertsArray(fVerts, globalTextureArray, new Vector3(1f, 0.8f, 0.8f), 0.9f);
                }

                if (held != null)
                {
                    RenderItemInfoPanel(held);
                }
            }

            // Pass TextureArray to Inventory
            uiRenderer.RenderInventory(inventory, selectedHotbarSlot, CurrentState == GameState.InventoryOpen, textRenderer, Size.X, Size.Y, globalTextureArray);

            GL.BindVertexArray(0);
        }

        private void RenderSelectionOutline(Vector3i pos)
        {
            BlockType type = world.GetBlock(new Vector3(pos.X, pos.Y, pos.Z));
            byte data = world.GetBlockData(pos);

            // Default size
            float minY = 0.0f;
            float maxY = 1.0f;

            // Adjust box height for Snow Layers
            if (type == BlockType.SNOW_LAYER)
            {
                int layers = (data & 7) + 1;
                maxY = layers * 0.125f;
            }
            else if (type == BlockType.FLOWER_RED || type == BlockType.FLOWER_YELLOW ||
         type == BlockType.MUSHROOM_RED || type == BlockType.MUSHROOM_BROWN ||
         type == BlockType.DEAD_BUSH)
            {
                minY = 0.0f;
                maxY = 0.6f; // Неполный блок
            }
    

            float size = 1.005f;
            float offset = (size - 1.0f) / 2.0f;

            // Draw relative to block center-bottom usually, but here we calculate from corner
            float x = pos.X - 0.5f - offset;
            float y = pos.Y - 0.5f - offset;
            float z = pos.Z - 0.5f - offset;
            float w = size;

            // Adjust Y height based on block shape
            float h = (maxY - minY) + (offset * 2);

            List<Vector3> verts = new List<Vector3>() {
                // Bottom square
                new Vector3(x, y, z), new Vector3(x+w, y, z),
                new Vector3(x+w, y, z), new Vector3(x+w, y, z+w),
                new Vector3(x+w, y, z+w), new Vector3(x, y, z+w),
                new Vector3(x, y, z+w), new Vector3(x, y, z),
                // Top square
                new Vector3(x, y+h, z), new Vector3(x+w, y+h, z),
                new Vector3(x+w, y+h, z), new Vector3(x+w, y+h, z+w),
                new Vector3(x+w, y+h, z+w), new Vector3(x, y+h, z+w),
                new Vector3(x, y+h, z+w), new Vector3(x, y+h, z),
                // Pillars
                new Vector3(x, y, z), new Vector3(x, y+h, z),
                new Vector3(x+w, y, z), new Vector3(x+w, y+h, z),
                new Vector3(x+w, y, z+w), new Vector3(x+w, y+h, z+w),
                new Vector3(x, y, z+w), new Vector3(x, y+h, z+w)
            };

            shader.Bind();
            GL.Uniform4(GL.GetUniformLocation(shader.ID, "overlayColor"), new Vector4(0, 0, 0, 1));
            outlineVAO.Bind();
            GL.BindBuffer(BufferTarget.ArrayBuffer, outlineVBO.ID);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Count * Vector3.SizeInBytes, verts.ToArray(), BufferUsageHint.StreamDraw);
            Matrix4 model = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "model"), true, ref model);
            GL.LineWidth(2.0f);
            GL.DrawArrays(PrimitiveType.Lines, 0, verts.Count);
            GL.LineWidth(1.0f);
            outlineVAO.Unbind();
            GL.Uniform4(GL.GetUniformLocation(shader.ID, "overlayColor"), Vector4.Zero);
        }

        private bool DrawButton(string text, float x, float y, float w, float h)
        {
            Vector2 m = MouseState.Position;
            bool hover = m.X >= x && m.X <= x + w && m.Y >= y && m.Y <= y + h;

            uiRenderer.DrawButton(x, y, w, h, hover);
            textRenderer.UpdateText(text);

            float tw = text.Length * 14f;
            textRenderer.Render(x + (w / 2) - (tw / 2), y + (h / 2) - 12, 0.5f, uiRenderer.GetProjection());

            if (hover && MouseState.IsButtonPressed(MouseButton.Left)) return true;
            return false;
        }

        private void RenderMainMenu()
        {
            uiRenderer.Prepare();
            uiRenderer.DrawTiledBackground(Size.X, Size.Y);

            // Заголовок
            textRenderer.UpdateText("EARTHBOUND");
            textRenderer.Render((Size.X / 2) - 200, 100, 1.5f, uiRenderer.GetProjection());

            float cx = (Size.X - 400) / 2;
            if (DrawButton("PLAY", cx, Size.Y / 2 - 50, 400, 50))
            {
                CurrentState = GameState.WorldSelect;
                savedWorlds = SaveManager.GetWorlds();
                selectedWorldIndex = -1;
            }
            if (DrawButton("SETTINGS", cx, Size.Y / 2 + 10, 400, 50))
            {
                previousState = GameState.MainMenu; // Remember we came from Main Menu
                CurrentState = GameState.Settings;
            }
            if (DrawButton("EXIT", cx, Size.Y / 2 + 70, 400, 50))
            {
                Close();
                // Document / License Button (Top Right)
                float docSize = 40;
                // Move it significantly to the left and down to avoid window borders/controls
                float docX = Size.X - docSize - 20;
                float docY = 20;

                // Draw Button Background
                bool hoverDoc = MouseState.Position.X >= docX && MouseState.Position.X <= docX + docSize &&
                                MouseState.Position.Y >= docY && MouseState.Position.Y <= docY + docSize;

                uiRenderer.DrawButton(docX, docY, docSize, docSize, hoverDoc);

                // Icon rendering
                uiRenderer.RenderQuadCustomVerts(
                new float[] {
                    docX + 35, docY + 5, 1, 1, 0,
                    docX + 35, docY + 35, 1, 0, 0,
                    docX + 5, docY + 35, 0, 0, 0,
                    docX + 5, docY + 5, 0, 1, 0 // Added 4th vertex for Quad consistency if logic changed, but standard 3 is fine for triangle fan style
                },
                texBtnDocument, Vector3.One, 1.0f);



                // Click logic
                if (MouseState.IsButtonPressed(MouseButton.Left))
                {
                    if (MouseState.Position.X >= docX && MouseState.Position.X <= docX + docSize &&
                        MouseState.Position.Y >= docY && MouseState.Position.Y <= docY + docSize)
                    {
                        LoadLicenses();
                        CurrentState = GameState.LicenseMenu;
                    }
                }
            }
        }

        private void RenderSettings()
        {
            uiRenderer.Prepare();

            // Background logic:
            // If we came from Main Menu -> Draw opaque tiled background
            // If we came from Pause -> Draw transparent overlay (so we see the game behind)
            if (previousState == GameState.MainMenu)
            {
                uiRenderer.DrawTiledBackground(Size.X, Size.Y);
            }
            else
            {
                // Dark overlay over the game world
                uiRenderer.DrawRect(0, 0, Size.X, Size.Y, new Vector3(0), 0.7f);
            }

            textRenderer.UpdateText("SETTINGS");
            textRenderer.Render((Size.X / 2) - 100, 50, 1.0f, uiRenderer.GetProjection());

            float cx = (Size.X - 400) / 2;
            float sliderY = 200;

            // --- RENDER DISTANCE SLIDER LOGIC ---

            // 1. Draw Label (Убрали слово Chunks)
            textRenderer.UpdateText($"Render Distance: {Settings.RenderDistance}");

            // Центрируем текст над слайдером
            // (Сдвигаем X чуть левее, так как текст стал короче, или оставляем cx)
            textRenderer.Render(cx + 80, sliderY - 30, 0.5f, uiRenderer.GetProjection());


            // 2. Draw and Interact with Slider
            float sliderW = 400;
            float sliderH = 50;

            uiRenderer.RenderSlider(cx, sliderY, sliderW, sliderH, Settings.RenderDistance, Settings.MinRenderDistance, Settings.MaxRenderDistance);

            // Interaction
            if (MouseState.IsButtonDown(MouseButton.Left))
            {
                float mx = MouseState.Position.X;
                float my = MouseState.Position.Y;

                // Check if mouse is roughly over the slider area (with some padding for ease of use)
                if (mx >= cx - 20 && mx <= cx + sliderW + 20 && my >= sliderY && my <= sliderY + sliderH)
                {
                    // Calculate percentage (0.0 to 1.0)
                    float t = (mx - cx) / sliderW;
                    t = Math.Clamp(t, 0.0f, 1.0f);

                    // Map to Range
                    int newVal = (int)(Settings.MinRenderDistance + (t * (Settings.MaxRenderDistance - Settings.MinRenderDistance)));
                    Settings.RenderDistance = newVal;
                }
            }
            // ------------------------------------

            if (DrawButton("Back", cx, Size.Y - 100, 400, 50))
            {
                CurrentState = previousState; // Return to where we came from
            }
        }

        private void RenderWorldSelect()
        {
            uiRenderer.Prepare();
            uiRenderer.DrawTiledBackground(Size.X, Size.Y);
            textRenderer.UpdateText("Select World");
            textRenderer.Render((Size.X / 2) - 150, 50, 1.0f, uiRenderer.GetProjection());

            float ly = 150;
            float lw = 600;
            float lx = (Size.X - lw) / 2;

            for (int i = 0; i < savedWorlds.Count; i++)
            {
                float y = ly + (i * 90);
                WorldMetadata w = savedWorlds[i];
                bool sel = (i == selectedWorldIndex);

                if (!worldIcons.ContainsKey(w.FolderName))
                {
                    // Исправленная загрузка
                    worldIcons[w.FolderName] = new Texture($"saves/{w.FolderName}/icon.png");
                }

                uiRenderer.DrawRect(lx, y, lw, 80, sel ? new Vector3(0.6f, 0.6f, 0.6f) : new Vector3(0.3f, 0.3f, 0.3f), 1.0f);

                // Рисуем иконку (используем тот самый метод, который сделали public)
                // Using RenderQuadCustomVerts for standard 2D texture (world icon)
                float[] iconVerts = {
                lx + 5, y + 5, 0, 1, 0,
                lx + 75, y + 5, 1, 1, 0,
                lx + 5, y + 75, 0, 0, 0
   };
                uiRenderer.RenderQuadCustomVerts(iconVerts, worldIcons[w.FolderName], Vector3.One, 1f);


                // --- ФИКС ДЛИННОГО ИМЕНИ ---
                string displayName = w.Name;
                if (displayName.Length > 25)
                {
                    displayName = displayName.Substring(0, 25) + "...";
                }

                textRenderer.UpdateText($"{displayName}\n{w.LastPlayed}");
                textRenderer.Render(lx + 90, y + 20, 0.4f, uiRenderer.GetProjection());

                Vector2 m = MouseState.Position;

                if (m.X >= lx && m.X <= lx + lw && m.Y >= y && m.Y <= y + 80 && MouseState.IsButtonPressed(MouseButton.Left))
                {
                    if (selectedWorldIndex == i && (TimeSinceStart() - lastClickTime) < 0.5f) LoadWorld(i);
                    else { selectedWorldIndex = i; lastClickTime = TimeSinceStart(); }
                }
            }

            if (DrawButton("Create New", lx, Size.Y - 140, 200, 50))
            {
                CurrentState = GameState.CreateWorld;
                inputWorldName = "New World";
                inputSeed = "";
            }

            if (selectedWorldIndex >= 0)
            {
                if (DrawButton("Delete", lx + 220, Size.Y - 140, 150, 50))
                {
                    SaveManager.DeleteWorld(savedWorlds[selectedWorldIndex].FolderName);
                    savedWorlds = SaveManager.GetWorlds();
                    selectedWorldIndex = -1;
                }
                if (DrawButton("Play", lx + 390, Size.Y - 140, 210, 50))
                {
                    LoadWorld(selectedWorldIndex);
                }
            }
            if (DrawButton("Back", 20, 20, 100, 40)) CurrentState = GameState.MainMenu;
        }

        private void LoadWorld(int index)
        {
            selectedWorldIndex = index;
            var meta = savedWorlds[index];
            currentWorldFolder = meta.FolderName;
            currentSeed = meta.Seed;

            // СБРОС СОСТОЯНИЯ
            world = new WorldClass(currentWorldFolder, currentSeed);
            inventory = new InventorySystem(); // Очистить инвентарь от старого мира

            camera.position = new Vector3(0, 100, 0); // Сбросить позицию
            camera.velocity = Vector3.Zero;
            camera.smoothRotation = new Vector3(0, -90, 0);
            camera.rawRotation = new Vector3(0, -90, 0);

            isGameLoaded = false;
            // Load time from file
            float t = SaveManager.LoadWorldTime(currentWorldFolder);
            worldTime.SetTime(t);
            CurrentState = GameState.Loading;
        }

        private void LoadLicenses()
        {
            licenseFiles.Clear();
            if (!Directory.Exists("assets")) return;

            try
            {
                // Find all .txt and license files
                var files = Directory.GetFiles("assets", "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".txt") || s.ToLower().Contains("license"));

                foreach (var f in files)
                {
                    string content = File.ReadAllText(f);
                    // Simplify path for display
                    string shortPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), f);
                    licenseFiles.Add(new LicenseFile { Name = Path.GetFileName(f), Path = shortPath, Content = content });
                }
            }
            catch { }
        }

        private void RenderLicenseMenu()
        {
            uiRenderer.Prepare();
            uiRenderer.DrawTiledBackground(Size.X, Size.Y);

            // Header
            textRenderer.UpdateText("LICENSES & ASSETS");
            textRenderer.Render((Size.X / 2) - 150, 30, 1.0f, uiRenderer.GetProjection());

            // Scroll Logic
            float scrollSpeed = 30f;
            licenseScroll += MouseState.ScrollDelta.Y * scrollSpeed;
            if (licenseScroll > 0) licenseScroll = 0;

            float startY = 100 + licenseScroll;
            float contentHeight = 0;

            // Clip Area (Virtual, just don't draw outside Y=80 to Y=Size.Y-80)
            // Ideally use GL.Scissor, but for now just simple render

            foreach (var file in licenseFiles)
            {
                float y = startY + contentHeight;

                // Only render if visible
                if (y > -500 && y < Size.Y + 100)
                {
                    // File Name Box
                    uiRenderer.DrawRect(50, y, Size.X - 100, 40, new Vector3(0.2f), 0.8f);
                    textRenderer.UpdateText(file.Path);
                    textRenderer.Render(60, y + 10, 0.4f, uiRenderer.GetProjection());

                    // Content
                    string[] lines = file.Content.Split('\n');
                    float textY = y + 50;
                    foreach (var line in lines)
                    {
                        // Simple check to not render thousands of lines offscreen
                        if (textY > 0 && textY < Size.Y)
                        {
                            // Very basic text wrapping/truncating would be needed for real app,
                            // here we just render raw
                            string safeLine = line.Length > 80 ? line.Substring(0, 80) + "..." : line;
                            textRenderer.UpdateText(safeLine);
                            textRenderer.Render(60, textY, 0.3f, uiRenderer.GetProjection());
                        }
                        textY += 20;
                    }
                    contentHeight += 50 + (lines.Length * 20) + 30; // Box + Text + Margin
                }
                else
                {
                    // Calculate height without rendering
                    string[] lines = file.Content.Split('\n');
                    contentHeight += 50 + (lines.Length * 20) + 30;
                }
            }

            // Scroll limit bottom
            float minScroll = -(contentHeight - (Size.Y - 150));
            if (contentHeight < Size.Y - 150) minScroll = 0;
            if (licenseScroll < minScroll) licenseScroll = minScroll;

            // Back Button
            if (DrawButton("Back", 20, Size.Y - 60, 100, 40))
            {
                CurrentState = GameState.MainMenu;
            }
        }
        private void RenderCreateWorld()
        {
            uiRenderer.Prepare();
            uiRenderer.DrawTiledBackground(Size.X, Size.Y);
            textRenderer.UpdateText("Create World");
            textRenderer.Render((Size.X / 2) - 150, 50, 1.0f, uiRenderer.GetProjection());

            float cx = (Size.X - 400) / 2;
            uiRenderer.DrawRect(cx, 200, 400, 50, new Vector3(0), 0.7f);
            textRenderer.UpdateText(inputWorldName + (isTypingName ? "_" : ""));
            textRenderer.Render(cx + 10, 210, 0.5f, uiRenderer.GetProjection());

            uiRenderer.DrawRect(cx, 300, 400, 50, new Vector3(0), 0.7f);
            textRenderer.UpdateText("Seed: " + inputSeed + (isTypingSeed ? "_" : ""));
            textRenderer.Render(cx + 10, 310, 0.5f, uiRenderer.GetProjection());

            Vector2 m = MouseState.Position;
            if (MouseState.IsButtonPressed(MouseButton.Left))
            {
                isTypingName = (m.X > cx && m.X < cx + 400 && m.Y > 200 && m.Y < 250);
                isTypingSeed = (m.X > cx && m.X < cx + 400 && m.Y > 300 && m.Y < 350);
            }

            if (DrawButton("Create", cx, 400, 400, 50))
            {
                int s = string.IsNullOrEmpty(inputSeed) ? new Random().Next() : (int.TryParse(inputSeed, out int ps) ? ps : inputSeed.GetHashCode());
                SaveManager.CreateWorld(inputWorldName, s);
                savedWorlds = SaveManager.GetWorlds();
                LoadWorld(savedWorlds.Count - 1);
            }
            if (DrawButton("Cancel", cx, 470, 400, 50)) CurrentState = GameState.WorldSelect;
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            if (CurrentState == GameState.CreateWorld)
            {
                if (isTypingName) inputWorldName += (char)e.Unicode;
                else if (isTypingSeed) inputSeed += (char)e.Unicode;
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (CurrentState == GameState.CreateWorld && e.Key == Keys.Backspace)
            {
                if (isTypingName && inputWorldName.Length > 0)
                    inputWorldName = inputWorldName.Substring(0, inputWorldName.Length - 1);

                if (isTypingSeed && inputSeed.Length > 0)
                    inputSeed = inputSeed.Substring(0, inputSeed.Length - 1);
            }
        }

        private void RenderLoading()
        {
            uiRenderer.Prepare();
            uiRenderer.DrawTiledBackground(Size.X, Size.Y);
            float p = world.GetLoadingProgress();
            textRenderer.UpdateText($"Loading... {(int)(p * 100)}%");
            textRenderer.Render((Size.X / 2) - 150, (Size.Y / 2) - 50, 0.8f, uiRenderer.GetProjection());
            uiRenderer.RenderLoadingBar(Size.X, Size.Y, p);
        }

        private void RenderPauseMenu()
        {
            uiRenderer.Prepare();
            uiRenderer.DrawRect(0, 0, Size.X, Size.Y, new Vector3(0), 0.6f);
            textRenderer.UpdateText("PAUSED");
            textRenderer.Render((Size.X / 2) - 100, 100, 1.2f, uiRenderer.GetProjection());

            float btnW = 300;
            float btnX = (Size.X - btnW) / 2;

            if (DrawButton("Resume", btnX, 300, btnW, 50))
            {
                CurrentState = GameState.Playing;
                CursorState = CursorState.Grabbed;
            }

            // --- NEW SETTINGS BUTTON ---
            if (DrawButton("Settings", btnX, 370, btnW, 50))
            {
                previousState = GameState.Paused; // Remember we came from Pause
                CurrentState = GameState.Settings;
            }
            // ---------------------------

            if (DrawButton("Quit", btnX, 440, btnW, 50)) PerformSaveAndExit();
        }
        private void RenderDeathScreen()
        {
            uiRenderer.Prepare();
            // Фон
            uiRenderer.DrawRect(0, 0, Size.X, Size.Y, new Vector3(0.5f, 0, 0), 0.6f);

            // Текст "YOU DIED"
            // Смещаем сильнее влево. (Size.X / 2) - 220
            textRenderer.UpdateText("YOU DIED");
            textRenderer.Render((Size.X / 2) - 220, Size.Y / 3, 2.0f, uiRenderer.GetProjection());

            float btnW = 300;
            float btnX = (Size.X - btnW) / 2;
            float btnStart = Size.Y / 2;

            if (DrawButton("Respawn", btnX, btnStart, btnW, 50))
            {
                RespawnPlayer();
            }
            if (DrawButton("Back to Menu", btnX, btnStart + 70, btnW, 50))
            {
                PerformSaveAndExit();
            }
        }

        private void RespawnPlayer()
        {
            // Reset logic
            playerHealth = 20.0f;

            // Find world spawn (same logic as Loading)
            int sX = 0; int sZ = 0;
            for (int y = 120; y > 0; y--)
            {
                BlockType b = world.GetBlock(new Vector3(sX, y, sZ));
                if (b != BlockType.AIR && b != BlockType.WATER && b != BlockType.LEAVES && b != BlockType.LOG)
                {
                    camera.position = new Vector3(sX + 0.5f, y + 2, sZ + 0.5f);
                    break;
                }
            }

            camera.Reset();

            camera.ResetFallState();
            CurrentState = GameState.Playing;
            CursorState = CursorState.Grabbed;
        }
        private void PerformSaveAndExit()
        {
            if (world != null && !string.IsNullOrEmpty(currentWorldFolder))
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                shader.Bind();

                bool underwater = world.IsWater(
                    camera.position.X,
                    camera.position.Y + 1.62f,
                    camera.position.Z
                );

                GL.Uniform3(
                    GL.GetUniformLocation(shader.ID, "fogColor"),
                    underwater
                        ? new Vector3(0, 0.1f, 0.4f)
                        : new Vector3(0.5f, 0.7f, 1f)
                );

                GL.Uniform1(
                    GL.GetUniformLocation(shader.ID, "fogDensity"),
                    underwater ? 0.15f : 0.007f
                );

                Matrix4 model = Matrix4.Identity;
                Matrix4 view = camera.GetViewMatrix();
                Matrix4 projection = camera.GetProjectionMatrix();

                GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "model"), true, ref model);
                GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "view"), true, ref view);
                GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "projection"), true, ref projection);

                Matrix4 viewProj = view * projection;
                world.RenderSolid(shader, viewProj, camera.position, globalTextureArray);
                world.RenderTransparent(shader, viewProj, camera.position, globalTextureArray);



                SaveManager.CaptureIcon(currentWorldFolder, Size.X, Size.Y);

                PlayerData pd = new PlayerData();
                // --- SAVE BURNING STATE ---
                pd.SetFromGame(camera, inventory, isBurning, burnTimer);

                if (CurrentState == GameState.Dead)
                {
                    Vector3 bodyPos = camera.GetPositionForSave(true);
                    pd.X = bodyPos.X;
                    pd.Y = bodyPos.Y;
                    pd.Z = bodyPos.Z;
                }

                pd.Health = playerHealth;
                SaveManager.SavePlayer(currentWorldFolder, "Player", pd);

                Console.WriteLine($"[GAME] Saving world to: {currentWorldFolder}");
                world.SaveWorld(currentWorldFolder);

                SaveManager.UpdateWorldData(currentWorldFolder, inputWorldName, currentSeed, worldTime.CurrentTime);
            }

            world = null;
            CurrentState = GameState.MainMenu;
            savedWorlds = SaveManager.GetWorlds();
        }



        private double TimeSinceStart() => GLFW.GetTime();

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, e.Width, e.Height);
            camera.UpdateSize(e.Width, e.Height);
            if (uiRenderer != null) uiRenderer.UpdateSize(e.Width, e.Height);
        }

        private void RenderBreakOverlay(Vector3i pos, int stage)
        {
            // Проверка на валидность стадии
            if (stage < 0 || stage >= TextureData.BreakLayerIndices.Count) return;

            // Получаем индекс слоя текстуры для текущей стадии (0-9)
            int layer = TextureData.BreakLayerIndices[stage];

            float size = 0.503f; // Чуть больше блока, чтобы не мерцало
            float x = pos.X;
            float y = pos.Y;
            float z = pos.Z;

            // Координаты вершин куба (Box)
            List<Vector3> verts = new List<Vector3>() {
                // Front
                new Vector3(x - size, y - size, z + size), new Vector3(x + size, y - size, z + size),
                new Vector3(x + size, y + size, z + size), new Vector3(x - size, y + size, z + size),
                // Back
                new Vector3(x + size, y - size, z - size), new Vector3(x - size, y - size, z - size),
                new Vector3(x - size, y + size, z - size), new Vector3(x + size, y + size, z - size),
                // Left
                new Vector3(x - size, y - size, z - size), new Vector3(x - size, y - size, z + size),
                new Vector3(x - size, y + size, z + size), new Vector3(x - size, y + size, z - size),
                // Right
                new Vector3(x + size, y - size, z + size), new Vector3(x + size, y - size, z - size),
                new Vector3(x + size, y + size, z - size), new Vector3(x + size, y + size, z + size),
                // Top
                new Vector3(x - size, y + size, z + size), new Vector3(x + size, y + size, z + size),
                new Vector3(x + size, y + size, z - size), new Vector3(x - size, y + size, z - size),
                // Bottom
                new Vector3(x - size, y - size, z - size), new Vector3(x + size, y - size, z - size),
                new Vector3(x + size, y - size, z + size), new Vector3(x - size, y - size, z + size)
            };

            // UV координаты (U, V, Layer)
            List<Vector3> allUVs = new List<Vector3>();
            for (int i = 0; i < 6; i++) // 6 граней
            {
                allUVs.Add(new Vector3(0, 0, layer));
                allUVs.Add(new Vector3(1, 0, layer));
                allUVs.Add(new Vector3(1, 1, layer));
                allUVs.Add(new Vector3(0, 1, layer));
            }

            // Цвета (белый)
            List<Vector3> colors = new List<Vector3>();
            for (int i = 0; i < verts.Count; i++) colors.Add(Vector3.One);

            // Биндим VAO и обновляем буферы
            crackVAO.Bind();

            GL.BindBuffer(BufferTarget.ArrayBuffer, crackVBO.ID);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Count * Vector3.SizeInBytes, verts.ToArray(), BufferUsageHint.DynamicDraw);

            // ВАЖНО: crackUV должен быть VBO, принимающим Vector3. 
            // Если в Game.cs при Init он был создан как VBO(List<Vector2>), это может вызвать проблемы.
            // Но мы перезаписываем данные, так что главное - размер байт.
            GL.BindBuffer(BufferTarget.ArrayBuffer, crackUV.ID);
            GL.BufferData(BufferTarget.ArrayBuffer, allUVs.Count * Vector3.SizeInBytes, allUVs.ToArray(), BufferUsageHint.DynamicDraw);

            // Обновляем связь в VAO, так как размер данных изменился (с 2 float на 3 float)
            // Локация 1 (TexCoord), размер 3 (vec3), stride 0 (плотно упаковано)
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, crackColor.ID);
            GL.BufferData(BufferTarget.ArrayBuffer, colors.Count * Vector3.SizeInBytes, colors.ToArray(), BufferUsageHint.DynamicDraw);
            // Fill dummy light data (full brightness 15)
            List<Vector2> lightData = new List<Vector2>();
            for (int i = 0; i < verts.Count; i++) lightData.Add(new Vector2(15, 15));

            GL.BindBuffer(BufferTarget.ArrayBuffer, crackLight.ID);
            GL.BufferData(BufferTarget.ArrayBuffer, lightData.Count * Vector2.SizeInBytes, lightData.ToArray(), BufferUsageHint.DynamicDraw);
            // Настройка шейдера
            shader.Bind();
            Matrix4 model = Matrix4.Identity;
            GL.UniformMatrix4(GL.GetUniformLocation(shader.ID, "model"), true, ref model);

            // Биндим массив текстур
            globalTextureArray.Bind();

            // Включаем режим рендера трещин (в шейдере)
            GL.Uniform1(GL.GetUniformLocation(shader.ID, "isCrack"), 1);

            crackIBO.Bind();
            GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);

            crackVAO.Unbind();

            // Выключаем режим трещин
            GL.Uniform1(GL.GetUniformLocation(shader.ID, "isCrack"), 0);
        }
    
        private float GetMiningSpeed(BlockType tool, BlockType target)
        {
            // Temporary simple implementation to fix compilation
            return 1.0f; 
        }
    }
}
