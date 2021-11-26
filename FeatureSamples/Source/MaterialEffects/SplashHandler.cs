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
using Urho.Resources;
using System.Collections.Generic;
using System.Reflection;

namespace MaterialEffects
{

    public enum SplashTypes : int
    {
        Splash_Invalid,
        Splash_Water,
        Splash_Ripple,
        Splash_WaterfallSplash,
        Splash_LavaBubble,
        Splash_MAX,
    };


    public class SplashData : Serializable
    {
        public SplashData()
        {
            splashType = 0;
            maxImages = 0;
            duration = 0;
            timePerFrame = 0;
            uOffset = 0.0f;
            vOffset = 0.0f;
            uInc = 0.0f;
            vInc = 0.0f;
            elapsedTime = 0;
            curImageIdx = 0;
            uCur = 0.0f;
            vCur = 0.0f;
            transparencyRate = 0.0f;
            scale = Vector3.One;
            faceCamMode = 0;
            timer = new Timer();
        }

        public SplashData(IntPtr handle) : base(handle)
        {
            splashType = 0;
            maxImages = 0;
            duration = 0;
            timePerFrame = 0;
            uOffset = 0.0f;
            vOffset = 0.0f;
            uInc = 0.0f;
            vInc = 0.0f;
            elapsedTime = 0;
            curImageIdx = 0;
            uCur = 0.0f;
            vCur = 0.0f;
            transparencyRate = 0.0f;
            scale = Vector3.One;
            faceCamMode = 0;
            timer = new Timer();
        }

        public SplashData Copy(SplashData rhs)
        {
            matFile = rhs.matFile;
            splashType = rhs.splashType;
            maxImages = rhs.maxImages;
            duration = rhs.duration;
            timePerFrame = rhs.timePerFrame;
            uOffset = rhs.uOffset;
            vOffset = rhs.vOffset;
            uInc = rhs.uInc;
            vInc = rhs.vInc;
            scaleRate = rhs.scaleRate;
            transparencyRate = rhs.transparencyRate;
            pos = rhs.pos;
            direction = rhs.direction;
            scale = rhs.scale;
            faceCamMode = rhs.faceCamMode;
            return this;
        }

        public String matFile;
        public int splashType;
        public int maxImages;
        public uint duration;
        public uint timePerFrame;
        public float uOffset;
        public float vOffset;
        public float uInc;
        public float vInc;
        public Vector3 scaleRate;
        public float transparencyRate;

        public Vector3 pos;
        public Vector3 direction;
        public Vector3 scale;
        public uint faceCamMode;

        public uint elapsedTime;
        public int curImageIdx;
        public float uCur;
        public float vCur;

        public Node node;
        public Timer timer = new Timer();

    }

    public class SplashDataList : Serializable
    {
        public String item00;
        public String item01;
        public String item02;
        public String item03;
        public String item04;
        public String item05;
        public String item06;
        public String item07;
        public String item08;
        public String item09;

        public List<String> splashList_ = new List<String>();

        public SplashDataList()
        {

        }

        public SplashDataList(IntPtr handle) : base(handle)
        {

        }

        public override bool LoadXml(XmlElement source)
        {
            base.LoadXml(source);

            if (item00 != null) splashList_.Add(item00);
            if (item01 != null) splashList_.Add(item01);
            if (item02 != null) splashList_.Add(item02);
            if (item03 != null) splashList_.Add(item03);
            if (item04 != null) splashList_.Add(item04);
            if (item05 != null) splashList_.Add(item05);
            if (item06 != null) splashList_.Add(item06);
            if (item07 != null) splashList_.Add(item07);
            if (item08 != null) splashList_.Add(item08);
            if (item09 != null) splashList_.Add(item09);

            return (splashList_.Count > 0);

        }


    }

    public class SplashHandler : LogicComponent
    {
        List<SplashData> registeredSplashList_ = new List<SplashData>();
        List<SplashData> activeSplashList_ = new List<SplashData>();

        public SplashHandler()
        {

        }

        public SplashHandler(IntPtr handle) : base(handle)
        {

        }

        protected override void OnStart()
        {
            SetUpdateEventMask((uint)UpdateEvent.Fixedupdate);
            base.OnStart();
        }

