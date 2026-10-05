using System.Collections.Generic;
using Godot;

namespace AimTrainer
{
    /// <summary>
    /// A single shootable target: Area3D + SphereMesh with a classic
    /// red/white bullseye. Registered on the "Targets" physics layer so the
    /// hitscan raycast can find it without any real physics simulation cost.
    /// </summary>
    public partial class Target : Area3D
    {
        /// <summary>Physics layer 2 (bit value 2) - targets only.</summary>
        public const uint CollisionLayerTargets = 1u << 1;

        [Export] public float Radius = GameSettings.DefaultTargetRadius;
        [Export] public float MoveSpeed = GameSettings.DefaultTargetMoveSpeed; // reserved for moving-target modes

        private Vector3 _moveDirection;   // reserved for future "moving target" modes
        private MeshInstance3D _meshInstance;

        public override void _Ready()
        {
            CollisionLayer = CollisionLayerTargets;
            CollisionMask = 0;          // targets never need to collide with anything
            Monitorable = true;         // required so ray queries can see this area
            Monitoring = false;         // targets don't monitor anything (no physics cost)
            InputPickable = false;      // UI should never intercept shots

            RebuildVisual();
        }

        /// <summary>Creates/refreshes the mesh + collision shape for the current Radius.</summary>
        private void RebuildVisual()
        {
            _meshInstance = GetNodeOrNull<MeshInstance3D>("Visual");
            if (_meshInstance == null)
            {
                // Build the visual programmatically if the scene didn't provide one.
                _meshInstance = new MeshInstance3D { Name = "Visual" };
                AddChild(_meshInstance);
            }
            _meshInstance.Mesh = TargetFactory.CreateBullseyeMesh(Radius);
            _meshInstance.MaterialOverride = TargetFactory.GetBullseyeMaterial();

            CollisionShape3D shape = GetNodeOrNull<CollisionShape3D>("CollisionShape3D");
            if (shape == null)
            {
                shape = new CollisionShape3D { Name = "CollisionShape3D" };
                AddChild(shape);
            }
            shape.Shape = new SphereShape3D { Radius = Radius };
        }

        public override void _PhysicsProcess(double delta)
        {
            // Reserved hook: future moving-target modes will drift the target
            // using _moveDirection and bounce it off the spawn bounds.
            if (MoveSpeed <= 0f) return;

            Vector3 step = _moveDirection * MoveSpeed * (float)delta;
            GlobalPosition += step;
        }

        /// <summary>Called by the spawner when this target is (re)placed.</summary>
        public void PlaceAt(Vector3 position, float newRadius)
        {
            if (newRadius > 0f && !Mathf.IsEqualApprox(newRadius, Radius))
            {
                Radius = newRadius;
                RebuildVisual(); // resize mesh + collision shape
            }
            GlobalPosition = position;
        }

        /// <summary>Random unit direction used by future moving-target modes.</summary>
        public void SetRandomMoveDirection()
        {
            _moveDirection = new Vector3(
                GD.RandRange(-1f, 1f), 0f, GD.RandRange(-1f, 1f)).Normalized();
        }
    }

    /// <summary>
    /// Builds target visuals from Godot primitives only (no external assets).
    /// Bullseye = red sphere + white center disc facing the player.
    /// </summary>
    public static class TargetFactory
    {
        private static Material _bullseyeMat;

        public static SphereMesh CreateBullseyeMesh(float radius)
        {
            return new SphereMesh
            {
                Radius = radius,
                Height = radius * 2f,
                RadialSegments = 24,
                Rings = 12,
                IsFlatShaded = false,
                Material = GetBullseyeMaterial(),
            };
        }

        /// <summary>
        /// Bright unshaded material so targets stay visible in any lighting.
        /// Swap the color here later for "different target colors" support.
        /// </summary>
        public static Material GetBullseyeMaterial()
        {
            if (_bullseyeMat == null)
            {
                StandardMaterial3D mat = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.9f, 0.15f, 0.15f),
                    EmissionEnabled = true,
                    Emission = new Color(0.8f, 0.1f, 0.1f),
                    EmissionEnergyMultiplier = 0.6f,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    VertexColorUseAsAlbedo = false,
                };
                _bullseyeMat = mat;
            }
            return _bullseyeMat;
        }
    }
}
