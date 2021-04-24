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

using UrhoNetSamples;
using Urho;
using Urho.Physics;
using Urho.Gui;
using System;
using System.Runtime.InteropServices;

namespace MaterialEffects
{
    public class MaterialEffects : Sample
    {

        enum EmissionState
        {
            EmissionState_R,
            EmissionState_None1,
            EmissionState_G,
            EmissionState_None2,
            EmissionState_B,
            EmissionState_None3,
            EmissionState_MAX,
        };

        const int Max_Lightmaps = 3;
        public const float CameraMinDist = 1.0f;
        public const float CameraInitialDist = 5.0f;
        public const float CameraMaxDist = 20.0f;

        public const float GyroscopeThreshold = 0.1f;

        public const int CtrlForward = 1;
        public const int CtrlBack = 2;
        public const int CtrlLeft = 4;
        public const int CtrlRight = 8;
        public const int CtrlJump = 16;

        public const float MoveForce = 0.8f;
        public const float InairMoveForce = 0.02f;
        public const float BrakeForce = 0.2f;
        public const float JumpForce = 7.0f;
        public const float YawSensitivity = 0.1f;
        public const float InairThresholdTime = 0.1f;

        bool drawDebug = false;

        /// Touch utility obj.
        Touch touch;
        /// The controllable character component.
        Character character;
        /// First person camera flag.
        bool firstPerson;
        PhysicsWorld physicsWorld;

        // emission
        Color emissionColor_ = Color.Black;
        int emissionState_ = (int)EmissionState.EmissionState_R;


        // lightmap
        String lightmapPathName_ = "MaterialEffects/Textures/checkers-lightmap";
        Timer lightmapTimer_ = new Timer();
        int lightmapIdx_;

        // vcol
        int vcolColorIdx_;
        uint vertIdx_;
        Timer vcolTimer_ = new Timer();


        [Preserve]
        public MaterialEffects() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            if (TouchEnabled)
                touch = new Touch(TouchSensitivity, Input);

            //  LogSharp.LogLevel = LogSharpLevel.Debug;

            CreateScene();

            CreateSequencers();

            CreateWaterRefection();

            CreateCharacter();

            if (isMobile)
            {
                CreateScreenJoystick(E_JoystickType.OneJoyStick_OneButton);
            }

            if (isMobile)
            {
                SimpleCreateInstructionsWithWasd("Button A to jump", Color.Black);
            }
            else
            {
                SimpleCreateInstructionsWithWasd("Space to jump, F to toggle 1st/3rd person\nF5 to save scene, F7 to load", Color.Black);
            }
            SubscribeToEvents();

        }


        protected override void Stop()
        {
            UnSubscribeFromEvents();
            base.Stop();
        }



        void SubscribeToEvents()
        {
            Engine.PostUpdate += OnPostUpdate;
            Engine.PostRenderUpdate += OnPostRenderUpdate;
        }

        void UnSubscribeFromEvents()
        {
            Engine.PostUpdate -= OnPostUpdate;
            Engine.PostRenderUpdate -= OnPostRenderUpdate;
        }

        void CreateScene()
        {
            var cache = ResourceCache;
            var renderer = Renderer;
            var graphics = Graphics;

            scene = new Scene();

            CameraNode = new Node();
            Camera camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;

            var viewport = new Viewport(scene, camera, null);
            Renderer.SetViewport(0, viewport);

            // post-process glow
            RenderPath effectRenderPath = viewport.RenderPath.Clone();
            effectRenderPath.Append(cache.GetXmlFile("PostProcess/Glow.xml"));

            // set BlurHInvSize to proper value
            // **note** be sure to do this if screen size changes (not done for this demo)
            effectRenderPath.SetShaderParameter("BlurHInvSize", new Vector2(1.0f / (float)(graphics.Width), 1.0f / (float)(graphics.Height)));
            effectRenderPath.SetEnabled("Glow", true);
            viewport.RenderPath = effectRenderPath;

            // load scene
            var xmlLevel = cache.GetXmlFile("MaterialEffects/Level1.xml");
            scene.LoadXml(xmlLevel.GetRoot());

        }

