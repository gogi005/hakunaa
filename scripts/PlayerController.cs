using Godot;

namespace AimTrainer
{
    /// <summary>
    /// First-person controller: mouse look + WASD movement + hitscan shooting.
    /// The parent node must be a CharacterBody3D; this script rotates its two
    /// children: YawNode (turn left/right) and CameraPivot/Camera3D (look up/down).
    /// </summary>
    public partial class PlayerController : Node
    {
        [Export] public float MouseSensitivity = GameSettings.DefaultMouseSensitivity;
        [Export] public float MoveSpeed = GameSettings.DefaultMoveSpeed;
        [Export] public NodePath YawNodePath = "YawNode";
        [Export] public NodePath CameraPivotPath = "YawNode/CameraPivot";
        [Export] public NodePath ShootAudioPath = "YawNode/CameraPivot/ShootAudio";
        [Export] public PackedScene BulletTracerScene = null; // optional, leave null for now

        private CharacterBody3D _body;
        private Node3D _yawNode;
        private Node3D _cameraPivot;
        private Camera3D _camera;
        private AudioStreamPlayer _shootAudio;

        private bool _mouseCaptured = true;

        public override void _Ready()
        {
            _body = GetParent<CharacterBody3D>();
            _yawNode = GetNode<Node3D>(YawNodePath);
            _cameraPivot = GetNode<Node3D>(CameraPivotPath);
            _camera = _cameraPivot.GetNode<Camera3D>("Camera3D");
            _shootAudio = GetNodeOrNull<AudioStreamPlayer>(ShootAudioPath);

            CaptureMouse();
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseMotion motion && _mouseCaptured)
            {
                // Horizontal -> yaw (player), vertical -> pitch (camera only).
                _yawNode.RotateY(-motion.Relative.X * MouseSensitivity);

                float pitch = _cameraPivot.Rotation.X - motion.Relative.Y * MouseSensitivity;
                pitch = Mathf.Clamp(
                    pitch,
                    Mathf.DegToRad(GameSettings.MinPitchDeg),
                    Mathf.DegToRad(GameSettings.MaxPitchDeg));
                _cameraPivot.Rotation = new Vector3(pitch, 0f, 0f);
            }
            else if (@event is InputEventMouseButton mb && mb.Pressed)
            {
                switch (mb.ButtonIndex)
                {
                    case MouseButton.Left:
                        if (_mouseCaptured)
                            Shoot();
                        else
                            CaptureMouse(); // click in window re-grabs the cursor
                        break;
                    case MouseButton.Right:
                        GD.Print("Right click not used in this prototype.");
                        break;
                }
            }
            else if (@event is InputEventKey key && key.Pressed && !key.Echo)
            {
                if (key.Keycode == Key.Escape)
                    ToggleMouseCapture();
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            // Simple planar WASD movement (no jumping/gravity needed in a practice room).
            Vector2 inputDir = Vector2.Zero;
            if (Input.IsActionPressed("move_forward")) inputDir.y -= 1f;
            if (Input.IsActionPressed("move_back")) inputDir.y += 1f;
            if (Input.IsActionPressed("move_left")) inputDir.x -= 1f;
            if (Input.IsActionPressed("move_right")) inputDir.x += 1f;

            Vector3 velocity = _body.Velocity;
            if (inputDir != Vector2.Zero)
            {
                Vector3 dir = new Vector3(inputDir.X, 0f, inputDir.Y);
                dir = _yawNode.GlobalTransform.Basis * dir; // relative to where we look
                dir.y = 0f;
                dir = dir.Normalized();
                velocity.x = dir.X * MoveSpeed;
                velocity.z = dir.Z * MoveSpeed;
            }
            else
            {
                velocity.x = Mathf.Lerp(_body.Velocity.x, 0f, 0.2f);
                velocity.z = Mathf.Lerp(_body.Velocity.z, 0f, 0.2f);
            }
            velocity.y = _body.Velocity.Y; // keep gravity result untouched

            _body.Velocity = velocity;
            _body.MoveAndSlide();
        }

        /// <summary>
        /// Hitscan shot: raycast straight out of the camera center.
        /// Only the "Targets" physics layer is queried, so walls simply count as misses.
        /// </summary>
        public void Shoot()
        {
            PlayShootSound();
            SpawnTracer();

            Vector3 from = _camera.GlobalPosition;
            Vector3 to = from - _camera.GlobalTransform.Basis.Z * 200f;

            Query query = new Query();
            query.CollisionMask = Target.CollisionLayerTargets; // layer 2 only
            query.CollideWithAreas = true;                      // targets use Area3D
            query.CollideWithBodies = true;
            query.Exclude = new System.Collections.Generic.List<Rid> { _camera.GetRid() };

            PhysicsDirectSpaceState3D space =
                GetViewport().World3D.DirectSpaceState; // Godot 4.x API
            Dictionary hit = space.IntersectRayQuery(query);

            AimTrainerManager manager = AimTrainerManager.Instance;
            if (manager == null) return;

            if (hit.Count > 0 &&
                hit["collider"].As<GodotObject>() is Target target)
            {
                manager.RegisterHit(target);
            }
            else
            {
                manager.RegisterMiss();
            }
        }

        private void PlayShootSound()
        {
            if (_shootAudio == null) return;

            if (_shootAudio.Stream == null)
            {
                // Placeholder: synthesize a short "pop" at runtime so no audio
                // asset files are required. Replace by dropping your own WAV into
                // res://audio/ and assigning it to the ShootAudio node.
                _shootAudio.Stream = SoundGenerator.CreatePopSound();
            }

            _shootAudio.Stop();
            _shootAudio.Play();
        }

        private void SpawnTracer()
        {
            // Hook point for future tracer/particle/muzzle-flash effects.
            if (BulletTracerScene == null) return;
            Node3D tracer = BulletTracerScene.Instantiate<Node3D>();
            _cameraPivot.AddChild(tracer);
        }

        private void ToggleMouseCapture()
        {
            if (_mouseCaptured) ReleaseMouse();
            else CaptureMouse();
        }

        public void CaptureMouse()
        {
            _mouseCaptured = true;
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        public void ReleaseMouse()
        {
            _mouseCaptured = false;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }

        public bool IsMouseCaptured => _mouseCaptured;
    }
}
