using Urho;
using Urho.Urho2D;
using Urho.Gui;
using System;

namespace Urho2DIsometricDemo
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
            moveSpeedScale_ = 1.0f;
            zoom_ = 0.0f;
        }
        public Character2D(IntPtr handle) : base(handle)
        {
            wounded_ = (false);
            killed_ = (false);
            timer_ = (0.0f);
            maxCoins_ = (0);
            remainingCoins_ = (0);
            remainingLifes_ = (3);
            moveSpeedScale_ = 1.0f;
            zoom_ = 0.0f;
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
            var animatedSprite = GetComponent<AnimatedSprite2D>();

            // Set direction
            Vector3 moveDir = Vector3.Zero; // Reset
            float speedX = Math.Clamp(MOVE_SPEED_X / zoom_, 0.4f, 1.0f);
            float speedY = speedX;


            if (input.GetKeyDown(Key.A) || input.GetKeyDown(Key.Left))
            {
                moveDir = moveDir + Vector3.Left * speedX;
                animatedSprite.FlipX = false; // Flip sprite (reset to default play on the X axis)
            }

            if (input.GetKeyDown(Key.D) || input.GetKeyDown(Key.Right))
            {
                moveDir = moveDir + Vector3.Right * speedX;
                animatedSprite.FlipX = true; // Flip sprite (flip animation on the X axis)
            }



            if (!moveDir.Equals(Vector3.Zero))
                speedY = speedX * moveSpeedScale_;

            if (input.GetKeyDown(Key.W) || input.GetKeyDown(Key.Up))
                moveDir = moveDir + Vector3.Up * speedY;
            if (input.GetKeyDown(Key.S) || input.GetKeyDown(Key.Down))
                moveDir = moveDir + Vector3.Down * speedY;

            // Move
            if (!moveDir.Equals(Vector3.Zero))
                Node.Translate(moveDir * timeStep);


            // Animate
            if (input.GetKeyDown(Key.Space))
            {
                if (animatedSprite.Animation != "attack")
                {
                    animatedSprite.SetAnimation("attack", LoopMode2D.ForceLooped);
                    animatedSprite.Speed = 1.5f;
                }
            }
            else if (!moveDir.Equals(Vector3.Zero))
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
                    Node.Position = new Vector3(-5.0f, 11.0f, 0.0f);
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
            if (animatedSprite.Animation != "dead")
                animatedSprite.SetAnimation("dead");
        }

        const float MOVE_SPEED_X = 4.0f;
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

        public float moveSpeedScale_ = 1.0f;
        public float zoom_ = 0.0f;
        /// Indicate when the player is climbing a ladder or a rope.

    }

}