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
using Urho.Gui;
using Urho.Urho2D;
using System;
using System.Runtime.InteropServices;
using Urho.Resources;

namespace AsyncLoading
{
    public class AsyncLoading : Sample
    {

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

        Scene scene;

        /// Touch utility obj.
        Touch touch;
        /// The controllable character component.
        Character character;
        /// First person camera flag.
        bool firstPerson;

        Node curLevel_ = null;
        Node nextLevel_ = null;
        string levelPathName_ = "";
        string levelLoadPending_ = "";

        Text levelText_ = null;
        Text triggerText_ = null;
        Text progressText_ = null;

        Text dbgPrimitiveText_ = null;

        AsyncLoader asyncLoader;

        [Preserve]
        public AsyncLoading() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            if (TouchEnabled)
                touch = new Touch(TouchSensitivity, Input);

            CreateAsyncLoader();

            CreateUI();

            CreateScene();

            CreateCharacter();

            if (IsMobile)
            {
                CreateScreenJoystick(E_JoystickType.OneJoyStick_OneButton);
            }


            SubscribeToEvents();

        }

        private void CreateAsyncLoader()
        {
            // asyncLoader = UrhoNetSamples.UrhoNetSamples.asyncLoader;

            asyncLoader = new AsyncLoader();
            asyncLoader.AsyncLoadProgress += HandleLoadProgress;
            asyncLoader.AsyncLoadFinished += HandleLevelLoaded;
            asyncLoader.AsyncLoadingMs = 5;
            asyncLoader.AsyncIntervalMs = 20;

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

            if (asyncLoader != null)
            {
                asyncLoader.AsyncLoadProgress -= HandleLoadProgress;
                asyncLoader.AsyncLoadFinished -= HandleLevelLoaded;
            }

            if (nextLevel_ != null)
            {
                Log.Info("Removing level " + nextLevel_.Name);
                NodeUnRegisterLoadTriggers(nextLevel_);
                nextLevel_.Remove();
                nextLevel_ = null;
            }

        }

   
        void CreateUI()
        {
            UIElement root = UI.Root;
            var cache = ResourceCache;

            XmlFile uiStyle = cache.GetXmlFile("UI/DefaultStyle.xml");
            // Set style to the UI root so that elements will inherit it
            root.SetDefaultStyle(uiStyle);
            // if (IsMobile)
            // {
            //     SimpleCreateInstructionsWithWasd("Button A to jump", Color.Black);
            // }
            // else
            // {
            //     SimpleCreateInstructionsWithWasd("Space to jump, F to toggle 1st/3rd person\nF5 to save scene, F7 to load", Color.Black);
            // }

            levelText_ = CreateTextLabel(new IntVector2(Graphics.Width / 4, 0), Color.Black);
            triggerText_ = CreateTextLabel(new IntVector2(Graphics.Width / 4, 30), Color.Black);
            progressText_ = CreateTextLabel(new IntVector2(Graphics.Width / 4, 60), Color.Black);

        }

        Text CreateTextLabel(IntVector2 position, Color color)
        {
            UIElement root = UI.Root;
            Text labelText = root.CreateChild<Text>();
            if (Graphics.Width <= 480)
            {
                labelText.SetFont(ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 10);
            }
            else if (Graphics.Width <= 1024)
            {
                labelText.SetFont(ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 16);
            }
            else if (Graphics.Width <= 1440)
            {
                labelText.SetFont(ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 20);
            }
            else
            {
                labelText.SetFont(ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 25);
            }

            labelText.TextAlignment = HorizontalAlignment.Center;
            labelText.SetColor(color);
            labelText.Position = position;

            return labelText;
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


            var xmlLevel = cache.GetXmlFile("AsyncLevel/mainLevel.xml");
            var status = scene.LoadXml(xmlLevel.GetRoot());


            var skyNode = scene.CreateChild("Sky");
            skyNode.SetScale(500.0f); // The scale actually does not matter
            var skybox = skyNode.CreateComponent<Skybox>();
            skybox.Model = ResourceCache.GetModel("Models/Box.mdl");
            skybox.SetMaterial(ResourceCache.GetMaterial("Materials/Skybox.xml"));



            levelPathName_ = "AsyncLevel/";
            xmlLevel = cache.GetXmlFile(levelPathName_ + "Level_1.xml");

            if (xmlLevel != null)
            {
                Node childNode = scene.CreateChild();
                if (childNode.LoadXml(xmlLevel.GetRoot()))
                {
                    curLevel_ = childNode;
                    NodeRegisterLoadTriggers(curLevel_);
                    levelText_.Value = ("Level: " + curLevel_.Name);
                }
            }

        }

