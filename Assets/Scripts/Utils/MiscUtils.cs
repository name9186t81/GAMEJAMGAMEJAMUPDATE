using System;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

namespace Utils
{
    public static class MiscUtils
    {
        private static RaycastHit2D[] _cached = new RaycastHit2D[32];
        public static float SafeEvaluate(this AnimationCurve curve, float t, float defaultValue = 1)
        {
            return curve == null ? defaultValue : curve.Evaluate(t);
        }

        public static void SeededAction(int seed, Action action)
        {
            var state = Random.state;
            Random.InitState(seed);

            action();

            Random.state = state;
        }

        public static RaycastHit2D RaycastIgnoreCollisions(Vector2 start, Vector2 direction, float length, int layerMask, HashSet<Collider2D> ignored)
        {
            var over = Physics2D.RaycastNonAlloc(start, direction, _cached, length, layerMask); //unity sorts all colliders by distance

            for(int i = 0; i < over; i++)
            {
                if (ignored.Contains(_cached[i].collider)) continue;

                return _cached[i];
            }

            return default;
        }

        /// <summary>
        /// Filter should return true if the raycasthit passes it.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        /// <param name="layerMask"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static RaycastHit2D RaycastWithFilter(Vector2 start, Vector2 direction, float length, int layerMask, Func<RaycastHit2D, bool> filter)
        {
            var over = Physics2D.RaycastNonAlloc(start, direction, _cached, length, layerMask); //unity sorts all colliders by distance

            for (int i = 0; i < over; i++)
            {
                if (filter(_cached[i]))
                {
                    return _cached[i];
                }
            }

            return default;
        }

        /// <summary>
        /// Filter should return true if the raycasthit passes it.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="rad"></param>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        /// <param name="layerMask"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static RaycastHit2D CircleCastWithFilter(Vector2 start, float rad, Vector2 direction, float length, int layerMask, Func<RaycastHit2D, bool> filter)
        {
            var over = Physics2D.CircleCast(start, rad, direction, new ContactFilter2D() { layerMask = layerMask, useLayerMask = true }, _cached, length); //unity sorts all colliders by distance

            for (int i = 0; i < over; i++)
            {
                if (filter(_cached[i]))
                {
                    return _cached[i];
                }
            }

            return default;
        }
    }
}
