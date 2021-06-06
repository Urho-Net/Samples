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
using Nakama;
using Nakama.TinyJson;
using System.Text;
using System.Collections.Generic;
using Urho.Gui;

namespace NakamaNetworking
{
    public class RemoteKinematicCharacter : Component
    {

        public RemotePlayerNetworkData NetworkData;
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

        bool IsSceneSet = false;

        bool IsFirstNetWorkPacket = true;

        Vector3 NewLinearVelocity = Vector3.Zero;
        Vector3 NewPosition = Vector3.Zero;
        Quaternion NewRotation = Quaternion.Identity;

        Text3D characterHUDText  = null;


        public RemoteKinematicCharacter()
        {
            okToJump = true;
        }

        // constructor needed for deserialization
        public RemoteKinematicCharacter(IntPtr handle) : base(handle) { }

        public override void OnSceneSet(Scene scene)
        {
            base.OnSceneSet(scene);

            if (scene != null)
            {
                kinematicController = Node.CreateComponent<KinematicCharacterController>();
                physicsWorld = Scene.GetComponent<PhysicsWorld>();
                physicsWorld.PhysicsPreStep += OnPhysicsPreStep;

                // Add an event listener to handle incoming match state data.
                Global.NakamaConnection.Socket.ReceivedMatchState += EnqueueOnReceivedMatchState;
                Application.Engine.PostUpdate += HandlePostUpdate;

                IsSceneSet = true;
                SetCharacterHUD();
            }
            else
            {
                IsSceneSet = false;
            }
        }

        public void  SetNetWorkData(RemotePlayerNetworkData networkData)
        {
            NetworkData = networkData;
            SetCharacterHUD();
        }

        public void SetCharacterHUD()
        {
            if(NetworkData == null || Node == null)return;


            Node characterHUDNode = Node.CreateChild("CharacterTitle");
            characterHUDNode.Position = (new Vector3(0.0f, 2.0f, 0.0f));
            characterHUDNode.Rotate(new Quaternion(0,180,0));

            characterHUDText = characterHUDNode.CreateComponent<Text3D>();
            characterHUDText.FaceCameraMode = FaceCameraMode.RotateXyz;
            characterHUDText.Text = NetworkData.User.Username;
            characterHUDText.SetFont(Application.ResourceCache.GetFont("Fonts/Anonymous Pro.sdf"), 24);//sdf, not ttf. size of font doesn't matter.
            characterHUDText.SetColor(Color.Red);
            
        }


        protected override void OnDeleted()
        {
            physicsWorld.PhysicsPreStep -= OnPhysicsPreStep;
            Application.Engine.PostUpdate -= HandlePostUpdate;
            Global.NakamaConnection.Socket.ReceivedMatchState -= EnqueueOnReceivedMatchState;
        }

        private void OnPhysicsPreStep(PhysicsPreStepEventArgs obj)
        {
            FixedUpdate(obj.TimeStep);
        }

        // update remote character transform and physics , based upon the networking data recieved from the remote character
        void FixedUpdate(float timeStep)
        {
            if (IsSceneSet == false || kinematicController == null) return;

            Vector3 oldPosition;
            Quaternion oldRotation;
            
            if(IsFirstNetWorkPacket == true)
            {
                IsFirstNetWorkPacket = false;
                kinematicController.SetTransform(NewPosition, NewRotation);
            }
            else
            {
                kinematicController.GetTransform(out oldPosition, out oldRotation);
                var position = Vector3.Lerp(oldPosition, NewPosition, 0.15f);
                var rotation = Quaternion.Slerp(oldRotation, NewRotation, 0.15f);
                kinematicController.SetTransform(position, rotation);
            }

            kinematicController.SetLinearVelocity(NewLinearVelocity);

        }


        // update Remote character Animation , based upon the key inputs recieved over the network from the remote character.

