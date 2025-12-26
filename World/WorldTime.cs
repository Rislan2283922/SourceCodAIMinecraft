using OpenTK.Mathematics;
using System;

namespace EarthBound.World
{
    public class WorldTime
    {
        // Total cycle length in real seconds (20 minutes = 1200 seconds)
        public const float DAY_LENGTH_SECONDS = 1200.0f;

        // Current time in seconds (0 to 1200)
        public float CurrentTime { get; private set; } = 300.0f; // Start at 5 mins (Day)

        // Visual properties
        public Vector3 SkyColor { get; private set; }
        public Vector3 GlobalLight { get; private set; }
        public float SunAngle { get; private set; } // Rotation in radians
        public float StarAlpha { get; private set; }

        // Cycle phases (in seconds)
        // 0-120: Dawn
        // 120-720: Day (10 mins)
        // 720-840: Sunset
        // 840-1200: Night (6 mins)

        public void Update(float dt)
        {
            CurrentTime += dt;
            if (CurrentTime >= DAY_LENGTH_SECONDS) CurrentTime -= DAY_LENGTH_SECONDS;

            CalculateVisuals();
        }

        public void SetTime(float time)
        {
            CurrentTime = time;
            CalculateVisuals();
        }

        private void CalculateVisuals()
        {
            // Calculate progress 0.0 to 1.0
            float progress = CurrentTime / DAY_LENGTH_SECONDS;

            // Sun Rotation
            SunAngle = (progress * MathF.PI * 2.0f) - (MathF.PI / 2.0f);

            // Colors
            Vector3 colNight = new Vector3(0.01f, 0.01f, 0.03f); // Очень темная синяя ночь
            Vector3 colDawn = new Vector3(0.9f, 0.4f, 0.2f);
            Vector3 colDay = new Vector3(0.5f, 0.7f, 1.0f);      // Чуть ярче небо
            Vector3 colSunset = new Vector3(0.8f, 0.3f, 0.1f);

            // Light intensity (Global Light for shader)
            // Night was 0.2 -> Now 0.05 (Very dark)
            float lightIntensity = 0.05f;

            if (CurrentTime < 120) // Dawn (0 - 2 min)
            {
                float t = CurrentTime / 120.0f;
                SkyColor = Vector3.Lerp(colNight, colDawn, t);
                lightIntensity = MathHelper.Lerp(0.05f, 0.7f, t);
                StarAlpha = 1.0f - t;
            }
            else if (CurrentTime < 720) // Day (2 - 12 min)
            {
                float t = (CurrentTime - 120) / 600.0f;
                if (t < 0.2f) SkyColor = Vector3.Lerp(colDawn, colDay, t * 5.0f);
                else SkyColor = colDay;

                lightIntensity = 1.0f; // Full sun
                StarAlpha = 0.0f;
            }
            else if (CurrentTime < 840) // Sunset (12 - 14 min)
            {
                float t = (CurrentTime - 720) / 120.0f;
                SkyColor = Vector3.Lerp(colDay, colSunset, t);
                lightIntensity = MathHelper.Lerp(1.0f, 0.5f, t);
                StarAlpha = t * 0.5f;
            }
            else // Night (14 - 20 min)
            {
                float t = (CurrentTime - 840) / 360.0f;
                if (t < 0.2f) SkyColor = Vector3.Lerp(colSunset, colNight, t * 5.0f);
                else SkyColor = colNight;

                lightIntensity = 0.05f; // Dark night
                StarAlpha = 1.0f;
            }

            GlobalLight = new Vector3(lightIntensity);
        }
    }
}