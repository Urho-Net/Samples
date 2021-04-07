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
    public class MovingPlatform : Component
    {
        Node platformNode_;
        Node platformVolumdNode_;

        Vector3 initialPosition_;
        Vector3 finishPosition_;
        Vector3 directionToFinish_;
        float maxLiftSpeed_ = 3.0f;
        float minLiftSpeed_ = 1.5f;
        float curLiftSpeed_ = 0.0f;

        enum PlatformStateType
        {
            PLATFORM_STATE_START,
            PLATFORM_STATE_MOVETO_FINISH,
            PLATFORM_STATE_MOVETO_START,
            PLATFORM_STATE_FINISH
        };

        PlatformStateType platformState_;

        public MovingPlatform() { }
        public MovingPlatform(IntPtr handle) : base(handle) { }

        public void Initialize(Node platformNode, Vector3 finishPosition, bool updateBodyOnPlatform)
        {
            // get other lift components
            platformNode_ = platformNode;
            platformVolumdNode_ = platformNode_.GetChild("PlatformVolume", true);

            // positions
            initialPosition_ = platformNode_.WorldPosition;
            finishPosition_ = finishPosition;
            directionToFinish_ = Vector3.Normalize(finishPosition_ - initialPosition_);

            // state
            platformState_ = PlatformStateType.PLATFORM_STATE_MOVETO_FINISH;
            curLiftSpeed_ = maxLiftSpeed_;

            platformVolumdNode_.SetVar(new StringHash("IsMovingPlatform"), true);

            PhysicsWorld physicsWorld = Scene.GetComponent<PhysicsWorld>();
            physicsWorld.PhysicsPreStep += (args) => FixedUpdate(args.TimeStep);

        }

        void FixedUpdate(float timeStep)
        {
            Vector3 platformPos = platformNode_.Position;
            Vector3 newPos = platformPos;

            // move platform
            if (platformState_ == PlatformStateType.PLATFORM_STATE_MOVETO_FINISH)
            {
                Vector3 curDistance = finishPosition_ - platformPos;
                Vector3 curDirection = Vector3.Normalize(curDistance);
                float dist = curDistance.Length;
                float dotd = Vector3.Dot(directionToFinish_, curDirection);

                if (dotd > 0.0f)
                {
                    // slow down near the end
                    if (dist < 1.0f)
                    {
                        curLiftSpeed_ *= 0.92f;
                    }
                    curLiftSpeed_ = Math.Clamp(curLiftSpeed_, minLiftSpeed_, maxLiftSpeed_);
                    newPos += curDirection * curLiftSpeed_ * timeStep;
                }
                else
                {
                    newPos = finishPosition_;
                    curLiftSpeed_ = maxLiftSpeed_;
                    platformState_ = PlatformStateType.PLATFORM_STATE_MOVETO_START;
                }
                platformNode_.Position = newPos;
            }
            else if (platformState_ == PlatformStateType.PLATFORM_STATE_MOVETO_START)
            {
                Vector3 curDistance = initialPosition_ - platformPos;
                Vector3 curDirection = Vector3.Normalize(curDistance);
                float dist = curDistance.Length;
                float dotd = Vector3.Dot(directionToFinish_, curDirection);

                if (dotd < 0.0f)
                {
                    // slow down near the end
                    if (dist < 1.0f)
                    {
                        curLiftSpeed_ *= 0.92f;
                    }
                    curLiftSpeed_ = Math.Clamp(curLiftSpeed_, minLiftSpeed_, maxLiftSpeed_);
                    newPos += curDirection * curLiftSpeed_ * timeStep;
                }
                else
                {
                    newPos = initialPosition_;
                    curLiftSpeed_ = maxLiftSpeed_;
                    platformState_ = PlatformStateType.PLATFORM_STATE_MOVETO_FINISH;
                }

                platformNode_.Position = newPos;
            }
        }

    }

}