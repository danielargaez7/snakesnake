using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// UV coordinates of each game hole within each number sprite.
    /// UV convention: x = 0 left → 1 right, y = 0 top → 1 bottom (texture space).
    /// Detected by flood-fill analysis; numeral-shape loops excluded.
    /// </summary>
    public static class HolePositionData
    {
        public static readonly Vector2[][] HoleUVs = new Vector2[11][]
        {
            null, // [0] unused
            // [1]
            new[] { new Vector2(0.560f, 0.509f) },
            // [2]
            new[] { new Vector2(0.357f, 0.359f), new Vector2(0.652f, 0.728f) },
            // [3]
            new[] { new Vector2(0.353f, 0.308f), new Vector2(0.493f, 0.497f), new Vector2(0.347f, 0.678f) },
            // [4]  (excludes small 334 px numeral loop)
            new[] { new Vector2(0.547f, 0.272f), new Vector2(0.339f, 0.599f),
                    new Vector2(0.669f, 0.606f), new Vector2(0.604f, 0.738f) },
            // [5]
            new[] { new Vector2(0.621f, 0.263f), new Vector2(0.402f, 0.266f),
                    new Vector2(0.370f, 0.452f), new Vector2(0.630f, 0.599f),
                    new Vector2(0.338f, 0.719f) },
            // [6]  (excludes 768 px numeral loop)
            new[] { new Vector2(0.486f, 0.263f), new Vector2(0.373f, 0.419f),
                    new Vector2(0.603f, 0.474f), new Vector2(0.350f, 0.608f),
                    new Vector2(0.639f, 0.642f), new Vector2(0.484f, 0.738f) },
            // [7]
            new[] { new Vector2(0.491f, 0.263f), new Vector2(0.342f, 0.266f),
                    new Vector2(0.644f, 0.268f), new Vector2(0.588f, 0.387f),
                    new Vector2(0.523f, 0.500f), new Vector2(0.465f, 0.617f),
                    new Vector2(0.406f, 0.737f) },
            // [8]  (excludes two numeral-loop holes at ~528 and ~598 px)
            new[] { new Vector2(0.388f, 0.294f), new Vector2(0.624f, 0.301f),
                    new Vector2(0.391f, 0.433f), new Vector2(0.606f, 0.442f),
                    new Vector2(0.364f, 0.572f), new Vector2(0.636f, 0.574f),
                    new Vector2(0.603f, 0.717f), new Vector2(0.412f, 0.719f) },
            // [9]  (excludes 771 px numeral loop)
            new[] { new Vector2(0.458f, 0.260f), new Vector2(0.613f, 0.288f),
                    new Vector2(0.348f, 0.347f), new Vector2(0.664f, 0.401f),
                    new Vector2(0.348f, 0.467f), new Vector2(0.649f, 0.517f),
                    new Vector2(0.473f, 0.540f), new Vector2(0.596f, 0.631f),
                    new Vector2(0.512f, 0.735f) },
            // [10] (excludes 2388 px "0" numeral interior)
            new[] { new Vector2(0.692f, 0.285f), new Vector2(0.553f, 0.287f),
                    new Vector2(0.312f, 0.287f), new Vector2(0.312f, 0.439f),
                    new Vector2(0.502f, 0.500f), new Vector2(0.736f, 0.502f),
                    new Vector2(0.312f, 0.588f), new Vector2(0.544f, 0.706f),
                    new Vector2(0.681f, 0.713f), new Vector2(0.312f, 0.729f) },
        };
    }
}
