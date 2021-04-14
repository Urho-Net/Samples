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
using Urho.Gui;
using System;

namespace KinematicCharacterDemo
{
    public class KinematicCharacterDemo : Sample
    {

        /// Touch utility object.
        Touch touch;
        /// The controllable character component.
        KinematicCharacter character;

        /// First person camera flag.
        bool firstPerson;
        PhysicsWorld physicsWorld;

        [Preserve]
        public KinematicCharacterDemo() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            if (TouchEnabled)
                touch = new Touch(TouchSensitivity, Input);
            CreateScene();
            CreateCharacter();
            if (isMobile)
            {
                SimpleCreateInstructionsWithWasd("Button to jump, Button to toggle 1st/3rd person", Color.Black);
            }
            else
            {
                SimpleCreateInstructionsWithWasd("Space to jump, F to toggle 1st/3rd person", Color.Black);
            }
            SubscribeToEvents();

        }

        protected override void Stop()
        {
            base.Stop();
            UnSubscribeFromEvents();
        }



        void SubscribeToEvents()
        {
            Engine.PostUpdate += (HandlePostUpdate);
        }

        void UnSubscribeFromEvents()
        {
            Engine.PostUpdate -= (HandlePostUpdate);
        }

        void HandlePostUpdate(PostUpdateEventArgs args)
        {

            if (character == null)
                return;

            Node characterNode = character.Node;

            // Get camera lookat dir from character yaw + pitch
            Quaternion rot = characterNode.Rotation;
            Quaternion dir = rot * Quaternion.FromAxisAngle(Vector3.UnitX, character.Controls.Pitch);

            // Turn head to camera pitch, but limit to avoid unnatural animation
            Node headNode = characterNode.GetChild("Mutant:Head", true);
            float limitPitch = MathHelper.Clamp(character.Controls.Pitch, -45.0f, 45.0f);
            Quaternion headDir = rot * Quaternion.FromAxisAngle(new Vector3(1.0f, 0.0f, 0.0f), limitPitch);
            // This could be expanded to look at an arbitrary target, now just look at a point in front
            Vector3 headWorldTarget = headNode.WorldPosition + headDir * new Vector3(0.0f, 0.0f, 1.0f);
            headNode.LookAt(headWorldTarget, new Vector3(0.0f, 1.0f, 0.0f), TransformSpace.World);
            // Correct head orientation because LookAt assumes Z = forward, but the bone has been authored differently (Y = forward)
            headNode.Rotate(new Quaternion(0.0f, 90.0f, 90.0f), TransformSpace.Local);

            if (firstPerson)
            {
                CameraNode.Position = headNode.WorldPosition + rot * new Vector3(0.0f, 0.15f, 0.2f);
                CameraNode.Rotation = dir;
            }
            else
            {
                // Third person camera: position behind the character
                Vector3 aimPoint = characterNode.Position + rot * new Vector3(0.0f, 1.7f, 0.0f);

                // Collide camera ray with static physics objects (layer bitmask 2) to ensure we see the character properly
                Vector3 rayDir = dir * new Vector3(0f, 0f, -1f);
                float rayDistance = touch != null ? touch.CameraDistance : Global.CameraInitialDist;

                PhysicsRaycastResult result = new PhysicsRaycastResult();
                scene.GetComponent<PhysicsWorld>().RaycastSingle(ref result, new Ray(aimPoint, rayDir), rayDistance, 2);
                if (result.Body != null)
                    rayDistance = Math.Min(rayDistance, result.Distance);
                rayDistance = MathHelper.Clamp(rayDistance, Global.CameraMinDist, Global.CameraMaxDist);

                CameraNode.Position = aimPoint + rayDir * rayDistance;
                CameraNode.Rotation = dir;
            }
        }

