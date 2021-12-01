//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Urho;

namespace ShapeBlaster
{
    public class Grid
    {
        class PointMass
        {
            public Vector3 Position = Vector3.Zero;
            public Vector3 Velocity  = Vector3.Zero;
            public float InverseMass
            {
                get;
                set;
            } = 0;

            private Vector3 acceleration = Vector3.Zero;
            private float damping = 0.98f;

            public PointMass(Vector3 position, float invMass)
            {
                Position = position;
                InverseMass = invMass;
            }

            public void ApplyForce(Vector3 force)
            {
                acceleration += force * InverseMass;
            }

            public void IncreaseDamping(float factor)
            {
                damping *= factor ;
            }

            public void Update(float deltaTime)
            {
                Velocity += acceleration * deltaTime ;
                Position += Velocity;
                acceleration = Vector3.Zero;
                if (Velocity.LengthSquared < 0.001f * 0.001f)
                    Velocity = Vector3.Zero;

                Velocity *= damping;
                damping = 0.98f;
            }
        }

        struct Spring
        {
            public PointMass End1;
            public PointMass End2;
            public float TargetLength;
            public float Stiffness;
            public float Damping;

            public Spring(PointMass end1, PointMass end2, float stiffness, float damping)
            {
                End1 = end1;
                End2 = end2;
                Stiffness = stiffness;
                Damping = damping;
                TargetLength = Vector3.Distance(end1.Position, end2.Position) * 0.95f;
            }

            public void Update(float deltaTime)
            {
                var x = End1.Position - End2.Position;

                float length = x.Length;
                // these springs can only pull, not push
                if (length > TargetLength)
                {
                    x = (x / length) * (length - TargetLength);
                    var dv = End2.Velocity - End1.Velocity;
                    var force = Stiffness * x - dv * Damping;

                    End1.ApplyForce(-force);
                    End2.ApplyForce(force);
                }
            }
        }

        Spring[] springs;
        PointMass[,] points;
        Vector2 screenSize;

        public Grid(IntRect size, Vector2 spacing)
        {
            var springList = new List<Spring>();

            int numColumns = (int)(size.Width() / spacing.X) + 1;
            int numRows = (int)(size.Height() / spacing.Y) + 1;
            points = new PointMass[numColumns, numRows];

            // these fixed points will be used to anchor the grid to fixed positions on the screen
            PointMass[,] fixedPoints = new PointMass[numColumns, numRows];

            // create the point masses
            int column = 0, row = 0;
            for (float y = size.Top; y <= size.Bottom; y += spacing.Y)
            {
                for (float x = size.Left; x <= size.Right; x += spacing.X)
                {
                    points[column, row] = new PointMass(new Vector3(x, y, 0), 1f);
                    fixedPoints[column, row] = new PointMass(new Vector3(x, y, 0), 0f);
                    column++;
                }
                row++;
                column = 0;
            }

            // link the point masses with springs
            for (int y = 0; y < numRows; y++)
                for (int x = 0; x < numColumns; x++)
                {
                    if (x == 0 || y == 0 || x == numColumns - 1 || y == numRows - 1)    // anchor the border of the grid
                        springList.Add(new Spring(fixedPoints[x, y], points[x, y], 0.1f, 0.1f));
                    else if (x % 3 == 0 && y % 3 == 0)                                  // loosely anchor 1/9th of the point masses
                        springList.Add(new Spring(fixedPoints[x, y], points[x, y], 0.002f, 0.02f));

                    const float stiffness = 0.28f;
                    const float damping = 0.06f;

                    if (x > 0)
                        springList.Add(new Spring(points[x - 1, y], points[x, y], stiffness, damping));
                    if (y > 0)
                        springList.Add(new Spring(points[x, y - 1], points[x, y], stiffness, damping));
                }

            springs = springList.ToArray();
        }

        public void ApplyDirectedForce(Vector2 force, Vector2 position, float radius)
        {
            ApplyDirectedForce(new Vector3(force.X, force.Y, 0), new Vector3(position.X, position.Y, 0), radius);
        }

        public void ApplyDirectedForce(Vector3 force, Vector3 position, float radius)
        {
            foreach (var mass in points)
                if (Vector3.Distance(position, mass.Position) < radius)
                    mass.ApplyForce(10 * force / (10 + Vector3.Distance(position, mass.Position)));
        }

        public void ApplyImplosiveForce(float force, Vector2 position, float radius)
        {
            ApplyImplosiveForce(force, new Vector3(position.X, position.Y, 0), radius);
        }

        public void ApplyImplosiveForce(float force, Vector3 position, float radius)
        {
            foreach (var mass in points)
            {
                float dist2 = Vector3.Distance(position, mass.Position);
                if (dist2 < radius)
                {
                    mass.ApplyForce(10 * force * (position - mass.Position) / (100 + dist2));
                    mass.IncreaseDamping(0.6f);
                }
            }
        }

