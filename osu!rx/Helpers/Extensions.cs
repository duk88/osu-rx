using System;

namespace osu_rx.Helpers
{
    public static class Extensions
    {
        public static float NextFloat(this Random random, float min, float max) => (float)random.NextDouble() * (max - min) + min;
    }
}