        private void HandlePostUpdate(PostUpdateEventArgs obj)
        {
            animCtrl = animCtrl ?? Node.GetComponent<AnimationController>(true);
            kinematicController = kinematicController ?? Node.GetComponent<KinematicCharacterController>(true);

            if (IsSceneSet == false || kinematicController == null) return;

            SetCharacaterOrientation();

            onGround = kinematicController.OnGround();

            // Update the in air timer. Reset if grounded
            if (!onGround)
                inAirTimer += obj.TimeStep;
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

            if (softGrounded)
            {
                // Jump. Must release jump control between jumps
                if (Controls.IsDown(Global.CtrlJump))
                {
                    if (okToJump)
                    {
                        okToJump = false;
                        jumpStarted = true;

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
                if ((softGrounded) && !moveDir.Equals(Vector3.Zero))
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

        }

        void SetCharacaterOrientation()
        {
            Node characterNode = Node;

            // Get camera lookat dir from character yaw + pitch
            Quaternion rot = characterNode.Rotation;
            Quaternion dir = rot * Quaternion.FromAxisAngle(Vector3.UnitX, Controls.Pitch);

            // Turn head to camera pitch, but limit to avoid unnatural animation
            Node headNode = characterNode.GetChild("Mutant:Head", true);
            float limitPitch = MathHelper.Clamp(Controls.Pitch, -45.0f, 45.0f);
            Quaternion headDir = rot * Quaternion.FromAxisAngle(new Vector3(1.0f, 0.0f, 0.0f), limitPitch);
            // This could be expanded to look at an arbitrary target, now just look at a point in front
            Vector3 headWorldTarget = headNode.WorldPosition + headDir * new Vector3(0.0f, 0.0f, 1.0f);
            headNode.LookAt(headWorldTarget, new Vector3(0.0f, 1.0f, 0.0f), TransformSpace.World);
            // Correct head orientation because LookAt assumes Z = forward, but the bone has been authored differently (Y = forward)
            headNode.Rotate(new Quaternion(0.0f, 90.0f, 90.0f), TransformSpace.Local);

            Node.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, Controls.Yaw);
        }



        /// <summary>
        /// Passes execution of the event handler to the main thread so that we can interact with GameObjects.
        /// </summary>
        /// <param name="matchState">The incoming match state data.</param>
        private void EnqueueOnReceivedMatchState(IMatchState matchState)
        {
            if (Application.IsActive)
                Application.InvokeOnMain(() => OnReceivedMatchState(matchState));
        }

        /// <summary>
        /// Called when receiving match data from the Nakama server.
        /// </summary>
        /// <param name="matchState">The incoming match state data.</param>
        private void OnReceivedMatchState(IMatchState matchState)
        {
            // If the incoming data is not related to this remote player, ignore it and return early.
            if (matchState.UserPresence.SessionId != NetworkData.User.SessionId)
            {
                return;
            }

            // Decide what to do based on the Operation Code of the incoming state data as defined in OpCodes.
            switch (matchState.OpCode)
            {
                case OpCodes.VelocityAndPositionAndRotation:
                    UpdateVelocityPositionRotationFromState(matchState.State);
                    break;
                case OpCodes.Input:
                    SetInputFromState(matchState.State);
                    break;
                case OpCodes.PlayerName:
                    SetPlayerNameFromState(matchState.State);
                    break;

            }
        }


        /// <summary>
        /// Converts a byte array of a UTF8 encoded JSON string into a Dictionary.
        /// </summary>
        /// <param name="state">The incoming state byte array.</param>
        /// <returns>A Dictionary containing state data as strings.</returns>
        private IDictionary<string, string> GetStateAsDictionary(byte[] state)
        {
            return Encoding.UTF8.GetString(state).FromJson<Dictionary<string, string>>();
        }


        /// <summary>
        /// Sets the appropriate input values on the PlayerMovementController based on incoming state data.
        /// </summary>
        /// <param name="state">The incoming state Dictionary.</param>
        private void SetInputFromState(byte[] state)
        {
            var stateDictionary = GetStateAsDictionary(state);

            Controls.Pitch = float.Parse(stateDictionary["pitch"]);
            Controls.Yaw = float.Parse(stateDictionary["yaw"]);
            Controls.Set(Global.CtrlForward, bool.Parse(stateDictionary["forward"]));
            Controls.Set(Global.CtrlBack, bool.Parse(stateDictionary["back"]));
            Controls.Set(Global.CtrlLeft, bool.Parse(stateDictionary["left"]));
            Controls.Set(Global.CtrlRight, bool.Parse(stateDictionary["right"]));
            Controls.Set(Global.CtrlJump, bool.Parse(stateDictionary["jump"]));

            var axisInput = new Vector2(float.Parse(stateDictionary["axis.x"]), float.Parse(stateDictionary["axis.y"]));
            Controls.ExtraData["axis_0"] = axisInput;

        }

        /// <summary>
        /// Updates the player's velocity and position based on incoming state data.
        /// </summary>
        /// <param name="state">The incoming state byte array.</param>
        private void UpdateVelocityPositionRotationFromState(byte[] state)
        {
            if (IsSceneSet == false || kinematicController == null) return;

            var stateDictionary = GetStateAsDictionary(state);

            NewLinearVelocity = new Vector3(float.Parse(stateDictionary["velocity.x"]), float.Parse(stateDictionary["velocity.y"]), float.Parse(stateDictionary["velocity.z"]));
            NewPosition = new Vector3(float.Parse(stateDictionary["position.x"]), float.Parse(stateDictionary["position.y"]), float.Parse(stateDictionary["position.z"]));
            NewRotation = new Quaternion(float.Parse(stateDictionary["rotation.x"]), float.Parse(stateDictionary["rotation.y"]), float.Parse(stateDictionary["rotation.z"]), float.Parse(stateDictionary["rotation.w"]));

        }

        
        private void SetPlayerNameFromState(byte[] state)
        {
            var stateDictionary = GetStateAsDictionary(state);

            characterHUDText.Text = stateDictionary["name"];
        }


    }
}
