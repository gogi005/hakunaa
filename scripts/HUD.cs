using Godot;

namespace AimTrainer
{
    /// <summary>
    /// All in-game UI: live stats, timer, crosshair and the end-of-round panel.
    /// Node references are looked up by name with code-built fallbacks, so this
    /// script keeps working even if the scene tree is edited later.
    /// </summary>
    public partial class HUD : CanvasLayer
    {
        [Export] public Color CrosshairColor = new Color(0.1f, 1f, 0.35f);
        [Export] public int CrosshairLineLength = 10;
        [Export] public int CrosshairLineThickness = 2;
        [Export] public int CrosshairGap = 4;

        private Label _scoreLabel;
        private Label _hitsLabel;
        private Label _missesLabel;
        private Label _accuracyLabel;
        private Label _timeLabel;
        private Control _crosshair;
        private PanelContainer _resultsPanel;
        private Label _resultsLabel;
        private Button _playAgainButton;

        public override void _Ready()
        {
            Layer = 10; // always on top of any future menu layers

            _scoreLabel = FindLabel("StatsBox/ScoreLabel");
            _hitsLabel = FindLabel("StatsBox/HitsLabel");
            _missesLabel = FindLabel("StatsBox/MissesLabel");
            _accuracyLabel = FindLabel("StatsBox/AccuracyLabel");
            _timeLabel = FindLabel("TimerBox/TimeLabel");

            _resultsPanel = GetNodeOrNull<PanelContainer>("ResultsPanel");
            if (_resultsPanel != null)
            {
                _resultsLabel = _resultsPanel.GetNode<Label>("VBox/ResultsLabel");
                _playAgainButton = _resultsPanel.GetNode<Button>("VBox/PlayAgainButton");
                _playAgainButton.Pressed += OnPlayAgainPressed;
            }

            EnsureCrosshair();
        }

        private Label FindLabel(string path)
        {
            Label label = GetNodeOrNull<Label>(path);
            if (label == null)
            {
                // Fallback: build the label from code so the HUD never breaks.
                label = new Label { Name = path.GetFile() };
                GD.Print($"HUD: '{path}' missing from scene, created at runtime.");
            }
            return label;
        }

        private void EnsureCrosshair()
        {
            _crosshair = GetNodeOrNull<Control>("Crosshair");
            if (_crosshair == null)
            {
                // Fallback: full-screen control that draws the crosshair dead center.
                _crosshair = new Control
                {
                    Name = "Crosshair",
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    AnchorMode = Control.LayoutPresetMode.FullRect,
                };
                AddChild(_crosshair);
                _crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPresetMode.FullRect);
            }
            _crosshair.Draw += OnCrosshairDraw;
        }

        /// <summary>Four small lines around a center gap - classic aim-trainer look.</summary>
        private void OnCrosshairDraw()
        {
            Vector2 c = _crosshair.Size / 2f;
            float len = CrosshairLineLength;
            float thick = CrosshairLineThickness;
            float gap = CrosshairGap;

            // Dark outline first for contrast against any background.
            Color outline = new Color(0f, 0f, 0f, 0.85f);
            DrawLines(c, len, thick, gap, outline, 1);
            DrawLines(c, len, thick, gap, CrosshairColor, 0);
        }

        private void DrawLines(Vector2 c, float len, float thick, float gap, Color color, float inflate)
        {
            float t = thick + inflate * 2;
            float g = gap + inflate;
            Vector2 h = new Vector2(len, 0), v = new Vector2(0, len);
            Rect2[] rects =
            {
                new Rect2(c.X + g, c.Y - t / 2f, len, t),          // right
                new Rect2(c.X - g - len, c.Y - t / 2f, len, t),    // left
                new Rect2(c.X - t / 2f, c.Y - g - len, t, len),    // up
                new Rect2(c.X - t / 2f, c.Y + g, t, len),          // down
            };
            foreach (Rect2 r in rects)
                _crosshair.DrawRect(r, color, true);
        }

        // ---------------- Public API used by AimTrainerManager ----------------

        public void ResetStats()
        {
            UpdateStats(0, 0, 0, 100f);
            if (_resultsPanel != null) _resultsPanel.Visible = false;
        }

        public void ShowHud()
        {
            SetHudVisible(true);
            if (_resultsPanel != null) _resultsPanel.Visible = false;
        }

        public void UpdateStats(int score, int hits, int misses, float accuracy)
        {
            _scoreLabel.Text = $"Score: {score}";
            _hitsLabel.Text = $"Hits: {hits}";
            _missesLabel.Text = $"Misses: {misses}";
            _accuracyLabel.Text = $"Accuracy: {accuracy:F0}%";
        }

        public void UpdateTimer(float timeLeft)
        {
            _timeLabel.Text = $"Time: {Mathf.CeilToInt(timeLeft)}";
        }

        public void ShowResults(int score, int hits, int misses,
                               float accuracy, float hitsPerMinute)
        {
            SetHudVisible(false);
            if (_resultsPanel == null) return;

            _resultsLabel.Text =
                "ROUND COMPLETE\n\n" +
                $"Score: {score}\n" +
                $"Hits: {hits}\n" +
                $"Misses: {misses}\n" +
                $"Accuracy: {accuracy:F1}%\n" +
                $"Hits/Minute: {hitsPerMinute:F1}";
            _resultsPanel.Visible = true;
            _playAgainButton.GrabFocus();
        }

        private void OnPlayAgainPressed()
        {
            AimTrainerManager.Instance?.StartRound();
        }

        private void SetHudVisible(bool visible)
        {
            if (_scoreLabel?.GetParent() is Control box) box.Visible = visible;
            if (_timeLabel?.GetParent() is Control tbox) tbox.Visible = visible;
            if (_crosshair != null) _crosshair.Visible = visible;
        }
    }
}
