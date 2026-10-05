using Godot;

namespace AimTrainer
{
    /// <summary>
    /// Game flow controller: runs the round timer, tracks score/hits/misses,
    /// and tells the HUD and spawner what to do. Singleton via Instance.
    /// </summary>
    public partial class AimTrainerManager : Node
    {
        public static AimTrainerManager Instance { get; private set; }

        [Export] public float RoundDuration = GameSettings.DefaultRoundDuration;
        [Export] public int ScorePerHit = GameSettings.HitScore;
        [Export] public int PenaltyPerMiss = GameSettings.MissPenalty;
        [Export] public NodePath SpawnerPath = "World/TargetSpawner";
        [Export] public NodePath HudPath = "Player/HUD";
        [Export] public NodePath PlayerControlPath = "Player/PlayerController";

        public TargetSpawner Spawner { get; private set; }
        public HUD Hud { get; private set; }
        public PlayerController Player { get; private set; }

        public int Score { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public float TimeLeft { get; private set; }
        public bool RoundActive { get; private set; }

        private TrainingMode _mode;
        private float _elapsed;

        public override void _Ready()
        {
            Instance = this;
            Spawner = GetNode<TargetSpawner>(SpawnerPath);
            Hud = GetNode<HUD>(HudPath);
            Player = GetNode<PlayerController>(PlayerControlPath);

            // Future: pick the mode from a menu / profile here.
            _mode = new FlickPracticeMode
            {
                ModeName = "Flick Practice",
                DurationSeconds = RoundDuration,
                MaxActiveTargets = GameSettings.DefaultMaxActiveTargets,
            };

            StartRound();
        }

        public override void _Process(double delta)
        {
            if (!RoundActive) return;

            _elapsed += (float)delta;
            TimeLeft = Mathf.Max(0f, RoundDuration - _elapsed);
            Hud.UpdateTimer(TimeLeft);

            _mode?.OnUpdate(this, delta);

            if (TimeLeft <= 0f)
                EndRound();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey key && key.Pressed && !key.Echo &&
                key.Keycode == Key.R)
            {
                StartRound(); // R = restart round anytime
            }
        }

        public void StartRound()
        {
            Spawner.DespawnAll();

            Score = 0;
            Hits = 0;
            Misses = 0;
            _elapsed = 0f;
            TimeLeft = RoundDuration;
            RoundActive = true;

            Hud.ShowHud();
            Hud.ResetStats();
            Hud.UpdateTimer(TimeLeft);
            Player.CaptureMouse();

            _mode?.OnRoundStart(this);
        }

        public void RegisterHit(Target target)
        {
            if (!RoundActive) return;

            Score += ScorePerHit;
            Hits++;
            Hud.UpdateStats(Score, Hits, Misses, Accuracy);
            _mode?.OnTargetHit(this, target);
        }

        public void RegisterMiss()
        {
            if (!RoundActive) return;

            Misses++;
            Score = Mathf.Max(0, Score - PenaltyPerMiss);
            Hud.UpdateStats(Score, Hits, Misses, Accuracy);
        }

        public float Accuracy
        {
            get
            {
                int shots = Hits + Misses;
                return shots == 0 ? 100f : 100f * Hits / shots;
            }
        }

        public float HitsPerMinute =>
            _elapsed > 0.01f ? Hits * 60f / _elapsed : 0f;

        private void EndRound()
        {
            RoundActive = false;
            Spawner.DespawnAll();
            Player.ReleaseMouse(); // free cursor so the button can be clicked
            Hud.ShowResults(Score, Hits, Misses, Accuracy, HitsPerMinute);
        }
    }
}
