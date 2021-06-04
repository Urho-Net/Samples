// Copyright (c) 2008-2021 the Urho3D project.
//
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

namespace NakamaNetworking
{
    public class LocalKinematicCharacter : Component
    {
        /// Movement controls. Assigned by the main program each frame.
        public Controls Controls { get; set; } = new Controls();

        /// Grounded flag for movement.
        bool onGround;
        /// Jump flag.
        bool okToJump;
        /// In air timer. Due to possible physics inaccuracy, character can be off ground for max. 1/10 second and still be allowed to move.
        float inAirTimer;

        public AnimationController animCtrl;
        KinematicCharacterController kinematicController;

        Vector3 curMoveDir = Vector3.Zero;

        bool jumpStarted = false;

        const float MOVE_FORCE = 0.2f;
        const float INAIR_MOVE_FORCE = 0.2f;

        PhysicsWorld physicsWorld = null;

        public LocalKinematicCharacter()
        {
            okToJump = true;
        }

        // constructor needed for deserialization
        public LocalKinematicCharacter(IntPtr handle) : base(handle) { }

        public override void OnSceneSet(Scene scene)
        {
            base.OnSceneSet(scene);

            if (scene != null)
            {
                kinematicController= Node.CreateComponent<KinematicCharacterController>();
                physicsWorld = Scene.GetComponent<PhysicsWorld>();
                physicsWorld.PhysicsPreStep += (args) => FixedUpdate(args.TimeStep);
            }
        }
        void FixedUpdate(float timeStep)
        {
            animCtrl = animCtrl ?? Node.GetComponent<AnimationController>(true);
            kinematicController = kinematicController ?? Node.GetComponent<KinematicCharacterController>(true);

            onGround = kinematicController.OnGround();



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

            var LinearVelocity = kinematicController.LinearVelocity;
            // Velocity on the XZ plane
            Vector3 planeVelocity = new Vector3(LinearVelocity.X, 0.0f, LinearVelocity.Z);




            if (Controls.IsDown(Global.CtrlForward))
                moveDir += Vector3.Forward;
            if (Controls.IsDown(Global.CtrlBack))
                moveDir += Vector3.Back;
            if (Controls.IsDown(Global.CtrlLeft))
                moveDir += Vector3.Left;
            if (Controls.IsDown(Global.CtrlRight))
                moveDir += Vector3.Right;

            // left stick
            Vector2 axisInput = Controls.ExtraData["axis_0"];
            moveDir += Vector3.Forward * -axisInput.Y;
            moveDir += Vector3.Right * axisInput.X;

            // Normalize move vector so that diagonal strafing is not faster
            if (moveDir.LengthSquared > 0.0f)
                moveDir.Normalize();

            // rotate movedir
            Vector3 velocity = rot * moveDir;
            if (onGround)
            {
                curMoveDir = velocity;
            }
            else
            {   // In-air direction control is limited
                curMoveDir = Vector3.Lerp(curMoveDir, velocity, 0.03f);
            }

            kinematicController.SetWalkDirection(curMoveDir * (softGrounded ? MOVE_FORCE : INAIR_MOVE_FORCE));

            if (softGrounded)
            {
                // Jump. Must release jump control between jumps
                if (Controls.IsDown(Global.CtrlJump))
                {
                    if (okToJump)
                    {
                        okToJump = false;
                        jumpStarted = true;
                        kinematicController.Jump(Vector3.Zero);

                        animCtrl.StopLayer(0);
                        animCtrl.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, false, 0.2f);


                        animCtrl.SetTime("Models/Mutant/Mutant_Jump1.ani", 0);
                    }
                }
                else
                {
                    okToJump = true;
                }

            }


            if ((!onGround && !softGrounded) || jumpStarted)
            {
                if (jumpStarted)
                {
                    if (animCtrl.IsAtEnd("Models/Mutant/Mutant_Jump1.ani"))
                    {
                        animCtrl.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, true, 0.3f);
                        animCtrl.SetTime("Models/Mutant/Mutant_Jump1.ani", 0);
                    }

                    jumpStarted = false;
                }
                else
                {
                    const float maxDistance = 50.0f;
                    const float segmentDistance = 10.01f;
                    PhysicsRaycastResult result = new PhysicsRaycastResult();

                    physicsWorld?.RaycastSingleSegmented(ref result, new Ray(Node.Position, Vector3.Down),
                                                                                     maxDistance, segmentDistance, 0xffff);
                    if (result.Body != null && result.Distance > 0.7f)
                    {
                        animCtrl.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, true, 0.2f);
                    }
                }
            }
            else
            {
                // Play walk animation if moving on ground, otherwise fade it out
                if ((softGrounded) &&  !moveDir.Equals(Vector3.Zero))
                {
                    animCtrl.PlayExclusive("Models/Mutant/Mutant_Run.ani", 0, true, 0.2f);
                    // Set walk animation speed proportional to velocity
                    animCtrl.SetSpeed("Models/Mutant/Mutant_Run.ani", planeVelocity.Length * 10f);
                }
                else
                {
                    animCtrl.PlayExclusive("Models/Mutant/Mutant_Idle0.ani", 0, true, 0.2f);
                }
            }

            
            // send data over network
            Vector3 position;
            Quaternion rotation;
            kinematicController.GetTransform(out position, out rotation);
            Global.SendMatchState(
                OpCodes.VelocityAndPositionAndRotation,
                MatchDataJson.VelocityPositionRotation(curMoveDir * (softGrounded ? MOVE_FORCE : INAIR_MOVE_FORCE), position, rotation));

            Global.SendMatchState(OpCodes.Input, MatchDataJson.ControlsInput(Controls));

        }

    }
}