        public void ApplyExplosiveForce(float force, Vector2 position, float radius)
        {
            ApplyExplosiveForce(force, new Vector3(position.X, position.Y, 0), radius);
        }

        public void ApplyExplosiveForce(float force, Vector3 position, float radius)
        {
            foreach (var mass in points)
            {
                float dist2 = Vector3.Distance(position, mass.Position);
                if (dist2 < radius * radius)
                {
                    mass.ApplyForce(100 * force * (mass.Position - position) / (10000 + dist2));
                    mass.IncreaseDamping(0.6f);
                }
            }
        }

        public void Update(float deltaTime)
        {
            // foreach (var spring in springs)
            //     spring.Update(deltaTime);

            // foreach (var mass in points)
            //     mass.Update(deltaTime);
        }

        public void Draw2()
        {
            int width = points.GetLength(0);
            int height = points.GetLength(1);
             Color color = new Color(30 / 255.0f, 30 / 255.0f, 139 / 255.0f, 100 / 255.0f);   // dark blue

            for (int y = 1; y < height; y++)
            {
                for (int x = 1; x < width; x++)
                {
                    Vector2 left = new Vector2(), up = new Vector2(); Vector2 p = ToVec2(points[x, y].Position); 
                    if (x > 1)
                    {
                        left = ToVec2(points[x - 1, y].Position);
                        float thickness = y % 3 == 1 ? 3f : 1f;
                        CustomRenderer.DrawLine(left, p, color, thickness);
                    }
                    if (y > 1)
                    {
                        up = ToVec2(points[x, y - 1].Position);
                        float thickness = x % 3 == 1 ? 3f : 1f;
                        CustomRenderer.DrawLine(up, p, color, thickness);
                    }
                }
            }
        }
        public void Draw(/*SpriteBatch spriteBatch*/)
        {
            var graphics = Application.Current.Graphics;
            screenSize = GameRoot.ScreenSize;

            int width = points.GetLength(0);
            int height = points.GetLength(1);
            Color color = new Color(30 / 255.0f, 30 / 255.0f, 139 / 255.0f, 100 / 255.0f);   // dark blue

            for (int y = 1; y < height; y++)
            {
                for (int x = 1; x < width; x++)
                {
                    Vector2 left = new Vector2(), up = new Vector2();
                    Vector2 p = ToVec2(points[x, y].Position);
                    if (x > 1)
                    {
                        left = ToVec2(points[x - 1, y].Position);
                        float thickness = y % 3 == 1 ? 3f : 1f;

                        // use Catmull-Rom interpolation to help smooth bends in the grid
                        int clampedX = Math.Min(x + 1, width - 1);

                        Vector2 mid = MathUtil.CatmullRom(ToVec2(points[x - 2, y].Position), left, p, ToVec2(points[clampedX, y].Position), 0.5f);

                        // If the grid is very straight here, draw a single straight line. Otherwise, draw lines to our
                        // new interpolated midpoint
                        if (mid.Distance((left + p) / 2) > 1)
                        {
                            CustomRenderer.DrawLine(left, mid, color, thickness);
                            CustomRenderer.DrawLine(mid, p, color, thickness);
                        }
                        else
                            CustomRenderer.DrawLine(left, p, color, thickness);
                    }
                    if (y > 1)
                    {
                        up = ToVec2(points[x, y - 1].Position);
                        float thickness = x % 3 == 1 ? 3f : 1f;
                        int clampedY = Math.Min(y + 1, height - 1);
                        Vector2 mid = MathUtil.CatmullRom(ToVec2(points[x, y - 2].Position), up, p, ToVec2(points[x, clampedY].Position), 0.5f);

                        if (mid.Distance((up + p) / 2) > 1)
                        {
                            CustomRenderer.DrawLine(up, mid, color, thickness);
                            CustomRenderer.DrawLine(mid, p, color, thickness);
                        }
                        else
                            CustomRenderer.DrawLine(up, p, color, thickness);
                    }

                    // Add interpolated lines halfway between our point masses. This makes the grid look
                    // denser without the cost of simulating more springs and point masses.
                    if (x > 1 && y > 1)
                    {
                        Vector2 upLeft = ToVec2(points[x - 1, y - 1].Position);
                        CustomRenderer.DrawLine(0.5f * (upLeft + up), 0.5f * (left + p), color, 1f);   // vertical line
                        CustomRenderer.DrawLine(0.5f * (upLeft + left), 0.5f * (up + p), color, 1f);   // horizontal line
                    }
                }
            }
        }

        public Vector2 ToVec2(Vector3 v)
        {
            // do a perspective projection
            float factor = (v.Z + 2000) / 2000;
            return (new Vector2(v.X, v.Y) - screenSize / 2f) * factor + screenSize / 2;
        }
    }
}
