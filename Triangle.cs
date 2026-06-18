using System;

namespace ShapeShootingGame.Models
{
    internal class Triangle
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public int XDirection = 1;
        public int YDirection = 1;
        public int HitCount = 0; // Tracks wall/shape collisions

        public Triangle(double x, double y, double width, double height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }
    }
}