        void NodeRegisterLoadTriggers(Node node)
        {
            if (node != null)
            {
                var children = node.GetChildrenWithTag("levelLoadTrigger", true);
                foreach (var child in children)
                {
                    child.NodeCollisionStart += HandleLoadTriggerEntered;
                }
            }
            else
            {
                Log.Error("NodeRegisterLoadTriggers - node is NULL.");
            }
        }

        void NodeUnRegisterLoadTriggers(Node node)
        {
            if (node != null)
            {
                var children = node.GetChildrenWithTag("levelLoadTrigger", true);
                foreach (var child in children)
                {
                    child.NodeCollisionStart -= HandleLoadTriggerEntered;
                }
            }
            else
            {
                Log.Error("NodeRegisterLoadTriggers - node is NULL.");
            }
        }


        private async void HandleLoadTriggerEntered(NodeCollisionStartEventArgs args)
        {
            var node = args.Body.Node;
            var tags = node.Tags;
            string levelName = string.Empty;
            string loadLevel = string.Empty;

            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (tag.StartsWith("levelName="))
                {
                    int nameLen = "levelName=".Length;
                    levelName = tag.Substring(nameLen, tag.Length - nameLen);
                }
                else if (tag.StartsWith("loadLevel="))
                {
                    int loadLen = "loadLevel=".Length;
                    loadLevel = tag.Substring(loadLen, tag.Length - loadLen);
                }
            }

            if (levelName != string.Empty && loadLevel != string.Empty)
            {
                levelText_.Value = "Level:" + levelName;
                triggerText_.Value = ("Trig info:" + " level=" + levelName + " load=" + loadLevel);

                string curLevelName = curLevel_ != null ? curLevel_.Name : string.Empty;
                string loadLevelName = nextLevel_ != null ? nextLevel_.Name : string.Empty;

                if (curLevelName != levelName)
                {
                    // swap nodes
                    if (curLevelName == loadLevel && loadLevelName == levelName)
                    {
                        Node tmpNode = curLevel_;
                        curLevel_ = nextLevel_;
                        nextLevel_ = tmpNode;
                    }
                    else
                    {
                        Log.Error("Trigger level and load names out of sequence.");
                    }
                }
                else if (loadLevelName != loadLevel)
                {
                  
                    // remove any existing level
                    if (nextLevel_ != null)
                    {
                        Log.Info("Removing level " + nextLevel_.Name);
                        NodeUnRegisterLoadTriggers(nextLevel_);
                        nextLevel_.Remove();
                        nextLevel_ = null;
                    }

                
                    String levelPathFile = levelPathName_ + loadLevel + ".xml";
                    asyncLoader.LoadAsyncNodeXml(levelPathFile);
         
                }

            }

        }

        private void HandleLevelLoaded(AsyncLoadFinishedEventArgs args)
        {
            scene.AddChild(args.Node);
            nextLevel_ = args.Node;
            NodeRegisterLoadTriggers(nextLevel_);
        }

        private void HandleLoadProgress(AsyncLoadProgressEventArgs args)
        {
            string progressStr = "progress=" + ((int)(args.Progress *100)).ToString();
            progressStr += " nodes: "+ args.LoadedNodes.ToString()+"/"+args.TotalNodes.ToString();
            progressStr += " resources: "+args.LoadedResources.ToString()+"/"+args.TotalResources.ToString();
            progressText_.Value = progressStr;
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
            character.Controls.Yaw = 90;
            character.Controls.Pitch = 1.19f;

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

                    if (IsMobile == false)
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
