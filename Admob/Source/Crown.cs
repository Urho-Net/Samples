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
using System;
using System.Collections.Generic;

namespace Admob
{
    public class Crown : Component
    {
        public override void OnAttachedToNode(Node node)
        {
            base.OnAttachedToNode(node);

            var cache = Application.ResourceCache;

            Node.Position = Vector3.Right * 19.0f;

            Node.Rotation = new Quaternion(23.5f, Vector3.Forward);

            StaticModel crownModel = Node.CreateComponent<StaticModel>();
            crownModel.Model = cache.GetModel("Models/Crown.mdl");
            crownModel.CastShadows = true;
            crownModel.ApplyMaterialList();

            Vector3 v = Vector3.Normalize(Vector3.Left);

        }

        protected override void OnUpdate(float timeStep)
        {
            base.OnUpdate(timeStep);

            if (Global.Score > Global.Highscore)
            {

                Node.Position = Vector3.Lerp(Node.Position, Global.CAMERA_DEFAULT_POS, Math.Clamp(2.0f * timeStep, 0.0f, 1.0f));
                Node.Rotation = Quaternion.Slerp(Node.Rotation, new Quaternion(90.0f, Vector3.Right), Math.Clamp(3.0f * timeStep, 0.0f, 1.0f));
                Node.Rotate(new Quaternion(235.0f * timeStep, Vector3.Up), TransformSpace.Local);

            }
            else
            {

                Node.Rotate(new Quaternion(timeStep * 23.0f, Vector3.Up), TransformSpace.World);
                Node.Rotate(new Quaternion(timeStep * 23.0f, Vector3.Up), TransformSpace.Local);

                float x = 2.3f + ((Global.Highscore == 0) ? 1.0f : 0.0f) + 25.0f * (Global.Highscore - Global.Score) / (float)Math.Max(Global.Highscore, 1);
                float y = Node.Scene.GetChild("Urho").Position.Y - Node.Position.Y;
                Vector3 targetPos = new Vector3(x, y, Node.Position.Z);

                Node.Position = new Vector3(0.01f * (targetPos + 99.0f * Node.Position));
            }

        }

        public void Reset()
        {
            Node.Position = Vector3.Right * 19.0f;
            Node.Rotation = new Quaternion(23.5f, Vector3.Forward);
        }

    }

}