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

namespace MaterialEffects
{
    public class UVSequencer : LogicComponent
    {

        public const int UVSeq_UScroll = 0;      // 0
        public const int UVSeq_VScroll = 1;      // 1
        public const int UVSeq_UVFrame = 2;      // 2
        public const int UVSeq_SwapImage = 3;    // 3
        public UVSequencer()
        {
            uvSeqType = (0);
            uScrollSpeed = (0.0f);
            vScrollSpeed = (0.0f);
            timerFraction = (1.0f);
            rows = (0);
            cols = (0);
            numFrames = (0);
            timePerFrame = (0);
            enabled = (false);
            repeat = (false);
            curUVOffset_ = (Vector2.Zero);
            curFrameIdx_ = (0);
            curImageIdx_ = (0);
            swapTUEnum = (0);
            swapBegIdx = (0);
            swapEndIdx = (0);
            decFormat_ = (null);


        }

        public UVSequencer(IntPtr handle) : base(handle)
        {
            uvSeqType = (0);
            uScrollSpeed = (0.0f);
            vScrollSpeed = (0.0f);
            timerFraction = (1.0f);
            rows = (0);
            cols = (0);
            numFrames = (0);
            timePerFrame = (0);
            enabled = (false);
            repeat = (false);
            curUVOffset_ = (Vector2.Zero);
            curFrameIdx_ = (0);
            curImageIdx_ = (0);
            swapTUEnum = (0);
            swapBegIdx = (0);
            swapEndIdx = (0);
            decFormat_ = (null);
        }

        protected override void OnStart()
        {
            // currently only looks for billboardset and staticmodel, add more if you need
            // **sadly, the drawable base class doesn't have a virutal GetMaterial() function
            if (Node.GetComponent<BillboardSet>() != null)
            {
                drawableComponent_ = Node.GetComponent<BillboardSet>();
                componentMat_ = Node.GetComponent<BillboardSet>().Material;
            }
            else if (Node.GetComponent<StaticModel>() != null)
            {
                componentMat_ = Node.GetComponent<StaticModel>().Material;
                drawableComponent_ = Node.GetComponent<StaticModel>();
            }

            // init
            Reset();

            // auto start
            if (!enabled)
            {
                SetUpdateEventMask((uint)UpdateEvent.NoEvent);
            }
            else
            {
                 SetUpdateEventMask((uint)UpdateEvent.Fixedupdate);
            }
        }

        public bool SetEnabled(bool enable)
        {
            if (enable == enabled)
            {
                return false;
            }

            enabled = enable;

            if (enabled)
            {
                SetUpdateEventMask((uint)UpdateEvent.Fixedupdate);
            }
            else
            {
                SetUpdateEventMask((uint)UpdateEvent.NoEvent);
            }

            return true;
        }

        private void Reset()
        {
            // init common
            curFrameIdx_ = 0;
            curImageIdx_ = 0;
            curUVOffset_ = Vector2.Zero;
            seqTimer_.Reset();

            // and specifics 
            switch (uvSeqType)
            {
                case UVSeq_UScroll:
                    componentMat_.SetShaderParameter("UOffset", new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    break;

                case UVSeq_VScroll:
                    componentMat_.SetShaderParameter("VOffset", new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                    break;

                case UVSeq_UVFrame:
                    InitUVFrameSize();
                    UpdateUVFrame();
                    break;

                case UVSeq_SwapImage:
                    InitSwapDecFormat();
                    break;
            }

        }

        private void InitSwapDecFormat()
        {
            throw new NotImplementedException();
        }

        private void InitUVFrameSize()
        {
            uvFrameSize_.X = 1.0f / (float)cols;
            uvFrameSize_.Y = 1.0f / (float)rows;
        }

        protected override void OnFixedUpdate(PhysicsPreStepEventArgs e)
        {
            float timeStep = e.TimeStep;
            // skip if not in view
            if (!drawableComponent_.InView)
                return;

            // update
            switch (uvSeqType)
            {
                case UVSeq_UScroll:
                    UpdateUScroll(timeStep);
                    break;

                case UVSeq_VScroll:
                    UpdateVScroll(timeStep);
                    break;

                case UVSeq_UVFrame:
                    UpdateUVFrame();
                    break;

                case UVSeq_SwapImage:
                    UpdateSwapImage();
                    break;
            }

        }

        private void UpdateVScroll(float timeStep)
        {
            curUVOffset_.Y += vScrollSpeed + Math.Sign(vScrollSpeed) * timeStep * timerFraction;
            componentMat_.SetShaderParameter("VOffset", new Vector4(0.0f, 1.0f, 0.0f, curUVOffset_.Y));
        }

        private void UpdateUScroll(float timeStep)
        {
            curUVOffset_.X += uScrollSpeed + Math.Sign(uScrollSpeed) * timeStep * timerFraction;
            componentMat_.SetShaderParameter("UOffset", new Vector4(1.0f, 0.0f, 0.0f, curUVOffset_.X));
        }

        private void UpdateUVFrame()
        {
            if (seqTimer_.GetMSec(false) > timePerFrame)
            {
                if (++curFrameIdx_ < numFrames)
                {
                    UpdateUVFrameShader();
                }
                else
                {
                    if (repeat)
                    {
                        curFrameIdx_ = 0;

                        UpdateUVFrameShader();
                    }
                    else
                    {
                        SetUpdateEventMask(0);
                    }
                }

                seqTimer_.Reset();
            }
        }

        private void UpdateUVFrameShader()
        {
            float curRow = (float)(curFrameIdx_ / cols);
            float curCol = (float)(curFrameIdx_ % cols);

            componentMat_.SetShaderParameter("CurRowCol", new Vector2(curRow, curCol));
        }


        private void UpdateSwapImage()
        {
            if (seqTimer_.GetMSec(false) > timePerFrame)
            {
                if (++curImageIdx_ < swapEndIdx)
                {
                    UpdateSwapImageTexture();
                }
                else
                {
                    if (repeat)
                    {
                        curImageIdx_ = swapBegIdx;

                        UpdateSwapImageTexture();
                    }
                    else
                    {
                        SetUpdateEventMask(0);
                    }
                }

                seqTimer_.Reset();
            }
        }

        private void UpdateSwapImageTexture()
        {
            throw new NotImplementedException();
        }

        Drawable drawableComponent_;
        Material componentMat_;

        // type
        public int uvSeqType;
        public bool enabled;
        public bool repeat;

        // uv scroll
        public float uScrollSpeed;
        public float vScrollSpeed;
        public float timerFraction;       // something to even slow the timer (lava)

        // uv offset
        public int rows;
        public int cols;
        public int numFrames;
        public uint timePerFrame;

        // image swap - this doesn't belong but it's here to support the original demo
        public uint swapTUEnum;
        public int swapBegIdx;
        public int swapEndIdx;
        public string swapPrefixName;
        public string swapFileExt;
        public string swapDecFormat;
        public string decFormat_;

        // status update
        public Vector2 curUVOffset_;
        public Vector2 uvFrameSize_;
        public int curFrameIdx_;
        public int curImageIdx_;
        Timer seqTimer_ = new Timer();
    }
}