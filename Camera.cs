using EarthBound.World.Blocks;
using EarthBound.World;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
namespace EarthBound
{
    public class Camera
    {
        private float SCREENWIDTH;
        private float SCREENHEIGHT;

    // -- MOVEMENT SETTINGS --
    // Увеличили ускорение, чтобы персонаж сразу набирал скорость (фикс "вязкого" управления)
    private const float ACCELERATION = 80.0f;
        private const float FRICTION = 10.0f;
        private const float AIR_FRICTION = 2.0f;

        // Увеличили скорость ходьбы. Спринт оставили (или чуть подтянули для баланса)
        private const float MAX_SPEED_WALK = 6.0f;
        private const float MAX_SPEED_SPRINT = 7.5f;

        private const float JUMP_FORCE = 9.0f;
        private const float GRAVITY = 28.0f;
        private float safetyTimer = 0.0f; // Таймер безопасности

        // -- CAMERA FEEL --
        private const float MOUSE_SENSITIVITY = 0.12f;
        private const float SMOOTH_LOOK_SPEED = 18.0f;
        private const float BOB_FREQUENCY = 10.0f;
        private const float BOB_AMPLITUDE = 0.08f;

        // -- STATE --
        public Vector3 position;
        public Vector3 rawRotation;
        public Vector3 smoothRotation;
        public Vector3 velocity;

        // --- NEW: Walking Distance for Audio ---
        public float WalkDistance = 0.0f;
        // -------------------------------------

        public Vector3 Front { get; private set; }
        public bool JustLanded = false;

        // -- COLLISION --
        private const float PLAYER_WIDTH = 0.6f;
        // Increased height to accommodate higher camera
        private const float PLAYER_HEIGHT = 1.9f;
        // User requested ~1.8 blocks height for camera view
        private const float EYE_LEVEL = 1.8f;

        // Changed FOV to 70 as requested
        private float currentFov = 70.0f;
        private float bobTimer = 0.0f;
        private float currentBobOffset = 0.0f;


        private bool firstMove = true;
        private Vector2 lastPos;

        // --- DAMAGE & FALLING ---
        public float DamageFlash = 0.0f; // 0 to 1 (Red overlay alpha)
        public float DamageTilt = 0.0f;  // Angle in degrees

        private bool wasGrounded = false;
        private float airPeakY = 0.0f;
        public int PendingDamage = 0; // Read by Game.cs

        // --- DEATH CAM ---
        private float deathTimer = 0.0f;
        private Vector3 deathCenter = Vector3.Zero;

        public Camera(float width, float height, Vector3 position)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
            this.position = position;
            this.rawRotation = new Vector3(0, -90, 0);
            this.smoothRotation = rawRotation;
            this.airPeakY = position.Y;
            UpdateVectors();
        }
        // Вызывать сразу после телепортации или загрузки!
        public void ResetFallState()
        {
            airPeakY = position.Y;
            velocity = Vector3.Zero;
            PendingDamage = 0;
            wasGrounded = true;
            safetyTimer = 1.0f; // <-- Даем 1 секунду неуязвимости к гравитации
        }

        // Если мы мертвы, камера летает где попало. 
        // Сохранять эту позицию нельзя, иначе при загрузке мы будем в небе.
        // Возвращаем позицию тела (центра смерти).
        public Vector3 GetPositionForSave(bool isDead)
        {
            return isDead ? deathCenter : position;
        }

        // Чтобы при загрузке мертвого сохранения камера сразу начинала крутиться
        public void SetDeathState(Vector3 bodyPos)
        {
            deathCenter = bodyPos;
            deathTimer = 0.0f;
            // Ставим камеру чуть выше, чтобы сразу начать облет
            position = bodyPos + new Vector3(0, 5, 0);
        }
        public void UpdateSize(float width, float height)
        {
            SCREENWIDTH = width;
            SCREENHEIGHT = height;
        }

        public Vector3 GetEyePosition()
        {
            return position + new Vector3(0, EYE_LEVEL + currentBobOffset, 0);
        }

