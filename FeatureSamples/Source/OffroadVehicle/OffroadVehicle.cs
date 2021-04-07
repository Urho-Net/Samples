//
// The MIT License (MIT)
//
// Copyright (c) 2021 Eli Aloni (A.K.A elix22)
// Lumak, Copyright (c) 2018
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
// C# port for https://github.com/Lumak/Urho3D-Offroad-Vehicle 

using Urho;
using UrhoNetSamples;
using Urho.Physics;
using Urho.Gui;
using Urho.Audio;

namespace OffroadVehicle
{
    public class OffroadVehicle : Sample
    {
        Camera camera;

        Vehicle vehicle;


        const float CameraDistance = 10.0f;

        /// The controllable vehicle component.


        Text textKmH_;

        // smooth step
        Quaternion vehicleRot_;
        Vector3 targetCameraPos_;


        [Preserve]
        public OffroadVehicle() : base(new ApplicationOptions(assetsFolder: "Data;CoreData")) { }

        protected override void Start()
        {
            base.Start();
            CreateScene();
            CreateVehicle();
            InitAudio();
            CreateInstructions();
            SubscribeToEvents();
        }

        protected override void Stop()
        {
            UnsubscribeFromEvents();
            base.Stop(); 
        }

        void CreateScene()
        {
            var cache = ResourceCache;

            scene = new Scene();

            // Create scene subsystem components
            scene.CreateComponent<Octree>();
            scene.CreateComponent<PhysicsWorld>();

            // Create camera and define viewport. We will be doing load / save, so it's convenient to create the camera outside the scene,
            // so that it won't be destroyed and recreated, and we don't have to redefine the viewport on load
            CameraNode = new Node();
            camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 500.0f;
            Renderer.SetViewport(0, new Viewport(Context, scene, camera, null));

            // Create static scene content. First create a zone for ambient lighting and fog control
            Node zoneNode = scene.CreateChild("Zone");
            Zone zone = zoneNode.CreateComponent<Zone>();
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 300.0f;
            zone.FogEnd = 500.0f;
            zone.SetBoundingBox(new BoundingBox(-2000.0f, 2000.0f));

            // Create a directional light with cascaded shadow mapping
            Node lightNode = scene.CreateChild("DirectionalLight");
            lightNode.SetDirection(new Vector3(0.3f, -0.5f, 0.425f));
            Light light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.Directional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);
            light.SpecularIntensity = 0.5f;

            // Create heightmap terrain with collision
            Node terrainNode = scene.CreateChild("Terrain");
            terrainNode.Position = (Vector3.Zero);
            Terrain terrain = terrainNode.CreateComponent<Terrain>();
            terrain.PatchSize = 64;
            terrain.Spacing = new Vector3(2.8f, 0.12f, 2.8f); // Spacing between vertices and vertical resolution of the height map
            terrain.Smoothing = true;
            terrain.SetHeightMap(cache.GetImage("Offroad/Terrain/HeightMapRace-257.png"));
            terrain.Material = cache.GetMaterial("Offroad/Terrain/TerrainRace-256.xml");
            // The terrain consists of large triangles, which fits well for occlusion rendering, as a hill can occlude all
            // terrain patches and other objects behind it
            terrain.Occluder = true;

            RigidBody body = terrainNode.CreateComponent<RigidBody>();
            body.CollisionLayer = 2; // Use layer bitmask 2 for static geometry
            CollisionShape shape = terrainNode.CreateComponent<CollisionShape>();
            shape.SetTerrain(0);

        }


