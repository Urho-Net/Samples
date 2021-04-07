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
using Urho.Physics;
using Urho.Audio;
using System;
using System.Collections.Generic;

namespace FlappyUrho
{
    public class Fish : Component
    {

        float verticalSpeed = 0.0f;

        float jumpDelay = 0.0f;

        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);

            CreateFish();
            Node.NodeCollisionStart += HandleCollisionStart;
            Node.NodeCollisionEnd += HandleCollisionEnd;

        }

        void CreateFish()
        {

            var cache = Application.ResourceCache;

            AnimatedModel urhoObject = Node.CreateComponent<AnimatedModel>();
            urhoObject.Model = cache.GetModel("Models/Urho.mdl");
            urhoObject.CastShadows = true;
            Node.Rotation = Global.URHO_DEFAULT_ROTATION;

            urhoObject.ApplyMaterialList();

            AnimationController animCtrl = Node.CreateComponent<AnimationController>();
            animCtrl.PlayExclusive("Models/Swim.ani", 0, true);
            animCtrl.SetSpeed("Models/Swim.ani", 1.23f);

            RigidBody body = Node.CreateComponent<RigidBody>();
            body.Mass = 1.0f;
            body.Kinematic = true;

            CollisionShape shape1 = Node.CreateComponent<CollisionShape>();
            shape1.ShapeType = ShapeType.Capsule;
            shape1.Size = new Vector3(2.0f, 3.8f, 0.0f);
            shape1.Position = new Vector3(0.0f, 0.1f, -0.2f);
            shape1.Rotation = new Quaternion(90.0f, 0.0f, 0.0f);
        }

        void HandleCollisionStart(NodeCollisionStartEventArgs args)
        {
            Node otherNode = args.OtherNode;

            if (otherNode.Name == "Net")
                Global.neededGameState = GameState.GS_DEAD;
        }

        void HandleCollisionEnd(NodeCollisionEndEventArgs args)
        {
            Node otherNode = args.OtherNode;
            var cache = Application.ResourceCache;

            if (otherNode.Name == "Barrier")
            {
                if (Global.gameState == GameState.GS_PLAY)
                {
                    Global.Score = (Global.Score + 1);
                    SoundSource soundSource = otherNode.GetOrCreateComponent<SoundSource>();
                    if (Global.Score == Global.Highscore + 1)
                    {
                        soundSource.Play(cache.GetSound("Samples/King.ogg"));
                    }
                    else
                    {
                        soundSource.Play(cache.GetSound("Samples/Pass.ogg"));
                    }
                }
            }
        }

        public void Reset()
        {
            Node.Position = Vector3.Zero;
            Node.Rotation = Global.URHO_DEFAULT_ROTATION;

            verticalSpeed = 0.0f;
            jumpDelay = 0.0f;
        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);

            var input = Application.Input;
            var cache = Application.ResourceCache;

            if (Global.gameState == GameState.GS_DEAD)
            {
                Node.Rotate(new Quaternion(0.0f, timeStep * 200.0f, -timeStep * 400.0f));

                if (Node.Position.Z > -50.0f)
                {
                    Node.Translate(new Vector3(0.0f, -10.0f * timeStep, -30.0f * timeStep), TransformSpace.World);
                }

                return;
            }

            AnimationController animationController = Node.GetComponent<AnimationController>();
            animationController.SetSpeed("Models/Swim.ani", Math.Clamp(0.1f * (verticalSpeed + 23.0f), 0.0f, 5.0f));

            if (Global.gameState != GameState.GS_PLAY)
                return;

            Vector3 pos = Node.Position;

            verticalSpeed -= timeStep * Global.GRAV_ACC;

            if (jumpDelay > 0.0f)
                jumpDelay -= timeStep;

            if ((input.GetMouseButtonPress(MouseButton.Left) || (input.NumTouches > 0)) && jumpDelay <= 0.0f)
            {
                verticalSpeed = Global.UP_SPEED;
                SoundSource soundSource = Node.GetOrCreateComponent<SoundSource>();
                soundSource.Play(cache.GetSound("Samples/Blup" + Randoms.Next(4).ToString() + ".ogg"));

                jumpDelay = 0.75f;
            }

            pos += Vector3.Up * verticalSpeed * timeStep;
            Node.Position = pos;
            float xRot = Math.Clamp(MathHelper.Lerp(0.0f, 34.0f * verticalSpeed, Math.Clamp(timeStep * 2.0f, 0.0f, 0.666f)), -13.0f, 13.0f);
            Node.Rotation = new Quaternion(xRot, -90.0f, 0.0f);

            AnimatedModel animatedModel = Node.GetComponent<AnimatedModel>();
            if (!animatedModel.InView)
                Global.neededGameState = GameState.GS_DEAD;

        }


    }

}