using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BouncingBallsHexagon
{
    public partial class Form1 : Form
    {
        // Constants
        const int WIDTH = 600;
        const int HEIGHT = 600;
        readonly Point CENTER = new Point(WIDTH / 2, HEIGHT / 2);
        const int HEX_RADIUS = 200;
        const int BALL_RADIUS = 10;
        readonly Color HEX_COLOR = Color.Blue;
        readonly Color BG_COLOR = Color.Black;
        const float GRAVITY = 9.8f * 100;
        const float FRICTION = 0.02f;
        const float COR = 0.7f;
        const float ANGULAR_SPEED = 1.5f;

        // Ball collection and random generator
        private List<Ball> balls = new List<Ball>();
        private Random rnd = new Random();

        // Hexagon properties
        private float hexRotation = 0;

        // FPS counter fields
        private int frameCount = 0;
        private float currentFPS = 0;
        private Label fpsLabel;
        private Timer fpsTimer;

        // Number of iterations for ball-to-ball collision resolution per frame
        const int COLLISION_ITERATIONS = 5;

        public Form1()
        {
            // Initialize the form
            InitializeComponent();

            // Set up form properties
            this.DoubleBuffered = true;
            this.ClientSize = new Size(WIDTH, HEIGHT);
            this.Text = "Bouncing Balls in Spinning Hexagon (Click to add balls, button to add, R to remove, X to exit)";
            this.BackColor = BG_COLOR;
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            // Set up game loop timer
            Timer gameTimer = new Timer();
            gameTimer.Interval = 16; // ~60 FPS
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            // Set up FPS timer (updates every second)
            fpsTimer = new Timer();
            fpsTimer.Interval = 1000;
            fpsTimer.Tick += FpsTimer_Tick;
            fpsTimer.Start();

            // FPS Label setup (top-right corner)
            fpsLabel = new Label();
            fpsLabel.AutoSize = true;
            fpsLabel.ForeColor = Color.White;
            fpsLabel.BackColor = Color.Transparent;
            fpsLabel.Location = new Point(WIDTH - 80, 10);
            fpsLabel.Text = "FPS: 0";
            this.Controls.Add(fpsLabel);

            // Add click handler for creating new balls on mouse click.
            this.MouseClick += (sender, e) =>
            {
                Color randomColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                balls.Add(new Ball(new PointF(e.X, e.Y), randomColor));
            };

            // Add a Button control to create a new ball at the center of the hexagon.
            Button addBallButton = new Button();
            addBallButton.Text = "Add Ball";
            addBallButton.Size = new Size(80, 30);
            addBallButton.Location = new Point(10, 10);
            addBallButton.Click += (sender, e) =>
            {
                // Create a new ball at the center with a random color.
                Color randomColor = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                balls.Add(new Ball(new PointF(CENTER.X, CENTER.Y), randomColor));
            };
            this.Controls.Add(addBallButton);
        }

        // Key event handler for removing balls and exiting the application.
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.R)
            {
                // Remove the last ball in the list if any exist.
                if (balls.Count > 0)
                {
                    balls.RemoveAt(balls.Count - 1);
                }
            }
            else if (e.KeyCode == Keys.X)
            {
                // Exit the application.
                Application.Exit();
            }
        }

        // The Ball class representing each ball.
        private class Ball
        {
            public PointF Position { get; set; }
            public PointF Velocity { get; set; }
            public Color Color { get; set; }

            public Ball(PointF position, Color color)
            {
                Position = position;
                Velocity = new PointF(0, 0);
                Color = color;
            }
        }

        // Compute the hexagon vertices based on the given radius and rotation.
        private PointF[] GetHexVertices(float radius, float rotation)
        {
            PointF[] vertices = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)(i * 60 * Math.PI / 180) + rotation;
                vertices[i] = new PointF(
                    CENTER.X + radius * (float)Math.Cos(angle),
                    CENTER.Y + radius * (float)Math.Sin(angle)
                );
            }
            return vertices;
        }

        // Find the closest point on a line segment AB to point P.
        private PointF ClosestPointOnSegment(PointF A, PointF B, PointF P)
        {
            PointF AP = new PointF(P.X - A.X, P.Y - A.Y);
            PointF AB = new PointF(B.X - A.X, B.Y - A.Y);

            float t = (AP.X * AB.X + AP.Y * AB.Y) / (AB.X * AB.X + AB.Y * AB.Y + 1e-8f);
            t = Math.Max(0, Math.Min(1, t));

            return new PointF(A.X + t * AB.X, A.Y + t * AB.Y);
        }

        // Main game loop: update physics and redraw.
        private void GameLoop(object sender, EventArgs e)
        {
            float deltaTime = 16 / 1000f;
            hexRotation += ANGULAR_SPEED * deltaTime;
            PointF[] hexVertices = GetHexVertices(HEX_RADIUS, hexRotation);

            // Process each ball's physics and hexagon collisions.
            foreach (var ball in balls)
            {
                // Apply friction and gravity.
                ball.Velocity = new PointF(
                    ball.Velocity.X * (1 - FRICTION * deltaTime),
                    ball.Velocity.Y + GRAVITY * deltaTime
                );

                // Update position.
                ball.Position = new PointF(
                    ball.Position.X + ball.Velocity.X * deltaTime,
                    ball.Position.Y + ball.Velocity.Y * deltaTime
                );

                // Collision detection and response with hexagon edges.
                for (int i = 0; i < 6; i++)
                {
                    PointF A = hexVertices[i];
                    PointF B = hexVertices[(i + 1) % 6];
                    PointF P = ClosestPointOnSegment(A, B, ball.Position);

                    float dx = ball.Position.X - P.X;
                    float dy = ball.Position.Y - P.Y;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                    if (distance < BALL_RADIUS)
                    {
                        // Normal: from the edge's midpoint to the hexagon center.
                        PointF M = new PointF((A.X + B.X) / 2, (A.Y + B.Y) / 2);
                        PointF normal = new PointF(CENTER.X - M.X, CENTER.Y - M.Y);
                        float length = (float)Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
                        normal.X /= length;
                        normal.Y /= length;

                        // Wall's velocity due to rotation.
                        float pxRel = P.X - CENTER.X;
                        float pyRel = P.Y - CENTER.Y;
                        PointF vWall = new PointF(-ANGULAR_SPEED * pyRel, ANGULAR_SPEED * pxRel);

                        // Relative velocity with respect to the wall.
                        PointF relVel = new PointF(ball.Velocity.X - vWall.X, ball.Velocity.Y - vWall.Y);
                        float dot = relVel.X * normal.X + relVel.Y * normal.Y;

                        if (dot < 0)
                        {
                            float impulse = -(1 + COR) * dot;
                            relVel = new PointF(
                                relVel.X + impulse * normal.X,
                                relVel.Y + impulse * normal.Y
                            );

                            ball.Velocity = new PointF(relVel.X + vWall.X, relVel.Y + vWall.Y);

                            // Positional correction to remove penetration.
                            float overlap = BALL_RADIUS - distance;
                            ball.Position = new PointF(
                                ball.Position.X + normal.X * overlap,
                                ball.Position.Y + normal.Y * overlap
                            );
                        }
                        break;
                    }
                }
            }

            // --- Ball-to-Ball Collision Handling ---
            // Run multiple iterations to reduce deep penetrations and sticking.
            for (int iter = 0; iter < COLLISION_ITERATIONS; iter++)
            {
                for (int i = 0; i < balls.Count; i++)
                {
                    for (int j = i + 1; j < balls.Count; j++)
                    {
                        Ball ballA = balls[i];
                        Ball ballB = balls[j];

                        float dx = ballB.Position.X - ballA.Position.X;
                        float dy = ballB.Position.Y - ballA.Position.Y;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                        float minDist = BALL_RADIUS * 2;

                        // Check if the balls are overlapping.
                        if (distance < minDist && distance > 0)
                        {
                            // Normalized collision normal.
                            float nx = dx / distance;
                            float ny = dy / distance;

                            // Calculate the overlap.
                            float overlap = minDist - distance;

                            // Relative velocity along the normal.
                            float rvx = ballA.Velocity.X - ballB.Velocity.X;
                            float rvy = ballA.Velocity.Y - ballB.Velocity.Y;
                            float dot = rvx * nx + rvy * ny;

                            // Resolve only if the balls are moving toward each other.
                            if (dot < 0)
                            {
                                // Impulse scalar (assuming equal mass).
                                float impulse = -(1 + COR) * dot / 2;
                                ballA.Velocity = new PointF(
                                    ballA.Velocity.X + impulse * nx,
                                    ballA.Velocity.Y + impulse * ny
                                );
                                ballB.Velocity = new PointF(
                                    ballB.Velocity.X - impulse * nx,
                                    ballB.Velocity.Y - impulse * ny
                                );
                            }

                            // Separate the balls proportionally.
                            ballA.Position = new PointF(
                                ballA.Position.X - nx * overlap / 2,
                                ballA.Position.Y - ny * overlap / 2
                            );
                            ballB.Position = new PointF(
                                ballB.Position.X + nx * overlap / 2,
                                ballB.Position.Y + ny * overlap / 2
                            );
                        }
                    }
                }
            }

            // Increment frame counter for FPS calculation.
            frameCount++;
            this.Invalidate();
        }

        // FPS timer tick: update the FPS label once per second.
        private void FpsTimer_Tick(object sender, EventArgs e)
        {
            currentFPS = frameCount;
            fpsLabel.Text = $"FPS: {currentFPS}";
            frameCount = 0;
        }

        // Drawing routine.
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw hexagon.
            g.DrawPolygon(new Pen(HEX_COLOR, 2), GetHexVertices(HEX_RADIUS, hexRotation));

            // Draw all balls.
            foreach (var ball in balls)
            {
                using (SolidBrush brush = new SolidBrush(ball.Color))
                {
                    g.FillEllipse(brush,
                        ball.Position.X - BALL_RADIUS,
                        ball.Position.Y - BALL_RADIUS,
                        BALL_RADIUS * 2,
                        BALL_RADIUS * 2);
                }
            }
        }
    }
}
