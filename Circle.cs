using System;

namespace ShapeShootingGame.Models
{
    internal class Circle
    {
        public double X;
        public double Y;
        public double Radius;

        public int XDirection = 1;
        public int YDirection = 1;
        public int HitCount = 0; // Tracks wall/shape collisions

        public Circle(double x, double y, double r)
        {
            this.X = x;
            this.Y = y;
            this.Radius = r;
        }
    }
}