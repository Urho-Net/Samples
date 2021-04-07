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


namespace MovingPlatforms
{
    public class SplinePlatform : Component
    {
        Node splinePathNode_;
        SplinePath splinePath_;
        Node controlNode_;
        float rotation_ = 0.3f;

        public SplinePlatform() { }
        public SplinePlatform(IntPtr handle) : base(handle) { }

        public void Initialize(Node node)
        {
            splinePathNode_ = node;
            splinePath_ = splinePathNode_.GetComponent<SplinePath>();
            controlNode_ = splinePath_.ControlledNode;

            PhysicsWorld physicsWorld = Scene.GetComponent<PhysicsWorld>();
            physicsWorld.PhysicsPreStep += (args) => FixedUpdate(args.TimeStep);
        }

        void FixedUpdate(float timeStep)
        {
            if (splinePath_ != null)
            {
                splinePath_.Move(timeStep);

                // looped path, reset to continue
                if (splinePath_.Finished)
                {
                    splinePath_.Reset();
                }

                // rotate
                if (controlNode_ != null)
                {
                    Quaternion drot = new Quaternion(rotation_, new Vector3(0, 1, 0));
                    Quaternion nrot = controlNode_.WorldRotation;
                    controlNode_.SetWorldRotation(nrot * drot);
                }
            }
        }

    }
}