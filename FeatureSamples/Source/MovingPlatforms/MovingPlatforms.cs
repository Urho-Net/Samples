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
using UrhoNetSamples;
using Urho.Physics;
using System;
using Urho.Resources;

namespace MovingPlatforms
{
    public class MovingPlatforms : Sample
    {


        Touch touch;
        KinematicCharacter character;
        KinematicCharacterController kinematicCharacter;
        /// First person camera flag.
        bool firstPerson = false;
        bool drawDebug = false;

        enum CollisionLayerType
        {
            ColLayer_None = (0),

            ColLayer_Static = (1 << 0), // 1
            ColLayer_Unused = (1 << 1), // 2 -- previously thought Bullet used this as kinematic layer, turns out Bullet has a kinematic collision flag=2

            ColLayer_Character = (1 << 2), // 4

            ColLayer_Projectile = (1 << 3), // 8

            ColLayer_Platform = (1 << 4), // 16
            ColLayer_Trigger = (1 << 5), // 32

            ColLayer_Ragdoll = (1 << 6), // 64
            ColLayer_Kinematic = (1 << 7), // 128

            ColLayer_All = (0xffff)
        };


        enum CollisionMaskType
        {
            ColMask_Static = 0xFFFF - (CollisionLayerType.ColLayer_Platform | CollisionLayerType.ColLayer_Trigger),       // ~(16|32) = 65487
            ColMask_Character = 0xFFFF - (CollisionLayerType.ColLayer_Ragdoll | CollisionLayerType.ColLayer_Kinematic),                           // ~(64)    = 65471
            ColMask_Kinematic = 0xFFFF - (CollisionLayerType.ColLayer_Ragdoll | CollisionLayerType.ColLayer_Character),                           // ~(64)    = 65471
            ColMask_Projectile = 0xFFFF - (CollisionLayerType.ColLayer_Trigger),                           // ~(32)    = 65503
            ColMask_Platform = 0xFFFF - (CollisionLayerType.ColLayer_Static | CollisionLayerType.ColLayer_Trigger),         // ~(1|32)  = 65502
            ColMask_Trigger = 0xFFFF - (CollisionLayerType.ColLayer_Projectile | CollisionLayerType.ColLayer_Platform),    // ~(8|16)  = 65511
            ColMask_Ragdoll = 0xFFFF - (CollisionLayerType.ColLayer_Character),                         // ~(4)     = 65531

            ColMask_Camera = 0xFFFF - (CollisionLayerType.ColLayer_Character | CollisionLayerType.ColLayer_Projectile | CollisionLayerType.ColLayer_Trigger) // ~(4|8|32) = 65491
        };

        [Preserve]
        public MovingPlatforms() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            if (TouchEnabled)
                touch = new Touch(TouchSensitivity, Input);
            CreateScene();
            CreateCharacter();
            if (isMobile)
            {
                CreateScreenJoystick(E_JoystickType.OneJoyStick_OneButton);
            }

            if(isMobile)
            {
                SimpleCreateInstructionsWithWasd("Button A to jump", Color.Black);
            }
            else
            {
                SimpleCreateInstructionsWithWasd("Press Space to jump", Color.Black);
            }

            SubscribeToEvents();

        }

        protected override void Stop()
        {
            UnSubscribeFromEvents();
            base.Stop();
        }

        void CreateScene()
        {
            var cache = ResourceCache;

            scene = new Scene();

            CameraNode = new Node();
            Camera camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;
            Renderer.SetViewport(0, new Viewport(scene, camera, null));

            // load scene
            XmlFile xmlLevel = cache.GetXmlFile("Platforms/Scenes/playGroundTest.xml");
            scene.LoadXml(xmlLevel.GetRoot());

            // Create skybox. The Skybox component is used like StaticModel, but it will be always located at the camera, giving the
            // illusion of the box planes being far away. Use just the ordinary Box model and a suitable material, whose shader will
            // generate the necessary 3D texture coordinates for cube mapping
            var skyNode = scene.CreateChild("Sky");
            skyNode.SetScale(500.0f); // The scale actually does not matter
            var skybox = skyNode.CreateComponent<Skybox>();
            skybox.Model = ResourceCache.GetModel("Models/Box.mdl");
            skybox.SetMaterial(ResourceCache.GetMaterial("Materials/Skybox.xml"));


            // init platforms
            Lift lift = scene.CreateComponent<Lift>();
            Node liftNode = scene.GetChild("Lift", true);
            lift.Initialize(liftNode, liftNode.WorldPosition + new Vector3(0, 6.8f, 0));

            MovingPlatform movingPlatform = scene.CreateComponent<MovingPlatform>();
            Node movingPlatNode = scene.GetChild("movingPlatformDisk1", true);
            movingPlatform.Initialize(movingPlatNode, movingPlatNode.WorldPosition + new Vector3(0, 0, 20.0f), true);

            SplinePlatform splinePlatform = scene.CreateComponent<SplinePlatform>();
            Node splineNode = scene.GetChild("splinePath1", true);
            splinePlatform.Initialize(splineNode);
        }