        protected override void OnUpdate(float timeStep)
        {
            Input input = Input;

            if (character != null)
            {
                // Clear previous controls
                character.Controls.Set(Global.CtrlForward | Global.CtrlBack | Global.CtrlLeft | Global.CtrlRight | Global.CtrlJump, false);

                // Update controls using touch utility class
                touch?.UpdateTouches(character.Controls);

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
                    character.Controls.Set(Global.CtrlJump, input.GetKeyDown(Key.Space));

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

                    /* TBD ELI , not working for KinematicCharacter ,  needs some debug session
                    if (!isMobile && input.GetKeyPress(Key.F5))
                    {
                        string path = FileSystem.CurrentDir + "Assets/Data/Scenes";
                        if (!FileSystem.DirExists(path))
                        {
                            FileSystem.CreateDir(path);
                        }
                        scene.SaveXml(path + "/KinematicCharacterDemo.xml");
                    }
                    if (!isMobile && input.GetKeyPress(Key.F7))
                    {
                        string path = FileSystem.CurrentDir + "Assets/Data/Scenes/KinematicCharacterDemo.xml";
                        if (FileSystem.FileExists(path))
                        {
                            scene.LoadXml(path);
                            Node characterNode = scene.GetChild("Jack", true);
                            if (characterNode != null)
                            {
                                character = characterNode.GetComponent<KinematicCharacter>();
                            }
                            physicsWorld = scene.CreateComponent<PhysicsWorld>();
                        }
                    }
                    */
                }

                // Set rotation already here so that it's updated every rendering frame instead of every physics frame
                if (character != null)
                    character.Node.Rotation = Quaternion.FromAxisAngle(Vector3.UnitY, character.Controls.Yaw);
            }
        }

        void CreateScene()
        {
            var cache = ResourceCache;

            scene = new Scene();

            // Create scene subsystem components
            scene.CreateComponent<Octree>();
            physicsWorld = scene.CreateComponent<PhysicsWorld>();

            // Create camera and define viewport. We will be doing load / save, so it's convenient to create the camera outside the scene,
            // so that it won't be destroyed and recreated, and we don't have to redefine the viewport on load
            CameraNode = new Node();
            Camera camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;
            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));

            // Create static scene content. First create a zone for ambient lighting and fog control
            Node zoneNode = scene.CreateChild("Zone");
            Zone zone = zoneNode.CreateComponent<Zone>();
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 100.0f;
            zone.FogEnd = 300.0f;
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));

            // Create a directional light with cascaded shadow mapping
            Node lightNode = scene.CreateChild("DirectionalLight");
            lightNode.SetDirection(new Vector3(0.3f, -0.5f, 0.425f));
            Light light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);
            light.SpecularIntensity = 0.5f;

            // Create the floor object
            Node floorNode = scene.CreateChild("Floor");
            floorNode.Position = new Vector3(0.0f, -0.5f, 0.0f);
            floorNode.Scale = new Vector3(200.0f, 1.0f, 200.0f);
            StaticModel sm = floorNode.CreateComponent<StaticModel>();
            sm.Model = cache.GetModel("Models/Box.mdl");
            sm.SetMaterial(cache.GetMaterial("Materials/Stone.xml"));

            RigidBody body = floorNode.CreateComponent<RigidBody>();
            // Use collision layer bit 2 to mark world scenery. This is what we will raycast against to prevent camera from going
            // inside geometry
            body.CollisionLayer = 2;
            CollisionShape shape = floorNode.CreateComponent<CollisionShape>();
            shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);

            // Create mushrooms of varying sizes
            uint numMushrooms = 60;
            for (uint i = 0; i < numMushrooms; ++i)
            {
                Node objectNode = scene.CreateChild("Mushroom");
                objectNode.Position = new Vector3(NextRandom(180.0f) - 90.0f, 0.0f, NextRandom(180.0f) - 90.0f);
                objectNode.Rotation = new Quaternion(0.0f, NextRandom(360.0f), 0.0f);
                objectNode.SetScale(2.0f + NextRandom(5.0f));
                StaticModel o = objectNode.CreateComponent<StaticModel>();
                o.Model = cache.GetModel("Models/Mushroom.mdl");
                o.SetMaterial(cache.GetMaterial("Materials/Mushroom.xml"));
                o.CastShadows = true;

                body = objectNode.CreateComponent<RigidBody>();
                body.CollisionLayer = 2;
                shape = objectNode.CreateComponent<CollisionShape>();
                shape.SetTriangleMesh(o.Model, 0, Vector3.One, Vector3.Zero, Quaternion.Identity);
            }

            // Create movable boxes. Let them fall from the sky at first
            const uint numBoxes = 100;
            for (uint i = 0; i < numBoxes; ++i)
            {
                float scale = NextRandom(2.0f) + 0.5f;

                Node objectNode = scene.CreateChild("Box");
                objectNode.Position = new Vector3(NextRandom(180.0f) - 90.0f, NextRandom(10.0f) + 10.0f, NextRandom(180.0f) - 90.0f);
                objectNode.Rotation = new Quaternion(NextRandom(360.0f), NextRandom(360.0f), NextRandom(360.0f));
                objectNode.SetScale(scale);
                StaticModel o = objectNode.CreateComponent<StaticModel>();
                o.Model = cache.GetModel("Models/Box.mdl");
                o.SetMaterial(cache.GetMaterial("Materials/Stone.xml"));
                o.CastShadows = true;

                body = objectNode.CreateComponent<RigidBody>();
                body.CollisionLayer = 2;
                // Bigger boxes will be heavier and harder to move
                body.Mass = scale * 2.0f;
                shape = objectNode.CreateComponent<CollisionShape>();
                shape.SetBox(Vector3.One, Vector3.Zero, Quaternion.Identity);
            }
        }

        void CreateCharacter()
        {
            var cache = ResourceCache;

            Node objectNode = scene.CreateChild("Jack");
            objectNode.Position = (new Vector3(0.0f, 1.0f, 0.0f));

            // spin node
            Node adjustNode = objectNode.CreateChild("AdjNode");
            adjustNode.Rotation = (new Quaternion(0, 180, 0));

            // Create the rendering component + animation controller
            AnimatedModel obj = adjustNode.CreateComponent<AnimatedModel>();
            obj.Model = cache.GetModel("Models/Mutant/Mutant.mdl");
            obj.SetMaterial(cache.GetMaterial("Models/Mutant/Materials/mutant_M.xml"));
            obj.CastShadows = true;
            adjustNode.CreateComponent<AnimationController>();

            // Set the head bone for manual control
            obj.Skeleton.GetBoneSafe("Mutant:Head").Animated = false;

            // Create rigidbody, and set non-zero mass so that the body becomes dynamic
            RigidBody body = objectNode.CreateComponent<RigidBody>();
            body.CollisionLayer = 1;
            body.Kinematic = true;
            body.Trigger = true;

            // Set zero angular factor so that physics doesn't turn the character on its own.
            // Instead we will control the character yaw manually
            body.SetAngularFactor(Vector3.Zero);

            // Set the rigidbody to signal collision also when in rest, so that we get ground collisions properly
            body.CollisionEventMode = CollisionEventMode.Always;

            // Set a capsule shape for collision
            CollisionShape shape = objectNode.CreateComponent<CollisionShape>();
            shape.SetCapsule(0.7f, 1.8f, new Vector3(0.0f, 0.9f, 0.0f), Quaternion.Identity);

            // Create the character logic component, which takes care of steering the rigidbody
            // Remember it so that we can set the controls. Use a WeakPtr because the scene hierarchy already owns it
            // and keeps it alive as long as it's not removed from the hierarchy
            character = objectNode.CreateComponent<KinematicCharacter>();
            objectNode.CreateComponent<KinematicCharacterController>();
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