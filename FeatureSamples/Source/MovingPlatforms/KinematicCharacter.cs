// Copyright (c) 2020-2021 Eli Aloni (a.k.a  elix22)
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

namespace MovingPlatforms
{

    public class KinematicCharacter : Component
    {

#region Variables
        struct MovingData
        {
            public static bool operator == (MovingData lhs, MovingData rhs)
            {
                return (lhs.node_ != null && lhs.node_ == rhs.node_);
            }

            public static bool operator != (MovingData lhs, MovingData rhs)
            {
                return (lhs.node_ != null && lhs.node_ != rhs.node_);
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            
            public Node node_;
            public Matrix3x4 transform_;
        }

        MovingData[] movingData_ = new MovingData[2];

        /// Movement controls. Assigned by the main program each frame.
        public Controls Controls { get; set; } = new Controls();

        /// Grounded flag for movement.
        bool onGround = false;
        /// Jump flag.
        bool okToJump = true;
        /// In air timer. Due to possible physics inaccuracy, character can be off ground for max. 1/10 second and still be allowed to move.
        float inAirTimer = 0.0f;

        public AnimationController animController;
        KinematicCharacterController kinematicController;

        Vector3 curMoveDir = Vector3.Zero;

        bool jumpStarted = false;

        const float MOVE_FORCE = 0.2f;
        const float INAIR_MOVE_FORCE = 0.2f;

        PhysicsWorld physicsWorld = null;

#endregion Variables

        public KinematicCharacter()
        {
            okToJump = true;
        }

        // constructor needed for deserialization
        public KinematicCharacter(IntPtr handle) : base(handle) { }

        public override void OnSceneSet(Scene scene)
        {
            base.OnSceneSet(scene);

            if (scene != null)
            {
                physicsWorld = Scene.GetComponent<PhysicsWorld>();
                physicsWorld.PhysicsPreStep += (args) => FixedUpdate(args.TimeStep);
                physicsWorld.PhysicsPostStep += (args) => FixedPostUpdate(args.TimeStep);
            }
        }

        public override void OnAttachedToNode(Node node)
        {

            // Component has been inserted into its scene node. Subscribe to events now
            node.NodeCollision += (HandleNodeCollision);
        }

        void FixedUpdate(float timeStep)
        {
            animController = animController ?? Node.GetComponent<AnimationController>(true);
            kinematicController = kinematicController ?? Node.GetComponent<KinematicCharacterController>(true);

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
            onGround = kinematicController.OnGround();

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

                        animController.StopLayer(0);
                        animController.PlayExclusive("Platforms/Models/BetaLowpoly/Beta_JumpStart.ani", 0, false, 0.2f);
                        animController.SetTime("Platforms/Models/BetaLowpoly/Beta_JumpStart.ani", 0);
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

                    physicsWorld?.RaycastSingleSegmented(ref result, new Ray(Node.Position, Vector3.Down),
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

        }

        void FixedPostUpdate(float timeStep)
        {
            if (movingData_[0] == movingData_[1])
            {
                Matrix3x4 delta = movingData_[0].transform_ * movingData_[1].transform_.Inverse();

                Vector3 kPos;
                Quaternion kRot;
                kinematicController.GetTransform(out kPos, out kRot);

                Matrix3x4 matKC = new Matrix3x4(kPos, kRot, Vector3.One);

                // update
                matKC = delta * matKC;
                kinematicController.SetTransform(matKC.Translation(), matKC.Rotation());

                // update yaw control (directly rotates char)
                Controls.Yaw += delta.Rotation().YawAngle;
            }

            movingData_[1] = movingData_[0];
            movingData_[0].node_ = null;
        }

        bool IsNodeMovingPlatform(Node node)
        {
            if (node == null)
            {
                return false;
            }

            return node.GetVar(new StringHash("IsMovingPlatform"));
        }

        void HandleNodeCollision(NodeCollisionEventArgs args)
        {
            if (args.OtherBody.Trigger == true && args.OtherNode != null)
            {
                if (IsNodeMovingPlatform(args.OtherNode))
                {
                    movingData_[0].node_ = args.OtherNode;
                    movingData_[0].transform_ = args.OtherNode.WorldTransform;
                }
            }
        }
        
    }
}
