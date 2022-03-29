// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
// Copyright (c) 2008-2021 the Urho3D project.
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using Urho;
using System;
using Urho.Physics;
using System.Runtime.InteropServices;

namespace AsyncLoading
{
    public class Character : LogicComponent
    {
        /// Movement controls. Assigned by the main program each frame.
        public Controls Controls { get; set; } = new Controls();

        /// Grounded flag for movement.
        bool onGround;
        /// Jump flag.
        bool okToJump;

        bool jumpStarted = false;

        /// In air timer. Due to possible physics inaccuracy, character can be off ground for max. 1/10 second and still be allowed to move.
        float inAirTimer;

        RigidBody body;
        public AnimationController animController;

        // water contact
        bool inWater;
        Vector3 waterContatct;

        public Character()
        {
            okToJump = true;
        }

        // constructor needed for deserialization
        public Character(IntPtr handle) : base(handle) { }

        public override void OnSceneSet(Scene scene)
        {
            base.OnSceneSet(scene);

            if (scene != null)
            {
                // init char anim, so we don't see the t-pose char as it's spawned
                AnimationController animCtrl = Node.GetComponent<AnimationController>(true);
                animCtrl.PlayExclusive("Platforms/Models/BetaLowpoly/Beta_JumpLoop1.ani", 0, true, 0.0f);

                // anim trigger event
                AnimatedModel animModel = Node.GetComponent<AnimatedModel>(true);
                animModel.Node.AnimationTrigger += HandleAnimationTrigger;
                Node.NodeCollision += (HandleNodeCollision);

                //enable receiving FixedUpdate events,  OnFixedUpdate()  will be called
                ReceiveFixedUpdates = true;
            }

        }



        protected override void OnFixedUpdate(PhysicsPreStepEventArgs e)
        {
            float timeStep = e.TimeStep;

            animController = animController ?? Node.GetComponent<AnimationController>(true);
            body = body ?? GetComponent<RigidBody>();

            // Update the in air timer. Reset if grounded
            if (!onGround)
                inAirTimer += timeStep;
            else
                inAirTimer = 0.0f;
            // When character has been in air less than 1/10 second, it's still interpreted as being on ground
            bool softGrounded = inAirTimer < Global.InairThresholdTime;

            // Update movement & animation
            var rot = Node.Rotation;
            Vector3 moveDir = Vector3.Zero;
            var velocity = body.LinearVelocity;
            // Velocity on the XZ plane
            Vector3 planeVelocity = new Vector3(velocity.X, 0.0f, velocity.Z);

            if (Controls.IsDown(Global.CtrlForward))
                moveDir += Vector3.UnitZ;
            if (Controls.IsDown(Global.CtrlBack))
                moveDir += new Vector3(0f, 0f, -1f);
            if (Controls.IsDown(Global.CtrlLeft))
                moveDir += new Vector3(-1f, 0f, 0f);
            if (Controls.IsDown(Global.CtrlRight))
                moveDir += Vector3.UnitX;

            // left stick
            Vector2 axisInput = Controls.ExtraData["axis_0"];
            moveDir += Vector3.Forward * -axisInput.Y;
            moveDir += Vector3.Right * axisInput.X;


            // Normalize move vector so that diagonal strafing is not faster
            if (moveDir.LengthSquared > 0.0f)
                moveDir.Normalize();

            // If in air, allow control, but slower than when on ground
            body.ApplyImpulse(rot * moveDir * (softGrounded ? Global.MoveForce : Global.InairMoveForce));

            if (softGrounded)
            {
                // When on ground, apply a braking force to limit maximum ground velocity
                Vector3 brakeForce = -planeVelocity * Global.BrakeForce;
                body.ApplyImpulse(brakeForce);

                // Jump. Must release jump control inbetween jumps
                if (Controls.IsDown(Global.CtrlJump))
                {
                    if (okToJump)
                    {
                        body.ApplyImpulse(Vector3.UnitY * Global.JumpForce);
                        okToJump = false;
                        animController.StopLayer(0);
                        animController.PlayExclusive("Platforms/Models/BetaLowpoly/Beta_JumpStart.ani", 0, false, 0.2f);
                        animController.SetTime("Platforms/Models/BetaLowpoly/Beta_JumpStart.ani", 0);
                    }
                }
                else
                    okToJump = true;
            }

            if ((!onGround && !softGrounded) || jumpStarted)
            {
                if (jumpStarted)
                {
                    if (animController.IsAtEnd("Platforms/Models/BetaLowpoly/Beta_JumpStart.ani"))
                    {
                        animController.PlayExclusive("Platforms/Models/BetaLowpoly/Beta_JumpLoop1.ani", 0, true, 0.3f);
                        animController.SetTime("Platforms/Models/BetaLowpoly/Beta_JumpLoop1.ani", 0);
                        jumpStarted = false;
                    }
                }
                else
                {
                    const float maxDistance = 50.0f;
                    const float segmentDistance = 10.01f;
                    PhysicsRaycastResult result = new PhysicsRaycastResult();

                    Scene.GetComponent<PhysicsWorld>()?.RaycastSingleSegmented(ref result, new Ray(Node.Position, Vector3.Down),
                                                                                     maxDistance, segmentDistance, 0xffff);
                    if (result.Body != null && result.Distance > 0.7f)
                    {
                        animController.PlayExclusive("Platforms/Models/BetaLowpoly/Beta_JumpLoop1.ani", 0, true, 0.2f);
                    }
                }
            }
            else
            {
                // Play walk animation if moving on ground, otherwise fade it out
                if ((softGrounded) && !moveDir.Equals(Vector3.Zero))
                {
                    animController.PlayExclusive("Platforms/Models/BetaLowpoly/Beta_Run.ani", 0, true, 0.2f);
                }
                else
                {
                    animController.PlayExclusive("Platforms/Models/BetaLowpoly/Beta_Idle.ani", 0, true, 0.2f);
                }
            }

            // Reset grounded flag for next frame
            onGround = false;
            inWater = false;
        }

        void HandleNodeCollision(NodeCollisionEventArgs args)
        {
            RigidBody otherBody = args.OtherBody;

            bool isWater = false;
            if (otherBody.Trigger)
            {
                if (otherBody.CollisionLayer == (uint)Global.CollisionLayerType.ColLayer_Water)
                {
                    isWater = true;
                }
                else
                {
                    return;
                }
            }

            foreach (var contact in args.Contacts)
            {
                // If contact is below node center and mostly vertical, assume it's a ground contact
                if (contact.ContactPosition.Y < (Node.Position.Y + 1.0f))
                {
                    if (isWater)
                    {
                        waterContatct = contact.ContactPosition;
                        inWater = true;
                        break;
                    }

                    float level = Math.Abs(contact.ContactNormal.Y);
                    if (level > 0.75)
                        onGround = true;
                }
            }
        }


        private void HandleAnimationTrigger(AnimationTriggerEventArgs obj)
        {
            Animation animation = obj.Animation;
            String strAction = obj.Data;

            if (strAction != null && strAction.Contains("Foot"))
            {
                Node footNode = Node.GetChild(strAction, true);
                if (footNode != null)
                {
                    Vector3 fwd = Node.WorldDirection;
                    Vector3 pos = footNode.WorldPosition;// + Vector3(0.0, 0.2f, 0.0f);

                }

            }
        }

    }
}