        void CreateCharacter()
        {
            var cache = ResourceCache;

            Node objectNode = scene.CreateChild("Player");
            objectNode.Position = new Vector3(28.0f, 8.0f, -4.0f);

            // spin node
            Node adjustNode = objectNode.CreateChild("spinNode");
            adjustNode.Rotation = new Quaternion(180, new Vector3(0, 1, 0));

            // Create the rendering component + animation controller
            AnimatedModel obj = adjustNode.CreateComponent<AnimatedModel>();
            obj.Model = cache.GetModel("Platforms/Models/BetaLowpoly/Beta.mdl");
            obj.SetMaterial(0, cache.GetMaterial("Platforms/Materials/BetaBody_MAT.xml"));
            obj.SetMaterial(1, cache.GetMaterial("Platforms/Materials/BetaBody_MAT.xml"));
            obj.SetMaterial(2, cache.GetMaterial("Platforms/Materials/BetaJoints_MAT.xml"));
            obj.CastShadows = true;
            adjustNode.CreateComponent<AnimationController>();

            // Create rigidbody, and set non-zero mass so that the body becomes dynamic
            RigidBody body = objectNode.CreateComponent<RigidBody>();
            body.SetCollisionLayerAndMask((uint)CollisionLayerType.ColLayer_Character, (uint)CollisionMaskType.ColMask_Character);
            body.Kinematic = true;
            body.Trigger = true;
            body.SetAngularFactor(Vector3.Zero);
            body.CollisionEventMode = CollisionEventMode.Always;

            // Set a capsule shape for collision
            CollisionShape shape = objectNode.CreateComponent<CollisionShape>();
            shape.SetCapsule(0.7f, 1.8f, new Vector3(0.0f, 0.85f, 0.0f), Quaternion.Identity);

            // character
            character = objectNode.CreateComponent<KinematicCharacter>();
            kinematicCharacter = objectNode.CreateComponent<KinematicCharacterController>();
            kinematicCharacter.SetCollisionLayerAndMask((uint)CollisionLayerType.ColLayer_Kinematic, (uint)CollisionMaskType.ColMask_Kinematic);
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
        }


        public void UpdateJoystickInputs(Controls controls)
        {
            JoystickState joystick;
            if (screenJoystickIndex != -1 && Input.GetJoystick(screenJoystickIndex, out joystick))
            {
                controls.Set(Global.CtrlJump, joystick.GetButtonDown(JoystickState.Button_A));
                controls.ExtraData["axis_0"] =  new Vector2(joystick.GetAxisPosition(JoystickState.AxisLeft_X), joystick.GetAxisPosition(JoystickState.AxisLeft_Y));
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

                scene.GetComponent<PhysicsWorld>().RaycastSingle(ref result, new Ray(aimPoint, rayDir), rayDistance, (uint)CollisionMaskType.ColMask_Camera);
                if (result.Body != null)
                    rayDistance = Math.Min(rayDistance, result.Distance);
                rayDistance = Math.Clamp(rayDistance, Touch.CAMERA_MIN_DIST, Touch.CAMERA_MAX_DIST);

                CameraNode.Position = aimPoint + rayDir * rayDistance;
                CameraNode.Rotation = dir;
            }
        }

        void OnPostRenderUpdate(PostRenderUpdateEventArgs args)
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

        /// <summary>
        /// Set custom Joystick layout for mobile platforms
        /// </summary>
        protected override string JoystickLayoutPatch =>
            "<patch>" +
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