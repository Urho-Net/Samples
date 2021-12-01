//---------------------------------------------------------------------------------
// Ported to the Atomic Game Engine
// Originally written for XNA by Michael Hoffman
// Find the full tutorial at: http://gamedev.tutsplus.com/series/vector-shooter-xna/
//----------------------------------------------------------------------------------

using System;
using Urho;

namespace ShapeBlaster
{
    class PlayerShip : Entity
    {
        private static PlayerShip instance;
        public static PlayerShip Instance
        {
            get
            {
                if (instance == null)
                    instance = new PlayerShip();

                return instance;
            }
        }

        const int cooldownFrames = 6;
        int cooldowmRemaining = 0;

        int framesUntilRespawn = 0;
        public bool IsDead { get { return framesUntilRespawn > 0; } }

        static Random rand = new Random();

        private PlayerShip()
        {
            image = Art.Player;
            Position = ShapeBlaster.ScreenSize / 2;
            Radius = 10;
        }

        public override void Update()
        {
            if (IsDead)
            {
                if (--framesUntilRespawn == 0)
                {
                    if (PlayerStatus.Lives == 0)
                    {
                        PlayerStatus.Reset();
                        Position = ShapeBlaster.ScreenSize / 2;
                    }
                    ShapeBlaster.Grid.ApplyDirectedForce(new Vector3(0, 0, 5000), new Vector3(Position.X, Position.Y, 0), 50);
                }

                return;
            }

            const float speed = 8;

            Velocity += speed * ShipInput.GetMovementDirection();

            Position += Velocity;
            Position = Vector2.Clamp(Position, Size / 2, ShapeBlaster.ScreenSize - Size / 2);

            if (Velocity.LengthSquared > 0)
                Orientation = Velocity.ToAngle();

            MakeExhaustFire();

            var aim = ShipInput.GetAimDirection();

            if (aim.LengthSquared > 0 && cooldowmRemaining <= 0)
            {
                cooldowmRemaining = cooldownFrames;
                float aimAngle = aim.ToAngle();
                
                Quaternion aimQuat = MathUtil.CreateFromYawPitchRoll(0, 0, aimAngle);

                float randomSpread = rand.NextFloat(-0.04f, 0.04f) + rand.NextFloat(-0.04f, 0.04f);
                Vector2 vel = MathUtil.FromPolar(aimAngle + randomSpread, 11f);

                Vector2 offset = MathUtil.Transform(new Vector2(35, -8), aimQuat);
                EntityManager.Add(new Bullet(Position + offset, vel));

                offset = MathUtil.Transform(new Vector2(35, 8), aimQuat);
                EntityManager.Add(new Bullet(Position + offset, vel));

                // Sound.Shot.Play(0.2f, rand.NextFloat(-0.2f, 0.2f), 0);
            }

            if (cooldowmRemaining > 0)
                cooldowmRemaining--;

            Velocity = Vector2.Zero;
        }

        private void MakeExhaustFire()
        {
            if (Velocity.LengthSquared > 0.1f)
            {
                // set up some variables
                Orientation = Velocity.ToAngle();
                Quaternion rot = MathUtil.CreateFromYawPitchRoll(0f, 0f, Orientation);

                double t = ShapeBlaster.ElapsedTime;
                // The primary velocity of the particles is 3 pixels/frame in the direction opposite to which the ship is travelling.
                Vector2 baseVel = Velocity.ScaleTo(-3);
                // Calculate the sideways velocity for the two side streams. The direction is perpendicular to the ship's velocity and the
                // magnitude varies sinusoidally.
                Vector2 perpVel = new Vector2(baseVel.Y, -baseVel.X) * (0.6f * (float)Math.Sin(t * 10));

                Color sideColor = new Color(200.0f/255.0f, 38.0f/255.0f, 9.0f/255.0f);    // deep red
                Color midColor = new Color(255/255.0f, 187/255.0f, 30/255.0f);   // orange-yellow

                Vector2 pos = Position + MathUtil.Transform(new Vector2(-25, 0), rot);   // position of the ship's exhaust pipe.
                const float alpha = 0.7f;

                // middle particle stream
                Vector2 velMid = baseVel + rand.NextVector2(0, 1);
                ShapeBlaster.ParticleManager.CreateParticle(Art.LineParticle, pos, Color.White * alpha, 60f, new Vector2(0.5f, 1),
                    new ParticleState(velMid, ParticleType.Enemy));
                ShapeBlaster.ParticleManager.CreateParticle(Art.Glow, pos, midColor * alpha, 60f, new Vector2(0.5f, 1),
                    new ParticleState(velMid, ParticleType.Enemy));

                // side particle streams
                Vector2 vel1 = baseVel + perpVel + rand.NextVector2(0, 0.3f);
                Vector2 vel2 = baseVel - perpVel + rand.NextVector2(0, 0.3f);
                ShapeBlaster.ParticleManager.CreateParticle(Art.LineParticle, pos, Color.White * alpha, 60f, new Vector2(0.5f, 1),
                    new ParticleState(vel1, ParticleType.Enemy));
                ShapeBlaster.ParticleManager.CreateParticle(Art.LineParticle, pos, Color.White * alpha, 60f, new Vector2(0.5f, 1),
                    new ParticleState(vel2, ParticleType.Enemy));

                ShapeBlaster.ParticleManager.CreateParticle(Art.Glow, pos, sideColor * alpha, 60f, new Vector2(0.5f, 1),
                    new ParticleState(vel1, ParticleType.Enemy));
                ShapeBlaster.ParticleManager.CreateParticle(Art.Glow, pos, sideColor * alpha, 60f, new Vector2(0.5f, 1),
                    new ParticleState(vel2, ParticleType.Enemy));
            }
        }

        public override void Draw(/*SpriteBatch spriteBatch*/)
        {
            if (!IsDead)
            {
                base.Draw();

                // Draw the aiming pointer
                var aim = ShipInput.GetAimDirection();
                float aimAngle = aim.ToAngle();
                Quaternion aimQuat = MathUtil.CreateFromYawPitchRoll(0, 0, aimAngle);
                Vector2 offset = MathUtil.Transform(new Vector2(Size.X +10, Art.Pointer.Height/2), aimQuat);
                CustomRenderer.Draw(Art.Pointer, Position + offset, Color.Green, aimAngle -2.1f,  Vector2.Zero, 1, 0f);
            }
        }

        public void Kill()
        {
            PlayerStatus.RemoveLife();
            framesUntilRespawn = PlayerStatus.IsGameOver ? 300 : 120;

            Color explosionColor = new Color(0.8f, 0.8f, 0.4f); // yellow

            for (int i = 0; i < 1200; i++)
            {
                float speed = 18f * (1f - 1 / rand.NextFloat(1f, 10f));
                Color color = MathUtil.Lerp(Color.White, explosionColor, rand.NextFloat(0, 1));
                var state = new ParticleState()
                {
                    Velocity = rand.NextVector2(speed, speed),
                    Type = ParticleType.None,
                    LengthMultiplier = 1
                };

                ShapeBlaster.ParticleManager.CreateParticle(Art.LineParticle, Position, color, 190, 1.5f, state);
            }
        }
    }
}
