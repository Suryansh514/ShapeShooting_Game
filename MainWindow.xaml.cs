using ShapeShootingGame.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media; // Crucial for SoundPlayer
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ShapeShootingGame
{
    public partial class MainWindow : Window
    {
        GameModel gameModel = new GameModel();
        DispatcherTimer gameTimer = new DispatcherTimer();

        double x1, y1, x2, y2;
        Ellipse center;
        bool isDragging = false;

        string currentShapeMode = "Circle";

        // Gun control variables
        double gunAngle = -90;
        double gunLength = 40;

        // Cooldown timers for automatic fire holding
        int rifleCooldown = 0;
        int machineGunCooldown = 0;

        // Native Windows Sound Players (Solves silent playback bugs)
        private SoundPlayer shootSound = new SoundPlayer();
        private SoundPlayer explodeSound = new SoundPlayer();
        private SoundPlayer uiSound = new SoundPlayer();

        public MainWindow()
        {
            InitializeComponent();

            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameTimer_Tick;

            // Load absolute sound mappings into memory upfront
            InitializeAudio();

            this.Focus();
        }
        private void InitializeAudio()
        {
            try
            {
               
                shootSound.SoundLocation = @"C:\Users\surya\OneDrive\Documents\Net_Technologies_Using_C#\ShapeShootingGame -- Part 3\ShapeShootingGame\ShapeShootingGame\Sounds\Bullet.wav";
                explodeSound.SoundLocation = @"C:\Users\surya\OneDrive\Documents\Net_Technologies_Using_C#\ShapeShootingGame -- Part 3\ShapeShootingGame\ShapeShootingGame\Sounds\Explosion.wav";
                uiSound.SoundLocation = @"C:\Users\surya\OneDrive\Documents\Net_Technologies_Using_C#\ShapeShootingGame -- Part 3\ShapeShootingGame\ShapeShootingGame\Sounds\Switch.wav";

                shootSound.Load();
                explodeSound.Load();
                uiSound.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"System Audio Test Failed: {ex.Message}");
            }
        }

        private void PlaySound(SoundPlayer player)
        {
            try
            {
                // 1. Try playing your file
                player.Play();
            }
            catch
            {
                // 2. Hardware Fallback: Makes a system motherboard beep if audio engine fails
                Console.Beep(800, 100);
            }
        }

        private void ShapeBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            currentShapeMode = btn.Content.ToString();

            // Play click audio safely without a MessageBox blocking the UI thread
            PlaySound(uiSound);
        }

        private void ShapesCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Focus();

            x1 = e.GetPosition(ShapesCanvas).X;
            y1 = e.GetPosition(ShapesCanvas).Y;

            DrawCenter();
            isDragging = true;
        }

        private void ShapesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private void ShapesCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging) return;
            isDragging = false;

            x2 = e.GetPosition(ShapesCanvas).X;
            y2 = e.GetPosition(ShapesCanvas).Y;

            double width = Math.Abs(x2 - x1);
            double height = Math.Abs(y2 - y1);

            if (width < 2) width = 40;
            if (height < 2) height = 40;

            if (currentShapeMode == "Circle")
            {
                double r = Math.Sqrt(width * width + height * height);
                Circle c = new Circle(x1, y1, r);
                gameModel.Circles.Add(c);
            }
            else if (currentShapeMode == "Rectangle")
            {
                double topLeftX = Math.Min(x1, x2);
                double topLeftY = Math.Min(y1, y2);

                Models.Rectangle rect = new Models.Rectangle(topLeftX, topLeftY, width, height);
                gameModel.Rectangles.Add(rect);
            }
            else if (currentShapeMode == "Triangle")
            {
                double topLeftX = Math.Min(x1, x2);
                double topLeftY = Math.Min(y1, y2);

                Triangle tri = new Triangle(topLeftX, topLeftY, width, height);
                gameModel.Triangles.Add(tri);
            }

            Render();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Forces keyboard focus to stay on the game window
            this.Focus();
            Keyboard.Focus(this);

            // STEER COUNTER-CLOCKWISE (Turn Left)
            if (e.Key == Key.Left || e.Key == Key.A)
            {
                gunAngle -= 5;
                Render();
            }
            // STEER CLOCKWISE (Turn Right)
            else if (e.Key == Key.Right || e.Key == Key.D)
            {
                gunAngle += 5;
                Render();
            }
            // FIRE WEAPON
            else if (e.Key == Key.Space)
            {
                if (!e.IsRepeat)
                {
                    FireBullet();
                    rifleCooldown = 0;
                    machineGunCooldown = 0;
                }

                // Stops the Spacebar from accidentally triggering/clicking your UI buttons
                e.Handled = true;
            }
        }

        private void ModeBtn_Click(object sender, RoutedEventArgs e)
        {
            gameModel.WeaponMode = gameModel.WeaponMode == "Rifle" ? "Machine Gun" : "Rifle";
            ModeBtn.Content = $"Weapon: {gameModel.WeaponMode}";

            // Instantly plays weapon change sound
            PlaySound(uiSound);
        }

        private void FireBullet()
        {
            double canvasW = ShapesCanvas.ActualWidth > 0 ? ShapesCanvas.ActualWidth : 1000;
            double canvasH = ShapesCanvas.ActualHeight > 0 ? ShapesCanvas.ActualHeight : 550;

            double gunStartX = canvasW / 2;
            double gunStartY = canvasH - 20;
            double radians = gunAngle * Math.PI / 180.0;

            Bullet b = new Bullet();
            b.X = gunStartX + Math.Cos(radians) * gunLength;
            b.Y = gunStartY + Math.Sin(radians) * gunLength;

            double speed = gameModel.WeaponMode == "Machine Gun" ? 12 : 8;
            b.DX = Math.Cos(radians) * speed;
            b.DY = Math.Sin(radians) * speed;

            gameModel.Bullets.Add(b);

            // Play firing audio track
            PlaySound(shootSound);
        }

        private void Render()
        {
            ShapesCanvas.Children.Clear();

            // 1. Render Circles
            foreach (var c in gameModel.Circles)
            {
                Ellipse ellipse = new Ellipse { Width = c.Radius * 2, Height = c.Radius * 2, Stroke = Brushes.Red };
                Canvas.SetLeft(ellipse, c.X - c.Radius);
                Canvas.SetTop(ellipse, c.Y - c.Radius);
                ShapesCanvas.Children.Add(ellipse);
            }

            // 2. Render Rectangles
            foreach (var r in gameModel.Rectangles)
            {
                System.Windows.Shapes.Rectangle rectVisual = new System.Windows.Shapes.Rectangle { Width = r.Width, Height = r.Height, Stroke = Brushes.Blue };
                Canvas.SetLeft(rectVisual, r.X);
                Canvas.SetTop(rectVisual, r.Y);
                ShapesCanvas.Children.Add(rectVisual);
            }

            // 3. Render Triangles
            foreach (var t in gameModel.Triangles)
            {
                Polygon triangleVisual = new Polygon { Stroke = Brushes.Green };
                triangleVisual.Points.Add(new Point(t.X + (t.Width / 2), t.Y));
                triangleVisual.Points.Add(new Point(t.X, t.Y + t.Height));
                triangleVisual.Points.Add(new Point(t.X + t.Width, t.Y + t.Height));
                ShapesCanvas.Children.Add(triangleVisual);
            }

            // 4. Render Aiming Gun Line
            double canvasW = ShapesCanvas.ActualWidth > 0 ? ShapesCanvas.ActualWidth : 1000;
            double canvasH = ShapesCanvas.ActualHeight > 0 ? ShapesCanvas.ActualHeight : 550;
            double gunStartX = canvasW / 2;
            double gunStartY = canvasH - 20;
            double rads = gunAngle * Math.PI / 180.0;

            Line gunLine = new Line
            {
                X1 = gunStartX,
                Y1 = gunStartY,
                X2 = gunStartX + Math.Cos(rads) * gunLength,
                Y2 = gunStartY + Math.Sin(rads) * gunLength,
                Stroke = Brushes.LightGreen,
                StrokeThickness = 6
            };
            ShapesCanvas.Children.Add(gunLine);

            // 5. Render Bullets
            foreach (var b in gameModel.Bullets)
            {
                Ellipse bulletVisual = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Yellow };
                Canvas.SetLeft(bulletVisual, b.X - 3);
                Canvas.SetTop(bulletVisual, b.Y - 3);
                ShapesCanvas.Children.Add(bulletVisual);
            }

            // 6. Render Explosions
            foreach (var exp in gameModel.Explosions)
            {
                Ellipse expVisual = new Ellipse
                {
                    Width = exp.Radius * 2,
                    Height = exp.Radius * 2,
                    Stroke = Brushes.Orange,
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)(exp.Life * 15), 255, 69, 0))
                };
                Canvas.SetLeft(expVisual, exp.X - exp.Radius);
                Canvas.SetTop(expVisual, exp.Y - exp.Radius);
                ShapesCanvas.Children.Add(expVisual);
            }
        }

        private void GameOver()
        {
            gameTimer.Stop(); // Freezes game loop entirely 

            // Animate a large base crash
            gameModel.Explosions.Add(new Explosion
            {
                X = ShapesCanvas.ActualWidth / 2,
                Y = ShapesCanvas.ActualHeight - 20,
                Radius = 60
            });

            Render();
            PlaySound(explodeSound);

            MessageBox.Show("Game Over! A shape destroyed your gun.", "Defeat", MessageBoxButton.OK);
        }

        private void DrawCenter()
        {
            center = new Ellipse { Width = 4, Height = 4, Stroke = Brushes.Red };
            Canvas.SetLeft(center, x1 - 2);
            Canvas.SetTop(center, y1 - 2);
            ShapesCanvas.Children.Add(center);
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            // Continuously handles key holds directly inside loop
            if (Keyboard.IsKeyDown(Key.Space))
            {
                if (gameModel.WeaponMode == "Machine Gun")
                {
                    machineGunCooldown++;
                    if (machineGunCooldown >= 4)
                    {
                        FireBullet();
                        machineGunCooldown = 0;
                    }
                }
                else if (gameModel.WeaponMode == "Rifle")
                {
                    rifleCooldown++;
                    if (rifleCooldown >= 14)
                    {
                        FireBullet();
                        rifleCooldown = 0;
                    }
                }
            }

            UpdateEnginePhysics();
        }

        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateEnginePhysics();
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            gameTimer.Start();
            PlaySound(uiSound);
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            gameTimer.Stop();
            PlaySound(uiSound);
        }

        private void UpdateEnginePhysics()
        {
            // --- 1. CIRCLE WALL BOUNCES (Pure Physics) ---
            for (int i = gameModel.Circles.Count - 1; i >= 0; i--)
            {
                var c = gameModel.Circles[i];
                c.X += c.XDirection * 5;
                c.Y += c.YDirection * 5;

                if (c.X > ShapesCanvas.ActualWidth - c.Radius || c.X < c.Radius) { c.XDirection = -c.XDirection; }
                if (c.Y > ShapesCanvas.ActualHeight - c.Radius || c.Y < c.Radius) { c.YDirection = -c.YDirection; }
            }

            // --- 2. RECTANGLE WALL BOUNCES (Pure Physics) ---
            for (int i = gameModel.Rectangles.Count - 1; i >= 0; i--)
            {
                var r = gameModel.Rectangles[i];
                r.X += r.XDirection * 5;
                r.Y += r.YDirection * 5;

                if (r.X > ShapesCanvas.ActualWidth - r.Width || r.X < 0) { r.XDirection = -r.XDirection; }
                if (r.Y > ShapesCanvas.ActualHeight - r.Height || r.Y < 0) { r.YDirection = -r.YDirection; }
            }

            // --- 3. TRIANGLE WALL BOUNCES (Pure Physics) ---
            for (int i = gameModel.Triangles.Count - 1; i >= 0; i--)
            {
                var t = gameModel.Triangles[i];
                t.X += t.XDirection * 5;
                t.Y += t.YDirection * 5;

                if (t.X > ShapesCanvas.ActualWidth - t.Width || t.X < 0) { t.XDirection = -t.XDirection; }
                if (t.Y > ShapesCanvas.ActualHeight - t.Height || t.Y < 0) { t.YDirection = -t.YDirection; }
            }

            // --- 4. SHAPE VS SHAPE COLLISIONS (Pure Physics) ---
            var circleList = gameModel.Circles;
            var rectList = gameModel.Rectangles;
            var triList = gameModel.Triangles;

            // Circle vs Circle
            for (int i = 0; i < circleList.Count; i++)
            {
                for (int j = i + 1; j < circleList.Count; j++)
                {
                    Rect r1 = new Rect(circleList[i].X - circleList[i].Radius, circleList[i].Y - circleList[i].Radius, circleList[i].Radius * 2, circleList[i].Radius * 2);
                    Rect r2 = new Rect(circleList[j].X - circleList[j].Radius, circleList[j].Y - circleList[j].Radius, circleList[j].Radius * 2, circleList[j].Radius * 2);
                    if (r1.IntersectsWith(r2))
                    {
                        circleList[i].XDirection = -circleList[i].XDirection; circleList[i].YDirection = -circleList[i].YDirection;
                        circleList[j].XDirection = -circleList[j].XDirection; circleList[j].YDirection = -circleList[j].YDirection;
                        PlaySound(explodeSound);
                    }
                }
            }

            // Rect vs Rect
            for (int i = 0; i < rectList.Count; i++)
            {
                for (int j = i + 1; j < rectList.Count; j++)
                {
                    Rect r1 = new Rect(rectList[i].X, rectList[i].Y, rectList[i].Width, rectList[i].Height);
                    Rect r2 = new Rect(rectList[j].X, rectList[j].Y, rectList[j].Width, rectList[j].Height);
                    if (r1.IntersectsWith(r2))
                    {
                        rectList[i].XDirection = -rectList[i].XDirection; rectList[i].YDirection = -rectList[i].YDirection;
                        rectList[j].XDirection = -rectList[j].XDirection; rectList[j].YDirection = -rectList[j].YDirection;
                        PlaySound(explodeSound);
                    }
                }
            }

            // Tri vs Tri
            for (int i = 0; i < triList.Count; i++)
            {
                for (int j = i + 1; j < triList.Count; j++)
                {
                    Rect r1 = new Rect(triList[i].X, triList[i].Y, triList[i].Width, triList[i].Height);
                    Rect r2 = new Rect(triList[j].X, triList[j].Y, triList[j].Width, triList[j].Height);
                    if (r1.IntersectsWith(r2))
                    {
                        triList[i].XDirection = -triList[i].XDirection; triList[i].YDirection = -triList[i].YDirection;
                        triList[j].XDirection = -triList[j].XDirection; triList[j].YDirection = -triList[j].YDirection;
                        PlaySound(explodeSound);
                    }
                }
            }

            // Circle vs Rect
            for (int i = 0; i < circleList.Count; i++)
            {
                for (int j = 0; j < rectList.Count; j++)
                {
                    Rect r1 = new Rect(circleList[i].X - circleList[i].Radius, circleList[i].Y - circleList[i].Radius, circleList[i].Radius * 2, circleList[i].Radius * 2);
                    Rect r2 = new Rect(rectList[j].X, rectList[j].Y, rectList[j].Width, rectList[j].Height);
                    if (r1.IntersectsWith(r2))
                    {
                        circleList[i].XDirection = -circleList[i].XDirection; circleList[i].YDirection = -circleList[i].YDirection;
                        rectList[j].XDirection = -rectList[j].XDirection; rectList[j].YDirection = -rectList[j].YDirection;
                        PlaySound(explodeSound);
                    }
                }
            }

            // Circle vs Tri
            for (int i = 0; i < circleList.Count; i++)
            {
                for (int j = 0; j < triList.Count; j++)
                {
                    Rect r1 = new Rect(circleList[i].X - circleList[i].Radius, circleList[i].Y - circleList[i].Radius, circleList[i].Radius * 2, circleList[i].Radius * 2);
                    Rect r2 = new Rect(triList[j].X, triList[j].Y, triList[j].Width, triList[j].Height);
                    if (r1.IntersectsWith(r2))
                    {
                        circleList[i].XDirection = -circleList[i].XDirection; circleList[i].YDirection = -circleList[i].YDirection;
                        triList[j].XDirection = -triList[j].XDirection; triList[j].YDirection = -triList[j].YDirection;
                        PlaySound(explodeSound);
                    }
                }
            }

            // Rect vs Tri
            for (int i = 0; i < rectList.Count; i++)
            {
                for (int j = 0; j < triList.Count; j++)
                {
                    Rect r1 = new Rect(rectList[i].X, rectList[i].Y, rectList[i].Width, rectList[i].Height);
                    Rect r2 = new Rect(triList[j].X, triList[j].Y, triList[j].Width, triList[j].Height);
                    if (r1.IntersectsWith(r2))
                    {
                        rectList[i].XDirection = -rectList[i].XDirection; rectList[i].YDirection = -rectList[i].YDirection;
                        triList[j].XDirection = -triList[j].XDirection; triList[j].YDirection = -triList[j].YDirection;
                        PlaySound(explodeSound);
                    }
                }
            }

            // --- 5. MOVE BULLETS ---
            for (int i = gameModel.Bullets.Count - 1; i >= 0; i--)
            {
                var b = gameModel.Bullets[i];
                b.X += b.DX;
                b.Y += b.DY;

                if (b.X < 0 || b.X > ShapesCanvas.ActualWidth || b.Y < 0 || b.Y > ShapesCanvas.ActualHeight)
                {
                    gameModel.Bullets.RemoveAt(i);
                }
            }

            // --- 6. BULLET VS SHAPE IMPACTS (5 Direct Bullet Hits To Explode) ---
            // Bullet vs Circle
            for (int i = gameModel.Circles.Count - 1; i >= 0; i--)
            {
                var c = gameModel.Circles[i];
                Rect cRect = new Rect(c.X - c.Radius, c.Y - c.Radius, c.Radius * 2, c.Radius * 2);
                for (int j = gameModel.Bullets.Count - 1; j >= 0; j--)
                {
                    var b = gameModel.Bullets[j];
                    if (cRect.Contains(new Point(b.X, b.Y)))
                    {
                        c.HitCount++;
                        gameModel.Bullets.RemoveAt(j);

                        if (c.HitCount >= 5)
                        {
                            gameModel.Explosions.Add(new Explosion { X = c.X, Y = c.Y, Radius = c.Radius / 2 });
                            PlaySound(explodeSound);
                            gameModel.Circles.RemoveAt(i);
                        }
                        break;
                    }
                }
            }

            // Bullet vs Rectangle
            for (int i = gameModel.Rectangles.Count - 1; i >= 0; i--)
            {
                var r = gameModel.Rectangles[i];
                Rect rRect = new Rect(r.X, r.Y, r.Width, r.Height);
                for (int j = gameModel.Bullets.Count - 1; j >= 0; j--)
                {
                    var b = gameModel.Bullets[j];
                    if (rRect.Contains(new Point(b.X, b.Y)))
                    {
                        r.HitCount++;
                        gameModel.Bullets.RemoveAt(j);

                        if (r.HitCount >= 5)
                        {
                            gameModel.Explosions.Add(new Explosion { X = r.X + r.Width / 2, Y = r.Y + r.Height / 2, Radius = r.Width / 2 });
                            PlaySound(explodeSound);
                            gameModel.Rectangles.RemoveAt(i);
                        }
                        break;
                    }
                }
            }

            // Bullet vs Triangle
            for (int i = gameModel.Triangles.Count - 1; i >= 0; i--)
            {
                var t = gameModel.Triangles[i];
                Rect tRect = new Rect(t.X, t.Y, t.Width, t.Height);
                for (int j = gameModel.Bullets.Count - 1; j >= 0; j--)
                {
                    var b = gameModel.Bullets[j];
                    if (tRect.Contains(new Point(b.X, b.Y)))
                    {
                        t.HitCount++;
                        gameModel.Bullets.RemoveAt(j);

                        if (t.HitCount >= 5)
                        {
                            gameModel.Explosions.Add(new Explosion { X = t.X + t.Width / 2, Y = t.Y + t.Height / 2, Radius = t.Width / 2 });
                            PlaySound(explodeSound);
                            gameModel.Triangles.RemoveAt(i);
                        }
                        break;
                    }
                }
            }

            // --- 7. ANIMATE EXPLOSIONS ---
            for (int i = gameModel.Explosions.Count - 1; i >= 0; i--)
            {
                var exp = gameModel.Explosions[i];
                exp.Life--;
                exp.Radius += 2;
                if (exp.Life <= 0) gameModel.Explosions.RemoveAt(i);
            }

            // --- 8. GLOBAL BOTTOM ZONE TRIPWIRE (Triggers Game Over near the bottom) ---
            double criticalFloor = 500;
            if (ShapesCanvas.ActualHeight > 100)
            {
                criticalFloor = ShapesCanvas.ActualHeight - 45;
            }

            // Circle floor check
            for (int i = gameModel.Circles.Count - 1; i >= 0; i--)
            {
                var c = gameModel.Circles[i];
                if (c.Y >= criticalFloor || c.Y >= 500)
                {
                    GameOver();
                    return;
                }
            }

            // Rectangle floor check
            for (int i = gameModel.Rectangles.Count - 1; i >= 0; i--)
            {
                var r = gameModel.Rectangles[i];
                if (r.Y >= criticalFloor || r.Y >= 500)
                {
                    GameOver();
                    return;
                }
            }

            // Triangle floor check
            for (int i = gameModel.Triangles.Count - 1; i >= 0; i--)
            {
                var t = gameModel.Triangles[i];
                if (t.Y >= criticalFloor || t.Y >= 500)
                {
                    GameOver();
                    return;
                }
            }

            Render();
        }
    }
}