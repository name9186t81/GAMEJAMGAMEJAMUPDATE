using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Utils
{
    public static class MathUtils
    {
        /// <summary>
        /// Returns normalized direction to another point in space.
        /// </summary>
        /// <param name="vec2"></param>
        /// <param name="target"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Vector2 DirectionNormalized(this Vector2 vec2, Vector2 target, out float distance)
        {
            distance = Vector2.Distance(vec2, target);
            return (target - vec2) / distance;
        }

        /// <summary>
        /// Returns normalized direction to another point in space.
        /// </summary>
        /// <param name="vec2"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Vector2 DirectionNormalized(this Vector2 vec2, Vector2 target)
        {
            return (target - vec2).normalized;
        }

        public static Vector2 Perpendicular1(this Vector2 vec)
        {
            return new Vector2(-vec.y, vec.x);
        }

        public static Vector2 Perpendicular2(this Vector2 vec)
        {
            return new Vector2(vec.y, -vec.x);
        }

        /// <summary>
        /// Returns direction to another point in space.
        /// </summary>
        /// <param name="vec2"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Vector2 Direction(this Vector2 vec2, Vector2 target)
        {
            return target - vec2;
        }

        /// <summary>
        /// Angle in radians.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Rotate(this in Vector2 vector, float angle)
        {
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);

            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        /// <summary>
        /// Calculates integral of a given curve on a range[0, 1).
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Area(this Func<float, float> curve, int steps = 100)
        {
            float delta = 1f / steps;

            float res = 0;
            float prevVal = curve(0);
            for (int i = 1; i < steps; i++)
            {
                float val = curve(delta * i);

                res += delta * 0.5f * (prevVal + val);
                prevVal = val;
            }

            return res;
        }


        /// <summary>
        /// Calculates integral of a given curve on a range[0, 1).
        /// </summary>
        /// <param name="curve"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Area(this AnimationCurve curve, int steps = 100)
        {
            float delta = 1f / steps;

            float res = 0;
            float prevVal = curve.Evaluate(0);
            for (int i = 1; i < steps; i++)
            {
                float val = curve.Evaluate(delta * i);

                res += delta * 0.5f * (prevVal + val);
                prevVal = val;
            }

            return res;
        }
        public static float Perlin2D(int x, int y, int width, int height, int offset = 0)
        {
            return Mathf.PerlinNoise1D((x + y * width + offset) / (float)(width * height));
        }

        /// <summary>
        /// Lerps angle presented in normal way(0..1) with correct wrapping.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float LerpAngleNormalized(float a, float b, float t)
        {
            return Mathf.LerpAngle(a * 360, b * 360, t) / 360;
        }

        /// <summary>
        /// Returns positive number if vec2 is clockwise, negative number if vec2 is counterclockwise, 0 if vectors are pointing in the same direction.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cross(this in Vector2 vec1, Vector2 vec2)
        {
            return vec1.x * vec2.y - vec1.y * vec2.x;
        }

        public static bool AABB(Vector2 center1, Vector2 size1, Vector2 center2, Vector2 size2)
        {
            return Mathf.Abs(center1.x - center2.x) <= (size1.x + size2.x) &&
                Mathf.Abs(center1.y - center2.y) <= (size1.y + size2.y);
        }

        public static Vector2 Slerp(this Vector2 start, Vector2 end, float t)
        {
            float angleA = Mathf.Atan2(start.y, start.x);
            float angleB = Mathf.Atan2(end.y, end.x);

            float diff = angleB - angleA;
            diff = Mathf.Repeat(diff + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;
            float angle = angleA + diff * t;
            float len = Mathf.Lerp(start.magnitude, end.magnitude, t);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * len;
        }

        public static Vector2 SlerpNormals(this Vector2 start, Vector2 end, float t)
        {
            float angleA = Mathf.Atan2(start.y, start.x);
            float angleB = Mathf.Atan2(end.y, end.x);

            float diff = angleB - angleA;
            diff = Mathf.Repeat(diff + Mathf.PI * 3, 2f * Mathf.PI) - Mathf.PI;
            float angle = angleA + diff * t;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public static Vector2 ClampRadial(this Vector2 targetDirection, Vector2 direction, float maxAngleRad) => ClampRadial(targetDirection, direction, maxAngleRad, maxAngleRad);
        public static Vector2 ClampRadial(this Vector2 targetDirection, Vector2 direction, float maxRadAngleCounterClockwise, float maxRadAngleClockwise)
        {
            float dot   = direction.x * targetDirection.x + direction.y * targetDirection.y;
            float cross = direction.x * targetDirection.y - direction.y * targetDirection.y;

            float diff = Mathf.Atan2(cross, dot);

            if (diff > maxRadAngleCounterClockwise)
            {
                diff = maxRadAngleCounterClockwise;
            }
            else if (diff < -maxRadAngleClockwise)
            {
                diff = -maxRadAngleClockwise;
            }

            return Mathf.Abs(diff) < Mathf.Epsilon ? targetDirection : direction.Rotate(diff);
        }
    }
}
