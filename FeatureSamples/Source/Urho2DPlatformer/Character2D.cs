using Urho;
using Urho.Urho2D;
using Urho.Gui;
using System;

namespace Urho2DPlatformer
{
    public class Character2D : LogicComponent
    {

        public Character2D()
        {
            wounded_ = (false);
            killed_ = (false);
            timer_ = (0.0f);
            maxCoins_ = (0);
            remainingCoins_ = (0);
            remainingLifes_ = (3);
            isClimbing_ = (false);
            climb2_ = (false);
            aboveClimbable_ = (false);
            onSlope_ = (false);
        }
        public Character2D(IntPtr handle) : base(handle)
        {
            wounded_ = (false);
            killed_ = (false);
            timer_ = (0.0f);
            maxCoins_ = (0);
            remainingCoins_ = (0);
            remainingLifes_ = (3);
            isClimbing_ = (false);
            climb2_ = (false);
            aboveClimbable_ = (false);
            onSlope_ = (false);
        }

        protected override void OnUpdate(float timeStep)
        {
            // Handle wounded/killed states
            if (killed_)
                return;

            if (wounded_)
            {
                HandleWoundedState(timeStep);
                return;
            }

            var input = Application.Input;
            
            var body = GetComponent<RigidBody2D>();
            var animatedSprite = GetComponent<AnimatedSprite2D>();
            bool onGround = false;
            bool jump = false;

            // Collision detection (AABB query)
            Vector2 characterHalfSize = new Vector2(0.16f, 0.16f);
            var physicsWorld = Scene.GetComponent<PhysicsWorld2D>();

            RigidBody2D[] collidingBodies = physicsWorld.GetRigidBodies(new Rect(Node.WorldPosition2D - characterHalfSize - new Vector2(0.0f, 0.1f), Node.WorldPosition2D + characterHalfSize));

            if (collidingBodies.Length > 1 && !isClimbing_)
                onGround = true;

            // Set direction
            Vector2 moveDir = Vector2.Zero; // Reset

            if (input.GetKeyDown(Key.A) || input.GetKeyDown(Key.Left))
            {
                moveDir = moveDir + Vector2.Left;
                animatedSprite.FlipX = false; // Flip sprite (reset to default play on the X axis)
            }

            if (input.GetKeyDown(Key.D) || input.GetKeyDown(Key.Right))
            {
                moveDir = moveDir + Vector2.Right;
                animatedSprite.FlipX = true; // Flip sprite (flip animation on the X axis)
            }

            // Jump
            if ((onGround || aboveClimbable_) && (input.GetKeyPress(Key.W) || input.GetKeyPress(Key.Up)))
                jump = true;

            // Climb
            if (isClimbing_)
            {
                if (!aboveClimbable_ && (input.GetKeyDown(Key.Up) || input.GetKeyDown(Key.W)))
                    moveDir = moveDir + new Vector2(0.0f, 1.0f);

                if (input.GetKeyDown(Key.Down) || input.GetKeyDown(Key.S))
                    moveDir = moveDir + new Vector2(0.0f, -1.0f);
            }

            // Move
            if (!moveDir.Equals(Vector2.Zero) || jump)
            {
                if (onSlope_)
                    body.ApplyForceToCenter(moveDir * MOVE_SPEED / 2, true); // When climbing a slope, apply force (todo: replace by setting linear velocity to zero when will work)
                else
                    Node.Translate(new Vector3(moveDir.X, moveDir.Y, 0) * timeStep * 1.8f);
                if (jump)
                    body.ApplyLinearImpulse(new Vector2(0.0f, 0.17f) * MOVE_SPEED, body.MassCenter, true);
            }

            // Animate
            if (input.GetKeyDown(Key.Space))
            {
                if (animatedSprite.Animation != "attack")
                {
                    animatedSprite.SetAnimation("attack", LoopMode2D.ForceLooped);
                    animatedSprite.Speed = 1.5f;
                }
            }
            else if (!moveDir.Equals(Vector2.Zero))
            {
                if (animatedSprite.Animation != "run")
                    animatedSprite.SetAnimation("run");
            }
            else if (animatedSprite.Animation != "idle")
            {
                animatedSprite.SetAnimation("idle");
            }


            base.OnUpdate(timeStep);
        }

        void HandleWoundedState(float timeStep)
        {
            var body = GetComponent<RigidBody2D>();
            var animatedSprite = GetComponent<AnimatedSprite2D>();

            // Play "hit" animation in loop
            if (animatedSprite.Animation != "hit")
                animatedSprite.SetAnimation("hit", LoopMode2D.ForceLooped);

            // Update timer
            timer_ += timeStep;

            if (timer_ > 2.0f)
            {
                // Reset timer
                timer_ = 0.0f;

                // Clear forces (should be performed by setting linear velocity to zero, but currently doesn't work)
                body.SetLinearVelocity(Vector2.Zero);
                body.Awake = false;
                body.Awake = true;

                // Remove particle emitter
                Node.GetChild("Emitter", true).Remove();

                // Update lifes UI and counter
                remainingLifes_ -= 1;
                var ui = Application.UI;
                Text lifeText = (Text)ui.Root.GetChild("LifeText", true);
                lifeText.Value = new string(remainingLifes_.ToString()); // Update lifes UI counter

                // Reset wounded state
                wounded_ = false;

                // Handle death
                if (remainingLifes_ == 0)
                {
                    HandleDeath();
                    return;
                }

                // Re-position the character to the nearest point
                if (Node.Position.X < 15.0f)
                    Node.Position = new Vector3(1.0f, 8.0f, 0.0f);
                else
                    Node.Position = new Vector3(18.8f, 9.2f, 0.0f);
            }

        }

        private void HandleDeath()
        {
            var body = GetComponent<RigidBody2D>();
            var animatedSprite = GetComponent<AnimatedSprite2D>();

            // Set state to 'killed'
            killed_ = true;

            // Update UI elements
            var ui = Application.UI;
            Text instructions = (Text)ui.Root.GetChild("Instructions", true);
            instructions.Value = "!!! GAME OVER !!!";
            ui.Root.GetChild("ExitButton", true).Visible = true;
            ui.Root.GetChild("PlayButton", true).Visible = true;

            // Show mouse cursor so that we can click
            Application.Input.SetMouseVisible(true);

            // Put character outside of the scene and magnify him
            Node.Position = new Vector3(-20.0f, 0.0f, 0.0f);
            Node.SetScale(1.2f);

            // Play death animation once
            if (animatedSprite.Animation != "dead2")
                animatedSprite.SetAnimation("dead2");
        }


        const float MOVE_SPEED = 23.0f;
        const int LIFES = 3;

        /// Flag when player is wounded.
        public bool wounded_ = false;
        /// Flag when player is dead.
        public bool killed_ = false;
        /// Timer for particle emitter duration.
        public float timer_ = 0f;
        /// Number of coins in the current level.
        public uint maxCoins_ = 0;
        /// Counter for remaining coins to pick.
        public uint remainingCoins_ = 0;
        /// Counter for remaining lifes.
        public uint remainingLifes_ = 3;
        /// Indicate when the player is climbing a ladder or a rope.
        public bool isClimbing_ = false;
        /// Used only for ropes, as they are split into 2 shapes.
        public bool climb2_ = false;
        /// Indicate when the player is above a climbable object, so we can still jump anyway.
        public bool aboveClimbable_ = false;
        /// Indicate when the player is climbing a slope, so we can apply force to its body.
        public bool onSlope_ = false;

    }

}