using System;
using System.Collections.Generic;

namespace ShapeShootingGame.Models
{
    internal class GameModel
    {
        public List<Circle> Circles = new List<Circle>();
        public List<Rectangle> Rectangles = new List<Rectangle>();
        public List<Triangle> Triangles = new List<Triangle>();
        public List<Bullet> Bullets = new List<Bullet>();
        public List<Explosion> Explosions = new List<Explosion>();
        public string WeaponMode = "Rifle";
    }

    internal class Explosion
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public int Life { get; set; } = 15;
    }
}