        protected override void OnFixedUpdate(PhysicsPreStepEventArgs e)
        {
            base.OnFixedUpdate(e);

            float timeStep = e.TimeStep;

            // TBD ELI , hack , causing exception on browser
            if(Application.Platform == Platforms.Web)return;
            
            for (uint i = 0; i < activeSplashList_.Count; ++i)
            {
                SplashData splashData = activeSplashList_.ToArray()[i];
                splashData.elapsedTime += (uint)(timeStep * 1000.0f);

                if (splashData.elapsedTime > splashData.duration)
                    continue;

                // update billboard
                Node splashNode = splashData.node;
                BillboardSet bbset = splashNode.GetComponent<BillboardSet>();
                BillboardWrapper bboard = bbset.GetBillboardSafe(0);
                Material mat = bbset.Material;

                switch (splashData.splashType)
                {
                    case (int)SplashTypes.Splash_Water:
                        break;

                    case (int)SplashTypes.Splash_Ripple:
                        {
                            bboard.Size = bboard.Size * new Vector2(splashData.scaleRate.X, splashData.scaleRate.Y);
                            bbset.Commit();
                            Color matCol = mat.GetShaderParameter("MatDiffColor");
                            matCol.A *= splashData.transparencyRate;
                            mat.SetShaderParameter("MatDiffColor", matCol);
                        }
                        break;

                    case (int)SplashTypes.Splash_WaterfallSplash:
                        break;

                    case (int)SplashTypes.Splash_LavaBubble:
                        break;

                }
            }

            // remove expired
            for (uint i = 0; i < activeSplashList_.Count; ++i)
            {
                SplashData splashData = activeSplashList_.ToArray()[i];

                if (splashData.elapsedTime > splashData.duration)
                {
                    Scene.RemoveChild(splashData.node);
                    activeSplashList_.Remove(splashData);
                    --i;
                }
            }

        }

        public bool LoadSplashList(String strlist)
        {
            var cache = Application.ResourceCache;

            SplashDataList splashList = new SplashDataList();

            using (var xmlList = cache.GetXmlFile(strlist))
            {
                if (xmlList != null && splashList.LoadXml(xmlList.GetRoot()))
                {
                    for (uint i = 0; i < splashList.splashList_.Count; ++i)
                    {
                        String strSplashData = splashList.splashList_.ToArray()[i];

                        if (strSplashData != string.Empty)
                        {
                            using (var xmlFile = cache.GetXmlFile(strSplashData))
                            {
                                if (xmlFile != null)
                                {
                                    SplashData splashData = new SplashData();
                                    if (splashData.LoadXml(xmlFile.GetRoot()))
                                        registeredSplashList_.Add(splashData);
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }


        public void OnSplashEvent(Vector3 pos, Vector3 dir, SplashTypes type)
        {
            int sptype = (int)type;

            // create splash
            for (uint i = 0; i < registeredSplashList_.Count; ++i)
            {
                var registeredSplashList = registeredSplashList_.ToArray();

                if (registeredSplashList[i].splashType == sptype)
                {
                    SplashData newSplashData = new SplashData();
                    newSplashData.Copy(registeredSplashList[i]);

                    newSplashData.node = Scene.CreateChild();
                    newSplashData.node.Position = (pos);
                    newSplashData.node.SetDirection(Vector3.Down);

                    if (CreateDrawableObj(newSplashData))
                    {
                        newSplashData.timer.Reset();
                        activeSplashList_.Add(newSplashData);
                    }
                    break;
                }
            }
        }

        private bool CreateDrawableObj(SplashData splashData)
        {
            var cache = Application.ResourceCache;
            switch (splashData.splashType)
            {
                case (int)SplashTypes.Splash_Water:
                    break;

                case (int)SplashTypes.Splash_Ripple:
                    {
                        BillboardSet bbset = splashData.node.CreateComponent<BillboardSet>();
                        bbset.NumBillboards = 1;
                        Material mat = cache.GetMaterial(splashData.matFile);
                        bbset.Material = mat.Clone();
                        bbset.FaceCameraMode = ((FaceCameraMode)splashData.faceCamMode);
                        BillboardWrapper bboard = bbset.GetBillboardSafe(0);
                        bboard.Size = new Vector2(splashData.scale.X, splashData.scale.Y);
                        bboard.Enabled = true;
                    }
                    break;

                case (int)SplashTypes.Splash_WaterfallSplash:
                    break;

                case (int)SplashTypes.Splash_LavaBubble:
                    break;

            }

            return true;
        }
    }
}