        void CreateCharacter()
        {
            var cache = ResourceCache;
            Node spawnNode = scene.GetChild("playerSpawn");
            Node objectNode = scene.CreateChild("Player");
            objectNode.Position = spawnNode.Position;

            // spin node
            Node adjustNode = objectNode.CreateChild("spinNode");
            adjustNode.Rotation = new Quaternion(180, new Vector3(0, 1, 0));

            // Create the rendering component + animation controller
            AnimatedModel obj = adjustNode.CreateComponent<AnimatedModel>();
            obj.SetModel(cache.GetModel("Platforms/Models/BetaLowpoly/Beta.mdl"));
            obj.SetMaterial(0, cache.GetMaterial("Platforms/Materials/BetaBody_MAT.xml"));
            obj.SetMaterial(1, cache.GetMaterial("Platforms/Materials/BetaBody_MAT.xml"));
            obj.SetMaterial(2, cache.GetMaterial("Platforms/Materials/BetaJoints_MAT.xml"));
            obj.CastShadows = true;
            adjustNode.CreateComponent<AnimationController>();

            // Create rigidbody, and set non-zero mass so that the body becomes dynamic
            RigidBody body = objectNode.CreateComponent<RigidBody>();
            body.CollisionLayer = (uint)Global.CollisionLayerType.ColLayer_Character;
            body.CollisionMask = (uint)Global.CollisionMaskType.ColMask_Character;
            body.Mass = 1.0f;

            body.SetAngularFactor(Vector3.Zero);
            body.CollisionEventMode = CollisionEventMode.Always;

            // Set a capsule shape for collision
            CollisionShape shape = objectNode.CreateComponent<CollisionShape>();
            shape.SetCapsule(0.7f, 1.8f, new Vector3(0.0f, 0.94f, 0.0f), Quaternion.Identity);

            // character
            character = objectNode.CreateComponent<Character>();

            // set rotation
            character.Controls.Yaw = -199.7f;
            character.Controls.Pitch = 1.19f;

        }

        private void CreateWaterRefection()
        {

        }

        private void CreateSequencers()
        {

        }


        protected override void OnUpdate(float timeStep)
        {
            Input input = Input;

            if (character != null)
            {
                // Clear previous controls
                character.Controls.Set(Global.CtrlForward | Global.CtrlBack | Global.CtrlLeft | Global.CtrlRight | Global.CtrlJump, false);

                // Update controls using touch utility class
                // touch?.UpdateTouches(character.Controls);
                UpdateJoystickInputs(character.Controls);

                // Update controls using keys
                if (UI.FocusElement == null)
                {
                    if (touch == null || !touch.UseGyroscope)
                    {
                        character.Controls.Set(Global.CtrlForward, input.GetKeyDown(Key.W));
                        character.Controls.Set(Global.CtrlBack, input.GetKeyDown(Key.S));
                        character.Controls.Set(Global.CtrlLeft, input.GetKeyDown(Key.A));
                        character.Controls.Set(Global.CtrlRight, input.GetKeyDown(Key.D));
                    }

                    if (isMobile == false)
                    {
                        character.Controls.Set(Global.CtrlJump, input.GetKeyDown(Key.Space));
                    }

                    // Add character yaw & pitch from the mouse motion or touch input
                    if (TouchEnabled)
                    {
                        for (uint i = 0; i < input.NumTouches; ++i)
                        {
                            TouchState state = input.GetTouch(i);
                            if (state.TouchedElement == null)    // Touch on empty space
                            {
                                Camera camera = CameraNode.GetComponent<Camera>();
                                if (camera == null)
                                    return;

                                var graphics = Graphics;
                                character.Controls.Yaw += TouchSensitivity * camera.Fov / graphics.Height * state.Delta.X;
                                character.Controls.Pitch += TouchSensitivity * camera.Fov / graphics.Height * state.Delta.Y;
                            }
                        }
                    }
                    else
                    {
                        character.Controls.Yaw += (float)input.MouseMove.X * Global.YawSensitivity;
                        character.Controls.Pitch += (float)input.MouseMove.Y * Global.YawSensitivity;
                    }
                    // Limit pitch
                    character.Controls.Pitch = MathHelper.Clamp(character.Controls.Pitch, -80.0f, 80.0f);

                    // Switch between 1st and 3rd person
                    if (input.GetKeyPress(Key.F))
                        firstPerson = !firstPerson;

                    // Turn on/off gyroscope on mobile platform
                    if (touch != null && input.GetKeyPress(Key.G))
                        touch.UseGyroscope = !touch.UseGyroscope;
                }

                // Set rotation already here so that it's updated every rendering frame instead of every physics frame
                if (character != null)
                    character.Node.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, character.Controls.Yaw);
            }

            // Toggle debug geometry with space
            if (input.GetKeyPress(Key.M))
                drawDebug = !drawDebug;

