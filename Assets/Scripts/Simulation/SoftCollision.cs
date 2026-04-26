using System.Collections.Generic;
using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Pure-software collision for XZ movement — no PhysX required.
    ///
    /// Because Unity 6 does not register procedurally-created static colliders
    /// into the PhysX broadphase in this project, the player walks through every
    /// wall, table, and NPC.  This class maintains a simple list of axis-aligned
    /// box obstacles (AABBs) and resolves player-circle-vs-box overlap in C#,
    /// completely bypassing PhysX.
    ///
    /// Usage:
    ///   • Register obstacles once at scene-build time with AddBox().
    ///   • Each Update frame, call ResolveXZ(oldPos, newPos) to get an adjusted
    ///     position that doesn't penetrate any obstacle.
    ///   • Call Clear() when restarting or changing scenes.
    /// </summary>
    public static class SoftCollision
    {
        // Capsule radius used for all player-vs-box checks (metres).
        // Slightly larger than the CharacterController radius (0.3) to give a
        // comfortable buffer so the player never "clips" visually.
        public const float PlayerRadius = 0.45f;

        private static readonly List<Bounds> _boxes = new List<Bounds>();

        /// <summary>Register one AABB obstacle.</summary>
        public static void AddBox(Vector3 center, Vector3 size)
        {
            _boxes.Add(new Bounds(center,
                new Vector3(Mathf.Abs(size.x),
                            Mathf.Abs(size.y),
                            Mathf.Abs(size.z))));
        }

        /// <summary>Remove all registered obstacles (call on scene change).</summary>
        public static void Clear() => _boxes.Clear();

        /// <summary>
        /// Given the player's current position and a proposed new position,
        /// return a corrected position that respects all registered AABBs.
        ///
        /// Only X and Z are adjusted — Y is handled by the ground-snap system.
        /// Three resolution passes handle concave corners (e.g. a table corner).
        /// </summary>
        public static Vector3 ResolveXZ(Vector3 current, Vector3 proposed)
        {
            Vector3 result = proposed;

            // Three iterations handle multi-box corner cases cleanly.
            for (int iter = 0; iter < 3; iter++)
            {
                foreach (Bounds b in _boxes)
                {
                    // Nearest point on the AABB surface to the player's XZ position
                    float cx = Mathf.Clamp(result.x, b.min.x, b.max.x);
                    float cz = Mathf.Clamp(result.z, b.min.z, b.max.z);

                    float dx = result.x - cx;
                    float dz = result.z - cz;
                    float sqDist = dx * dx + dz * dz;

                    if (sqDist >= PlayerRadius * PlayerRadius) continue; // no overlap

                    float dist = Mathf.Sqrt(sqDist);

                    if (dist < 0.001f)
                    {
                        // Player centre is INSIDE the box — derive push direction
                        // from the previous (valid) position so we always push outward.
                        float prevCx = Mathf.Clamp(current.x, b.min.x, b.max.x);
                        float prevCz = Mathf.Clamp(current.z, b.min.z, b.max.z);
                        float pdx = current.x - prevCx;
                        float pdz = current.z - prevCz;
                        float pd = Mathf.Sqrt(pdx * pdx + pdz * pdz);
                        if (pd > 0.001f)
                        {
                            result.x = prevCx + (pdx / pd) * PlayerRadius;
                            result.z = prevCz + (pdz / pd) * PlayerRadius;
                        }
                        else
                        {
                            // Degenerate — push right as last resort
                            result.x = b.max.x + PlayerRadius;
                        }
                    }
                    else
                    {
                        // Push the player out of the overlap zone along the
                        // surface normal (shortest escape direction).
                        float push = PlayerRadius - dist;
                        result.x += (dx / dist) * push;
                        result.z += (dz / dist) * push;
                    }
                }
            }

            return result;
        }
    }
}
