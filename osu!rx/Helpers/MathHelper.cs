using System;
using System.Numerics;

namespace osu_rx.Helpers
{
    public class MathHelper
    {
        public static int Clamp(int value, int min, int maX) => value < min ? min : value > maX ? maX : value;

        public static double GetAngle(Vector2 a, Vector2 b, Vector2 c)
        {
            float ABSquared = Vector2.DistanceSquared(a, b);
            float BCSquared = Vector2.DistanceSquared(b, c);
            float ACSquared = Vector2.DistanceSquared(a, c);

            double result = Math.Acos((ABSquared + ACSquared - BCSquared) / (2 * Math.Sqrt(ABSquared) * Math.Sqrt(ACSquared)));
            if (result < 0)
                result = Math.PI - result;

            return result * RadToDeg;
        }

        public static double RadToDeg => 360 / (Math.PI * 2);
    }
}
