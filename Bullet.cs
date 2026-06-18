using System;

namespace ShapeShootingGame.Models
{
    internal class Bullet
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double DX { get; set; } // Tracks horizontal speed
        public double DY { get; set; } // Tracks vertical speed

        public void ResetPosition(double canvasWidth, double canvasHeight)
        {
            X = canvasWidth / 2;
            Y = canvasHeight - 20;
        }
    }
}