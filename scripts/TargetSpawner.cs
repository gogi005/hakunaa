using System.Collections.Generic;
using Godot;

namespace AimTrainer
{
    /// <summary>
    /// Owns the pool of Target instances and decides WHERE they appear.
    /// Spawn positions are generated relative to the player's facing direction
    /// so targets never appear behind the player or too close to the camera.
    /// </summary>
    public partial class TargetSpawner : Node3D
    {
        [Export] public PackedScene TargetScene;
        [Export] public NodePath PlayerPath = "../Player";
        [Export] public float TargetRadius = GameSettings.DefaultTargetRadius;
        [Export] public float MinDistance = GameSettings.DefaultMinSpawnDistance;
        [Export] public float MaxDistance = GameSettings.DefaultMaxSpawnDistance;
        [Export] public int MaxActiveTargets = GameSettings.DefaultMaxActiveTargets;
        [Export] public int PoolSize = 10; // pre-warmed target pool (no runtime allocation)

        private Node3D _playerYaw;   // yaw pivot: its -Z is "in front of the player"
        private readonly List<Target> _active = new List<Target>();
        private readonly Stack<Target> _pool = new Stack<Target>();

        public override void _Ready()
        {
            Node player = GetNodeOrNull(_playerPath);
            if (player != null)
                _playerYaw = player.GetNode<Node3D>("YawNode");

            // Pre-warm a small pool so we never allocate during a round.
            int poolSize = PoolSize;
            for (int i = 0; i < poolSize; i++)
                _pool.Add(CreateTarget());
        }

        /// <summary>Spawn one target in front of the player. Returns it (or null).</summary>
        public Target SpawnOne()
        {
            if (_active.Count >= MaxActiveTargets) return null;

            Target t = _pool.Count > 0 ? _pool.Pop() : CreateTarget();
            t.Visible = true;
            t.PlaceAt(GetRandomSpawnPosition(), TargetRadius);
            t.SetRandomMoveDirection(); // used only by future moving-target modes
            _active.Add(t);
            return t;
        }

        /// <summary>Called by the manager when a target is hit.</summary>
        public void Despawn(Target t)
        {
            if (!_active.Remove(t)) return;
            t.Visible = false;
            t.Reparent(this, keepGlobalTransform: false); // park in spawner
            _pool.Push(t);
        }

        public void DespawnAll()
        {
            foreach (Target t in _active)
            {
                t.Visible = false;
                t.Reparent(this, keepGlobalTransform: false);
                _pool.Push(t);
            }
            _active.Clear();
        }

        public IReadOnlyList<Target> ActiveTargets => _active;

        /// <summary>
        /// Random position inside a forward-facing arc:
        /// +-45 deg yaw, +-20 deg pitch from where the player looks,
        /// distance clamped between MinDistance and MaxDistance.
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 origin = _playerYaw != null ? _playerYaw.GlobalPosition : GlobalPosition;

            float yawDeg = GD.RandRange(-45f, 45f);
            float pitchDeg = GD.RandRange(-20f, 20f);
            float distance = GD.RandRange(MinDistance, MaxDistance);

            Basis basis = Basis.FromEuler(new Vector3(
                Mathf.DegToRad(pitchDeg), Mathf.DegToRad(yawDeg), 0f));
            Vector3 dir = basis * Vector3.Forward; // rotated forward vector

            Vector3 pos = origin + dir * distance;

            // Keep targets inside sane room bounds and above the floor.
            pos.y = Mathf.Clamp(pos.y, TargetRadius + 0.2f, 6f);
            pos.x = Mathf.Clamp(pos.x, -13f, 13f);
            pos.z = Mathf.Clamp(pos.z, -13f, 13f);
            return pos;
        }

        private Target CreateTarget()
        {
            Target t;
            if (TargetScene != null)
            {
                t = TargetScene.Instantiate<Target>();
            }
            else
            {
                // Fallback: build the whole target from code, no scene needed.
                // The Target script creates its own mesh + collision in _Ready().
                t = new Target { Name = "Target" };
                t.SetScript(GD.Load<Script>("res://scripts/Target.cs"));
                GD.Print("TargetSpawner: no TargetScene assigned, using code-built target.");
            }
            AddChild(t);
            t.Visible = false;
            return t;
        }
    }
}