            // update material effects
            UpdateEmission(timeStep);
            UpdateLightmap(timeStep);
            UpdateVertexColor(timeStep);
        }

        private void UpdateVertexColor(float timeStep)
        {
            int iNumColors = 13;
            uint[] uColors = {
                0xFF00D7FF,  // 0     Gold           = 0xFFFFD700
                0xFF20A5DA,  // 1     Goldenrod      = 0xFFDAA520
                0xFFB9DAFF,  // 2     Peachpuff      = 0xFFFFDAB9
                0xFF008000,  // 3     Green          = 0xFF008000
                0xFF2FFFAD,  // 4     GreenYellow    = 0xFFADFF2F
                0xFFF0FFF0,  // 5     Honeydew       = 0xFFF0FFF0
                0xFFB469FF,  // 6     HotPink        = 0xFFFF69B4
                0xFF5C5CCD,  // 7     IndianRed      = 0xFFCD5C5C
                0xFF82004B,  // 8     Indigo         = 0xFF4B0082
                0xFFD0E040,  // 9     Turquoise      = 0xFF40E0D0
                0xFF8CE6F0,  // 10    Khaki          = 0xFFF0E68C
                0xFF9370DB,  // 11    PaleVioletRed  = 0xFFDB7093
                0xFF1E69D2,  // 12    Chocolate      = 0xFFD2691E
            };

            if (vcolTimer_.GetMSec(false) > 1)
            {
                Node vcolsphNode = scene.GetChild("vcolSphere");
                if (vcolsphNode != null)
                {
                    if (!vcolsphNode.GetComponent<StaticModel>().IsInView(CameraNode.GetComponent<Camera>()))
                        return;

                    Model model = vcolsphNode.GetComponent<StaticModel>().Model;
                    Geometry geometry = model.GetGeometry(0, 0);
                    var vbuffers = model.VertexBuffers;
                    VertexBuffer vbuffer = vbuffers[0];
                    uint elementMask = vbuffer.GetElementMask();

                    if ((elementMask & (uint)ElementMask.Color) == 0)
                    {
                        return;
                    }

                    uint vertexSize = vbuffer.VertexSize;
                    uint numVertices = vbuffer.VertexCount;
                    IntPtr vertexData = vbuffer.Lock(0, vbuffer.VertexCount);

                    if (vertexData != IntPtr.Zero)
                    {
                        IntPtr dataAlign = IntPtr.Add(vertexData, (int)(vertIdx_ * vertexSize)); 

                        int Vector3_Size = Marshal.SizeOf(typeof(Vector3));

                        if ((elementMask & (uint)ElementMask.Position) != 0)
                            dataAlign = IntPtr.Add(dataAlign, Vector3_Size);

                        if ((elementMask & (uint)ElementMask.Normal) != 0)
                            dataAlign = IntPtr.Add(dataAlign, Vector3_Size);

                        // TBD ELI , uint marshaling is not supported by the system runtime , I have to add support for it 
                        Marshal.WriteInt32(dataAlign, (int)uColors[vcolColorIdx_]);

                        vbuffer.Unlock();
                    }

                    if (++vertIdx_ >= numVertices)
                    {
                        vertIdx_ = 0;
                        vcolColorIdx_ = ++vcolColorIdx_ % iNumColors;
                    }

                }

                vcolTimer_.Reset();
            }
        }

        private void UpdateLightmap(float timeStep)
        {
            if (lightmapTimer_.GetMSec(false) > 1000)
            {
                Node lightmapNode = scene.GetChild("lightmapSphere");
                lightmapIdx_ = ++lightmapIdx_ % Max_Lightmaps;

                if (lightmapNode != null)
                {
                    String diffName = lightmapPathName_ + String.Format("{0:D3}.png", lightmapIdx_);

                    var cache = ResourceCache;
                    Material mat = lightmapNode.GetComponent<StaticModel>().Material;
                    var texture = cache.GetTexture2D(diffName);
                    mat.SetTexture(TextureUnit.Emissive, texture);
                }

                lightmapTimer_.Reset();
            }
        }

        private void UpdateEmission(float timeStep)
        {
            Node emissionNode = scene.GetChild("emissionSphere1");
            if (emissionNode != null)
            {
                if (!emissionNode.GetComponent<StaticModel>().IsInView(CameraNode.GetComponent<Camera>()))
                    return;
            }

            timeStep *= 2.0f;
            switch (emissionState_)
            {
                case (int)EmissionState.EmissionState_R:
                    emissionColor_ = emissionColor_.Lerp(Color.Red, timeStep);
                    if (emissionColor_.R > 0.99f)
                    {
                        emissionState_ = (int)EmissionState.EmissionState_None1;
                    }
                    break;

                case (int)EmissionState.EmissionState_G:
                    emissionColor_ = emissionColor_.Lerp(Color.Green, timeStep);
                    if (emissionColor_.G > 0.99f)
                    {
                        emissionState_ = (int)EmissionState.EmissionState_None2;
                    }
                    break;

                case (int)EmissionState.EmissionState_B:
                    emissionColor_ = emissionColor_.Lerp(Color.Blue, timeStep);
                    if (emissionColor_.B > 0.99f)
                    {
                        emissionState_ = (int)EmissionState.EmissionState_None3;
                    }
                    break;

                case (int)EmissionState.EmissionState_None1:
                    emissionColor_ = emissionColor_.Lerp(Color.Black, timeStep);
                    if (emissionColor_.SumRGB() < 0.01f)
                    {
                        emissionState_ = ++emissionState_ % (int)EmissionState.EmissionState_MAX;
                    }
                    break;

                case (int)EmissionState.EmissionState_None2:
                    emissionColor_ = emissionColor_.Lerp(Color.Black, timeStep);
                    if (emissionColor_.SumRGB() < 0.01f)
                    {
                        emissionState_ = ++emissionState_ % (int)EmissionState.EmissionState_MAX;
                    }
                    break;

                case (int)EmissionState.EmissionState_None3:

                    emissionColor_ = emissionColor_.Lerp(Color.Black, timeStep);
                    if (emissionColor_.SumRGB() < 0.01f)
                    {
                        emissionState_ = ++emissionState_ % (int)EmissionState.EmissionState_MAX;
                    }

                    break;
            }

            if (emissionNode != null)
            {
                Material mat = emissionNode.GetComponent<StaticModel>().Material;
                mat.SetShaderParameter("MatEmissiveColor", emissionColor_);
            }
        }

        void OnPostUpdate(PostUpdateEventArgs args)
        {
            if (character == null)
                return;

            Node characterNode = character.Node;
            Quaternion rot = characterNode.Rotation;
            Quaternion dir = rot * new Quaternion(character.Controls.Pitch, Vector3.Right);

            {
                Vector3 aimPoint = characterNode.Position + rot * new Vector3(0.0f, 1.7f, 0.0f);
                Vector3 rayDir = dir * Vector3.Back;
                float rayDistance = (touch != null) ? touch.CameraDistance : Touch.CAMERA_INITIAL_DIST;
                PhysicsRaycastResult result = new PhysicsRaycastResult();

                scene.GetComponent<PhysicsWorld>().RaycastSingle(ref result, new Ray(aimPoint, rayDir), rayDistance, (uint)Global.CollisionMaskType.ColMask_Camera);
                if (result.Body != null)
                    rayDistance = Math.Min(rayDistance, result.Distance);
                rayDistance = Math.Clamp(rayDistance, Touch.CAMERA_MIN_DIST, Touch.CAMERA_MAX_DIST);

                CameraNode.Position = aimPoint + rayDir * rayDistance;
                CameraNode.Rotation = dir;
            }
        }
        private void OnPostRenderUpdate(PostRenderUpdateEventArgs obj)
        {
            if (drawDebug)
            {
                scene.GetComponent<PhysicsWorld>().DrawDebugGeometry(true);
                DebugRenderer dbgRenderer = scene.GetComponent<DebugRenderer>();

                Node objectNode = scene.GetChild("Player");
                if (objectNode != null)
                {
                    dbgRenderer.AddSphere(new Sphere(objectNode.WorldPosition, 0.1f), Color.Yellow);
                }
            }
        }

        public void UpdateJoystickInputs(Controls controls)
        {
            JoystickState joystick;
            if (screenJoystickIndex != -1 && Input.GetJoystick(screenJoystickIndex, out joystick))
            {
                controls.Set(CtrlJump, joystick.GetButtonDown(JoystickState.Button_A));
                controls.ExtraData["axis_0"] = new Vector2(joystick.GetAxisPosition(JoystickState.AxisLeft_X), joystick.GetAxisPosition(JoystickState.AxisLeft_Y));
            }
        }


        /// <summary>
        /// Set custom Joystick layout for mobile platforms
        /// </summary>
        protected override string JoystickLayoutPatch =>
            "<patch>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">1st/3rd</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"F\" />" +
            "        </element>" +
            "    </add>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Jump</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"SPACE\" />" +
            "        </element>" +
            "    </add>" +
            "</patch>";
    }
}