        protected void CreateInstructions()
        {
            var cache = ResourceCache;

            textKmH_ = UI.Root.CreateText();
            textKmH_.SetFont(cache.GetFont("Fonts/Anonymous Pro.ttf"), 24);
            textKmH_.SetColor(Color.Green);
            textKmH_.TextAlignment = (HorizontalAlignment.Center);
            textKmH_.HorizontalAlignment = (HorizontalAlignment.Center);
            textKmH_.Position = new IntVector2(0, UI.Root.Height - 140);

            var textElement = new Text()
            {
                Value = "Use WASD keys and mouse/touch to move",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            textElement.SetFont(ResourceCache.GetFont("Fonts/Anonymous Pro.ttf"), 15);
            UI.Root.AddChild(textElement);

        }

        void CreateVehicle()
        {
            Node vehicleNode = scene.CreateChild("Vehicle");
            vehicleNode.Position = (new Vector3(273.0f, 7.0f, 77.0f));

            // Create the vehicle logic component
            vehicle = new Vehicle();
            vehicleNode.AddComponent(vehicle);
            // Create the rendering and physics components
            vehicle.Init();

            vehicleRot_ = vehicleNode.Rotation;
            Quaternion dir = new Quaternion(vehicleRot_.YawAngle, Vector3.Up);
            dir = dir * new Quaternion(vehicle.Controls.Yaw, Vector3.Up);
            dir = dir * new Quaternion(vehicle.Controls.Pitch, Vector3.Right);
            targetCameraPos_ = vehicleNode.Position - dir * new Vector3(0.0f, 0.0f, CameraDistance);
        }

        void InitAudio()
        {
            var audio = Audio;
            audio.Listener = (CameraNode.CreateComponent<SoundListener>());
        }

        void SubscribeToEvents()
        {
            Engine.PostUpdate += OnPostUpdate;
            Engine.PostUpdate += vehicle.OnPostUpdate;
            scene.GetComponent<PhysicsWorld>().PhysicsPreStep += (args => vehicle?.FixedUpdate(args.TimeStep));
            scene.GetComponent<PhysicsWorld>().PhysicsPostStep += (args => vehicle?.FixedPostUpdate(args.TimeStep));
        }

        void UnsubscribeFromEvents()
        {
            Engine.PostUpdate -= OnPostUpdate;
            Engine.PostUpdate -= vehicle.OnPostUpdate;
        }

        protected override void OnUpdate(float timeStep)
        {
            Input input = Input;
            UI ui = UI;

            if (vehicle != null)
            {
                if (ui.FocusElement == null)
                {
                    vehicle.Controls.Set(Vehicle.CtrlForward, input.GetKeyDown(Key.W));
                    vehicle.Controls.Set(Vehicle.CtrlBack, input.GetKeyDown(Key.S));
                    vehicle.Controls.Set(Vehicle.CtrlLeft, input.GetKeyDown(Key.A));
                    vehicle.Controls.Set(Vehicle.CtrlRight, input.GetKeyDown(Key.D));

                    // Add yaw & pitch from the mouse motion or touch input. Used only for the camera, does not affect motion
                    if (TouchEnabled)
                    {
                        for (uint i = 0; i < input.NumTouches; ++i)
                        {
                            TouchState state = input.GetTouch(i);
                            Camera camera = CameraNode.GetComponent<Camera>();
                            if (camera == null)
                                return;

                            var graphics = Graphics;
                            //     vehicle.Controls.Yaw += TouchSensitivity * camera.Fov / graphics.Height * state.Delta.X;
                            //     vehicle.Controls.Pitch += TouchSensitivity * camera.Fov / graphics.Height * state.Delta.Y;
                        }
                    }
                    else
                    {
                        //  vehicle.Controls.Yaw += (float)input.MouseMoveX * Vehicle.YawSensitivity;
                        //  vehicle.Controls.Pitch += (float)input.MouseMoveY * Vehicle.YawSensitivity;
                    }
                    // Limit pitch
                    vehicle.Controls.Pitch = MathHelper.Clamp(vehicle.Controls.Pitch, 0.0f, 80.0f);
                }
                else
                {
                    vehicle.Controls.Set(Vehicle.CtrlForward | Vehicle.CtrlBack | Vehicle.CtrlLeft | Vehicle.CtrlRight, false);
                }


                float spd = vehicle.GetSpeedKmH();
                if (spd < 0.0f) spd = 0.0f;
                int gear = vehicle.GetCurrentGear() + 1;
                float rpm = vehicle.GetCurrentRPM();

                string str = string.Format("{0:F1} KmH  \ngear: {1}  {2:F1} RPM",
                         spd, gear, rpm);

                textKmH_.Value = str;

            }
        }

        protected void OnPostUpdate(PostUpdateEventArgs args)
        {
            if (vehicle == null)
                return;

            Node vehicleNode = vehicle.Node;

            // Physics update has completed. Position camera behind vehicle
            Quaternion dir = Quaternion.FromAxisAngle(Vector3.UnitY, vehicleNode.Rotation.YawAngle);
            dir = dir * Quaternion.FromAxisAngle(Vector3.UnitY, vehicle.Controls.Yaw);
            dir = dir * Quaternion.FromAxisAngle(Vector3.UnitX, vehicle.Controls.Pitch);

            Vector3 cameraTargetPos = vehicleNode.Position - (dir * new Vector3(0.0f, -2.0f, CameraDistance));
            Vector3 cameraStartPos = vehicleNode.Position;

            // Raycast camera against static objects (physics collision mask 2)
            // and move it closer to the vehicle if something in between
            Ray cameraRay = new Ray(cameraStartPos, cameraTargetPos - cameraStartPos);
            float cameraRayLength = (cameraTargetPos - cameraStartPos).Length;
            PhysicsRaycastResult result = new PhysicsRaycastResult();
            scene.GetComponent<PhysicsWorld>().RaycastSingle(ref result, cameraRay, cameraRayLength, 2);
            if (result.Body != null)
            {
                cameraTargetPos = cameraStartPos + cameraRay.Direction * (result.Distance - 0.5f);
            }

            CameraNode.Position = cameraTargetPos;
            CameraNode.Rotation = dir;
        }

        
        /// <summary>
        /// Set custom Joystick layout for mobile platforms
        /// </summary>
        protected override string JoystickLayoutPatch =>
            "<patch>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Forward</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button0']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"W\" />" +
            "        </element>" +
            "    </add>" +
            "    <remove sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/attribute[@name='Is Visible']\" />" +
            "    <replace sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]/element[./attribute[@name='Name' and @value='Label']]/attribute[@name='Text']/@value\">Back</replace>" +
            "    <add sel=\"/element/element[./attribute[@name='Name' and @value='Button1']]\">" +
            "        <element type=\"Text\">" +
            "            <attribute name=\"Name\" value=\"KeyBinding\" />" +
            "            <attribute name=\"Text\" value=\"S\" />" +
            "        </element>" +
            "    </add>" +
            "</patch>";
    }
}