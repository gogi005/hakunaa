using Godot;

namespace AimTrainer
{
    /// <summary>
    /// Central place for tunable gameplay values.
    /// Future systems (weapon selection, training presets, Valorant-style
    /// sensitivity conversion, player profiles...) should read/write these
    /// values so the rest of the code never hard-codes a number.
    /// </summary>
    public static class GameSettings
    {
        // ----- Mouse / camera ------------------------------------------------
        public const float DefaultMouseSensitivity = 0.0025f;   // radians per raw mouse pixel
        public const float MinPitchDeg = -89.0f;                // look-up limit
        public const float MaxPitchDeg = 89.0f;                 // look-down limit

        // ----- Player ---------------------------------------------------------
        public const float DefaultMoveSpeed = 5.0f;             // m/s (WASD)
        public const float EyeHeight = 1.7f;                    // camera height above floor

        // ----- Round / scoring ------------------------------------------------
        public const float DefaultRoundDuration = 60.0f;        // seconds
        public const int HitScore = 1;                          // points per target hit
        public const int MissPenalty = 0;                       // optional: points removed per miss

        // ----- Targets --------------------------------------------------------
        public const float DefaultTargetRadius = 0.15f;         // metres
        public const float DefaultMinSpawnDistance = 4.0f;      // closest spawn (m)
        public const float DefaultMaxSpawnDistance = 10.0f;     // farthest spawn (m)
        public const int DefaultMaxActiveTargets = 1;           // Flick Practice = 1
        public const float DefaultTargetMoveSpeed = 0.0f;       // reserved: moving-target modes
    }
}
