using Godot;

namespace AimTrainer
{
    /// <summary>
    /// Base class for all training modes. New modes (Tracking, Reaction,
    /// Spray Practice, ...) only need to override these hooks - the manager,
    /// HUD and spawner stay unchanged.
    /// </summary>
    public abstract partial class TrainingMode : Resource
    {
        [Export] public string ModeName = "Unnamed Mode";
        [Export] public float DurationSeconds = GameSettings.DefaultRoundDuration;
        [Export] public int MaxActiveTargets = GameSettings.DefaultMaxActiveTargets;

        /// <summary>Called when a round starts.</summary>
        public virtual void OnRoundStart(AimTrainerManager manager) { }

        /// <summary>Called every frame while the round is running.</summary>
        public virtual void OnUpdate(AimTrainerManager manager, double delta) { }

        /// <summary>Called after the player hits a target.</summary>
        public virtual void OnTargetHit(AimTrainerManager manager, Target target)
        {
            // Default Flick-Practice behavior: immediately replace the target.
            manager.Spawner.SpawnOne();
        }
    }

    /// <summary>
    /// "Flick Practice": one target at a time, 60 seconds, as many hits as possible.
    /// </summary>
    public partial class FlickPracticeMode : TrainingMode
    {
        public override void OnRoundStart(AimTrainerManager manager)
        {
            manager.Spawner.MaxActiveTargets = MaxActiveTargets;
            manager.Spawner.SpawnOne();
        }
    }
}