        private void UpdateVectors()
        {
            float pitchRad = MathHelper.DegreesToRadians(smoothRotation.X);
            float yawRad = MathHelper.DegreesToRadians(smoothRotation.Y);

            Front = new Vector3(
                MathF.Cos(pitchRad) * MathF.Cos(yawRad),
                MathF.Sin(pitchRad),
                MathF.Cos(pitchRad) * MathF.Sin(yawRad)
            ).Normalized();
        }

        public Matrix4 GetViewMatrix()
        {
            UpdateVectors();

            currentBobOffset = MathF.Sin(bobTimer) * BOB_AMPLITUDE * (IsGrounded(null) ? 1 : 0);
            if (velocity.Length < 0.5f) currentBobOffset = 0;

            Vector3 eyePos = GetEyePosition();

            // Apply Damage Tilt (Rotate Z)
            Matrix4 tiltMat = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(DamageTilt));

            return Matrix4.LookAt(eyePos, eyePos + Front, Vector3.UnitY) * tiltMat;
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(currentFov), SCREENWIDTH / SCREENHEIGHT, 0.1f, 300.0f);
        }

        public void TriggerDamageEffect()
        {
            DamageFlash = 0.8f; // Bright red flash
            DamageTilt = 10.0f; // Tilt left
        }

        public void Reset()
        {
            velocity = Vector3.Zero;
            DamageFlash = 0;
            DamageTilt = 0;
            PendingDamage = 0;
            airPeakY = position.Y;
            smoothRotation = rawRotation;
            deathTimer = 0;
        }

        internal void Update(KeyboardState input, MouseState mouse, FrameEventArgs e, WorldClass world, bool isDead)
        {
            float dt = (float)e.Time;
            if (dt > 0.1f) dt = 0.1f;

            // --- DAMAGE EFFECTS DECAY ---
            if (DamageFlash > 0)
            {
                DamageFlash -= dt * 2.0f; // Fade out red
                if (DamageFlash < 0) DamageFlash = 0;
            }

            if (MathF.Abs(DamageTilt) > 0.1f)
            {
                // Smoothly return tilt to 0
                DamageTilt = MathHelper.Lerp(DamageTilt, 0, dt * 5.0f);
            }

            // --- DEATH CAMERA LOGIC ---
            if (isDead)
            {
                if (deathTimer == 0) deathCenter = position; // Capture death spot
                deathTimer += dt;

                // Spiral up and around
                float radius = 5.0f;
                float speed = 0.5f;
                float height = Math.Min(deathTimer * 2.0f, 10.0f);

                position.X = deathCenter.X + MathF.Cos(deathTimer * speed) * radius;
                position.Z = deathCenter.Z + MathF.Sin(deathTimer * speed) * radius;
                position.Y = deathCenter.Y + height;

                // Look at death body (center)
                Vector3 direction = (deathCenter - position).Normalized();
                rawRotation.Y = MathHelper.RadiansToDegrees(MathF.Atan2(direction.Z, direction.X));
                rawRotation.X = MathHelper.RadiansToDegrees(MathF.Asin(direction.Y));

                smoothRotation = Vector3.Lerp(smoothRotation, rawRotation, dt * 5.0f);
                return; // Stop processing movement
            }


            // --- NORMAL MOVEMENT ---

            if (firstMove) { lastPos = new Vector2(mouse.X, mouse.Y); firstMove = false; }
            float deltaX = mouse.X - lastPos.X;
            float deltaY = mouse.Y - lastPos.Y;
            lastPos = new Vector2(mouse.X, mouse.Y);

            rawRotation.Y += deltaX * MOUSE_SENSITIVITY;
            rawRotation.X -= deltaY * MOUSE_SENSITIVITY;
            rawRotation.X = Math.Clamp(rawRotation.X, -89f, 89f);

            smoothRotation = Vector3.Lerp(smoothRotation, rawRotation, dt * SMOOTH_LOOK_SPEED);

            // ТЕПЕРЬ БЕГ НА CTRL
            bool isSprinting = input.IsKeyDown(Keys.LeftControl);

            bool eyesInWater = world != null && world.IsWater(position.X, position.Y + EYE_LEVEL, position.Z);
            bool feetInWater = world != null && world.IsWater(position.X, position.Y + 0.2f, position.Z);

            // Мы "в жидкости", если хотя бы ноги или глаза в воде
            bool isInLiquid = eyesInWater || feetInWater;

            bool onGround = IsGrounded(world);

            // === ФИКС НАКОПЛЕНИЯ УРОНА ===
            if (isInLiquid)
            {
                // Если мы в воде - мы в безопасности.
                // Сбрасываем точку "начала падения" на текущую позицию.
                airPeakY = position.Y;
                PendingDamage = 0;
                wasGrounded = true; // Считаем, что мы "на земле", чтобы логика прыжков работала
            }
            else if (!onGround)
            {
                // Мы в воздухе и НЕ в воде. Запоминаем максимальную высоту.
                if (position.Y > airPeakY) airPeakY = position.Y;
            }

            if (onGround && !wasGrounded && !isInLiquid)
            {
                // Приземлились на твердое (не вода)
                float fallDistance = airPeakY - position.Y;

                // Звук приземления
                if (fallDistance > 0.2f)
                {
                    JustLanded = true;
                }

                // Урон (только если упали сильно)
                if (fallDistance > 4.0f)
                {
                    int dmg = (int)(fallDistance - 3.0f);
                    if (dmg > 0) PendingDamage += dmg;
                }

                // Сброс высоты после приземления
                airPeakY = position.Y;
            }

            wasGrounded = onGround || isInLiquid; // В воде мы как бы "на земле" для логики состояний
            // -------------------------

            float yawRad = MathHelper.DegreesToRadians(rawRotation.Y);
            Vector3 frontFlat = new Vector3(MathF.Cos(yawRad), 0, MathF.Sin(yawRad)).Normalized();
            Vector3 rightFlat = Vector3.Normalize(Vector3.Cross(frontFlat, Vector3.UnitY));

            Vector3 wishDir = Vector3.Zero;
            if (input.IsKeyDown(Keys.W)) wishDir += frontFlat;
            if (input.IsKeyDown(Keys.S)) wishDir -= frontFlat;
            if (input.IsKeyDown(Keys.A)) wishDir -= rightFlat;
            if (input.IsKeyDown(Keys.D)) wishDir += rightFlat;

            if (wishDir.LengthSquared > 0) wishDir.Normalize();

            // Скорость. В воде медленнее.
            float maxSpeed = isInLiquid ? 3.5f : (isSprinting ? MAX_SPEED_SPRINT : MAX_SPEED_WALK);
            float friction = (onGround && !isInLiquid) ? FRICTION : AIR_FRICTION;

            // Если в воде - сильное трение, чтобы не скользить
            if (isInLiquid) friction = 5.0f;

            velocity.X -= velocity.X * friction * dt;
            velocity.Z -= velocity.Z * friction * dt;
            velocity += wishDir * ACCELERATION * dt;

            Vector2 horizontalVel = new Vector2(velocity.X, velocity.Z);
            if (horizontalVel.Length > maxSpeed)
            {
                horizontalVel = horizontalVel.Normalized() * maxSpeed;
                velocity.X = horizontalVel.X;
                velocity.Z = horizontalVel.Y;
            }

            if (safetyTimer > 0)
            {
                safetyTimer -= dt;
                velocity.Y = 0; // Держим игрока в воздухе
            }
            else
            {
                // Стандартная логика гравитации
                if (isInLiquid)
                {
                    velocity.Y -= GRAVITY * 0.2f * dt; // Медленное падение в воде
                    if (input.IsKeyDown(Keys.Space)) velocity.Y += ACCELERATION * dt * 0.5f; // Плывем вверх
                    velocity.Y = Math.Clamp(velocity.Y, -4f, 4f);
                }
                else
                {
                    velocity.Y -= GRAVITY * dt;
                    if (onGround && input.IsKeyDown(Keys.Space))
                    {
                        velocity.Y = JUMP_FORCE;
                    }
                }
            }


            // Collisions X
            position.X += velocity.X * dt;
            if (CheckCollision(position, world))
            {
                // Попытка ступеньки (Step Up)
                if (onGround && !CheckCollision(position + new Vector3(0, 0.6f, 0), world))
                {
                    position.Y += 0.6f; // Поднимаем временно
                    if (CheckCollision(position, world)) // Если все еще стена
                    {
                        position.Y -= 0.6f; // Отменяем
                        position.X -= velocity.X * dt;
                    }
                    else
                    {
                        // Успешный степ! Но мы поднялись резко на 0.6.
                        // В идеале плавно, но для простоты оставляем резкий подъем (как в майне 1.0)
                        // НО! Нужно опустить игрока на землю, если ступенька ниже 0.6
                        // Просто оставляем поднятым, гравитация в след кадре прижмет
                    }
                }
                else
                {
                    position.X -= velocity.X * dt;
                }
            }

            // Collisions Z
            position.Z += velocity.Z * dt;
            if (CheckCollision(position, world))
            {
                // Попытка ступеньки (Step Up)
                if (onGround && !CheckCollision(position + new Vector3(0, 0.6f, 0), world))
                {
                    position.Y += 0.6f;
                    if (CheckCollision(position, world))
                    {
                        position.Y -= 0.6f;
                        position.Z -= velocity.Z * dt;
                    }
                }
                else
                {
                    position.Z -= velocity.Z * dt;
                }
            }

            // Collisions Y
            position.Y += velocity.Y * dt;
            if (CheckCollision(position, world))
            {
                position.Y -= velocity.Y * dt;
                velocity.Y = 0;
            }

            if (onGround && horizontalVel.Length > 0.1f)
            {
                bobTimer += dt * (isSprinting ? BOB_FREQUENCY * 1.3f : BOB_FREQUENCY);
                // --- NEW: Accumulate Distance for Steps ---
                WalkDistance += horizontalVel.Length * dt;
            }
            else
            {
                bobTimer = 0;
                // Не сбрасываем WalkDistance, чтобы не сбивать ритм
            }

            float targetFov = (isSprinting && horizontalVel.Length > 4f) ? 85.0f : 75.0f;
            currentFov = MathHelper.Lerp(currentFov, targetFov, dt * 5.0f);
        }

        private bool IsGrounded(WorldClass world)
        {
            if (world == null) return false;
            return CheckCollision(position - new Vector3(0, 0.05f, 0), world);
        }

        private bool CheckCollision(Vector3 pos, WorldClass world)
        {
            if (world == null) return false;

            float r = PLAYER_WIDTH / 2.0f;
            float h = PLAYER_HEIGHT;

            int minX = (int)MathF.Floor(pos.X - r + 0.5f);
            int maxX = (int)MathF.Floor(pos.X + r + 0.5f);
            int minZ = (int)MathF.Floor(pos.Z - r + 0.5f);
            int maxZ = (int)MathF.Floor(pos.Z + r + 0.5f);

            int minY = (int)MathF.Floor(pos.Y + 0.5f);
            int maxY = (int)MathF.Floor(pos.Y + h - 0.1f + 0.5f);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        BlockType type = world.GetBlock(new Vector3(x, y, z));
                        if (type == BlockType.AIR) continue;

                        byte data = world.GetBlockData(new Vector3(x, y, z));

                        // --- ЗАМЕНА ---
                        AABB box = EarthBound.World.Blocks.BlocksManager.GetBlock(type).GetBoundingBox(data);
                        // --------------

                        if (box.Max == box.Min) continue;

                        Vector3 blockPos = new Vector3(x, y, z);
                        Vector3 boxMin = blockPos + box.Min;
                        Vector3 boxMax = blockPos + box.Max;

                        if (pos.X + r > boxMin.X && pos.X - r < boxMax.X &&
                            pos.Y + h > boxMin.Y && pos.Y < boxMax.Y &&
                            pos.Z + r > boxMin.Z && pos.Z - r < boxMax.Z)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
